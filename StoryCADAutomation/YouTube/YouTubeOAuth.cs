using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace StoryCADAutomation.YouTube;

/// <summary>The "installed" part of a Google Desktop OAuth client file (client_secret.json).</summary>
public sealed record YouTubeClient(string ClientId, string ClientSecret, string AuthUri, string TokenUri);

/// <summary>
///     OAuth 2.0 for `youtube auth` and `youtube upload` (#1626): the installed-app loopback flow
///     with PKCE, the code exchange, and the access-token refresh. Nothing here writes a secret
///     or a token to the console.
/// </summary>
public static class YouTubeOAuth
{
    // force-ssl covers videos.insert, playlistItems.insert, captions.insert and thumbnails.set.
    public const string Scope = "https://www.googleapis.com/auth/youtube.force-ssl";

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
            // offline plus consent, so Google returns a refresh token even on a second auth.
            ["access_type"] = "offline",
            ["prompt"] = "consent",
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

    /// <summary>The token endpoint form for an access-token refresh.</summary>
    internal static Dictionary<string, string> RefreshForm(YouTubeClient client, string refreshToken) => new()
    {
        ["client_id"] = client.ClientId,
        ["client_secret"] = client.ClientSecret,
        ["refresh_token"] = refreshToken,
        ["grant_type"] = "refresh_token",
    };

    /// <summary>
    ///     Exchanges the code from the consent page. Returns the access token and the refresh
    ///     token; throws <see cref="HttpRequestException" /> when Google gives no refresh token.
    /// </summary>
    public static async Task<(string AccessToken, string RefreshToken)> ExchangeCodeAsync(YouTubeClient client,
        string code, string verifier, string redirectUri, HttpClient http, CancellationToken cancellation = default)
    {
        using var json = await PostTokenAsync(client, CodeExchangeForm(client, code, verifier, redirectUri), http,
            cancellation);
        var access = json.RootElement.GetProperty("access_token").GetString()!;
        if (!json.RootElement.TryGetProperty("refresh_token", out var refresh) || refresh.GetString() is not { } token)
        {
            throw new HttpRequestException("Google returned no refresh token.");
        }

        return (access, token);
    }

    /// <summary>A fresh access token from the saved refresh token.</summary>
    public static async Task<string> RefreshAsync(YouTubeClient client, string refreshToken, HttpClient http,
        CancellationToken cancellation = default)
    {
        using var json = await PostTokenAsync(client, RefreshForm(client, refreshToken), http, cancellation);
        return json.RootElement.GetProperty("access_token").GetString()!;
    }

    /// <summary>Writes token.json with the refresh token only.</summary>
    public static void SaveRefreshToken(string path, string refreshToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        // The file sits in the user's profile, which only that user (and administrators) can read.
        File.WriteAllText(path, JsonSerializer.Serialize(new Dictionary<string, string>
        {
            ["refresh_token"] = refreshToken, ["scope"] = Scope,
        }));
    }

    /// <summary>Reads the refresh token from token.json.</summary>
    public static string LoadRefreshToken(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        return document.RootElement.TryGetProperty("refresh_token", out var token) && token.GetString() is { Length: > 0 } value
            ? value
            : throw new InvalidDataException($"{path} has no refresh_token; run `youtube auth` again.");
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
    ///     the user denies consent, or the time runs out.
    /// </summary>
    public static async Task<string> WaitForCodeAsync(HttpListener listener, string state, TimeSpan timeout)
    {
        using var timer = new CancellationTokenSource(timeout);
        while (true)
        {
            var next = listener.GetContextAsync();
            if (await Task.WhenAny(next, Task.Delay(Timeout.Infinite, timer.Token)) != next)
            {
                throw new TimeoutException($"no answer from the browser in {timeout.TotalMinutes:0} minutes.");
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
                Respond(context, HttpStatusCode.BadRequest, "The state did not match. Run youtube auth again.");
                throw new InvalidOperationException("the redirect's state did not match; no token saved.");
            }

            if (error is not null)
            {
                Respond(context, HttpStatusCode.OK, "Authorization was not given. You can close this tab.");
                throw new InvalidOperationException($"Google returned '{error}'; no token saved.");
            }

            Respond(context, HttpStatusCode.OK, "StoryCAD automation is authorized. You can close this tab.");
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
