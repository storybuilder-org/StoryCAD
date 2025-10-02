using System;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCAD.Models;
using StoryCAD.Services;
using StoryCAD.Services.Backup;
using StoryCAD.Services.Messages;
using StoryCAD.Services.Outline;
using StoryCAD.Services.Logging;

namespace StoryCADTests.Services.Outline;

[TestClass]
public class FileCreateServiceTests
{
    private FileCreateService _fileCreateService;
    private ILogService _logger;
    private OutlineService _outlineService;
    private AppState _appState;
    private PreferenceService _preferences;
    private Windowing _windowing;
    private BackupService _backupService;
    private AutoSaveService _autoSaveService;
    private string _testFolder;

    [TestInitialize]
    public void Setup()
    {
        // Get services from IoC container
        _logger = Ioc.Default.GetRequiredService<ILogService>();
        _outlineService = Ioc.Default.GetRequiredService<OutlineService>();
        _appState = Ioc.Default.GetRequiredService<AppState>();
        _preferences = Ioc.Default.GetRequiredService<PreferenceService>();
        _windowing = Ioc.Default.GetRequiredService<Windowing>();
        _backupService = Ioc.Default.GetRequiredService<BackupService>();
        _autoSaveService = Ioc.Default.GetRequiredService<AutoSaveService>();

        _fileCreateService = new FileCreateService(
            _logger,
            _outlineService,
            _appState,
            _preferences,
            _windowing,
            _backupService,
            _autoSaveService);

        // Create a test folder
        _testFolder = Path.Combine(Path.GetTempPath(), $"StoryCADTest_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testFolder);

        // Reset app state
        _appState.CurrentDocument = null;
    }

    [TestCleanup]
    public void Cleanup()
    {
        // Clean up test folder
        if (Directory.Exists(_testFolder))
        {
            try
            {
                Directory.Delete(_testFolder, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [TestMethod]
    public void FileCreateService_CanBeCreated()
    {
        // Assert
        Assert.IsNotNull(_fileCreateService);
    }

    [TestMethod]
    public void FileCreateService_CanBeCreatedFromIoC()
    {
        // Act
        var service = Ioc.Default.GetRequiredService<FileCreateService>();

        // Assert
        Assert.IsNotNull(service);
    }

    [TestMethod]
     public async Task CreateFile_WithValidParameters_CreatesFile()
    {
        // Arrange
        string fileName = "TestStory.stbx";
        int templateIndex = 0;

        // Act
        string result = await _fileCreateService.CreateFile(_testFolder, fileName, templateIndex);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(File.Exists(result));
        Assert.IsNotNull(_appState.CurrentDocument);
        Assert.AreEqual(result, _appState.CurrentDocument.FilePath);
    }

    [TestMethod]
    public async Task CreateFile_WithoutExtension_AddsStbxExtension()
    {
        // Arrange
        string fileName = "TestStory";
        int templateIndex = 0;

        // Act
        string result = await _fileCreateService.CreateFile(_testFolder, fileName, templateIndex);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.EndsWith(".stbx"));
        Assert.IsTrue(File.Exists(result));
    }

    [TestMethod]
    public async Task CreateFile_WithInvalidPath_ReturnsNull()
    {
        // Arrange
        string invalidFolder = @"Z:\InvalidPath\That\Does\Not\Exist";
        string fileName = "TestStory.stbx";
        int templateIndex = 0;

        // Act
        string result = await _fileCreateService.CreateFile(invalidFolder, fileName, templateIndex);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task CreateFile_SendsStatusMessages()
    {
        // Arrange
        string fileName = "TestStory.stbx";
        int templateIndex = 0;
        bool messageReceived = false;

        WeakReferenceMessenger.Default.Register<StatusChangedMessage>(this, (r, m) =>
        {
            messageReceived = true;
        });

        // Act
        await _fileCreateService.CreateFile(_testFolder, fileName, templateIndex);

        // Assert
        Assert.IsTrue(messageReceived);

        // Cleanup
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }

    [TestMethod]
    public async Task CreateFile_SetsAuthorFromPreferences()
    {
        // Arrange
        _preferences.Model.FirstName = "Test";
        _preferences.Model.LastName = "Author";
        string fileName = "TestStory.stbx";
        int templateIndex = 0;

        // Act
        string result = await _fileCreateService.CreateFile(_testFolder, fileName, templateIndex);

        // Assert
        Assert.IsNotNull(_appState.CurrentDocument);
        Assert.IsNotNull(_appState.CurrentDocument.Model);
        // The author would be set in the model's overview
        var overview = _appState.CurrentDocument.Model.StoryElements.StoryElementGuids[
            _appState.CurrentDocument.Model.ExplorerView[0].Uuid] as OverviewModel;
        Assert.IsNotNull(overview);
        Assert.AreEqual("Test Author", overview.Author);
    }

    [TestMethod]
    public async Task CreateFile_WithExistingChangedDocument_SavesFirst()
    {
        // Arrange
        // Create an initial document with changes
        var initialModel = new StoryModel();
        var initialDoc = new StoryDocument(initialModel, Path.Combine(_testFolder, "initial.stbx"));
        initialModel.Changed = true;
        _appState.CurrentDocument = initialDoc;

        string fileName = "NewStory.stbx";
        int templateIndex = 0;

        // Act
        string result = await _fileCreateService.CreateFile(_testFolder, fileName, templateIndex);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreNotEqual(initialDoc.FilePath, result);
        Assert.AreEqual(result, _appState.CurrentDocument.FilePath);
    }

    [TestMethod]
    public async Task CreateFile_WithInvalidCharacters_ReturnsNull()
    {
        // Arrange
        string invalidFileName = "Test<>Story|?.stbx";
        int templateIndex = 0;

        // Act
        string result = await _fileCreateService.CreateFile(_testFolder, invalidFileName, templateIndex);

        // Assert
        Assert.IsNull(result);
    }
}
