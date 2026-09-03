using System.Text.Json;
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
///     Collaborator #217 rule 5: ProblemBuilder offers free elements only. Free means on no
///     sheet and not in the trash. The target, the Story Problem, and the target's ancestors
///     are never Problem candidates. Rule 2 (a Scene may sit on many sheets) stays a hand
///     operation on the Structure tab; this run never offers the second placement.
/// </summary>
[TestClass]
public class CandidateSetTests
{
    private static StoryCADApi CreateApi() => new(
        Ioc.Default.GetRequiredService<OutlineService>(),
        Ioc.Default.GetRequiredService<ListData>(),
        Ioc.Default.GetRequiredService<ControlData>(),
        Ioc.Default.GetRequiredService<ToolsData>());

    private static (StoryModel Model, StoryCADApi Api, WorkflowRunner Runner) Arrange()
    {
        var model = new StoryModel();
        var api = CreateApi();
        api.CurrentModel = model;
        var workflow = WorkflowRegistry.Get("ProblemBuilder");
        return (model, api, new WorkflowRunner(model, workflow, api));
    }

    /// <summary>Bind an element on a new beat of the owner's sheet, the way OutlineService does.</summary>
    private static void Place(ProblemModel owner, StoryElement element)
    {
        owner.StructureBeats.Add(new StructureBeat("Beat", "row") { Guid = element.Uuid });
        if (element is ProblemModel child)
            child.BoundStructure = owner.Uuid;
    }

    private static List<Guid> OfferedGuids(WorkflowProxyBody body, string requestName) =>
        JsonDocument.Parse(body.Args[requestName]).RootElement
            .EnumerateArray()
            .Select(e => Guid.Parse(e.GetProperty("GUID").GetString()))
            .ToList();

    [TestMethod]
    public void GetCandidates_SceneInTrash_IsExcluded()
    {
        var (model, _, runner) = Arrange();
        var target = new ProblemModel("Target", model, null);
        var trash = new TrashCanModel(model, null);
        var deleted = new SceneModel("Deleted", model, trash.Node);
        var live = new SceneModel("Live", model, null);

        var candidates = runner.GetCandidates(target.Uuid);

        Assert.IsFalse(candidates.Scenes.Contains(deleted.Uuid), "a Scene under the trash is not offered");
        Assert.IsTrue(candidates.Scenes.Contains(live.Uuid), "a Scene outside the trash on no sheet is offered");
    }

    [TestMethod]
    public void GetCandidates_ScenePlacedOnAnotherProblemsSheet_IsExcluded()
    {
        var (model, _, runner) = Arrange();
        var target = new ProblemModel("Target", model, null);
        var other = new ProblemModel("Other", model, null);
        var placed = new SceneModel("Placed", model, null);
        Place(other, placed);

        var candidates = runner.GetCandidates(target.Uuid);

        Assert.IsFalse(candidates.Scenes.Contains(placed.Uuid),
            "a Scene on another Problem's sheet is placed; the second placement is a hand operation (rule 2)");
    }

    [TestMethod]
    public void GetCandidates_ScenePlacedOnTargetsOwnSheet_IsExcluded()
    {
        var (model, _, runner) = Arrange();
        var target = new ProblemModel("Target", model, null);
        var placed = new SceneModel("Placed", model, null);
        Place(target, placed);

        var candidates = runner.GetCandidates(target.Uuid);

        Assert.IsFalse(candidates.Scenes.Contains(placed.Uuid), "a Scene already on this sheet is not offered again");
    }

