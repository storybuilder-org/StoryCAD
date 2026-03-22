using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCADLib.Models;
using StoryCADLib.Services;
using StoryCADLib.ViewModels;
using StoryCADLib.ViewModels.SubViewModels;
using StoryCADLib.ViewModels.Tools;

#nullable disable

namespace StoryCADTests.ViewModels;

[TestClass]
public class BeatEditorViewModelTests
{
    private BeatEditorViewModel _beatEditor;
    private StoryModel _storyModel;
    private ProblemModel _problemModel;
    private bool _dirtyNotified;

    [TestInitialize]
    public void TestInitialize()
    {
        // Reset AppState to clean state
        var appState = Ioc.Default.GetRequiredService<AppState>();
        appState.CurrentDocument = null;

        _dirtyNotified = false;
        _storyModel = CreateTestStoryModel();
        _problemModel = CreateTestProblemModel(_storyModel);

        // Set up AppState with document so element resolution works
        appState.CurrentDocument = new StoryDocument(_storyModel);

        _beatEditor = new BeatEditorViewModel(() => _dirtyNotified = true);
    }

    #region Step 1: Construction Tests

    [TestMethod]
    public void Constructor_ShouldInitializeStructureBeatsCollection()
    {
        Assert.IsNotNull(_beatEditor.StructureBeats);
        Assert.AreEqual(0, _beatEditor.StructureBeats.Count);
    }

    [TestMethod]
    public void Constructor_ShouldSetDefaultElementSource()
    {
        Assert.AreEqual("Scene", _beatEditor.SelectedElementSource);
    }

    [TestMethod]
    public void Constructor_ShouldExposeElementSourceList()
    {
        Assert.IsNotNull(_beatEditor.ElementSource);
        Assert.AreEqual(2, _beatEditor.ElementSource.Count);
        Assert.IsTrue(_beatEditor.ElementSource.Contains("Scene"));
        Assert.IsTrue(_beatEditor.ElementSource.Contains("Problem"));
    }

    #endregion

    #region Step 2: Beat CRUD Command Tests

    [TestMethod]
    public void CreateBeatCommand_ShouldAddNewBeat()
    {
        // Arrange
        Assert.AreEqual(0, _beatEditor.StructureBeats.Count);

        // Act
        _beatEditor.CreateBeatCommand.Execute(null);

        // Assert
        Assert.AreEqual(1, _beatEditor.StructureBeats.Count);
        Assert.AreEqual("New Beat", _beatEditor.StructureBeats[0].Title);
        Assert.AreEqual("Describe your beat here", _beatEditor.StructureBeats[0].Description);
    }

    [TestMethod]
    public void CreateBeatCommand_MultipleCalls_ShouldAddMultipleBeats()
    {
        _beatEditor.CreateBeatCommand.Execute(null);
        _beatEditor.CreateBeatCommand.Execute(null);

        Assert.AreEqual(2, _beatEditor.StructureBeats.Count);
    }

    [TestMethod]
    public void DeleteBeatCommand_WithValidSelection_ShouldRemoveBeat()
    {
        // Arrange — need StoryModel context for DeleteBeat
        _beatEditor.SetStoryModel(_storyModel);
        _beatEditor.SetProblemModel(_problemModel);
        _problemModel.StructureBeats = _beatEditor.StructureBeats;
        _beatEditor.StructureBeats.Add(CreateTestBeat("Beat 1"));
        _beatEditor.SelectedBeat = _beatEditor.StructureBeats[0];
        _beatEditor.SelectedBeatIndex = 0;

        // Act
        _beatEditor.DeleteBeatCommand.Execute(null);

        // Assert
        Assert.AreEqual(0, _beatEditor.StructureBeats.Count);
    }

    [TestMethod]
    public void DeleteBeatCommand_CanExecute_FalseWhenNoBeatSelected()
    {
        _beatEditor.SelectedBeat = null;
        Assert.IsFalse(_beatEditor.DeleteBeatCommand.CanExecute(null));
    }

