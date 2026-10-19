#if WINDOWS10_0_22621_0_OR_GREATER
using StoryCADAutomation.YouTube;

namespace StoryCADTests.Automation;

/// <summary>
///     The youtube command (#1626): what it prints, and that no client secret, access token or
///     refresh token is among it. Google is replaced by <see cref="FakeGoogle" />.
/// </summary>
[TestClass]
public class YouTubeCommandTests
{
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
        foreach (var secret in new[]
                 {
                     YouTubeOAuthTests.Client.ClientSecret, FakeGoogle.AccessToken, FakeGoogle.RefreshToken, "STORED-REFRESH",
                 })
        {
            Assert.IsFalse(printed.Contains(secret, StringComparison.Ordinal), $"'{secret}' was printed:\n{printed}");
        }
    }

    private YouTubeMeta Meta(string? playlistId = null, string? thumbnail = null) => new()
    {
        Title = "Creating an outline", PlaylistId = playlistId, ThumbnailPath = thumbnail,
    };

    [TestMethod]
    public async Task FinishAuthAsync_Always_SavesTokenAndPrintsChannelOnly()
    {
        var fake = new FakeGoogle();

        var exit = await YouTubeCommand.FinishAuthAsync(YouTubeOAuthTests.Client, "CODE", "VERIFIER",
            "http://127.0.0.1:50000/", _folder, new HttpClient(fake), _out.Add);

        Assert.AreEqual(0, exit);
        Assert.AreEqual(FakeGoogle.RefreshToken, YouTubeOAuth.LoadRefreshToken(Path.Combine(_folder, "token.json")));
        Assert.IsTrue(_out.Any(line => line.Contains("\"StoryCAD\" (UC123)")), string.Join("\n", _out));
        AssertNoSecretPrinted();
    }

    [TestMethod]
    public async Task FinishAuthAsync_WhenChannelLookupFails_WarnsAndSucceeds()
    {
        var fake = new FakeGoogle { FailPath = "/channels" };

        var exit = await YouTubeCommand.FinishAuthAsync(YouTubeOAuthTests.Client, "CODE", "VERIFIER",
            "http://127.0.0.1:50000/", _folder, new HttpClient(fake), _out.Add);

        Assert.AreEqual(0, exit);
        Assert.IsTrue(File.Exists(Path.Combine(_folder, "token.json")));
        StringAssert.Contains(_out[^1], "warning: the token is saved, but the channel lookup failed");
        AssertNoSecretPrinted();
    }

    [TestMethod]
    public async Task UploadAsync_WhenTokenExpiresDuringUpload_RefreshesBeforePlaylist()
    {
        var fake = new FakeGoogle { ExpireFirstTokenAtPut = true };

        var exit = await YouTubeCommand.UploadAsync(_video, Meta(playlistId: "PL1"), YouTubeOAuthTests.Client,
            "STORED-REFRESH", new HttpClient(fake), _out.Add, _err.Add);

        Assert.AreEqual(0, exit, string.Join(" | ", _err));
        CollectionAssert.AreEqual(
            new[] { "/token", "/upload/youtube/v3/videos", "/upload/youtube/v3/videos", "/token", "/youtube/v3/playlistItems" },
            fake.Requests.Select(r => new Uri(r.Url).AbsolutePath).ToArray());
        Assert.AreEqual($"Bearer {FakeGoogle.AccessToken}-2", fake.Requests[^1].Headers["Authorization"]);
        AssertNoSecretPrinted();
    }

    [TestMethod]
    public async Task UploadAsync_WhenStepGetsNonJson_ReportsStepAndPrintsLinkFirst()
    {
        var fake = new FakeGoogle { GarbagePath = "/playlistItems" };

        var exit = await YouTubeCommand.UploadAsync(_video, Meta(playlistId: "PL1"), YouTubeOAuthTests.Client,
            "STORED-REFRESH", new HttpClient(fake), _out.Add, _err.Add);

        Assert.AreEqual(3, exit);
        StringAssert.Contains(_err.Single(), "playlist failed");
        CollectionAssert.Contains(_out, "youtube: https://youtu.be/vid123");
    }

    [TestMethod]
    public async Task UploadAsync_Always_RefreshesUploadsAndPrintsLink()
    {
        var fake = new FakeGoogle();

        var exit = await YouTubeCommand.UploadAsync(_video, Meta(), YouTubeOAuthTests.Client, "STORED-REFRESH",
            new HttpClient(fake), _out.Add, _err.Add);

        Assert.AreEqual(0, exit);
        CollectionAssert.AreEqual(new[] { "/token", "/upload/youtube/v3/videos", "/upload/youtube/v3/videos" },
            fake.Requests.Select(r => new Uri(r.Url).AbsolutePath).ToArray());
        Assert.AreEqual($"Bearer {FakeGoogle.AccessToken}", fake.Requests[1].Headers["Authorization"]);
        Assert.AreEqual("youtube: https://youtu.be/vid123", _out[^1]);
        Assert.AreEqual(0, _err.Count, string.Join("\n", _err));
        AssertNoSecretPrinted();
    }

    [TestMethod]
    public async Task UploadAsync_WhenYouTubeLocksPrivate_WarnsAndSucceeds()
    {
        var fake = new FakeGoogle { ReturnedPrivacy = "private" };

        var exit = await YouTubeCommand.UploadAsync(_video, Meta(), YouTubeOAuthTests.Client, "STORED-REFRESH",
            new HttpClient(fake), _out.Add, _err.Add);

        Assert.AreEqual(0, exit);
        StringAssert.Contains(_err.Single(), "private, not unlisted");
    }

    [TestMethod]
    public async Task UploadAsync_WithPlaylist_AddsVideoAfterUpload()
    {
        var fake = new FakeGoogle();

        await YouTubeCommand.UploadAsync(_video, Meta(playlistId: "PL1"), YouTubeOAuthTests.Client, "STORED-REFRESH",
            new HttpClient(fake), _out.Add, _err.Add);

        Assert.AreEqual("/youtube/v3/playlistItems", new Uri(fake.Requests[^1].Url).AbsolutePath);
    }

    [TestMethod]
    public async Task UploadAsync_WhenThumbnailFails_ReturnsEnvironmentFailureAndStillPrintsId()
    {
        var thumbnail = Path.Combine(_folder, "thumb.jpg");
        File.WriteAllBytes(thumbnail, new byte[10]);
        var fake = new FakeGoogle { FailPath = "/thumbnails/set" };

        var exit = await YouTubeCommand.UploadAsync(_video, Meta(thumbnail: thumbnail), YouTubeOAuthTests.Client,
            "STORED-REFRESH", new HttpClient(fake), _out.Add, _err.Add);

        Assert.AreEqual(3, exit);
        StringAssert.Contains(_err.Single(), "thumbnail failed");
        Assert.AreEqual("youtube: https://youtu.be/vid123", _out[^1]);
        AssertNoSecretPrinted();
    }

    [TestMethod]
    public async Task UploadAsync_WhenRefreshRevoked_ThrowsWithoutSecret()
    {
        var fake = new FakeGoogle { TokenStatus = System.Net.HttpStatusCode.BadRequest };

        var ex = await Assert.ThrowsExactlyAsync<HttpRequestException>(() => YouTubeCommand.UploadAsync(_video, Meta(),
            YouTubeOAuthTests.Client, "STORED-REFRESH", new HttpClient(fake), _out.Add, _err.Add));

        _err.Add(ex.Message);
        Assert.AreEqual(1, fake.Requests.Count);
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
            // No client file or token exists in the test, so reaching the network step would print other errors.
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
}
#endif
