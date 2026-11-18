using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCADLib.Models;
using StoryCADLib.Services;
using StoryCADLib.ViewModels;

#nullable disable

namespace StoryCADTests.ViewModels;

[TestClass]
public class OverviewViewModelTests
{
    private StoryModel _storyModel;
    private OverviewModel _overview;
    private OverviewViewModel _viewModel;

    [TestInitialize]
    public void TestInitialize()
    {
        _storyModel = new StoryModel();
        _overview = new OverviewModel("Danger Calls", _storyModel, null);
        _storyModel.ExplorerView.Add(_overview.Node);
        _overview.StoryProblem = Guid.Empty;
        _overview.Premise = string.Empty;

        Ioc.Default.GetRequiredService<AppState>().CurrentDocument = new StoryDocument(_storyModel);
        _viewModel = Ioc.Default.GetRequiredService<OverviewViewModel>();
    }

    [TestMethod]
    public void SelectedProblem_UserPicksProblem_ShowsProblemPremise()
    {
        var problem = new ProblemModel("Catching the drug dealer Lacas", _storyModel, _overview.Node);
        problem.Premise = "Leonard catches Lacas.";
        _viewModel.Activate(_overview);

        _viewModel.SelectedProblem = problem;

        Assert.AreEqual("Leonard catches Lacas.", _viewModel.Premise);
    }

    [TestMethod]
    public void SelectedProblem_SwitchAfterTypingPremise_KeepsTypedPremiseOnPreviousProblem()
    {
        var first = new ProblemModel("Catching the drug dealer Lacas", _storyModel, _overview.Node);
        first.Premise = "Old";
        var second = new ProblemModel("Leonard's impulsiveness", _storyModel, _overview.Node);
        second.Premise = "Second premise.";
        _viewModel.Activate(_overview);
        _viewModel.SelectedProblem = first;
        _viewModel.Premise = "Typed, not yet saved.";

        _viewModel.SelectedProblem = second;

        Assert.AreEqual("Typed, not yet saved.", first.Premise);
        Assert.AreEqual("Second premise.", _viewModel.Premise);
    }

    [TestMethod]
    public void SaveModel_AfterPickingProblem_KeepsProblemPremise()
    {
        var problem = new ProblemModel("Catching the drug dealer Lacas", _storyModel, _overview.Node);
        problem.Premise = "Leonard catches Lacas.";
        _viewModel.Activate(_overview);

        _viewModel.SelectedProblem = problem;
        _viewModel.Deactivate(null);

        Assert.AreEqual("Leonard catches Lacas.", problem.Premise);
        Assert.AreEqual("Leonard catches Lacas.", _overview.Premise);
    }

    [TestMethod]
    public void SaveModel_EditPremiseAfterPick_WritesBothPremises()
    {
        var problem = new ProblemModel("Catching the drug dealer Lacas", _storyModel, _overview.Node);
        problem.Premise = "Old";
        _viewModel.Activate(_overview);

        _viewModel.SelectedProblem = problem;
        _viewModel.Premise = "New";
        _viewModel.Deactivate(null);

        Assert.AreEqual("New", problem.Premise);
        Assert.AreEqual("New", _overview.Premise);
    }

    [TestMethod]
    public void Activate_LinkedProblemHasPremise_ShowsProblemPremise()
    {
        var problem = new ProblemModel("Catching the drug dealer Lacas", _storyModel, _overview.Node);
        problem.Premise = "Leonard catches Lacas.";
        _overview.StoryProblem = problem.Uuid;
        _overview.Premise = string.Empty;

        _viewModel.Activate(_overview);
        _viewModel.Deactivate(null);

        Assert.AreEqual("Leonard catches Lacas.", _viewModel.Premise);
        Assert.AreEqual("Leonard catches Lacas.", problem.Premise);
    }

    [TestMethod]
    public void SelectedProblem_FirstPickOfEmptyProblem_KeepsOverviewPremise()
    {
        var problem = new ProblemModel("Empty", _storyModel, _overview.Node);
        problem.Premise = string.Empty;
        _overview.Premise = "Overview text";
        _viewModel.Activate(_overview);

        _viewModel.SelectedProblem = problem;
        _viewModel.Deactivate(null);

        Assert.AreEqual("Overview text", problem.Premise);
    }
}
