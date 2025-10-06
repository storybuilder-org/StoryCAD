using System.Runtime.CompilerServices;

namespace StoryCAD.Services.Locking;

/// <summary>
///     A single shared gate used to serialize saves, backups, and manual commands.
///     Supports reentrant locking within the same async context.
/// </summary>
public sealed class SerializationLock : IDisposable
{
    // One shared gate for the whole process.
    private static readonly SemaphoreSlim Gate = new(1, 1);

    // AsyncLocal persists data across await statements and thread context switches, making it suitable for tracking logical data
    // through an asynchronous call stack. In this case it stores and propagates the current lock depth, which is incremented on
    // each nested lock acquisition and decremented on each release. If the depth is greater than zero, it indicates that the current
    // execution context already holds the lock, allowing reentrant acquisition without deadlock.
    private static readonly AsyncLocal<int> Depth = new();
    private readonly string _caller;
    private readonly bool _isNested;
    private readonly ILogService _logger;

    private bool _held;

    // Constructor with optional logger
    public SerializationLock(ILogService logger = null, [CallerMemberName] string caller = "")
    {
        _caller = caller;
        _logger = logger;

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
            _isNested = true;
            return;
        }

        // First acquisition in this context
        _logger?.Log(LogLevel.Info, $"[CallerMemberName] {_caller} waiting for lock...");
        Gate.Wait();
        Depth.Value = 1;
        _held = true;
        _isNested = false;
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
    /// </summary>
    public static async Task RunExclusiveAsync(
        Func<CancellationToken, Task> body,
        CancellationToken ct = default,
        ILogService logger = null,
        [CallerMemberName] string caller = "")
    {
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
                await body(ct).ConfigureAwait(false);
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
