using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCAD.Services;
using StoryCAD.Services.Backup;
using StoryCAD.Services.Locking;
using StoryCAD.Services.Logging;
using StoryCAD.ViewModels.SubViewModels;

namespace StoryCADTests;

[TestClass]
public class LockTest
{
    [TestMethod]
    public void TestLocks()
    {
        // Arrange: get service instances from IoC.
        var autoSaveService = Ioc.Default.GetService<AutoSaveService>();
        var backupService = Ioc.Default.GetService<BackupService>();
        var logger = Ioc.Default.GetService<ILogService>();
        var outlineVm = Ioc.Default.GetService<OutlineViewModel>();

        // Ensure a known initial state.
        // Assume commands are enabled and both services are running.
        Ioc.Default.GetRequiredService<PreferenceService>().Model.AutoSaveInterval = 1;
        autoSaveService.StartAutoSave();
        backupService.StartTimedBackup();

        // Pre-check initial state.
        Assert.IsTrue(SerializationLock.CanExecuteCommands(), "Pre-condition: Commands should be enabled.");

        // Act: Create the SerializationLock (which disables commands and stops background services).
        using (var serializationLock = new SerializationLock(autoSaveService, backupService, logger))
        {
            // Within the lock, commands should be disabled and both services stopped.
            Assert.IsFalse(SerializationLock.CanExecuteCommands(), "During lock: Commands should be disabled.");
            Assert.IsFalse(autoSaveService.IsRunning, "During lock: AutoSave should be stopped.");
            Assert.IsFalse(backupService.IsRunning, "During lock: BackupService should be stopped.");
            System.Threading.Thread.Sleep(10000); 
        }

        // Assert: After disposing the lock, commands and services should be restored.
        Assert.IsTrue(SerializationLock.CanExecuteCommands(), "After disposal: Commands should be re-enabled.");
    }
}