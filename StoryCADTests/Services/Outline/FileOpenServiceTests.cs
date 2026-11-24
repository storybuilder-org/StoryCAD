using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using StoryCADLib.DAL;
using StoryCADLib.Models;
using StoryCADLib.Services;
using StoryCADLib.Services.Backup;
using StoryCADLib.Services.Logging;
using StoryCADLib.Services.Messages;
using StoryCADLib.Services.Outline;
using StoryCADLib.ViewModels;

#nullable disable

namespace StoryCADTests.Services.Outline;

[TestClass]
public class FileOpenServiceTests
{
    private AppState _appState;
    private AutoSaveService _autoSaveService;
    private BackupService _backupService;
    private EditFlushService _editFlushService;
    private FileOpenService _fileOpenService;
    private ILogService _logger;
    private OutlineService _outlineService;
    private PreferenceService _preferences;
    private StoryIO _storyIO;
    private Windowing _windowing;

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
        var messageReceived = false;
        WeakReferenceMessenger.Default.Register<StatusChangedMessage>(this, (r, m) => { messageReceived = true; });

        // Act
        await _fileOpenService.OpenFile();

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
        var nonExistentPath = Path.Combine(Path.GetTempPath(), "nonexistent.stbx");

        // In headless mode, ShowFilePicker returns null, which causes the method to return early
        // This test verifies that the service handles non-existent files gracefully by showing
        // a file picker (which returns null in tests) rather than crashing

        // Act
        await _fileOpenService.OpenFile(nonExistentPath);

        // Assert - test passes if no exception thrown
        // In production, this would show a file picker to the user

