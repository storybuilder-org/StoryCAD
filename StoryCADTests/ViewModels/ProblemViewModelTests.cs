using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCADLib.Models;
using StoryCADLib.Services;
using StoryCADLib.ViewModels;
using StoryCADLib.ViewModels.Tools;

#nullable disable

namespace StoryCADTests.ViewModels;

[TestClass]
public class ProblemViewModelTests
{
    private ProblemModel _problemModel;
    private StoryModel _storyModel;
    private ProblemViewModel _viewModel;

    [TestInitialize]
    public void TestInitialize()
    {
        // Reset AppState to clean state (critical for singleton ViewModel isolation)
        var appState = Ioc.Default.GetRequiredService<AppState>();
        appState.CurrentDocument = null;

        // Create a test story model and problem model
        _storyModel = CreateTestStoryModel();
        _problemModel = CreateTestProblemModel(_storyModel);

        // Initialize the view model with proper AppState setup
        _viewModel = SetupViewModelWithStory();

        // Re-subscribe PropertyChanged handler (may have been unsubscribed by previous test)
        // This is safe: unsubscribe first prevents duplicates, then re-subscribe ensures handler works
        _viewModel.PropertyChanged -= _viewModel.OnPropertyChanged;
        _viewModel.PropertyChanged += _viewModel.OnPropertyChanged;

        _viewModel.StructureBeats = new ObservableCollection<StructureBeatViewModel>();
        _viewModel.StructureModelTitle = "Custom Beat Sheet"; // Enable editing
    }

    [TestMethod]
    public void CreateBeat_ShouldAddNewBeatToCollection()
    {
        // Arrange
        var initialCount = _viewModel.StructureBeats.Count;

        // Act
        _viewModel.CreateBeat(null, null);

        // Assert
        Assert.AreEqual(initialCount + 1, _viewModel.StructureBeats.Count);
        var newBeat = _viewModel.StructureBeats.Last();
        Assert.AreEqual("New Beat", newBeat.Title);
        Assert.AreEqual("Describe your beat here", newBeat.Description);
    }

    [TestMethod]
    public void CreateBeat_MultipleCalls_ShouldAddMultipleBeats()
    {
        // Arrange
        var initialCount = _viewModel.StructureBeats.Count;

        // Act
        _viewModel.CreateBeat(null, null);
        _viewModel.CreateBeat(null, null);
        _viewModel.CreateBeat(null, null);

        // Assert
        Assert.AreEqual(initialCount + 3, _viewModel.StructureBeats.Count);
    }

    [TestMethod]
    public void DeleteBeat_WithNullSelectedBeat_ShouldNotRemoveAnyBeat()
    {
        // Arrange
        _viewModel.StructureBeats.Add(CreateTestBeat("Test Beat"));
        _viewModel.SelectedBeat = null;
        var initialCount = _viewModel.StructureBeats.Count;

        // Act
        _viewModel.DeleteBeat(null, null);

        // Assert
        Assert.AreEqual(initialCount, _viewModel.StructureBeats.Count);
    }

    [TestMethod]
    public void MoveUp_WithSelectedBeatAtIndexZero_ShouldNotMove()
    {
        // Arrange
        var beat1 = CreateTestBeat("Beat 1");
        var beat2 = CreateTestBeat("Beat 2");
        _viewModel.StructureBeats.Add(beat1);
        _viewModel.StructureBeats.Add(beat2);
        _viewModel.SelectedBeat = beat1;
        _viewModel.SelectedBeatIndex = 0;

        // Act
        _viewModel.MoveUp(null, null);

        // Assert
        Assert.AreEqual(0, _viewModel.StructureBeats.IndexOf(beat1));
        Assert.AreEqual(1, _viewModel.StructureBeats.IndexOf(beat2));
    }

    [TestMethod]
    public void MoveUp_WithSelectedBeatAtIndexOne_ShouldMoveToIndexZero()
    {
        // Arrange
        var beat1 = CreateTestBeat("Beat 1");
        var beat2 = CreateTestBeat("Beat 2");
        _viewModel.StructureBeats.Add(beat1);
        _viewModel.StructureBeats.Add(beat2);
        _viewModel.SelectedBeat = beat2;
        _viewModel.SelectedBeatIndex = 1;

        // Act
        _viewModel.MoveUp(null, null);

        // Assert
        Assert.AreEqual(1, _viewModel.StructureBeats.IndexOf(beat1));
        Assert.AreEqual(0, _viewModel.StructureBeats.IndexOf(beat2));
    }

