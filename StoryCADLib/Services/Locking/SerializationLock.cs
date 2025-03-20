using System.Runtime.CompilerServices;
using StoryCAD.Services.Backup;
using StoryCAD.ViewModels.SubViewModels;

namespace StoryCAD.Services.Locking;
public class SerializationLock : IDisposable
{
    private readonly AutoSaveService _autoSaveService;
    private readonly BackupService _backupService;
    private readonly LogService _logger;
    private readonly ShellViewModel shellVm;
    private readonly OutlineViewModel outlineVm;
    private string _caller;
    private bool _disposed;
    private static string? currentHolder;

    public SerializationLock(AutoSaveService autoSaveService, BackupService backupService, LogService logger,
    [CallerMemberName] string caller = null)
    {
        _autoSaveService = autoSaveService;
        _backupService = backupService;
        _logger = logger;
        outlineVm = Ioc.Default.GetService<OutlineViewModel>();
        shellVm = Ioc.Default.GetService<ShellViewModel>();
        _caller = caller;
        // Acquire lock: disable commands, autosave, and backup.
        DisableCommands();
        _autoSaveService.StopAutoSave();
        _backupService.StopTimedBackup();
        _logger.Log(LogLevel.Info,$"Serialization lock acquired by {_caller}");
    }

    private void DisableCommands()
    {
        if (!outlineVm._canExecuteCommands)
        {
            _logger.Log(LogLevel.Warn, $"{_caller} Tried to lock when already locked by {currentHolder}");
            throw new InvalidOperationException($"Commands are already disabled by {currentHolder}");
        }

        currentHolder = _caller;

        // Set your _canExecuteCommands flag to false
        // (Assuming you can access it via a shared service or static member)
        _logger.Log(LogLevel.Warn, $"{_caller} has locked commands");
        outlineVm._canExecuteCommands = false;
    }

    /// <summary>
    /// Re-enables commands.
    /// </summary>
    public void EnableCommands()
    {
        outlineVm._canExecuteCommands = true;
        _logger.Log(LogLevel.Warn, $"{_caller} has unlocked commands");
        currentHolder = null;

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
            currentHolder = null;
            _logger.Log(LogLevel.Warn, $"{_caller} has unlocked commands");

        }
    }
}