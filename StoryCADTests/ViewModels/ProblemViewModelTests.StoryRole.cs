using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCADLib.Models;
using StoryCADLib.Services;
using StoryCADLib.Services.Outline;
using StoryCADLib.ViewModels;
using StoryCADTests.Services.Outline;
using static StoryCADTests.Services.Outline.StoryRoleRuleTests;

namespace StoryCADTests.ViewModels;

/// <summary>Collaborator #261: the Story Role rule of a seated Character, through the Problem page.</summary>
public partial class ProblemViewModelTests
{
    [TestMethod]
    public void SaveModel_SaveModelWithNewProtagonist_SetsStoryRole()
    {
        var model = new StoryModel();
        var overview = new OverviewModel("Roles", model, null);
        model.ExplorerView.Add(overview.Node);
        var problem = new ProblemModel("Banquo's line", model, overview.Node);
        var character = new CharacterModel("Fleance", model, overview.Node);
        Ioc.Default.GetRequiredService<AppState>().CurrentDocument = new StoryDocument(model);
        var vm = Ioc.Default.GetRequiredService<ProblemViewModel>();
        vm.Activate(problem);

        vm.Protagonist = character.Uuid;
        vm.SaveModel();

        Assert.AreEqual("Major Role", character.StoryRole);
    }

    [TestMethod]
    public void SaveModel_SaveModelWithStoryProblemCategory_SetsSeatRoles()
    {
        var model = new StoryModel();
        var overview = new OverviewModel("Roles", model, null);
        model.ExplorerView.Add(overview.Node);
        var problem = new ProblemModel("Crown", model, overview.Node);
        var protagonist = new CharacterModel("Macbeth", model, overview.Node);
        var antagonist = new CharacterModel("Macduff", model, overview.Node);
        problem.Protagonist = protagonist.Uuid;
        problem.Antagonist = antagonist.Uuid;
        Ioc.Default.GetRequiredService<AppState>().CurrentDocument = new StoryDocument(model);
        var vm = Ioc.Default.GetRequiredService<ProblemViewModel>();
        vm.Activate(problem);

        vm.ProblemCategory = OverviewModel.StoryProblemCategoryListValue;
        vm.SaveModel();

        Assert.AreEqual(problem.Uuid, overview.StoryProblem);
        Assert.AreEqual("Protagonist", protagonist.StoryRole);
        Assert.AreEqual("Antagonist", antagonist.StoryRole);
    }

}
