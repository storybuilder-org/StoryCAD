using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCADLib.Models;
using StoryCADLib.Models.Tools;
using StoryCADLib.Services.API;
using StoryCADLib.Services.Outline;
using StoryCADLib.ViewModels;

namespace StoryCADTests.Services.Outline;

/// <summary>
/// Collaborator #261: a Character seated as Protagonist or Antagonist gets a Story Role that
/// matches the seat. Protagonist or Antagonist on the Story Problem, Major Role elsewhere.
/// </summary>
[TestClass]
public class StoryRoleRuleTests
{
    [TestMethod]
    [DataRow(true, "Protagonist")]
    [DataRow(false, "Antagonist")]
    public async Task ApplySeats_EmptyRoleOnStoryProblem_GivesSeatName(bool protagonistSeat, string expected)
    {
        var (api, storyProblem) = await OutlineWithStoryProblem();
        var character = AddCharacter(api, "Macbeth");
        if (protagonistSeat) storyProblem.Protagonist = character.Uuid; else storyProblem.Antagonist = character.Uuid;

        StoryRoleRule.ApplySeats(api.CurrentModel!, storyProblem);

        Assert.AreEqual(expected, character.StoryRole);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("Supporting Role")]
    [DataRow("Minor Role")]
    [DataRow("Service Role")]
    public async Task ApplySeats_LesserRoleOnOtherProblem_GivesMajorRole(string current)
    {
        var (api, _) = await OutlineWithStoryProblem();
        var subplot = AddProblem(api, "Banquo's line");
        var character = AddCharacter(api, "Fleance");
        character.StoryRole = current;
        subplot.Protagonist = character.Uuid;

        StoryRoleRule.ApplySeats(api.CurrentModel!, subplot);

        Assert.AreEqual("Major Role", character.StoryRole);
    }

    [TestMethod]
    public async Task ApplySeats_MajorRoleOnStoryProblem_GivesProtagonist()
    {
        var (api, storyProblem) = await OutlineWithStoryProblem();
        var character = AddCharacter(api, "Macbeth");
        character.StoryRole = "Major Role";
        storyProblem.Protagonist = character.Uuid;

        StoryRoleRule.ApplySeats(api.CurrentModel!, storyProblem);

        Assert.AreEqual("Protagonist", character.StoryRole);
    }

    [TestMethod]
    public async Task ApplySeats_ProtagonistOnOtherProblem_KeepsProtagonist()
    {
        var (api, _) = await OutlineWithStoryProblem();
        var subplot = AddProblem(api, "Banquo's line");
        var character = AddCharacter(api, "Macbeth");
        character.StoryRole = "Protagonist";
        subplot.Antagonist = character.Uuid;

        StoryRoleRule.ApplySeats(api.CurrentModel!, subplot);

        Assert.AreEqual("Protagonist", character.StoryRole);
    }

    [TestMethod]
    public async Task ApplySeats_AntagonistInProtagonistSeat_KeepsAntagonist()
    {
        var (api, storyProblem) = await OutlineWithStoryProblem();
        var character = AddCharacter(api, "Macduff");
        character.StoryRole = "Antagonist";
        storyProblem.Protagonist = character.Uuid;

        StoryRoleRule.ApplySeats(api.CurrentModel!, storyProblem);

        Assert.AreEqual("Antagonist", character.StoryRole);
    }

    [TestMethod]
    public async Task ApplySeats_UnknownRole_KeepsRole()
    {
        var (api, storyProblem) = await OutlineWithStoryProblem();
        var character = AddCharacter(api, "Macbeth");
        character.StoryRole = "Villain";
        storyProblem.Protagonist = character.Uuid;

        StoryRoleRule.ApplySeats(api.CurrentModel!, storyProblem);

        Assert.AreEqual("Villain", character.StoryRole);
    }

    [TestMethod]
    public async Task ApplySeats_OneCharacterInBothSeats_GivesProtagonist()
    {
        var (api, storyProblem) = await OutlineWithStoryProblem();
        var character = AddCharacter(api, "Macbeth");
        storyProblem.Protagonist = character.Uuid;
        storyProblem.Antagonist = character.Uuid;

        StoryRoleRule.ApplySeats(api.CurrentModel!, storyProblem);

        Assert.AreEqual("Protagonist", character.StoryRole);
    }

