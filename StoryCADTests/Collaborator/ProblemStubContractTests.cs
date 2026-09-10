using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCADLib.Models;
using StoryCADLib.Models.Tools;
using StoryCADLib.Services.API;
using StoryCADLib.Services.Outline;
using StoryCADLib.ViewModels;
using StoryCollaborator;
using StoryCollaborator.Models;
using StoryCollaborator.Workflows;

#nullable disable

namespace StoryCADTests.Collaborator;

/// <summary>Collaborator #246: Problem Builder may stub a Problem on an empty beat.</summary>
[TestClass]
public class ProblemStubContractTests
{
    private static StoryCADApi CreateApi() => new(
        Ioc.Default.GetRequiredService<OutlineService>(),
        Ioc.Default.GetRequiredService<ListData>(),
        Ioc.Default.GetRequiredService<ControlData>(),
        Ioc.Default.GetRequiredService<ToolsData>());

    private static (StoryModel Model, ProblemModel Parent, WorkflowRunner Runner) Arrange()
    {
        var model = new StoryModel();
        var parent = new ProblemModel("Story Problem", model, null);
        var prot = new CharacterModel("Hero", model, null);
        var antag = new CharacterModel("Villain", model, null);
        parent.Protagonist = prot.Uuid;
        parent.Antagonist = antag.Uuid;
        var api = CreateApi();
        api.CurrentModel = model;
        var workflow = new Workflow("ProblemBuilder", "Problem Builder", "test", StoryItemType.Problem)
        {
            CreatesScenesForBeats = true,
            CreatesProblemsForBeats = true
        };
        return (model, parent, new WorkflowRunner(model, workflow, api));
    }

    [TestMethod]
    public void CreateProblemStub_WritesCategoryDescriptionSeatsAndBeat()
    {
        var (model, parent, runner) = Arrange();
        var result = new WorkflowResult();

        runner.ApplyBeatSheetMerge(
            parent.Uuid,
            new List<BeatInfo>
            {
                new("Midpoint", "the nested fight",
                    ProblemName: "The Yellow Brick Road",
                    ProblemDescription: "Dorothy must reach the Emerald City.",
                    ProblemCategory: "Sequence")
            },
            result);

        var stub = model.StoryElements.OfType<ProblemModel>().Single(p => p.Uuid != parent.Uuid);
        Assert.AreEqual("The Yellow Brick Road", stub.Name);
        Assert.AreEqual("Sequence", stub.ProblemCategory);
        Assert.AreEqual("Dorothy must reach the Emerald City.", stub.Description);
        Assert.AreEqual(parent.Protagonist, stub.Protagonist);
        Assert.AreEqual(parent.Antagonist, stub.Antagonist);
        Assert.AreEqual(stub.Uuid, parent.StructureBeats[0].Guid);
        StringAssert.Contains(string.Join('\n', result.StatusMessages), "created Problem");
    }

    [TestMethod]
    public void CreateProblemStub_StoryProblemCategory_DoesNotCreate()
    {
        var (model, parent, runner) = Arrange();

        runner.ApplyBeatSheetMerge(
            parent.Uuid,
            new List<BeatInfo>
            {
                new("Midpoint", "bad",
                    ProblemName: "Another spine",
                    ProblemCategory: "Story problem")
            },
            new WorkflowResult());

        Assert.AreEqual(1, model.StoryElements.OfType<ProblemModel>().Count());
        Assert.AreEqual(Guid.Empty, parent.StructureBeats[0].Guid);
    }

    [TestMethod]
    public void Plan_BothStubKinds_RefusesAndCreatesNeither()
    {
        var (model, parent, runner) = Arrange();
        var proposal = new List<BeatInfo>
        {
            new("Midpoint", "both",
                SceneName: "Leave Kansas",
                ProblemName: "The Yellow Brick Road",
                ProblemCategory: "Sequence")
        };

        var plan = runner.PlanBeatSheetMerge(parent.Uuid, proposal);

        Assert.AreEqual(BeatRowOutcome.Refuse, plan[0].Outcome);
        StringAssert.Contains(plan[0].ElementName, "Scene stub and a Problem stub");

        runner.ApplyBeatSheetMerge(parent.Uuid, proposal, new WorkflowResult());

        Assert.AreEqual(0, model.StoryElements.OfType<SceneModel>().Count());
        Assert.AreEqual(1, model.StoryElements.OfType<ProblemModel>().Count());
    }

    [TestMethod]
    public void Plan_ProblemStub_DisplaysCategory()
    {
        var (_, parent, runner) = Arrange();
        var text = runner.FormatBeatSheetDisplay(
            parent.Uuid,
            new List<BeatInfo>
            {
                new("Midpoint", "the nested fight",
                    ProblemName: "The Yellow Brick Road",
                    ProblemCategory: "Sequence")
            });

        StringAssert.Contains(text, "new Problem \"The Yellow Brick Road\" (Sequence)");
    }
}
