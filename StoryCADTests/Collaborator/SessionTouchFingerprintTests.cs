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
///     Collaborator #272: the session touch map holds a fingerprint of the value Collaborator wrote.
///     A property the writer changed afterward is not touched, so Classify gives Protect and
///     Accept All asks first. Real StoryCADApi, no mocks. ApplyPendingList is a local function in
///     Collaborator, so these tests use ApplyUpdates and set the map with ReadTouchFingerprint,
///     as ApplyPendingList does.
/// </summary>
[TestClass]
public class SessionTouchFingerprintTests
{
    private const string Written = "Arrest Lacas before the shipment lands.";
    private const string WritersText = "Prove Lacas framed her brother.";
    private const string NextProposal = "Stop Lacas at the docks.";

    private static readonly PropertySpec GoalSpec = new("ProtGoal");

    private static StoryCADApi CreateApi() => new(
        Ioc.Default.GetRequiredService<OutlineService>(),
        Ioc.Default.GetRequiredService<ListData>(),
        Ioc.Default.GetRequiredService<ControlData>(),
        Ioc.Default.GetRequiredService<ToolsData>());

    private static (StoryCADApi Api, WorkflowRunner Runner, ProblemModel Problem) Arrange()
    {
        var model = new StoryModel();
        var api = CreateApi();
        api.CurrentModel = model;
        var workflow = new Workflow("ProblemBuilder", "Problem Builder", "test", StoryItemType.Problem);
        var problem = new ProblemModel("Catching Lacas", model, null);
        return (api, new WorkflowRunner(model, workflow, api), problem);
    }

    private static WorkflowResult GoalRun(ProblemModel problem, string value, OutputFieldState? state)
    {
        var result = WorkflowResult.Succeeded();
        result.PendingUpdates.Add(new PendingUpdate("Problem", problem.Uuid, GoalSpec, value));
        if (state.HasValue)
            result.FieldStates["ProtGoal"] = state.Value;
        return result;
    }

    /// <summary>Apply one run and record its touches, as ApplyPendingList does.</summary>
    private static void AcceptAndTouch(
        WorkflowRunner runner, WorkflowResult result, Dictionary<string, string> touchMap)
    {
        var n = runner.ApplyUpdates(result, new Dictionary<string, StoryElement>());
        Assert.IsTrue(n > 0, "the accept wrote nothing");
        foreach (var u in result.PendingUpdates)
        {
            var fingerprint = runner.ReadTouchFingerprint(u);
            if (fingerprint != null)
                touchMap[u.SessionTouchKey] = fingerprint;
        }
    }

    private static Dictionary<string, string> NewTouchMap() => new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Run 1 fills ProtGoal and the writer accepts it.</summary>
    private static Dictionary<string, string> AcceptFirstRun(WorkflowRunner runner, ProblemModel problem)
    {
        var touchMap = NewTouchMap();
        var first = GoalRun(problem, Written, OutputFieldState.Fill);
        runner.ClassifyScalarUpdates(first, touchMap, "ProblemBuilder");
        Assert.AreEqual(UpdateKind.Fill, first.PendingUpdates.Single().Kind);
        AcceptAndTouch(runner, first, touchMap);
        return touchMap;
    }

    [TestMethod]
    public void WriterChangesWrittenValue_NextRunRevise_IsProtectAndPartitionedToProtect()
    {
        var (api, runner, problem) = Arrange();
        var touchMap = AcceptFirstRun(runner, problem);
        Assert.IsTrue(api.UpdateElementProperty(problem.Uuid, "ProtGoal", WritersText).IsSuccess);

        var second = GoalRun(problem, NextProposal, OutputFieldState.Revise);
        runner.ClassifyScalarUpdates(second, touchMap, "ProblemBuilder");
        var row = second.PendingUpdates.Single();

        Assert.AreEqual(UpdateKind.Protect, row.Kind);
        Assert.IsFalse(row.AcceptAllMayApply);
        var (free, protect) = OverwriteAcceptanceSession.Partition(second.PendingUpdates);
        Assert.AreEqual(0, free.Count);
        Assert.AreEqual(1, protect.Count);
    }

    [TestMethod]
    public void WriterChangesWrittenValue_NextRunFillOnFilledProperty_IsProtect()
    {
        var (api, runner, problem) = Arrange();
        var touchMap = AcceptFirstRun(runner, problem);
        Assert.IsTrue(api.UpdateElementProperty(problem.Uuid, "ProtGoal", WritersText).IsSuccess);

        var second = GoalRun(problem, NextProposal, OutputFieldState.Fill);
        runner.ClassifyScalarUpdates(second, touchMap, "ProblemBuilder");

        Assert.AreEqual(UpdateKind.Protect, second.PendingUpdates.Single().Kind);
    }

    [TestMethod]
    public void WriterChangesOnlyCase_NextRunRevise_IsProtect()
    {
        var (api, runner, problem) = Arrange();
        var touchMap = AcceptFirstRun(runner, problem);
        var lowered = "a" + Written.Substring(1);
        Assert.AreNotEqual(Written, lowered);
        Assert.IsTrue(api.UpdateElementProperty(problem.Uuid, "ProtGoal", lowered).IsSuccess);

        var second = GoalRun(problem, NextProposal, OutputFieldState.Revise);
        runner.ClassifyScalarUpdates(second, touchMap, "ProblemBuilder");

        Assert.AreEqual(UpdateKind.Protect, second.PendingUpdates.Single().Kind);
    }

    [TestMethod]
    public void WriterDoesNotChangeWrittenValue_NextRunRevise_IsRefresh()
    {
        var (_, runner, problem) = Arrange();
        var touchMap = AcceptFirstRun(runner, problem);

        var second = GoalRun(problem, NextProposal, OutputFieldState.Revise);
        runner.ClassifyScalarUpdates(second, touchMap, "ProblemBuilder");
        var row = second.PendingUpdates.Single();

        Assert.AreEqual(UpdateKind.Refresh, row.Kind);
        Assert.IsTrue(row.AcceptAllMayApply);
    }

