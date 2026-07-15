#if HAS_UNO
using System.Threading;
using StoryCADLib.Services.Locking;

namespace StoryCADLib.Services.Store;

/// <summary>
///     macOS <see cref="IStoreService" /> over the StoreKit shim (<see cref="StoreKitInterop" />).
///     Registered only when running on macOS and the dylib resolves; elsewhere the desktop head uses
///     <see cref="NullStoreService" />. Gating decisions read entitlement <see cref="EntitlementState" />,
///     not transaction presence.
/// </summary>
public sealed class MacStoreService : IStoreService
{
    private readonly Windowing _windowing;
    private readonly ILogService _logService;

    public MacStoreService(Windowing windowing, ILogService logService)
    {
        _windowing = windowing;
        _logService = logService;
        // Start the transaction-updates listener; renewals and completed Ask-to-Buy purchases arrive here.
        StoreKitInterop.SetEntitlementChangedHandler(OnNativeEntitlementChanged);
    }

    public bool IsSupported => true;

    public IReadOnlyList<string> ProductIds => StoreConfig.AppleProductIds;

    public event EventHandler<StoreEntitlement> EntitlementChanged;

    public async Task<IReadOnlyList<StoreProduct>> GetProductsAsync(IReadOnlyList<string> productIds,
        CancellationToken ct = default)
    {
        var payload = await StoreKitInterop.GetProductsAsync(StoreKitPayloads.SerializeProductIds(productIds), ct);
        return StoreKitPayloads.ParseProducts(payload);
    }

    public async Task<PurchaseResult> PurchaseAsync(string productId, string userGuid, CancellationToken ct = default)
    {
        // Null (not empty) token so the shim omits the option rather than rejecting an invalid UUID.
        var appAccountToken = string.IsNullOrWhiteSpace(userGuid) ? null : userGuid;
        var payload = await StoreKitInterop.PurchaseAsync(productId, appAccountToken, ct);
        return StoreKitPayloads.ParsePurchase(payload);
    }

    public async Task<IReadOnlyList<StoreEntitlement>> GetCurrentEntitlementsAsync(CancellationToken ct = default)
    {
        var payload = await StoreKitInterop.CurrentEntitlementsAsync(ct);
        return StoreKitPayloads.ParseEntitlements(payload);
    }

    public async Task RestoreAsync(CancellationToken ct = default)
    {
        // The shim runs AppStore.sync() (system sign-in prompt) and returns fresh entitlements;
        // callers re-read via GetPurchaseProofAsync, so the payload is only logged here.
        var payload = await StoreKitInterop.RestoreAsync(ct);
        _logService.Log(LogLevel.Info, $"Store restore returned {StoreKitPayloads.ParseEntitlements(payload).Count} entitlement(s).");
    }

    public async Task<PurchaseProof> GetPurchaseProofAsync(string userGuid, CancellationToken ct = default)
    {
        var entitlements = await GetCurrentEntitlementsAsync(ct);

        // Access-granting states only; StoreKit already excludes lapsed/revoked from currentEntitlements,
        // but guard anyway so a stray Expired/Revoked entry never becomes proof.
        var current = entitlements.FirstOrDefault(e =>
            e.State is EntitlementState.Active or EntitlementState.GracePeriod or EntitlementState.BillingRetry);
        if (current is null || string.IsNullOrEmpty(current.Jws))
        {
            return null;
        }

        return new PurchaseProof("apple", current.Jws, current.ProductId, userGuid);
    }

    // Delivered on a Swift task thread; marshal to the UI thread before raising.
    private void OnNativeEntitlementChanged(string payload)
    {
        var entitlement = StoreKitPayloads.ParseEntitlements(payload).FirstOrDefault();
        if (entitlement is null)
        {
            return;
        }

        _ = _windowing.GlobalDispatcher.EnqueueAsync(() => EntitlementChanged?.Invoke(this, entitlement));
    }
}
#endif
