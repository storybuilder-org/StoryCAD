using System.Net;
using System.Net.Http;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCADLib.Collaborator.ViewModels;
using StoryCADLib.Models;
using StoryCADLib.Models.Tools;
using StoryCADLib.Services.API;
using StoryCADLib.Services.Collaborator.Contracts;
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
///     Collaborator #291. The session logic lives in WorkflowSessionService; the Collaborator
///     view models reach it through WorkflowViewModelWiring. These tests drive the real view
///     models, the real service and a real outline. The split is at the model boundary: each
///     test builds a WorkflowResult with known proposals and hands it to the session, so no
///     Worker call happens.
/// </summary>
[TestClass]
public class WorkflowSessionTests
{
    private const string WriterText = "Writer text";
    private const string ModelText = "Model text";

    private static StoryCADApi CreateApi() => new(
        Ioc.Default.GetRequiredService<OutlineService>(),
        Ioc.Default.GetRequiredService<ListData>(),
        Ioc.Default.GetRequiredService<ControlData>(),
        Ioc.Default.GetRequiredService<ToolsData>());

    /// <summary>One view model path: everything a test needs to click and inspect.</summary>
    private sealed class Rig
    {
        public StoryCADApi Api;
        public OverviewModel Overview;
        public WorkflowSession Session;
        public WorkflowSessionService Service;
        public WorkflowViewModel ViewModel;
        public WorkflowShellViewModel Shell;
        public List<int> ConfirmAsked = new();
        public bool ConfirmAnswer;
    }

    private static string Key(string property) => $"Overview.{property}";

    private static PendingUpdate Row(OverviewModel overview, string property, string value) =>
        new("Overview", overview.Uuid, new PropertySpec(property), value);

    /// <summary>
    ///     An outline whose Overview holds the given writer text in the given properties, a
    ///     session holding one proposal per entry of <paramref name="proposed" />, and the view
    ///     models wired to the service. The real classifier decides Fill, Refresh or Protect.
    /// </summary>
    private static async Task<Rig> ArrangeAsync(
        bool confirmAnswer,
        IReadOnlyDictionary<string, string> writerText,
        params (string Property, string Value)[] proposed)
    {
        var rig = new Rig { Api = CreateApi(), ConfirmAnswer = confirmAnswer };
        var create = await rig.Api.CreateEmptyOutline("Session Test", "Author", "0");
        Assert.IsTrue(create.IsSuccess);
        rig.Overview = rig.Api.CurrentModel.StoryElements.OfType<OverviewModel>().First();
        foreach (var (property, text) in writerText)
            Assert.IsTrue(rig.Api.UpdateElementProperty(rig.Overview.Uuid, property, text).IsSuccess);

        var workflow = new Workflow("SessionTest", "Session Test", "test", StoryItemType.StoryOverview);
        rig.Session = new WorkflowSession(workflow, rig.Api.CurrentModel);
        rig.Service = new WorkflowSessionService(rig.Api, null, CollaboratorSettings.Default);

        var result = WorkflowResult.Succeeded();
        foreach (var (property, value) in proposed)
            result.PendingUpdates.Add(Row(rig.Overview, property, value));
        rig.Service.SetResult(rig.Session, result);

        rig.ViewModel = new WorkflowViewModel();
        rig.Shell = new WorkflowShellViewModel();
        WorkflowViewModelWiring.Wire(rig.ViewModel, rig.Shell, rig.Service, rig.Session, protect =>
        {
            rig.ConfirmAsked.Add(protect.Count);
            return Task.FromResult(rig.ConfirmAnswer);
        });
        WorkflowViewModelWiring.Show(rig.ViewModel, rig.Shell, rig.Session);
        return rig;
    }

    private static string Text(Rig rig, string property) =>
        (string)typeof(OverviewModel).GetProperty(property)!.GetValue(rig.Overview);

    private static ProposalSessionStatus Status(Rig rig, string property) =>
        rig.Session.Proposals.Get(Key(property)).Status;

    /// <summary>The panel commands are async void under a RelayCommand; wait for the effect.</summary>
    private static async Task WaitUntil(Func<bool> done)
    {
        for (var i = 0; i < 100 && !done(); i++)
            await Task.Delay(50);
        Assert.IsTrue(done(), "the command did not finish");
    }

    private static IReadOnlyDictionary<string, string> NoWriterText =>
        new Dictionary<string, string>();

    #region View model path

    [TestMethod]
    public async Task AcceptItem_FillProposal_WritesValueAndMarksAccepted()
    {
        var rig = await ArrangeAsync(true, NoWriterText, ("Premise", ModelText));
        Assert.AreEqual(UpdateKind.Fill, rig.Session.Result.PendingUpdates[0].Kind);

        await rig.ViewModel.AcceptItemAsync(Key("Premise"));

        Assert.AreEqual(ModelText, Text(rig, "Premise"));
        Assert.AreEqual(ProposalSessionStatus.Accepted, Status(rig, "Premise"));
        Assert.AreEqual("Accepted", rig.ViewModel.PendingUpdateItems.Single().KindLabel);
        Assert.AreEqual(0, rig.ConfirmAsked.Count, "a Fill row asks nothing");
    }

