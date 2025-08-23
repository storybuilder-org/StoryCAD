using System.Runtime.CompilerServices;
using StoryCAD.Services.Backup;
using StoryCAD.ViewModels.SubViewModels;

namespace StoryCAD.Services.Locking;
public class SerializationLock : IDisposable
{
    /// <summary>
    /// Lock that controls the UI, Autosave, Backup.
    /// </summary>
    private static bool _canExecuteCommands = true;

    /// <summary>
    /// Returns if a lock is currently active.
    /// </summary>
    /// <returns>Lock status</returns>
    public static bool IsLocked() => !_canExecuteCommands;

    private readonly AutoSaveService _autoSaveService;
    private readonly BackupService _backupService;
    private readonly ILogService _logger;
    private readonly AppState _appState;
    private readonly PreferenceService _preferenceService;
    private string _caller;
    private bool _disposed;
    private static string currentHolder;

    public SerializationLock(AutoSaveService autoSaveService, BackupService backupService, ILogService logger,
    [CallerMemberName] string caller = null)
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
        _logger.Log(LogLevel.Info,$"Serialization lock acquired by {_caller}");
    }

    private void DisableCommands()
    {
        if (!_canExecuteCommands)
        {
            _logger.Log(LogLevel.Warn, $"{_caller} Tried to lock when already locked by {currentHolder}");

            if (currentHolder != _caller) //Some locks run twice i.e. datasource, (this shouldn't happen)
            {
                if (_appState.Headless)
                {
                    throw new InvalidOperationException($"Commands are already disabled by {currentHolder}");
                }
                else
                {
                    _logger.Log(LogLevel.Warn, $"{_caller} tried to lock when already locked by {currentHolder}");
                    return;
                }
            }
        }

        currentHolder = _caller;

        // Set your _canExecuteCommands flag to false
        // (Assuming you can access it via a shared service or static member)
        _logger.Log(LogLevel.Info, $"{_caller} has locked commands");
        _canExecuteCommands = false;
    }

    /// <summary>
    /// Re-enables commands.
    /// </summary>
    public void EnableCommands()
    {
        _canExecuteCommands = true;
        _logger.Log(LogLevel.Info, $"{_caller} has unlocked commands");
        currentHolder = null;

    }

    public void Dispose()
    {
        if (!_disposed)
        {
            // Re-enable background tasks and commands.
            EnableCommands();

            //Reenable backup/autosave 
            if (_preferenceService.Model.AutoSave)
            {
                _autoSaveService.StartAutoSave();
            }
            if (_preferenceService.Model.TimedBackup )
            {
                _backupService.StartTimedBackup();
            }
            
            _logger.Log(LogLevel.Info,"Serialization lock released: commands enabled, autosave and backup restarted.");
            _disposed = true;
            currentHolder = null;
            _logger.Log(LogLevel.Warn, $"{_caller} has unlocked commands");

        }
    }
}