    [TestMethod]
    public void DeleteBeatCommand_CanExecute_TrueWhenBeatSelected()
    {
        _beatEditor.StructureBeats.Add(CreateTestBeat("Beat 1"));
        _beatEditor.SelectedBeat = _beatEditor.StructureBeats[0];
        _beatEditor.SelectedBeatIndex = 0;

        Assert.IsTrue(_beatEditor.DeleteBeatCommand.CanExecute(null));
    }

    #endregion

    #region Step 3: Move Command Tests

    [TestMethod]
    public void MoveUpCommand_AtIndexOne_ShouldMoveToIndexZero()
    {
        // Arrange
        _beatEditor.StructureBeats.Add(CreateTestBeat("Beat 1"));
        _beatEditor.StructureBeats.Add(CreateTestBeat("Beat 2"));
        _beatEditor.SelectedBeat = _beatEditor.StructureBeats[1];
        _beatEditor.SelectedBeatIndex = 1;

        // Act
        _beatEditor.MoveUpCommand.Execute(null);

        // Assert
        Assert.AreEqual("Beat 2", _beatEditor.StructureBeats[0].Title);
        Assert.AreEqual("Beat 1", _beatEditor.StructureBeats[1].Title);
    }

    [TestMethod]
    public void MoveUpCommand_CanExecute_FalseWhenAtTop()
    {
        _beatEditor.StructureBeats.Add(CreateTestBeat("Beat 1"));
        _beatEditor.SelectedBeat = _beatEditor.StructureBeats[0];
        _beatEditor.SelectedBeatIndex = 0;

        Assert.IsFalse(_beatEditor.MoveUpCommand.CanExecute(null));
    }

    [TestMethod]
    public void MoveUpCommand_CanExecute_FalseWhenNoBeatSelected()
    {
        _beatEditor.SelectedBeat = null;
        Assert.IsFalse(_beatEditor.MoveUpCommand.CanExecute(null));
    }

    [TestMethod]
    public void MoveDownCommand_AtIndexZero_ShouldMoveToIndexOne()
    {
        // Arrange
        _beatEditor.StructureBeats.Add(CreateTestBeat("Beat 1"));
        _beatEditor.StructureBeats.Add(CreateTestBeat("Beat 2"));
        _beatEditor.SelectedBeat = _beatEditor.StructureBeats[0];
        _beatEditor.SelectedBeatIndex = 0;

        // Act
        _beatEditor.MoveDownCommand.Execute(null);

        // Assert
        Assert.AreEqual("Beat 2", _beatEditor.StructureBeats[0].Title);
        Assert.AreEqual("Beat 1", _beatEditor.StructureBeats[1].Title);
    }

    [TestMethod]
    public void MoveDownCommand_CanExecute_FalseWhenAtBottom()
    {
        _beatEditor.StructureBeats.Add(CreateTestBeat("Beat 1"));
        _beatEditor.SelectedBeat = _beatEditor.StructureBeats[0];
        _beatEditor.SelectedBeatIndex = 0;

        Assert.IsFalse(_beatEditor.MoveDownCommand.CanExecute(null));
    }

    [TestMethod]
    public void MoveDownCommand_CanExecute_FalseWhenNoBeatSelected()
    {
        _beatEditor.SelectedBeat = null;
        Assert.IsFalse(_beatEditor.MoveDownCommand.CanExecute(null));
    }

    #endregion

    #region Step 4: Unbind Command Tests

    [TestMethod]
    public void UnbindElementCommand_WithBoundBeat_ClearsGuid()
    {
        // Arrange
        _beatEditor.SetStoryModel(_storyModel);
        _beatEditor.SetProblemModel(_problemModel);
        var scene = _storyModel.StoryElements.Scenes.First(s => s.Uuid != Guid.Empty);
        var beat = CreateTestBeat("Beat 1");
        beat.Guid = scene.Uuid;
        _beatEditor.StructureBeats.Add(beat);
        _problemModel.StructureBeats = _beatEditor.StructureBeats;
        _beatEditor.SelectedBeat = beat;
        _beatEditor.SelectedBeatIndex = 0;

        // Verify CanExecute is true before executing
        Assert.IsTrue(_beatEditor.UnbindElementCommand.CanExecute(null),
            $"CanExecute should be true. SelectedBeat={_beatEditor.SelectedBeat != null}, Guid={_beatEditor.SelectedBeat?.Guid}");

        // Act
        _beatEditor.UnbindElementCommand.Execute(null);

        // Assert
        Assert.AreEqual(Guid.Empty, beat.Guid);
        Assert.IsNull(_beatEditor.SelectedBeat);
        Assert.AreEqual(-1, _beatEditor.SelectedBeatIndex);
    }

