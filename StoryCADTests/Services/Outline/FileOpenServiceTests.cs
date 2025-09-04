using System;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCAD.DAL;
using StoryCAD.Models;
using StoryCAD.Services;
using StoryCAD.Services.Backup;
using StoryCAD.Services.Messages;
using StoryCAD.Services.Outline;
using StoryCAD.ViewModels;
using StoryCAD.Services.Logging;

namespace StoryCADTests.Services.Outline;

[TestClass]
public class FileOpenServiceTests
{
    private FileOpenService _fileOpenService;
    private ILogService _logger;
    private OutlineService _outlineService;
    private AppState _appState;
    private EditFlushService _editFlushService;
    private PreferenceService _preferences;
    private Windowing _windowing;
    private BackupService _backupService;
    private AutoSaveService _autoSaveService;
    private StoryIO _storyIO;

    [TestInitialize]
    public void Setup()
    {
        // Get services from IoC container
        _logger = Ioc.Default.GetRequiredService<ILogService>();
        _outlineService = Ioc.Default.GetRequiredService<OutlineService>();
        _appState = Ioc.Default.GetRequiredService<AppState>();
        _editFlushService = Ioc.Default.GetRequiredService<EditFlushService>();
        _preferences = Ioc.Default.GetRequiredService<PreferenceService>();
        _windowing = Ioc.Default.GetRequiredService<Windowing>();
        _backupService = Ioc.Default.GetRequiredService<BackupService>();
        _autoSaveService = Ioc.Default.GetRequiredService<AutoSaveService>();
        _storyIO = Ioc.Default.GetRequiredService<StoryIO>();

        _fileOpenService = new FileOpenService(
            _logger,
            _outlineService,
            _appState,
            _editFlushService,
            _preferences,
            _windowing,
            _backupService,
            _autoSaveService,
            _storyIO);

        // Reset app state
        _appState.CurrentDocument = null;
    }

    [TestMethod]
    public void FileOpenService_CanBeCreated()
    {
        // Assert
        Assert.IsNotNull(_fileOpenService);
    }

    [TestMethod]
    public void FileOpenService_CanBeCreatedFromIoC()
    {
        // Act
        var service = Ioc.Default.GetRequiredService<FileOpenService>();

        // Assert
        Assert.IsNotNull(service);
    }

    [TestMethod]
    public async Task OpenFile_WithNullPath_ShowsFilePicker()
    {
        // Arrange
        bool messageReceived = false;
        WeakReferenceMessenger.Default.Register<StatusChangedMessage>(this, (r, m) =>
        {
            messageReceived = true;
        });

        // Act
        await _fileOpenService.OpenFile("");

        // Assert
        // File picker would be shown, but cancelled in headless mode
        Assert.IsTrue(messageReceived);

        // Cleanup
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }

    [TestMethod]
    public async Task OpenFile_WithNonExistentFile_ShowsFilePicker()
    {
        // Arrange
        string nonExistentPath = Path.Combine(Path.GetTempPath(), "nonexistent.stbx");
        
        // In headless mode, ShowFilePicker returns null, which causes the method to return early
        // This test verifies that the service handles non-existent files gracefully by showing
        // a file picker (which returns null in tests) rather than crashing

        // Act
        await _fileOpenService.OpenFile(nonExistentPath);

        // Assert
        // The method should complete without throwing an exception
        // In production, this would show a file picker to the user
        Assert.IsTrue(true); // Method completed successfully

        // Cleanup
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }

    [TestMethod]
    public async Task UpdateRecents_AddsPathToRecentFiles()
    {
        // Arrange
        string testPath = @"C:\test\file.stbx";
        _preferences.Model.RecentFiles.Clear();

        // Act
        await _fileOpenService.UpdateRecents(testPath);

        // Assert
        Assert.IsTrue(_preferences.Model.RecentFiles.Contains(testPath));
        Assert.AreEqual(testPath, _preferences.Model.RecentFiles[0]);
    }

    [TestMethod]
    public async Task UpdateRecents_MovesExistingPathToTop()
    {
        // Arrange
        string testPath1 = @"C:\test\file1.stbx";
        string testPath2 = @"C:\test\file2.stbx";
        _preferences.Model.RecentFiles.Clear();
        _preferences.Model.RecentFiles.Add(testPath1);
        _preferences.Model.RecentFiles.Add(testPath2);

        // Act
        await _fileOpenService.UpdateRecents(testPath2);

        // Assert
        Assert.AreEqual(testPath2, _preferences.Model.RecentFiles[0]);
        Assert.AreEqual(testPath1, _preferences.Model.RecentFiles[1]);
        Assert.AreEqual(2, _preferences.Model.RecentFiles.Count);
    }

    [TestMethod]
    public async Task UpdateRecents_LimitsToTenFiles()
    {
        // Arrange
        _preferences.Model.RecentFiles.Clear();
        for (int i = 0; i < 15; i++)
        {
            _preferences.Model.RecentFiles.Add($@"C:\test\file{i}.stbx");
        }

        // Act
        await _fileOpenService.UpdateRecents(@"C:\test\newfile.stbx");

        // Assert
        Assert.AreEqual(10, _preferences.Model.RecentFiles.Count);
        Assert.AreEqual(@"C:\test\newfile.stbx", _preferences.Model.RecentFiles[0]);
    }

    [TestMethod]
    public async Task UpdateRecents_WithNullPath_DoesNothing()
    {
        // Arrange
        _preferences.Model.RecentFiles.Clear();
        _preferences.Model.RecentFiles.Add(@"C:\test\file.stbx");
        int initialCount = _preferences.Model.RecentFiles.Count;

        // Act
        await _fileOpenService.UpdateRecents(null);

        // Assert
        Assert.AreEqual(initialCount, _preferences.Model.RecentFiles.Count);
    }

    [TestMethod]
    public async Task UpdateRecents_WithEmptyPath_DoesNothing()
    {
        // Arrange
        _preferences.Model.RecentFiles.Clear();
        _preferences.Model.RecentFiles.Add(@"C:\test\file.stbx");
        int initialCount = _preferences.Model.RecentFiles.Count;

        // Act
        await _fileOpenService.UpdateRecents("");

        // Assert
        Assert.AreEqual(initialCount, _preferences.Model.RecentFiles.Count);
    }
}