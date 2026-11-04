using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using StoryCADLib.Models;
using StoryCADLib.Services;
using StoryCADLib.Services.Backend;
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
    private TestUsageTrackingService _testUsageTracking;
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
        _testUsageTracking = new TestUsageTrackingService();

        _fileCreateService = new FileCreateService(
            _logger,
            _outlineService,
            _appState,
            _preferences,
            _windowing,
            _backupService,
            _autoSaveService,
            _testUsageTracking);

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
    public async Task CreateFile_WithValidParameters_WritesOutlineToFile()
    {
        // Arrange - a new outline must not sit on disk as a zero-byte file until first save (#1682)
        var fileName = "WrittenStory.stbx";

        // Act
        var result = await _fileCreateService.CreateFile(_testFolder, fileName, OutlineTemplate.ProblemsAndCharacters);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(new FileInfo(result).Length > 0, "New outline file is empty.");
        var reopened = await _outlineService.OpenFile(result);
        Assert.AreEqual(_appState.CurrentDocument.Model.StoryElements.Count, reopened.StoryElements.Count);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow(".stbx")]
    [DataRow(".STBX")]
    public async Task CreateFile_WithBlankName_ReturnsNullAndCreatesNoFile(string fileName)
    {
        // Act - a blank name produced a file named ".stbx" in production (#1682)
        var result = await _fileCreateService.CreateFile(_testFolder, fileName, 0);

        // Assert
        Assert.IsNull(result);
        Assert.AreEqual(0, Directory.GetFiles(_testFolder).Length);
    }

    [TestMethod]
    [DataRow(".45 Magnum")]
    [DataRow(".hack")]
    public async Task CreateFile_WithNameStartingWithDot_CreatesFile(string fileName)
    {
        // Act - only a trailing ".stbx" is an extension; ".hack" is a title (#1682)
        var result = await _fileCreateService.CreateFile(_testFolder, fileName, 0);

        // Assert
        Assert.AreEqual(Path.Combine(_testFolder, fileName + ".stbx"), result);
        Assert.IsTrue(File.Exists(result));
    }

    [TestMethod]
    public async Task CreateFile_WithInvalidCharacters_SendsInvalidPathStatusToUntokenedListener()
    {
        // Arrange - ShellViewModel registers without a token; a tokened send never reaches it (#1682)
        var invalidFileName = OperatingSystem.IsWindows() ? "Test<>Story|?.stbx" : "Test\0Story.stbx";
        var statuses = new List<StatusMessage>();
        WeakReferenceMessenger.Default.Register<StatusChangedMessage>(this, (_, m) => statuses.Add(m.Value));

        try
        {
            // Act
            await _fileCreateService.CreateFile(_testFolder, invalidFileName, 0);

            // Assert
            Assert.IsTrue(statuses.Any(m => m.Status == "New project command executing"));
            // Warn, not Error: ShellViewModel logs status messages at their level, and Error reaches elmah.io
            Assert.IsTrue(statuses.Any(m => m.Status == "Invalid file path" && m.Level == LogLevel.Warn));
        }
        finally
        {
            WeakReferenceMessenger.Default.UnregisterAll(this);
        }
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
    public async Task CreateFile_WithForwardSlashFolder_CreatesFile()
    {
        // Arrange - the dialog's folder box is free text; .NET accepts '/' separators (#1615)
        var folder = _testFolder.Replace('\\', '/');

        // Act
        var result = await _fileCreateService.CreateFile(folder, "TestStory.stbx", 0);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(File.Exists(result));
    }

    [TestMethod]
    public async Task CreateFile_WhenFileExists_GeneratesUniqueName()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_testFolder, "TestStory.stbx"), "existing");

        // Act
        var result = await _fileCreateService.CreateFile(_testFolder, "TestStory.stbx", 0);

        // Assert
        Assert.AreEqual(Path.Combine(_testFolder, "TestStory (2).stbx"), result);
        Assert.AreEqual("existing", File.ReadAllText(Path.Combine(_testFolder, "TestStory.stbx")));
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
    public async Task CreateFile_WithValidParameters_TracksOutlineOpen()
    {
        // Arrange
        var fileName = "TrackingTest.stbx";
        var templateIndex = 0;

        // Act
        await _fileCreateService.CreateFile(_testFolder, fileName, templateIndex);

        // Assert
        Assert.AreEqual(1, _testUsageTracking.OutlineOpenedCalls.Count,
            "CreateFile should fire OutlineOpened exactly once");
        var overview = _appState.CurrentDocument.Model.StoryElements
            .OfType<OverviewModel>().First();
        Assert.AreEqual(overview.Uuid, _testUsageTracking.OutlineOpenedCalls[0].guid);
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

/// <summary>
/// Test double for IUsageTrackingService that records calls.
/// Follows the same pattern as TestMySqlIo and TestLogService in BackendServiceTests.
/// </summary>
public class TestUsageTrackingService : IUsageTrackingService
{
    public int StartSessionCalls { get; private set; }
    public int EndSessionCalls { get; private set; }
    public List<(Guid guid, string genre, string storyForm, int elementCount)> OutlineOpenedCalls { get; } = new();
    public List<(Guid guid, int elementCount)> OutlineClosedCalls { get; } = new();
    public int ElementAddedCalls { get; private set; }
    public int ElementRemovedCalls { get; private set; }
    public int ElementRestoredCalls { get; private set; }
    public List<string> FeatureUsedCalls { get; } = new();

    public void StartSession() => StartSessionCalls++;

    public Task EndSession()
    {
        EndSessionCalls++;
        return Task.CompletedTask;
    }

    public void OutlineOpened(Guid outlineGuid, string genre, string storyForm, int elementCount)
        => OutlineOpenedCalls.Add((outlineGuid, genre, storyForm, elementCount));

    public void OutlineClosed(Guid outlineGuid, int elementCount)
        => OutlineClosedCalls.Add((outlineGuid, elementCount));

    public void ElementAdded() => ElementAddedCalls++;
    public void ElementRemoved() => ElementRemovedCalls++;
    public void ElementRestored() => ElementRestoredCalls++;
    public void FeatureUsed(string featureName) => FeatureUsedCalls.Add(featureName);
}
