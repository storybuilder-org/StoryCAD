using System;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCAD.Services.Backup;
using StoryCAD.ViewModels.SubViewModels;

namespace StoryCAD.Services.Locking
{
    /// <summary>
    /// Disposable lock that disables UI commands, autosave, and timed backup while in scope.
    /// Notifies listeners when CanExecute state changes so bound commands can requery.
    /// </summary>
    public class SerializationLock : IDisposable
    {
        /// <summary>
        /// Flag controlling whether UI commands may execute.
        /// </summary>
        private static bool _canExecuteCommands = true;

        /// <summary>
        /// Fired whenever <see cref="_canExecuteCommands"/> changes.
        /// ViewModels should subscribe and call IRelayCommand.NotifyCanExecuteChanged() on the UI thread.
        /// </summary>
        public static event EventHandler? CanExecuteStateChanged;

        /// <summary>
        /// Returns true if commands may execute (i.e., no active SerializationLock).
        /// </summary>
        public static bool CanExecuteCommands() => _canExecuteCommands;

        private readonly AutoSaveService _autoSaveService;
        private readonly BackupService _backupService;
        private readonly ILogService _logger;
        private readonly AppState _appState;
        private readonly PreferenceService _preferenceService;
        private string? _caller;
        private bool _disposed;
        private static string? currentHolder;

        public SerializationLock(
            AutoSaveService autoSaveService,
            BackupService backupService,
            ILogService logger,
            [CallerMemberName] string? caller = null)
        {
            _autoSaveService = autoSaveService;
            _backupService = backupService;
            _logger = logger;
            _appState = Ioc.Default.GetRequiredService<AppState>();
            _preferenceService = Ioc.Default.GetRequiredService<PreferenceService>();

            _caller = caller;

            // Acquire lock: disable commands, autosave, and backup.
            DisableCommands();
            _autoSaveService.StopAutoSave();
            _backupService.StopTimedBackup();
            _logger.Log(LogLevel.Info, $"Serialization lock acquired by {_caller}");
        }

        private void DisableCommands()
        {
            if (!_canExecuteCommands)
            {
                _logger.Log(LogLevel.Warn, $"{_caller} Tried to lock when already locked by {currentHolder}");

                // Some callers legitimately nest; we guard against different owners.
                if (currentHolder != _caller)
                {
                    if (_appState.Headless)
                    {
                        throw new InvalidOperationException($"Commands are already disabled by {currentHolder}");
                    }
                    else
                    {
                        // Ignore duplicate/competing lock request in UI mode.
                        _logger.Log(LogLevel.Warn, $"{_caller} tried to lock when already locked by {currentHolder}");
                        return;
                    }
                }
            }

            currentHolder = _caller;

            // Disable commands if not already disabled.
            if (_canExecuteCommands)
            {
                _canExecuteCommands = false;
                _logger.Log(LogLevel.Info, $"{_caller} has locked commands");
                RaiseCanExecuteStateChanged();
            }
        }

        /// <summary>
        /// Re-enables commands and raises notification. Public for rare manual resets.
        /// </summary>
        public void EnableCommands()
        {
            if (!_canExecuteCommands)
            {
                _canExecuteCommands = true;
                _logger.Log(LogLevel.Info, $"{_caller} has unlocked commands");
                currentHolder = null;
                RaiseCanExecuteStateChanged();
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            // Re-enable background tasks and commands.
            EnableCommands();

            // Re-enable backup/autosave according to user preferences.
            if (_preferenceService.Model.AutoSave)
            {
                _autoSaveService.StartAutoSave();
            }
            if (_preferenceService.Model.TimedBackup)
            {
                _backupService.StartTimedBackup();
            }

            _logger.Log(LogLevel.Info, "Serialization lock released: commands enabled, autosave and backup restarted.");
            _disposed = true;

            // Extra visibility in logs (matches previous behavior).
            _logger.Log(LogLevel.Warn, $"{_caller} has unlocked commands");
        }

        private static void RaiseCanExecuteStateChanged()
        {
            try
            {
                CanExecuteStateChanged?.Invoke(null, EventArgs.Empty);
            }
            catch
            {
                // Never let event handler exceptions take down the lock path.
            }
        }
    }
}
