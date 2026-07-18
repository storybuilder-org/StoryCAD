#if HAS_UNO
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace StoryCADLib.Services.Store;

/// <summary>
///     Thin managed side of the StoreKit shim's C ABI (shim contract in
///     StoryCADWiki: wiki/repos/Collaborator/sources/iap-billing-docs.md). Each async
///     export takes a request id and a completion callback; a single unmanaged callback fans results
///     back to the awaiting <see cref="TaskCompletionSource{T}" /> by id, or to the entitlement-change
///     handler for the id -1 transaction-updates push. Only compiled into the desktop head; the dylib
///     loads at runtime on macOS only and the load fails soft so non-MAS desktop builds keep working.
/// </summary>
// unsafe is scoped to the pointer-using members (not the class) so the async response
// awaiter can live here too: C# forbids await inside an unsafe context.
internal static partial class StoreKitInterop
{
    private const string Library = "StoryCADStoreKit";
    private const string DylibFile = "libStoryCADStoreKit.dylib";

    // StoreKit queries answer quickly or fail; purchase/restore show interactive system UI
    // (payment sheet / sign-in prompt) the user may sit on, so those get a much longer leash.
    private static readonly TimeSpan QueryTimeout = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan InteractiveTimeout = TimeSpan.FromMinutes(10);

    private static readonly ConcurrentDictionary<long, TaskCompletionSource<string>> Pending = new();
    private static long _nextId;
    // volatile: written on a managed thread, read on the unmanaged callback thread.
    private static volatile Action<string> _entitlementChanged;
    private static int _resolverRegistered;
    private static int _listenerStarted;

    // --- Native surface ---

    [LibraryImport(Library)]
    private static unsafe partial void storycad_iap_set_transaction_callback(
        delegate* unmanaged[Cdecl]<long, byte*, void> cb);

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    private static unsafe partial void storycad_iap_get_products(long requestId, string productIdsJson,
        delegate* unmanaged[Cdecl]<long, byte*, void> cb);

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    private static unsafe partial void storycad_iap_purchase(long requestId, string productId, string appAccountToken,
        delegate* unmanaged[Cdecl]<long, byte*, void> cb);

    // Issue #90 design section 10 "Credit packs" (step 10): finishes a consumable transaction the
    // shim deliberately left open (StoreKitShim.swift's storycad_iap_purchase), once the caller has
    // confirmed the Worker credited it.
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    private static unsafe partial void storycad_iap_finish_transaction(long requestId, string transactionId,
        delegate* unmanaged[Cdecl]<long, byte*, void> cb);

    [LibraryImport(Library)]
    private static unsafe partial void storycad_iap_current_entitlements(long requestId,
        delegate* unmanaged[Cdecl]<long, byte*, void> cb);

    [LibraryImport(Library)]
    private static unsafe partial void storycad_iap_restore(long requestId,
        delegate* unmanaged[Cdecl]<long, byte*, void> cb);

    // --- Callback ---

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static unsafe void OnCallback(long requestId, byte* payload)
    {
        // Copy immediately; the pointer is valid only for this call.
        var json = Marshal.PtrToStringUTF8((IntPtr)payload) ?? string.Empty;
        if (requestId == -1)
        {
            _entitlementChanged?.Invoke(json);
            return;
        }

        if (Pending.TryRemove(requestId, out var tcs))
        {
            tcs.TrySetResult(json);
        }
    }

    // --- Managed API ---

    internal static unsafe Task<string> GetProductsAsync(string productIdsJson, CancellationToken ct = default)
    {
        var (id, tcs) = NewRequest();
        storycad_iap_get_products(id, productIdsJson, &OnCallback);
        return AwaitResponseAsync(id, tcs, QueryTimeout, ct);
    }

    internal static unsafe Task<string> PurchaseAsync(string productId, string appAccountToken,
        CancellationToken ct = default)
    {
        var (id, tcs) = NewRequest();
        storycad_iap_purchase(id, productId, appAccountToken, &OnCallback);
        return AwaitResponseAsync(id, tcs, InteractiveTimeout, ct);
    }

