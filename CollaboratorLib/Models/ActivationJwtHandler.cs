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

        var response = await SendAttemptAsync(request, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode != HttpStatusCode.Unauthorized)
            return response;

        response.Dispose();
        await _reactivate().ConfigureAwait(false);

        using var retry = CloneRequest(request, body, contentType);
        return await SendAttemptAsync(retry, cancellationToken).ConfigureAwait(false);
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
