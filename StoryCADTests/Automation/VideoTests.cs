#if WINDOWS10_0_22621_0_OR_GREATER
using System.Drawing;
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
    public void BodyArguments_WithoutClips_UsesSilence()
    {
        var plan = new VideoPlan { RecordingPath = "rec.mkv", RecordingLength = TimeSpan.FromSeconds(3) };

        var args = VideoAssembler.BodyArguments(plan, "body.mp4");

        CollectionAssert.Contains(args.ToList(), "anullsrc=r=48000:cl=stereo:d=3");
    }

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
        Assert.AreEqual((TimeSpan.FromSeconds(-1), TimeSpan.FromSeconds(2), "First"),
            (plan.Cards[0].Start, plan.Cards[0].End, plan.Cards[0].Text));
        Assert.AreEqual((TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(7)), (plan.Cards[1].Start, plan.Cards[1].End));
        Assert.AreEqual(Path.Combine(_folder, "pie.png"), plan.Cards[1].ImagePath);
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
        };
        var mp4 = Path.Combine(_folder, "out.mp4");

        VideoAssembler.Assemble(ffmpeg, plan, Path.Combine(_folder, "work"), mp4);

        // 3 s title + 6 s recording + 10 s closing.
        var length = ffmpeg.Duration(mp4);
        Assert.IsNotNull(length);
        Assert.AreEqual(19, length.Value.TotalSeconds, 0.2);
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
