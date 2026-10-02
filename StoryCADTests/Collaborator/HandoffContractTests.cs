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
///     Collaborator #208 handoff, product law for #77:
///     stub carries SceneType and CastMembers. Collaborator #217 (StoryCAD #1546 rule 4):
///     any Problem's beats may hold free Scenes and free Problems together.
/// </summary>
[TestClass]
public class HandoffContractTests
{
    private static StoryCADApi CreateApi() => new(
        Ioc.Default.GetRequiredService<OutlineService>(),
        Ioc.Default.GetRequiredService<ListData>(),
        Ioc.Default.GetRequiredService<ControlData>(),
        Ioc.Default.GetRequiredService<ToolsData>());

    private static Workflow Builder() => new(
        "ProblemBuilder", "Problem Builder", "test", StoryItemType.Problem)
    {
        CreatesScenesForBeats = true
    };

    private static (StoryModel Model, ProblemModel Problem, WorkflowRunner Runner)
        Arrange(string category)
    {
        var model = new StoryModel();
        var problem = new ProblemModel("Under test", model, null) { ProblemCategory = category };
        var api = CreateApi();
        api.CurrentModel = model;
        return (model, problem, new WorkflowRunner(model, Builder(), api));
    }

    [TestMethod]
    public void CreatedStub_CarriesSceneType()
    {
        var (model, problem, runner) = Arrange("Complication");

        runner.ApplyBeatSheetMerge(problem.Uuid,
            new List<BeatInfo> { new("Catalyst", "the turn", SceneName: "The letter arrives",
                SceneType: "Crisis scene") },
            new WorkflowResult());

        var scene = model.StoryElements.OfType<SceneModel>().Single();
        Assert.AreEqual("Crisis scene", scene.SceneType,
            "the handoff requires SceneType on a created stub");
    }

    [TestMethod]
    public void CreatedStub_CarriesResolvableCastMembers()
    {
        var (model, problem, runner) = Arrange("Complication");
        var hero = new CharacterModel("Hero", model, null);

        runner.ApplyBeatSheetMerge(problem.Uuid,
            new List<BeatInfo> { new("Catalyst", "the turn", SceneName: "The letter arrives",
                SceneCast: new List<Guid> { hero.Uuid }) },
            new WorkflowResult());

        var scene = model.StoryElements.OfType<SceneModel>().Single();
        CollectionAssert.Contains(scene.CastMembers.ToList(), hero.Uuid,
            "characters named in the description and resolvable must join the cast");
    }

    [TestMethod]
    public void CreatedStub_SkipsUnresolvableCastMembers()
    {
        var (model, problem, runner) = Arrange("Complication");

        runner.ApplyBeatSheetMerge(problem.Uuid,
            new List<BeatInfo> { new("Catalyst", "the turn", SceneName: "The letter arrives",
                SceneCast: new List<Guid> { Guid.NewGuid() }) },
            new WorkflowResult());

        var scene = model.StoryElements.OfType<SceneModel>().Single();
        Assert.AreEqual(0, scene.CastMembers.Count,
            "an unresolved name is skipped, not invented");
    }

    [TestMethod]
    public void SubproblemBeat_BindsAFreeProblem()
    {
        var (model, problem, runner) = Arrange("Complication");
        var other = new ProblemModel("Another problem", model, null);
        problem.StructureBeats.Add(new StructureBeat("Catalyst", "the turn"));

        runner.ApplyBeatSheetMerge(problem.Uuid,
            new List<BeatInfo> { new("Catalyst", "the turn", AssignedElement: other.Uuid) },
            new WorkflowResult());

        Assert.AreEqual(other.Uuid, problem.StructureBeats[0].Guid,
            "#217: a subproblem's beats take free Problems as well as Scenes (StoryCAD #1546 rule 4)");
    }

    [TestMethod]
    public void StoryProblemBeat_AcceptsAProblemBinding()
    {
        var (model, problem, runner) = Arrange("Story problem");
        var other = new ProblemModel("A complication", model, null);
        problem.StructureBeats.Add(new StructureBeat("Act I", "setup"));

        runner.ApplyBeatSheetMerge(problem.Uuid,
            new List<BeatInfo> { new("Act I", "setup", AssignedElement: other.Uuid) },
            new WorkflowResult());

        Assert.AreEqual(other.Uuid, problem.StructureBeats[0].Guid,
            "the Story Problem's beats may hold other Problems");
    }
}
