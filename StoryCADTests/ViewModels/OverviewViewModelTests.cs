using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCADLib.Models;
using StoryCADLib.Services;
using StoryCADLib.ViewModels;

#nullable disable

namespace StoryCADTests.ViewModels;

[TestClass]
public class OverviewViewModelTests
{
    private OverviewModel _overviewModel;
    private StoryModel _storyModel;
    private OverviewViewModel _viewModel;
    private AppState _appState;

    [TestInitialize]
    public void TestInitialize()
    {
        // Create a test story model and overview model
        _storyModel = new StoryModel();
        _overviewModel = new OverviewModel("Test Story", _storyModel, null);

        // Get AppState and set up the current document
        _appState = Ioc.Default.GetRequiredService<AppState>();
        _appState.CurrentDocument = new StoryDocument(_storyModel);

        // Initialize the view model
        _viewModel = Ioc.Default.GetRequiredService<OverviewViewModel>();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _viewModel = null;
        _overviewModel = null;
        _storyModel = null;
    }

    #region ISaveable Implementation Tests

    [TestMethod]
    public void OverviewViewModel_ImplementsISaveable()
    {
        // Arrange - ViewModel already created in TestInitialize

        // Act
        var saveable = _viewModel as ISaveable;

        // Assert
        Assert.IsNotNull(saveable);
    }

    [TestMethod]
    public void SaveModel_ExistsAsPublicMethod()
    {
        // Arrange - ViewModel already created in TestInitialize

        // Act
        var method = _viewModel.GetType().GetMethod("SaveModel");

        // Assert
        Assert.IsNotNull(method);
        Assert.IsTrue(method.IsPublic);
    }

    #endregion

    #region SaveModel Null-Safety Tests (Bug Fix Verification)

    [TestMethod]
    public void SaveModel_WhenModelIsNull_DoesNotThrow()
    {
        // Arrange - Don't activate the ViewModel (Model remains null)
        // This simulates the state during file close when Model may be cleared

        // Act & Assert - Should not throw NullReferenceException
        _viewModel.SaveModel();
    }

    [TestMethod]
    public void SaveModel_AfterActivateWithValidModel_DoesNotThrow()
    {
        // Arrange - Activate with valid model
        _viewModel.Activate(_overviewModel);

        // Act & Assert - Should not throw
        _viewModel.SaveModel();
    }

    [TestMethod]
    public void SaveModel_WithStoryProblemSet_DoesNotThrowWhenStoryModelIsValid()
    {
        // Arrange - Create a problem and link it to the overview
        var problem = new ProblemModel("Test Problem", _storyModel, null);
        _storyModel.StoryElements.Problems.Add(problem);

        _viewModel.Activate(_overviewModel);
        _viewModel.StoryProblem = problem.Uuid;
        _viewModel.Premise = "Test premise that should sync";

        // Act & Assert - Should not throw during premise sync
        _viewModel.SaveModel();

        // Verify premise was synced
        Assert.AreEqual("Test premise that should sync", problem.Premise);
    }

    [TestMethod]
    public void SaveModel_WithStoryProblemSetToEmptyGuid_DoesNotThrow()
    {
        // Arrange - Activate but don't set a story problem
        _viewModel.Activate(_overviewModel);
        _viewModel.StoryProblem = Guid.Empty;
        _viewModel.Premise = "Test premise";

        // Act & Assert - Should not throw (empty Guid should skip sync)
        _viewModel.SaveModel();
    }

    [TestMethod]
    public void Deactivate_AfterActivate_DoesNotThrow()
    {
        // Arrange - Simulate normal navigation lifecycle
        _viewModel.Activate(_overviewModel);
        _viewModel.Name = "Modified Story Name";

        // Act & Assert - Deactivate triggers SaveModel, should not throw
        _viewModel.Deactivate(null);

        // Verify changes were saved
        Assert.AreEqual("Modified Story Name", _overviewModel.Name);
    }

    [TestMethod]
    public void Deactivate_WithStoryProblemDuringClose_DoesNotThrow()
    {
        // Arrange - This simulates the exact bug scenario:
        // 1. User has Overview open with a StoryProblem linked
        // 2. User closes file and clicks "No" on save dialog
        // 3. Navigation triggers Deactivate -> SaveModel
        // 4. SaveModel tries to sync premise via GetByGuid

        var problem = new ProblemModel("Story Problem", _storyModel, null);
        _storyModel.StoryElements.Problems.Add(problem);

        _viewModel.Activate(_overviewModel);
        _viewModel.StoryProblem = problem.Uuid;
        _viewModel.Premise = "Updated premise";

        // Act & Assert - Should not throw NullReferenceException
        // The fix passes _storyModel to GetByGuid instead of relying on AppState
        _viewModel.Deactivate(null);
    }

    #endregion

    #region Core Lifecycle Tests

    [TestMethod]
    public void Activate_WithValidOverviewModel_LoadsProperties()
    {
        // Arrange - Set up overview model with known values
        _overviewModel.Name = "Test Story";
        _overviewModel.Author = "Test Author";
        _overviewModel.DateCreated = "2025-01-01";
        _overviewModel.Concept = "Test concept";

        // Act
        _viewModel.Activate(_overviewModel);

        // Assert
        Assert.AreEqual("Test Story", _viewModel.Name);
        Assert.AreEqual("Test Author", _viewModel.Author);
        Assert.AreEqual("2025-01-01", _viewModel.DateCreated);
        Assert.AreEqual("Test concept", _viewModel.Concept);
    }

    [TestMethod]
    public void SaveModel_WithModifiedProperties_UpdatesOverviewModel()
    {
        // Arrange
        _viewModel.Activate(_overviewModel);

        // Act - Modify properties
        _viewModel.Name = "Modified Story Name";
        _viewModel.Author = "Modified Author";
        _viewModel.Concept = "Modified concept";
        _viewModel.Premise = "Modified premise";
        _viewModel.SaveModel();

        // Assert
        Assert.AreEqual("Modified Story Name", _overviewModel.Name);
        Assert.AreEqual("Modified Author", _overviewModel.Author);
        Assert.AreEqual("Modified concept", _overviewModel.Concept);
        Assert.AreEqual("Modified premise", _overviewModel.Premise);
    }

    #endregion
}
