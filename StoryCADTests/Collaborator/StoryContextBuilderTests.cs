using CommunityToolkit.Mvvm.DependencyInjection;
using CollaboratorLib.Context;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCADLib.Models;
using StoryCADLib.Models.Tools;
using StoryCADLib.Services.API;
using StoryCADLib.Services.Outline;
using StoryCADLib.ViewModels;
using StoryCollaborator;
using StoryCollaborator.Workflows;

namespace StoryCADTests.Collaborator;

/// <summary>
///     Collaborator #286: the storyContext object every migrated workflow receives carries the
///     Overview's Story Idea, Concept and Notes, and the Story World's description. On
///     2026-10-17 two models ignored the Overview Notes (the mining family that kept the sword)
///     because no workflow received them.
/// </summary>
[TestClass]
public class StoryContextBuilderTests
{
    private static StoryCADApi CreateApi() => new(
        Ioc.Default.GetRequiredService<OutlineService>(),
        Ioc.Default.GetRequiredService<ListData>(),
        Ioc.Default.GetRequiredService<ControlData>(),
        Ioc.Default.GetRequiredService<ToolsData>());

    private static async Task<StoryCADApi> ArrangeOutline()
    {
        var api = CreateApi();
        var create = await api.CreateEmptyOutline("Context Test", "Author", "0");
        Assert.IsTrue(create.IsSuccess);
        return api;
    }

    private static OverviewModel Overview(StoryCADApi api) =>
        (OverviewModel)api.GetElementsByType(StoryItemType.StoryOverview).Payload!.First();

    private static StoryWorldModel? StoryWorld(StoryCADApi api)
    {
        var found = api.GetElementsByType(StoryItemType.StoryWorld);
        return found.IsSuccess ? found.Payload?.FirstOrDefault() as StoryWorldModel : null;
    }

    [TestMethod]
    public async Task BuildContextObject_CarriesStoryIdeaConceptNotesAndStoryWorld()
    {
        var api = await ArrangeOutline();
        var overview = Overview(api);
        overview.Description = "A hero inherits a crystal sword that splits light into seven colors.";
        overview.Concept = "What if each color used alone costs the bearer something vital?";
        overview.Notes = "A mining family in the Deepcut kept the sword hidden for generations.";
        overview.Premise = "When a hero inherits a crystal sword...";
        var world = StoryWorld(api);
        if (world != null)
            world.Description = "Bright, high country where light-craft grew.";

        var context = new StoryContextBuilder(api).BuildContextObject(api.CurrentModel!);

        Assert.AreEqual("A hero inherits a crystal sword that splits light into seven colors.", context.StoryIdea);
        Assert.AreEqual("What if each color used alone costs the bearer something vital?", context.Concept);
        Assert.AreEqual("A mining family in the Deepcut kept the sword hidden for generations.", context.Notes);
        Assert.AreEqual("When a hero inherits a crystal sword...", context.Premise);
        Assert.AreEqual(world != null ? "Bright, high country where light-craft grew." : string.Empty, context.StoryWorld);
    }

    [TestMethod]
    public async Task BuildContextObject_EmptyOverview_GivesEmptyStringsNotNulls()
    {
        var api = await ArrangeOutline();
        var overview = Overview(api);
        overview.Description = "";
        overview.Concept = "";
        overview.Notes = "";

        var context = new StoryContextBuilder(api).BuildContextObject(api.CurrentModel!);

        Assert.AreEqual(string.Empty, context.StoryIdea);
        Assert.AreEqual(string.Empty, context.Concept);
        Assert.AreEqual(string.Empty, context.Notes);
        Assert.IsNotNull(context.StoryWorld);
    }

    [TestMethod]
    public async Task CharacterBuilderInput_StoryContextHoldsTheNewKeys()
    {
        var api = await ArrangeOutline();
        var model = api.CurrentModel!;
        Overview(api).Notes = "A mining family in the Deepcut kept the sword hidden.";
        var character = new CharacterModel("John Stonebender", model, null);
        var workflow = WorkflowRegistry.Get("CharacterBuilder")!;
        var gathered = new Dictionary<string, StoryElement> { ["Character"] = character };

        var input = WorkflowInputBuilder.Build(workflow, gathered, api, model)!;

        var storyContext = input["storyContext"]!;
        Assert.AreEqual("A mining family in the Deepcut kept the sword hidden.", storyContext["Notes"]!.GetValue<string>());
        Assert.IsNotNull(storyContext["StoryIdea"]);
        Assert.IsNotNull(storyContext["Concept"]);
        Assert.IsNotNull(storyContext["StoryWorld"]);
    }
}