    internal static unsafe Task<string> FinishTransactionAsync(string transactionId, CancellationToken ct = default)
    {
        var (id, tcs) = NewRequest();
        storycad_iap_finish_transaction(id, transactionId, &OnCallback);
        // Not interactive (no system UI), but not a pure query either -- give it the same leash as
        // PurchaseAsync since it can race a purchase's own in-flight interactive call on first use.
        return AwaitResponseAsync(id, tcs, QueryTimeout, ct);
    }

    internal static unsafe Task<string> CurrentEntitlementsAsync(CancellationToken ct = default)
    {
        var (id, tcs) = NewRequest();
        storycad_iap_current_entitlements(id, &OnCallback);
        return AwaitResponseAsync(id, tcs, QueryTimeout, ct);
    }

    internal static unsafe Task<string> RestoreAsync(CancellationToken ct = default)
    {
        var (id, tcs) = NewRequest();
        storycad_iap_restore(id, &OnCallback);
        return AwaitResponseAsync(id, tcs, InteractiveTimeout, ct);
    }

    // A response that never arrives must not strand the caller (or any lock held above it)
    // forever: evict the pending entry and fail the call on timeout or cancellation. A late
    // native callback then finds no entry and is ignored.
    private static async Task<string> AwaitResponseAsync(long id, TaskCompletionSource<string> tcs,
        TimeSpan timeout, CancellationToken ct)
    {
        try
        {
            return await tcs.Task.WaitAsync(timeout, ct);
        }
        catch
        {
            Pending.TryRemove(id, out _);
            throw;
        }
    }

    /// <summary>
    ///     Registers the entitlement-change handler and starts the shim's transaction listener once.
    /// </summary>
    internal static unsafe void SetEntitlementChangedHandler(Action<string> handler)
    {
        _entitlementChanged = handler;
        if (Interlocked.Exchange(ref _listenerStarted, 1) == 0)
        {
            storycad_iap_set_transaction_callback(&OnCallback);
        }
    }

    /// <summary>
    ///     True when the dylib resolves and loads. Registers the resolver as a side effect. Called
    ///     only after an <c>OperatingSystem.IsMacOS()</c> check; never throws.
    /// </summary>
    internal static bool IsAvailable()
    {
        EnsureResolver();
        try
        {
            return NativeLibrary.TryLoad(Library, typeof(StoreKitInterop).Assembly, null, out _);
        }
        catch
        {
            return false;
        }
    }

    private static (long, TaskCompletionSource<string>) NewRequest()
    {
        var id = Interlocked.Increment(ref _nextId);
        var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        Pending[id] = tcs;
        return (id, tcs);
    }

    private static void EnsureResolver()
    {
        if (Interlocked.Exchange(ref _resolverRegistered, 1) == 0)
        {
            NativeLibrary.SetDllImportResolver(typeof(StoreKitInterop).Assembly, Resolve);
        }
    }

    // Prefer the signed copy in Contents/Frameworks; fall back to beside the executable for local
    // dev runs; last resort let the OS search (rpath/DYLD).
    private static IntPtr Resolve(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (libraryName != Library)
        {
            return IntPtr.Zero;
        }

        var baseDir = AppContext.BaseDirectory;
        var frameworks = Path.Combine(baseDir, "..", "Frameworks", DylibFile);
        if (File.Exists(frameworks) && NativeLibrary.TryLoad(frameworks, out var h1))
        {
            return h1;
        }

        var beside = Path.Combine(baseDir, DylibFile);
        if (File.Exists(beside) && NativeLibrary.TryLoad(beside, out var h2))
        {
            return h2;
        }

        return NativeLibrary.TryLoad(DylibFile, out var h3) ? h3 : IntPtr.Zero;
    }
}
#endif
