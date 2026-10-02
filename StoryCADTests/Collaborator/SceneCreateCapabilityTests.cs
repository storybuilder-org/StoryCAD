using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCADLib.Models;
using StoryCADLib.Models.Tools;
using StoryCADLib.Services.API;
using StoryCADLib.Services.Outline;
using StoryCADLib.ViewModels;
using StoryCADLib.ViewModels.Tools;
using StoryCollaborator;
using StoryCollaborator.Models;
using StoryCollaborator.Workflows;

#nullable disable

namespace StoryCADTests.Collaborator;

/// <summary>
///     Collaborator #77 step 4: Scene creation is a workflow capability, not a label.
///     Before this change <c>allowSceneCreate</c> was <c>Label == "BeatScenes"</c>, so any
///     new workflow that set <c>BeatInfo.SceneName</c> silently created nothing.
/// </summary>
[TestClass]
public class SceneCreateCapabilityTests
{
    private static StoryCADApi CreateApi() => new(
        Ioc.Default.GetRequiredService<OutlineService>(),
        Ioc.Default.GetRequiredService<ListData>(),
        Ioc.Default.GetRequiredService<ControlData>(),
        Ioc.Default.GetRequiredService<ToolsData>());

    private static (StoryModel Model, ProblemModel Problem, WorkflowRunner Runner) Arrange(Workflow workflow)
    {
        var model = new StoryModel();
        var problem = new ProblemModel("Complication", model, null);
        var api = CreateApi();
        api.CurrentModel = model;
        return (model, problem, new WorkflowRunner(model, workflow, api));
    }

    private static Workflow SceneCreatingWorkflow(string label) => new(
        label, label, "test", StoryItemType.Problem)
    {
        CreatesScenesForBeats = true
    };

    [TestMethod]
    public void ApplyBeatSheetMerge_WorkflowThatCreatesScenes_CreatesTheScene()
    {
        var (model, problem, runner) = Arrange(SceneCreatingWorkflow("ProblemBuilder"));

        runner.ApplyBeatSheetMerge(
            problem.Uuid,
            new List<BeatInfo> { new("Setup", "the opening", SceneName: "Arrival at the gate") },
            new WorkflowResult());

        var scenes = model.StoryElements.OfType<SceneModel>().ToList();
        Assert.AreEqual(1, scenes.Count,
            "a workflow declaring CreatesScenesForBeats must create the Scene");
        Assert.AreEqual("Arrival at the gate", scenes[0].Name);
    }

    [TestMethod]
    public void ApplyBeatSheetMerge_WorkflowThatDoesNotCreateScenes_CreatesNothing()
    {
        var workflow = new Workflow("NoCreate", "No Create", "test", StoryItemType.Problem);
        var (model, problem, runner) = Arrange(workflow);

        runner.ApplyBeatSheetMerge(
            problem.Uuid,
            new List<BeatInfo> { new("Setup", "the opening", SceneName: "Arrival at the gate") },
            new WorkflowResult());

        Assert.AreEqual(0, model.StoryElements.OfType<SceneModel>().Count(),
            "a workflow without the capability must not create a Scene");
    }

    [TestMethod]
    public void RegisteredProblemBuilder_DeclaresTheCapability()
    {
        var problemBuilder = WorkflowRegistry.All
            .FirstOrDefault(w => w.Label == "ProblemBuilder");

        Assert.IsNotNull(problemBuilder, "ProblemBuilder is the surviving Problem surface");
        Assert.IsTrue(problemBuilder.CreatesScenesForBeats,
            "#211 deleted BeatScenes; ProblemBuilder carries scene creation on the flag");
    }
}
