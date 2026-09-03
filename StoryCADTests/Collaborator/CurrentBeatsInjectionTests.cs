using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCADLib.Models;
using StoryCADLib.Models.Tools;
using StoryCADLib.Services.API;
using StoryCADLib.Services.Outline;
using StoryCADLib.ViewModels;
using StoryCADLib.ViewModels.Tools;
using StoryCollaborator;
using StoryCollaborator.Workflows;

#nullable disable

namespace StoryCADTests.Collaborator;

/// <summary>
///     Collaborator #217 section 5.4. The template told the model to keep filled beats and to
///     return the rows of a sheet the Problem arrived with, and showed it neither. CurrentBeats
///     is that sheet: "none" without one, else the title and one line per beat with its binding.
/// </summary>
[TestClass]
public class CurrentBeatsInjectionTests
{
    private static StoryCADApi CreateApi() => new(
        Ioc.Default.GetRequiredService<OutlineService>(),
        Ioc.Default.GetRequiredService<ListData>(),
        Ioc.Default.GetRequiredService<ControlData>(),
        Ioc.Default.GetRequiredService<ToolsData>());

    private static (StoryModel Model, WorkflowRunner Runner) Arrange(Workflow workflow = null)
    {
        var model = new StoryModel();
        var api = CreateApi();
        api.CurrentModel = model;
        workflow ??= new Workflow("ProblemBuilder", "Problem Builder", "test", StoryItemType.Problem);
        return (model, new WorkflowRunner(model, workflow, api));
    }

    [TestMethod]
    public void EnrichWithCurrentBeats_NoSheet_WritesNone()
    {
        var (model, runner) = Arrange();
        var problem = new ProblemModel("Fresh", model, null);
        var args = new Dictionary<string, string>();

        runner.EnrichWithCurrentBeats(args, problem.Uuid);

        Assert.AreEqual("none", args["CurrentBeats"], "the template's fallback rule keys on the exact text none");
    }

    [TestMethod]
    public void EnrichWithCurrentBeats_Sheet_ListsEachBeatWithItsBinding()
    {
        var (model, runner) = Arrange();
        var problem = new ProblemModel("Subplot", model, null) { StructureTitle = "Save The Cat (Mini)" };
        var scene = new SceneModel("The lighthouse at dawn", model, null);
        problem.StructureBeats.Add(new StructureBeat("Opening Image", "") { Guid = scene.Uuid });
        problem.StructureBeats.Add(new StructureBeat("Catalyst", ""));
        var args = new Dictionary<string, string>();

        runner.EnrichWithCurrentBeats(args, problem.Uuid);

        Assert.AreEqual(
            "Sheet: Save The Cat (Mini)\n1. Opening Image: The lighthouse at dawn\n2. Catalyst: empty",
            args["CurrentBeats"]);
    }

    [TestMethod]
    public void EnrichWithCurrentBeats_BeatDescription_FollowsOnAnIndentedLine()
    {
        var (model, runner) = Arrange();
        var problem = new ProblemModel("Subplot", model, null);
        problem.StructureBeats.Add(new StructureBeat("Catalyst", "the turn"));
        var args = new Dictionary<string, string>();

        runner.EnrichWithCurrentBeats(args, problem.Uuid);

        Assert.AreEqual("Sheet: (untitled)\n1. Catalyst: empty\n  the turn", args["CurrentBeats"]);
    }

    [TestMethod]
    public void ApplyDeclaredInjections_RegisteredProblemBuilder_InjectsCurrentBeatsForTheGatheredProblem()
    {
        var registered = WorkflowRegistry.All.Single(w => w.Label == "ProblemBuilder");
        var (model, runner) = Arrange(registered);
        var problem = new ProblemModel("Subplot", model, null);
        var args = new Dictionary<string, string>();

        runner.ApplyDeclaredInjections(args, new Dictionary<string, StoryElement> { ["Problem"] = problem });

        Assert.IsTrue(registered.InjectsCurrentBeats, "ProblemBuilder declares the injection");
        Assert.AreEqual("none", args["CurrentBeats"]);
    }

    [TestMethod]
    public void ApplyDeclaredInjections_NoGatheredProblem_SkipsCurrentBeats()
    {
        var registered = WorkflowRegistry.All.Single(w => w.Label == "ProblemBuilder");
        var (_, runner) = Arrange(registered);
        var args = new Dictionary<string, string>();

        runner.ApplyDeclaredInjections(args, new Dictionary<string, StoryElement>());

        Assert.IsFalse(args.ContainsKey("CurrentBeats"),
            "with no target the placeholder merges to empty on the Worker and the template falls back to the catalog");
    }
}
