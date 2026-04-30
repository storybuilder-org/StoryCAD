using System.Text.Json;
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
public class StoryCADApiCollectionEntryTests
{
    private readonly StoryCADApi _api = new(
        Ioc.Default.GetRequiredService<OutlineService>(),
        Ioc.Default.GetRequiredService<ListData>(),
        Ioc.Default.GetRequiredService<ControlData>(),
        Ioc.Default.GetRequiredService<ToolsData>());

    private async Task<Guid> CreateOutlineAndGetStoryWorldGuid()
    {
        var createResult = await _api.CreateEmptyOutline("Test Outline", "Author", "0");
        Assert.IsTrue(createResult.IsSuccess, "Expected outline creation to succeed.");
        var overview = _api.CurrentModel.StoryElements
            .First(e => e.ElementType == StoryItemType.StoryOverview);
        var add = _api.AddElement(StoryItemType.StoryWorld, overview.Uuid.ToString(), "World");
        Assert.IsTrue(add.IsSuccess, $"Expected StoryWorld add to succeed: {add.ErrorMessage}");
        return add.Payload;
    }

    // ----- AddCollectionEntry -----

    [TestMethod]
    public async Task AddCollectionEntry_SuccessPath_AppendsAndReturnsIndex()
    {
        var storyWorldGuid = await CreateOutlineAndGetStoryWorldGuid();
        var entry = new PhysicalWorldEntry { Name = "Aerth" };

        var result = _api.AddCollectionEntry(storyWorldGuid, "PhysicalWorlds", entry);

        Assert.IsTrue(result.IsSuccess, $"Expected success, got: {result.ErrorMessage}");
        Assert.AreEqual(0, result.Payload, "First entry should be at index 0.");
        Assert.IsTrue(_api.CurrentModel.Changed, "CurrentModel.Changed should be set.");
    }

    [TestMethod]
    public async Task AddCollectionEntry_ElementNotFound_ReturnsFailure()
    {
        await CreateOutlineAndGetStoryWorldGuid();
        var result = _api.AddCollectionEntry(Guid.NewGuid(), "PhysicalWorlds", new PhysicalWorldEntry());

        Assert.IsFalse(result.IsSuccess);
        Assert.IsTrue(result.ErrorMessage.Contains("not found"), $"Expected not-found message, got: {result.ErrorMessage}");
    }

    [TestMethod]
    public async Task AddCollectionEntry_PropertyNotFound_ReturnsFailure()
    {
        var storyWorldGuid = await CreateOutlineAndGetStoryWorldGuid();
        var result = _api.AddCollectionEntry(storyWorldGuid, "Bogus", new PhysicalWorldEntry());

        Assert.IsFalse(result.IsSuccess);
        Assert.IsTrue(result.ErrorMessage.Contains("Bogus"), $"Expected property name in error, got: {result.ErrorMessage}");
    }

    [TestMethod]
    public async Task AddCollectionEntry_PropertyNotCollection_ReturnsFailure()
    {
        var storyWorldGuid = await CreateOutlineAndGetStoryWorldGuid();
        var result = _api.AddCollectionEntry(storyWorldGuid, "WorldType", "anything");

        Assert.IsFalse(result.IsSuccess);
        Assert.IsTrue(result.ErrorMessage.Contains("collection"), $"Expected 'not a collection' message, got: {result.ErrorMessage}");
    }

    [TestMethod]
    public async Task AddCollectionEntry_TypeMismatch_ReturnsFailure()
    {
        var storyWorldGuid = await CreateOutlineAndGetStoryWorldGuid();
        var result = _api.AddCollectionEntry(storyWorldGuid, "PhysicalWorlds", 42);

        Assert.IsFalse(result.IsSuccess);
        Assert.IsTrue(result.ErrorMessage.Contains("type"), $"Expected type mismatch message, got: {result.ErrorMessage}");
    }

    // ----- UpdateCollectionEntry -----

