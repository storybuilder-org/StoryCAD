using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCADLib.Models;
using StoryCADLib.Services.API;
using StoryCADLib.Services.Outline;
using StoryCADLib.Models.Tools;
using StoryCADLib.ViewModels;
using StoryCADLib.ViewModels.Tools;
using StoryCollaborator;
using StoryCollaborator.Models;
using StoryCollaborator.Workflows;

#nullable disable

namespace StoryCADTests.Collaborator;

/// <summary>
///     Collaborator #77 step 8: beat guards on <c>ApplyBeatSheetMerge</c>.
///     A filled beat is a beat whose row holds an element GUID. ProblemBuilder must not
///     modify a filled beat, and must not grow a sheet that is already present.
/// </summary>
[TestClass]
public class BeatSheetMergeGuardTests
{
    private static StoryCADApi CreateApi() => new(
        Ioc.Default.GetRequiredService<OutlineService>(),
        Ioc.Default.GetRequiredService<ListData>(),
        Ioc.Default.GetRequiredService<ControlData>(),
        Ioc.Default.GetRequiredService<ToolsData>());

    /// <summary>Problem carrying one beat that already holds an assigned element.</summary>
    private static (StoryCADApi Api, ProblemModel Problem, WorkflowRunner Runner) ArrangeFilledBeat(
        string beatTitle, string beatDescription)
    {
        var model = new StoryModel();
        var problem = new ProblemModel("Subplot", model, null)
        {
            StructureTitle = "Save The Cat (Mini)",
            StructureDescription = "user wrote this"
        };
        var scene = new SceneModel("Existing Scene", model, null);

        problem.StructureBeats.Add(new StructureBeat(beatTitle, beatDescription)
        {
            Guid = scene.Uuid
        });

        var api = CreateApi();
        api.CurrentModel = model;

        var workflow = new Workflow(
            "ProblemBuilder", "Problem Builder", "test", StoryItemType.Problem);
        var runner = new WorkflowRunner(model, workflow, api);

        return (api, problem, runner);
    }

    [TestMethod]
    public void ApplyBeatSheetMerge_FilledBeatWithBlankDescription_KeepsItBlank()
    {
        var (_, problem, runner) = ArrangeFilledBeat("Catalyst", string.Empty);

        runner.ApplyBeatSheetMerge(
            problem.Uuid,
            new List<BeatInfo> { new("Catalyst", "text the model invented") },
            new WorkflowResult());

        Assert.AreEqual(string.Empty, problem.StructureBeats[0].Description,
            "a beat that already holds an element must not take model text");
    }

    [TestMethod]
    public void ApplyBeatSheetMerge_FilledBeatWithBlankTitle_KeepsItBlank()
    {
        var (_, problem, runner) = ArrangeFilledBeat(string.Empty, "user wrote this");

        runner.ApplyBeatSheetMerge(
            problem.Uuid,
            new List<BeatInfo> { new("Title the model invented", "user wrote this") },
            new WorkflowResult());

        Assert.AreEqual(string.Empty, problem.StructureBeats[0].Title,
            "a beat that already holds an element must not take a model title");
    }

    [TestMethod]
    public void ApplyBeatSheetMerge_SheetAlreadyPresent_DoesNotGrow()
    {
        var (_, problem, runner) = ArrangeFilledBeat("Catalyst", "user wrote this");

        runner.ApplyBeatSheetMerge(
            problem.Uuid,
            new List<BeatInfo>
            {
                new("Catalyst", "user wrote this"),
                new("Midpoint", "model wants a second beat"),
                new("Finale", "model wants a third beat")
            },
            new WorkflowResult());

        Assert.AreEqual(1, problem.StructureBeats.Count,
            "a sheet that is already present must not gain rows");
    }

    [TestMethod]
    public void ApplyBeatSheetMerge_EmptyProblem_InstallsProposedSheet()
    {
        var model = new StoryModel();
        var problem = new ProblemModel("Fresh", model, null);
        var api = CreateApi();
        api.CurrentModel = model;

        var workflow = new Workflow(
            "ProblemBuilder", "Problem Builder", "test", StoryItemType.Problem);
        var runner = new WorkflowRunner(model, workflow, api);

        runner.ApplyBeatSheetMerge(
            problem.Uuid,
            new List<BeatInfo>
            {
                new("Goal", "what the character wants"),
                new("Conflict", "what stops them"),
                new("Outcome", "how it lands")
            },
            new WorkflowResult());

        Assert.AreEqual(3, problem.StructureBeats.Count,
            "a Problem with no sheet must receive the proposed beats");
    }
}
