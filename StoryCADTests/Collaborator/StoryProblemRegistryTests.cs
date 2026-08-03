using StoryCADLib.Models;
using StoryCollaborator.Workflows;

namespace StoryCADTests.Collaborator;

/// <summary>
/// Registry contract for Story Problem gather links (Collaborator #118).
/// </summary>
[TestClass]
public class StoryProblemRegistryTests
{
    [TestMethod]
    public void AllWorkflows_HavePrimaryElementType_ForNavGrouping()
    {
        // #129: nav menu groups by PrimaryElementType; Unknown would orphan a workflow.
        foreach (var workflow in WorkflowRegistry.All)
        {
            Assert.AreNotEqual(StoryCADLib.Models.StoryItemType.Unknown, workflow.PrimaryElementType,
                $"Workflow '{workflow.Label}' has no PrimaryElementType.");
        }
    }

    [TestMethod]
    public void StoryProblem_OptionalInputs_WriteStructuralLinksOnGather()
    {
        var workflow = WorkflowRegistry.Get("StoryProblem");
        Assert.IsNotNull(workflow);

        var io = workflow.GetIO();
        Assert.IsNotNull(io);

        var problem = io.OptionalInputs.Single(i => i.ElementLabel == "Problem");
        var protagonist = io.OptionalInputs.Single(i => i.ElementLabel == "Protagonist");
        var antagonist = io.OptionalInputs.Single(i => i.ElementLabel == "Antagonist");

        Assert.AreEqual("Overview.StoryProblem", problem.ReferencedElementLabel);
        Assert.AreEqual(StoryItemType.Problem, problem.ElementType);

        Assert.AreEqual("Problem.Protagonist", protagonist.ReferencedElementLabel);
        Assert.AreEqual(StoryItemType.Character, protagonist.ElementType);

        Assert.AreEqual("Problem.Antagonist", antagonist.ReferencedElementLabel);
        Assert.AreEqual(StoryItemType.Character, antagonist.ElementType);
    }

    [TestMethod]
    public void StoryProblem_GatherOrder_ProblemBeforeCast()
    {
        var io = WorkflowRegistry.Get("StoryProblem")!.GetIO();
        var labels = io.OptionalInputs.Select(i => i.ElementLabel).ToList();

        Assert.IsTrue(labels.IndexOf("Problem") < labels.IndexOf("Protagonist"));
        Assert.IsTrue(labels.IndexOf("Protagonist") < labels.IndexOf("Antagonist"));
    }

    [TestMethod]
    public void StoryProblem_RequiredOverview_HasNoStoryProblemReferenceCycle()
    {
        var io = WorkflowRegistry.Get("StoryProblem")!.GetIO();
        var overview = io.RequiredInputs.Single(i => i.ElementLabel == "Overview");

        Assert.AreEqual(StoryItemType.StoryOverview, overview.ElementType);
        Assert.IsTrue(string.IsNullOrEmpty(overview.ReferencedElementLabel));
    }

    [TestMethod]
    public void StoryProblem_ProblemOutputs_IncludeResolutionFields()
    {
        var io = WorkflowRegistry.Get("StoryProblem")!.GetIO();
        var problemOut = io.Outputs.Single(o => o.ElementLabel == "Problem");
        var props = problemOut.PropertiesToUpdate.Select(p => p.Property).ToHashSet();

        Assert.IsTrue(props.Contains("Outcome"), "Outcome (Resolution tab)");
        Assert.IsTrue(props.Contains("Method"), "Method (Resolution tab)");
        Assert.IsTrue(props.Contains("Theme"), "Theme (Resolution tab)");
        Assert.IsTrue(props.Contains("Premise"), "Premise stays on Problem outputs");
    }
}
