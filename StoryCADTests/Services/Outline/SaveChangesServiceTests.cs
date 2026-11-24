using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using StoryCADLib.Models;
using StoryCADLib.Services.Logging;
using StoryCADLib.Services.Messages;
using StoryCADLib.Services.Outline;

#nullable disable

namespace StoryCADTests.Services.Outline;

/// <summary>
///     Test outlines for the "Save changes?" tests (#1636): an outline saved to disk with the
///     author "Original Author", then changed in memory to "Modified Author".
/// </summary>
internal static class SaveChangesTestOutline
{
    public const string SavedAuthor = "Original Author";
    public const string ChangedAuthor = "Modified Author";

    /// <summary>
    ///     Writes a new outline to App.ResultsDir and makes it the current document. With
    ///     <paramref name="changed" /> the author is then changed in memory only.
    /// </summary>
    public static async Task<string> OpenAsync(string name, bool changed = true)
    {
        var outlineService = Ioc.Default.GetRequiredService<OutlineService>();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var filePath = Path.Combine(App.ResultsDir, name + ".stbx");

        var model = await outlineService.CreateModel(name, "StoryBuilder", 0);
        var overview = model.StoryElements.OfType<OverviewModel>().First();
        overview.Author = SavedAuthor;
        await outlineService.WriteModel(model, filePath);
        outlineService.SetChanged(model, false);
        appState.CurrentDocument = new StoryDocument(model, filePath);

        if (changed)
        {
            overview.Author = ChangedAuthor;
            outlineService.SetChanged(model, true);
        }

        return filePath;
    }

    /// <summary>
    ///     Reads the author from the outline file on disk.
    /// </summary>
    public static async Task<string> AuthorOnDiskAsync(string filePath)
    {
        var outlineService = Ioc.Default.GetRequiredService<OutlineService>();
        var model = await outlineService.OpenFile(filePath);
        return model.StoryElements.OfType<OverviewModel>().First().Author;
    }
}

[TestClass]
public class SaveChangesServiceTests
{
    private const string CancelText = "Test command cancelled";

    private AppState _appState;
    private SaveChangesService _service;
    private readonly List<StatusMessage> _statusMessages = new();

    [TestInitialize]
    public void Setup()
    {
        _appState = Ioc.Default.GetRequiredService<AppState>();
        _service = Ioc.Default.GetRequiredService<SaveChangesService>();
        _appState.CurrentDocument = null;
        _statusMessages.Clear();
        WeakReferenceMessenger.Default.Register<StatusChangedMessage>(this,
            (_, m) => _statusMessages.Add(m.Value));
    }

    [TestCleanup]
    public void Cleanup()
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
        _service.HeadlessAnswer = SaveChangesAnswer.No;
        _appState.CurrentDocument = null;
    }

    [TestMethod]
    public async Task ConfirmLeaveAsync_NoDocument_ReturnsTrue()
    {
        _service.HeadlessAnswer = SaveChangesAnswer.Cancel;

        Assert.IsTrue(await _service.ConfirmLeaveAsync(CancelText));
    }

    [TestMethod]
    public async Task ConfirmLeaveAsync_Unchanged_ReturnsTrue()
    {
        await SaveChangesTestOutline.OpenAsync("SaveChanges_Unchanged", changed: false);
        _service.HeadlessAnswer = SaveChangesAnswer.Cancel;

        Assert.IsTrue(await _service.ConfirmLeaveAsync(CancelText));
    }

    [TestMethod]
    public async Task ConfirmLeaveAsync_ChangedNoFilePath_ReturnsTrue()
    {
        var model = new StoryModel { Changed = true };
        _appState.CurrentDocument = new StoryDocument(model, null);
        _service.HeadlessAnswer = SaveChangesAnswer.Cancel;

        Assert.IsTrue(await _service.ConfirmLeaveAsync(CancelText));
    }

    [TestMethod]
    public async Task ConfirmLeaveAsync_AnswerYes_SavesAndClearsChanged()
    {
        var filePath = await SaveChangesTestOutline.OpenAsync("SaveChanges_Yes");
        _service.HeadlessAnswer = SaveChangesAnswer.Yes;

        var result = await _service.ConfirmLeaveAsync(CancelText);

        Assert.IsTrue(result);
        Assert.IsFalse(_appState.CurrentDocument.Model.Changed);
        Assert.AreEqual(SaveChangesTestOutline.ChangedAuthor,
            await SaveChangesTestOutline.AuthorOnDiskAsync(filePath));
    }

    [TestMethod]
    public async Task ConfirmLeaveAsync_AnswerNo_ReturnsTrueAndFileUnchanged()
    {
        var filePath = await SaveChangesTestOutline.OpenAsync("SaveChanges_No");
        _service.HeadlessAnswer = SaveChangesAnswer.No;

        var result = await _service.ConfirmLeaveAsync(CancelText);

        Assert.IsTrue(result);
        Assert.IsTrue(_appState.CurrentDocument.Model.Changed);
        Assert.AreEqual(SaveChangesTestOutline.SavedAuthor,
            await SaveChangesTestOutline.AuthorOnDiskAsync(filePath));
    }

    [TestMethod]
    public async Task ConfirmLeaveAsync_AnswerCancel_ReturnsFalseAndFileUnchanged()
    {
        var filePath = await SaveChangesTestOutline.OpenAsync("SaveChanges_Cancel");
        _service.HeadlessAnswer = SaveChangesAnswer.Cancel;

        var result = await _service.ConfirmLeaveAsync(CancelText);

        Assert.IsFalse(result);
        Assert.IsTrue(_appState.CurrentDocument.Model.Changed);
        Assert.AreEqual(SaveChangesTestOutline.SavedAuthor,
            await SaveChangesTestOutline.AuthorOnDiskAsync(filePath));
    }

    [TestMethod]
    public async Task ConfirmLeaveAsync_AnswerCancel_SendsCancelMessage()
    {
        await SaveChangesTestOutline.OpenAsync("SaveChanges_CancelMessage");
        _service.HeadlessAnswer = SaveChangesAnswer.Cancel;

        await _service.ConfirmLeaveAsync(CancelText);

        Assert.IsTrue(_statusMessages.Any(m => m.Status == CancelText && m.Level == LogLevel.Info));
    }

    [TestMethod]
    public async Task ConfirmLeaveAsync_AnswerYesWriteError_ReturnsFalseAndSendsError()
    {
        await SaveChangesTestOutline.OpenAsync("SaveChanges_WriteError");
        // A folder with the name of the outline file: the write cannot replace it.
        var blockedPath = Path.Combine(App.ResultsDir, "SaveChanges_WriteError_Folder.stbx");
        Directory.CreateDirectory(blockedPath);
        _appState.CurrentDocument.FilePath = blockedPath;
        _service.HeadlessAnswer = SaveChangesAnswer.Yes;

        var result = await _service.ConfirmLeaveAsync(CancelText);

        Assert.IsFalse(result);
        Assert.IsTrue(_appState.CurrentDocument.Model.Changed);
        Assert.IsTrue(_statusMessages.Any(m =>
            m.Status == "Save error. The outline stays open." && m.Level == LogLevel.Error));
        Assert.IsFalse(_statusMessages.Any(m => m.Status == CancelText));
    }
}
