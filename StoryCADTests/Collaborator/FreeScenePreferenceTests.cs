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

/// <summary>
///     Collaborator #77 step 7. Terry: for an empty beat you either create a Scene or find a
///     Scene that has not been assigned. Binding an existing Scene is preferred over creating
///     a second one, so a proposal carrying both must bind and not create.
/// </summary>
[TestClass]
public class FreeScenePreferenceTests
{
    private static StoryCADApi CreateApi() => new(
        Ioc.Default.GetRequiredService<OutlineService>(),
        Ioc.Default.GetRequiredService<ListData>(),
        Ioc.Default.GetRequiredService<ControlData>(),
        Ioc.Default.GetRequiredService<ToolsData>());

    [TestMethod]
    public void EmptyBeat_ProposalHasBothFreeSceneAndSceneName_BindsAndDoesNotCreate()
    {
        var model = new StoryModel();
        var problem = new ProblemModel("Complication", model, null);
        var freeScene = new SceneModel("Already written", model, null);

        var api = CreateApi();
        api.CurrentModel = model;
        var workflow = new Workflow("ProblemBuilder", "Problem Builder", "test", StoryItemType.Problem)
        {
            CreatesScenesForBeats = true
        };

        new WorkflowRunner(model, workflow, api).ApplyBeatSheetMerge(
            problem.Uuid,
            new List<BeatInfo>
            {
                new("Setup", "the opening",
                    AssignedElement: freeScene.Uuid,
                    SceneName: "A scene the model would have invented")
            },
            new WorkflowResult());

        Assert.AreEqual(1, model.StoryElements.OfType<SceneModel>().Count(),
            "an existing free Scene must be bound rather than duplicated");
        Assert.AreEqual(freeScene.Uuid, problem.StructureBeats[0].Guid,
            "the beat must hold the existing Scene");
    }

    [TestMethod]
    public void EmptyBeat_ProposalHasSceneNameOnly_StillCreates()
    {
        var model = new StoryModel();
        var problem = new ProblemModel("Complication", model, null);

        var api = CreateApi();
        api.CurrentModel = model;
        var workflow = new Workflow("ProblemBuilder", "Problem Builder", "test", StoryItemType.Problem)
        {
            CreatesScenesForBeats = true
        };

        new WorkflowRunner(model, workflow, api).ApplyBeatSheetMerge(
            problem.Uuid,
            new List<BeatInfo> { new("Setup", "the opening", SceneName: "Arrival") },
            new WorkflowResult());

        Assert.AreEqual(1, model.StoryElements.OfType<SceneModel>().Count(),
            "with no free Scene named, creation is still the fallback");
    }
}
