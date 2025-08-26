using System;
using System.Threading;
using System.Threading.Tasks;

namespace StoryCAD.Services.Locking
{
    /// <summary>
    /// A single shared gate used to serialize saves, backups, and manual commands.
    /// </summary>
    public sealed class SerializationLock : IDisposable
    {
        // One shared gate for the whole process.
        private static readonly SemaphoreSlim Gate = new(1, 1);
        private bool _held;

        /// <summary>
        /// Raised whenever the lock state changes (acquire/release).
        /// Used by ShellViewModel / Shell.xaml.cs to refresh CanExecute.
        /// </summary>
        public static event EventHandler? CanExecuteStateChanged;

        // Keep a permissive ctor so existing "using (new SerializationLock(...))" still compiles.
        public SerializationLock(params object[] _)
        {
            Gate.Wait();      // acquire synchronously (matches "using" pattern)
            _held = true;
            CanExecuteStateChanged?.Invoke(null, EventArgs.Empty);
        }

        public void Dispose()
        {
            if (_held)
            {
                _held = false;
                Gate.Release();
                CanExecuteStateChanged?.Invoke(null, EventArgs.Empty);
            }
        }

        /// <summary>True when nothing is currently saving/backing up.</summary>
        public static bool IsIdle => Gate.CurrentCount == 1;

        /// <summary>Preserve existing predicate name used by ShellViewModel CanExecute.</summary>
        public static bool CanExecuteCommands() => IsIdle;

        /// <summary>
        /// Awaitable helper when you want the gate to cover async work end-to-end.
        /// </summary>
        public static async Task RunExclusiveAsync(Func<CancellationToken, Task> body, CancellationToken ct = default)
        {
            await Gate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                CanExecuteStateChanged?.Invoke(null, EventArgs.Empty);
                await body(ct).ConfigureAwait(false);
            }
            finally
            {
                Gate.Release();
                CanExecuteStateChanged?.Invoke(null, EventArgs.Empty);
            }
        }
    }
}