    [TestMethod]
    public void MoveUp_WithNullSelectedBeat_ShouldNotMoveAnyBeat()
    {
        // Arrange
        var beat1 = CreateTestBeat("Beat 1");
        var beat2 = CreateTestBeat("Beat 2");
        _viewModel.StructureBeats.Add(beat1);
        _viewModel.StructureBeats.Add(beat2);
        _viewModel.SelectedBeat = null;

        // Act
        _viewModel.MoveUp(null, null);

        // Assert
        Assert.AreEqual(0, _viewModel.StructureBeats.IndexOf(beat1));
        Assert.AreEqual(1, _viewModel.StructureBeats.IndexOf(beat2));
    }

    [TestMethod]
    public void MoveDown_WithSelectedBeatAtLastIndex_ShouldNotMove()
    {
        // Arrange
        var beat1 = CreateTestBeat("Beat 1");
        var beat2 = CreateTestBeat("Beat 2");
        _viewModel.StructureBeats.Add(beat1);
        _viewModel.StructureBeats.Add(beat2);
        _viewModel.SelectedBeat = beat2;
        _viewModel.SelectedBeatIndex = 1;

        // Act
        _viewModel.MoveDown(null, null);

        // Assert
        Assert.AreEqual(0, _viewModel.StructureBeats.IndexOf(beat1));
        Assert.AreEqual(1, _viewModel.StructureBeats.IndexOf(beat2));
    }

    [TestMethod]
    public void MoveDown_WithSelectedBeatAtIndexZero_ShouldMoveToIndexOne()
    {
        // Arrange
        var beat1 = CreateTestBeat("Beat 1");
        var beat2 = CreateTestBeat("Beat 2");
        _viewModel.StructureBeats.Add(beat1);
        _viewModel.StructureBeats.Add(beat2);
        _viewModel.SelectedBeat = beat1;
        _viewModel.SelectedBeatIndex = 0;

        // Act
        _viewModel.MoveDown(null, null);

        // Assert
        Assert.AreEqual(1, _viewModel.StructureBeats.IndexOf(beat1));
        Assert.AreEqual(0, _viewModel.StructureBeats.IndexOf(beat2));
    }

    [TestMethod]
    public void MoveDown_WithNullSelectedBeat_ShouldNotMoveAnyBeat()
    {
        // Arrange
        var beat1 = CreateTestBeat("Beat 1");
        var beat2 = CreateTestBeat("Beat 2");
        _viewModel.StructureBeats.Add(beat1);
        _viewModel.StructureBeats.Add(beat2);
        _viewModel.SelectedBeat = null;

        // Act
        _viewModel.MoveDown(null, null);

        // Assert
        Assert.AreEqual(0, _viewModel.StructureBeats.IndexOf(beat1));
        Assert.AreEqual(1, _viewModel.StructureBeats.IndexOf(beat2));
    }

    [TestMethod]
    public void MoveDown_WithMultipleBeats_ShouldMoveCorrectly()
    {
        // Arrange
        var beat1 = CreateTestBeat("Beat 1");
        var beat2 = CreateTestBeat("Beat 2");
        var beat3 = CreateTestBeat("Beat 3");
        _viewModel.StructureBeats.Add(beat1);
        _viewModel.StructureBeats.Add(beat2);
        _viewModel.StructureBeats.Add(beat3);
        _viewModel.SelectedBeat = beat2;
        _viewModel.SelectedBeatIndex = 1;

        // Act
        _viewModel.MoveDown(null, null);

        // Assert
        Assert.AreEqual(0, _viewModel.StructureBeats.IndexOf(beat1));
        Assert.AreEqual(2, _viewModel.StructureBeats.IndexOf(beat2));
        Assert.AreEqual(1, _viewModel.StructureBeats.IndexOf(beat3));
    }

    [TestMethod]
    public void MoveUp_WithMultipleBeats_ShouldMoveCorrectly()
    {
        // Arrange
        var beat1 = CreateTestBeat("Beat 1");
        var beat2 = CreateTestBeat("Beat 2");
        var beat3 = CreateTestBeat("Beat 3");
        _viewModel.StructureBeats.Add(beat1);
        _viewModel.StructureBeats.Add(beat2);
        _viewModel.StructureBeats.Add(beat3);
        _viewModel.SelectedBeat = beat2;
        _viewModel.SelectedBeatIndex = 1;

        // Act
        _viewModel.MoveUp(null, null);

        // Assert
        Assert.AreEqual(1, _viewModel.StructureBeats.IndexOf(beat1));
        Assert.AreEqual(0, _viewModel.StructureBeats.IndexOf(beat2));
        Assert.AreEqual(2, _viewModel.StructureBeats.IndexOf(beat3));
    }

