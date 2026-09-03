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
///     Collaborator #217 section 5.5. One planner, two consumers: the Review pane renders the
///     plan and ApplyBeatSheetMerge executes it, so the pane cannot show a bind the apply then
///     refuses. The old one-line display hid which Scene went to which beat until after Accept.
/// </summary>
[TestClass]
public class BeatSheetPlanTests
{
    private static StoryCADApi CreateApi() => new(
        Ioc.Default.GetRequiredService<OutlineService>(),
        Ioc.Default.GetRequiredService<ListData>(),
        Ioc.Default.GetRequiredService<ControlData>(),
        Ioc.Default.GetRequiredService<ToolsData>());

    private static (StoryModel Model, WorkflowRunner Runner) Arrange()
    {
        var model = new StoryModel();
        var api = CreateApi();
        api.CurrentModel = model;
        var workflow = new Workflow("ProblemBuilder", "Problem Builder", "test", StoryItemType.Problem)
        {
            CreatesScenesForBeats = true
        };
        return (model, new WorkflowRunner(model, workflow, api));
    }

    /// <summary>
    ///     A sheet of five beats, the first filled, plus one free Scene and one Scene placed on
    ///     another Problem. The six-row proposal exercises every outcome in order:
    ///     Keep, Bind, Create, Refuse, Empty, Drop.
    /// </summary>
    private static (ProblemModel Target, SceneModel Kept, SceneModel Free, SceneModel Placed, List<BeatInfo> Proposal)
        EveryOutcome(StoryModel model)
    {
        var target = new ProblemModel("Target", model, null) { StructureTitle = "Save The Cat (Mini)" };
        var kept = new SceneModel("Kept", model, null);
        var free = new SceneModel("Free", model, null);
        var placed = new SceneModel("Placed", model, null);
        var other = new ProblemModel("Other", model, null);
        other.StructureBeats.Add(new StructureBeat("Row", "") { Guid = placed.Uuid });

        target.StructureBeats.Add(new StructureBeat("Opening Image", "") { Guid = kept.Uuid });
        target.StructureBeats.Add(new StructureBeat("Theme Stated", ""));
        target.StructureBeats.Add(new StructureBeat("Set-Up", ""));
        target.StructureBeats.Add(new StructureBeat("Catalyst", ""));
        target.StructureBeats.Add(new StructureBeat("Debate", ""));

        var proposal = new List<BeatInfo>
        {
            new("Opening Image", "", AssignedElement: free.Uuid),
            new("Theme Stated", "", AssignedElement: free.Uuid),
            new("Set-Up", "", SceneName: "Stub"),
            new("Catalyst", "", AssignedElement: placed.Uuid),
            new("Debate", ""),
            new("Extra", "")
        };
        return (target, kept, free, placed, proposal);
    }

    [TestMethod]
    public void PlanBeatSheetMerge_EveryOutcome_MatchesWhatApplyDoes()
    {
        var (model, runner) = Arrange();
        var (target, kept, free, placed, proposal) = EveryOutcome(model);

        var plan = runner.PlanBeatSheetMerge(target.Uuid, proposal);

        CollectionAssert.AreEqual(
            new[]
            {
                BeatRowOutcome.Keep, BeatRowOutcome.Bind, BeatRowOutcome.Create,
                BeatRowOutcome.Refuse, BeatRowOutcome.Empty, BeatRowOutcome.Drop
            },
            plan.Select(r => r.Outcome).ToArray());
        Assert.IsTrue(plan.All(r => !r.InstallsBeat), "a present sheet installs no beat");

        runner.ApplyBeatSheetMerge(target.Uuid, proposal, new WorkflowResult());

        var stub = model.StoryElements.OfType<SceneModel>().Single(s => s.Name == "Stub");
        Assert.AreEqual(5, target.StructureBeats.Count, "Drop: the sheet did not grow");
        Assert.AreEqual(kept.Uuid, target.StructureBeats[0].Guid, "Keep: the filled beat is untouched");
        Assert.AreEqual(free.Uuid, target.StructureBeats[1].Guid, "Bind: the free Scene is bound");
        Assert.AreEqual(stub.Uuid, target.StructureBeats[2].Guid, "Create: the stub is created and bound");
        Assert.AreEqual(Guid.Empty, target.StructureBeats[3].Guid, "Refuse: the placed Scene is not bound");
        Assert.AreEqual(Guid.Empty, target.StructureBeats[4].Guid, "Empty: nothing to bind");
        Assert.AreEqual(4, model.StoryElements.OfType<SceneModel>().Count(), "one Scene was created, no other");
        Assert.AreEqual(placed.Uuid, model.StoryElements.OfType<ProblemModel>().Single(p => p.Name == "Other").StructureBeats[0].Guid,
            "the other sheet keeps its Scene");
    }

