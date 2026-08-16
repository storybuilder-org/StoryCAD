using CollaboratorLib.Context;
using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCADLib.Models;
using StoryCADLib.Models.Tools;
using StoryCADLib.Services.API;
using StoryCADLib.Services.Outline;
using StoryCADLib.ViewModels;
using StoryCADLib.ViewModels.Tools;

namespace StoryCADTests.Collaborator;

/// <summary>
/// Collaborator #107: Guess classifier and Problem prompt line.
/// </summary>
[TestClass]
public class DevelopmentGuessTests
{
    private static StoryCADApi CreateApi() => new(
        Ioc.Default.GetRequiredService<OutlineService>(),
        Ioc.Default.GetRequiredService<ListData>(),
        Ioc.Default.GetRequiredService<ControlData>(),
        Ioc.Default.GetRequiredService<ToolsData>());

    [TestMethod]
    public async Task BlankPremise_IsIdeation_NotCharacterDevelopment()
    {
        var api = CreateApi();
        var create = await api.CreateEmptyOutline("Guess Test", "Author", "0");
        Assert.IsTrue(create.IsSuccess);
        var overview = Overview(api);
        overview.StoryType = "";
        overview.StoryGenre = "";
        overview.Premise = "";

        var guess = new StoryContextBuilder(api).Classify(api.CurrentModel!);

        Assert.AreEqual(StoryContextBuilder.DevelopmentPhase.Ideation, guess.Earliest);
        CollectionAssert.DoesNotContain(guess.OpenSteps.ToList(),
            StoryContextBuilder.DevelopmentPhase.CharacterDevelopment);
        StringAssert.Contains(guess.GapsSentence, "Ideation");
    }

    [TestMethod]
    public async Task PremiseNoStoryProblem_IsProblemDevelopmentOnly()
    {
        var api = await OutlineWithPremise();
        var overview = Overview(api);
        overview.StoryProblem = Guid.Empty;

        var guess = new StoryContextBuilder(api).Classify(api.CurrentModel!);

        Assert.AreEqual(StoryContextBuilder.DevelopmentPhase.ProblemDevelopment, guess.Earliest);
        Assert.AreEqual(1, guess.OpenSteps.Count);
        Assert.AreEqual(StoryContextBuilder.DevelopmentPhase.ProblemDevelopment, guess.OpenSteps[0]);
        Assert.IsFalse(guess.PromptLine.Contains("Problem/character"));
    }

    [TestMethod]
    public async Task UnseatedStoryProblem_NamesBothSteps()
    {
        var api = await OutlineWithPremise();
        var problem = AddProblem(api, "Crown");
        Overview(api).StoryProblem = problem.Uuid;

        var guess = new StoryContextBuilder(api).Classify(api.CurrentModel!);

        Assert.AreEqual(StoryContextBuilder.DevelopmentPhase.ProblemDevelopment, guess.Earliest);
        CollectionAssert.AreEqual(
            new[]
            {
                StoryContextBuilder.DevelopmentPhase.ProblemDevelopment,
                StoryContextBuilder.DevelopmentPhase.CharacterDevelopment
            },
            guess.OpenSteps.ToList());
        StringAssert.Contains(guess.GapsSentence, "both open");
        StringAssert.Contains(guess.GapsSentence, "seats");
    }

    [TestMethod]
    public async Task OneSeatEmpty_SameAsUnseated()
    {
        var api = await OutlineWithPremise();
        var problem = AddProblem(api, "Crown");
        var prot = AddCharacter(api, "Macbeth");
        FillEssentials(prot);
        problem.Protagonist = prot.Uuid;
        Overview(api).StoryProblem = problem.Uuid;

        var guess = new StoryContextBuilder(api).Classify(api.CurrentModel!);

        Assert.AreEqual(StoryContextBuilder.DevelopmentPhase.ProblemDevelopment, guess.Earliest);
        Assert.AreEqual(2, guess.OpenSteps.Count);
    }

