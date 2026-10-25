#if WINDOWS10_0_22621_0_OR_GREATER
using System.Net;
using System.Text.Json;
using StoryCADAutomation.YouTube;

namespace StoryCADTests.Automation;

/// <summary>
///     A stand-in for Google's token endpoint and the YouTube Data API (#1626). It answers by
///     address and keeps a copy of each request, read on arrival because the caller disposes it.
/// </summary>
internal sealed class FakeGoogle : HttpMessageHandler
{
    public const string AccessToken = "ACCESS-TOKEN-xyz";
    public const string SessionUrl = "https://www.googleapis.com/upload/youtube/v3/videos?uploadType=resumable&upload_id=SESSION1";
    public const string VideoId = "vid123";

    public sealed record Seen(HttpMethod Method, string Url, Dictionary<string, string> Headers, string? ContentType,
        long? ContentLength, string Body, long BodyBytes);

    public List<Seen> Requests { get; } = new();
    /// <summary>The status of the revoke endpoint's answer.</summary>
    public HttpStatusCode RevokeStatus { get; init; } = HttpStatusCode.OK;

    /// <summary>The revoke endpoint never answers, to test the revoke timeout.</summary>
    public bool StallRevoke { get; init; }
    public string ReturnedPrivacy { get; init; } = "unlisted";

    /// <summary>Paths whose request gets a 403, to test a failed later step.</summary>
    public string? FailPath { get; init; }

    /// <summary>Paths whose request gets a 200 with a body that is not JSON.</summary>
    public string? GarbagePath { get; init; }

    /// <summary>The file PUT gets a 503, to test an upload that stops.</summary>
    public bool FailPut { get; init; }

    /// <summary>After the file PUT, the first access token gets a 401, as when it expires during the upload.</summary>
    public bool ExpireFirstTokenAtPut { get; init; }

    private bool _putSeen;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var bytes = request.Content is null ? Array.Empty<byte>() : await request.Content.ReadAsByteArrayAsync(cancellationToken);
        Requests.Add(new Seen(request.Method, request.RequestUri!.ToString(),
            request.Headers.ToDictionary(h => h.Key, h => string.Join(",", h.Value)),
            request.Content?.Headers.ContentType?.MediaType, request.Content?.Headers.ContentLength,
            // Video bytes are not text; keep only small bodies as text.
            bytes.Length < 100_000 ? System.Text.Encoding.UTF8.GetString(bytes) : "", bytes.Length));

        var url = request.RequestUri!;
        if (FailPath is not null && url.AbsolutePath.Contains(FailPath, StringComparison.Ordinal))
        {
            return Json(HttpStatusCode.Forbidden, """{"error":{"code":403,"message":"The user is not allowed."}}""");
        }

        if (GarbagePath is not null && url.AbsolutePath.Contains(GarbagePath, StringComparison.Ordinal))
        {
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("<html>proxy</html>") };
        }

        if (url.Host == "oauth2.googleapis.com" && url.AbsolutePath == "/revoke")
        {
            if (StallRevoke)
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }

            return RevokeStatus == HttpStatusCode.OK
                ? Json(HttpStatusCode.OK, "{}")
                : Json(RevokeStatus, """{"error":"invalid_token","error_description":"Token expired or revoked"}""");
        }

        if (url.Host == "oauth2.googleapis.com")
        {
            return Json(HttpStatusCode.OK, $$"""{"access_token":"{{AccessToken}}","expires_in":3599,"token_type":"Bearer"}""");
        }

        if (request.Method == HttpMethod.Post && url.AbsolutePath == "/upload/youtube/v3/videos")
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("") };
            response.Headers.Location = new Uri(SessionUrl);
            return response;
        }

        if (_putSeen && ExpireFirstTokenAtPut && request.Headers.Authorization?.Parameter == AccessToken)
        {
            return Json(HttpStatusCode.Unauthorized, """{"error":{"code":401,"message":"Invalid Credentials"}}""");
        }

        if (request.Method == HttpMethod.Put && url.ToString() == SessionUrl)
        {
            _putSeen = true;
            if (FailPut)
            {
                return Json(HttpStatusCode.ServiceUnavailable, """{"error":{"code":503,"message":"Backend Error"}}""");
            }

            return Json(HttpStatusCode.OK, $$$"""{"id":"{{{VideoId}}}","status":{"privacyStatus":"{{{ReturnedPrivacy}}}"}}""");
        }

        if (url.AbsolutePath == "/youtube/v3/channels")
        {
            return Json(HttpStatusCode.OK, """{"items":[{"id":"UC123","snippet":{"title":"StoryCAD"}}]}""");
        }

        return Json(HttpStatusCode.OK, """{"id":"other"}""");
    }

    private static HttpResponseMessage Json(HttpStatusCode status, string body) =>
        new(status) { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json") };
}