    [TestMethod]
    public async Task UpdateCollectionEntry_SuccessPath_ReplacesEntry()
    {
        var storyWorldGuid = await CreateOutlineAndGetStoryWorldGuid();
        _api.AddCollectionEntry(storyWorldGuid, "PhysicalWorlds", new PhysicalWorldEntry { Name = "Original" });

        var replacement = new PhysicalWorldEntry { Name = "Replacement" };
        var result = _api.UpdateCollectionEntry(storyWorldGuid, "PhysicalWorlds", 0, replacement);

        Assert.IsTrue(result.IsSuccess, $"Expected success, got: {result.ErrorMessage}");
        var sw = (StoryWorldModel)_api.GetStoryElement(storyWorldGuid).Payload;
        Assert.AreEqual("Replacement", sw.PhysicalWorlds[0].Name);
    }

    [TestMethod]
    public async Task UpdateCollectionEntry_IndexOutOfRange_ReturnsFailure()
    {
        var storyWorldGuid = await CreateOutlineAndGetStoryWorldGuid();
        var result = _api.UpdateCollectionEntry(storyWorldGuid, "PhysicalWorlds", 99, new PhysicalWorldEntry());

        Assert.IsFalse(result.IsSuccess);
        Assert.IsTrue(result.ErrorMessage.Contains("range") || result.ErrorMessage.Contains("Index"),
            $"Expected index-out-of-range message, got: {result.ErrorMessage}");
    }

    [TestMethod]
    public async Task UpdateCollectionEntry_TypeMismatch_ReturnsFailure()
    {
        var storyWorldGuid = await CreateOutlineAndGetStoryWorldGuid();
        _api.AddCollectionEntry(storyWorldGuid, "PhysicalWorlds", new PhysicalWorldEntry());

        var result = _api.UpdateCollectionEntry(storyWorldGuid, "PhysicalWorlds", 0, "wrong type");

        Assert.IsFalse(result.IsSuccess);
        Assert.IsTrue(result.ErrorMessage.Contains("type"), $"Expected type mismatch message, got: {result.ErrorMessage}");
    }

    // ----- RemoveCollectionEntry -----

    [TestMethod]
    public async Task RemoveCollectionEntry_SuccessPath_RemovesAtIndex()
    {
        var storyWorldGuid = await CreateOutlineAndGetStoryWorldGuid();
        _api.AddCollectionEntry(storyWorldGuid, "PhysicalWorlds", new PhysicalWorldEntry { Name = "Aerth" });
        _api.AddCollectionEntry(storyWorldGuid, "PhysicalWorlds", new PhysicalWorldEntry { Name = "Mirror" });

        var result = _api.RemoveCollectionEntry(storyWorldGuid, "PhysicalWorlds", 0);

        Assert.IsTrue(result.IsSuccess, $"Expected success, got: {result.ErrorMessage}");
        var sw = (StoryWorldModel)_api.GetStoryElement(storyWorldGuid).Payload;
        Assert.AreEqual(1, sw.PhysicalWorlds.Count);
        Assert.AreEqual("Mirror", sw.PhysicalWorlds[0].Name);
    }

    [TestMethod]
    public async Task RemoveCollectionEntry_IndexOutOfRange_ReturnsFailure()
    {
        var storyWorldGuid = await CreateOutlineAndGetStoryWorldGuid();
        var result = _api.RemoveCollectionEntry(storyWorldGuid, "PhysicalWorlds", 99);

        Assert.IsFalse(result.IsSuccess);
        Assert.IsTrue(result.ErrorMessage.Contains("range") || result.ErrorMessage.Contains("Index"),
            $"Expected index-out-of-range message, got: {result.ErrorMessage}");
    }

    [TestMethod]
    public async Task RemoveCollectionEntry_PropertyNotFound_ReturnsFailure()
    {
        var storyWorldGuid = await CreateOutlineAndGetStoryWorldGuid();
        var result = _api.RemoveCollectionEntry(storyWorldGuid, "Bogus", 0);

        Assert.IsFalse(result.IsSuccess);
        Assert.IsTrue(result.ErrorMessage.Contains("Bogus"), $"Expected property name in error, got: {result.ErrorMessage}");
    }

    // ----- JSON conversion paths (the MCP scenario) -----

