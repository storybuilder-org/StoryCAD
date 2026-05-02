using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCADLib.Models;
using StoryCADLib.Models.Tools;
using StoryCADLib.Services.API;
using StoryCADLib.Services.Outline;
using StoryCADLib.ViewModels;

#nullable disable

namespace StoryCADTests.Services.API;

[TestClass]
public class StoryCADApiMoveElementIndexTests
{
    private readonly StoryCADApi _api = new(
        Ioc.Default.GetRequiredService<OutlineService>(),
        Ioc.Default.GetRequiredService<ListData>(),
        Ioc.Default.GetRequiredService<ControlData>(),
        Ioc.Default.GetRequiredService<ToolsData>());

    private async Task<Guid> CreateOutlineAndGetOverviewGuid()
    {
        var result = await _api.CreateEmptyOutline("MoveIndex Test", "Test Author", "0");
        Assert.IsTrue(result.IsSuccess, $"Outline creation must succeed: {result.ErrorMessage}");
        return _api.CurrentModel.StoryElements
            .First(e => e.ElementType == StoryItemType.StoryOverview).Uuid;
    }

    [TestMethod]
    public async Task MoveElement_WhenIndexProvided_ReorderInSameParent_PlacesAtIndex()
    {
        var overviewGuid = await CreateOutlineAndGetOverviewGuid();

        var r0 = _api.AddElement(StoryItemType.Scene, overviewGuid.ToString(), "Child0");
        var r1 = _api.AddElement(StoryItemType.Scene, overviewGuid.ToString(), "Child1");
        var r2 = _api.AddElement(StoryItemType.Scene, overviewGuid.ToString(), "Child2");
        Assert.IsTrue(r0.IsSuccess && r1.IsSuccess && r2.IsSuccess);
        var child0Guid = r0.Payload;
        var child2Guid = r2.Payload;

        var result = _api.MoveElement(child2Guid, overviewGuid, 0);

        Assert.IsTrue(result.IsSuccess, $"MoveElement must succeed: {result.ErrorMessage}");
        var overviewNode = _api.CurrentModel.StoryElements
            .First(e => e.Uuid == overviewGuid).Node;
        Assert.AreEqual(child2Guid, overviewNode.Children[0].Uuid);
        Assert.AreEqual(child0Guid, overviewNode.Children[1].Uuid);
    }

    [TestMethod]
    public async Task MoveElement_WhenIndexProvided_ReParentWithPosition_LandsAtIndex()
    {
        var overviewGuid = await CreateOutlineAndGetOverviewGuid();

        var folderAResult = _api.AddElement(StoryItemType.Folder, overviewGuid.ToString(), "FolderA");
        var folderBResult = _api.AddElement(StoryItemType.Folder, overviewGuid.ToString(), "FolderB");
        Assert.IsTrue(folderAResult.IsSuccess && folderBResult.IsSuccess);
        var folderAGuid = folderAResult.Payload;
        var folderBGuid = folderBResult.Payload;

        var b0 = _api.AddElement(StoryItemType.Scene, folderBGuid.ToString(), "B-Child0");
        var b1 = _api.AddElement(StoryItemType.Scene, folderBGuid.ToString(), "B-Child1");
        Assert.IsTrue(b0.IsSuccess && b1.IsSuccess);

        var movedResult = _api.AddElement(StoryItemType.Scene, folderAGuid.ToString(), "ToMove");
        Assert.IsTrue(movedResult.IsSuccess);
        var movedGuid = movedResult.Payload;

        var result = _api.MoveElement(movedGuid, folderBGuid, 1);

        Assert.IsTrue(result.IsSuccess, $"MoveElement must succeed: {result.ErrorMessage}");
        var folderBNode = _api.CurrentModel.StoryElements
            .First(e => e.Uuid == folderBGuid).Node;
        Assert.AreEqual(3, folderBNode.Children.Count);
        Assert.AreEqual(movedGuid, folderBNode.Children[1].Uuid);
    }

    [TestMethod]
    public async Task MoveElement_WhenIndexOutOfRange_ReturnsFailure()
    {
        var overviewGuid = await CreateOutlineAndGetOverviewGuid();

        var c0 = _api.AddElement(StoryItemType.Scene, overviewGuid.ToString(), "Child0");
        var c1 = _api.AddElement(StoryItemType.Scene, overviewGuid.ToString(), "Child1");
        Assert.IsTrue(c0.IsSuccess && c1.IsSuccess);
        var elementGuid = c0.Payload;

        var overviewNode = _api.CurrentModel.StoryElements.First(e => e.Uuid == overviewGuid).Node;
        var originalParent = _api.CurrentModel.StoryElements.First(e => e.Uuid == elementGuid).Node.Parent;
        int originalChildCount = overviewNode.Children.Count;

        var result = _api.MoveElement(elementGuid, overviewGuid, 99);

        Assert.IsFalse(result.IsSuccess, "Out-of-range index must return IsSuccess=false.");
        Assert.IsTrue(result.ErrorMessage.Contains("out of range", StringComparison.OrdinalIgnoreCase),
            $"ErrorMessage must contain 'out of range' but was: {result.ErrorMessage}");

        // Tree state must be unchanged on failure -- no orphan.
        var elementAfter = _api.CurrentModel.StoryElements.First(e => e.Uuid == elementGuid).Node;
        Assert.AreSame(originalParent, elementAfter.Parent, "Failed move must not change parent.");
        Assert.AreEqual(originalChildCount, overviewNode.Children.Count, "Failed move must not change child count.");
        Assert.IsTrue(overviewNode.Children.Contains(elementAfter), "Element must still be in original parent's Children.");
    }

    [TestMethod]
    public async Task MoveElement_WhenIndexOmitted_AppendsAsCurrent()
    {
        var overviewGuid = await CreateOutlineAndGetOverviewGuid();

        var folderAResult = _api.AddElement(StoryItemType.Folder, overviewGuid.ToString(), "FolderA");
        var folderBResult = _api.AddElement(StoryItemType.Folder, overviewGuid.ToString(), "FolderB");
        Assert.IsTrue(folderAResult.IsSuccess && folderBResult.IsSuccess);
        var folderAGuid = folderAResult.Payload;
        var folderBGuid = folderBResult.Payload;

        _api.AddElement(StoryItemType.Scene, folderBGuid.ToString(), "B-Child0");
        _api.AddElement(StoryItemType.Scene, folderBGuid.ToString(), "B-Child1");

        var movedResult = _api.AddElement(StoryItemType.Scene, folderAGuid.ToString(), "ToMove");
        Assert.IsTrue(movedResult.IsSuccess);
        var movedGuid = movedResult.Payload;

        var result = _api.MoveElement(movedGuid, folderBGuid, null);

        Assert.IsTrue(result.IsSuccess, $"MoveElement must succeed: {result.ErrorMessage}");
        var folderBNode = _api.CurrentModel.StoryElements
            .First(e => e.Uuid == folderBGuid).Node;
        Assert.AreEqual(3, folderBNode.Children.Count);
        Assert.AreEqual(movedGuid, folderBNode.Children[2].Uuid);
    }
}
