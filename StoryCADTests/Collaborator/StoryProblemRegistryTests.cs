using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCADLib.Models;
using StoryCADLib.Models.Tools;
using StoryCADLib.Services.API;
using StoryCADLib.Services.Outline;
using StoryCADLib.ViewModels;
using StoryCollaborator;
using StoryCollaborator.Workflows;

namespace StoryCADTests.Collaborator;

/// <summary>
/// Registry contract for Story Problem gather links (Collaborator #118)
/// and immediate ProblemCategory write when Overview.StoryProblem is set.
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
        Assert.IsFalse(props.Contains("ProblemCategory"),
            "ProblemCategory is link-time structural write, not LLM pending");
    }

    [TestMethod]
    public async Task StoryProblemCategory_ListValue_WritesViaApiImmediately()
    {
        // Contract: linking StoryProblem sets this exact Lists.json value on the Problem
        // (Collaborator.ApplyStoryProblemCategory → UpdateElementProperty, not pending).
        var api = new StoryCADApi(
            Ioc.Default.GetRequiredService<OutlineService>(),
            Ioc.Default.GetRequiredService<ListData>(),
            Ioc.Default.GetRequiredService<ControlData>(),
            Ioc.Default.GetRequiredService<ToolsData>());
        var create = await api.CreateEmptyOutline("Category Link", "Author", "0");
        Assert.IsTrue(create.IsSuccess);

        var model = api.CurrentModel!;
        var overview = model.StoryElements.First(e => e.ElementType == StoryItemType.StoryOverview);
        var add = api.AddElement(StoryItemType.Problem, overview.Uuid.ToString(), "Do Not Open the Box");
        Assert.IsTrue(add.IsSuccess);
        var problem = (ProblemModel)model.StoryElements.StoryElementGuids[add.Payload];
        Assert.IsTrue(string.IsNullOrEmpty(problem.ProblemCategory));

        var write = api.UpdateElementProperty(
            problem.Uuid, "ProblemCategory", StoryCollaborator.Collaborator.StoryProblemCategoryListValue);
        Assert.IsTrue(write.IsSuccess);
        Assert.AreEqual("Story problem", problem.ProblemCategory);
        Assert.AreEqual(
            StoryCollaborator.Collaborator.StoryProblemCategoryListValue, problem.ProblemCategory);
    }
}
