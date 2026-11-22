using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCADLib.Models;
using StoryCADLib.Services;
using StoryCADLib.Services.Outline;
using StoryCADLib.ViewModels;
using StoryCADTests.Services.Outline;
using static StoryCADTests.Services.Outline.StoryRoleRuleTests;

namespace StoryCADTests.Services.Outline;

/// <summary>Collaborator #261: the Story Role rule of a seated Character, through the outline templates and ConvertSceneToProblem.</summary>
public partial class OutlineServiceTests
{
    [TestMethod]
    [DataRow(OutlineTemplate.OverviewAndStoryProblem)]
    [DataRow(OutlineTemplate.ExternalAndInternalProblems)]
    [DataRow(OutlineTemplate.ProtagonistAndAntagonist)]
    [DataRow(OutlineTemplate.ProblemsAndCharacters)]
    public async Task CreateModel_TemplateWithSeats_SetsStoryProblemRoles(OutlineTemplate template)
    {
        var model = await Ioc.Default.GetRequiredService<OutlineService>().CreateModel("Roles", "Author", template);
        var overview = model.StoryElements.OfType<OverviewModel>().First();
        var storyProblem = (ProblemModel)model.StoryElements.StoryElementGuids[overview.StoryProblem];

        var protagonist = (CharacterModel)model.StoryElements.StoryElementGuids[storyProblem.Protagonist];
        var antagonist = (CharacterModel)model.StoryElements.StoryElementGuids[storyProblem.Antagonist];

        Assert.AreEqual("Protagonist", protagonist.StoryRole);
        Assert.AreEqual("Antagonist", antagonist.StoryRole);
    }

    [TestMethod]
    public async Task ConvertSceneToProblem_SceneWithProtagonist_SetsMajorRole()
    {
        var (api, _) = await OutlineWithStoryProblem();
        var character = AddCharacter(api, "Fleance");
        var add = api.AddElement(StoryItemType.Scene, Overview(api).Uuid.ToString(), "Escape");
        var scene = (SceneModel)api.GetStoryElement(add.Payload).Payload!;
        scene.Protagonist = character.Uuid;

        Ioc.Default.GetRequiredService<OutlineService>().ConvertSceneToProblem(api.CurrentModel!, scene);

        Assert.AreEqual("Major Role", character.StoryRole);
    }

}