    [TestMethod]
    public void UnbindElementCommand_CanExecute_FalseWhenNoBeatSelected()
    {
        _beatEditor.SelectedBeat = null;
        Assert.IsFalse(_beatEditor.UnbindElementCommand.CanExecute(null));
    }

    [TestMethod]
    public void UnbindElementCommand_CanExecute_FalseWhenBeatHasNoBinding()
    {
        var beat = CreateTestBeat("Beat 1");
        beat.Guid = Guid.Empty;
        _beatEditor.StructureBeats.Add(beat);
        _beatEditor.SelectedBeat = beat;
        _beatEditor.SelectedBeatIndex = 0;

        Assert.IsFalse(_beatEditor.UnbindElementCommand.CanExecute(null));
    }

    #endregion

    #region Step 5: PropertyChanged Cascade Tests

    [TestMethod]
    public void SelectedElementSource_WhenSetToScene_SetsCurrentElementSourceToScenes()
    {
        // Arrange — need Scenes populated
        _beatEditor.Scenes = _storyModel.StoryElements.Scenes;
        _beatEditor.Problems = _storyModel.StoryElements.Problems;
        _beatEditor.SelectedElementSource = "Problem"; // start from Problem

        // Act
        _beatEditor.SelectedElementSource = "Scene";

        // Assert
        Assert.AreSame(_beatEditor.Scenes, _beatEditor.CurrentElementSource);
    }

    [TestMethod]
    public void SelectedElementSource_WhenSetToProblem_SetsCurrentElementSourceToProblems()
    {
        // Arrange
        _beatEditor.Scenes = _storyModel.StoryElements.Scenes;
        _beatEditor.Problems = _storyModel.StoryElements.Problems;

        // Act
        _beatEditor.SelectedElementSource = "Problem";

        // Assert
        Assert.AreSame(_beatEditor.Problems, _beatEditor.CurrentElementSource);
    }

    [TestMethod]
    public void SelectedBeat_WhenChanged_UpdatesCurrentElementDescription()
    {
        // Arrange
        var beat = CreateTestBeat("Beat 1");
        _beatEditor.StructureBeats.Add(beat);

        // Act
        _beatEditor.SelectedBeat = beat;

        // Assert — beat has no bound element, so description from beat's own ElementDescription
        Assert.AreEqual(beat.ElementDescription, _beatEditor.CurrentElementDescription);
    }

    [TestMethod]
    public void SelectedBeat_WhenChanged_NotifiesCanExecuteOnCommands()
    {
        // Arrange
        var beat = CreateTestBeat("Beat 1");
        _beatEditor.StructureBeats.Add(beat);
        var canExecuteChangedFired = false;
        _beatEditor.DeleteBeatCommand.CanExecuteChanged += (_, _) => canExecuteChangedFired = true;

        // Act
        _beatEditor.SelectedBeat = beat;

        // Assert
        Assert.IsTrue(canExecuteChangedFired);
    }

    #endregion

    #region Step 6: LoadBeats / SaveBeats Tests

    [TestMethod]
    public void LoadBeats_ShouldPopulateStructureBeats()
    {
        // Arrange
        _problemModel.StructureBeats = new ObservableCollection<StructureBeat>
        {
            CreateTestBeat("Beat 1"),
            CreateTestBeat("Beat 2")
        };

        // Act
        _beatEditor.LoadBeats(_problemModel, _storyModel);

        // Assert
        Assert.AreSame(_problemModel.StructureBeats, _beatEditor.StructureBeats);
        Assert.AreEqual(2, _beatEditor.StructureBeats.Count);
    }