    [TestMethod]
    public async Task UnresolvedSeat_SameAsUnseated()
    {
        var api = await OutlineWithPremise();
        var problem = AddProblem(api, "Crown");
        problem.Protagonist = Guid.NewGuid();
        problem.Antagonist = Guid.NewGuid();
        Overview(api).StoryProblem = problem.Uuid;

        var guess = new StoryContextBuilder(api).Classify(api.CurrentModel!);

        Assert.AreEqual(StoryContextBuilder.DevelopmentPhase.ProblemDevelopment, guess.Earliest);
        Assert.AreEqual(2, guess.OpenSteps.Count);
    }

    [TestMethod]
    public async Task SeatedMissingBackStory_IsCharacterDevelopment()
    {
        var api = await OutlineWithPremise();
        var problem = AddProblem(api, "Crown");
        var prot = AddCharacter(api, "Macbeth");
        var antag = AddCharacter(api, "Macduff");
        FillEssentials(prot);
        FillEssentials(antag);
        antag.BackStory = "";
        problem.Protagonist = prot.Uuid;
        problem.Antagonist = antag.Uuid;
        Overview(api).StoryProblem = problem.Uuid;

        var guess = new StoryContextBuilder(api).Classify(api.CurrentModel!);

        Assert.AreEqual(StoryContextBuilder.DevelopmentPhase.CharacterDevelopment, guess.Earliest);
        Assert.AreEqual(1, guess.OpenSteps.Count);
        Assert.AreEqual(
            "Character development - filling essential Character fields",
            guess.PromptLine);
    }

    [TestMethod]
    public async Task SeatedEssentialsFull_NoBeats_IsStructureBuilding()
    {
        var api = await SeatedCompleteCast();

        var guess = new StoryContextBuilder(api).Classify(api.CurrentModel!);

        Assert.AreEqual(StoryContextBuilder.DevelopmentPhase.StructureBuilding, guess.Earliest);
        Assert.IsFalse(guess.PromptLine.Contains("Problem/character"));
    }

    [TestMethod]
    public async Task SeatedEssentialsFull_BeatsNoScene_IsStructureBuilding()
    {
        var api = await SeatedCompleteCast();
        var problem = StoryProblem(api);
        problem.StructureBeats.Add(new StructureBeat("Catalyst", "Something happens"));

        var guess = new StoryContextBuilder(api).Classify(api.CurrentModel!);

        Assert.AreEqual(StoryContextBuilder.DevelopmentPhase.StructureBuilding, guess.Earliest);
    }

    [TestMethod]
    public async Task SeatedEssentialsFull_SceneOnBeat_IsSceneWork()
    {
        var api = await SeatedCompleteCast();
        var problem = StoryProblem(api);
        var scene = AddScene(api, problem, "Battle");
        problem.StructureBeats.Add(new StructureBeat("Climax", "They fight") { Guid = scene.Uuid });

        var guess = new StoryContextBuilder(api).Classify(api.CurrentModel!);

        Assert.AreEqual(StoryContextBuilder.DevelopmentPhase.SceneWork, guess.Earliest);
    }

    [TestMethod]
    public async Task ExtraCharacterThin_DoesNotHoldGuessInCharacterDevelopment()
    {
        var api = await SeatedCompleteCast();
        AddCharacter(api, "Porter");

        var guess = new StoryContextBuilder(api).Classify(api.CurrentModel!);

        Assert.AreEqual(StoryContextBuilder.DevelopmentPhase.StructureBuilding, guess.Earliest);
    }

    [TestMethod]
    public async Task CharacterLinkedOnlyOnNonStoryProblem_DoesNotHoldGuess()
    {
        var api = await SeatedCompleteCast();
        var side = AddProblem(api, "Side conflict");
        var extra = AddCharacter(api, "Banquo");
        side.Protagonist = extra.Uuid;
        side.Antagonist = extra.Uuid;

        var guess = new StoryContextBuilder(api).Classify(api.CurrentModel!);

        Assert.AreEqual(StoryContextBuilder.DevelopmentPhase.StructureBuilding, guess.Earliest);
    }

