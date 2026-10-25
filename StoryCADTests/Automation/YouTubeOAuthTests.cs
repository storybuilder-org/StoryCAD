#if WINDOWS10_0_22621_0_OR_GREATER
using System.Net;
using StoryCADAutomation.YouTube;

namespace StoryCADTests.Automation;

/// <summary>
///     OAuth for the youtube command (#1626): client file, PKCE, consent address and token
///     requests. Google is replaced by a fake handler; no request leaves the machine.
/// </summary>
[TestClass]
public class YouTubeOAuthTests
{
    internal static readonly YouTubeClient Client = new("client-id.apps.googleusercontent.com", "CLIENT-SECRET-123",
        "https://accounts.google.com/o/oauth2/auth", "https://oauth2.googleapis.com/token");

    private string _folder = null!;

    [TestInitialize]
    public void Setup()
    {
        _folder = Path.Combine(Path.GetTempPath(), "StoryCADAutomation", "youtube-oauth-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_folder);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, true);
        }
    }

    private static Dictionary<string, string> ParseQuery(string query) =>
        query.TrimStart('?').Split('&').Select(p => p.Split('=', 2))
            .ToDictionary(p => Uri.UnescapeDataString(p[0]), p => Uri.UnescapeDataString(p[1]));

    [TestMethod]
    public void LoadClient_WithInstalledFile_ReadsFourFields()
    {
        var path = Path.Combine(_folder, "client_secret.json");
        File.WriteAllText(path, """
            {"installed":{"client_id":"id1","project_id":"p","auth_uri":"https://a/auth","token_uri":"https://a/token",
             "client_secret":"s1","redirect_uris":["http://localhost"]}}
            """);

        Assert.AreEqual(new YouTubeClient("id1", "s1", "https://a/auth", "https://a/token"), YouTubeOAuth.LoadClient(path));
    }

    [TestMethod]
    public void LoadClient_WithWebClientFile_ThrowsInvalidData()
    {
        var path = Path.Combine(_folder, "client_secret.json");
        File.WriteAllText(path, """{"web":{"client_id":"id1"}}""");

        Assert.ThrowsExactly<InvalidDataException>(() => YouTubeOAuth.LoadClient(path));
    }

