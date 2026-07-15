using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCollaborator;

#nullable disable

namespace StoryCADTests.Collaborator;

/// <summary>
///     Covers the outgoing-request half of the PR #1470 review item (issue #89 omission):
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
}