    [TestMethod]
    public void GetCandidates_SelfStoryProblemAndAncestors_AreExcluded()
    {
        var (model, _, runner) = Arrange();
        var overview = new OverviewModel("Overview", model, null);
        model.ExplorerView.Add(overview.Node);
        var storyProblem = new ProblemModel("Story problem", model, null) { ProblemCategory = "Story problem" };
        overview.StoryProblem = storyProblem.Uuid;

        // A holds B, B holds C. A is on no sheet, so only the ancestor walk removes it.
        var a = new ProblemModel("A", model, null);
        var b = new ProblemModel("B", model, null);
        var c = new ProblemModel("C", model, null);
        var free = new ProblemModel("Free", model, null);
        Place(a, b);
        Place(b, c);

        var candidates = runner.GetCandidates(c.Uuid);

        Assert.IsFalse(candidates.Problems.Contains(c.Uuid), "the target is never its own candidate");
        Assert.IsFalse(candidates.Problems.Contains(storyProblem.Uuid), "the Story Problem is never bound to a beat (rule 3)");
        Assert.IsFalse(candidates.Problems.Contains(b.Uuid), "B is placed on A's sheet");
        Assert.IsFalse(candidates.Problems.Contains(a.Uuid), "A on C's beat would close a cycle (rule 4)");
        Assert.IsTrue(candidates.Problems.Contains(free.Uuid), "a Problem on no sheet outside the chain is offered");
    }

    [TestMethod]
    public void GetCandidates_CycleAlreadyInFile_DoesNotHang()
    {
        var (model, _, runner) = Arrange();
        var a = new ProblemModel("A", model, null);
        var c = new ProblemModel("C", model, null);
        // A bad file from before the StoryCAD #1546 guard: each names the other as parent.
        a.BoundStructure = c.Uuid;
        c.BoundStructure = a.Uuid;

        var candidates = runner.GetCandidates(c.Uuid);

        Assert.IsFalse(candidates.Problems.Contains(a.Uuid), "the walk still removes the ancestor it reached");
    }

    [TestMethod]
    public void GetCandidates_FreeSceneAndFreeProblem_AreIncluded()
    {
        var (model, _, runner) = Arrange();
        var target = new ProblemModel("Target", model, null);
        var scene = new SceneModel("Free scene", model, null);
        var problem = new ProblemModel("Free problem", model, null);

        var candidates = runner.GetCandidates(target.Uuid);

        Assert.IsTrue(candidates.Scenes.Contains(scene.Uuid));
        Assert.IsTrue(candidates.Problems.Contains(problem.Uuid));
        Assert.IsTrue(candidates.Contains(scene.Uuid) && candidates.Contains(problem.Uuid));
    }

    [TestMethod]
    public void BuildWorkflowRequestBody_ProblemBuilder_OffersCandidatesOnly()
    {
        var (model, _, runner) = Arrange();
        var target = new ProblemModel("Target", model, null);
        var other = new ProblemModel("Other", model, null);
        var placed = new SceneModel("Placed", model, null);
        var free = new SceneModel("Free", model, null);
        Place(other, placed);

        var body = runner.BuildWorkflowRequestBody(
            new Dictionary<string, StoryElement> { ["Problem"] = target });

        CollectionAssert.AreEquivalent(new List<Guid> { free.Uuid }, OfferedGuids(body, "SceneChoices"),
            "SceneChoices holds the free Scene and nothing else");
        CollectionAssert.AreEquivalent(new List<Guid> { other.Uuid }, OfferedGuids(body, "ProblemChoices"),
            "ProblemChoices holds the free Problem; the target is not offered to itself");
    }

    [TestMethod]
    public void BuildWorkflowRequestBody_CollectionWithoutFreeElementsFor_OffersEveryElement()
    {
        var model = new StoryModel();
        var api = CreateApi();
        api.CurrentModel = model;
        var other = new ProblemModel("Other", model, null);
        var placed = new SceneModel("Placed", model, null);
        var free = new SceneModel("Free", model, null);
        Place(other, placed);
        var workflow = new Workflow("Plain", "Plain", "t", "t", new WorkflowIO
        {
            CollectionInputs = new List<CollectionInput>
            {
                new()
                {
                    RequestName = "SceneChoices",
                    ElementType = StoryItemType.Scene,
                    Projection = ElementProjection.IdAndName
                }
            }
        });

        var body = new WorkflowRunner(model, workflow, api).BuildWorkflowRequestBody(
            new Dictionary<string, StoryElement>());

        CollectionAssert.AreEquivalent(new List<Guid> { placed.Uuid, free.Uuid }, OfferedGuids(body, "SceneChoices"),
            "a collection that does not declare FreeElementsFor still gets every element of the type");
    }
}