    [TestMethod]
    public async Task ApplySeats_CharacterInTrash_WritesNothing()
    {
        var (api, storyProblem) = await OutlineWithStoryProblem();
        var character = AddCharacter(api, "Macbeth");
        Ioc.Default.GetRequiredService<OutlineService>().MoveToTrash(character, api.CurrentModel!);
        storyProblem.Protagonist = character.Uuid;

        StoryRoleRule.ApplySeats(api.CurrentModel!, storyProblem);

        Assert.AreEqual(string.Empty, character.StoryRole ?? string.Empty);
    }

    [TestMethod]
    public async Task ApplySeats_SeatHoldsSetting_MakesNoChange()
    {
        var (api, storyProblem) = await OutlineWithStoryProblem();
        var add = api.AddElement(StoryItemType.Setting, Overview(api).Uuid.ToString(), "Inverness");
        var setting = (SettingModel)api.GetStoryElement(add.Payload).Payload!;
        var before = setting.Description;
        storyProblem.Protagonist = setting.Uuid;

        StoryRoleRule.ApplySeats(api.CurrentModel!, storyProblem);

        Assert.AreEqual(before, setting.Description);
    }

    [TestMethod]
    public async Task ApplySeats_CharacterRemovedFromSeat_KeepsRole()
    {
        var (api, _) = await OutlineWithStoryProblem();
        var subplot = AddProblem(api, "Banquo's line");
        var character = AddCharacter(api, "Fleance");
        subplot.Protagonist = character.Uuid;
        StoryRoleRule.ApplySeats(api.CurrentModel!, subplot);

        subplot.Protagonist = Guid.Empty;
        StoryRoleRule.ApplySeats(api.CurrentModel!, subplot);

        Assert.AreEqual("Major Role", character.StoryRole);
    }

    [TestMethod]
    public async Task ApplySeats_ProblemInTrash_MakesNoChange()
    {
        var (api, _) = await OutlineWithStoryProblem();
        var subplot = AddProblem(api, "Banquo's line");
        var character = AddCharacter(api, "Fleance");
        Ioc.Default.GetRequiredService<OutlineService>().MoveToTrash(subplot, api.CurrentModel!);
        subplot.Protagonist = character.Uuid;

        StoryRoleRule.ApplySeats(api.CurrentModel!, subplot);

        Assert.AreEqual(string.Empty, character.StoryRole ?? string.Empty);
    }

    [TestMethod]
    public async Task ApplySeats_StoryProblemInTrash_GivesMajorRole()
    {
        var (api, storyProblem) = await OutlineWithStoryProblem();
        var other = AddProblem(api, "Banquo's line");
        var character = AddCharacter(api, "Fleance");
        Ioc.Default.GetRequiredService<OutlineService>().MoveToTrash(storyProblem, api.CurrentModel!);
        Overview(api).StoryProblem = storyProblem.Uuid;
        other.Protagonist = character.Uuid;

        StoryRoleRule.ApplySeats(api.CurrentModel!, other);

        Assert.AreEqual("Major Role", character.StoryRole);
    }

    // ---- helpers ----

    internal static StoryCADApi CreateApi() => new(
        Ioc.Default.GetRequiredService<OutlineService>(),
        Ioc.Default.GetRequiredService<ListData>(),
        Ioc.Default.GetRequiredService<ControlData>(),
        Ioc.Default.GetRequiredService<ToolsData>());

    internal static async Task<(StoryCADApi Api, ProblemModel StoryProblem)> OutlineWithStoryProblem()
    {
        var api = CreateApi();
        Assert.IsTrue((await api.CreateEmptyOutline("Roles", "Author", "0")).IsSuccess);
        var problem = AddProblem(api, "Crown");
        Overview(api).StoryProblem = problem.Uuid;
        return (api, problem);
    }

    internal static OverviewModel Overview(StoryCADApi api) =>
        api.CurrentModel!.StoryElements.OfType<OverviewModel>().First();

    internal static ProblemModel AddProblem(StoryCADApi api, string name)
    {
        var add = api.AddElement(StoryItemType.Problem, Overview(api).Uuid.ToString(), name);
        Assert.IsTrue(add.IsSuccess, add.ErrorMessage);
        return (ProblemModel)api.GetStoryElement(add.Payload).Payload!;
    }

    internal static CharacterModel AddCharacter(StoryCADApi api, string name)
    {
        var add = api.AddElement(StoryItemType.Character, Overview(api).Uuid.ToString(), name);
        Assert.IsTrue(add.IsSuccess, add.ErrorMessage);
        return (CharacterModel)api.GetStoryElement(add.Payload).Payload!;
    }
}
