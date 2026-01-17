using System.Linq;
using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCADLib.Models;
using StoryCADLib.Services;
using StoryCADLib.ViewModels;

namespace StoryCADTests.Services;

/// <summary>
///     Tests for IReloadable interface implementation across ViewModels.
///     Ensures ViewModels can reload their data from the Model after external updates.
/// </summary>
[TestClass]
public class IReloadableTests
{
    /// <summary>
    ///     Sets up AppState.CurrentDocument with a valid StoryModel.
    ///     Required because OverviewViewModel accesses CurrentDocument.Model in constructor.
    /// </summary>
    private void SetupCurrentDocument()
    {
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var storyModel = new StoryModel();
        appState.CurrentDocument = new StoryDocument(storyModel);
    }

    #region Interface Implementation Tests

    [TestMethod]
    public void OverviewViewModel_ImplementsIReloadable()
    {
        // Arrange - OverviewViewModel requires CurrentDocument in constructor
        SetupCurrentDocument();
        var viewModel = Ioc.Default.GetRequiredService<OverviewViewModel>();

        // Assert
        Assert.IsInstanceOfType(viewModel, typeof(IReloadable));
    }

    [TestMethod]
    public void CharacterViewModel_ImplementsIReloadable()
    {
        var viewModel = Ioc.Default.GetRequiredService<CharacterViewModel>();
        Assert.IsInstanceOfType(viewModel, typeof(IReloadable));
    }

    [TestMethod]
    public void SceneViewModel_ImplementsIReloadable()
    {
        var viewModel = Ioc.Default.GetRequiredService<SceneViewModel>();
        Assert.IsInstanceOfType(viewModel, typeof(IReloadable));
    }

    [TestMethod]
    public void ProblemViewModel_ImplementsIReloadable()
    {
        var viewModel = Ioc.Default.GetRequiredService<ProblemViewModel>();
        Assert.IsInstanceOfType(viewModel, typeof(IReloadable));
    }

    [TestMethod]
    public void SettingViewModel_ImplementsIReloadable()
    {
        var viewModel = Ioc.Default.GetRequiredService<SettingViewModel>();
        Assert.IsInstanceOfType(viewModel, typeof(IReloadable));
    }

    [TestMethod]
    public void FolderViewModel_ImplementsIReloadable()
    {
        var viewModel = Ioc.Default.GetRequiredService<FolderViewModel>();
        Assert.IsInstanceOfType(viewModel, typeof(IReloadable));
    }

    [TestMethod]
    public void WebViewModel_ImplementsIReloadable()
    {
        var viewModel = Ioc.Default.GetRequiredService<WebViewModel>();
        Assert.IsInstanceOfType(viewModel, typeof(IReloadable));
    }

    #endregion

    #region ReloadFromModel Behavior Tests

    [TestMethod]
    public void OverviewViewModel_ReloadFromModel_UpdatesViewModelFromModel()
    {
        // Arrange - OverviewViewModel requires CurrentDocument in constructor
        SetupCurrentDocument();
        var viewModel = Ioc.Default.GetRequiredService<OverviewViewModel>();
        var model = new OverviewModel { Name = "Test Story", Description = "Original" };
        viewModel.Model = model;
        viewModel.Activate(model); // Load initial values

        // Act - simulate external update to model
        model.Description = "Updated by Collaborator";
        ((IReloadable)viewModel).ReloadFromModel();

        // Assert
        Assert.AreEqual("Updated by Collaborator", viewModel.Description);
    }

    [TestMethod]
    public void CharacterViewModel_ReloadFromModel_UpdatesViewModelFromModel()
    {
        // Arrange - Create a properly initialized CharacterModel
        SetupCurrentDocument();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var storyModel = appState.CurrentDocument!.Model;

        // Create CharacterModel using the pattern from ProblemViewModelTests
        var model = new CharacterModel("Test Character", storyModel, null);
        model.Role = "Protagonist";
        storyModel.StoryElements.Characters.Add(model);

        var viewModel = Ioc.Default.GetRequiredService<CharacterViewModel>();
        viewModel.Model = model;
        viewModel.Activate(model); // Load initial values
        Assert.AreEqual("Protagonist", viewModel.Role);

        // Act - simulate external update to model (like Collaborator would do)
        model.Role = "Antagonist";
        ((IReloadable)viewModel).ReloadFromModel();

        // Assert
        Assert.AreEqual("Antagonist", viewModel.Role);
    }

    [TestMethod]
    public void ReloadFromModel_WithNullModel_DoesNotThrow()
    {
        // Arrange - OverviewViewModel requires CurrentDocument in constructor
        SetupCurrentDocument();
        var viewModel = Ioc.Default.GetRequiredService<OverviewViewModel>();
        viewModel.Model = null;

        // Act & Assert - should not throw
        ((IReloadable)viewModel).ReloadFromModel();
    }

    #endregion

    #region ISaveable and IReloadable Symmetry Tests

    [TestMethod]
    public void OverviewViewModel_SaveThenReload_PreservesData()
    {
        // Arrange - OverviewViewModel requires CurrentDocument in constructor
        SetupCurrentDocument();
        var viewModel = Ioc.Default.GetRequiredService<OverviewViewModel>();
        var model = new OverviewModel { Name = "Test Story" };
        viewModel.Model = model;
        viewModel.Activate(model);

        // Act - modify ViewModel, save, modify model externally, reload
        viewModel.Description = "ViewModel Edit";
        ((ISaveable)viewModel).SaveModel();
        Assert.AreEqual("ViewModel Edit", model.Description);

        model.Description = "External Edit";
        ((IReloadable)viewModel).ReloadFromModel();

        // Assert
        Assert.AreEqual("External Edit", viewModel.Description);
    }

    #endregion
}
