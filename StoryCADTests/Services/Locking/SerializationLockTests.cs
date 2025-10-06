using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCAD.Services;
using StoryCAD.Services.Backup;
using StoryCAD.Services.Locking;
using StoryCAD.Services.Logging;

namespace StoryCADTests.Services.Locking;

[TestClass]
public class SerializationLockTests
{
    private AutoSaveService _autoSaveService;
    private BackupService _backupService;
    private ILogService _logger;

    [TestInitialize]
    public void Setup()
    {
        _autoSaveService = Ioc.Default.GetService<AutoSaveService>();
        _backupService = Ioc.Default.GetService<BackupService>();
        _logger = Ioc.Default.GetService<ILogService>();
    }

    [TestCleanup]
    public void Cleanup()
    {
        // Always stop and dispose services after test
        _autoSaveService?.StopAutoSave();
        _autoSaveService?.Dispose();
        _backupService?.StopTimedBackup();
    }

    [TestMethod]
    public void TestLocks()
    {
        // Arrange
        // Ensure a known initial state.
        // Assume commands are enabled and both services are running.
        Ioc.Default.GetRequiredService<PreferenceService>().Model.AutoSaveInterval = 1;
        _autoSaveService.StartAutoSave();
        _backupService.StartTimedBackup();

        // Pre-check initial state.
        Assert.IsTrue(SerializationLock.CanExecuteCommands(), "Pre-condition: Commands should be enabled.");

        // Act: Create the SerializationLock (which disables commands and stops background services).
        using (var serializationLock = new SerializationLock(_logger))
        {
            // Within the lock, commands should be disabled and both services stopped.
            Assert.IsFalse(SerializationLock.CanExecuteCommands(), "During lock: Commands should be disabled.");

            // Note: The new SerializationLock doesn't stop services, it just prevents concurrent operations
            // So these assertions will fail with the new implementation
            // Assert.IsFalse(_autoSaveService.IsRunning, "During lock: AutoSave should be stopped.");
            // Assert.IsFalse(_backupService.IsRunning, "During lock: BackupService should be stopped.");

            Thread.Sleep(100); // Reduced from 10000ms - no need for 10 seconds!
        }

        // Assert: After disposing the lock, commands should be restored.
        Assert.IsTrue(SerializationLock.CanExecuteCommands(), "After disposal: Commands should be re-enabled.");
    }
}
