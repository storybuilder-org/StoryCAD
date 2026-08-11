using System.Threading.Tasks;
using StoryCADLib.Collaborator.ViewModels;
using StoryCADLib.Services.Collaborator.Contracts;

#nullable disable

namespace StoryCADTests.Collaborator.ViewModels;

/// <summary>
/// The suppression guard that stops a star toggle from being read as a workflow choice.
/// A workflow selection runs the workflow, so a false positive here spends the user's quota on
/// a request they never made.
/// </summary>
[TestClass]
public class WorkflowShellStarSelectionTests
{
    private WorkflowShellViewModel _viewModel;

    [TestInitialize]
    public void Setup()
    {
        _viewModel = new WorkflowShellViewModel
        {
            OnWorkflowSelected = _ => Task.CompletedTask
        };
    }

    [TestMethod]
    public void ShouldRunWorkflowForSelection_WithFreshTag_ReturnsTrue()
    {
        Assert.IsTrue(_viewModel.ShouldRunWorkflowForSelection("Premise"));
    }

    [TestMethod]
    public void ShouldRunWorkflowForSelection_WhenSuppressed_ReturnsFalse()
    {
        _viewModel.SuppressWorkflowNavigation = true;

        Assert.IsFalse(_viewModel.ShouldRunWorkflowForSelection("Premise"));
    }

    [TestMethod]
    public void ShouldRunWorkflowForSelection_AfterSuppressionClears_RunsTheSameWorkflow()
    {
        // The suppressed pass must not record the tag: if it did, the user's next genuine click
        // on that workflow would look like a menu restore and silently do nothing.
        _viewModel.SuppressWorkflowNavigation = true;
        _viewModel.ShouldRunWorkflowForSelection("Premise");
        _viewModel.SuppressWorkflowNavigation = false;

        Assert.IsTrue(_viewModel.ShouldRunWorkflowForSelection("Premise"));
    }

    [TestMethod]
    public void ShouldRunWorkflowForSelection_WithTheSameTagTwice_ReturnsFalse()
    {
        _viewModel.ShouldRunWorkflowForSelection("Premise");

        Assert.IsFalse(_viewModel.ShouldRunWorkflowForSelection("Premise"),
            "A rebuild re-selects the current tag; re-running it would repeat the workflow.");
    }

    [TestMethod]
    public void ShouldRunWorkflowForSelection_WithNullTag_ReturnsFalse()
    {
        Assert.IsFalse(_viewModel.ShouldRunWorkflowForSelection(null));
    }

    [TestMethod]
    public void ShouldRunWorkflowForSelection_WithNoHandler_ReturnsFalse()
    {
        _viewModel.OnWorkflowSelected = null;

        Assert.IsFalse(_viewModel.ShouldRunWorkflowForSelection("Premise"));
    }

    [TestMethod]
    public void SuppressWorkflowNavigation_ByDefault_IsFalse()
    {
        Assert.IsFalse(new WorkflowShellViewModel().SuppressWorkflowNavigation);
    }

    [TestMethod]
    public void StarEntries_ByDefault_IsEmpty()
    {
        Assert.IsNotNull(_viewModel.StarEntries);
        Assert.AreEqual(0, _viewModel.StarEntries.Count);
    }

    [TestMethod]
    public void ExitCommand_WhenInvoked_ClearsStarState()
    {
        _viewModel.StarEntries.Add(new WorkflowStarEntry { Label = "Premise" });
        _viewModel.SuppressWorkflowNavigation = true;
        _viewModel.OnStarsChanged = _ => Task.CompletedTask;

        _viewModel.ExitCommand.Execute(null);

        Assert.AreEqual(0, _viewModel.StarEntries.Count);
        Assert.IsFalse(_viewModel.SuppressWorkflowNavigation);
        Assert.IsNull(_viewModel.OnStarsChanged);
    }
}
