using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCADLib.Models;
using StoryCADLib.Services;
using StoryCADLib.Services.Outline;
using StoryCADLib.ViewModels;
using StoryCADTests.Services.Outline;
using static StoryCADTests.Services.Outline.StoryRoleRuleTests;

namespace StoryCADTests.ViewModels;

/// <summary>Collaborator #261: the Story Role rule of a seated Character, through the Overview page.</summary>
public partial class OverviewViewModelTests
{
    [TestMethod]
    public void SaveModel_SaveModelWithNewStoryProblem_SetsSeatRoles()
    {
        var model = new StoryModel();
        var overview = new OverviewModel("Roles", model, null);
        model.ExplorerView.Add(overview.Node);
        overview.StoryProblem = Guid.Empty;
        var problem = new ProblemModel("Crown", model, overview.Node);
        var protagonist = new CharacterModel("Macbeth", model, overview.Node);
        var antagonist = new CharacterModel("Macduff", model, overview.Node);
        problem.Protagonist = protagonist.Uuid;
        problem.Antagonist = antagonist.Uuid;
        Ioc.Default.GetRequiredService<AppState>().CurrentDocument = new StoryDocument(model);
        var vm = Ioc.Default.GetRequiredService<OverviewViewModel>();
        vm.Activate(overview);

        vm.SelectedProblem = problem;
        vm.Deactivate(null);

        Assert.AreEqual("Protagonist", protagonist.StoryRole);
        Assert.AreEqual("Antagonist", antagonist.StoryRole);
    }

}
