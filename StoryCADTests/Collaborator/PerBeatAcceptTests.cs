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
using StoryCollaborator.Services;
using StoryCollaborator.Workflows;

#nullable disable

namespace StoryCADTests.Collaborator;

/// <summary>
///     Collaborator #217 section 5.7. The beats proposal becomes one Review pane row per beat
///     that binds or creates, each its own Accept or Skip, and the run result carries the
///     model's field_states so classify reads intent instead of falling through to compare.
/// </summary>
[TestClass]
public class PerBeatAcceptTests
{
    private static readonly PropertySpec BeatsSpec =
        new("StructureBeats", WriteVia.BeatSheet, JsonKey: "beats");

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
    private static (ProblemModel Target, SceneModel Kept, SceneModel Free, List<BeatInfo> Proposal)
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
        return (target, kept, free, proposal);
    }

    private static WorkflowResult SheetResult(ProblemModel target, List<BeatInfo> proposal)
    {
        var result = WorkflowResult.Succeeded();
        var update = new PendingUpdate("Problem", target.Uuid, BeatsSpec, proposal);
        result.PendingUpdates.Add(update);
        result.UpdatedProperties[update.Key] = "6 beats";
        return result;
    }

    private static WorkflowResult Slice(params PendingUpdate[] updates)
    {
        var slice = WorkflowResult.Succeeded();
        foreach (var u in updates)
            slice.PendingUpdates.Add(u);
        return slice;
    }

    [TestMethod]
    public void ExpandBeatSheetUpdates_EveryOutcome_OneRowPerBindOrCreate()
    {
        var (model, runner) = Arrange();
        var (target, _, free, proposal) = EveryOutcome(model);
        var result = SheetResult(target, proposal);

        runner.ExpandBeatSheetUpdates(result);

        Assert.AreEqual(2, result.PendingUpdates.Count, "Keep, Refuse, Empty and Drop make no row");
        var bind = result.PendingUpdates[0];
        var create = result.PendingUpdates[1];
        Assert.AreEqual("Problem.StructureBeats[1]", bind.Key);
        Assert.AreEqual("Problem.StructureBeats[2]", create.Key);
        Assert.AreEqual("Beat 2: Theme Stated", bind.DisplayNameOverride);
        Assert.AreEqual("Beat 3: Set-Up", create.DisplayNameOverride);

        var bindValue = (BeatRowValue)bind.Value;
        Assert.AreEqual(free.Uuid, bindValue.BindGuid);
        Assert.AreEqual("Free", bindValue.ElementName);
        Assert.AreEqual("Scene", bindValue.ElementType);
        Assert.AreSame(proposal, bindValue.Sheet, "the row keeps the whole proposal for a sheet install");

        var createValue = (BeatRowValue)create.Value;
        Assert.IsNull(createValue.BindGuid);
        Assert.AreEqual("Stub", createValue.Row.SceneName);

        Assert.IsFalse(result.UpdatedProperties.ContainsKey("Problem.StructureBeats"),
            "the sheet-level display entry is gone");
        Assert.IsTrue(result.UpdatedProperties.ContainsKey(bind.Key));
        StringAssert.Contains(string.Join("\n", result.StatusMessages),
            "6 beats: 1 kept, 1 bound, 1 new scene, 1 empty, 1 refused, 1 dropped");
        Assert.IsTrue(result.StatusMessages.Any(m => m.Contains("not in candidate set")), "Refuse keeps its status line");
        Assert.IsTrue(result.StatusMessages.Any(m => m.Contains("not applied, sheet has 5 rows")), "Drop keeps its status line");
    }

    [TestMethod]
    public void ValueDisplay_BeatRow_RendersWhatAcceptWillDo()
    {
        var (model, runner) = Arrange();
        var (target, _, _, proposal) = EveryOutcome(model);
        var result = SheetResult(target, proposal);
        runner.ExpandBeatSheetUpdates(result);

        Assert.AreEqual("binds Free (Scene)", ValueDisplay.Format(result.PendingUpdates[0].Value));
        Assert.AreEqual("new Scene \"Stub\"", ValueDisplay.Format(result.PendingUpdates[1].Value));
    }

    [TestMethod]
    public void ClassifyScalarUpdates_BeatRow_IsFillWithEmptyCurrent()
    {
        var (model, runner) = Arrange();
        var (target, _, _, proposal) = EveryOutcome(model);
        var result = SheetResult(target, proposal);
        runner.ExpandBeatSheetUpdates(result);

        runner.ClassifyScalarUpdates(result, null, "ProblemBuilder");

        Assert.AreEqual(2, result.PendingUpdates.Count);
        foreach (var row in result.PendingUpdates)
        {
            Assert.AreEqual(UpdateKind.Fill, row.Kind, row.Key);
            Assert.AreEqual("empty", row.CurrentDisplay, row.Key);
            Assert.IsTrue(row.AcceptAllMayApply, "Accept All and Accept Free Remaining include the row");
        }
        Assert.AreEqual("binds Free (Scene)", result.UpdatedProperties["Problem.StructureBeats[1]"]);
    }

    [TestMethod]
    public void ApplyUpdates_OneBeatRow_BindsOnlyThatRow()
    {
        var (model, runner) = Arrange();
        var (target, kept, free, proposal) = EveryOutcome(model);
        var result = SheetResult(target, proposal);
        runner.ExpandBeatSheetUpdates(result);
        var bind = result.PendingUpdates[0];
        var create = result.PendingUpdates[1];
        var gathered = new Dictionary<string, StoryElement>();

        var applied = runner.ApplyUpdates(Slice(bind), gathered);

        Assert.AreEqual(1, applied);
        Assert.AreEqual(kept.Uuid, target.StructureBeats[0].Guid, "the filled beat is untouched");
        Assert.AreEqual(free.Uuid, target.StructureBeats[1].Guid, "the accepted row is bound");
        Assert.AreEqual(Guid.Empty, target.StructureBeats[2].Guid, "the other row waits for its own Accept");
        Assert.AreEqual(3, model.StoryElements.OfType<SceneModel>().Count(), "no stub was created");

        applied = runner.ApplyUpdates(Slice(create), gathered);

        Assert.AreEqual(1, applied);
        var stub = model.StoryElements.OfType<SceneModel>().Single(s => s.Name == "Stub");
        Assert.AreEqual(stub.Uuid, target.StructureBeats[2].Guid, "the second Accept creates and binds its stub");
        Assert.AreEqual(5, target.StructureBeats.Count, "the sheet did not grow");
    }

    [TestMethod]
    public void ApplyUpdates_BeatRowOnProblemWithNoSheet_InstallsSheetThenBinds()
    {
        var (model, runner) = Arrange();
        var target = new ProblemModel("Target", model, null);
        var free = new SceneModel("Free", model, null);
        var proposal = new List<BeatInfo>
        {
            new("Opening Image", "Where we start", AssignedElement: free.Uuid),
            new("Catalyst", "The turn", SceneName: "The letter arrives"),
            new("Debate", "Second thoughts")
        };
        var result = SheetResult(target, proposal);
        runner.ExpandBeatSheetUpdates(result);
        Assert.AreEqual(2, result.PendingUpdates.Count);
        var bind = result.PendingUpdates[0];
        var create = result.PendingUpdates[1];
        var gathered = new Dictionary<string, StoryElement>();

        // Accept the second row first: the sheet must exist before any row can bind.
        var applied = runner.ApplyUpdates(Slice(create), gathered);

        Assert.AreEqual(1, applied);
        Assert.AreEqual(3, target.StructureBeats.Count, "the first accepted row installs every beat");
        Assert.AreEqual("Opening Image", target.StructureBeats[0].Title);
        Assert.AreEqual("Second thoughts", target.StructureBeats[2].Description);
        var stub = model.StoryElements.OfType<SceneModel>().Single(s => s.Name == "The letter arrives");
        Assert.AreEqual(stub.Uuid, target.StructureBeats[1].Guid);
        Assert.AreEqual(Guid.Empty, target.StructureBeats[0].Guid, "the skipped row stays empty");

        applied = runner.ApplyUpdates(Slice(bind), gathered);

        Assert.AreEqual(1, applied);
        Assert.AreEqual(3, target.StructureBeats.Count, "the sheet is installed once");
        Assert.AreEqual(free.Uuid, target.StructureBeats[0].Guid);
    }

    [TestMethod]
    public void ApplyUpdates_BeatRowWhoseBeatWasFilledMeanwhile_DoesNotApply()
    {
        var (model, runner) = Arrange();
        var (target, _, free, proposal) = EveryOutcome(model);
        var result = SheetResult(target, proposal);
        runner.ExpandBeatSheetUpdates(result);
        var bind = result.PendingUpdates[0];
        var byHand = new SceneModel("By hand", model, null);
        target.StructureBeats[1].Guid = byHand.Uuid;

        var applied = runner.ApplyUpdates(Slice(bind), new Dictionary<string, StoryElement>());

        Assert.AreEqual(0, applied, "the row is planned again at apply and finds the beat filled");
        Assert.AreEqual(byHand.Uuid, target.StructureBeats[1].Guid);
        Assert.IsFalse(target.StructureBeats.Any(b => b.Guid == free.Uuid));
    }

    [TestMethod]
    public void MergeExtractResult_CopiesFieldStates()
    {
        var result = WorkflowResult.Succeeded();
        var extract = WorkflowResult.Succeeded();
        extract.StatusMessages.Add("Received");
        extract.PendingUpdates.Add(new PendingUpdate("Problem", Guid.NewGuid(), new PropertySpec("Name"), "x"));
        extract.UpdatedProperties["Problem.Name"] = "x";
        extract.FieldStates["Name"] = OutputFieldState.Unchanged;

        WorkflowRunner.MergeExtractResult(result, extract);

        Assert.AreEqual(1, result.StatusMessages.Count);
        Assert.AreEqual(1, result.PendingUpdates.Count);
        Assert.AreEqual("x", result.UpdatedProperties["Problem.Name"]);
        Assert.AreEqual(OutputFieldState.Unchanged, result.FieldStates["Name"]);
    }

    [TestMethod]
    public void ClassifyScalarUpdates_AfterMerge_ReadsTheModelsFieldState()
    {
        var (model, runner) = Arrange();
        var problem = new ProblemModel("Catching the drug dealer Lacas", model, null);
        var elements = new Dictionary<string, StoryElement> { ["Problem"] = problem };
        var outputs = new List<ElementOutput>
        {
            new()
            {
                ElementType = StoryItemType.Problem,
                ElementLabel = "Problem",
                PropertiesToUpdate = new List<PropertySpec> { new("Name") }
            }
        };
        const string json = "{\"Name\":\"Tracking Lacas\",\"field_states\":{\"Name\":\"Unchanged\"}}";

        // Old merge: pending copied, field_states left behind. Compare sees a different
        // non-empty Name and protects it, the row the 2026-09-04 smoke showed.
        var stale = WorkflowResult.Succeeded();
        foreach (var u in runner.ExtractOutputs(json, elements, outputs).PendingUpdates)
            stale.PendingUpdates.Add(u);
        runner.ClassifyScalarUpdates(stale, null, "ProblemBuilder");
        Assert.AreEqual(UpdateKind.Protect, stale.PendingUpdates.Single().Kind, "without the map, compare protects");

        var merged = WorkflowResult.Succeeded();
        WorkflowRunner.MergeExtractResult(merged, runner.ExtractOutputs(json, elements, outputs));
        runner.ClassifyScalarUpdates(merged, null, "ProblemBuilder");

        Assert.AreEqual(0, merged.PendingUpdates.Count, "Unchanged from the model is a NoOp, so the row is dropped");
    }

    [TestMethod]
    public void SessionProposalSet_ChatPatch_RenamesAStubAndRefusesABind()
    {
        var (model, runner) = Arrange();
        var (target, _, _, proposal) = EveryOutcome(model);
        var result = SheetResult(target, proposal);
        runner.ExpandBeatSheetUpdates(result);
        var set = new SessionProposalSet();
        set.ReplaceFromPending(result.PendingUpdates, null);

        Assert.IsFalse(set.TryApplyPatch("Problem.StructureBeats[1]", "Something else", out _),
            "a Bind row names an outline element and takes no free text");
        Assert.IsTrue(set.TryApplyPatch("Problem.StructureBeats[2]", "The letter arrives", out _));

        var entry = set.Get("Problem.StructureBeats[2]");
        var value = (BeatRowValue)entry.Update.Value;
        Assert.AreEqual("The letter arrives", value.Row.SceneName);
        Assert.AreEqual("new Scene \"The letter arrives\"", entry.ProposedText);
    }
}