    [TestMethod]
    public async Task AddCollectionEntry_FromDictionary_DeserializesAndAppends()
    {
        var storyWorldGuid = await CreateOutlineAndGetStoryWorldGuid();
        var entry = new Dictionary<string, object>
        {
            ["Name"] = "Aerth",
            ["Geography"] = "Continental"
        };

        var result = _api.AddCollectionEntry(storyWorldGuid, "PhysicalWorlds", entry);

        Assert.IsTrue(result.IsSuccess, $"Expected success, got: {result.ErrorMessage}");
        var sw = (StoryWorldModel)_api.GetStoryElement(storyWorldGuid).Payload;
        Assert.AreEqual("Aerth", sw.PhysicalWorlds[0].Name);
        Assert.AreEqual("Continental", sw.PhysicalWorlds[0].Geography);
    }

    [TestMethod]
    public async Task AddCollectionEntry_FromJsonElement_DeserializesAndAppends()
    {
        var storyWorldGuid = await CreateOutlineAndGetStoryWorldGuid();
        var json = JsonDocument.Parse("{\"Name\":\"Mirror\",\"Climate\":\"Temperate\"}");

        var result = _api.AddCollectionEntry(storyWorldGuid, "PhysicalWorlds", json.RootElement);

        Assert.IsTrue(result.IsSuccess, $"Expected success, got: {result.ErrorMessage}");
        var sw = (StoryWorldModel)_api.GetStoryElement(storyWorldGuid).Payload;
        Assert.AreEqual("Mirror", sw.PhysicalWorlds[0].Name);
        Assert.AreEqual("Temperate", sw.PhysicalWorlds[0].Climate);
    }

    [TestMethod]
    public async Task UpdateCollectionEntry_FromDictionary_ReplacesEntry()
    {
        var storyWorldGuid = await CreateOutlineAndGetStoryWorldGuid();
        _api.AddCollectionEntry(storyWorldGuid, "PhysicalWorlds", new PhysicalWorldEntry { Name = "Old" });

        var replacement = new Dictionary<string, object> { ["Name"] = "New" };
        var result = _api.UpdateCollectionEntry(storyWorldGuid, "PhysicalWorlds", 0, replacement);

        Assert.IsTrue(result.IsSuccess, $"Expected success, got: {result.ErrorMessage}");
        var sw = (StoryWorldModel)_api.GetStoryElement(storyWorldGuid).Payload;
        Assert.AreEqual("New", sw.PhysicalWorlds[0].Name);
    }

    // ----- Null entry rejection -----

    [TestMethod]
    public async Task AddCollectionEntry_NullEntry_ReturnsFailure()
    {
        var storyWorldGuid = await CreateOutlineAndGetStoryWorldGuid();
        var result = _api.AddCollectionEntry(storyWorldGuid, "PhysicalWorlds", null);

        Assert.IsFalse(result.IsSuccess);
        Assert.IsTrue(result.ErrorMessage.Contains("null"), $"Expected null-entry message, got: {result.ErrorMessage}");
    }

    [TestMethod]
    public async Task UpdateCollectionEntry_NullEntry_ReturnsFailure()
    {
        var storyWorldGuid = await CreateOutlineAndGetStoryWorldGuid();
        _api.AddCollectionEntry(storyWorldGuid, "PhysicalWorlds", new PhysicalWorldEntry());
        var result = _api.UpdateCollectionEntry(storyWorldGuid, "PhysicalWorlds", 0, null);

        Assert.IsFalse(result.IsSuccess);
        Assert.IsTrue(result.ErrorMessage.Contains("null"), $"Expected null-entry message, got: {result.ErrorMessage}");
    }

    // ----- Type errors take precedence over index errors -----

    [TestMethod]
    public async Task UpdateCollectionEntry_BadTypeAndBadIndex_ReportsTypeError()
    {
        var storyWorldGuid = await CreateOutlineAndGetStoryWorldGuid();
        var result = _api.UpdateCollectionEntry(storyWorldGuid, "PhysicalWorlds", 99, "wrong type");

        Assert.IsFalse(result.IsSuccess);
        Assert.IsTrue(result.ErrorMessage.Contains("type"),
            $"Type error should win over index error, got: {result.ErrorMessage}");
    }
}