    [TestMethod]
    public void OutlineHoldsRtfOfWrittenValue_NextRunRevise_IsRefresh()
    {
        var (_, runner, problem) = Arrange();
        var touchMap = AcceptFirstRun(runner, problem);
        // The element page stores the same words as RTF.
        problem.ProtGoal = @"{\rtf1\ansi{\fonttbl{\f0 Segoe UI;}}\pard " + Written + @"\par}";

        var second = GoalRun(problem, NextProposal, OutputFieldState.Revise);
        runner.ClassifyScalarUpdates(second, touchMap, "ProblemBuilder");

        Assert.AreEqual(UpdateKind.Refresh, second.PendingUpdates.Single().Kind);
    }

    [TestMethod]
    public void NoTouchKey_WriterWritesValue_NextRunRevise_IsProtect()
    {
        var (api, runner, problem) = Arrange();
        Assert.IsTrue(api.UpdateElementProperty(problem.Uuid, "ProtGoal", WritersText).IsSuccess);

        var second = GoalRun(problem, NextProposal, OutputFieldState.Revise);
        runner.ClassifyScalarUpdates(second, NewTouchMap(), "ProblemBuilder");

        Assert.AreEqual(UpdateKind.Protect, second.PendingUpdates.Single().Kind);
    }

    [TestMethod]
    public async Task WriterChangesSimpleListWrittenValue_NextRunRevise_IsProtect()
    {
        var api = CreateApi();
        var scene = await ArrangeScene(api);
        var runner = new WorkflowRunner(api.CurrentModel!, WorkflowRegistry.Get("StoryProblem")!, api);
        var touchMap = NewTouchMap();
        var spec = new PropertySpec("ScenePurpose", WriteVia.SimpleList, ListEntryType: typeof(string));

        var first = WorkflowResult.Succeeded();
        first.PendingUpdates.Add(new PendingUpdate("Scene", scene.Uuid, spec, new List<string> { "Reveal" }));
        AcceptAndTouch(runner, first, touchMap);
        Assert.IsTrue(touchMap.ContainsKey(first.PendingUpdates.Single().SessionTouchKey));
        scene.ScenePurpose.Clear();
        scene.ScenePurpose.Add("Hide");

        var second = WorkflowResult.Succeeded();
        second.FieldStates["ScenePurpose"] = OutputFieldState.Revise;
        second.PendingUpdates.Add(new PendingUpdate("Scene", scene.Uuid, spec, new List<string> { "Turn" }));
        runner.ClassifyScalarUpdates(second, touchMap, "StoryProblem");

        Assert.AreEqual(UpdateKind.Protect, second.PendingUpdates.Single().Kind);
    }

    [TestMethod]
    public async Task SceneBuilderScenePurpose_WriterChangesWrittenList_IsProtect()
    {
        var api = CreateApi();
        var scene = await ArrangeScene(api);
        var runner = new WorkflowRunner(api.CurrentModel!, WorkflowRegistry.Get("SceneBuilder")!, api);
        var touchMap = NewTouchMap();
        var spec = new PropertySpec("ScenePurpose", WriteVia.SimpleList, ListEntryType: typeof(string));

        var first = WorkflowResult.Succeeded();
        first.PendingUpdates.Add(new PendingUpdate("Scene", scene.Uuid, spec, new List<string> { "Reveal" }));
        runner.ClassifyScalarUpdates(first, touchMap, "SceneBuilder");
        Assert.AreEqual(UpdateKind.Fill, first.PendingUpdates.Single().Kind);
        AcceptAndTouch(runner, first, touchMap);
        scene.ScenePurpose.Clear();
        scene.ScenePurpose.Add("Hide");

        var second = WorkflowResult.Succeeded();
        second.PendingUpdates.Add(new PendingUpdate("Scene", scene.Uuid, spec, new List<string> { "Turn" }));
        runner.ClassifyScalarUpdates(second, touchMap, "SceneBuilder");

        Assert.AreEqual(UpdateKind.Protect, second.PendingUpdates.Single().Kind);
    }

    [TestMethod]
    public void CollaboratorWritesTouchedPropertyAgain_TouchMapHoldsNewFingerprint()
    {
        var (_, runner, problem) = Arrange();
        var touchMap = AcceptFirstRun(runner, problem);
        var key = $"{problem.Uuid:N}.ProtGoal";
        var oldFingerprint = touchMap[key];

        var second = GoalRun(problem, NextProposal, OutputFieldState.Revise);
        runner.ClassifyScalarUpdates(second, touchMap, "ProblemBuilder");
        Assert.AreEqual(UpdateKind.Refresh, second.PendingUpdates.Single().Kind);
        AcceptAndTouch(runner, second, touchMap);

        Assert.AreNotEqual(oldFingerprint, touchMap[key]);
        Assert.AreEqual(WorkflowRunner.NormalizeCompareText(NextProposal), touchMap[key]);
    }

    private static async Task<SceneModel> ArrangeScene(StoryCADApi api)
    {
        var create = await api.CreateEmptyOutline("Fingerprint", "Author", "0");
        Assert.IsTrue(create.IsSuccess);
        var overview = api.CurrentModel!.StoryElements.First(e => e.ElementType == StoryItemType.StoryOverview);
        var add = api.AddElement(StoryItemType.Scene, overview.Uuid.ToString(), "Docks");
        Assert.IsTrue(add.IsSuccess);
        return (SceneModel)api.GetStoryElement(add.Payload).Payload!;
    }
}
