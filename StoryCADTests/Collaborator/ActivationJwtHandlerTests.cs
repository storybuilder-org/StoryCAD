using System.Net;
using System.Net.Http;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCollaborator.Models;

#nullable disable

namespace StoryCADTests.Collaborator;

/// <summary>
/// Collaborator #95: chat HTTP pipeline stamps the current activation JWT per request and
/// heals once on 401. Offline tests with a scripted inner handler and injected delegates
/// (same pattern as CollaboratorTests/WorkflowRunner401Tests; no Moq).
/// </summary>
[TestClass]
public class ActivationJwtHandlerTests
{
    [TestMethod]
    public async Task SendAsync_UsesLatestCredentialOnEveryRequest()
    {
        var tokens = new Queue<string>(new[] { "A", "B" });
        var inner = new ScriptedHandler(HttpStatusCode.OK, HttpStatusCode.OK);
        using var handler = new ActivationJwtHandler(() => tokens.Dequeue(), () => Task.CompletedTask)
        {
            InnerHandler = inner
        };
        using var client = new HttpClient(handler);

        using var r1 = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://example.test/1"));
        using var r2 = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://example.test/2"));

        Assert.AreEqual(HttpStatusCode.OK, r1.StatusCode);
        Assert.AreEqual(HttpStatusCode.OK, r2.StatusCode);
        Assert.AreEqual(2, inner.Requests.Count);
        Assert.AreEqual("Bearer A", inner.Requests[0].Authorization);
        Assert.AreEqual("Bearer B", inner.Requests[1].Authorization);
    }

    [TestMethod]
    public async Task SendAsync_On401_ReactivatesOnceAndRetries()
    {
        var credential = "stale";
        var reactivateCount = 0;
        var body = "{\"messages\":[{\"role\":\"user\",\"content\":\"hi\"}]}";
        var inner = new ScriptedHandler(HttpStatusCode.Unauthorized, HttpStatusCode.OK);
        using var handler = new ActivationJwtHandler(
            () => credential,
            () =>
            {
                reactivateCount++;
                credential = "fresh";
                return Task.CompletedTask;
            })
        {
            InnerHandler = inner
        };
        using var client = new HttpClient(handler);

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://example.test/v1/chat/completions")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        using var response = await client.SendAsync(request);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual(1, reactivateCount, "reactivation must run once before the retry");
        Assert.AreEqual(2, inner.Requests.Count, "exactly one retry: two sends total");
        Assert.AreEqual("Bearer stale", inner.Requests[0].Authorization);
        Assert.AreEqual("Bearer fresh", inner.Requests[1].Authorization);
        Assert.AreEqual(HttpMethod.Post, inner.Requests[0].Method);
        Assert.AreEqual(HttpMethod.Post, inner.Requests[1].Method);
        Assert.AreEqual(inner.Requests[0].Uri, inner.Requests[1].Uri);
        Assert.AreEqual(body, inner.Requests[0].Body);
        Assert.AreEqual(body, inner.Requests[1].Body,
            "retry must resend the same JSON body");
    }

    [TestMethod]
    public async Task SendAsync_Second401_DoesNotReactivateAgain()
    {
        var reactivateCount = 0;
        var inner = new ScriptedHandler(HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized);
        using var handler = new ActivationJwtHandler(
            () => "token",
            () =>
            {
                reactivateCount++;
                return Task.CompletedTask;
            })
        {
            InnerHandler = inner
        };
        using var client = new HttpClient(handler);

        using var response = await client.SendAsync(
            new HttpRequestMessage(HttpMethod.Get, "https://example.test/chat"));

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.AreEqual(1, reactivateCount, "reactivation must happen once, never loop");
        Assert.AreEqual(2, inner.Requests.Count, "exactly one retry: two sends total, never more");
    }

    [TestMethod]
    public async Task SendAsync_NoCredential_Returns401WithoutNetwork()
    {
        var inner = new ScriptedHandler(HttpStatusCode.OK);
        using var handler = new ActivationJwtHandler(() => null, () => Task.CompletedTask)
        {
            InnerHandler = inner
        };
        using var client = new HttpClient(handler);

        using var response = await client.SendAsync(
            new HttpRequestMessage(HttpMethod.Get, "https://example.test/chat"));

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.AreEqual(0, inner.Requests.Count, "must not call the network without a credential");
    }

    /// <summary>
    /// Records every request that reaches the transport; returns scripted status codes in order.
    /// </summary>
    private sealed class ScriptedHandler : HttpMessageHandler
    {
        private readonly Queue<HttpStatusCode> _statuses;

        public List<RecordedRequest> Requests { get; } = new();

        public ScriptedHandler(params HttpStatusCode[] statuses)
        {
            _statuses = new Queue<HttpStatusCode>(statuses);
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string body = null;
            if (request.Content is not null)
                body = await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            Requests.Add(new RecordedRequest(
                request.Method,
                request.RequestUri?.ToString(),
                request.Headers.Authorization?.ToString(),
                body));

            var status = _statuses.Count > 0 ? _statuses.Dequeue() : HttpStatusCode.OK;
            return new HttpResponseMessage(status);
        }
    }

    private sealed record RecordedRequest(
        HttpMethod Method, string Uri, string Authorization, string Body);
}