    [TestMethod]
    public async Task AcceptItem_ProtectProposalWriterSaysNo_KeepsText()
    {
        var rig = await ArrangeAsync(false,
            new Dictionary<string, string> { ["Premise"] = WriterText },
            ("Premise", ModelText));
        Assert.AreEqual(UpdateKind.Protect, rig.Session.Result.PendingUpdates[0].Kind);

        await rig.ViewModel.AcceptItemAsync(Key("Premise"));

        Assert.AreEqual(WriterText, Text(rig, "Premise"));
        Assert.AreEqual(1, rig.ConfirmAsked.Count, "the last row of the list asks once");
        Assert.AreEqual(ProposalSessionStatus.Skipped, Status(rig, "Premise"),
            "a refused overwrite is flipped from Accepted to Skipped");
        Assert.IsFalse(rig.Session.Stage.HasStaged);
    }

    [TestMethod]
    public async Task AcceptItem_ProtectProposalWriterSaysYes_WritesValue()
    {
        var rig = await ArrangeAsync(true,
            new Dictionary<string, string> { ["Premise"] = WriterText },
            ("Premise", ModelText));

        await rig.ViewModel.AcceptItemAsync(Key("Premise"));

        Assert.AreEqual(ModelText, Text(rig, "Premise"));
        Assert.AreEqual(1, rig.ConfirmAsked.Count);
        Assert.AreEqual(ProposalSessionStatus.Accepted, Status(rig, "Premise"));
    }

    [TestMethod]
    public async Task AcceptItem_TwoProtectProposals_AsksOneTime()
    {
        var rig = await ArrangeAsync(true,
            new Dictionary<string, string> { ["Premise"] = WriterText, ["Concept"] = WriterText },
            ("Premise", ModelText), ("Concept", ModelText));

        await rig.ViewModel.AcceptItemAsync(Key("Premise"));
        Assert.AreEqual(0, rig.ConfirmAsked.Count, "one row is still open, so nothing is asked yet");
        Assert.AreEqual(WriterText, Text(rig, "Premise"), "a staged row is not written yet");

        await rig.ViewModel.AcceptItemAsync(Key("Concept"));

        CollectionAssert.AreEqual(new[] { 2 }, rig.ConfirmAsked, "one question that names both rows");
        Assert.AreEqual(ModelText, Text(rig, "Premise"));
        Assert.AreEqual(ModelText, Text(rig, "Concept"));
    }

    [TestMethod]
    public async Task SkipItem_Proposal_MarksSkippedAndKeepsText()
    {
        var rig = await ArrangeAsync(true, NoWriterText, ("Premise", ModelText));

        await rig.ViewModel.SkipItemAsync(Key("Premise"));

        Assert.AreEqual(ProposalSessionStatus.Skipped, Status(rig, "Premise"));
        Assert.AreEqual(string.Empty, Text(rig, "Premise") ?? string.Empty);
        Assert.AreEqual("Skipped", rig.ViewModel.PendingUpdateItems.Single().KindLabel);
    }

    [TestMethod]
    public async Task AcceptAllCommand_WriterSaysNo_AppliesAllButProtect()
    {
        var rig = await ArrangeAsync(false,
            new Dictionary<string, string> { ["Premise"] = WriterText },
            ("Premise", ModelText), ("Concept", ModelText));

        rig.ViewModel.AcceptAllCommand.Execute(null);
        await WaitUntil(() => Status(rig, "Concept") == ProposalSessionStatus.Accepted);

        Assert.AreEqual(ModelText, Text(rig, "Concept"));
        Assert.AreEqual(WriterText, Text(rig, "Premise"));
        Assert.AreEqual(ProposalSessionStatus.Open, Status(rig, "Premise"), "the Protect row stays open");
        CollectionAssert.AreEqual(new[] { 1 }, rig.ConfirmAsked);
    }

    [TestMethod]
    public async Task TryAgainCommand_FromPanel_SendsOpenAndSkippedRows()
    {
        var rig = await ArrangeAsync(true, NoWriterText,
            ("Premise", "accepted text"), ("Concept", "skipped text"), ("Description", "open text"));
        await rig.ViewModel.AcceptItemAsync(Key("Premise"));
        await rig.ViewModel.SkipItemAsync(Key("Concept"));

        // The session holds no input element, so the run stops at the required-input check
        // before any Worker call. What the runner was told to depart from is still set.
        rig.ViewModel.TryAgainCommand.Execute(null);
        await WaitUntil(() => rig.Session.Runner?.RejectedProposals != null);

        var rejected = rig.Session.Runner.RejectedProposals;
        StringAssert.Contains(rejected, "skipped text");
        StringAssert.Contains(rejected, "open text");
        Assert.IsFalse(rejected.Contains("accepted text"), "an accepted row is in the outline, not rejected");
    }

