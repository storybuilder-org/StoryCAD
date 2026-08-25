using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCollaborator;
using StoryCollaborator.Workflows;

#nullable disable

namespace StoryCADTests.Collaborator;

/// <summary>
///     Collaborator #77 step 6. ProblemBuilder needs ProblemCategory set: it picks the beat
///     sheet class. Nothing sets the value on a ProblemBuilder run, so the user sets it first.
///     Unlike BeatScenes, ProblemBuilder runs on the Story Problem too.
/// </summary>
[TestClass]
public class ProblemCategoryGateTests
{
    [TestMethod]
    public void ValidateProblemCategory_WhenBlank_ReturnsMessage()
    {
        Assert.IsNotNull(WorkflowRunner.ValidateProblemCategory(string.Empty),
            "an empty category must stop the run");
    }

    [TestMethod]
    public void ValidateProblemCategory_WhenStoryProblem_IsAllowed()
    {
        Assert.IsNull(WorkflowRunner.ValidateProblemCategory("Story problem"),
            "ProblemBuilder runs on any Problem, including the story problem");
    }

    [TestMethod]
    public void ValidateProblemCategory_WhenSubproblem_IsAllowed()
    {
        Assert.IsNull(WorkflowRunner.ValidateProblemCategory("Complication"));
    }

    [TestMethod]
    public void Workflow_CanDeclareTheCategoryRequirement()
    {
        var workflow = new Workflow("ProblemBuilder", "Problem Builder", "test",
            StoryCADLib.Models.StoryItemType.Problem)
        {
            RequiresProblemCategory = true
        };

        Assert.IsTrue(workflow.RequiresProblemCategory);
    }
}