    [TestMethod]
    public void FormatBeatSheetDisplay_PresentSheet_RendersSummaryAndOneLinePerRow()
    {
        var (model, runner) = Arrange();
        var (target, _, _, placed, proposal) = EveryOutcome(model);

        var text = runner.FormatBeatSheetDisplay(target.Uuid, proposal);

        Assert.AreEqual(
            "6 beats: 1 kept, 1 bound, 1 new scenes, 1 empty, 1 refused, 1 dropped\n" +
            "1. Opening Image: keeps Kept\n" +
            "2. Theme Stated: binds Free (Scene)\n" +
            "3. Set-Up: new Scene \"Stub\"\n" +
            $"4. Catalyst: {placed.Uuid} is not a candidate, stays empty\n" +
            "5. Debate: stays empty\n" +
            "6. Extra: not applied, sheet has 5 rows",
            text);
    }

    [TestMethod]
    public void FormatBeatSheetDisplay_EmptyProblem_SaysNewBeats()
    {
        var (model, runner) = Arrange();
        var target = new ProblemModel("Fresh", model, null);
        var free = new ProblemModel("Free problem", model, null);
        var proposal = new List<BeatInfo>
        {
            new("Goal", "what the character wants", AssignedElement: free.Uuid),
            new("Outcome", "how it lands")
        };

        var text = runner.FormatBeatSheetDisplay(target.Uuid, proposal);

        Assert.AreEqual(
            "2 new beats: 0 kept, 1 bound, 0 new scenes, 1 empty\n" +
            "1. Goal: binds Free problem (Problem)\n" +
            "2. Outcome: stays empty",
            text);
    }

    [TestMethod]
    public void ApplyBeatSheetMerge_PlacedGuid_IsRefusedWithStatus()
    {
        var (model, runner) = Arrange();
        var target = new ProblemModel("Target", model, null);
        var other = new ProblemModel("Other", model, null);
        var placed = new SceneModel("Placed", model, null);
        other.StructureBeats.Add(new StructureBeat("Row", "") { Guid = placed.Uuid });
        target.StructureBeats.Add(new StructureBeat("Catalyst", ""));
        var result = new WorkflowResult();

        runner.ApplyBeatSheetMerge(target.Uuid, new List<BeatInfo> { new("Catalyst", "", AssignedElement: placed.Uuid) }, result);

        Assert.AreEqual(Guid.Empty, target.StructureBeats[0].Guid);
        Assert.IsTrue(result.StatusMessages.Any(m => m.Contains("not in candidate set")), string.Join(" | ", result.StatusMessages));
    }

    [TestMethod]
    public void ApplyBeatSheetMerge_AncestorGuid_IsRefusedAndMakesNoCycle()
    {
        var (model, runner) = Arrange();
        var parent = new ProblemModel("Parent", model, null);
        var target = new ProblemModel("Target", model, null);
        parent.StructureBeats.Add(new StructureBeat("Act II", "") { Guid = target.Uuid });
        target.BoundStructure = parent.Uuid;
        target.StructureBeats.Add(new StructureBeat("Catalyst", ""));
        var result = new WorkflowResult();

        runner.ApplyBeatSheetMerge(target.Uuid, new List<BeatInfo> { new("Catalyst", "", AssignedElement: parent.Uuid) }, result);

        Assert.AreEqual(Guid.Empty, target.StructureBeats[0].Guid, "the parent is not bound below itself (rule 4)");
        Assert.AreEqual(Guid.Empty, parent.BoundStructure, "the parent keeps no sheet parent");
        Assert.IsTrue(result.StatusMessages.Any(m => m.Contains("not in candidate set")));
    }

    [TestMethod]
    public void PlanBeatSheetMerge_SameCandidateTwice_BindsOnceAndRefusesTheSecond()
    {
        var (model, runner) = Arrange();
        var target = new ProblemModel("Target", model, null);
        var free = new SceneModel("Free", model, null);
        target.StructureBeats.Add(new StructureBeat("Setup", ""));
        target.StructureBeats.Add(new StructureBeat("Payoff", ""));
        var proposal = new List<BeatInfo>
        {
            new("Setup", "", AssignedElement: free.Uuid),
            new("Payoff", "", AssignedElement: free.Uuid)
        };

        var plan = runner.PlanBeatSheetMerge(target.Uuid, proposal);
        var result = new WorkflowResult();
        runner.ApplyBeatSheetMerge(target.Uuid, proposal, result);

        Assert.AreEqual(BeatRowOutcome.Bind, plan[0].Outcome);
        Assert.AreEqual(BeatRowOutcome.Refuse, plan[1].Outcome, "a sheet is a linear order; one placement per Scene");
        Assert.AreEqual(free.Uuid, target.StructureBeats[0].Guid);
        Assert.AreEqual(Guid.Empty, target.StructureBeats[1].Guid);
        Assert.IsTrue(result.StatusMessages.Any(m => m.Contains("already used on this sheet")));
    }
}
