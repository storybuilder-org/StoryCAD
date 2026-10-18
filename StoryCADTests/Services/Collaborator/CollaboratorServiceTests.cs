using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCADLib.Models;
using StoryCADLib.Services;
using StoryCADLib.Services.API;
using StoryCADLib.Services.Backup;
using StoryCADLib.Services.Collaborator;
using StoryCADLib.Services.Collaborator.Contracts;
using StoryCADLib.Services.Logging;
using StoryCADLib.Services.Outline;
using StoryCADLib.ViewModels;

#nullable disable

namespace StoryCADTests.Services.Collaborator;

[TestClass]
public class CollaboratorServiceTests
{
    private static CollaboratorService CreateService(Func<ICollaborator> factory = null) =>
        new(
            Ioc.Default.GetRequiredService<AppState>(),
            Ioc.Default.GetRequiredService<ILogService>(),
            Ioc.Default.GetRequiredService<PreferenceService>(),
            Ioc.Default.GetRequiredService<AutoSaveService>(),
            Ioc.Default.GetRequiredService<BackupService>(),
            Ioc.Default.GetRequiredService<StoryCADApi>(),
            Ioc.Default.GetRequiredService<EditFlushService>(),
            factory);

    /// <summary>
    ///     Test that CollaboratorService can be created
    /// </summary>
    [TestMethod]
    public void CollaboratorService_CanBeCreated()
    {
        // Arrange & Act
        var service = Ioc.Default.GetRequiredService<CollaboratorService>();

        // Assert
        Assert.IsNotNull(service);
    }

    /// <summary>
    ///     With no factory registered (public/free build, CollaboratorLib not compiled in),
    ///     HasCollaborator is false and the Collaborator entry point stays hidden.
    /// </summary>
    [TestMethod]
    public void HasCollaborator_NoFactory_ReturnsFalse()
    {
        var service = CreateService(factory: null);

        Assert.IsFalse(service.HasCollaborator,
            "HasCollaborator should be false when no Collaborator factory is registered");
    }

    /// <summary>
    ///     When a factory is registered at the composition root (CollaboratorLib compiled in),
    ///     HasCollaborator is true. The factory itself is the seam #30's license gate will wrap.
    /// </summary>
    [TestMethod]
    public void HasCollaborator_WithFactory_ReturnsTrue()
    {
        var service = CreateService(factory: () => null);

        Assert.IsTrue(service.HasCollaborator,
            "HasCollaborator should be true when a Collaborator factory is registered");
    }

    /// <summary>
    ///     Issue #1639: edits typed on the open page live in its view model until SaveModel runs.
    ///     OpenCollaborator must flush them to the model before it hands the model to Collaborator.
    /// </summary>
    [TestMethod]
    public async Task OpenCollaborator_FlushesPendingPageEditsToModel()
    {
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var outlineService = Ioc.Default.GetRequiredService<OutlineService>();
        var model = await outlineService.CreateModel("Flush Test", "Test Author", 0);
        appState.CurrentDocument = new StoryDocument(model, "test.stbx");
        var overview = model.StoryElements.OfType<OverviewModel>().First();
        overview.StoryGenre = "Mystery";

        // Real page view model, as the Story Overview page uses it, with an unsaved edit
        var vm = new OverviewViewModel();
        vm.Activate(overview);
        vm.StoryGenre = "";
        appState.CurrentSaveable = vm;
        Assert.AreEqual("Mystery", overview.StoryGenre, "Precondition: model still holds the old value");

        // No factory: OpenCollaborator returns right after the flush, so no dialog or window is created.
        var service = CreateService();
        try
        {
            service.OpenCollaborator();

            Assert.AreEqual("", overview.StoryGenre, "Pending page edit should reach the model before Collaborator opens");
        }
        finally
        {
            appState.CurrentSaveable = null;
        }
    }
}
