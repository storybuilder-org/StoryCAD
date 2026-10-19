#if WINDOWS10_0_22621_0_OR_GREATER
using StoryCADAutomation.YouTube;

namespace StoryCADTests.Automation;

/// <summary>The meta.json checks for `youtube upload` (#1626). No request leaves the machine.</summary>
[TestClass]
public class YouTubeMetaTests
{
    private string _folder = null!;

    [TestInitialize]
    public void Setup()
    {
        _folder = Path.Combine(Path.GetTempPath(), "StoryCADAutomation", "youtube-meta-" + Guid.NewGuid().ToString("N"));
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

    private YouTubeMeta? Load(string json, out IReadOnlyList<string> errors)
    {
        var path = Path.Combine(_folder, "meta.json");
        File.WriteAllText(path, json);
        return YouTubeMeta.Load(path, out errors);
    }

    private void AssertOneError(string json, string expectedPart)
    {
        var meta = Load(json, out var errors);

        Assert.IsNull(meta);
        Assert.AreEqual(1, errors.Count, string.Join(" | ", errors));
        StringAssert.Contains(errors[0], expectedPart);
    }

    [TestMethod]
    public void Load_WithTitleOnly_AppliesDefaults()
    {
        var meta = Load("""{ "title": "Creating an outline" }""", out var errors);

        Assert.AreEqual(0, errors.Count, string.Join(" | ", errors));
        Assert.AreEqual("Creating an outline", meta!.Title);
        Assert.AreEqual("", meta.Description);
        Assert.AreEqual(0, meta.Tags.Count);
        Assert.AreEqual("28", meta.CategoryId);
        Assert.AreEqual("unlisted", meta.Privacy);
        Assert.IsFalse(meta.MadeForKids);
        Assert.IsNull(meta.PlaylistId);
        Assert.IsNull(meta.CaptionsPath);
        Assert.IsNull(meta.ThumbnailPath);
    }

    [TestMethod]
    public void Load_WithEveryField_ReadsEachValue()
    {
        File.WriteAllText(Path.Combine(_folder, "intro.srt"), "1\n00:00:00,000 --> 00:00:01,000\nHi\n");
        File.WriteAllBytes(Path.Combine(_folder, "thumb.png"), new byte[10]);

        var meta = Load("""
            { "title": "T", "description": "D", "tags": ["a", "b"], "categoryId": "27", "privacy": "public",
              "madeForKids": true, "playlistId": "PL1", "captions": "intro.srt", "thumbnail": "thumb.png" }
            """, out var errors);

        Assert.AreEqual(0, errors.Count, string.Join(" | ", errors));
        CollectionAssert.AreEqual(new[] { "a", "b" }, meta!.Tags.ToArray());
        Assert.AreEqual("27", meta.CategoryId);
        Assert.AreEqual("public", meta.Privacy);
        Assert.IsTrue(meta.MadeForKids);
        Assert.AreEqual("PL1", meta.PlaylistId);
    }

    [TestMethod]
    public void Load_WithMissingFile_ReportsNotFound()
    {
        var meta = YouTubeMeta.Load(Path.Combine(_folder, "none.json"), out var errors);

        Assert.IsNull(meta);
        StringAssert.Contains(errors.Single(), "not found");
    }

    [TestMethod]
    public void Load_WithInvalidJson_ReportsJsonError()
    {
        AssertOneError("{ \"title\": ", "not valid JSON");
    }

    [TestMethod]
    public void Load_WithoutTitle_ReportsTitleRequired()
    {
        AssertOneError("""{ "description": "D" }""", "title is required");
    }

    [TestMethod]
    public void Load_WithBlankTitle_ReportsTitleRequired()
    {
        AssertOneError("""{ "title": "   " }""", "title is required");
    }

    [TestMethod]
    public void Load_WithTitleOf100Characters_Accepts()
    {
        var meta = Load($$"""{ "title": "{{new string('x', 100)}}" }""", out var errors);

        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(100, meta!.Title.Length);
    }

    [TestMethod]
    public void Load_WithTitleOf101Characters_ReportsLimit()
    {
        AssertOneError($$"""{ "title": "{{new string('x', 101)}}" }""", "limit is 100");
    }

    [TestMethod]
    public void Load_WithAngleBracketInTitle_ReportsBracket()
    {
        AssertOneError("""{ "title": "a <b> c" }""", "'<' or '>'");
    }

    [TestMethod]
    public void Load_WithDescriptionOver5000Bytes_ReportsLimit()
    {
        // 2,501 two-byte characters: 2,501 characters but 5,002 bytes.
        AssertOneError($$"""{ "title": "T", "description": "{{new string('é', 2501)}}" }""", "limit is 5000");
    }

    [TestMethod]
    public void Load_WithDescriptionOf5000Bytes_Accepts()
    {
        Assert.IsNotNull(Load($$"""{ "title": "T", "description": "{{new string('é', 2500)}}" }""", out _));
    }

    [TestMethod]
    public void Load_WithAngleBracketInDescription_ReportsBracket()
    {
        AssertOneError("""{ "title": "T", "description": "Use <Ctrl> S." }""", "description must not contain '<' or '>'");
    }

    [TestMethod]
    public void Load_WithTagsOf500CountedCharacters_Accepts()
    {
        // 5 x 98 characters + "abcde" (5) + 5 commas = 500.
        var tags = Enumerable.Repeat(new string('x', 98), 5).Append("abcde").ToList();

        Assert.AreEqual(500, YouTubeMeta.TagsLength(tags));
        Assert.IsNotNull(Load(TagsJson(tags), out var errors), string.Join(" | ", errors));
    }

    [TestMethod]
    public void Load_WithTagsOver500CountedCharacters_ReportsLimit()
    {
        // As the 500 case, but "ab de" has a space, so its quotes add 2: 502.
        var tags = Enumerable.Repeat(new string('x', 98), 5).Append("ab de").ToList();

        Assert.AreEqual(502, YouTubeMeta.TagsLength(tags));
        AssertOneError(TagsJson(tags), "the limit is 500");
    }

    [TestMethod]
    public void TagsLength_WithSpaceInTag_CountsQuotesAndCommas()
    {
        Assert.AreEqual(3 + 1 + 5 + 2, YouTubeMeta.TagsLength(new[] { "abc", "ab cd" }));
    }

    private static string TagsJson(IEnumerable<string> tags) =>
        $$"""{ "title": "T", "tags": {{System.Text.Json.JsonSerializer.Serialize(tags)}} }""";

    [TestMethod]
    public void Load_WithTagsNotArray_ReportsTags()
    {
        AssertOneError("""{ "title": "T", "tags": "a,b" }""", "tags must be an array");
    }

    [TestMethod]
    public void Load_WithEmptyTag_ReportsTags()
    {
        AssertOneError("""{ "title": "T", "tags": ["a", ""] }""", "non-empty strings");
    }

    [TestMethod]
    public void Load_WithNonNumericCategory_ReportsCategory()
    {
        AssertOneError("""{ "title": "T", "categoryId": "Science" }""", "categoryId");
    }

    [TestMethod]
    public void Load_WithNumberCategory_ReportsMustBeString()
    {
        AssertOneError("""{ "title": "T", "categoryId": 28 }""", "categoryId must be a string");
    }

    [TestMethod]
    public void Load_WithUnknownPrivacy_ReportsAllowedValues()
    {
        AssertOneError("""{ "title": "T", "privacy": "friends" }""", "private, unlisted, public");
    }

    [TestMethod]
    public void Load_WithMadeForKidsString_ReportsBoolean()
    {
        AssertOneError("""{ "title": "T", "madeForKids": "no" }""", "madeForKids must be true or false");
    }

    [TestMethod]
    public void Load_WithEmptyPlaylistId_ReportsEmpty()
    {
        AssertOneError("""{ "title": "T", "playlistId": "" }""", "playlistId must not be empty");
    }

    [TestMethod]
    public void Load_WithMissingCaptionsFile_ReportsNotFound()
    {
        AssertOneError("""{ "title": "T", "captions": "none.srt" }""", "captions file not found");
    }

    [TestMethod]
    public void Load_WithCaptionsNotSrtOrVtt_ReportsType()
    {
        File.WriteAllText(Path.Combine(_folder, "intro.txt"), "x");

        AssertOneError("""{ "title": "T", "captions": "intro.txt" }""", ".srt or .vtt");
    }

    [TestMethod]
    public void Load_WithThumbnailNotImage_ReportsType()
    {
        File.WriteAllText(Path.Combine(_folder, "thumb.gif"), "x");

        AssertOneError("""{ "title": "T", "thumbnail": "thumb.gif" }""", ".jpg, .jpeg or .png");
    }

    [TestMethod]
    public void Load_WithThumbnailOver2MB_ReportsSize()
    {
        File.WriteAllBytes(Path.Combine(_folder, "thumb.jpg"), new byte[2 * 1024 * 1024 + 1]);

        AssertOneError("""{ "title": "T", "thumbnail": "thumb.jpg" }""", "larger than 2 MB");
    }

    [TestMethod]
    public void Load_WithMissingThumbnail_ReportsNotFound()
    {
        AssertOneError("""{ "title": "T", "thumbnail": "thumb.png" }""", "thumbnail file not found");
    }

    [TestMethod]
    public void Load_WithUnknownField_ReportsField()
    {
        AssertOneError("""{ "title": "T", "privacyStatus": "public" }""", "unknown field 'privacyStatus'");
    }

    [TestMethod]
    public void Load_WithSeveralErrors_ReportsAllAtOnce()
    {
        var meta = Load("""{ "privacy": "friends", "madeForKids": 1, "captions": "none.srt" }""", out var errors);

        Assert.IsNull(meta);
        Assert.AreEqual(4, errors.Count, string.Join(" | ", errors));
    }

    [TestMethod]
    public void Load_WithRelativePaths_ResolvesAgainstMetaFolder()
    {
        var sub = Path.Combine(_folder, "assets");
        Directory.CreateDirectory(sub);
        File.WriteAllText(Path.Combine(sub, "intro.vtt"), "WEBVTT\n");
        File.WriteAllBytes(Path.Combine(sub, "thumb.jpg"), new byte[10]);

        // The current folder is elsewhere, so a path resolved against it would not exist.
        var meta = Load("""{ "title": "T", "captions": "assets/intro.vtt", "thumbnail": "assets\\thumb.jpg" }""",
            out var errors);

        Assert.AreEqual(0, errors.Count, string.Join(" | ", errors));
        Assert.AreEqual(Path.Combine(sub, "intro.vtt"), meta!.CaptionsPath);
        Assert.AreEqual(Path.Combine(sub, "thumb.jpg"), meta.ThumbnailPath);
    }

    [TestMethod]
    public void Load_WithAbsolutePath_KeepsIt()
    {
        var captions = Path.Combine(_folder, "intro.srt");
        File.WriteAllText(captions, "x");

        var meta = Load($$"""{ "title": "T", "captions": {{System.Text.Json.JsonSerializer.Serialize(captions)}} }""", out _);

        Assert.AreEqual(captions, meta!.CaptionsPath);
    }
}
#endif
