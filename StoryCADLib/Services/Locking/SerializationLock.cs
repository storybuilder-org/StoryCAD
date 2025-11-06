#pragma warning disable CS8632 // Nullable annotations used without nullable context
using System.Runtime.CompilerServices;

namespace StoryCADLib.Services.Locking;

/// <summary>
///     A single shared gate used to serialize saves, backups, and manual commands.
///     Supports reentrant locking within the same async context.
/// </summary>
public sealed class SerializationLock : IDisposable
{
    // One shared gate for the whole process.
    private static readonly SemaphoreSlim Gate = new(1, 1);

    // NEW: simple UI-thread probe; default = false so headless/tests are safe.
    // Wire this once at app startup: SerializationLock.ConfigureUi(() => windowing.GlobalDispatcher.HasThreadAccess);
    private static Func<bool> _uiHasThreadAccess = static () => false;
    public static void ConfigureUi(Func<bool> uiHasThreadAccess) => _uiHasThreadAccess = uiHasThreadAccess ?? (() => false);

    // AsyncLocal persists data across await statements and thread context switches, making it suitable for tracking logical data
    // through an asynchronous call stack. In this case it stores and propagates the current lock depth, which is incremented on
    // each nested lock acquisition and decremented on each release. If the depth is greater than zero, it indicates that the current
    // execution context already holds the lock, allowing reentrant acquisition without deadlock.
    private static readonly AsyncLocal<int> Depth = new();
    private readonly string _caller;
    private readonly ILogService _logger;

    private bool _held;

    // Constructor with optional logger
    public SerializationLock(ILogService logger = null, [CallerMemberName] string caller = "")
    {
        _caller = caller;
        _logger = logger;

        if (!IsIdle && _uiHasThreadAccess())
        {
            _held = false;
            _logger?.Log(LogLevel.Warn, $"{caller} skipped: UI-thread sync lock while busy.");
            return;   // prevents deadlock
        }

        // Check if we already hold the lock in this async context
        if (Depth.Value > 0)
        {
            // Nested acquisition - don't wait or fire events
            if (Depth.Value < int.MaxValue - 1) // Prevent overflow
            {
                Depth.Value++;
                _logger?.Log(LogLevel.Warn,
                    $"[CallerMemberName] REENTRANT: {_caller} acquired nested lock (depth: {Depth.Value})");
            }

            _held = true;
            return;
        }

        // First acquisition in this context
        _logger?.Log(LogLevel.Info, $"[CallerMemberName] {_caller} waiting for lock...");
        Gate.Wait();
        Depth.Value = 1;
        _held = true;
        _logger?.Log(LogLevel.Info, $"[CallerMemberName] {_caller} acquired lock (depth: 1)");

        // Only fire event for outermost lock acquisition
        CanExecuteStateChanged?.Invoke(null, EventArgs.Empty);
    }

    /// <summary>True when nothing is currently saving/backing up.</summary>
    public static bool IsIdle => Gate.CurrentCount == 1 && Depth.Value == 0;

    public void Dispose()
    {
        if (!_held)
        {
            return;
        }

        _held = false;

        // Atomically decrement and check depth
        var newDepth = Math.Max(0, Depth.Value - 1);
        Depth.Value = newDepth;

        // Only release gate and fire event when fully released
        if (newDepth == 0)
        {
            _logger?.Log(LogLevel.Info, $"[CallerMemberName] {_caller} releasing lock (depth: 0)");
            Gate.Release();
            CanExecuteStateChanged?.Invoke(null, EventArgs.Empty);
        }
        else
        {
            // For nested releases, don't fire events since lock state hasn't changed
            _logger?.Log(LogLevel.Warn,
                $"[CallerMemberName] REENTRANT: {_caller} releasing nested lock (depth: {newDepth})");
        }
    }

    /// <summary>
    ///     Raised whenever the lock state changes (acquire/release).
    ///     Used by ShellViewModel / Shell.xaml.cs to refresh CanExecute.
    /// </summary>
    public static event EventHandler? CanExecuteStateChanged;

    /// <summary>Preserve existing predicate name used by ShellViewModel CanExecute.</summary>
    public static bool CanExecuteCommands() => IsIdle;

    /// <summary>
    ///     Awaitable helper when you want the gate to cover async work end-to-end.
    ///     TEMPORARY PRODUCTION SAFETY GUARD:
    ///       - If the UI thread calls while the gate is not idle, return immediately to avoid hangs
    ///         from re-entrant UI events or invalid nested calls.
    ///       - Background callers still await the gate normally.
    /// </summary>
    public static async Task RunExclusiveAsync(
        Func<CancellationToken, Task> body,
        CancellationToken ct = default,
        ILogService logger = null,
        [CallerMemberName] string caller = "")
    {
        // --- TEMP GUARD: centralize Jake’s workaround inside the lock ---
        if (!IsIdle && _uiHasThreadAccess())
        {
            logger?.Log(LogLevel.Warn, $"[CallerMemberName] {caller} skipped: UI-thread call while busy.");
            return; // ignore UI-triggered work while another op holds the gate
        }

        // Check if we already hold the lock
        if (Depth.Value > 0)
        {
            // Already held - just increment depth and run
            if (Depth.Value < int.MaxValue - 1)
            {
                Depth.Value++;
                logger?.Log(LogLevel.Warn,
                    $"[CallerMemberName] REENTRANT: {caller} acquired nested async lock (depth: {Depth.Value})");
            }

            try
            {
                await body(ct).ConfigureAwait(false); // (left as-is to minimize churn)
            }
            finally
            {
                var newDepth = Math.Max(0, Depth.Value - 1);
                Depth.Value = newDepth;
                logger?.Log(LogLevel.Warn,
                    $"[CallerMemberName] REENTRANT: {caller} releasing nested async lock (depth: {newDepth})");
            }

            return;
        }

        // First acquisition - wait for lock
        logger?.Log(LogLevel.Info, $"[CallerMemberName] {caller} waiting for async lock...");
        await Gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            Depth.Value = 1;
            logger?.Log(LogLevel.Info, $"[CallerMemberName] {caller} acquired async lock (depth: 1)");
            CanExecuteStateChanged?.Invoke(null, EventArgs.Empty);
            await body(ct).ConfigureAwait(false);
        }
        finally
        {
            Depth.Value = 0;
            logger?.Log(LogLevel.Info, $"[CallerMemberName] {caller} releasing async lock (depth: 0)");
            Gate.Release();
            CanExecuteStateChanged?.Invoke(null, EventArgs.Empty);
        }
    }
}