    [TestMethod]
    public void LoadBeats_ShouldSetStructureModelTitle()
    {
        _problemModel.StructureTitle = "Save the Cat";
        _problemModel.StructureBeats = new ObservableCollection<StructureBeat>();

        _beatEditor.LoadBeats(_problemModel, _storyModel);

        Assert.AreEqual("Save the Cat", _beatEditor.StructureModelTitle);
    }

    [TestMethod]
    public void LoadBeats_CustomSheet_SetsEditableTrue()
    {
        _problemModel.StructureTitle = "Custom Beat Sheet";
        _problemModel.StructureBeats = new ObservableCollection<StructureBeat>();

        _beatEditor.LoadBeats(_problemModel, _storyModel);

        Assert.IsFalse(_beatEditor.IsBeatSheetReadOnly);
        Assert.AreEqual(Visibility.Visible, _beatEditor.BeatsheetEditButtonsVisibility);
    }

    [TestMethod]
    public void LoadBeats_TemplateSheet_SetsReadOnlyTrue()
    {
        _problemModel.StructureTitle = "Save the Cat";
        _problemModel.StructureBeats = new ObservableCollection<StructureBeat>();

        _beatEditor.LoadBeats(_problemModel, _storyModel);

        Assert.IsTrue(_beatEditor.IsBeatSheetReadOnly);
        Assert.AreEqual(Visibility.Collapsed, _beatEditor.BeatsheetEditButtonsVisibility);
    }

    [TestMethod]
    public void SaveBeats_ShouldWriteBackToProblemModel()
    {
        // Arrange — load first, then modify
        _problemModel.StructureBeats = new ObservableCollection<StructureBeat>();
        _beatEditor.LoadBeats(_problemModel, _storyModel);
        _beatEditor.StructureModelTitle = "My Custom Sheet";
        _beatEditor.StructureDescription = "A test description";
        _beatEditor.StructureBeats.Add(CreateTestBeat("New Beat"));

        // Act
        _beatEditor.SaveBeats(_problemModel);

        // Assert
        Assert.AreEqual("My Custom Sheet", _problemModel.StructureTitle);
        Assert.AreEqual("A test description", _problemModel.StructureDescription);
        Assert.AreEqual(1, _problemModel.StructureBeats.Count);
    }

    [TestMethod]
    public void SaveBeats_ShouldWriteBoundStructure()
    {
        _problemModel.StructureBeats = new ObservableCollection<StructureBeat>();
        _beatEditor.LoadBeats(_problemModel, _storyModel);
        _beatEditor.BoundStructure = "some-guid-string";

        _beatEditor.SaveBeats(_problemModel);

        Assert.AreEqual("some-guid-string", _problemModel.BoundStructure);
    }

    #endregion

    #region Helper Methods

    private StoryModel CreateTestStoryModel()
    {
        var storyModel = new StoryModel();
        var overview = new OverviewModel("Test Story", storyModel, null);
        storyModel.ExplorerView.Add(overview.Node);

        var character1 = new CharacterModel("Test Character 1", storyModel, null);
        storyModel.StoryElements.Characters.Add(character1);

        var scene = new SceneModel("Test Scene", storyModel, null);
        storyModel.StoryElements.Scenes.Add(scene);

        var problem = new ProblemModel("Test Story Problem", storyModel, null);
        storyModel.StoryElements.Problems.Add(problem);

        return storyModel;
    }

    private ProblemModel CreateTestProblemModel(StoryModel parent = null)
    {
        parent ??= CreateTestStoryModel();
        var problem = new ProblemModel("Test Problem", parent, null);
        problem.ProblemType = "Character";
        problem.ConflictType = "Internal";
        return problem;
    }

    private StructureBeat CreateTestBeat(string title, string description = "Test Description")
    {
        return new StructureBeat(title, description);
    }

    #endregion

    [TestCleanup]
    public void TestCleanup()
    {
        _beatEditor = null;
        _problemModel = null;
        _storyModel = null;
    }
}
