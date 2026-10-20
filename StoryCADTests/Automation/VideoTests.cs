#if WINDOWS10_0_22621_0_OR_GREATER
using System.Drawing;
using StoryCADAutomation.Driver;
using StoryCADAutomation.Interpreting;
using StoryCADAutomation.Scripting;
using StoryCADAutomation.Video;

namespace StoryCADTests.Automation;

/// <summary>
///     The video command's parts that need no app (#1421 Milestone 5, video design sections 8.2
///     and 9). One test runs the real ffmpeg when STORYCAD_FFMPEG is set; elsewhere it is
///     inconclusive.
/// </summary>
[TestClass]
public class VideoTests
{
    private string _folder = null!;

    [TestInitialize]
    public void Setup()
    {
        _folder = Path.Combine(Path.GetTempPath(), "StoryCADAutomation", "video-test-" + Guid.NewGuid().ToString("N"));
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

    [TestMethod]
    [DataRow(96, 1920, 1080)]
    [DataRow(120, 2400, 1350)]
    [DataRow(144, 2880, 1620)]
    [DataRow(192, 3840, 2160)]
    public void CaptureSize_AtDisplayDpi_GivesA1920x1080Layout(int dpi, int width, int height)
    {
        Assert.AreEqual(new Size(width, height), VideoCommand.CaptureSize(dpi));
    }

    [TestMethod]
    [DataRow(96, 1)]
    [DataRow(144, 2)]
    [DataRow(192, 2)]
    public void BorderInset_AtDisplayDpi_CoversTheWindowBorder(int dpi, int inset)
    {
        Assert.AreEqual(inset, VideoCommand.BorderInset(dpi));
    }

    [TestMethod]
    public void ParseOutTime_WithProgressLine_ReturnsTime()
    {
        Assert.AreEqual(TimeSpan.FromMilliseconds(1500), ScreenRecorder.ParseOutTime("out_time_us=1500000"));
        Assert.IsNull(ScreenRecorder.ParseOutTime("out_time_us=N/A"));
        Assert.IsNull(ScreenRecorder.ParseOutTime("frame=12"));
    }

    [TestMethod]
    public void ParseDuration_WithInputInfo_ReturnsLength()
    {
        Assert.AreEqual(TimeSpan.FromSeconds(83.25),
            Ffmpeg.ParseDuration("Input #0, wav, from 'a.wav':\n  Duration: 00:01:23.25, bitrate: 384 kb/s"));
        Assert.IsNull(Ffmpeg.ParseDuration("a.wav: No such file or directory"));
    }

    [TestMethod]
    public void BodyArguments_PlaceCardsAndClipsAtTheirTimes()
    {
        var plan = new VideoPlan
        {
            RecordingPath = "rec.mkv",
            RecordingLength = TimeSpan.FromSeconds(20),
            Cards = new[] { new VideoCard(TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(9.5), "Free", null) },
            Clips = new[] { new ClipPlacement("a.wav", TimeSpan.FromSeconds(1.25)) },
        };

        var filter = Filter(VideoAssembler.BodyArguments(plan, "body.mp4"));

        StringAssert.Contains(filter, "enable='between(t,4,9.5)'");
        StringAssert.Contains(filter, "adelay=1250:all=1");
        StringAssert.Contains(filter, "textfile=card0.txt");
    }

    [TestMethod]
    public void BodyArguments_WithCard_FadesTheCardInAndOut()
    {
        var plan = new VideoPlan
        {
            RecordingPath = "rec.mkv",
            RecordingLength = TimeSpan.FromSeconds(20),
            Cards = new[] { new VideoCard(TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(9.5), null, "pie.png") },
        };

        var filter = Filter(VideoAssembler.BodyArguments(plan, "body.mp4"));

        StringAssert.Contains(filter, "fps=30,format=yuva420p,fade=t=in:st=4:d=0.3:alpha=1,fade=t=out:st=9.2:d=0.3:alpha=1[c0]");
    }

    [TestMethod]
    public void CardFades_WithCardShorterThanTwoFades_UsesHalfItsLengthForEach()
    {
        var fades = VideoAssembler.CardFades(new VideoCard(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1.4), "Short", null));

        Assert.AreEqual(",format=yuva420p,fade=t=in:st=1:d=0.2:alpha=1,fade=t=out:st=1.2:d=0.2:alpha=1", fades);
    }

