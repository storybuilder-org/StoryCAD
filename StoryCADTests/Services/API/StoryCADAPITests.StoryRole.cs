using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCADLib.Models;
using StoryCADLib.Services;
using StoryCADLib.Services.Outline;
using StoryCADLib.ViewModels;
using StoryCADTests.Services.Outline;
using static StoryCADTests.Services.Outline.StoryRoleRuleTests;

namespace StoryCADTests.Services.API;

/// <summary>Collaborator #261: the Story Role rule of a seated Character, through UpdateElementProperty.</summary>
public partial class StoryCADApiTests
{
    [TestMethod]
    public async Task UpdateElementProperty_Protagonist_SetsMajorRole()
    {
        var (api, _) = await OutlineWithStoryProblem();
        var subplot = AddProblem(api, "Banquo's line");
        var character = AddCharacter(api, "Fleance");

        var result = api.UpdateElementProperty(subplot.Uuid, nameof(ProblemModel.Protagonist), character.Uuid);

        Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
        Assert.AreEqual("Major Role", character.StoryRole);
    }

    [TestMethod]
    public async Task UpdateElementProperty_StoryProblem_SetsSeatRoles()
    {
        var api = CreateApi();
        Assert.IsTrue((await api.CreateEmptyOutline("Roles", "Author", "0")).IsSuccess);
        var problem = AddProblem(api, "Crown");
        var protagonist = AddCharacter(api, "Macbeth");
        var antagonist = AddCharacter(api, "Macduff");
        problem.Protagonist = protagonist.Uuid;
        problem.Antagonist = antagonist.Uuid;

        var result = api.UpdateElementProperty(Overview(api).Uuid, nameof(OverviewModel.StoryProblem), problem.Uuid);

        Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
        Assert.AreEqual("Protagonist", protagonist.StoryRole);
        Assert.AreEqual("Antagonist", antagonist.StoryRole);
    }

}
