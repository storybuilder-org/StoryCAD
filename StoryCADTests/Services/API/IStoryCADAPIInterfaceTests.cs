using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCADLib.Models;
using StoryCADLib.Models.Tools;
using StoryCADLib.Services.API;
using StoryCADLib.Services.Collaborator.Contracts;
using StoryCADLib.Services.Outline;
using StoryCADLib.ViewModels;

#nullable disable

namespace StoryCADTests.Services.API;

[TestClass]
public class IStoryCADAPIInterfaceTests
{
    private readonly StoryCADApi _api = new(
        Ioc.Default.GetRequiredService<OutlineService>(),
        Ioc.Default.GetRequiredService<ListData>(),
        Ioc.Default.GetRequiredService<ControlData>(),
        Ioc.Default.GetRequiredService<ToolsData>());

    [TestMethod]
    public void AddElement_BasicOverload_IsOnInterface()
    {
        IStoryCADAPI api = _api;
        var result = api.AddElement(StoryItemType.Scene, Guid.Empty.ToString(), "x");
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void AddElement_PropertiesOverload_IsOnInterface()
    {
        IStoryCADAPI api = _api;
        var result = api.AddElement(StoryItemType.Scene, Guid.Empty.ToString(), "x", new Dictionary<string, object>());
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void GetStoryWorld_IsOnInterface()
    {
        IStoryCADAPI api = _api;
        var result = api.GetStoryWorld();
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task OpenOutline_IsOnInterface()
    {
        IStoryCADAPI api = _api;
        var result = await api.OpenOutline("nonexistent.stbx");
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task DeleteElement_IsOnInterface()
    {
        IStoryCADAPI api = _api;
        var result = await api.DeleteElement(Guid.Empty);
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void AddCastMember_IsOnInterface()
    {
        IStoryCADAPI api = _api;
        var result = api.AddCastMember(Guid.Empty, Guid.Empty);
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void AddRelationship_IsOnInterface()
    {
        IStoryCADAPI api = _api;
        var result = api.AddRelationship(Guid.Empty, Guid.Empty, "desc");
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void MoveElement_IsOnInterface()
    {
        IStoryCADAPI api = _api;
        var result = api.MoveElement(Guid.Empty, Guid.Empty);
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void SearchForText_IsOnInterface()
    {
        IStoryCADAPI api = _api;
        var result = api.SearchForText("x");
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void SearchForReferences_IsOnInterface()
    {
        IStoryCADAPI api = _api;
        var result = api.SearchForReferences(Guid.Empty);
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void RemoveReferences_IsOnInterface()
    {
        IStoryCADAPI api = _api;
        var result = api.RemoveReferences(Guid.Empty);
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void SearchInSubtree_IsOnInterface()
    {
        IStoryCADAPI api = _api;
        var result = api.SearchInSubtree(Guid.Empty, "x");
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task RestoreFromTrash_IsOnInterface()
    {
        IStoryCADAPI api = _api;
        var result = await api.RestoreFromTrash(Guid.Empty);
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task EmptyTrash_IsOnInterface()
    {
        IStoryCADAPI api = _api;
        var result = await api.EmptyTrash();
        Assert.IsNotNull(result);
    }
}
