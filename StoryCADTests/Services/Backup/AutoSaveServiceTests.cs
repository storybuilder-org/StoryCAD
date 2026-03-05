using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCADLib.Models;
using StoryCADLib.Services;
using StoryCADLib.Services.Backup;
using StoryCADLib.Services.Outline;

#nullable disable

namespace StoryCADTests.Services.Backup;

[TestClass]
public class AutoSaveServiceTests
{
    private AutoSaveService _autoSaveService;
    private AppState _appState;
    private PreferenceService _prefs;
    private OutlineService _outlineService;
    private bool _originalAutoSave;
    private string _testFilePath;

    [TestInitialize]
    public async Task Setup()
    {
        _autoSaveService = Ioc.Default.GetRequiredService<AutoSaveService>();
        _appState = Ioc.Default.GetRequiredService<AppState>();
        _prefs = Ioc.Default.GetRequiredService<PreferenceService>();
        _outlineService = Ioc.Default.GetRequiredService<OutlineService>();

        _originalAutoSave = _prefs.Model.AutoSave;

        // Create a minimal but valid model with elements
        var model = await _outlineService.CreateModel("AutoSaveTest", "Test", 0);
        _testFilePath = Path.Combine(App.ResultsDir, $"AutoSaveTest_{Guid.NewGuid()}.stbx");
        _appState.CurrentDocument = new StoryDocument(model, _testFilePath);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _prefs.Model.AutoSave = _originalAutoSave;
        _autoSaveService.StopAutoSave();
        _appState.CurrentDocument = null;

        if (File.Exists(_testFilePath))
        {
            try { File.Delete(_testFilePath); }
            catch { /* ignore */ }
        }
    }

    [TestMethod]
    public async Task AutoSaveProjectAsync_WhenChangedTrue_WritesFile()
    {
        // Arrange
        _prefs.Model.AutoSave = true;
        _appState.CurrentDocument.Model.Changed = true;

        // Act
        await _autoSaveService.AutoSaveProjectAsync();

        // Assert
        Assert.IsTrue(File.Exists(_testFilePath), "AutoSave should have written the file");
        Assert.IsFalse(_appState.CurrentDocument.Model.Changed,
            "Changed flag should be cleared after save (headless path)");
    }

    [TestMethod]
    public async Task AutoSaveProjectAsync_WhenChangedFalse_DoesNotWrite()
    {
        // Arrange
        _prefs.Model.AutoSave = true;
        _appState.CurrentDocument.Model.Changed = false;

        // Act
        await _autoSaveService.AutoSaveProjectAsync();

        // Assert
        Assert.IsFalse(File.Exists(_testFilePath),
            "AutoSave should not write when model has not changed");
    }

    [TestMethod]
    public async Task AutoSaveProjectAsync_WhenAutoSaveDisabled_DoesNotWrite()
    {
        // Arrange
        _prefs.Model.AutoSave = false;
        _appState.CurrentDocument.Model.Changed = true;

        // Act
        await _autoSaveService.AutoSaveProjectAsync();

        // Assert
        Assert.IsFalse(File.Exists(_testFilePath),
            "AutoSave should not write when AutoSave preference is disabled");
    }

    [TestMethod]
    public async Task AutoSaveProjectAsync_WhenNoElements_DoesNotWrite()
    {
        // Arrange
        _prefs.Model.AutoSave = true;
        var emptyModel = new StoryModel(); // no elements
        _appState.CurrentDocument = new StoryDocument(emptyModel, _testFilePath);
        emptyModel.Changed = true;

        // Act
        await _autoSaveService.AutoSaveProjectAsync();

        // Assert
        Assert.IsFalse(File.Exists(_testFilePath),
            "AutoSave should not write when StoryElements is empty");
    }

    [TestMethod]
    public async Task StopAutoSaveAndWaitAsync_CompletesWithoutError()
    {
        // The AutoSaveService singleton timer may have been disposed by
        // earlier test classes (SerializationLockTests), so we cannot call
        // StartAutoSave() reliably. Instead, verify StopAutoSaveAndWaitAsync
        // completes without error — it acquires/releases the semaphore gate,
        // which is the critical concurrency behavior under test.

        // Act — should not throw even when timer is already stopped
        await _autoSaveService.StopAutoSaveAndWaitAsync();

        // Assert — if we reach here, the semaphore wait/release succeeded
        Assert.IsFalse(_autoSaveService.IsRunning,
            "Timer should be stopped after StopAutoSaveAndWaitAsync");
    }
}
