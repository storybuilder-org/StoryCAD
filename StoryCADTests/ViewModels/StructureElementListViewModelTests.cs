using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using StoryCADLib.Models;
using StoryCADLib.Services.Messages;
using StoryCADLib.Services.Outline;
using StoryCADLib.ViewModels;
using StoryCADLib.ViewModels.SubViewModels;

#nullable disable

namespace StoryCADTests.ViewModels;

/// <summary>
///     Tests for the list of Scenes and Problems that the Structure tab and the beat tree window share (issue #1546).
/// </summary>
[TestClass]
public class StructureElementListViewModelTests
{
    private OutlineService _outlineService;
    private StoryModel _model;
    private StoryNodeItem _overview;
    private ProblemModel _problem;
    private StructureElementListViewModel _list;

    [TestInitialize]
    public async Task TestInitialize()
    {
        _outlineService = Ioc.Default.GetRequiredService<OutlineService>();
        _model = await _outlineService.CreateModel("Element List", "Author", 0);
        _overview = _model.ExplorerView.First();
        _problem = (ProblemModel)_outlineService.AddStoryElement(_model, StoryItemType.Problem, _overview);
        _outlineService.CreateBeat(_problem, "Beat", "Description");
        _list = new StructureElementListViewModel();
    }

    private SceneModel NewScene(string name)
    {
        var scene = (SceneModel)_outlineService.AddStoryElement(_model, StoryItemType.Scene, _overview);
        scene.Name = name;
        return scene;
    }

    [TestMethod]
    public void Load_SetsUseForEachItem()
    {
        var notUsed = NewScene("Not used");
        var onNoBeat = NewScene("On no beat");
        var onBeat = NewScene("On a beat");
        _outlineService.AddStructureElement(_model, _problem, onNoBeat.Uuid);
        _outlineService.AssignElementToBeat(_model, _problem, 0, onBeat.Uuid);

        _list.Load(_model);

        Assert.AreEqual(3, _list.Scenes.Count);
        Assert.AreEqual(StructureUse.NotUsed, _list.Scenes.Single(i => i.Element == notUsed).Use);
        Assert.AreEqual(StructureUse.OnNoBeat, _list.Scenes.Single(i => i.Element == onNoBeat).Use);
        Assert.AreEqual(StructureUse.OnBeat, _list.Scenes.Single(i => i.Element == onBeat).Use);
        Assert.AreEqual(string.Empty, _list.Scenes.Single(i => i.Element == notUsed).UseText);
        Assert.AreEqual("on no beat", _list.Scenes.Single(i => i.Element == onNoBeat).UseText);
        Assert.AreEqual("on a beat", _list.Scenes.Single(i => i.Element == onBeat).UseText);
        Assert.AreSame(_list.Scenes, _list.CurrentElementSource);
    }

    [TestMethod]
    public void Load_AfterNewElement_HasNewElement()
    {
        _list.Load(_model);
        Assert.AreEqual(0, _list.Scenes.Count);

        var scene = NewScene("Made later");
        _list.Load(_model);

        Assert.IsTrue(_list.Scenes.Any(i => i.Element == scene));
    }

    [TestMethod]
    public void Load_OmitsThePlaceholderAndTheTrash()
    {
        var kept = NewScene("Kept");
        var trashed = NewScene("Trashed");
        _outlineService.MoveToTrash(trashed, _model);

        _list.Load(_model);

        Assert.IsTrue(_list.Scenes.Any(i => i.Element == kept));
        Assert.IsFalse(_list.Scenes.Any(i => i.Element == trashed));
        Assert.IsFalse(_list.Scenes.Concat(_list.Problems).Any(i => i.Element.Uuid == Guid.Empty));
        Assert.IsTrue(_list.Problems.Any(i => i.Element == _problem));
    }

    [TestMethod]
    public void Load_KeepsTheSelection()
    {
        var scene = NewScene("Selected");
        _list.Load(_model);
        _list.SelectedListElement = _list.Scenes.Single(i => i.Element == scene);

        _list.Load(_model);

        Assert.IsNotNull(_list.SelectedListElement);
        Assert.AreSame(scene, _list.SelectedListElement.Element);
    }

    [TestMethod]
    public void StructureChangedMessage_RefreshesUse()
    {
        var scene = NewScene("Scene");
        _list.Load(_model);
        var item = _list.Scenes.Single(i => i.Element == scene);
        Assert.AreEqual(StructureUse.NotUsed, item.Use);

        _outlineService.AssignElementToBeat(_model, _problem, 0, scene.Uuid);
        WeakReferenceMessenger.Default.Send(new StructureChangedMessage(_problem.Uuid, this));

        Assert.AreEqual(StructureUse.OnBeat, item.Use);
    }

    [TestMethod]
    public void SelectedElementSource_Problem_ShowsTheProblems()
    {
        _list.Load(_model);

        _list.SelectedElementSource = "Problem";

        Assert.AreSame(_list.Problems, _list.CurrentElementSource);
    }
}
