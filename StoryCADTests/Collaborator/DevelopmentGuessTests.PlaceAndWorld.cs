using CollaboratorLib.Context;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCADLib.Models;
using StoryCADLib.Services.Outline;
using StoryCADLib.ViewModels.Tools;

namespace StoryCADTests.Collaborator;

/// <summary>
/// Collaborator #261: the Place and World step, the trash rule in the guess, and loops of
/// Problems on beats. The loop tests end the test process with a stack overflow before the
/// fix, so each one was first run alone.
/// </summary>
public partial class DevelopmentGuessTests
{
    [TestMethod]
    public async Task Classify_SettingWithEmptyLocale_IsPlaceAndWorld()
    {
        var api = await SeatedCompleteCast();
        AddSetting(api, "Inverness", locale: string.Empty);

        var guess = new StoryContextBuilder(api).Classify(api.CurrentModel!);

        Assert.AreEqual(StoryContextBuilder.DevelopmentPhase.PlaceAndWorld, guess.Earliest);
    }

    [TestMethod]
    public async Task Classify_SettingGapAndCharacterGap_IsCharacterDevelopment()
    {
        // (regression)
        var api = await SeatedCompleteCast();
        AddSetting(api, "Inverness", locale: string.Empty);
        ((CharacterModel)api.GetStoryElement(StoryProblem(api).Protagonist).Payload!).Age = string.Empty;

        var guess = new StoryContextBuilder(api).Classify(api.CurrentModel!);

        Assert.AreEqual(StoryContextBuilder.DevelopmentPhase.CharacterDevelopment, guess.Earliest);
    }

    [TestMethod]
    public async Task Classify_StoryWorldWithEmptyWorldType_IsPlaceAndWorld()
    {
        var api = await SeatedCompleteCast();
        AddStoryWorld(api, worldType: string.Empty);

        var guess = new StoryContextBuilder(api).Classify(api.CurrentModel!);

        Assert.AreEqual(StoryContextBuilder.DevelopmentPhase.PlaceAndWorld, guess.Earliest);
    }

    [TestMethod]
    public async Task Classify_SettingWithEmptyName_IsPlaceAndWorld()
    {
        var api = await SeatedCompleteCast();
        var setting = AddSetting(api, "Inverness", locale: "Highlands");
        setting.Name = string.Empty;

        var guess = new StoryContextBuilder(api).Classify(api.CurrentModel!);

        Assert.AreEqual(StoryContextBuilder.DevelopmentPhase.PlaceAndWorld, guess.Earliest);
    }

    [TestMethod]
    public async Task Classify_SettingGapAndSceneOnBeat_IsPlaceAndWorld()
    {
        var api = await SeatedCompleteCast();
        AddSetting(api, "Inverness", locale: string.Empty);
        var problem = StoryProblem(api);
        var scene = AddScene(api, problem, "Battle");
        problem.StructureBeats.Add(new StructureBeat("Climax", "They fight") { Guid = scene.Uuid });

        var guess = new StoryContextBuilder(api).Classify(api.CurrentModel!);

        Assert.AreEqual(StoryContextBuilder.DevelopmentPhase.PlaceAndWorld, guess.Earliest);
    }

    [TestMethod]
    public async Task Classify_SettingInTrash_IsNotPlaceAndWorld()
    {
        var api = await SeatedCompleteCast();
        var setting = AddSetting(api, "Inverness", locale: string.Empty);
        Ioc.Default.GetRequiredService<OutlineService>().MoveToTrash(setting, api.CurrentModel!);

        var guess = new StoryContextBuilder(api).Classify(api.CurrentModel!);

        Assert.AreNotEqual(StoryContextBuilder.DevelopmentPhase.PlaceAndWorld, guess.Earliest);
    }

    [TestMethod]
    public async Task Classify_StoryWorldInTrash_IsNotPlaceAndWorld()
    {
        var api = await SeatedCompleteCast();
        var world = AddStoryWorld(api, worldType: string.Empty);
        Ioc.Default.GetRequiredService<OutlineService>().MoveToTrash(world, api.CurrentModel!);

        var guess = new StoryContextBuilder(api).Classify(api.CurrentModel!);

        Assert.AreNotEqual(StoryContextBuilder.DevelopmentPhase.PlaceAndWorld, guess.Earliest);
    }

    [TestMethod]
    public void PromptLineFor_PlaceAndWorld_GivesDesignText()
    {
        Assert.AreEqual("Place and world development - Setting and Story World fields are empty",
            StoryContextBuilder.PromptLineFor(StoryContextBuilder.DevelopmentPhase.PlaceAndWorld));
    }

    [TestMethod]
    public void GapsSentenceFor_PlaceAndWorld_GivesDesignText()
    {
        Assert.AreEqual("Guess: the outline is in Place and World Development. Setting or Story World fields are empty.",
            StoryContextBuilder.GapsSentenceFor(new[] { StoryContextBuilder.DevelopmentPhase.PlaceAndWorld }));
    }

