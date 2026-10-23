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
}
#endif