        // Cleanup
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }

    [TestMethod]
    public async Task UpdateRecents_AddsPathToRecentFiles()
    {
        // Arrange
        var testPath = Path.Combine(Path.GetTempPath(), "test", "file.stbx");
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
        var testPath1 = Path.Combine(Path.GetTempPath(), "test", "file1.stbx");
        var testPath2 = Path.Combine(Path.GetTempPath(), "test", "file2.stbx");
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
        for (var i = 0; i < 15; i++)
        {
            _preferences.Model.RecentFiles.Add(Path.Combine(Path.GetTempPath(), "test", $"file{i}.stbx"));
        }

        // Act
        var newFilePath = Path.Combine(Path.GetTempPath(), "test", "newfile.stbx");
        await _fileOpenService.UpdateRecents(newFilePath);

        // Assert
        Assert.AreEqual(10, _preferences.Model.RecentFiles.Count);
        Assert.AreEqual(newFilePath, _preferences.Model.RecentFiles[0]);
    }

    [TestMethod]
    public async Task UpdateRecents_WithNullPath_DoesNothing()
    {
        // Arrange
        _preferences.Model.RecentFiles.Clear();
        _preferences.Model.RecentFiles.Add(Path.Combine(Path.GetTempPath(), "test", "file.stbx"));
        var initialCount = _preferences.Model.RecentFiles.Count;

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
        _preferences.Model.RecentFiles.Add(Path.Combine(Path.GetTempPath(), "test", "file.stbx"));
        var initialCount = _preferences.Model.RecentFiles.Count;

        // Act
        await _fileOpenService.UpdateRecents("");

        // Assert
        Assert.AreEqual(initialCount, _preferences.Model.RecentFiles.Count);
    }

    [TestMethod]
    public async Task OpenFile_ReopenAfterResetModel_DoesNotCrash()
    {
        // Regression test for issue #1153
        // When ResetModel() is called (e.g., during CloseFile()), it creates a
        // StoryDocument with FilePath = null. If the model has Changed = true,
        // OpenFile() would attempt to save using the null path, causing a crash.
        //
        // This test verifies that OpenFile() handles this scenario gracefully
        // by checking for null FilePath before attempting to save.

        // Arrange
        // Simulate the state after ResetModel() is called:
        // - CurrentDocument exists (not null)
        // - FilePath is null (no file path set)
        // - Model.Changed could be true (simulate unsaved changes)
        var emptyModel = new StoryModel();
        emptyModel.Changed = true; // Simulate unsaved changes
        _appState.CurrentDocument = new StoryDocument(emptyModel, null);

        // Act & Assert
        // This should NOT crash with ArgumentNullException
        // In headless mode, file picker returns null and method returns early
        await _fileOpenService.OpenFile(Path.Combine(Path.GetTempPath(), "test", "somefile.stbx"));

        // Test passes if no exception thrown
    }

    #region Save changes? (issue #1636)

    [TestCleanup]
    public void Cleanup()
    {
        Ioc.Default.GetRequiredService<SaveChangesService>().HeadlessAnswer = SaveChangesAnswer.No;
        _autoSaveService.StopAutoSave();
        _appState.CurrentDocument = null;
    }

    /// <summary>
    ///     Writes the outline to open, then makes a changed outline the current document.
    ///     Returns the two file paths.
    /// </summary>
    private static async Task<(string firstPath, string targetPath)> ArrangeChangedOutlineAndTarget(string name)
    {
        var targetPath = await SaveChangesTestOutline.OpenAsync(name + "_Target", changed: false);
        var firstPath = await SaveChangesTestOutline.OpenAsync(name + "_First");
        return (firstPath, targetPath);
    }

    private static void SetAnswer(SaveChangesAnswer answer) =>
        Ioc.Default.GetRequiredService<SaveChangesService>().HeadlessAnswer = answer;

    [TestMethod]
    public async Task OpenFile_AnswerYes_SavesThenOpens()
    {
        var (firstPath, targetPath) = await ArrangeChangedOutlineAndTarget("OpenFile_Yes");
        SetAnswer(SaveChangesAnswer.Yes);

        await _fileOpenService.OpenFile(targetPath);

        Assert.AreEqual(targetPath, _appState.CurrentDocument.FilePath);
        Assert.AreEqual(SaveChangesTestOutline.ChangedAuthor,
            await SaveChangesTestOutline.AuthorOnDiskAsync(firstPath));
    }

    [TestMethod]
    public async Task OpenFile_AnswerNo_OpensAndFirstFileUnchanged()
    {
        var (firstPath, targetPath) = await ArrangeChangedOutlineAndTarget("OpenFile_No");
        SetAnswer(SaveChangesAnswer.No);

        await _fileOpenService.OpenFile(targetPath);

        Assert.AreEqual(targetPath, _appState.CurrentDocument.FilePath);
        Assert.AreEqual(SaveChangesTestOutline.SavedAuthor,
            await SaveChangesTestOutline.AuthorOnDiskAsync(firstPath));
    }

    [TestMethod]
    public async Task OpenFile_AnswerCancel_KeepsFirstOutline()
    {
        var (firstPath, targetPath) = await ArrangeChangedOutlineAndTarget("OpenFile_Cancel");
        var firstDocument = _appState.CurrentDocument;
        SetAnswer(SaveChangesAnswer.Cancel);

        await _fileOpenService.OpenFile(targetPath);

        Assert.AreSame(firstDocument, _appState.CurrentDocument);
        Assert.IsTrue(_appState.CurrentDocument.Model.Changed);
        Assert.AreEqual(SaveChangesTestOutline.SavedAuthor,
            await SaveChangesTestOutline.AuthorOnDiskAsync(firstPath));
    }

    [TestMethod]
    public async Task OpenFile_AnswerCancel_KeepsRecentFiles()
    {
        var (_, targetPath) = await ArrangeChangedOutlineAndTarget("OpenFile_CancelRecents");
        _preferences.Model.RecentFiles.Clear();
        _preferences.Model.RecentFiles.Add(Path.Combine(Path.GetTempPath(), "test", "file.stbx"));
        var before = _preferences.Model.RecentFiles.ToList();
        SetAnswer(SaveChangesAnswer.Cancel);

        await _fileOpenService.OpenFile(targetPath);

        CollectionAssert.AreEqual(before, _preferences.Model.RecentFiles);
    }

    [TestMethod]
    public async Task OpenFile_PickerGivesNoFile_WritesNothing()
    {
        var firstPath = await SaveChangesTestOutline.OpenAsync("OpenFile_NoFilePicked");
        var firstDocument = _appState.CurrentDocument;
        SetAnswer(SaveChangesAnswer.Yes);

        // No path: the file picker comes first, and in a test it gives no file.
        await _fileOpenService.OpenFile();

        Assert.AreSame(firstDocument, _appState.CurrentDocument);
        Assert.IsTrue(_appState.CurrentDocument.Model.Changed);
        Assert.AreEqual(SaveChangesTestOutline.SavedAuthor,
            await SaveChangesTestOutline.AuthorOnDiskAsync(firstPath));
    }

    [TestMethod]
    public async Task OpenFile_Unchanged_OpensWithoutSave()
    {
        var targetPath = await SaveChangesTestOutline.OpenAsync("OpenFile_Unchanged_Target", changed: false);
        var firstPath = await SaveChangesTestOutline.OpenAsync("OpenFile_Unchanged_First", changed: false);
        var writtenAt = File.GetLastWriteTimeUtc(firstPath);
        SetAnswer(SaveChangesAnswer.Cancel);

        await _fileOpenService.OpenFile(targetPath);

        Assert.AreEqual(targetPath, _appState.CurrentDocument.FilePath);
        Assert.AreEqual(writtenAt, File.GetLastWriteTimeUtc(firstPath));
    }

    #endregion
}
