#if WINDOWS10_0_22621_0_OR_GREATER
using System.Diagnostics;
using StoryCADAutomation.Interpreting;

namespace StoryCADTests.Automation;

/// <summary>
///     Presentation profile pieces that need no app: subtitle output, reading time, pause
///     scaling, and the runner's --window parser (#1421 Milestone 4).
/// </summary>
[TestClass]
public class PresentationProfileTests
{
    [TestMethod]
    public void ToSrt_WithTwoCues_WritesNumberedSubRipBlocks()
    {
        var cues = new[]
        {
            new SubtitleCue(TimeSpan.FromMilliseconds(1500), TimeSpan.FromMilliseconds(4000), "First line."),
            new SubtitleCue(TimeSpan.FromSeconds(3725), TimeSpan.FromSeconds(3727), "Second line."),
        };

        var srt = PresentationProfile.ToSrt(cues);

        Assert.AreEqual(
            "1\r\n00:00:01,500 --> 00:00:04,000\r\nFirst line.\r\n\r\n" +
            "2\r\n01:02:05,000 --> 01:02:07,000\r\nSecond line.\r\n\r\n",
            srt);
    }

    [TestMethod]
    public void ToSrt_WhenNextCueStartsEarly_EndsAtNextStart()
    {
        var cues = new[]
        {
            new SubtitleCue(TimeSpan.Zero, TimeSpan.FromSeconds(5), "Long."),
            new SubtitleCue(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4), "Next."),
        };

        var srt = PresentationProfile.ToSrt(cues);

        StringAssert.Contains(srt, "00:00:00,000 --> 00:00:02,000");
    }

    [TestMethod]
    public void ReadingTime_WithShortText_ReturnsTwoSecondMinimum()
    {
        Assert.AreEqual(TimeSpan.FromSeconds(2), PresentationProfile.ReadingTime("Thanks."));
    }

    [TestMethod]
    public void ReadingTime_WithTenWords_ReturnsFourSeconds()
    {
        Assert.AreEqual(TimeSpan.FromSeconds(4),
            PresentationProfile.ReadingTime("one two three four five six seven eight nine ten"));
    }

    [TestMethod]
    public void ScalePause_WithPacing_MultipliesSeconds()
    {
        var profile = new PresentationProfile(0.5, new Stopwatch());

        Assert.AreEqual(TimeSpan.FromSeconds(1), profile.ScalePause(2));
    }

    [TestMethod]
    public void Constructor_WithZeroPacing_ThrowsArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new PresentationProfile(0, new Stopwatch()));
    }

    [TestMethod]
    public void TryParseWindowSize_WithWidthByHeight_ReturnsSize()
    {
        Assert.IsTrue(StoryCADAutomation.Program.TryParseWindowSize("1920x1080", out var size));
        Assert.AreEqual((1920, 1080), size);
    }

    [TestMethod]
    public void TryParseWindowSize_WithMissingHeight_ReturnsFalse()
    {
        Assert.IsFalse(StoryCADAutomation.Program.TryParseWindowSize("1920x", out _));
    }
}
#endif
