#if WINDOWS10_0_22621_0_OR_GREATER
using System.Diagnostics;
using StoryCADAutomation.Interpreting;

namespace StoryCADTests.Automation;

/// <summary>
///     Presentation profile pieces that need no app: subtitle output, reading time, pause
///     scaling, and the runner's --window parser (#1421 Milestone 4); step timing (#1421
///     Milestone 5, video design section 7).
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

    // Step timing (video design, section 7). The fake clock moves only when the profile sleeps
    // or a test advances it to stand for actions.
    private TimeSpan _now;

    private PresentationProfile TimedProfile(Func<string, TimeSpan?>? clipLength = null, double pacing = 1.0)
    {
        _now = TimeSpan.Zero;
        return new PresentationProfile(pacing, () => _now, t => _now += t, clipLength);
    }

    private static TimeSpan? Clips(string text) => text switch
    {
        "one" => TimeSpan.FromSeconds(3),
        "two" => TimeSpan.FromSeconds(4),
        _ => null,
    };

    [TestMethod]
    public void Narrate_InStep_StartsClipsOneAfterTheOtherAtStepStart()
    {
        var profile = TimedProfile(Clips);
        _now = TimeSpan.FromSeconds(10);

        profile.BeginStep(0);
        profile.Narrate("one", _ => { });
        profile.Narrate("two", _ => { });

        Assert.AreEqual(TimeSpan.FromSeconds(10), profile.Cues[0].Start);
        Assert.AreEqual(TimeSpan.FromSeconds(13), profile.Cues[1].Start);
        Assert.AreEqual(TimeSpan.FromSeconds(10), _now, "the actions must not wait for the voice");
    }

    [TestMethod]
    public void EndStep_WhenNarrationIsLonger_WaitsUntilVoiceStops()
    {
        var profile = TimedProfile(Clips);
        profile.BeginStep(0);
        profile.Narrate("one", _ => { });
        profile.Narrate("two", _ => { });
        _now += TimeSpan.FromSeconds(2); // actions

        profile.EndStep();

        Assert.AreEqual(TimeSpan.FromSeconds(7), _now);
    }

    [TestMethod]
    public void EndStep_WhenActionsAreLonger_DoesNotWait()
    {
        var profile = TimedProfile(Clips);
        profile.BeginStep(0);
        profile.Narrate("one", _ => { });
        _now += TimeSpan.FromSeconds(9); // actions

        profile.EndStep();

        Assert.AreEqual(TimeSpan.FromSeconds(9), _now);
    }

    [TestMethod]
    public void EndStep_WithHold_WaitsHoldAfterNarration()
    {
        var profile = TimedProfile(Clips);
        profile.BeginStep(2);
        profile.Narrate("one", _ => { });

        profile.EndStep();

        Assert.AreEqual(TimeSpan.FromSeconds(5), _now);
    }

    [TestMethod]
    public void BeginStep_WithOpenStep_EndsItFirst()
    {
        var profile = TimedProfile(Clips);
        profile.BeginStep(1);
        profile.Narrate("one", _ => { });

        profile.BeginStep(0);
        profile.Narrate("two", _ => { });

        Assert.AreEqual(TimeSpan.FromSeconds(4), profile.Cues[1].Start);
    }

    [TestMethod]
    public void Narrate_WithMissingClip_UsesReadingTime()
    {
        var profile = TimedProfile(Clips);
        profile.BeginStep(0);
        profile.Narrate("one two three four five six seven eight nine ten", _ => { });

        profile.EndStep();

        Assert.AreEqual(TimeSpan.FromSeconds(4), _now);
    }

    [TestMethod]
    public void Narrate_WithPacing_KeepsClipLength()
    {
        var profile = TimedProfile(Clips, pacing: 2.0);
        profile.BeginStep(0);
        profile.Narrate("one", _ => { });

        Assert.AreEqual(TimeSpan.FromSeconds(3), profile.Cues[0].End - profile.Cues[0].Start);
    }

    [TestMethod]
    public void Narrate_OutsideStep_WaitsForClip()
    {
        var profile = TimedProfile(Clips);

        profile.Narrate("one", _ => { });

        Assert.AreEqual(TimeSpan.FromSeconds(3), _now);
    }

    [TestMethod]
    public void EndStep_WithNoOpenStep_DoesNotWait()
    {
        var profile = TimedProfile(Clips);

        profile.EndStep();

        Assert.AreEqual(TimeSpan.Zero, _now);
    }

    [TestMethod]
    public void Execute_WithShowSteps_EachStepLastsNarrationPlusHold()
    {
        var profile = TimedProfile(Clips);
        var script = StoryCADAutomation.Scripting.ScriptParser.Parse(
            "script \"Cards\"\n" +
            "step \"A\" hold 1\nnarrate \"one\"\nshow text \"First\"\n" +
            "step \"B\" hold 2\nnarrate \"two\"\nshow text \"Second\"\n" +
            "closing \"Bye\"\n");
        TimeSpan? closingAt = null;
        var interpreter = new ScriptInterpreter(
            () => throw new InvalidOperationException("no launch in this script"),
            new InterpreterOptions
            {
                Profile = profile,
                Log = line => closingAt = line.StartsWith("closing:") ? _now : closingAt,
            });

        var result = interpreter.Execute(script);

        Assert.IsTrue(result.AllPassed);
        Assert.AreEqual(TimeSpan.FromSeconds(4), profile.Cues[1].Start, "step B starts after A's 3 s clip and 1 s hold");
        Assert.AreEqual(TimeSpan.FromSeconds(10), closingAt, "closing starts after B's 4 s clip and 2 s hold");
    }

    [TestMethod]
    public void Execute_WithoutClosing_WaitsForLastStep()
    {
        var profile = TimedProfile(Clips);
        var script = StoryCADAutomation.Scripting.ScriptParser.Parse(
            "script \"Cards\"\nstep \"A\" hold 1\nnarrate \"one\"\nshow text \"First\"\n");
        var interpreter = new ScriptInterpreter(
            () => throw new InvalidOperationException("no launch in this script"),
            new InterpreterOptions { Profile = profile, Log = _ => { } });

        interpreter.Execute(script);

        Assert.AreEqual(TimeSpan.FromSeconds(4), _now);
    }

    [TestMethod]
    public void EndStep_WithPacing_ScalesHoldOnly()
    {
        var profile = TimedProfile(Clips, pacing: 2.0);
        profile.BeginStep(1);
        profile.Narrate("one two three four five six seven eight nine ten", _ => { });

        profile.EndStep();

        Assert.AreEqual(TimeSpan.FromSeconds(6), _now, "4 s reading time unscaled, plus 1 s hold times 2");
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