    [TestMethod]
    public void BeatOperations_IntegrationTest_ShouldWorkTogether()
    {
        // Arrange - Start with empty collection
        Assert.AreEqual(0, _viewModel.StructureBeats.Count);

        // Act & Assert - Create beats
        _viewModel.CreateBeat(null, null);
        _viewModel.CreateBeat(null, null);
        _viewModel.CreateBeat(null, null);
        Assert.AreEqual(3, _viewModel.StructureBeats.Count);

        // Act & Assert - Move middle beat up
        _viewModel.SelectedBeat = _viewModel.StructureBeats[1];
        _viewModel.SelectedBeatIndex = 1;
        _viewModel.MoveUp(null, null);
        Assert.AreEqual(0, _viewModel.StructureBeats.IndexOf(_viewModel.SelectedBeat));

        // Act & Assert - Move it down twice
        _viewModel.SelectedBeatIndex = 0;
        _viewModel.MoveDown(null, null);
        Assert.AreEqual(1, _viewModel.StructureBeats.IndexOf(_viewModel.SelectedBeat));

        _viewModel.SelectedBeatIndex = 1;
        _viewModel.MoveDown(null, null);
        Assert.AreEqual(2, _viewModel.StructureBeats.IndexOf(_viewModel.SelectedBeat));
    }

