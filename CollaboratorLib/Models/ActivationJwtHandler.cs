using System.Net;
using System.Net.Http.Headers;

namespace StoryCollaborator.Models;

/// <summary>
/// HTTP pipeline handler for Semantic Kernel chat: stamps the current activation JWT on each
/// request, and on 401 reactivates once and retries once (Collaborator #95). Matches the
/// workflow path's per-call credential + single retry policy without rebuilding the kernel.
/// </summary>
internal sealed class ActivationJwtHandler : DelegatingHandler
{
    private readonly Func<string?> _resolveCredential;
    private readonly Func<Task> _reactivate;

    public ActivationJwtHandler(Func<string?> resolveCredential, Func<Task> reactivate)
    {
        _resolveCredential = resolveCredential ?? throw new ArgumentNullException(nameof(resolveCredential));
        _reactivate = reactivate ?? throw new ArgumentNullException(nameof(reactivate));
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Buffer body once so a retry can resend (HttpRequestMessage content is single-use).
        byte[]? body = null;
        MediaTypeHeaderValue? contentType = null;
        if (request.Content is not null)
        {
            contentType = request.Content.Headers.ContentType;
            body = await request.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
            request.Content = CreateContent(body, contentType);
        }

        // Last write wins within a scope: a request carrying no cost header records null and
        // overwrites an earlier capture. Correct for the pipelines that exist today (the
        // OpenAI client's own retries run failure-then-success, so the priced response is
        // last), but a chat service issuing several completions per call — SK function
        // calling, if it is ever enabled here — would report only the final one.
        var response = await SendAttemptAsync(request, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode != HttpStatusCode.Unauthorized)
        {
            ChatCostCapture.Record(ProxyCostParser.TryParseHeader(response));
            return response;
        }

        response.Dispose();
        await _reactivate().ConfigureAwait(false);

        using var retry = CloneRequest(request, body, contentType);
        var retried = await SendAttemptAsync(retry, cancellationToken).ConfigureAwait(false);

        // Cost is read from the response that actually carried the answer. The 401 above
        // never reached the model, so it has no cost to report and must not overwrite one.
        ChatCostCapture.Record(ProxyCostParser.TryParseHeader(retried));
        return retried;
    }

    private async Task<HttpResponseMessage> SendAttemptAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var credential = _resolveCredential();
        if (string.IsNullOrWhiteSpace(credential))
            return new HttpResponseMessage(HttpStatusCode.Unauthorized);

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", credential);
        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }

    private static HttpRequestMessage CloneRequest(
        HttpRequestMessage original, byte[]? body, MediaTypeHeaderValue? contentType)
    {
        var clone = new HttpRequestMessage(original.Method, original.RequestUri)
        {
            Version = original.Version
        };

        foreach (var header in original.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

        if (body is not null)
            clone.Content = CreateContent(body, contentType);

        return clone;
    }

    private static ByteArrayContent CreateContent(byte[] body, MediaTypeHeaderValue? contentType)
    {
        var content = new ByteArrayContent(body);
        if (contentType is not null)
            content.Headers.ContentType = contentType;
        return content;
    }
}
