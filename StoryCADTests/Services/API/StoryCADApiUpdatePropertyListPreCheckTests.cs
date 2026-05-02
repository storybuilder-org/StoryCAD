using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCADLib.Models;
using StoryCADLib.Models.StoryWorld;
using StoryCADLib.Models.Tools;
using StoryCADLib.Services.API;
using StoryCADLib.Services.Outline;
using StoryCADLib.ViewModels;

#nullable disable

namespace StoryCADTests.Services.API;

[TestClass]
public class StoryCADApiUpdatePropertyListPreCheckTests
{
    private readonly StoryCADApi _api = new(
        Ioc.Default.GetRequiredService<OutlineService>(),
        Ioc.Default.GetRequiredService<ListData>(),
        Ioc.Default.GetRequiredService<ControlData>(),
        Ioc.Default.GetRequiredService<ToolsData>());

    [TestMethod]
    public async Task UpdateElementProperty_OnListProperty_ReturnsCollectionMethodHint()
    {
        var createResult = await _api.CreateEmptyOutline("Test Outline", "Author", "0");
        Assert.IsTrue(createResult.IsSuccess);

        var overview = _api.CurrentModel.StoryElements
            .First(e => e.ElementType == StoryItemType.StoryOverview);
        var add = _api.AddElement(StoryItemType.StoryWorld, overview.Uuid.ToString(), "World");
        Assert.IsTrue(add.IsSuccess);

        var result = _api.UpdateElementProperty(add.Payload, "PhysicalWorlds",
            new List<PhysicalWorldEntry> { new() { Name = "X" } });

        Assert.IsFalse(result.IsSuccess, "Expected failure when target property is a List<T>.");
        Assert.IsTrue(result.ErrorMessage.Contains("AddCollectionEntry")
                      && result.ErrorMessage.Contains("UpdateCollectionEntry")
                      && result.ErrorMessage.Contains("RemoveCollectionEntry"),
            $"Expected error to point at the collection-entry methods, got: {result.ErrorMessage}");
    }
}
