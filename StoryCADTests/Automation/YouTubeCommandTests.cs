#if WINDOWS10_0_22621_0_OR_GREATER
using System.Net;
using StoryCADAutomation.YouTube;

namespace StoryCADTests.Automation;

/// <summary>
///     The youtube command (#1626): what it prints, that no client secret or access token is
///     among it, and that the sign-in is revoked when the run ends. Google is replaced by
///     <see cref="FakeGoogle" />.
/// </summary>
[TestClass]
public class YouTubeCommandTests
{
    private const string Revoke = "/revoke";

    private string _folder = null!;
    private string _video = null!;
    private readonly List<string> _out = new();
    private readonly List<string> _err = new();

    [TestInitialize]
    public void Setup()
    {
        _folder = Path.Combine(Path.GetTempPath(), "StoryCADAutomation", "youtube-cmd-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_folder);
        _video = Path.Combine(_folder, "intro.mp4");
        File.WriteAllBytes(_video, new byte[1000]);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, true);
        }
    }

    private void AssertNoSecretPrinted()
    {
        var printed = string.Join("\n", _out.Concat(_err));
        foreach (var secret in new[] { YouTubeOAuthTests.Client.ClientSecret, FakeGoogle.AccessToken })
        {
            Assert.IsFalse(printed.Contains(secret, StringComparison.Ordinal), $"'{secret}' was printed:\n{printed}");
        }
    }

    private static string[] Paths(FakeGoogle fake) => fake.Requests.Select(r => new Uri(r.Url).AbsolutePath).ToArray();

    private YouTubeMeta Meta(string? playlistId = null, string? thumbnail = null) => new()
    {
        Title = "Creating an outline", PlaylistId = playlistId, ThumbnailPath = thumbnail,
    };

    private Task<int> Upload(FakeGoogle fake, YouTubeMeta meta) =>
        YouTubeCommand.UploadAsync(_video, meta, FakeGoogle.AccessToken, new HttpClient(fake), _out.Add, _err.Add);

    [TestMethod]
    public async Task UploadAsync_Always_PrintsChannelUploadsAndRevokes()
    {
        var fake = new FakeGoogle();

        var exit = await Upload(fake, Meta());

        Assert.AreEqual(0, exit, string.Join(" | ", _err));
        CollectionAssert.AreEqual(
            new[] { "/youtube/v3/channels", "/upload/youtube/v3/videos", "/upload/youtube/v3/videos", Revoke },
            Paths(fake));
        Assert.AreEqual($"Bearer {FakeGoogle.AccessToken}", fake.Requests[1].Headers["Authorization"]);
        StringAssert.Contains(fake.Requests[^1].Body, "token=" + FakeGoogle.AccessToken);
        CollectionAssert.Contains(_out, "youtube: signed in to channel \"StoryCAD\" (UC123)");
        CollectionAssert.Contains(_out, "youtube: https://youtu.be/vid123");
        Assert.AreEqual("youtube: sign-in revoked", _out[^1]);
        Assert.AreEqual(0, _err.Count, string.Join("\n", _err));
        AssertNoSecretPrinted();
    }

    [TestMethod]
    public async Task UploadAsync_WhenUploadFails_StillRevokesAndRethrows()
    {
        var fake = new FakeGoogle { FailPut = true };

        await Assert.ThrowsExactlyAsync<HttpRequestException>(() => Upload(fake, Meta()));

        Assert.AreEqual(Revoke, Paths(fake)[^1]);
        Assert.AreEqual("youtube: sign-in revoked", _out[^1]);
        AssertNoSecretPrinted();
    }

    [TestMethod]
    public async Task UploadAsync_WhenRevokeFails_WarnsAndKeepsTheUploadResult()
    {
        var fake = new FakeGoogle { RevokeStatus = HttpStatusCode.BadRequest };

        var exit = await Upload(fake, Meta());

        Assert.AreEqual(0, exit);
        StringAssert.Contains(_err.Single(), "warning: the revoke failed");
        CollectionAssert.Contains(_out, "youtube: https://youtu.be/vid123");
        AssertNoSecretPrinted();
    }

    [TestMethod]
    public async Task UploadAsync_WhenUploadAndRevokeFail_ThrowsTheUploadFailure()
    {
        var fake = new FakeGoogle { FailPut = true, RevokeStatus = HttpStatusCode.BadRequest };

        var ex = await Assert.ThrowsExactlyAsync<HttpRequestException>(() => Upload(fake, Meta()));

        StringAssert.Contains(ex.Message, "Backend Error");
        StringAssert.Contains(_err.Single(), "warning: the revoke failed");
        AssertNoSecretPrinted();
    }

    [TestMethod]
    public async Task UploadAsync_WhenCancelled_StillRevokesAndThrowsCancellation()
    {
        // Ctrl+C cancels the run's token; the revoke has its own.
        var fake = new FakeGoogle();
        using var stop = new CancellationTokenSource();
        stop.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => YouTubeCommand.UploadAsync(_video, Meta(playlistId: "PL1"),
            FakeGoogle.AccessToken, new HttpClient(fake), _out.Add, _err.Add, stop.Token));

        // Nothing is uploaded, and the revoke comes last.
        CollectionAssert.DoesNotContain(Paths(fake), "/upload/youtube/v3/videos");
        Assert.AreEqual(Revoke, Paths(fake)[^1]);
        Assert.AreEqual("youtube: sign-in revoked", _out[^1]);
        AssertNoSecretPrinted();
    }

    [TestMethod]
    public async Task UploadAsync_WhenRevokeStalls_GivesUpAfterTheRevokeTimeout()
    {
        var fake = new FakeGoogle { StallRevoke = true };
        var clock = System.Diagnostics.Stopwatch.StartNew();

        var exit = await YouTubeCommand.UploadAsync(_video, Meta(), FakeGoogle.AccessToken, new HttpClient(fake),
            _out.Add, _err.Add, revokeTimeout: TimeSpan.FromMilliseconds(200));

        Assert.AreEqual(0, exit);
        Assert.IsTrue(clock.Elapsed < TimeSpan.FromSeconds(10), clock.Elapsed.ToString());
        StringAssert.Contains(_err.Single(), "the revoke failed, so the sign-in stays valid for up to an hour: no answer in 0.2 s");
        AssertNoSecretPrinted();
    }

    [TestMethod]
    public void RevokeTimeout_Is15Seconds()
    {
        Assert.AreEqual(TimeSpan.FromSeconds(15), YouTubeCommand.RevokeTimeout);
    }

    [TestMethod]
    public async Task UploadAsync_WhenTokenExpiresDuringUpload_ReportsTheStepAndRevokes()
    {
        // No refresh token exists, so a step after an upload that outlasts the token fails.
        var fake = new FakeGoogle { ExpireFirstTokenAtPut = true };

        var exit = await Upload(fake, Meta(playlistId: "PL1"));

        Assert.AreEqual(3, exit);
        StringAssert.Contains(_err.Single(), "playlist failed");
        Assert.AreEqual(Revoke, Paths(fake)[^1]);
        AssertNoSecretPrinted();
    }

    [TestMethod]
    public async Task UploadAsync_WhenChannelLookupFails_WarnsAndUploads()
    {
        var fake = new FakeGoogle { FailPath = "/channels" };

        var exit = await Upload(fake, Meta());

        Assert.AreEqual(0, exit);
        StringAssert.Contains(_err.Single(), "warning: the channel lookup failed");
        CollectionAssert.Contains(_out, "youtube: https://youtu.be/vid123");
    }

    [TestMethod]
    public async Task UploadAsync_WhenStepGetsNonJson_ReportsStepAndPrintsLinkFirst()
    {
        var fake = new FakeGoogle { GarbagePath = "/playlistItems" };

        var exit = await Upload(fake, Meta(playlistId: "PL1"));

        Assert.AreEqual(3, exit);
        StringAssert.Contains(_err.Single(), "playlist failed");
        CollectionAssert.Contains(_out, "youtube: https://youtu.be/vid123");
    }

    [TestMethod]
    public async Task UploadAsync_WhenYouTubeLocksPrivate_WarnsAndSucceeds()
    {
        var fake = new FakeGoogle { ReturnedPrivacy = "private" };

        var exit = await Upload(fake, Meta());

        Assert.AreEqual(0, exit);
        StringAssert.Contains(_err.Single(), "private, not unlisted");
    }

    [TestMethod]
    public async Task UploadAsync_WithPlaylist_AddsVideoAfterUploadThenRevokes()
    {
        var fake = new FakeGoogle();

        await Upload(fake, Meta(playlistId: "PL1"));

        CollectionAssert.AreEqual(new[] { "/youtube/v3/playlistItems", Revoke }, Paths(fake)[^2..]);
    }

    [TestMethod]
    public async Task UploadAsync_WhenThumbnailFails_ReturnsEnvironmentFailureAndStillPrintsId()
    {
        var thumbnail = Path.Combine(_folder, "thumb.jpg");
        File.WriteAllBytes(thumbnail, new byte[10]);
        var fake = new FakeGoogle { FailPath = "/thumbnails/set" };

        var exit = await Upload(fake, Meta(thumbnail: thumbnail));

        Assert.AreEqual(3, exit);
        StringAssert.Contains(_err.Single(), "thumbnail failed");
        CollectionAssert.Contains(_out, "youtube: https://youtu.be/vid123");
        AssertNoSecretPrinted();
    }

    [TestMethod]
    public void Upload_WithMissingVideoAndBadMeta_ReturnsUsageErrorBeforeNetwork()
    {
        var meta = Path.Combine(_folder, "meta.json");
        File.WriteAllText(meta, """{ "privacy": "friends" }""");
        var error = new StringWriter();
        var original = Console.Error;
        Console.SetError(error);
        try
        {
            // No client file exists in the test, so reaching the network step would print other errors.
            var exit = YouTubeCommand.Upload(Path.Combine(_folder, "none.mp4"), meta);

            Assert.AreEqual(64, exit);
        }
        finally
        {
            Console.SetError(original);
        }

        var lines = error.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.AreEqual(3, lines.Length, error.ToString());
        StringAssert.Contains(lines[0], "video not found");
    }

    [TestMethod]
    public async Task UpdateAsync_Always_PutsSnippetOnlyAndRevokes()
    {
        var fake = new FakeGoogle();
        var meta = new YouTubeMeta
        {
            Title = "StoryCAD: free story outlining software", Description = "Outline a novel.",
            Tags = new[] { "StoryCAD", "story outlining software" }, PlaylistId = "PL1", Privacy = "public",
        };

        var exit = await YouTubeCommand.UpdateAsync("oBOXeBVRNRE", meta, FakeGoogle.AccessToken, new HttpClient(fake),
            _out.Add, _err.Add);

        Assert.AreEqual(0, exit, string.Join(" | ", _err));
        CollectionAssert.AreEqual(new[] { "/youtube/v3/channels", "/youtube/v3/videos", Revoke }, Paths(fake));
        var put = fake.Requests[1];
        Assert.AreEqual(HttpMethod.Put, put.Method);
        Assert.AreEqual("https://www.googleapis.com/youtube/v3/videos?part=snippet", put.Url);
        using var body = System.Text.Json.JsonDocument.Parse(put.Body);
        var root = body.RootElement;
        Assert.AreEqual("oBOXeBVRNRE", root.GetProperty("id").GetString());
        var snippet = root.GetProperty("snippet");
        Assert.AreEqual("StoryCAD: free story outlining software", snippet.GetProperty("title").GetString());
        Assert.AreEqual("Outline a novel.", snippet.GetProperty("description").GetString());
        Assert.AreEqual("story outlining software", snippet.GetProperty("tags")[1].GetString());
        Assert.AreEqual(YouTubeMeta.DefaultCategoryId, snippet.GetProperty("categoryId").GetString());
        // Privacy and playlist stay as they are on YouTube: no status part, no playlist call.
        Assert.IsFalse(root.TryGetProperty("status", out _));
        CollectionAssert.Contains(_out, "youtube: updated title, description, tags and category of https://youtu.be/oBOXeBVRNRE");
        Assert.AreEqual("youtube: sign-in revoked", _out[^1]);
        AssertNoSecretPrinted();
    }

    [TestMethod]
    public async Task UpdateAsync_WhenUpdateFails_StillRevokesAndRethrows()
    {
        var fake = new FakeGoogle { FailPath = "/youtube/v3/videos" };

        await Assert.ThrowsExactlyAsync<HttpRequestException>(() => YouTubeCommand.UpdateAsync("oBOXeBVRNRE", Meta(),
            FakeGoogle.AccessToken, new HttpClient(fake), _out.Add, _err.Add));

        Assert.AreEqual(Revoke, Paths(fake)[^1]);
        Assert.AreEqual("youtube: sign-in revoked", _out[^1]);
        AssertNoSecretPrinted();
    }

    [TestMethod]
    [DataRow("oBOXeBVRNRE", true)]
    [DataRow("-2j-Bgflv7A", true)]
    [DataRow("https://youtu.be/oBOXeBVRNRE", false)]
    [DataRow("oBOXeBVRNR", false)]
    [DataRow("oBOXeBVRNR!", false)]
    public void IsVideoId_ForValue_MatchesYouTubeIdShape(string value, bool expected)
    {
        Assert.AreEqual(expected, YouTubeCommand.IsVideoId(value));
    }

    [TestMethod]
    public void Update_WithLinkInsteadOfIdAndBadMeta_ReturnsUsageErrorBeforeNetwork()
    {
        var meta = Path.Combine(_folder, "meta.json");
        File.WriteAllText(meta, """{ "privacy": "friends" }""");
        var error = new StringWriter();
        var original = Console.Error;
        Console.SetError(error);
        try
        {
            // No client file exists in the test, so reaching the network step would print other errors.
            var exit = YouTubeCommand.Update("https://youtu.be/oBOXeBVRNRE", meta);

            Assert.AreEqual(64, exit);
        }
        finally
        {
            Console.SetError(original);
        }

        var lines = error.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        StringAssert.Contains(lines[0], "is not a video id");
        Assert.IsTrue(lines.Length > 1, error.ToString());
    }
}
#endif