    [TestMethod]
    public void StructureBeats_PropertyChanges_ShouldNotifyPropertyChanged()
    {
        // Arrange
        var propertyChangedFired = false;
        _viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(_viewModel.StructureBeats))
            {
                propertyChangedFired = true;
            }
        };

        // Act
        _viewModel.StructureBeats = new ObservableCollection<StructureBeatViewModel>();

        // Assert
        Assert.IsTrue(propertyChangedFired);
    }

    [TestMethod]
    public void CurrentElementSource_OnLoadModel_DefaultsToScenes()
    {
        // Arrange - Setup AppState with CurrentDocument
        var appState = Ioc.Default.GetRequiredService<AppState>();
        appState.CurrentDocument = new StoryDocument(_storyModel);

        // Act
        _viewModel.Activate(_problemModel);

        // Assert
        Assert.IsNotNull(_viewModel.CurrentElementSource);
        Assert.AreSame(_viewModel.Scenes, _viewModel.CurrentElementSource);
    }

    [TestMethod]
    public void SelectedElementSource_WhenSetToScene_SetsCurrentElementSourceToScenes()
    {
        // Arrange - Setup AppState with CurrentDocument
        var appState = Ioc.Default.GetRequiredService<AppState>();
        appState.CurrentDocument = new StoryDocument(_storyModel);
        _viewModel.Activate(_problemModel);

        // Act
        _viewModel.SelectedElementSource = "Scene";

        // Assert
        Assert.AreSame(_viewModel.Scenes, _viewModel.CurrentElementSource);
    }

    [TestMethod]
    public void SelectedElementSource_WhenSetToProblem_SetsCurrentElementSourceToProblems()
    {
        // Arrange - AppState already setup in TestInitialize via SetupViewModelWithStory
        _viewModel.Activate(_problemModel);

        // Verify initial state
        Assert.AreEqual("Scene", _viewModel.SelectedElementSource, "Initial SelectedElementSource should be 'Scene'");
        Assert.IsNotNull(_viewModel.Problems, "Problems should not be null");
        Assert.IsNotNull(_viewModel.Scenes, "Scenes should not be null");
        Assert.AreSame(_viewModel.Scenes, _viewModel.CurrentElementSource, "Initial CurrentElementSource should be Scenes");

        // Act
        _viewModel.SelectedElementSource = "Problem";

        // Assert
        Assert.AreEqual("Problem", _viewModel.SelectedElementSource, "SelectedElementSource should be 'Problem'");
        Assert.AreSame(_viewModel.Problems, _viewModel.CurrentElementSource);
    }

    [TestMethod]
    public void CurrentElementSource_WhenToggled_RaisesPropertyChanged()
    {
        // Arrange - AppState already setup in TestInitialize via SetupViewModelWithStory
        _viewModel.Activate(_problemModel);

        // Ensure we start from a known state (Scene) since singleton may have stale state
        _viewModel.SelectedElementSource = "Scene";

        var propertyChangedFired = false;
        _viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(_viewModel.CurrentElementSource))
            {
                propertyChangedFired = true;
            }
        };

        // Act - Toggle from Scene to Problem
        _viewModel.SelectedElementSource = "Problem";

        // Assert
        Assert.IsTrue(propertyChangedFired);
    }

    [TestMethod]
    public void CurrentElementDescription_WhenSelectedBeatChanges_UpdatesFromBeatElement()
    {
        // Arrange
        _viewModel.Activate(_problemModel);
        var beat = CreateTestBeat("Test Beat");
        beat.Element = _storyModel.StoryElements.Scenes.First();
        _viewModel.StructureBeats.Add(beat);

        // Act
        _viewModel.SelectedBeat = beat;

        // Assert
        Assert.AreEqual(beat.ElementDescription, _viewModel.CurrentElementDescription);
    }

    [TestMethod]
    public void CurrentElementDescription_WhenSelectedBeatHasNoElement_IsNull()
    {
        // Arrange
        _viewModel.Activate(_problemModel);
        var beat = CreateTestBeat("Test Beat");
        _viewModel.StructureBeats.Add(beat);

        // Act
        _viewModel.SelectedBeat = beat;

        // Assert
        Assert.IsNull(_viewModel.CurrentElementDescription);
    }

    [TestMethod]
    public void CurrentElementDescription_WhenSelectedListElementChanges_UpdatesFromElement()
    {
        // Arrange
        _viewModel.Activate(_problemModel);
        var scene = _storyModel.StoryElements.Scenes.First();

        // Act
        _viewModel.SelectedListElement = scene;

        // Assert
        Assert.AreEqual(scene.Description, _viewModel.CurrentElementDescription);
    }

    [TestMethod]
    public void SelectedListElement_WhenSet_RaisesPropertyChanged()
    {
        // Arrange
        _viewModel.Activate(_problemModel);
        var propertyChangedFired = false;
        _viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(_viewModel.SelectedListElement))
                propertyChangedFired = true;
        };

        // Act
        _viewModel.SelectedListElement = _storyModel.StoryElements.Scenes.First();

        // Assert
        Assert.IsTrue(propertyChangedFired);
    }

    #region Helper Methods

    /// <summary>
    ///     Creates a ProblemViewModel with a properly configured StoryModel in AppState.
    ///     This follows the pattern from SceneViewModelTests using IoC container.
    /// </summary>
    private ProblemViewModel SetupViewModelWithStory()
    {
        // Get AppState from IoC container (no fake implementations)
        var appState = Ioc.Default.GetRequiredService<AppState>();

        // Create and assign a StoryDocument with our test StoryModel
        appState.CurrentDocument = new StoryDocument(_storyModel);

        // Get ProblemViewModel from IoC container with all dependencies injected
        var viewModel = Ioc.Default.GetService<ProblemViewModel>();

        return viewModel;
    }

    /// <summary>
    ///     Creates a test StoryModel populated with necessary story elements.
    ///     This provides realistic test data for ViewModel operations.
    /// </summary>
    private StoryModel CreateTestStoryModel()
    {
        // Create base story model
        var storyModel = new StoryModel();

        // Add OverviewModel to ExplorerView (required by ProblemViewModel.LoadModel at line 790)
        var overview = new OverviewModel("Test Story", storyModel, null);
        storyModel.ExplorerView.Add(overview.Node); // Add the Node, not the OverviewModel itself

        // Add test characters to the story
        var character1 = new CharacterModel("Test Character 1", storyModel, null);
        var character2 = new CharacterModel("Test Character 2", storyModel, null);
        storyModel.StoryElements.Characters.Add(character1);
        storyModel.StoryElements.Characters.Add(character2);

        // Add test scene to the story
        var scene = new SceneModel("Test Scene", storyModel, null);
        storyModel.StoryElements.Scenes.Add(scene);

        // Add test problem to the story (required for CurrentElementSource toggle tests)
        var problem = new ProblemModel("Test Story Problem", storyModel, null);
        storyModel.StoryElements.Problems.Add(problem);

        return storyModel;
    }

    /// <summary>
    ///     Creates a test ProblemModel with realistic default values.
    ///     Uses the pattern: new ProblemModel(name, parent, null)
    /// </summary>
    private ProblemModel CreateTestProblemModel(StoryModel parent = null)
    {
        // Use provided parent or create new one
        parent ??= CreateTestStoryModel();

        // Create problem with standard constructor
        var problem = new ProblemModel("Test Problem", parent, null);

        // Set common problem properties
        problem.ProblemType = "Character";
        problem.ConflictType = "Internal";
        problem.Subject = "Test Subject";

        return problem;
    }

    /// <summary>
    ///     Helper method to create test beats without relying on IoC container
    /// </summary>
    private StructureBeatViewModel CreateTestBeat(string title, string description = "Test Description")
    {
        // Create a beat with the required constructor parameters
        StructureBeatViewModel beat = new(title, description);

        return beat;
    }

    #endregion

    [TestCleanup]
    public void TestCleanup()
    {
        // Clean up test references (don't unsubscribe handler - breaks singleton for next test)
        _viewModel = null;
        _problemModel = null;
        _storyModel = null;
    }
}
