#if WINDOWS10_0_22621_0_OR_GREATER
using StoryCADAutomation.Interpreting;
using StoryCADAutomation.Scripting;
using StoryCADAutomation.Video;
using StoryCADAutomation.Voice;

namespace StoryCADTests.Automation;

/// <summary>
///     The caption file that the video command writes next to the .mp4 (#1626, Terry, 2026-10-23):
///     splitting, timing, the [music] caption, and SubRip format.
/// </summary>
[TestClass]
public class CaptionsTests
{
    private const string IntroLine =
        "Each outline is a single file with the extension .stbx, which you can copy, move or back up like any " +
        "other document. StoryCAD ships with sample outlines; this one is The Old Man and the Sea.";

    [TestMethod]
    public void Split_WithLongNarration_GivesAtMostTwoLinesOf32Characters()
    {
        var captions = Captions.Split(IntroLine);

        Assert.IsTrue(captions.Count > 1);
        foreach (var caption in captions)
        {
            var lines = caption.Split('\n');
            Assert.IsTrue(lines.Length <= 2, caption);
            Assert.IsTrue(lines.All(l => l.Length <= Captions.MaxLineLength), caption);
        }
    }

    [TestMethod]
    public void Split_WithLongNarration_BreaksOnlyBetweenWords()
    {
        var captions = Captions.Split(IntroLine);

        var words = captions.SelectMany(c => c.Split(new[] { ' ', '\n' })).ToList();
        CollectionAssert.AreEqual(IntroLine.Split(' '), words);
    }

    [TestMethod]
    public void Split_WithShortSentence_GivesOneLine()
    {
        CollectionAssert.AreEqual(new[] { "Free and open source." }, Captions.Split("Free and open source.").ToList());
    }

    [TestMethod]
    public void Split_WithTwoSentences_BreaksAtTheSentenceEnd()
    {
        var captions = Captions.Split("StoryCAD is free and open source. It helps you build an outline of your story.");

        CollectionAssert.AreEqual(
            new[] { "StoryCAD is free and open source.", "It helps you build an outline of your story." },
            captions.Select(c => c.Replace('\n', ' ')).ToList());
    }

    [TestMethod]
    public void Split_WithClauseInLongSentence_BreaksAtTheClauseEnd()
    {
        var captions = Captions.Split(
            "The navigation pane on the left shows the outline, with the story overview at the top of the tree");

        Assert.AreEqual("The navigation pane on the left shows the outline,", captions[0].Replace('\n', ' '));
    }

    [TestMethod]
    public void Split_WithWordLongerThanALine_KeepsTheWordWhole()
    {
        const string word = "Supercalifragilisticexpialidocious-word";

        CollectionAssert.AreEqual(new[] { word }, Captions.Split(word).ToList());
    }

    [TestMethod]
    public void Units_WithNameOfCapitalizedWords_KeepsTheNameTogether()
    {
        CollectionAssert.Contains(Captions.Units("this one is The Old Man and the Sea."), "The Old Man");
        // A sentence end does not join the next sentence's first word to the name.
        CollectionAssert.AreEqual(new[] { "Sea.", "StoryCAD", "ships" }, Captions.Units("Sea. StoryCAD ships"));
    }

    [TestMethod]
    public void Wrap_WithClauseEnd_PrefersTheBreakAfterIt()
    {
        var wrapped = Captions.Wrap(Captions.Units("Problems are conflicts, and they drive the plot"));

        Assert.AreEqual("Problems are conflicts,\nand they drive the plot", wrapped);
    }

    [TestMethod]
    public void Time_WithTwoCaptions_SharesTheClipByCharacters()
    {
        // 33 and 44 characters over 7.7 s.
        var captions = Captions.Time("StoryCAD is free and open source. It helps you build an outline of your story.",
            TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(7.7));

        Assert.AreEqual(2, captions.Count);
        Assert.AreEqual(10, captions[0].Start.TotalSeconds, 0.001);
        Assert.AreEqual(13.3, captions[0].End.TotalSeconds, 0.001);
        Assert.AreEqual(13.3, captions[1].Start.TotalSeconds, 0.001);
        Assert.AreEqual(17.7, captions[1].End.TotalSeconds, 0.001);
    }

    [TestMethod]
    public void Limit_WithCaptionOver6Seconds_EndsItAt6Seconds()
    {
        var limited = Captions.Limit(new List<Caption> { new(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(9), "Long") },
            TimeSpan.FromSeconds(100));

        Assert.AreEqual(TimeSpan.FromSeconds(7), limited[0].End);
    }

