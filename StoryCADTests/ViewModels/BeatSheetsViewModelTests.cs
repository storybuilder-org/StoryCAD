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
public class BeatSheetsViewModelTests
{
    private BeatSheetsViewModel _beatEditor;
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

        _beatEditor = new BeatSheetsViewModel(() => _dirtyNotified = true);
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
    public void CreateBeatCommand_NoBeatSheetSelected_DoesNotAddBeat()
    {
        // Arrange — no beat sheet loaded, StructureModelTitle is null
        Assert.AreEqual(0, _beatEditor.StructureBeats.Count);

        // Act
        _beatEditor.CreateBeatCommand.Execute(null);

        // Assert
        Assert.AreEqual(0, _beatEditor.StructureBeats.Count);
    }

    [TestMethod]
    public void CreateBeatCommand_ShouldAddNewBeat()
    {
        // Arrange
        _beatEditor.StructureModelTitle = "Save the Cat";
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
        _beatEditor.StructureModelTitle = "Save the Cat";
        _beatEditor.CreateBeatCommand.Execute(null);
        _beatEditor.CreateBeatCommand.Execute(null);

        Assert.AreEqual(2, _beatEditor.StructureBeats.Count);
    }

    [TestMethod]
    public void DeleteBeatCommand_WithNoSelection_DoesNotRemove()
    {
        _beatEditor.LoadBeats(_problemModel, _storyModel);
        _beatEditor.StructureModelTitle = "Save the Cat";
        _beatEditor.CreateBeatCommand.Execute(null);
        Assert.AreEqual(1, _beatEditor.StructureBeats.Count);

        // No selection
        _beatEditor.SelectedBeat = null;
        _beatEditor.DeleteBeatCommand.Execute(null);

        Assert.AreEqual(1, _beatEditor.StructureBeats.Count);
    }