    [TestMethod]
    public void CardFades_WithCardAtZero_DoesNotFadeIn()
    {
        var fades = VideoAssembler.CardFades(new VideoCard(TimeSpan.Zero, TimeSpan.FromSeconds(3), "First", null));

        Assert.AreEqual(",format=yuva420p,fade=t=out:st=2.7:d=0.3:alpha=1", fades);
    }

    [TestMethod]
    public void CardFades_WithEmptyCard_ReturnsNoFades()
    {
        Assert.AreEqual(string.Empty,
            VideoAssembler.CardFades(new VideoCard(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2), "None", null)));
    }

    [TestMethod]
    public void JoinArguments_WithTitleBodyAndClosing_CrossfadesAtCumulativeOffsets()
    {
        var parts = new[]
        {
            ("title.mp4", TimeSpan.FromSeconds(3)), ("body.mp4", TimeSpan.FromSeconds(6)), ("closing.mp4", TimeSpan.FromSeconds(10)),
        };

        var args = VideoAssembler.JoinArguments(parts, "out.mp4").ToList();

        // The second offset is 3 + 6 - 0.5 - 0.5: the first crossfade made the joined video shorter.
        Assert.AreEqual(
            "[0:v][1:v]xfade=transition=fade:duration=0.5:offset=2.5[v1];[0:a][1:a]acrossfade=d=0.5[a1];" +
            "[v1][2:v]xfade=transition=fade:duration=0.5:offset=8[v2];[a1][2:a]acrossfade=d=0.5[a2]",
            Filter(args));
        CollectionAssert.AreEqual(new[] { "-map", "[v2]", "-map", "[a2]" }, args.GetRange(args.IndexOf("-map"), 4));
        Assert.AreEqual("18", args[args.IndexOf("-t") + 1]);
        Assert.AreEqual("+faststart", args[args.IndexOf("-movflags") + 1]);
        Assert.AreEqual("libx264", args[args.IndexOf("-c:v") + 1]);
        Assert.AreEqual("out.mp4", args[^1]);
        CollectionAssert.DoesNotContain(args, "concat");
    }

    [TestMethod]
    public void JoinArguments_WithoutClosing_CrossfadesOnce()
    {
        var parts = new[] { ("title.mp4", TimeSpan.FromSeconds(3)), ("body.mp4", TimeSpan.FromSeconds(6)) };

        var args = VideoAssembler.JoinArguments(parts, "out.mp4").ToList();

        Assert.AreEqual(
            "[0:v][1:v]xfade=transition=fade:duration=0.5:offset=2.5[v1];[0:a][1:a]acrossfade=d=0.5[a1]", Filter(args));
        Assert.AreEqual("8.5", args[args.IndexOf("-t") + 1]);
    }

    [TestMethod]
    public void JoinArguments_WithoutTitle_CrossfadesBodyIntoClosing()
    {
        var parts = new[] { ("body.mp4", TimeSpan.FromSeconds(6)), ("closing.mp4", TimeSpan.FromSeconds(10)) };

        var args = VideoAssembler.JoinArguments(parts, "out.mp4").ToList();

        StringAssert.Contains(Filter(args), "offset=5.5[v1]");
        Assert.AreEqual("15.5", args[args.IndexOf("-t") + 1]);
    }

    [TestMethod]
    public void JoinArguments_WithBodyOnly_CopiesIt()
    {
        var args = VideoAssembler.JoinArguments(new[] { ("body.mp4", TimeSpan.FromSeconds(6)) }, "out.mp4").ToList();

        CollectionAssert.AreEqual(new[] { "-i", "body.mp4", "-c", "copy", "-movflags", "+faststart", "out.mp4" },
            args.Skip(4).ToList());
    }

    [TestMethod]
    public void BodyArguments_WithoutClips_UsesSilence()
    {
        var plan = new VideoPlan { RecordingPath = "rec.mkv", RecordingLength = TimeSpan.FromSeconds(3) };

        var args = VideoAssembler.BodyArguments(plan, "body.mp4");

        CollectionAssert.Contains(args.ToList(), "anullsrc=r=48000:cl=stereo:d=3");
    }

    [TestMethod]
    public void TitleArguments_WithMusicCredit_DrawsCreditNearTheBottom()
    {
        var plan = new VideoPlan
        {
            RecordingPath = "rec.mkv", RecordingLength = TimeSpan.FromSeconds(3), Title = "Intro",
            MusicCredit = "Music: \"Bright Day\" by Someone",
        };

        var filter = FilterOf(VideoAssembler.TitleArguments(plan, "title.mp4"));

        StringAssert.Contains(filter, "textfile=title.txt");
        StringAssert.Contains(filter, $"textfile={VideoAssembler.CreditFile}:expansion=none");
        StringAssert.Contains(filter, "fontsize=36:fontcolor=white@0.8:x=(w-text_w)/2:y=h-text_h-80");
        // The credit uses the title's font file.
        Assert.AreEqual(2, filter.Split("fontfile='C\\:/Windows/Fonts/segoeui.ttf'").Length - 1);
    }

    [TestMethod]
    public void TitleArguments_WithoutMusicCredit_DrawsTitleOnly()
    {
        var plan = new VideoPlan { RecordingPath = "rec.mkv", RecordingLength = TimeSpan.FromSeconds(3), Title = "Intro" };

        var filter = FilterOf(VideoAssembler.TitleArguments(plan, "title.mp4"));

        StringAssert.Contains(filter, "textfile=title.txt");
        Assert.IsFalse(filter.Contains(VideoAssembler.CreditFile), filter);
        Assert.AreEqual(1, filter.Split("drawtext=").Length - 1);
    }

    [TestMethod]
    public void Plan_WithMusicCreditFile_ReadsTheCredit()
    {
        File.WriteAllText(Path.Combine(_folder, VideoCommand.MusicCreditFile), "Music: \"Bright Day\" by Someone\r\n");
        var parsed = ScriptParser.Parse("script \"Cards\"\ntitle \"Intro\"\n");

        var plan = VideoCommand.Plan(parsed, new PresentationProfile(1.0, () => TimeSpan.Zero, _ => { }, null),
            TimeSpan.Zero, "rec.mkv", TimeSpan.FromSeconds(9), _folder, null, null, _folder);

        Assert.AreEqual("Music: \"Bright Day\" by Someone", plan.MusicCredit);
        // The credit file is not taken for the intro music.
        Assert.IsNull(plan.MusicPath);
    }

    [TestMethod]
    public void Plan_WithoutMusicCreditFile_HasNoCredit()
    {
        var parsed = ScriptParser.Parse("script \"Cards\"\ntitle \"Intro\"\n");

        var plan = VideoCommand.Plan(parsed, new PresentationProfile(1.0, () => TimeSpan.Zero, _ => { }, null),
            TimeSpan.Zero, "rec.mkv", TimeSpan.FromSeconds(9), _folder, null, null, _folder);

        Assert.IsNull(plan.MusicCredit);
    }

    private static string FilterOf(IReadOnlyList<string> args) => args[args.ToList().IndexOf("-filter_complex") + 1];

    [TestMethod]
    public void Plan_PutsEachCardOverItsStepInRecordingTime()
    {
        var now = TimeSpan.Zero;
        var profile = new PresentationProfile(1.0, () => now, t => now += t, _ => TimeSpan.FromSeconds(3));
        var parsed = ScriptParser.Parse(
            "script \"Cards\"\nstep \"A\"\nnarrate \"one\"\nshow text \"First\"\n" +
            "step \"B\" hold 2\nnarrate \"two\"\nshow image \"pie.png\"\n");
        new ScriptInterpreter(() => throw new InvalidOperationException(),
            new InterpreterOptions { Profile = profile, Log = _ => { } }).Execute(parsed);

        var plan = VideoCommand.Plan(parsed, profile, TimeSpan.FromSeconds(1), "rec.mkv", TimeSpan.FromSeconds(9),
            _folder, null, null, _folder);

        Assert.AreEqual(2, plan.Cards.Count);
        // The first card started before the recording, so it starts at 0 (#1626).
        Assert.AreEqual((TimeSpan.Zero, TimeSpan.FromSeconds(2), "First"),
            (plan.Cards[0].Start, plan.Cards[0].End, plan.Cards[0].Text));
        Assert.AreEqual((TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(7)), (plan.Cards[1].Start, plan.Cards[1].End));
        Assert.AreEqual(Path.Combine(_folder, "pie.png"), plan.Cards[1].ImagePath);
    }

    [TestMethod]
    [DataRow(9.43, 0.0)]
    [DataRow(9.0, 1.0)]
    public void Plan_WithCardStartUnderOneSecond_StartsCardAtZero(double offsetSeconds, double expectedStart)
    {
        // The step starts 10 s after the runner's clock starts; the offset puts it at 0.57 s or 1 s into the recording.
        var now = TimeSpan.FromSeconds(10);
        var profile = new PresentationProfile(1.0, () => now, t => now += t, _ => TimeSpan.FromSeconds(3));
        var parsed = ScriptParser.Parse("script \"Cards\"\nstep \"A\"\nnarrate \"one\"\nshow text \"First\"\n");
        new ScriptInterpreter(() => throw new InvalidOperationException(),
            new InterpreterOptions { Profile = profile, Log = _ => { } }).Execute(parsed);
        var offset = TimeSpan.FromSeconds(offsetSeconds);

        var plan = VideoCommand.Plan(parsed, profile, offset, "rec.mkv", TimeSpan.FromSeconds(9),
            _folder, null, null, _folder);

        Assert.AreEqual(TimeSpan.FromSeconds(expectedStart), plan.Cards[0].Start);
        Assert.AreEqual(profile.Steps[0].End - offset, plan.Cards[0].End);
    }

    [TestMethod]
    public void Assemble_WithRealFfmpeg_WritesMp4WithAllParts()
    {
        var ffmpeg = Ffmpeg.FromEnvironment(out var problem);
        if (ffmpeg is null)
        {
            Assert.Inconclusive($"needs ffmpeg: {problem}");
        }

        // Stand-ins: a 2880x1620 recording (150% scale), a clip, an image, a logo, music.
        ffmpeg.Run(new[] { "-y", "-f", "lavfi", "-i", "testsrc=s=2880x1620:r=30:d=6", "-pix_fmt", "yuv420p", "rec.mkv" }, _folder);
        ffmpeg.Run(new[] { "-y", "-f", "lavfi", "-i", "sine=f=440:d=2:r=24000", "clip.wav" }, _folder);
        ffmpeg.Run(new[] { "-y", "-f", "lavfi", "-i", "testsrc=s=800x600:d=1", "-frames:v", "1", "pie.png" }, _folder);
        ffmpeg.Run(new[] { "-y", "-f", "lavfi", "-i", "color=c=orange:s=600x300:d=1", "-frames:v", "1", "logo.png" }, _folder);
        ffmpeg.Run(new[] { "-y", "-f", "lavfi", "-i", "sine=f=220:d=3", "music.wav" }, _folder);
        var plan = new VideoPlan
        {
            RecordingPath = Path.Combine(_folder, "rec.mkv"),
            RecordingLength = TimeSpan.FromSeconds(6),
            Title = "A Five-Minute Introduction to StoryCAD",
            Closing = "Like and subscribe",
            Cards = new[]
            {
                new VideoCard(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), "Free and open source", null),
                new VideoCard(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(4), null, Path.Combine(_folder, "pie.png")),
            },
            Clips = new[] { new ClipPlacement(Path.Combine(_folder, "clip.wav"), TimeSpan.FromSeconds(0.5)) },
            LogoPath = Path.Combine(_folder, "logo.png"),
            MusicPath = Path.Combine(_folder, "music.wav"),
            MusicLength = TimeSpan.FromSeconds(3),
            MusicCredit = "Music: \"Bright Day\" by Someone",
        };
        var mp4 = Path.Combine(_folder, "out.mp4");

        VideoAssembler.Assemble(ffmpeg, plan, Path.Combine(_folder, "work"), mp4);

        // 3 s title + 6 s recording + 10 s closing, less two 0.5 s crossfades.
        var length = ffmpeg.Duration(mp4);
        Assert.IsNotNull(length);
        Assert.AreEqual(18, length.Value.TotalSeconds, 0.2);
    }

    // #1626 (Terry, 2026-10-20): a list card has one item on each line.
    [TestMethod]
    public void WriteCardFiles_WithLineBreaks_WritesOneLinePerItem()
    {
        var parsed = ScriptParser.Parse("script \"Cards\"\nstep \"A\"\n" + @"show text ""Story elements:\nProblem\nScene""" + "\n");
        var plan = new VideoPlan
        {
            RecordingPath = "rec.mkv",
            RecordingLength = TimeSpan.FromSeconds(9),
            Cards = new[]
            {
                new VideoCard(TimeSpan.Zero, TimeSpan.FromSeconds(4), null, "a.png"),
                new VideoCard(TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(9), parsed.Statements[2].Text, null),
            },
        };

        VideoAssembler.WriteCardFiles(plan, _folder);

        // Only text cards get a file, named for the card's place in the plan.
        Assert.IsFalse(File.Exists(Path.Combine(_folder, "card0.txt")));
        CollectionAssert.AreEqual(new[] { "Story elements:", "Problem", "Scene" },
            File.ReadAllLines(Path.Combine(_folder, "card1.txt")));
    }

    // #1626 (Terry, 2026-10-20): the intro video opens Terry's copy of a sample from the assets folder.
    [TestMethod]
    public void AssetOutlines_WithMixedAssets_ReturnsOnlyOutlineFiles()
    {
        File.WriteAllText(Path.Combine(_folder, "The Old Man and the Sea.stbx"), "{}");
        File.WriteAllText(Path.Combine(_folder, "Hamlet.stbx"), "{}");
        File.WriteAllText(Path.Combine(_folder, VideoCommand.LogoFile), "png");
        File.WriteAllText(Path.Combine(_folder, VideoCommand.MusicCreditFile), "credit");

        var outlines = VideoCommand.AssetOutlines(_folder).Select(Path.GetFileName).ToList();

        CollectionAssert.AreEqual(new[] { "Hamlet.stbx", "The Old Man and the Sea.stbx" }, outlines);
    }

    [TestMethod]
    public void CopyOutlines_WithAssetOutline_CopiesItIntoScratchOutlines()
    {
        var assets = Path.Combine(_folder, "assets");
        Directory.CreateDirectory(assets);
        var source = Path.Combine(assets, "The Old Man and the Sea.stbx");
        File.WriteAllText(source, "{\"Name\": \"Santiago\"}");
        var scratch = ScratchArea.Create(Path.Combine(_folder, "scratch"));

        scratch.CopyOutlines(VideoCommand.AssetOutlines(assets));

        var copy = Path.Combine(scratch.OutlineDirectory, "The Old Man and the Sea.stbx");
        Assert.AreEqual(Path.Combine(scratch.Root, "outlines"), scratch.OutlineDirectory);
        Assert.AreEqual(File.ReadAllText(source), File.ReadAllText(copy));
    }

    [TestMethod]
    public void Run_WithNarrateBeforeLaunch_ReturnsExitCode2()
    {
        var script = Path.Combine(_folder, "early.scs");
        File.WriteAllText(script, "script \"Early\"\nstep \"A\"\nnarrate \"Too soon.\"\nlaunch\nclose\n");
        var saved = Environment.GetEnvironmentVariable(Ffmpeg.PathVariable);
        // Any existing file passes the ffmpeg check; the command stops before it runs ffmpeg.
        Environment.SetEnvironmentVariable(Ffmpeg.PathVariable, script);
        try
        {
            Assert.AreEqual(2, VideoCommand.Run(script, _folder, _folder));
        }
        finally
        {
            Environment.SetEnvironmentVariable(Ffmpeg.PathVariable, saved);
        }
    }

    private static string Filter(IReadOnlyList<string> args) => args[args.ToList().IndexOf("-filter_complex") + 1];
}
#endif
