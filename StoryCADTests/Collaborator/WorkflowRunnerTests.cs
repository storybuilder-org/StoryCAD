using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCADLib.Models;
using StoryCADLib.Models.Tools;
using StoryCADLib.Services.API;
using StoryCADLib.Services.Outline;
using StoryCADLib.ViewModels;
using StoryCollaborator;
using StoryCollaborator.Models;
using StoryCollaborator.Services;
using StoryCollaborator.Workflows;

#nullable disable

namespace StoryCADTests.Collaborator;

/// <summary>
///     Tests of <see cref="WorkflowRunner"/>. The first covers the outgoing-request half of the
///     PR #1470 review item (issue #89 omission):
///     the credential resolved by <c>KernelFactory.ResolveWorkflowCredential</c> is the value
///     that leaves the machine in the Authorization header of every /workflow POST.
/// </summary>
[TestClass]
public class WorkflowRunnerTests
{
    [TestMethod]
    public void CreateWorkflowRequest_WithCredential_SetsBearerAuthorizationHeader()
    {
        using var request = WorkflowRunner.CreateWorkflowRequest(
            "https://proxy.example/v1", "activation-jwt", "{\"workflowId\":\"Tone\"}");

        Assert.AreEqual("Bearer", request.Headers.Authorization.Scheme);
        Assert.AreEqual("activation-jwt", request.Headers.Authorization.Parameter,
            "the resolved credential must ride the Authorization header of the workflow call");
        Assert.AreEqual("https://proxy.example/v1/workflow", request.RequestUri.ToString());
        Assert.AreEqual("application/json", request.Content.Headers.ContentType.MediaType);
    }

    /// <summary>
    ///     Collaborator #273: Accept applies a chat-renamed Problem Create row with the new name.
    ///     Before the fix the patch wrote SceneName, the row then named both stub kinds, and the
    ///     plan refused it, so Accept created nothing.
    /// </summary>
    [TestMethod]
    public void ApplyUpdates_PatchedProblemCreateRow_CreatesProblemWithPatchedName()
    {
        var model = new StoryModel();
        var parent = new ProblemModel("Story Problem", model, null);
        var api = new StoryCADApi(
            Ioc.Default.GetRequiredService<OutlineService>(),
            Ioc.Default.GetRequiredService<ListData>(),
            Ioc.Default.GetRequiredService<ControlData>(),
            Ioc.Default.GetRequiredService<ToolsData>());
        api.CurrentModel = model;
        var workflow = new Workflow("ProblemBuilder", "Problem Builder", "test", StoryItemType.Problem)
        {
            CreatesScenesForBeats = true,
            CreatesProblemsForBeats = true
        };
        var runner = new WorkflowRunner(model, workflow, api);
        var proposal = new List<BeatInfo>
        {
            new("Midpoint", "the nested fight",
                ProblemName: "The Yellow Brick Road", ProblemCategory: "Sequence")
        };
        var result = WorkflowResult.Succeeded();
        result.PendingUpdates.Add(new PendingUpdate("Problem", parent.Uuid,
            new PropertySpec("StructureBeats", WriteVia.BeatSheet, JsonKey: "beats"), proposal));
        runner.ExpandBeatSheetUpdates(result);
        var set = new SessionProposalSet();
        set.ReplaceFromPending(result.PendingUpdates, null);
        Assert.IsTrue(set.TryApplyPatch("Problem.StructureBeats[00]", "The Road to Oz", out _));

        var slice = WorkflowResult.Succeeded();
        slice.PendingUpdates.Add(set.Get("Problem.StructureBeats[00]").Update);
        var applied = runner.ApplyUpdates(slice, new Dictionary<string, StoryElement>());

        Assert.AreEqual(1, applied);
        var created = model.StoryElements.OfType<ProblemModel>().Single(p => p.Uuid != parent.Uuid);
        Assert.AreEqual("The Road to Oz", created.Name);
        Assert.AreEqual(created.Uuid, parent.StructureBeats[0].Guid);
        Assert.AreEqual(0, model.StoryElements.OfType<SceneModel>().Count(), "no Scene stub");
    }
}
