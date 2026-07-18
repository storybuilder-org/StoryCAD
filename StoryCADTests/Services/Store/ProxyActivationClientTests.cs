using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCADLib.Services.Logging;
using StoryCADLib.Services.Store;

#nullable disable

namespace StoryCADTests.Services.Store;

/// <summary>
///     Exercises <see cref="ProxyActivationClient" /> against the activation contract
///     (StoryCADWiki: wiki/repos/Collaborator/sources/iap-billing-docs.md) with a fake
///     <see cref="HttpMessageHandler" />, so no network is touched. Covers the 200-with-token,
///     403-per-refusal-reason, unexpected-status, unparseable-body, missing-expiresAt, transport
///     failure, and cancellation paths — the "do not rename members" wire logic.
/// </summary>
[TestClass]
public class ProxyActivationClientTests
{
    private static readonly PurchaseProof SampleProof =
        new("apple", "sample-jws", "org.storycad.collaborator.monthly", "11111111-1111-1111-1111-111111111111");

    private static ProxyActivationClient CreateClient(StubHttpMessageHandler handler) =>
        new(Ioc.Default.GetRequiredService<ILogService>(), new HttpClient(handler));

    // ── ActivateAsync: success ───────────────────────────────────────────────

    [TestMethod]
    public async Task ActivateAsync_200WithToken_ReturnsOkWithJwtAndExpiry()
    {
        var handler = StubHttpMessageHandler.Returning(HttpStatusCode.OK,
            "{\"ok\":true,\"jwt\":\"jwt-token\",\"expiresAt\":\"2026-08-01T00:00:00Z\"}");
        var client = CreateClient(handler);

        var response = await client.ActivateAsync(SampleProof);

        Assert.IsTrue(response.Ok);
        Assert.AreEqual("jwt-token", response.Jwt);
        Assert.AreEqual(new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc), response.ExpiresAtUtc);
        Assert.IsNull(response.Reason);
    }

    [TestMethod]
    public async Task ActivateAsync_200MissingExpiresAt_ReturnsOkWithNullExpiry()
    {
        var handler = StubHttpMessageHandler.Returning(HttpStatusCode.OK, "{\"ok\":true,\"jwt\":\"jwt-token\"}");
        var client = CreateClient(handler);

        var response = await client.ActivateAsync(SampleProof);

        Assert.IsTrue(response.Ok);
        Assert.AreEqual("jwt-token", response.Jwt);
        Assert.IsNull(response.ExpiresAtUtc);
    }

    [TestMethod]
    public async Task ActivateAsync_PostsToActivateWithProofBody()
    {
        var handler = StubHttpMessageHandler.Returning(HttpStatusCode.OK, "{\"ok\":true,\"jwt\":\"jwt-token\"}");
        var client = CreateClient(handler);

        await client.ActivateAsync(SampleProof);

        Assert.AreEqual(HttpMethod.Post, handler.LastRequest.Method);
        StringAssert.EndsWith(handler.LastRequest.RequestUri.AbsolutePath, "/activate");
        StringAssert.Contains(handler.LastRequestBody, "\"payload\":\"sample-jws\"");
        StringAssert.Contains(handler.LastRequestBody, "\"platform\":\"apple\"");
    }

    // ── ActivateAsync: authenticated refusal (403) ───────────────────────────

    [DataTestMethod]
    [DataRow("invalid")]
    [DataRow("revoked")]
    [DataRow("expired")]
    public async Task ActivateAsync_403WithReason_ReturnsRefusalWithReason(string reason)
    {
        var handler = StubHttpMessageHandler.Returning(HttpStatusCode.Forbidden,
            $"{{\"ok\":false,\"reason\":\"{reason}\"}}");
        var client = CreateClient(handler);

        var response = await client.ActivateAsync(SampleProof);

        Assert.IsFalse(response.Ok);
        Assert.AreEqual(reason, response.Reason);
        Assert.IsNull(response.Jwt);
    }

    [TestMethod]
    public async Task ActivateAsync_403WithoutReason_DefaultsToInvalid()
    {
        var handler = StubHttpMessageHandler.Returning(HttpStatusCode.Forbidden, "{\"ok\":false}");
        var client = CreateClient(handler);

        var response = await client.ActivateAsync(SampleProof);

        Assert.IsFalse(response.Ok);
        Assert.AreEqual("invalid", response.Reason);
    }

    // ── ActivateAsync: unreachable / try-later ───────────────────────────────

    [DataTestMethod]
    [DataRow(HttpStatusCode.InternalServerError)]
    [DataRow(HttpStatusCode.TooManyRequests)]
    [DataRow(HttpStatusCode.BadRequest)]
    public async Task ActivateAsync_UnexpectedStatus_ThrowsUnreachable(HttpStatusCode status)
    {
        var handler = StubHttpMessageHandler.Returning(status, "{}");
        var client = CreateClient(handler);

        await Assert.ThrowsExactlyAsync<StoreActivationUnreachableException>(
            () => client.ActivateAsync(SampleProof));
    }

    [TestMethod]
    public async Task ActivateAsync_UnparseableBody_ThrowsUnreachable()
    {
        var handler = StubHttpMessageHandler.Returning(HttpStatusCode.OK, "not-json");
        var client = CreateClient(handler);

        await Assert.ThrowsExactlyAsync<StoreActivationUnreachableException>(
            () => client.ActivateAsync(SampleProof));
    }

    [TestMethod]
    public async Task ActivateAsync_TransportFailure_ThrowsUnreachable()
    {
        var handler = StubHttpMessageHandler.Throwing(new HttpRequestException("connection refused"));
        var client = CreateClient(handler);

        await Assert.ThrowsExactlyAsync<StoreActivationUnreachableException>(
            () => client.ActivateAsync(SampleProof));
    }

    [TestMethod]
    public async Task ActivateAsync_Cancelled_PropagatesCancellation()
    {
        // A cancelled token must surface as cancellation, not as "unreachable", so a shutdown
        // mid-activation stays quiet rather than warning the user (StoreActivationService #7).
        var handler = StubHttpMessageHandler.Throwing(new TaskCanceledException());
        var client = CreateClient(handler);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsExactlyAsync<TaskCanceledException>(
            () => client.ActivateAsync(SampleProof, cts.Token));
    }

    // ── GetStoreTicketAsync ──────────────────────────────────────────────────

    [TestMethod]
    public async Task GetStoreTicketAsync_200WithTicket_ReturnsTicket()
    {
        var handler = StubHttpMessageHandler.Returning(HttpStatusCode.OK, "{\"ticket\":\"aad-token\"}");
        var client = CreateClient(handler);

        Assert.AreEqual("aad-token", await client.GetStoreTicketAsync());
    }

    [TestMethod]
    public async Task GetStoreTicketAsync_NonSuccess_ReturnsNull()
    {
        var handler = StubHttpMessageHandler.Returning(HttpStatusCode.InternalServerError, "{}");
        var client = CreateClient(handler);

        Assert.IsNull(await client.GetStoreTicketAsync());
    }

    [TestMethod]
    public async Task GetStoreTicketAsync_TransportFailure_ReturnsNull()
    {
        var handler = StubHttpMessageHandler.Throwing(new HttpRequestException("connection refused"));
        var client = CreateClient(handler);

        Assert.IsNull(await client.GetStoreTicketAsync());
    }

    [TestMethod]
    public async Task GetStoreTicketAsync_DefaultPurpose_NoQueryString()
    {
        // issue #90 design section 10 step 10 correction: the default must stay byte-for-byte the
        // path PR #1470's shipped GetStoreTicketAsync() call already uses, so existing subscription
        // activation is unaffected by the collections-purpose addition below.
        var handler = StubHttpMessageHandler.Returning(HttpStatusCode.OK, "{\"ticket\":\"aad-token\"}");
        var client = CreateClient(handler);

        await client.GetStoreTicketAsync();

        Assert.IsTrue(handler.LastRequest.RequestUri!.AbsolutePath.EndsWith("/store/ticket"));
        Assert.IsTrue(string.IsNullOrEmpty(handler.LastRequest.RequestUri.Query));
    }

    [TestMethod]
    public async Task GetStoreTicketAsync_CollectionsPurpose_AddsQueryString()
    {
        var handler = StubHttpMessageHandler.Returning(HttpStatusCode.OK, "{\"ticket\":\"aad-token-collections\"}");
        var client = CreateClient(handler);

        var ticket = await client.GetStoreTicketAsync("collections");

        Assert.AreEqual("aad-token-collections", ticket);
        Assert.AreEqual("?purpose=collections", handler.LastRequest.RequestUri!.Query);
    }

    // ── Test double ──────────────────────────────────────────────────────────

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly string _body;
        private readonly Exception _throw;

        public HttpRequestMessage LastRequest;
        public string LastRequestBody;

        private StubHttpMessageHandler(HttpStatusCode status, string body, Exception ex)
        {
            _status = status;
            _body = body;
            _throw = ex;
        }

        public static StubHttpMessageHandler Returning(HttpStatusCode status, string body) =>
            new(status, body, null);

        public static StubHttpMessageHandler Throwing(Exception ex) => new(default, null, ex);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;
            if (request.Content is not null)
            {
                LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);
            }

            if (_throw is not null)
            {
                throw _throw;
            }

            return new HttpResponseMessage(_status)
            {
                Content = new StringContent(_body, Encoding.UTF8, "application/json")
            };
        }
    }
}