    [TestMethod]
    public async Task AcceptItem_SecondWriteOfFingerprintedValue_ClassifiesRefresh()
    {
        var rig = await ArrangeAsync(true, NoWriterText, ("Premise", "first draft"));
        await rig.ViewModel.AcceptItemAsync(Key("Premise"));
        Assert.IsTrue(rig.Session.TouchedFields.Count > 0, "the write records the touch");

        var again = WorkflowResult.Succeeded();
        again.PendingUpdates.Add(Row(rig.Overview, "Premise", "second draft"));
        rig.Service.SetResult(rig.Session, again);

        Assert.AreEqual(UpdateKind.Refresh, rig.Session.Result.PendingUpdates.Single().Kind,
            "Collaborator wrote the text, so the next proposal replaces its own text, not the writer's");
    }

    #endregion

    #region Service path

    [TestMethod]
    public async Task AcceptAsync_ProtectProposal_StagesAndKeepsText()
    {
        var rig = await ArrangeAsync(true,
            new Dictionary<string, string> { ["Premise"] = WriterText },
            ("Premise", ModelText));

        var accepted = await rig.Service.AcceptAsync(rig.Session, Key("Premise"));

        Assert.IsTrue(accepted);
        Assert.AreEqual(WriterText, Text(rig, "Premise"));
        Assert.IsTrue(rig.Session.Stage.HasStaged);
        Assert.IsTrue(rig.Service.ShouldConfirmStaged(rig.Session));
        Assert.AreEqual(0, rig.ConfirmAsked.Count, "the service never asks; the caller does");
    }

    [TestMethod]
    public async Task AcceptAllAsync_WithoutOverwrite_AppliesAllButProtect()
    {
        var rig = await ArrangeAsync(true,
            new Dictionary<string, string> { ["Premise"] = WriterText },
            ("Premise", ModelText), ("Concept", ModelText));

        await rig.Service.AcceptAllAsync(rig.Session, overwrite: false);

        Assert.AreEqual(ModelText, Text(rig, "Concept"));
        Assert.AreEqual(WriterText, Text(rig, "Premise"));
        CollectionAssert.AreEqual(new[] { Key("Premise") },
            rig.Session.Result.PendingUpdates.Select(u => u.Key).ToList());
    }

    [TestMethod]
    public async Task ResolveInputs_InputNotGiven_ReturnsInputName()
    {
        var api = CreateApi();
        await api.CreateEmptyOutline("Resolve Test", "Author", "0");
        var workflow = new Workflow("ResolveTest", "Resolve Test", "test", StoryItemType.Character);
        var session = new WorkflowSession(workflow, api.CurrentModel);
        var service = new WorkflowSessionService(api, null, CollaboratorSettings.Default);
        var label = workflow.GetIO().RequiredInputs.Single().ElementLabel;

        var resolution = service.ResolveInputs(session, null, null);

        CollectionAssert.AreEqual(new[] { label }, resolution.Missing);
        Assert.IsFalse(session.Elements.ContainsKey(label));
    }

    #endregion

    #region Client header

    [TestMethod]
    public void CreateWorkflowRequest_SendsClientName()
    {
        var before = KernelFactory.ClientName;
        try
        {
            Assert.AreEqual("app", before, "the app is the default client");
            using var app = WorkflowRunner.CreateWorkflowRequest("https://example.test", "token", "{}");
            Assert.AreEqual("app", app.Headers.GetValues("X-Collab-Client").Single());

            KernelFactory.ClientName = "mcp";
            using var mcp = WorkflowRunner.CreateWorkflowRequest("https://example.test", "token", "{}");
            Assert.AreEqual("mcp", mcp.Headers.GetValues("X-Collab-Client").Single());
        }
        finally
        {
            KernelFactory.ClientName = before;
        }
    }

    [TestMethod]
    public async Task ChatRequest_SendsClientName()
    {
        var before = KernelFactory.ClientName;
        try
        {
            KernelFactory.ClientName = "mcp";
            var inner = new RecordingHandler(HttpStatusCode.Unauthorized, HttpStatusCode.OK);
            using var handler = new ActivationJwtHandler(() => "token", () => Task.CompletedTask)
            {
                InnerHandler = inner
            };
            using var client = new HttpClient(handler);

            using var response = await client.SendAsync(
                new HttpRequestMessage(HttpMethod.Post, "https://example.test/v1/chat/completions")
                {
                    Content = new StringContent("{}")
                });

            Assert.AreEqual(2, inner.ClientHeaders.Count, "first attempt and the 401 retry");
            CollectionAssert.AreEqual(new[] { "mcp", "mcp" }, inner.ClientHeaders);
        }
        finally
        {
            KernelFactory.ClientName = before;
        }
    }

    /// <summary>Plays back status codes and records the client header of each request.</summary>
    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly Queue<HttpStatusCode> _codes;
        public List<string> ClientHeaders { get; } = new();

        public RecordingHandler(params HttpStatusCode[] codes) => _codes = new Queue<HttpStatusCode>(codes);

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            ClientHeaders.Add(request.Headers.TryGetValues("X-Collab-Client", out var values)
                ? values.Single()
                : null);
            return Task.FromResult(new HttpResponseMessage(_codes.Dequeue()));
        }
    }

    #endregion
}
