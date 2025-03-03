
using StoryCAD.Services.Backup;

namespace StoryCAD.Services.Locking;
public class SerializationLock : IDisposable
{
    private readonly AutoSaveService _autoSaveService;
    private readonly BackupService _backupService;
    private readonly LogService _logger; // or however you perform logging
    private readonly ShellViewModel shellVm;
    private bool _disposed;

    public SerializationLock(AutoSaveService autoSaveService, BackupService backupService, LogService logger)
    {
        _autoSaveService = autoSaveService;
        _backupService = backupService;
        _logger = logger;
        shellVm = Ioc.Default.GetService<ShellViewModel>();

        // Acquire lock: disable commands, autosave, and backup.
        DisableCommands();
        _autoSaveService.StopAutoSave();
        _backupService.StopTimedBackup();
        _logger.Log(LogLevel.Info,"Serialization lock acquired: commands disabled, autosave and backup stopped.");
    }

    private void DisableCommands()
    {
        // Set your _canExecuteCommands flag to false
        // (Assuming you can access it via a shared service or static member)
        shellVm._canExecuteCommands = false;
    }

    public void EnableCommands()
    {
        // Re-enable the commands after the operation.
        shellVm._canExecuteCommands = true;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            // Re-enable background tasks and commands.
            EnableCommands();
            _autoSaveService.StartAutoSave();
            _backupService.StartTimedBackup();
            _logger.Log(LogLevel.Info,"Serialization lock released: commands enabled, autosave and backup restarted.");
            _disposed = true;
        }
    }
}