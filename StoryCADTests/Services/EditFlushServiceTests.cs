using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using StoryCADLib.Models;
using StoryCADLib.Services;
using StoryCADLib.Services.Logging;
using StoryCADLib.Services.Outline;
using StoryCADLib.ViewModels;

#nullable disable

namespace StoryCADTests.Services;

[TestClass]
public class EditFlushServiceTests
{
    private AppState _appState;
    private EditFlushService _service;

    [TestInitialize]
    public void Initialize()
    {
        _appState = new AppState();
        var logger = Ioc.Default.GetRequiredService<ILogService>();
        _service = new EditFlushService(_appState, logger);
    }

    [TestMethod]
    public void FlushCurrentEdits_WithNoCurrentSaveable_ReturnsSuccessfully()
    {
        // Arrange
        _appState.CurrentSaveable = null;

        // Act & Assert - should not throw
        _service.FlushCurrentEdits();
    }

    [TestMethod]
    public void FlushCurrentEdits_WithCurrentSaveable_CallsSaveModel()
    {
        // Arrange
        var saveable = new TestSaveable();
        _appState.CurrentSaveable = saveable;

        // Act
        _service.FlushCurrentEdits();

        // Assert
        Assert.IsTrue(saveable.SaveModelCalled);
    }

    [TestMethod]
    public void FlushCurrentEdits_WhenSaveModelThrows_LogsErrorAndContinues()
    {
        // Arrange
        var saveable = new ThrowingSaveable();
        _appState.CurrentSaveable = saveable;

        // Act & Assert - should not throw even though SaveModel throws
        _service.FlushCurrentEdits();
    }

    /// <summary>
    ///     Issue #1645: the Collaborator window is not modal. A click in it makes the main window
    ///     inactive, and the open page's edit must reach the model then, so the next workflow run
    ///     reads it.
    /// </summary>
    [TestMethod]
    public async Task OnMainWindowActivationChanged_Deactivated_FlushesPendingPageEditToModel()
    {
        var (overview, _) = await OverviewWithPendingEdit();

        _service.OnMainWindowActivationChanged(WindowActivationState.Deactivated);

        Assert.AreEqual("Western", overview.StoryGenre, "The page edit should reach the model when the main window goes inactive");
    }

    /// <summary>Issue #1645: activation is not a reason to copy page edits.</summary>
    [TestMethod]
    public async Task OnMainWindowActivationChanged_CodeActivated_LeavesModelUnchanged()
    {
        var (overview, _) = await OverviewWithPendingEdit();

        _service.OnMainWindowActivationChanged(WindowActivationState.CodeActivated);

        Assert.AreEqual("Mystery", overview.StoryGenre, "Activation should not copy page edits");
    }

    /// <summary>A real outline and the Story Overview page's view model, with an edit not yet in the model.</summary>
    private async Task<(OverviewModel overview, OverviewViewModel vm)> OverviewWithPendingEdit()
    {
        var outlineService = Ioc.Default.GetRequiredService<OutlineService>();
        var model = await outlineService.CreateModel("Flush Test", "Test Author", 0);
        var overview = model.StoryElements.OfType<OverviewModel>().First();
        overview.StoryGenre = "Mystery";
        _appState.CurrentDocument = new StoryDocument(model, "test.stbx");

        var vm = new OverviewViewModel(Ioc.Default.GetRequiredService<ILogService>(), _appState);
        vm.Activate(overview);
        vm.StoryGenre = "Western";
        _appState.CurrentSaveable = vm;
        Assert.AreEqual("Mystery", overview.StoryGenre, "Precondition: the model still holds the old value");
        return (overview, vm);
    }

    private class TestSaveable : ISaveable
    {
        public bool SaveModelCalled { get; private set; }

        public void SaveModel()
        {
            SaveModelCalled = true;
        }
    }

    private class ThrowingSaveable : ISaveable
    {
        public void SaveModel()
        {
            throw new InvalidOperationException("Test exception");
        }
    }
}