    [TestMethod]
    public async Task SameCharacterOnBothSeats_ScansOnce()
    {
        var api = await OutlineWithPremise();
        var problem = AddProblem(api, "Crown");
        var only = AddCharacter(api, "Macbeth");
        FillEssentials(only);
        problem.Protagonist = only.Uuid;
        problem.Antagonist = only.Uuid;
        Overview(api).StoryProblem = problem.Uuid;

        var guess = new StoryContextBuilder(api).Classify(api.CurrentModel!);

        Assert.AreEqual(StoryContextBuilder.DevelopmentPhase.StructureBuilding, guess.Earliest);
    }

    [TestMethod]
    public async Task BuildContext_ProblemLine_DoesNotSayProblemCharacter()
    {
        var api = await OutlineWithPremise();
        Overview(api).StoryProblem = Guid.Empty;
        var context = new StoryContextBuilder(api).BuildContext(
            null, ContextSpec.Default, api.CurrentModel!);

        StringAssert.Contains(context, "## Development Phase");
        Assert.IsFalse(context.Contains("Problem/character"));
        StringAssert.Contains(context, "Problem development - building the Story Problem");
    }

    private static async Task<StoryCADApi> OutlineWithPremise()
    {
        var api = CreateApi();
        var create = await api.CreateEmptyOutline("Guess Test", "Author", "0");
        Assert.IsTrue(create.IsSuccess, create.ErrorMessage);
        var overview = Overview(api);
        overview.StoryType = "Novel";
        overview.StoryGenre = "Tragedy";
        overview.Premise = "Ambition destroys a thane.";
        return api;
    }

    private static async Task<StoryCADApi> SeatedCompleteCast()
    {
        var api = await OutlineWithPremise();
        var problem = AddProblem(api, "Crown");
        var prot = AddCharacter(api, "Macbeth");
        var antag = AddCharacter(api, "Macduff");
        FillEssentials(prot);
        FillEssentials(antag);
        problem.Protagonist = prot.Uuid;
        problem.Antagonist = antag.Uuid;
        Overview(api).StoryProblem = problem.Uuid;
        return api;
    }

    private static OverviewModel Overview(StoryCADApi api) =>
        api.CurrentModel!.StoryElements.OfType<OverviewModel>().First();

    private static ProblemModel StoryProblem(StoryCADApi api)
    {
        var guid = Overview(api).StoryProblem;
        return (ProblemModel)api.GetStoryElement(guid).Payload!;
    }

    private static ProblemModel AddProblem(StoryCADApi api, string name)
    {
        var add = api.AddElement(StoryItemType.Problem, Overview(api).Uuid.ToString(), name);
        Assert.IsTrue(add.IsSuccess, add.ErrorMessage);
        return (ProblemModel)api.GetStoryElement(add.Payload).Payload!;
    }

    private static CharacterModel AddCharacter(StoryCADApi api, string name)
    {
        var add = api.AddElement(StoryItemType.Character, Overview(api).Uuid.ToString(), name);
        Assert.IsTrue(add.IsSuccess, add.ErrorMessage);
        return (CharacterModel)api.GetStoryElement(add.Payload).Payload!;
    }

    private static SceneModel AddScene(StoryCADApi api, ProblemModel parent, string name)
    {
        var add = api.AddElement(StoryItemType.Scene, parent.Uuid.ToString(), name);
        Assert.IsTrue(add.IsSuccess, add.ErrorMessage);
        return (SceneModel)api.GetStoryElement(add.Payload).Payload!;
    }

    private static void FillEssentials(CharacterModel character)
    {
        character.Description = "Sketch";
        character.Role = "Thane";
        character.StoryRole = "Protagonist";
        character.Age = "38";
        character.Sex = "Male";
        character.Appearance = "Weathered";
        character.BackStory = "The witches spoke first";
    }
}
