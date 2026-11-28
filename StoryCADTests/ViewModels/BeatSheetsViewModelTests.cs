using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using StoryCADLib.Services.Messages;
using StoryCADLib.Models;
using StoryCADLib.Services;
using StoryCADLib.Services.Outline;
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
        Assert.AreEqual("Scene", _beatEditor.ElementList.SelectedElementSource);
    }

    [TestMethod]
    public void Constructor_ShouldExposeElementSourceList()
    {
        Assert.IsNotNull(_beatEditor.ElementList.ElementSource);
        Assert.AreEqual(2, _beatEditor.ElementList.ElementSource.Count);
        Assert.IsTrue(_beatEditor.ElementList.ElementSource.Contains("Scene"));
        Assert.IsTrue(_beatEditor.ElementList.ElementSource.Contains("Problem"));
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
        // Arrange — Save The Cat loads 15 template beats
        _beatEditor.StructureModelTitle = "Save The Cat";
        Assert.AreEqual(15, _beatEditor.StructureBeats.Count);

        // Act
        _beatEditor.CreateBeatCommand.Execute(null);

        // Assert
        Assert.AreEqual(16, _beatEditor.StructureBeats.Count);
        Assert.AreEqual("New Beat", _beatEditor.StructureBeats[15].Title);
        Assert.AreEqual("Describe your beat here", _beatEditor.StructureBeats[15].Description);
    }

    [TestMethod]
    public void CreateBeatCommand_MultipleCalls_ShouldAddMultipleBeats()
    {
        _beatEditor.StructureModelTitle = "Save The Cat";
        _beatEditor.CreateBeatCommand.Execute(null);
        _beatEditor.CreateBeatCommand.Execute(null);

        Assert.AreEqual(17, _beatEditor.StructureBeats.Count);
    }

    [TestMethod]
    public void CreateBeatCommand_WithBeatSelected_InsertsAfterSelection()
    {
        // Arrange — Save The Cat loads 15 template beats; select the first one
        _beatEditor.StructureModelTitle = "Save The Cat";
        var firstBeatTitle = _beatEditor.StructureBeats[0].Title;
        var secondBeatTitle = _beatEditor.StructureBeats[1].Title;
        _beatEditor.SelectedBeat = _beatEditor.StructureBeats[0];
        _beatEditor.SelectedBeatIndex = 0;

        // Act
        _beatEditor.CreateBeatCommand.Execute(null);

        // Assert — new beat inserted after first template beat
        Assert.AreEqual(16, _beatEditor.StructureBeats.Count);
        Assert.AreEqual(firstBeatTitle, _beatEditor.StructureBeats[0].Title);
        Assert.AreEqual("New Beat", _beatEditor.StructureBeats[1].Title);
        Assert.AreEqual(secondBeatTitle, _beatEditor.StructureBeats[2].Title);
        Assert.AreEqual(_beatEditor.StructureBeats[1], _beatEditor.SelectedBeat);
    }

    [TestMethod]
    public void DeleteBeatCommand_WithNoSelection_DoesNotRemove()
    {
        _beatEditor.LoadBeats(_problemModel, _storyModel);
        _beatEditor.StructureModelTitle = "Save The Cat";
        _beatEditor.CreateBeatCommand.Execute(null);
        Assert.AreEqual(16, _beatEditor.StructureBeats.Count);

        // No selection
        _beatEditor.SelectedBeat = null;
        _beatEditor.DeleteBeatCommand.Execute(null);

        Assert.AreEqual(16, _beatEditor.StructureBeats.Count);
    }

    [TestMethod]
    public void DeleteBeatCommand_WithSelection_RemovesBeat()
    {
        _beatEditor.LoadBeats(_problemModel, _storyModel);
        _beatEditor.StructureModelTitle = "Save The Cat";
        _beatEditor.CreateBeatCommand.Execute(null);
        Assert.AreEqual(16, _beatEditor.StructureBeats.Count);

        _beatEditor.SelectedBeat = _beatEditor.StructureBeats[0];
        _beatEditor.SelectedBeatIndex = 0;
        _beatEditor.DeleteBeatCommand.Execute(null);

        Assert.AreEqual(15, _beatEditor.StructureBeats.Count);
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
        _beatEditor.ElementList.Load(_storyModel);
        _beatEditor.ElementList.SelectedElementSource = "Problem"; // start from Problem

        // Act
        _beatEditor.ElementList.SelectedElementSource = "Scene";

        // Assert
        Assert.AreSame(_beatEditor.ElementList.Scenes, _beatEditor.ElementList.CurrentElementSource);
    }

    [TestMethod]
    public void SelectedElementSource_WhenSetToProblem_SetsCurrentElementSourceToProblems()
    {
        // Arrange
        _beatEditor.ElementList.Load(_storyModel);

        // Act
        _beatEditor.ElementList.SelectedElementSource = "Problem";

        // Assert
        Assert.AreSame(_beatEditor.ElementList.Problems, _beatEditor.ElementList.CurrentElementSource);
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
        _beatEditor.ElementList.SelectedListElement = new StructureElementItem(scene);

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
        _problemModel.StructureTitle = "Save The Cat";
        _problemModel.StructureBeats = new ObservableCollection<StructureBeat>();

        _beatEditor.LoadBeats(_problemModel, _storyModel);

        Assert.AreEqual("Save The Cat", _beatEditor.StructureModelTitle);
    }

    /// <summary>
    ///     Issue #1641: Collaborator Accept reloads the open Problem page through LoadBeats. The
    ///     Beat Sheet ComboBox only updates on PropertyChanged, so a reload that changes the title
    ///     must raise it, without marking the outline dirty or applying the sheet again.
    /// </summary>
    [TestMethod]
    public void LoadBeats_Reload_RaisesStructureModelTitleChangedWithoutDirty()
    {
        _problemModel.StructureTitle = "";
        _problemModel.StructureBeats = new ObservableCollection<StructureBeat>();
        _beatEditor.LoadBeats(_problemModel, _storyModel);
        _dirtyNotified = false;

        var raised = new List<string>();
        _beatEditor.PropertyChanged += (_, e) => raised.Add(e.PropertyName);
        _problemModel.StructureTitle = "Three Act Play";

        _beatEditor.LoadBeats(_problemModel, _storyModel);

        Assert.AreEqual("Three Act Play", _beatEditor.StructureModelTitle);
        CollectionAssert.Contains(raised, nameof(BeatSheetsViewModel.StructureModelTitle));
        Assert.IsFalse(_dirtyNotified, "A reload must not mark the outline dirty");
        Assert.AreEqual(0, _beatEditor.StructureBeats.Count, "A reload must not apply the beat sheet");
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
        _beatEditor.ElementList.SelectedListElement = new StructureElementItem(_problemModel);

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
        problemA.StructureElements.Add(problemB.Uuid);
        problemB.BoundStructure = problemA.Uuid;

        // Our test problem (_problemModel) has a beat we want to assign B to
        _beatEditor.LoadBeats(_problemModel, _storyModel);
        var beat = CreateTestBeat("Beat on test problem");
        _beatEditor.StructureBeats.Add(beat);
        _beatEditor.SelectedBeat = beat;
        _beatEditor.SelectedBeatIndex = 0;
        _beatEditor.ElementList.SelectedListElement = new StructureElementItem(problemB);

        await _beatEditor.AssignBeatAsync();

        Assert.AreEqual(Guid.Empty, beatOnA.Guid, "Old parent's beat should be cleared");
        Assert.AreEqual(problemB.Uuid, beat.Guid, "New beat should point to problem B");
        Assert.AreEqual(_problemModel.Uuid, problemB.BoundStructure, "BoundStructure should point to new parent");
    }

    [TestMethod]
    public async Task AssignBeatAsync_Scene_KeepsBeatSelectedAndShowsDescriptions()
    {
        _beatEditor.LoadBeats(_problemModel, _storyModel);
        var beat = new StructureBeat("Opening", "The hero is stuck");
        _beatEditor.StructureBeats.Add(beat);
        _beatEditor.SelectedBeat = beat;
        _beatEditor.SelectedBeatIndex = 0;
        var scene = _storyModel.StoryElements.Scenes.First();
        scene.Description = "Stakeout goes nowhere";
        _beatEditor.ElementList.SelectedListElement = new StructureElementItem(scene);

        await _beatEditor.AssignBeatAsync();

        Assert.AreEqual(scene.Uuid, beat.Guid, "Beat should be bound to the scene");
        Assert.AreSame(beat, _beatEditor.SelectedBeat, "Assigned beat should stay selected");
        Assert.AreEqual(0, _beatEditor.SelectedBeatIndex);
        Assert.AreEqual("The hero is stuck", _beatEditor.SelectedBeatDescription);
        Assert.AreEqual("Stakeout goes nowhere", _beatEditor.CurrentElementDescription);
    }

    [TestMethod]
    public void ElementIcon_Scene_ReturnsWorldSymbol()
    {
        var scene = new SceneModel("Icon Test Scene", _storyModel, null);
        _beatEditor.LoadBeats(_problemModel, _storyModel);
        var beat = CreateTestBeat("Test Beat");

        beat.Guid = scene.Uuid;

        Assert.AreEqual(Symbol.AllApps, beat.ElementIcon);
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

        Assert.AreEqual(Symbol.ClearSelection, beat.ElementIcon);
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
        Assert.AreSame(beat, _beatEditor.SelectedBeat, "The beat stays selected (issue #1546)");
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

        _beatEditor.StructureModelTitle = "Save The Cat";

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

        _beatEditor.ElementList.SelectedListElement = new StructureElementItem(scene);

        Assert.IsFalse(_dirtyNotified);
    }

    [TestMethod]
    public void LoadBeats_ElementLists_OmitNonePlaceholder()
    {
        _beatEditor.LoadBeats(_problemModel, _storyModel);

        // The model lists start with a "(none)" placeholder (Guid.Empty) for drop-downs
        Assert.IsTrue(_storyModel.StoryElements.Scenes.Any(e => e.Uuid == Guid.Empty));
        Assert.IsTrue(_storyModel.StoryElements.Problems.Any(e => e.Uuid == Guid.Empty));

        Assert.IsFalse(_beatEditor.ElementList.Scenes.Any(e => e.Element.Uuid == Guid.Empty));
        Assert.IsFalse(_beatEditor.ElementList.Problems.Any(e => e.Element.Uuid == Guid.Empty));
        Assert.IsTrue(_beatEditor.ElementList.Scenes.Any(e => e.Name == "Test Scene"));
        Assert.IsTrue(_beatEditor.ElementList.Problems.Any(e => e.Name == "Test Story Problem"));
        Assert.IsFalse(_beatEditor.ElementList.CurrentElementSource.Any(e => e.Name == "(none)"));
    }

    [TestMethod]
    public void SelectedElementSource_WhenChanged_DoesNotNotifyDirty()
    {
        _beatEditor.LoadBeats(_problemModel, _storyModel);
        _dirtyNotified = false;

        _beatEditor.ElementList.SelectedElementSource = "Problem";

        Assert.IsFalse(_dirtyNotified);
    }

    #endregion

    #region Last beat selection (#1422)

    /// <summary>
    ///     #1422: selecting the last beat (Denouement) of Three Act Play hung the app. Selects the
    ///     last beat of every built-in beat sheet in Tools.json the way the Beats list does
    ///     (SelectedIndex and SelectedItem), then runs the commands that use the index.
    /// </summary>
    [TestMethod]
    public void SelectedBeat_LastBeatOfEveryBuiltInBeatSheet_CompletesAndShowsItsDescription()
    {
        var sheets = Ioc.Default.GetRequiredService<StoryCADLib.Models.Tools.ToolsData>().BeatSheetSource
            .Where(s => s.PlotPatternScenes.Count > 0)
            .ToList();
        Assert.IsTrue(sheets.Any(s => s.PlotPatternName == "Three Act Play"), "Three Act Play is a built-in sheet");

        foreach (var sheet in sheets)
        {
            var name = sheet.PlotPatternName;
            var work = Task.Run(() =>
            {
                var problem = CreateTestProblemModel(_storyModel);
                var editor = new BeatSheetsViewModel(() => { });
                editor.LoadBeats(problem, _storyModel);
                editor.StructureModelTitle = name;

                var last = editor.StructureBeats.Count - 1;
                Assert.AreEqual(sheet.PlotPatternScenes.Count, editor.StructureBeats.Count, name);
                var lastBeat = editor.StructureBeats[last];

                editor.SelectedBeatIndex = last;
                editor.SelectedBeat = lastBeat;

                Assert.AreEqual(sheet.PlotPatternScenes[^1].SceneTitle, lastBeat.Title, name);
                Assert.AreEqual(lastBeat.Description, editor.SelectedBeatDescription, name);
                Assert.AreEqual("Unassigned", lastBeat.ElementName, name);
                Assert.IsNull(editor.CurrentElementDescription, name);

                editor.MoveDownCommand.Execute(null); // already last: no move
                Assert.AreSame(lastBeat, editor.StructureBeats[last], name);
                editor.MoveUpCommand.Execute(null);
                Assert.AreSame(lastBeat, editor.StructureBeats[last - (last > 0 ? 1 : 0)], name);
            });

            Assert.IsTrue(work.Wait(TimeSpan.FromSeconds(10)), $"Selecting the last beat of {name} did not finish");
        }
    }

    #endregion

    #region Structure elements (issue #1546)

    private SceneModel AddScene(string name)
    {
        return new SceneModel(name, _storyModel, null); // the constructor adds it to the model
    }

    private ProblemModel AddProblem(string name)
    {
        return new ProblemModel(name, _storyModel, null);
    }

    private OutlineService Outline => Ioc.Default.GetRequiredService<OutlineService>();

    [TestMethod]
    public void LoadBeats_FillsHeaderAndNoBeatList()
    {
        var scene = AddScene("Waiting scene");
        _problemModel.ProblemCategory = "Subplot";
        Outline.AddStructureElement(_storyModel, _problemModel, scene.Uuid);

        _beatEditor.LoadBeats(_problemModel, _storyModel);

        Assert.AreEqual("Test Problem", _beatEditor.ProblemName);
        Assert.AreEqual("Subplot", _beatEditor.ProblemCategory);
        CollectionAssert.AreEqual(new[] { scene }, _beatEditor.ElementsOnNoBeat);
        Assert.AreSame(_problemModel.StructureBeats, _beatEditor.StructureBeats);
    }

    [TestMethod]
    public void ApplyBeatSheet_KeepsMembersInBeatSequence()
    {
        var first = AddScene("First");
        var second = AddScene("Second");
        _beatEditor.LoadBeats(_problemModel, _storyModel);
        _beatEditor.StructureModelTitle = "Save The Cat";
        var collection = _problemModel.StructureBeats;
        Outline.AssignElementToBeat(_storyModel, _problemModel, 3, second.Uuid);
        Outline.AssignElementToBeat(_storyModel, _problemModel, 1, first.Uuid);

        _beatEditor.StructureModelTitle = "Hero's Journey";

        Assert.AreEqual("Hero's Journey", _problemModel.StructureTitle);
        Assert.AreSame(collection, _problemModel.StructureBeats);
        Assert.AreSame(collection, _beatEditor.StructureBeats);
        Assert.AreEqual(12, _beatEditor.StructureBeats.Count);
        Assert.IsTrue(_beatEditor.StructureBeats.All(b => b.Guid == Guid.Empty));
        CollectionAssert.AreEqual(new[] { first, second }, _beatEditor.ElementsOnNoBeat);
    }

    [TestMethod]
    public void ApplyBeatSheet_ChildProblemKeepsParent()
    {
        var child = AddProblem("Child");
        _beatEditor.LoadBeats(_problemModel, _storyModel);
        _beatEditor.StructureModelTitle = "Save The Cat";
        Outline.AssignElementToBeat(_storyModel, _problemModel, 0, child.Uuid);

        _beatEditor.StructureModelTitle = "Hero's Journey";

        Assert.AreEqual(_problemModel.Uuid, child.BoundStructure);
        CollectionAssert.Contains(_problemModel.StructureElements, child.Uuid);
    }

    [TestMethod]
    public void LoadBeatSheet_KeepsMembers()
    {
        var scene = AddScene("Scene");
        _beatEditor.LoadBeats(_problemModel, _storyModel);
        _beatEditor.StructureModelTitle = "Save The Cat";
        Outline.AssignElementToBeat(_storyModel, _problemModel, 0, scene.Uuid);
        var saved = new SavedBeatsheet
        {
            Description = "From a file",
            Beats = new List<StructureBeat> { new("File beat 1", "D1"), new("File beat 2", "D2") }
        };

        _beatEditor.ApplyLoadedBeatSheet(saved);

        Assert.AreEqual("Custom Beat Sheet", _problemModel.StructureTitle);
        Assert.AreEqual("From a file", _problemModel.StructureDescription);
        Assert.AreEqual(2, _problemModel.StructureBeats.Count);
        CollectionAssert.AreEqual(new[] { scene }, _beatEditor.ElementsOnNoBeat);
    }

    [TestMethod]
    public void LoadBeatSheet_ChildProblemKeepsParent()
    {
        var child = AddProblem("Child");
        _beatEditor.LoadBeats(_problemModel, _storyModel);
        _beatEditor.StructureModelTitle = "Save The Cat";
        Outline.AssignElementToBeat(_storyModel, _problemModel, 0, child.Uuid);

        _beatEditor.ApplyLoadedBeatSheet(new SavedBeatsheet
        {
            Description = "From a file",
            Beats = new List<StructureBeat> { new("File beat", "D") }
        });

        Assert.AreEqual(_problemModel.Uuid, child.BoundStructure);
        CollectionAssert.AreEqual(new StoryElement[] { child }, _beatEditor.ElementsOnNoBeat);
    }

    [TestMethod]
    public void AddElementCommand_AddsMember()
    {
        var scene = AddScene("Scene");
        _beatEditor.LoadBeats(_problemModel, _storyModel);
        _beatEditor.ElementList.SelectedListElement = new StructureElementItem(scene);
        _dirtyNotified = false;

        _beatEditor.AddElementCommand.Execute(null);

        CollectionAssert.AreEqual(new[] { scene.Uuid }, _problemModel.StructureElements);
        CollectionAssert.AreEqual(new[] { scene }, _beatEditor.ElementsOnNoBeat);
        Assert.IsTrue(_dirtyNotified);
    }

    [TestMethod]
    public void RemoveElementCommand_RemovesMember()
    {
        var scene = AddScene("Scene");
        Outline.AddStructureElement(_storyModel, _problemModel, scene.Uuid);
        _beatEditor.LoadBeats(_problemModel, _storyModel);
        _beatEditor.SelectedNoBeatElement = scene;

        _beatEditor.RemoveElementCommand.Execute(null);

        Assert.AreEqual(0, _problemModel.StructureElements.Count);
        Assert.AreEqual(0, _beatEditor.ElementsOnNoBeat.Count);
    }

    [TestMethod]
    public async Task PlaceOnBeatAsync_FromBeat_Moves()
    {
        var child = AddProblem("Child");
        _beatEditor.LoadBeats(_problemModel, _storyModel);
        _beatEditor.StructureModelTitle = "Save The Cat";
        await _beatEditor.PlaceOnBeatAsync(child.Uuid, 0, -1);

        await _beatEditor.PlaceOnBeatAsync(child.Uuid, 2, 0);

        Assert.AreEqual(Guid.Empty, _beatEditor.StructureBeats[0].Guid);
        Assert.AreEqual(child.Uuid, _beatEditor.StructureBeats[2].Guid);
        Assert.AreEqual(0, _beatEditor.ElementsOnNoBeat.Count);
    }

    [TestMethod]
    public async Task PlaceOnBeatAsync_SceneOnSecondBeatHeadless_IsOnBothBeats()
    {
        var scene = AddScene("Scene");
        _beatEditor.LoadBeats(_problemModel, _storyModel);
        _beatEditor.StructureModelTitle = "Save The Cat";
        await _beatEditor.PlaceOnBeatAsync(scene.Uuid, 0, -1);

        await _beatEditor.PlaceOnBeatAsync(scene.Uuid, 2, -1);

        Assert.AreEqual(scene.Uuid, _beatEditor.StructureBeats[0].Guid);
        Assert.AreEqual(scene.Uuid, _beatEditor.StructureBeats[2].Guid);
    }

    [TestMethod]
    public async Task UnbindElement_KeepsElementOnNoBeatAndKeepsTheBeatSelected()
    {
        var scene = AddScene("Scene");
        _beatEditor.LoadBeats(_problemModel, _storyModel);
        _beatEditor.StructureModelTitle = "Save The Cat";
        await _beatEditor.PlaceOnBeatAsync(scene.Uuid, 1, -1);
        var beat = _beatEditor.StructureBeats[1];
        _beatEditor.SelectedBeat = beat;
        _beatEditor.SelectedBeatIndex = 1;

        _beatEditor.UnbindElement();

        Assert.AreEqual(Guid.Empty, beat.Guid);
        Assert.AreSame(beat, _beatEditor.SelectedBeat);
        CollectionAssert.AreEqual(new[] { scene }, _beatEditor.ElementsOnNoBeat);
    }

    [TestMethod]
    public async Task DeleteBeatCommand_Headless_KeepsElementOnNoBeat()
    {
        var scene = AddScene("Scene");
        _beatEditor.LoadBeats(_problemModel, _storyModel);
        _beatEditor.StructureModelTitle = "Save The Cat";
        await _beatEditor.PlaceOnBeatAsync(scene.Uuid, 0, -1);
        _beatEditor.SelectedBeat = _beatEditor.StructureBeats[0];
        _beatEditor.SelectedBeatIndex = 0;

        _beatEditor.DeleteBeatCommand.Execute(null);

        Assert.AreEqual(14, _beatEditor.StructureBeats.Count);
        CollectionAssert.AreEqual(new[] { scene }, _beatEditor.ElementsOnNoBeat);
    }

    [TestMethod]
    public void SelectedNoBeatElement_WhenSet_ClearsSelectedBeat()
    {
        var scene = AddScene("Scene");
        Outline.AddStructureElement(_storyModel, _problemModel, scene.Uuid);
        _beatEditor.LoadBeats(_problemModel, _storyModel);
        _beatEditor.StructureModelTitle = "Save The Cat";
        _beatEditor.SelectedBeat = _beatEditor.StructureBeats[0];

        _beatEditor.SelectedNoBeatElement = scene;

        Assert.IsNull(_beatEditor.SelectedBeat);

        _beatEditor.SelectedBeat = _beatEditor.StructureBeats[0];

        Assert.IsNull(_beatEditor.SelectedNoBeatElement);
    }

    [TestMethod]
    public async Task StructureChangedMessage_OtherSource_Refreshes()
    {
        var scene = AddScene("Scene");
        _beatEditor.LoadBeats(_problemModel, _storyModel);
        var other = new BeatSheetsViewModel(() => { });
        other.LoadBeats(_problemModel, _storyModel);
        _dirtyNotified = false;

        await other.AddElementAsync(scene.Uuid);
        other.StructureModelTitle = "Save The Cat";

        CollectionAssert.AreEqual(new[] { scene }, _beatEditor.ElementsOnNoBeat);
        Assert.AreEqual("Save The Cat", _beatEditor.StructureModelTitle);
        Assert.AreEqual(15, _beatEditor.StructureBeats.Count);
        Assert.IsFalse(_dirtyNotified, "A refresh from another screen is not an edit of this screen");
    }

    [TestMethod]
    public void Refresh_DoesNotSendMessage()
    {
        _beatEditor.LoadBeats(_problemModel, _storyModel);
        var recipient = new object();
        var received = 0;
        WeakReferenceMessenger.Default.Register<StructureChangedMessage>(recipient, (_, _) => received++);
        _dirtyNotified = false;

        _problemModel.StructureDescription = "Changed by another screen";
        _beatEditor.Refresh();

        WeakReferenceMessenger.Default.UnregisterAll(recipient);
        Assert.AreEqual("Changed by another screen", _beatEditor.StructureDescription);
        Assert.AreEqual(0, received);
        Assert.IsFalse(_dirtyNotified);
    }

    [TestMethod]
    public void ElementSummary_SceneWithRichText_IsPlainText()
    {
        var scene = AddScene("Scene");
        scene.Description = @"{\rtf1\ansi{\fonttbl{\f0 Segoe UI;}}\f0\fs20 The marlin takes the bait.\par}";
        var beat = new StructureBeat("Beat", "What the beat needs");

        Assert.AreEqual(string.Empty, beat.ElementSummary, "A beat with no element has no summary");

        beat.Guid = scene.Uuid;

        Assert.AreEqual("The marlin takes the bait.", beat.ElementSummary);
    }

    [TestMethod]
    public void Refresh_ElementRenamedOnAnotherForm_CardReadsItAgain()
    {
        var scene = AddScene("Old name");
        _beatEditor.LoadBeats(_problemModel, _storyModel);
        _beatEditor.StructureModelTitle = "Save The Cat";
        Outline.AssignElementToBeat(_storyModel, _problemModel, 0, scene.Uuid);
        var beat = _beatEditor.StructureBeats[0];
        var raised = new List<string>();
        beat.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        scene.Name = "New name";
        _beatEditor.Refresh();

        CollectionAssert.Contains(raised, nameof(StructureBeat.ElementName));
        CollectionAssert.Contains(raised, nameof(StructureBeat.ElementSummary));
        Assert.AreEqual("New name", beat.ElementName);
    }

    [TestMethod]
    public void SaveBeats_DoesNotWriteBeatsOrParent()
    {
        _beatEditor.LoadBeats(_problemModel, _storyModel);
        var modelBeats = _problemModel.StructureBeats;
        var parent = Guid.NewGuid();
        _problemModel.BoundStructure = parent; // set by another screen after this one loaded

        _beatEditor.SaveBeats(_problemModel);

        Assert.AreEqual(parent, _problemModel.BoundStructure);
        Assert.AreSame(modelBeats, _problemModel.StructureBeats);
    }

    [TestMethod]
    public void StructureDescription_WhenEdited_IsWrittenToTheModelAtOnce()
    {
        _beatEditor.LoadBeats(_problemModel, _storyModel);

        _beatEditor.StructureDescription = "Typed on this screen";

        Assert.AreEqual("Typed on this screen", _problemModel.StructureDescription);
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
