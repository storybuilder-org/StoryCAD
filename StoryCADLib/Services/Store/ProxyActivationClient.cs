using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;

namespace StoryCADLib.Services.Store;

// Wire body for POST /activate (devdocs/iap/activation-contract.md; do not rename members).
// Source-generated like StoreKitJsonContext so the desktop head stays trimming-friendly.
internal sealed class ActivationRequest
{
    [JsonPropertyName("platform")] public string Platform { get; set; }
    [JsonPropertyName("payload")] public string Payload { get; set; }
    [JsonPropertyName("productId")] public string ProductId { get; set; }
    [JsonPropertyName("userGuid")] public string UserGuid { get; set; }
}

[JsonSerializable(typeof(ActivationRequest))]
internal partial class ActivationJsonContext : JsonSerializerContext
{
}

/// <summary>
///     Thrown when the Worker activation endpoint cannot be reached or returns an
///     unexpected status. Distinct from an authenticated refusal, which comes back as an
///     <see cref="ActivationResponse" /> with <c>Ok == false</c>.
/// </summary>
public sealed class StoreActivationUnreachableException : Exception
{
    public StoreActivationUnreachableException(string message, Exception inner = null) : base(message, inner)
    {
    }
}

/// <summary>
///     Posts purchase proof to the Collaborator proxy Worker's <c>/activate</c> endpoint.
///     Interim authentication reuses the shared <c>COLLAB_PROXY_TOKEN</c> Bearer channel that
///     the Collaborator workflow client uses today; issue #30's per-install channel is a
///     Worker-track prerequisite that will replace it.
/// </summary>
public sealed class ProxyActivationClient : IActivationClient
{
    // Mirrors CollaboratorLib KernelFactory.DefaultProxyBaseUrl. StoryCADLib does not
    // reference CollaboratorLib, so the default is duplicated here; COLLAB_PROXY_URL overrides it.
    private const string DefaultProxyBaseUrl =
        "https://storycad-collaborator-proxy-production.storybuilder-foundation.workers.dev/v1";

    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(30) };

    private readonly ILogService _logService;

    // Resolved once: the class is a DI singleton and the proxy channel cannot change mid-process.
    private readonly string _baseUrl;
    private readonly string _token;

    public ProxyActivationClient(ILogService logService)
    {
        _logService = logService;
        _baseUrl = Environment.GetEnvironmentVariable("COLLAB_PROXY_URL") ?? DefaultProxyBaseUrl;
        _token = Environment.GetEnvironmentVariable("COLLAB_PROXY_TOKEN");
    }

    // Single home for the proxy-channel wiring (base URL + Bearer auth); issue #30's per-install
    // credential swap lands here once, for every endpoint.
    private HttpRequestMessage BuildRequest(HttpMethod method, string path)
    {
        var request = new HttpRequestMessage(method, $"{_baseUrl}{path}");
        if (!string.IsNullOrWhiteSpace(_token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        }

        return request;
    }

    public async Task<ActivationResponse> ActivateAsync(PurchaseProof proof, CancellationToken ct = default)
    {
        var body = JsonSerializer.Serialize(new ActivationRequest
        {
            Platform = proof.Platform,
            Payload = proof.Payload,
            ProductId = proof.ProductId,
            UserGuid = proof.UserGuid
        }, ActivationJsonContext.Default.ActivationRequest);

        using var request = BuildRequest(HttpMethod.Post, "/activate");
        request.Content = new StringContent(body, Encoding.UTF8, "application/json");

        HttpResponseMessage response;
        try
        {
            response = await HttpClient.SendAsync(request, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new StoreActivationUnreachableException("Store activation endpoint is unreachable.", ex);
        }

        using var _ = response;
        var content = await response.Content.ReadAsStringAsync(ct);

        // 200 = activated, 403 = authenticated refusal (both carry a JSON body). Anything else
        // (400 malformed, 429 rate-limited, 5xx) is treated as unreachable/try-later.
        if (response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Forbidden)
        {
            return Parse(content);
        }

        throw new StoreActivationUnreachableException(
            $"Store activation returned unexpected status {(int)response.StatusCode}.");
    }

    public async Task<string> GetStoreTicketAsync(CancellationToken ct = default)
    {
        // GET /store/ticket -> {"ticket":"<aad-access-token>"} (Worker contract, Windows only).
        using var request = BuildRequest(HttpMethod.Get, "/store/ticket");

        try
        {
            using var response = await HttpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logService.Log(LogLevel.Warn,
                    $"Store ticket endpoint returned status {(int)response.StatusCode}.");
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(content);
            return doc.RootElement.TryGetProperty("ticket", out var ticketEl) ? ticketEl.GetString() : null;
        }
        catch (Exception ex)
        {
            // Non-throwing: no ticket -> no Microsoft proof -> activation reports NotPurchased and
            // offers retry, rather than surfacing an exception up the proof path.
            _logService.Log(LogLevel.Warn, $"Failed to obtain store ticket: {ex.Message}");
            return null;
        }
    }

    private ActivationResponse Parse(string content)
    {
        try
        {
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;
            var ok = root.TryGetProperty("ok", out var okEl) && okEl.ValueKind == JsonValueKind.True;

            if (ok)
            {
                var jwt = root.TryGetProperty("jwt", out var jwtEl) ? jwtEl.GetString() : null;
                // Shared wire-date rule; a malformed expiresAt degrades to null (the caller's
                // conservative fallback lifetime) rather than discarding a valid JWT.
                var expiresAt = root.TryGetProperty("expiresAt", out var expEl) && expEl.ValueKind == JsonValueKind.String
                    ? StoreKitPayloads.ParseDate(expEl.GetString())
                    : null;

                return new ActivationResponse(true, jwt, expiresAt, null);
            }

            var reason = root.TryGetProperty("reason", out var reasonEl) ? reasonEl.GetString() : "invalid";
            return new ActivationResponse(false, null, null, reason);
        }
        catch (Exception ex)
        {
            // A body we can't parse is not a valid refusal; treat as unreachable/try-later.
            throw new StoreActivationUnreachableException("Store activation returned an unparseable response.", ex);
        }
    }
}
