using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using StoryCADLib.Models;
using StoryCADLib.Services;
using StoryCADLib.Services.Backup;
using StoryCADLib.Services.Logging;
using StoryCADLib.Services.Messages;
using StoryCADLib.Services.Outline;

#nullable disable

namespace StoryCADTests.Services.Outline;

[TestClass]
public class FileCreateServiceTests
{
    private AppState _appState;
    private AutoSaveService _autoSaveService;
    private BackupService _backupService;
    private FileCreateService _fileCreateService;
    private ILogService _logger;
    private OutlineService _outlineService;
    private PreferenceService _preferences;
    private string _testFolder;
    private Windowing _windowing;

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
        var fileName = "TestStory.stbx";
        var templateIndex = 0;

        // Act
        var result = await _fileCreateService.CreateFile(_testFolder, fileName, templateIndex);

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
        var fileName = "TestStory";
        var templateIndex = 0;

        // Act
        var result = await _fileCreateService.CreateFile(_testFolder, fileName, templateIndex);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.EndsWith(".stbx"));
        Assert.IsTrue(File.Exists(result));
    }

    [TestMethod]
    public async Task CreateFile_WithInvalidPath_ReturnsNull()
    {
        // Arrange - Use a path with invalid characters (cross-platform)
        // On Unix, null character is invalid in paths; on Windows, <, >, :, ", |, ?, * are invalid
        var invalidFolder = Path.Combine(Path.GetTempPath(), "Invalid\0Path");
        var fileName = "TestStory.stbx";
        var templateIndex = 0;

        // Act
        var result = await _fileCreateService.CreateFile(invalidFolder, fileName, templateIndex);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task CreateFile_SendsStatusMessages()
    {
        // Arrange
        var fileName = "TestStory.stbx";
        var templateIndex = 0;
        var messageReceived = false;

        WeakReferenceMessenger.Default.Register<StatusChangedMessage>(this, (r, m) => { messageReceived = true; });

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
        var fileName = "TestStory.stbx";
        var templateIndex = 0;

        // Act
        var result = await _fileCreateService.CreateFile(_testFolder, fileName, templateIndex);

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

        var fileName = "NewStory.stbx";
        var templateIndex = 0;

        // Act
        var result = await _fileCreateService.CreateFile(_testFolder, fileName, templateIndex);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreNotEqual(initialDoc.FilePath, result);
        Assert.AreEqual(result, _appState.CurrentDocument.FilePath);
    }

    [TestMethod]
    public async Task CreateFile_WithInvalidCharacters_ReturnsNull()
    {
        // Arrange - Use an invalid filename with invalid characters
        string invalidFileName;

        if (OperatingSystem.IsWindows())
        {
            // Windows doesn't allow these characters in filenames
            invalidFileName = "Test<>Story|?.stbx";
        }
        else
        {
            // Unix-like systems: Use null character which is invalid in filenames
            invalidFileName = "Test\0Story.stbx";
        }

        var templateIndex = 0;

        // Act
        var result = await _fileCreateService.CreateFile(_testFolder, invalidFileName, templateIndex);

        // Assert
        Assert.IsNull(result);
    }
}
