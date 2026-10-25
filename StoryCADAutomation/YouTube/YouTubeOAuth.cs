using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace StoryCADAutomation.YouTube;

/// <summary>The "installed" part of a Google Desktop OAuth client file (client_secret.json).</summary>
public sealed record YouTubeClient(string ClientId, string ClientSecret, string AuthUri, string TokenUri);

/// <summary>
///     OAuth 2.0 for `youtube upload` (#1626): the installed-app loopback flow with PKCE, the
///     code exchange, and the revoke at the end of the run. The access token stays in memory;
///     nothing is saved (Terry, 2026-10-23). Nothing here writes a secret or a token to the console.
/// </summary>
public static class YouTubeOAuth
{
    // force-ssl covers videos.insert, playlistItems.insert, captions.insert and thumbnails.set.
    public const string Scope = "https://www.googleapis.com/auth/youtube.force-ssl";

    /// <summary>Google's token revocation endpoint.</summary>
    public const string RevokeUri = "https://oauth2.googleapis.com/revoke";

    /// <summary>Reads the Desktop client file Google gives for download.</summary>
    public static YouTubeClient LoadClient(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        if (!document.RootElement.TryGetProperty("installed", out var installed))
        {
            throw new InvalidDataException(
                $"{path} has no \"installed\" section; download the client file for a Desktop app OAuth client.");
        }

        string Field(string name) =>
            installed.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String &&
            !string.IsNullOrWhiteSpace(value.GetString())
                ? value.GetString()!
                : throw new InvalidDataException($"{path} has no installed.{name}.");

        return new YouTubeClient(Field("client_id"), Field("client_secret"), Field("auth_uri"), Field("token_uri"));
    }

    /// <summary>A PKCE code verifier: 32 random bytes, base64url, 43 characters (RFC 7636 section 4.1).</summary>
    public static string NewVerifier() => Base64Url(RandomNumberGenerator.GetBytes(32));