    [TestMethod]
    public void DeleteBeatCommand_WithSelection_RemovesBeat()
    {
        _beatEditor.LoadBeats(_problemModel, _storyModel);
        _beatEditor.StructureModelTitle = "Save the Cat";
        _beatEditor.CreateBeatCommand.Execute(null);
        Assert.AreEqual(1, _beatEditor.StructureBeats.Count);

        _beatEditor.SelectedBeat = _beatEditor.StructureBeats[0];
        _beatEditor.SelectedBeatIndex = 0;
        _beatEditor.DeleteBeatCommand.Execute(null);

        Assert.AreEqual(0, _beatEditor.StructureBeats.Count);
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
    public void SelectedBeat_WhenChanged_UpdatesSelectedBeatDescription()
    {
        // Arrange
        var beat = CreateTestBeat("Beat 1", "My beat description");
        _beatEditor.StructureBeats.Add(beat);

        // Act
        _beatEditor.SelectedBeat = beat;

        // Assert
        Assert.AreEqual("My beat description", _beatEditor.SelectedBeatDescription);
    }

    [TestMethod]
    public void SelectedBeat_WhenNull_ClearsSelectedBeatDescription()
    {
        // Arrange
        var beat = CreateTestBeat("Beat 1", "My beat description");
        _beatEditor.StructureBeats.Add(beat);
        _beatEditor.SelectedBeat = beat;

        // Act
        _beatEditor.SelectedBeat = null;

        // Assert
        Assert.IsNull(_beatEditor.SelectedBeatDescription);
    }

    [TestMethod]
    public void SelectedListElement_WhenSet_UpdatesCurrentElementDescription()
    {
        // Arrange — use a scene with a description
        var scene = _storyModel.StoryElements.Scenes.First(s => s.Uuid != Guid.Empty);
        scene.Description = "A test scene description";

        // Act
        _beatEditor.SelectedListElement = scene;

        // Assert
        Assert.AreEqual("A test scene description", _beatEditor.CurrentElementDescription);
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
        var testGuid = Guid.NewGuid();
        _beatEditor.BoundStructure = testGuid;

        _beatEditor.SaveBeats(_problemModel);

        Assert.AreEqual(testGuid, _problemModel.BoundStructure);
    }

    #endregion

    #region Beat Assignment and Element Resolution Tests

    [TestMethod]
    public void AssignBeat_SceneGuid_UpdatesElementName()
    {
        var scene = new SceneModel("Registered Scene", _storyModel, null);
        _beatEditor.LoadBeats(_problemModel, _storyModel);
        var beat = CreateTestBeat("Test Beat");
        _beatEditor.StructureBeats.Add(beat);

        beat.Guid = scene.Uuid;

        Assert.AreEqual(scene.Name, beat.ElementName);
    }

    [TestMethod]
    public void AssignBeat_ProblemGuid_UpdatesElementName()
    {
        var problem = new ProblemModel("Registered Problem", _storyModel, null);
        _beatEditor.LoadBeats(_problemModel, _storyModel);
        var beat = CreateTestBeat("Test Beat");
        _beatEditor.StructureBeats.Add(beat);

        beat.Guid = problem.Uuid;

        Assert.AreEqual(problem.Name, beat.ElementName);
    }

    [TestMethod]
    public void UnboundBeat_ElementName_ReturnsUnassigned()
    {
        var beat = CreateTestBeat("Test Beat");

        Assert.AreEqual("Unassigned", beat.ElementName);
    }

    [TestMethod]
    public void AssignBeat_SetGuidToEmpty_RestoresUnassigned()
    {
        _beatEditor.LoadBeats(_problemModel, _storyModel);
        var beat = CreateTestBeat("Test Beat");
        var scene = _storyModel.StoryElements.Scenes.First();
        beat.Guid = scene.Uuid;

        beat.Guid = Guid.Empty;

        Assert.AreEqual("Unassigned", beat.ElementName);
    }

    [TestMethod]
    public async Task AssignBeatAsync_ProblemToSelf_RejectsAssignment()
    {
        _beatEditor.LoadBeats(_problemModel, _storyModel);
        var beat = CreateTestBeat("Test Beat");
        _beatEditor.StructureBeats.Add(beat);
        _beatEditor.SelectedBeat = beat;
        _beatEditor.SelectedListElement = _problemModel;

        await _beatEditor.AssignBeatAsync();

        Assert.AreEqual(Guid.Empty, beat.Guid, "Beat should remain unassigned when assigning problem to itself");
    }

    [TestMethod]
    public async Task AssignBeatAsync_ProblemAlreadyBound_ClearsOldParentBeat()
    {
        // Problem A has a beat sheet with problem B assigned
        var problemA = _storyModel.StoryElements.OfType<ProblemModel>().First(p => p.Name == "Test Story Problem");
        var problemB = new ProblemModel("Problem B", _storyModel, null);
        var beatOnA = new StructureBeat("Beat on A", "Desc") { Guid = problemB.Uuid };
        problemA.StructureBeats.Add(beatOnA);
        problemB.BoundStructure = problemA.Uuid;

        // Our test problem (_problemModel) has a beat we want to assign B to
        _beatEditor.LoadBeats(_problemModel, _storyModel);
        var beat = CreateTestBeat("Beat on test problem");
        _beatEditor.StructureBeats.Add(beat);
        _beatEditor.SelectedBeat = beat;
        _beatEditor.SelectedBeatIndex = 0;
        _beatEditor.SelectedListElement = problemB;

        await _beatEditor.AssignBeatAsync();

        Assert.AreEqual(Guid.Empty, beatOnA.Guid, "Old parent's beat should be cleared");
        Assert.AreEqual(problemB.Uuid, beat.Guid, "New beat should point to problem B");
        Assert.AreEqual(_problemModel.Uuid, problemB.BoundStructure, "BoundStructure should point to new parent");
    }

    [TestMethod]
    public void ElementIcon_Scene_ReturnsWorldSymbol()
    {
        var scene = new SceneModel("Icon Test Scene", _storyModel, null);
        _beatEditor.LoadBeats(_problemModel, _storyModel);
        var beat = CreateTestBeat("Test Beat");

        beat.Guid = scene.Uuid;

        Assert.AreEqual(Symbol.World, beat.ElementIcon);
    }

    [TestMethod]
    public void ElementIcon_Problem_ReturnsHelpSymbol()
    {
        var problem = new ProblemModel("Icon Test Problem", _storyModel, null);
        _beatEditor.LoadBeats(_problemModel, _storyModel);
        var beat = CreateTestBeat("Test Beat");

        beat.Guid = problem.Uuid;

        Assert.AreEqual(Symbol.Help, beat.ElementIcon);
    }

    [TestMethod]
    public void ElementIcon_Unassigned_ReturnsBulletsSymbol()
    {
        var beat = CreateTestBeat("Test Beat");

        Assert.AreEqual(Symbol.Bullets, beat.ElementIcon);
    }

    [TestMethod]
    public void UnbindElement_WithBoundBeat_ClearsGuid()
    {
        _beatEditor.LoadBeats(_problemModel, _storyModel);
        var beat = CreateTestBeat("Test Beat");
        var scene = _storyModel.StoryElements.Scenes.First(s => s.Name == "Test Scene");
        beat.Guid = scene.Uuid;
        _beatEditor.StructureBeats.Add(beat);
        _problemModel.StructureBeats = _beatEditor.StructureBeats;
        _beatEditor.SelectedBeat = beat;
        _beatEditor.SelectedBeatIndex = 0;

        _beatEditor.UnbindElement();

        Assert.AreEqual(Guid.Empty, beat.Guid);
        Assert.IsNull(_beatEditor.SelectedBeat);
        Assert.AreEqual(-1, _beatEditor.SelectedBeatIndex);
    }

    [TestMethod]
    public void UnbindElement_WithNullSelectedBeat_DoesNotCrash()
    {
        _beatEditor.LoadBeats(_problemModel, _storyModel);
        _beatEditor.SelectedBeat = null;

        _beatEditor.UnbindElement();
    }

    [TestMethod]
    public void UnbindElement_WithEmptyGuidBeat_DoesNothing()
    {
        _beatEditor.LoadBeats(_problemModel, _storyModel);
        var beat = CreateTestBeat("Test Beat");
        _beatEditor.StructureBeats.Add(beat);
        _beatEditor.SelectedBeat = beat;
        _beatEditor.SelectedBeatIndex = 0;

        _beatEditor.UnbindElement();

        Assert.AreEqual(Guid.Empty, beat.Guid);
    }

    [TestMethod]
    public void StructureBeat_SerializationRoundTrip_PreservesData()
    {
        var beat = CreateTestBeat("Test Title", "Test Description");
        var testGuid = Guid.NewGuid();
        beat.Guid = testGuid;

        var json = System.Text.Json.JsonSerializer.Serialize(beat);
        var deserialized = System.Text.Json.JsonSerializer.Deserialize<StructureBeat>(json);

        Assert.AreEqual("Test Title", deserialized.Title);
        Assert.AreEqual("Test Description", deserialized.Description);
        Assert.AreEqual(testGuid, deserialized.Guid);
    }

    #endregion

    #region Dirty Tracking Tests

    [TestMethod]
    public void StructureModelTitle_WhenChanged_NotifiesDirty()
    {
        _beatEditor.LoadBeats(_problemModel, _storyModel);
        _dirtyNotified = false;

        _beatEditor.StructureModelTitle = "New Title";

        Assert.IsTrue(_dirtyNotified);
    }

    [TestMethod]
    public void StructureDescription_WhenChanged_NotifiesDirty()
    {
        _beatEditor.LoadBeats(_problemModel, _storyModel);
        _dirtyNotified = false;

        _beatEditor.StructureDescription = "New Description";

        Assert.IsTrue(_dirtyNotified);
    }

    [TestMethod]
    public void BoundStructure_WhenChanged_NotifiesDirty()
    {
        _beatEditor.LoadBeats(_problemModel, _storyModel);
        _dirtyNotified = false;

        _beatEditor.BoundStructure = Guid.NewGuid();

        Assert.IsTrue(_dirtyNotified);
    }

    [TestMethod]
    public void SelectedBeatDescription_WhenEdited_NotifiesDirty()
    {
        _beatEditor.LoadBeats(_problemModel, _storyModel);
        var beat = CreateTestBeat("Beat 1", "Original");
        _beatEditor.StructureBeats.Add(beat);
        _beatEditor.SelectedBeat = beat;
        _dirtyNotified = false;

        _beatEditor.SelectedBeatDescription = "Edited description";

        Assert.IsTrue(_dirtyNotified);
    }

    [TestMethod]
    public void LoadBeats_DoesNotNotifyDirty()
    {
        _dirtyNotified = false;

        _beatEditor.LoadBeats(_problemModel, _storyModel);

        Assert.IsFalse(_dirtyNotified);
    }

    [TestMethod]
    public void SelectedBeat_WhenChanged_DoesNotNotifyDirty()
    {
        _beatEditor.LoadBeats(_problemModel, _storyModel);
        var beat = CreateTestBeat("Beat 1");
        _beatEditor.StructureBeats.Add(beat);
        _dirtyNotified = false;

        _beatEditor.SelectedBeat = beat;

        Assert.IsFalse(_dirtyNotified);
    }

    [TestMethod]
    public void SelectedListElement_WhenChanged_DoesNotNotifyDirty()
    {
        _beatEditor.LoadBeats(_problemModel, _storyModel);
        var scene = _storyModel.StoryElements.Scenes.First();
        _dirtyNotified = false;

        _beatEditor.SelectedListElement = scene;

        Assert.IsFalse(_dirtyNotified);
    }

    [TestMethod]
    public void SelectedElementSource_WhenChanged_DoesNotNotifyDirty()
    {
        _beatEditor.LoadBeats(_problemModel, _storyModel);
        _dirtyNotified = false;

        _beatEditor.SelectedElementSource = "Problem";

        Assert.IsFalse(_dirtyNotified);
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