/// <summary>The YouTube Data API calls for #1626, against <see cref="FakeGoogle" />.</summary>
[TestClass]
public class YouTubeApiTests
{
    private string _folder = null!;
    private string _video = null!;

    [TestInitialize]
    public void Setup()
    {
        _folder = Path.Combine(Path.GetTempPath(), "StoryCADAutomation", "youtube-api-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_folder);
        _video = Path.Combine(_folder, "intro.mp4");
        File.WriteAllBytes(_video, new byte[12345]);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, true);
        }
    }

    private static YouTubeMeta Meta(bool madeForKids = false, string privacy = "unlisted") => new()
    {
        Title = "Creating an outline", Description = "How to start.", Tags = new[] { "StoryCAD", "outline" },
        Privacy = privacy, MadeForKids = madeForKids,
    };

    [TestMethod]
    public async Task UploadVideoAsync_Always_SendsSessionPostThenFilePut()
    {
        var fake = new FakeGoogle();

        var (id, privacy) = await YouTubeApi.UploadVideoAsync(new HttpClient(fake), "AT", _video, Meta());

        Assert.AreEqual((FakeGoogle.VideoId, "unlisted"), (id, privacy));
        Assert.AreEqual(2, fake.Requests.Count);
        var start = fake.Requests[0];
        Assert.AreEqual(HttpMethod.Post, start.Method);
        Assert.AreEqual("https://www.googleapis.com/upload/youtube/v3/videos?uploadType=resumable&part=snippet,status", start.Url);
        Assert.AreEqual("Bearer AT", start.Headers["Authorization"]);
        Assert.AreEqual("video/mp4", start.Headers["X-Upload-Content-Type"]);
        Assert.AreEqual("12345", start.Headers["X-Upload-Content-Length"]);
        Assert.AreEqual("application/json", start.ContentType);

        var put = fake.Requests[1];
        Assert.AreEqual(HttpMethod.Put, put.Method);
        Assert.AreEqual(FakeGoogle.SessionUrl, put.Url);
        Assert.AreEqual("video/mp4", put.ContentType);
        Assert.AreEqual(12345L, put.ContentLength);
        Assert.AreEqual(12345L, put.BodyBytes);
        Assert.AreEqual("Bearer AT", put.Headers["Authorization"]);
    }

    [TestMethod]
    public async Task UploadVideoAsync_WhenPutFails_SaysReRunStartsOver()
    {
        var fake = new FakeGoogle { FailPut = true };

        var ex = await Assert.ThrowsExactlyAsync<HttpRequestException>(
            () => YouTubeApi.UploadVideoAsync(new HttpClient(fake), "AT", _video, Meta()));

        StringAssert.Contains(ex.Message, "Backend Error");
        StringAssert.Contains(ex.Message, "a re-run starts the upload over");
    }

    [TestMethod]
    public async Task UploadVideoAsync_Always_SendsSnippetAndStatusFields()
    {
        var fake = new FakeGoogle();

        await YouTubeApi.UploadVideoAsync(new HttpClient(fake), "AT", _video, Meta(madeForKids: false, privacy: "private"));

        using var body = JsonDocument.Parse(fake.Requests[0].Body);
        var snippet = body.RootElement.GetProperty("snippet");
        var status = body.RootElement.GetProperty("status");
        Assert.AreEqual("Creating an outline", snippet.GetProperty("title").GetString());
        Assert.AreEqual("How to start.", snippet.GetProperty("description").GetString());
        Assert.AreEqual("28", snippet.GetProperty("categoryId").GetString());
        CollectionAssert.AreEqual(new[] { "StoryCAD", "outline" },
            snippet.GetProperty("tags").EnumerateArray().Select(t => t.GetString()).ToArray());
        Assert.AreEqual("private", status.GetProperty("privacyStatus").GetString());
        Assert.AreEqual(JsonValueKind.False, status.GetProperty("selfDeclaredMadeForKids").ValueKind);
    }

    [TestMethod]
    public async Task UploadVideoAsync_WithMadeForKids_SendsSelfDeclaredTrue()
    {
        var fake = new FakeGoogle();

        await YouTubeApi.UploadVideoAsync(new HttpClient(fake), "AT", _video, Meta(madeForKids: true));

        using var body = JsonDocument.Parse(fake.Requests[0].Body);
        Assert.IsTrue(body.RootElement.GetProperty("status").GetProperty("selfDeclaredMadeForKids").GetBoolean());
    }

    [TestMethod]
    public async Task UploadVideoAsync_WhenSessionPostFails_ThrowsWithoutPut()
    {
        var fake = new FakeGoogle { FailPath = "/upload/youtube/v3/videos" };

        var ex = await Assert.ThrowsExactlyAsync<HttpRequestException>(
            () => YouTubeApi.UploadVideoAsync(new HttpClient(fake), "AT", _video, Meta()));

        StringAssert.Contains(ex.Message, "The user is not allowed.");
        Assert.AreEqual(1, fake.Requests.Count);
    }

    [TestMethod]
    public async Task ChannelAsync_WithChannel_ReturnsTitleAndId()
    {
        var fake = new FakeGoogle();

        var channel = await YouTubeApi.ChannelAsync(new HttpClient(fake), "AT");

        Assert.AreEqual(("StoryCAD", "UC123"), channel);
        Assert.AreEqual("https://www.googleapis.com/youtube/v3/channels?part=snippet&mine=true", fake.Requests.Single().Url);
    }

    [TestMethod]
    public async Task AddToPlaylistAsync_Always_PostsVideoResource()
    {
        var fake = new FakeGoogle();

        await YouTubeApi.AddToPlaylistAsync(new HttpClient(fake), "AT", "PL1", "vid1");

        var request = fake.Requests.Single();
        Assert.AreEqual("https://www.googleapis.com/youtube/v3/playlistItems?part=snippet", request.Url);
        using var body = JsonDocument.Parse(request.Body);
        var snippet = body.RootElement.GetProperty("snippet");
        Assert.AreEqual("PL1", snippet.GetProperty("playlistId").GetString());
        Assert.AreEqual("youtube#video", snippet.GetProperty("resourceId").GetProperty("kind").GetString());
        Assert.AreEqual("vid1", snippet.GetProperty("resourceId").GetProperty("videoId").GetString());
    }

    [TestMethod]
    public async Task InsertCaptionsAsync_Always_SendsMultipartWithEnglishTrack()
    {
        var captions = Path.Combine(_folder, "intro.srt");
        File.WriteAllText(captions, "1\n00:00:00,000 --> 00:00:01,000\nHello\n");
        var fake = new FakeGoogle();

        await YouTubeApi.InsertCaptionsAsync(new HttpClient(fake), "AT", "vid1", captions);

        var request = fake.Requests.Single();
        Assert.AreEqual("https://www.googleapis.com/upload/youtube/v3/captions?uploadType=multipart&part=snippet", request.Url);
        Assert.AreEqual("multipart/related", request.ContentType);
        StringAssert.Contains(request.Body, "\"language\":\"en\"");
        StringAssert.Contains(request.Body, "\"name\":\"\"");
        StringAssert.Contains(request.Body, "\"videoId\":\"vid1\"");
        StringAssert.Contains(request.Body, "Hello");
    }

    [TestMethod]
    public async Task SetThumbnailAsync_WithPng_PostsImageForVideo()
    {
        var thumbnail = Path.Combine(_folder, "thumb.png");
        File.WriteAllBytes(thumbnail, new byte[64]);
        var fake = new FakeGoogle();

        await YouTubeApi.SetThumbnailAsync(new HttpClient(fake), "AT", "vid1", thumbnail);

        var request = fake.Requests.Single();
        Assert.AreEqual("https://www.googleapis.com/upload/youtube/v3/thumbnails/set?videoId=vid1", request.Url);
        Assert.AreEqual("image/png", request.ContentType);
        Assert.AreEqual(64L, request.BodyBytes);
    }

    [TestMethod]
    public void ErrorDetail_WithTokenEndpointError_ReturnsErrorAndDescription()
    {
        Assert.AreEqual("invalid_grant: Bad Request",
            YouTubeApi.ErrorDetail("""{"error":"invalid_grant","error_description":"Bad Request"}"""));
    }
}
#endif