    [TestMethod]
    public void NewVerifier_Always_Returns43UrlSafeCharacters()
    {
        var verifier = YouTubeOAuth.NewVerifier();

        Assert.AreEqual(43, verifier.Length);
        Assert.IsTrue(verifier.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_'), verifier);
    }

    [TestMethod]
    public void Challenge_WithKnownVerifier_ReturnsBase64UrlSha256()
    {
        // Expected value from Python: base64.urlsafe_b64encode(hashlib.sha256(v).digest()).rstrip(b'=').
        Assert.AreEqual("zxX-3A5Py2DLOwOF8ROq6OqvTOhosb0IQugzheMx16M",
            YouTubeOAuth.Challenge("StoryCAD-pkce-test-verifier-0123456789-abcdefgh"));
    }

    [TestMethod]
    public void ConsentUrl_Always_CarriesScopePkceAndRedirect()
    {
        var url = new Uri(YouTubeOAuth.ConsentUrl(Client, "http://127.0.0.1:50123/", "CHALLENGE", "STATE"));
        var query = ParseQuery(url.Query);

        Assert.AreEqual("https://accounts.google.com/o/oauth2/auth", url.GetLeftPart(UriPartial.Path));
        Assert.AreEqual("https://www.googleapis.com/auth/youtube.force-ssl", query["scope"]);
        Assert.AreEqual("CHALLENGE", query["code_challenge"]);
        Assert.AreEqual("S256", query["code_challenge_method"]);
        Assert.AreEqual("http://127.0.0.1:50123/", query["redirect_uri"]);
        Assert.AreEqual("code", query["response_type"]);
        // No refresh token is wanted (#1626, Terry, 2026-10-23); the chooser shows on each run.
        Assert.IsFalse(query.ContainsKey("access_type"));
        Assert.AreEqual("select_account", query["prompt"]);
        Assert.AreEqual("STATE", query["state"]);
        Assert.AreEqual(Client.ClientId, query["client_id"]);
    }

    [TestMethod]
    public void ConsentUrl_Always_LeavesOutClientSecret()
    {
        StringAssert.DoesNotMatch(YouTubeOAuth.ConsentUrl(Client, "http://127.0.0.1:1/", "C", "S"),
            new System.Text.RegularExpressions.Regex(Client.ClientSecret));
    }

    [TestMethod]
    public async Task ExchangeCodeAsync_Always_PostsCodeVerifierAndRedirect()
    {
        var fake = new FakeGoogle();

        var access = await YouTubeOAuth.ExchangeCodeAsync(Client, "CODE-1", "VERIFIER-1",
            "http://127.0.0.1:50123/", new HttpClient(fake));

        Assert.AreEqual(FakeGoogle.AccessToken, access);
        var form = ParseQuery(fake.Requests.Single().Body);
        Assert.AreEqual("authorization_code", form["grant_type"]);
        Assert.AreEqual("CODE-1", form["code"]);
        Assert.AreEqual("VERIFIER-1", form["code_verifier"]);
        Assert.AreEqual("http://127.0.0.1:50123/", form["redirect_uri"]);
    }

    [TestMethod]
    public async Task RevokeAsync_Always_PostsTheTokenToTheRevokeEndpoint()
    {
        var fake = new FakeGoogle();

        await YouTubeOAuth.RevokeAsync("ACCESS-1", new HttpClient(fake));

        var request = fake.Requests.Single();
        Assert.AreEqual(HttpMethod.Post, request.Method);
        Assert.AreEqual("https://oauth2.googleapis.com/revoke", request.Url);
        Assert.AreEqual("application/x-www-form-urlencoded", request.ContentType);
        CollectionAssert.AreEquivalent(new[] { "token" }, ParseQuery(request.Body).Keys.ToArray());
        Assert.AreEqual("ACCESS-1", ParseQuery(request.Body)["token"]);
    }

    [TestMethod]
    public async Task RevokeAsync_WhenGoogleRefuses_ThrowsWithGoogleErrorOnly()
    {
        var fake = new FakeGoogle { RevokeStatus = HttpStatusCode.BadRequest };

        var ex = await Assert.ThrowsExactlyAsync<HttpRequestException>(
            () => YouTubeOAuth.RevokeAsync("ACCESS-1", new HttpClient(fake)));

        StringAssert.Contains(ex.Message, "invalid_token");
        Assert.IsFalse(ex.Message.Contains("ACCESS-1", StringComparison.Ordinal), ex.Message);
    }

    // The browser redirect (#1626 review): a real loopback listener, requests from HttpClient.
    private static async Task<(HttpListener Listener, string Uri)> Listen()
    {
        var listener = YouTubeCommand.StartListener(YouTubeOAuth.FreePort(), out var uri);
        await Task.Yield();
        return (listener, uri);
    }

    [TestMethod]
    public async Task WaitForCodeAsync_WithStrayRequestFirst_IgnoresItAndReturnsTheCode()
    {
        var (listener, uri) = await Listen();
        using var _ = listener;
        using var http = new HttpClient();
        var wait = YouTubeOAuth.WaitForCodeAsync(listener, "STATE-1", TimeSpan.FromSeconds(10));

        var stray = await http.GetAsync(uri + "favicon.ico");
        var redirect = await http.GetAsync(uri + "?code=CODE-1&state=STATE-1");

        Assert.AreEqual(HttpStatusCode.NotFound, stray.StatusCode);
        Assert.AreEqual(HttpStatusCode.OK, redirect.StatusCode);
        Assert.AreEqual("CODE-1", await wait);
    }

    [TestMethod]
    public async Task WaitForCodeAsync_WithWrongState_RejectsTheCode()
    {
        var (listener, uri) = await Listen();
        using var _ = listener;
        using var http = new HttpClient();
        var wait = YouTubeOAuth.WaitForCodeAsync(listener, "STATE-1", TimeSpan.FromSeconds(10));

        var redirect = await http.GetAsync(uri + "?code=CODE-1&state=OTHER");

        Assert.AreEqual(HttpStatusCode.BadRequest, redirect.StatusCode);
        var ex = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => wait);
        StringAssert.Contains(ex.Message, "state did not match");
    }

    [TestMethod]
    public async Task WaitForCodeAsync_WithAccessDenied_ThrowsWithGoogleError()
    {
        var (listener, uri) = await Listen();
        using var _ = listener;
        using var http = new HttpClient();
        var wait = YouTubeOAuth.WaitForCodeAsync(listener, "STATE-1", TimeSpan.FromSeconds(10));

        var redirect = await http.GetAsync(uri + "?error=access_denied&state=STATE-1");

        Assert.AreEqual(HttpStatusCode.OK, redirect.StatusCode);
        var ex = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => wait);
        StringAssert.Contains(ex.Message, "access_denied");
    }

    [TestMethod]
    public async Task WaitForCodeAsync_WithNoRedirect_TimesOut()
    {
        var (listener, _) = await Listen();
        using var __ = listener;

        await Assert.ThrowsExactlyAsync<TimeoutException>(
            () => YouTubeOAuth.WaitForCodeAsync(listener, "STATE-1", TimeSpan.FromMilliseconds(200)));
    }

    [TestMethod]
    public async Task WaitForCodeAsync_WhenCancelled_ThrowsCancellationNotTimeout()
    {
        var (listener, _) = await Listen();
        using var __ = listener;
        using var stop = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => YouTubeOAuth.WaitForCodeAsync(listener, "STATE-1", TimeSpan.FromSeconds(10), stop.Token));
    }
}
#endif