    [TestMethod]
    [DataRow(5.0, 2.33)]
    [DataRow(1.8, 1.8)]
    [DataRow(1.2, 1.2)]
    public void Limit_WithCaptionUnder133Seconds_ExtendsItUpToTheNextCaption(double nextStart, double expectedEnd)
    {
        var limited = Captions.Limit(new List<Caption>
        {
            new(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1.5), "Short"),
            new(TimeSpan.FromSeconds(nextStart), TimeSpan.FromSeconds(nextStart + 2), "Next"),
        }, TimeSpan.FromSeconds(100));

        Assert.AreEqual(expectedEnd, limited[0].End.TotalSeconds, 0.001);
    }

    [TestMethod]
    public void Limit_WithCaptionCutAtTheRecordingEnd_DoesNotPassTheRecordingEnd()
    {
        var limited = Captions.Limit(new List<Caption> { new(TimeSpan.FromSeconds(9.7), TimeSpan.FromSeconds(10), "Late") },
            TimeSpan.FromSeconds(10));

        Assert.AreEqual(TimeSpan.FromSeconds(10), limited[0].End);
    }

    [TestMethod]
    public void Time_WithShortLastSentence_GivesItWordsFromTheCaptionBefore()
    {
        // Split alone gives 63 characters and "Done.", which would get 0.29 s of the 4 s clip.
        const string text = "The navigation pane on the left shows the outline of the story. Done.";

        var captions = Captions.Time(text, TimeSpan.Zero, TimeSpan.FromSeconds(4));

        Assert.AreEqual(2, captions.Count);
        Assert.AreEqual("outline of the story. Done.", captions[1].Text.Replace('\n', ' '));
        Assert.IsTrue(captions.All(c => c.End - c.Start >= Captions.MinDuration), string.Join(" | ", captions));
        Assert.IsTrue(captions.All(c => c.Text.Split('\n').Length <= 2 &&
                                        c.Text.Split('\n').All(l => l.Length <= Captions.MaxLineLength)));
        CollectionAssert.AreEqual(text.Split(' '), captions.SelectMany(c => c.Text.Split(new[] { ' ', '\n' })).ToList());
    }

    [TestMethod]
    public void Balance_WithShortCaptionThatFitsWithItsNeighbour_JoinsThem()
    {
        var parts = new List<string> { "Hello there.", "Hi." };

        Captions.Balance(parts, 10);

        CollectionAssert.AreEqual(new[] { "Hello there. Hi." }, parts);
    }

    [TestMethod]
    public void ForPlan_WithClipPastTheRecordingEnd_CutsItsCaptionsAtTheEnd()
    {
        var plan = new VideoPlan
        {
            RecordingPath = "rec.mkv", RecordingLength = TimeSpan.FromSeconds(10),
            Clips = new[]
            {
                new ClipPlacement("a.wav", TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(4),
                    "StoryCAD is free and open source. It helps you build an outline of your story."),
                new ClipPlacement("b.wav", TimeSpan.FromSeconds(11), TimeSpan.FromSeconds(2), "After the end."),
            },
        };

        var captions = Captions.ForPlan(plan);

        // The second caption is cut at 10 s; Limit does not stretch it into the closing card.
        Assert.AreEqual(2, captions.Count);
        Assert.AreEqual(8, captions[0].Start.TotalSeconds, 0.001);
        Assert.AreEqual(TimeSpan.FromSeconds(10), captions[1].End);
    }

    [TestMethod]
    public void ForPlan_WithOverlappingClips_EndsTheFirstCaptionWhereTheNextStarts()
    {
        var plan = new VideoPlan
        {
            RecordingPath = "rec.mkv", RecordingLength = TimeSpan.FromSeconds(60),
            Clips = new[]
            {
                new ClipPlacement("a.wav", TimeSpan.FromSeconds(-1), TimeSpan.FromSeconds(3), "Early words here."),
                new ClipPlacement("b.wav", TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(3), "Later words."),
            },
        };

        var captions = Captions.ForPlan(plan);

        Assert.AreEqual(new Caption(TimeSpan.Zero, TimeSpan.FromSeconds(2), "Early words here."), captions[0]);
        Assert.AreEqual(new Caption(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), "Later words."), captions[1]);
    }

    [TestMethod]
    public void ForPlan_WithTitleMusic_AddsMusicCaptionAndMovesNarrationPastTheTitle()
    {
        var plan = new VideoPlan
        {
            RecordingPath = "rec.mkv", RecordingLength = TimeSpan.FromSeconds(60), Title = "Intro",
            MusicPath = "intro-music.mp3", MusicLength = TimeSpan.FromSeconds(8),
            Clips = new[] { new ClipPlacement("a.wav", TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(3), "Free and open source.") },
        };

        var captions = Captions.ForPlan(plan);

        // The body starts at 8 - 0.5: the title card crossfades into it.
        Assert.AreEqual(2, captions.Count);
        Assert.AreEqual(new Caption(TimeSpan.Zero, TimeSpan.FromSeconds(8), "[music]"), captions[0]);
        Assert.AreEqual(9.5, captions[1].Start.TotalSeconds, 0.001);
        Assert.AreEqual(12.5, captions[1].End.TotalSeconds, 0.001);
        Assert.AreEqual("Free and open source.", captions[1].Text);
    }

    [TestMethod]
    public void ForPlan_WithTitleWithoutMusic_HasNoMusicCaption()
    {
        var plan = new VideoPlan
        {
            RecordingPath = "rec.mkv", RecordingLength = TimeSpan.FromSeconds(60), Title = "Intro",
            Clips = new[] { new ClipPlacement("a.wav", TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(3), "Free.") },
        };

        var captions = Captions.ForPlan(plan);

        Assert.AreEqual(1, captions.Count);
        Assert.AreEqual(6.5, captions[0].Start.TotalSeconds, 0.001);
    }

    [TestMethod]
    public void ForPlan_WithoutTitle_StartsAtTheRecordingAndSkipsClipsWithoutCaption()
    {
        var plan = new VideoPlan
        {
            RecordingPath = "rec.mkv", RecordingLength = TimeSpan.FromSeconds(60),
            Clips = new[]
            {
                new ClipPlacement("a.wav", TimeSpan.FromSeconds(-0.5), TimeSpan.FromSeconds(3), "Early words here."),
                new ClipPlacement("b.wav", TimeSpan.FromSeconds(10)),
            },
        };

        var captions = Captions.ForPlan(plan);

        // A clip with a cue before the recording starts at 0, as assembly places it.
        Assert.AreEqual(1, captions.Count);
        Assert.AreEqual(new Caption(TimeSpan.Zero, TimeSpan.FromSeconds(3), "Early words here."), captions[0]);
    }

    [TestMethod]
    public void ToSrt_WithTwoCaptions_WritesIndexTimesTextAndBlankLines()
    {
        var srt = Captions.ToSrt(new[]
        {
            new Caption(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2.5), "Hello"),
            new Caption(new TimeSpan(0, 1, 2, 3, 4), new TimeSpan(0, 1, 2, 4), "Two\nlines"),
        });

        Assert.AreEqual(
            "1\n00:00:01,000 --> 00:00:02,500\nHello\n\n2\n01:02:03,004 --> 01:02:04,000\nTwo\nlines\n\n", srt);
    }

    [TestMethod]
    public void Plan_WithCaptionClause_GivesTheClipItsLengthAndWrittenForm()
    {
        var folder = Path.Combine(Path.GetTempPath(), "StoryCADAutomation", "captions-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            File.WriteAllText(Path.Combine(folder, VoiceClips.ClipName("voice", "dot S-T-B-X")), "wav");
            var now = TimeSpan.Zero;
            var profile = new PresentationProfile(1.0, () => now, t => now += t, _ => TimeSpan.FromSeconds(3));
            var parsed = ScriptParser.Parse("script \"Captions\"\nstep \"A\"\nnarrate \"dot S-T-B-X\" caption \".stbx\"\n");
            new ScriptInterpreter(() => throw new InvalidOperationException(),
                new InterpreterOptions { Profile = profile, Log = _ => { } }).Execute(parsed);

            var plan = VideoCommand.Plan(parsed, profile, TimeSpan.Zero, "rec.mkv", TimeSpan.FromSeconds(9),
                folder, null, "voice", folder);

            Assert.AreEqual(1, plan.Clips.Count);
            Assert.AreEqual((TimeSpan.FromSeconds(3), ".stbx"), (plan.Clips[0].Length, plan.Clips[0].Caption));
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }
}
#endif
