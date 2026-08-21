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
///     Collaborator #77 step 5 / #208 stub contract. A Scene stub must carry Name,
///     Description, and Notes. SceneBuilder fills the rest. Today the runner sets Name only.
/// </summary>
[TestClass]
public class SceneStubContractTests
{
    private static StoryCADApi CreateApi() => new(
        Ioc.Default.GetRequiredService<OutlineService>(),
        Ioc.Default.GetRequiredService<ListData>(),
        Ioc.Default.GetRequiredService<ControlData>(),
        Ioc.Default.GetRequiredService<ToolsData>());

    private static SceneModel CreateStub(BeatInfo beat)
    {
        var model = new StoryModel();
        var problem = new ProblemModel("Complication", model, null);
        var api = CreateApi();
        api.CurrentModel = model;

        var workflow = new Workflow("ProblemBuilder", "Problem Builder", "test", StoryItemType.Problem)
        {
            CreatesScenesForBeats = true
        };

        new WorkflowRunner(model, workflow, api)
            .ApplyBeatSheetMerge(problem.Uuid, new List<BeatInfo> { beat }, new WorkflowResult());

        return model.StoryElements.OfType<SceneModel>().Single();
    }

    [TestMethod]
    public void CreatedStub_CarriesSceneDescription()
    {
        var stub = CreateStub(new BeatInfo(
            "Setup", "the beat blurb",
            SceneName: "Arrival at the gate",
            SceneDescription: "She reaches the gate and finds it barred."));

        Assert.AreEqual("She reaches the gate and finds it barred.", stub.Description,
            "the stub must carry the scene summary, not just a name");
    }

    [TestMethod]
    public void CreatedStub_CarriesSceneNotes()
    {
        var stub = CreateStub(new BeatInfo(
            "Setup", "the beat blurb",
            SceneName: "Arrival at the gate",
            SceneNotes: "Dread. The gate stands for the promise her father broke."));

        Assert.AreEqual("Dread. The gate stands for the promise her father broke.", stub.Notes,
            "Notes is the channel that carries problem context down to the scene");
    }

    [TestMethod]
    public void CreatedStub_WithoutDescriptionOrNotes_LeavesThemEmpty()
    {
        var stub = CreateStub(new BeatInfo("Setup", "the beat blurb", SceneName: "Arrival"));

        Assert.AreEqual("Arrival", stub.Name);
        Assert.IsTrue(string.IsNullOrEmpty(stub.Description),
            "no invented description when the model supplied none");
        Assert.IsTrue(string.IsNullOrEmpty(stub.Notes),
            "no invented notes when the model supplied none");
    }
}
