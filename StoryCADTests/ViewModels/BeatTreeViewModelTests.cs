using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCADLib.Models;
using StoryCADLib.Services.Outline;
using StoryCADLib.ViewModels;

#nullable disable

namespace StoryCADTests.ViewModels;

/// <summary>
///     Tests for the view model of the beat tree window (issue #1546): which Problems get a
///     beat column, in which sequence, and which connectors join them.
/// </summary>
[TestClass]
public class BeatTreeViewModelTests
{
    private OutlineService _outlineService;
    private StoryModel _model;
    private StoryNodeItem _overviewNode;
    private OverviewModel _overview;
    private ProblemModel _story;
    private BeatTreeViewModel _tree;

    [TestInitialize]
    public async Task TestInitialize()
    {
        _outlineService = Ioc.Default.GetRequiredService<OutlineService>();
        _model = await _outlineService.CreateModel("Beat Tree", "Author", 0);
        _overviewNode = _model.ExplorerView.First();
        _overview = _model.StoryElements.OfType<OverviewModel>().First();
        Ioc.Default.GetRequiredService<AppState>().CurrentDocument = new StoryDocument(_model);

        _story = NewProblem("Story Problem", 4);
        _overview.StoryProblem = _story.Uuid;
        _tree = new BeatTreeViewModel();
    }

    private ProblemModel NewProblem(string name, int beats = 0)
    {
        var problem = (ProblemModel)_outlineService.AddStoryElement(_model, StoryItemType.Problem, _overviewNode);
        problem.Name = name;
        for (var i = 0; i < beats; i++)
        {
            _outlineService.CreateBeat(problem, $"Beat {i}", "Description");
        }

        return problem;
    }

    private SceneModel NewScene(string name)
    {
        var scene = (SceneModel)_outlineService.AddStoryElement(_model, StoryItemType.Scene, _overviewNode);
        scene.Name = name;
        return scene;
    }

    private string ColumnNames() => string.Join(", ", _tree.Columns.Select(c => c.ProblemName));

    [TestMethod]
    public void Load_ThreeLevels_ColumnsAndConnectors()
    {
        var late = NewProblem("Late", 2);
        var early = NewProblem("Early", 2);
        var deep = NewProblem("Deep");
        _outlineService.AssignElementToBeat(_model, _story, 3, late.Uuid);
        _outlineService.AssignElementToBeat(_model, _story, 1, early.Uuid);
        _outlineService.AssignElementToBeat(_model, _story, 0, NewScene("A scene").Uuid);
        _outlineService.AssignElementToBeat(_model, early, 1, deep.Uuid);

        _tree.Load(_model);

        // Each Problem comes after the Problem that holds it, in the sequence of the beats.
        Assert.AreEqual("Story Problem, Early, Deep, Late", ColumnNames());
        Assert.AreEqual(string.Empty, _tree.EmptyText);
        Assert.AreEqual(3, _tree.Connectors.Count);
        CollectionAssert.Contains(_tree.Connectors.ToList(), new BeatTreeConnector(_story.Uuid, early.Uuid, 1));
        CollectionAssert.Contains(_tree.Connectors.ToList(), new BeatTreeConnector(early.Uuid, deep.Uuid, 1));
        CollectionAssert.Contains(_tree.Connectors.ToList(), new BeatTreeConnector(_story.Uuid, late.Uuid, 3));
    }

    [TestMethod]
    public void Load_ChildOnNoBeat_HasColumnAndConnector()
    {
        var onBeat = NewProblem("On a beat");
        var waiting = NewProblem("Waiting");
        _outlineService.AddStructureElement(_model, _story, waiting.Uuid);
        _outlineService.AssignElementToBeat(_model, _story, 2, onBeat.Uuid);

        _tree.Load(_model);

        // The Problems on beats come first, then the Problems on no beat.
        Assert.AreEqual("Story Problem, On a beat, Waiting", ColumnNames());
        CollectionAssert.Contains(_tree.Connectors.ToList(), new BeatTreeConnector(_story.Uuid, waiting.Uuid, -1));
    }

    [TestMethod]
    public void Load_Loop_EachProblemOneTime()
    {
        var a = NewProblem("A", 1);
        var b = NewProblem("B", 1);
        _outlineService.AssignElementToBeat(_model, _story, 0, a.Uuid);
        _outlineService.AssignElementToBeat(_model, a, 0, b.Uuid);
        // A loop, written as an old file can hold it: B holds A, and A holds B.
        b.StructureBeats[0].Guid = a.Uuid;
        b.StructureElements.Add(a.Uuid);

        _tree.Load(_model);

        Assert.AreEqual("Story Problem, A, B", ColumnNames());
        Assert.AreEqual(2, _tree.Connectors.Count, "No connector goes back to a beat column");
    }

    [TestMethod]
    public void Load_NoStoryProblem_HasNoColumnsAndSaysWhy()
    {
        _overview.StoryProblem = Guid.Empty;

        _tree.Load(_model);

        Assert.AreEqual(0, _tree.Columns.Count);
        Assert.AreNotEqual(string.Empty, _tree.EmptyText);
    }

    [TestMethod]
    public void Load_StoryProblemInTrash_HasNoColumns()
    {
        _outlineService.MoveToTrash(_story, _model);

        _tree.Load(_model);

        Assert.AreEqual(0, _tree.Columns.Count);
        Assert.AreNotEqual(string.Empty, _tree.EmptyText);
    }

    [TestMethod]
    public void Load_ProblemWithNoParent_HasNoColumn()
    {
        NewProblem("On its own", 3);

        _tree.Load(_model);

        Assert.AreEqual("Story Problem", ColumnNames());
    }

    [TestMethod]
    public void Load_Again_KeepsTheColumnObjects()
    {
        var child = NewProblem("Child");
        _outlineService.AssignElementToBeat(_model, _story, 0, child.Uuid);
        _tree.Load(_model);
        var first = _tree.Columns.ToList();

        _tree.Load(_model);

        CollectionAssert.AreEqual(first, _tree.Columns.ToList());
        Assert.AreSame(_tree.ElementList, _tree.Columns[0].ElementList, "All beat columns share one element list");
    }

    [TestMethod]
    public async Task ChangeInAColumn_NewChildProblem_GetsAColumn()
    {
        var child = NewProblem("Child");
        _tree.Load(_model);
        Assert.AreEqual("Story Problem", ColumnNames());

        // A change made through a beat column sends StructureChangedMessage, and the tree loads again.
        await _tree.Columns[0].PlaceOnBeatAsync(child.Uuid, 2, -1);

        Assert.AreEqual("Story Problem, Child", ColumnNames());
        CollectionAssert.Contains(_tree.Connectors.ToList(), new BeatTreeConnector(_story.Uuid, child.Uuid, 2));
    }

    [TestMethod]
    public async Task ChangeInAColumn_BeatMoved_ConnectorFollows()
    {
        var child = NewProblem("Child");
        _outlineService.AssignElementToBeat(_model, _story, 0, child.Uuid);
        _tree.Load(_model);
        var column = _tree.Columns[0];
        column.SelectedBeat = column.StructureBeats[0];
        column.SelectedBeatIndex = 0;

        column.MoveDownCommand.Execute(null);
        await Task.CompletedTask;

        CollectionAssert.Contains(_tree.Connectors.ToList(), new BeatTreeConnector(_story.Uuid, child.Uuid, 1));
    }

    [TestMethod]
    public void ActiveColumn_AfterLoad_IsTheStoryProblem()
    {
        _tree.Load(_model);

        Assert.AreSame(_tree.Columns[0], _tree.ActiveColumn);
    }
}