    /// <summary>The S256 code challenge for a verifier (RFC 7636 section 4.2).</summary>
    public static string Challenge(string verifier) => Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));

    /// <summary>The consent page address the browser opens.</summary>
    public static string ConsentUrl(YouTubeClient client, string redirectUri, string challenge, string state)
    {
        var query = Query(new Dictionary<string, string>
        {
            ["client_id"] = client.ClientId,
            ["redirect_uri"] = redirectUri,
            ["response_type"] = "code",
            ["scope"] = Scope,
            ["code_challenge"] = challenge,
            ["code_challenge_method"] = "S256",
            // Google gives an installed app a refresh token whatever access_type says, so the
            // parameter is left out. The run never keeps that refresh token, and revoking the
            // access token revokes the refresh token linked to it. select_account shows the
            // account and channel chooser on each run; the consent page shows because each run
            // revokes its grant.
            ["prompt"] = "select_account",
            ["state"] = state,
        });
        return client.AuthUri + (client.AuthUri.Contains('?') ? "&" : "?") + query;
    }

    /// <summary>The token endpoint form for the authorization-code exchange.</summary>
    internal static Dictionary<string, string> CodeExchangeForm(YouTubeClient client, string code, string verifier,
        string redirectUri) => new()
    {
        ["code"] = code,
        ["client_id"] = client.ClientId,
        ["client_secret"] = client.ClientSecret,
        ["redirect_uri"] = redirectUri,
        ["grant_type"] = "authorization_code",
        ["code_verifier"] = verifier,
    };

    /// <summary>Exchanges the code from the consent page for an access token.</summary>
    public static async Task<string> ExchangeCodeAsync(YouTubeClient client, string code, string verifier,
        string redirectUri, HttpClient http, CancellationToken cancellation = default)
    {
        using var json = await PostTokenAsync(client, CodeExchangeForm(client, code, verifier, redirectUri), http,
            cancellation);
        return json.RootElement.GetProperty("access_token").GetString()!;
    }

    /// <summary>
    ///     Revokes the token at Google, so it stops working before its hour is up. Throws
    ///     <see cref="HttpRequestException" /> with Google's error, never the token.
    /// </summary>
    public static async Task RevokeAsync(string token, HttpClient http, CancellationToken cancellation = default)
    {
        using var content = new FormUrlEncodedContent(new Dictionary<string, string> { ["token"] = token });
        using var response = await http.PostAsync(RevokeUri, content, cancellation);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellation);
            throw new HttpRequestException(
                $"the revoke endpoint returned {(int)response.StatusCode}: {YouTubeApi.ErrorDetail(body)}", null,
                response.StatusCode);
        }
    }

    /// <summary>An unused loopback port for the redirect listener.</summary>
    public static int FreePort()
    {
        var probe = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        var port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();
        return port;
    }

    /// <summary>
    ///     Waits for the browser's redirect and returns the code. Requests without a code or an
    ///     error (a favicon, say) are answered and ignored. Throws when the state does not match,
    ///     the user denies consent, or the time runs out; throws <see cref="OperationCanceledException" />
    ///     when <paramref name="cancellation" /> is cancelled (Ctrl+C).
    /// </summary>
    public static async Task<string> WaitForCodeAsync(HttpListener listener, string state, TimeSpan timeout,
        CancellationToken cancellation = default)
    {
        using var timer = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        timer.CancelAfter(timeout);
        while (true)
        {
            var next = listener.GetContextAsync();
            if (await Task.WhenAny(next, Task.Delay(Timeout.Infinite, timer.Token)) != next)
            {
                cancellation.ThrowIfCancellationRequested();
                throw new TimeoutException($"no answer from the browser in {timeout.TotalSeconds:0} s.");
            }

            var context = await next;
            var query = context.Request.QueryString;
            var error = query["error"];
            var code = query["code"];
            if (error is null && code is null)
            {
                Respond(context, HttpStatusCode.NotFound, "Not found.");
                continue;
            }

            // A state from another page would let that page hand us its own code.
            if (query["state"] != state)
            {
                Respond(context, HttpStatusCode.BadRequest, "The state did not match. Run youtube upload again.");
                throw new InvalidOperationException("the redirect's state did not match; nothing uploaded.");
            }

            if (error is not null)
            {
                Respond(context, HttpStatusCode.OK, "Authorization was not given. You can close this tab.");
                throw new InvalidOperationException($"Google returned '{error}'; nothing uploaded.");
            }

            Respond(context, HttpStatusCode.OK, "StoryCAD automation is signed in. You can close this tab.");
            return code!;
        }
    }

    private static void Respond(HttpListenerContext context, HttpStatusCode status, string text)
    {
        var body = Encoding.UTF8.GetBytes($"<!doctype html><meta charset=\"utf-8\"><title>StoryCAD</title><p>{WebUtility.HtmlEncode(text)}</p>");
        context.Response.StatusCode = (int)status;
        context.Response.ContentType = "text/html; charset=utf-8";
        context.Response.ContentLength64 = body.Length;
        context.Response.OutputStream.Write(body);
        context.Response.Close();
    }

    private static async Task<JsonDocument> PostTokenAsync(YouTubeClient client, Dictionary<string, string> form,
        HttpClient http, CancellationToken cancellation)
    {
        using var content = new FormUrlEncodedContent(form);
        using var response = await http.PostAsync(client.TokenUri, content, cancellation);
        var body = await response.Content.ReadAsStringAsync(cancellation);
        if (!response.IsSuccessStatusCode)
        {
            // An error body has only error and error_description, never a token; still, print those two only.
            throw new HttpRequestException(
                $"the token endpoint returned {(int)response.StatusCode}: {YouTubeApi.ErrorDetail(body)}", null,
                response.StatusCode);
        }

        return JsonDocument.Parse(body);
    }

    private static string Query(Dictionary<string, string> values) =>
        string.Join("&", values.Select(p => Uri.EscapeDataString(p.Key) + "=" + Uri.EscapeDataString(p.Value)));

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