    [TestMethod]
    public async Task Classify_LoopOfProblems_IsStructureBuilding()
    {
        var api = await LoopOutline();

        var guess = new StoryContextBuilder(api).Classify(api.CurrentModel!);

        Assert.AreEqual(StoryContextBuilder.DevelopmentPhase.StructureBuilding, guess.Earliest);
    }

    [TestMethod]
    public async Task BuildContext_LoopOfProblemsTargetOutside_GivesText()
    {
        var api = await LoopOutline();
        var problemC = AddProblem(api, "ProblemC");

        var context = new StoryContextBuilder(api).BuildContext(problemC, LoopSpec(), api.CurrentModel!);

        Assert.IsFalse(string.IsNullOrEmpty(context));
    }

    [TestMethod]
    public async Task BuildContext_LoopOfProblemsTargetInLoop_GivesText()
    {
        var api = await LoopOutline();
        var problemA = api.CurrentModel!.StoryElements.OfType<ProblemModel>().First(p => p.Name == "ProblemA");

        var context = new StoryContextBuilder(api).BuildContext(problemA, LoopSpec(), api.CurrentModel!);

        Assert.IsFalse(string.IsNullOrEmpty(context));
    }

    [TestMethod]
    public async Task Classify_SceneInTrashOnStoryProblemBeat_IsStructureBuilding()
    {
        var api = await SeatedCompleteCast();
        var problem = StoryProblem(api);
        var scene = AddScene(api, problem, "Battle");
        Ioc.Default.GetRequiredService<OutlineService>().MoveToTrash(scene, api.CurrentModel!);
        problem.StructureBeats.Add(new StructureBeat("Climax", "They fight") { Guid = scene.Uuid });

        var guess = new StoryContextBuilder(api).Classify(api.CurrentModel!);

        Assert.AreEqual(StoryContextBuilder.DevelopmentPhase.StructureBuilding, guess.Earliest);
    }

    [TestMethod]
    public async Task Classify_ProtagonistInTrash_IsProblemDevelopment()
    {
        var api = await SeatedCompleteCast();
        var problem = StoryProblem(api);
        var protagonist = (CharacterModel)api.GetStoryElement(problem.Protagonist).Payload!;
        Ioc.Default.GetRequiredService<OutlineService>().MoveToTrash(protagonist, api.CurrentModel!);
        problem.Protagonist = protagonist.Uuid;

        var guess = new StoryContextBuilder(api).Classify(api.CurrentModel!);

        Assert.AreEqual(StoryContextBuilder.DevelopmentPhase.ProblemDevelopment, guess.Earliest);
    }

    // ---- helpers ----

    private static ContextSpec LoopSpec() => new()
    {
        IncludeBeatHierarchy = true,
        IncludePrecedingEvents = true,
        MaxPrecedingBeats = 5
    };

    /// <summary>
    /// The guess outline with ProblemA on a Story Problem beat, ProblemB on a beat of ProblemA,
    /// and ProblemA written straight into a beat of ProblemB. AssignElementToBeat would clear
    /// ProblemA's first beat (the one-parent rule), so the last link is written directly.
    /// </summary>
    private static async Task<StoryCADLib.Services.API.StoryCADApi> LoopOutline()
    {
        var api = await SeatedCompleteCast();
        var storyProblem = StoryProblem(api);
        var problemA = AddProblem(api, "ProblemA");
        var problemB = AddProblem(api, "ProblemB");
        Assert.IsTrue(api.CreateBeat(storyProblem.Uuid, "Midpoint", string.Empty).IsSuccess);
        Assert.IsTrue(api.CreateBeat(problemA.Uuid, "Turn", string.Empty).IsSuccess);
        Assert.IsTrue(api.AssignElementToBeat(storyProblem.Uuid, 0, problemA.Uuid).IsSuccess);
        Assert.IsTrue(api.AssignElementToBeat(problemA.Uuid, 0, problemB.Uuid).IsSuccess);
        Assert.IsTrue(api.CreateBeat(problemB.Uuid, "Return", string.Empty).IsSuccess);
        problemB.StructureBeats[0].Guid = problemA.Uuid;
        return api;
    }

    private static SettingModel AddSetting(StoryCADLib.Services.API.StoryCADApi api, string name, string locale)
    {
        var add = api.AddElement(StoryItemType.Setting, Overview(api).Uuid.ToString(), name);
        Assert.IsTrue(add.IsSuccess, add.ErrorMessage);
        var setting = (SettingModel)api.GetStoryElement(add.Payload).Payload!;
        setting.Description = "A castle";
        setting.Locale = locale;
        setting.Period = "1040";
        return setting;
    }

    private static StoryWorldModel AddStoryWorld(StoryCADLib.Services.API.StoryCADApi api, string worldType)
    {
        var add = api.AddElement(StoryItemType.StoryWorld, Overview(api).Uuid.ToString(), "Scotland");
        Assert.IsTrue(add.IsSuccess, add.ErrorMessage);
        var world = (StoryWorldModel)api.GetStoryElement(add.Payload).Payload!;
        world.Description = "Thanes feud.";
        world.WorldType = worldType;
        return world;
    }
}
