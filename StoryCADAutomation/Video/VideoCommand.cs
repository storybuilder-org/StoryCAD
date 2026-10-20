using System.Diagnostics;
using System.Drawing;
using StoryCADAutomation.Driver;
using StoryCADAutomation.Interpreting;
using StoryCADAutomation.Scripting;
using StoryCADAutomation.Voice;

namespace StoryCADAutomation.Video;

/// <summary>
///     `video &lt;script.scs&gt; --out &lt;dir&gt; --assets &lt;dir&gt;` (#1421 Milestone 5, video design
///     sections 8.2 and 9): runs the script in the presentation profile while ffmpeg records
///     the StoryCAD window, then assembles &lt;out&gt;/&lt;script name&gt;.mp4.
/// </summary>
internal static class VideoCommand
{
    // Exit codes as in Program (devdocs/issue_1421_dsl_design.md "Runner").
    private const int Passed = 0;
    private const int StepFailure = 1;
    private const int ScriptError = 2;
    private const int EnvironmentFailure = 3;

    /// <summary>The assets folder file names for the title card (design section 9).</summary>
    internal const string LogoFile = "logo.png";

    internal const string MusicFilePattern = "intro-music.*";

    // "intro-music.*" does not match this name: the pattern needs a dot right after "intro-music".
    /// <summary>One line of text that credits the intro music on the title card (#1626, Terry, 2026-10-20).</summary>
    internal const string MusicCreditFile = "intro-music-credit.txt";

    /// <summary>
    ///     A card that starts sooner than this into the recording starts at 0 instead, so the
    ///     viewer never sees StoryCAD start up (Terry, 2026-10-19, #1626: the intro video showed
    ///     the Open dialog and the DEV BUILD title for 0.57 s before its first card).
    /// </summary>
    internal static readonly TimeSpan CardStartCover = TimeSpan.FromSeconds(1);

    private const string SegoeUi = @"C:\Windows\Fonts\segoeui.ttf";

    public static int Run(string scriptPath, string outputDirectory, string assetsDirectory)
    {
        var ffmpeg = Ffmpeg.FromEnvironment(out var problem);
        if (ffmpeg is null)
        {
            Console.Error.WriteLine($"video: {problem}");
            return EnvironmentFailure;
        }

        if (!Directory.Exists(assetsDirectory))
        {
            Console.Error.WriteLine($"video: the assets folder '{assetsDirectory}' does not exist.");
            return EnvironmentFailure;
        }

        if (!File.Exists(SegoeUi))
        {
            Console.Error.WriteLine($"video: the card font {SegoeUi} is missing.");
            return EnvironmentFailure;
        }

        var facts = XamlUiFacts.LoadFromXamlScan();
        var parsed = ScriptParser.ParseFile(scriptPath);
        if (Program.PrintDiagnostics(parsed, new ScriptLinter(facts)))
        {
            return ScriptError;
        }

        // The recording starts at launch, so narration or a card before it has no place in it.
        var launchIndex = parsed.Statements.ToList().FindIndex(s => s.Verb == ScriptVerb.Launch);
        var early = parsed.Statements.Take(launchIndex < 0 ? parsed.Statements.Count : launchIndex)
            .Where(s => s.Verb is ScriptVerb.Narrate or ScriptVerb.ShowText or ScriptVerb.ShowImage).ToList();
        if (early.Count > 0)
        {
            Console.Error.WriteLine($"video: line {early[0].Line}: narrate and show must come after launch; the recording starts at launch.");
            return ScriptError;
        }

        var missing = parsed.Statements
            .Where(s => s.Verb == ScriptVerb.ShowImage && !File.Exists(Path.Combine(assetsDirectory, s.Text!)))
            .Select(s => s.Text!).ToList();
        if (missing.Count > 0)
        {
            Console.Error.WriteLine($"video: not in the assets folder: {string.Join(", ", missing)}");
            return EnvironmentFailure;
        }

        // Sets per-monitor DPI awareness, so window sizes below are in screen pixels.
        DisplayFacts.ReadPrimary();

        var output = Path.GetFullPath(outputDirectory);
        var work = Path.Combine(output, "video-work");
        if (Directory.Exists(work))
        {
            Directory.Delete(work, true);
        }

        Directory.CreateDirectory(work);
        var recordingPath = Path.Combine(work, "recording.mkv");
        var voiceId = Environment.GetEnvironmentVariable(VoiceClips.VoiceIdVariable);
        var clipFolder = VoiceClips.Folder(output);
        var clock = Stopwatch.StartNew();
        var profile = new PresentationProfile(1.0, clock,
            string.IsNullOrWhiteSpace(voiceId) ? null : VoiceClips.Lengths(clipFolder, voiceId));
        var driverOptions = new DriverOptions
        {
            AppPath = Program.DefaultAppPath(), RequiredDpiScalePercent = null, MinDesktopWidth = 0, MinDesktopHeight = 0,
            SeedOutlines = AssetOutlines(assetsDirectory),
        };

        ScreenRecorder? recorder = null;
        var interpreter = new ScriptInterpreter(
            () =>
            {
                var driver = StoryCADDriver.Launch(driverOptions);
                try
                {
                    // StoryCAD lays itself out in 1920x1080 at any display scale; the recording area
                    // is that size in screen pixels, and assembly scales it back to 1920x1080.
                    var dpi = driver.MainWindowDpi;
                    var area = CaptureSize(dpi);
                    var inset = BorderInset(dpi);
                    var frame = driver.PlaceVisibleFrame(area.Width + 2 * inset, area.Height + 2 * inset);
                    var inside = new Rectangle(frame.X + inset, frame.Y + inset, area.Width, area.Height);
                    recorder = ScreenRecorder.Start(ffmpeg, inside, recordingPath, clock);
                }
                catch (Exception ex)
                {
                    // The interpreter never receives this driver, so nothing else would close the app.
                    driver.Dispose();
                    throw new AutomationLaunchException($"video: recording did not start: {ex.Message}", ex);
                }

                return driver;
            },
            new InterpreterOptions
            {
                OnStatement = Program.PrintOutcome,
                OutputDirectory = output,
                MenuOpeners = facts.MenuOpeners,
                Profile = profile,
            });

        ScriptRunResult result;
        var recordingComplete = true;
        try
        {
            result = interpreter.Execute(parsed);
        }
        finally
        {
            try
            {
                recorder?.Stop();
            }
            catch (Exception ex)
            {
                recordingComplete = false;
                Console.Error.WriteLine($"video: {ex.Message}");
            }

            recorder?.Dispose();
            interpreter.Driver?.Dispose();
            foreach (var note in interpreter.Driver?.TeardownNotes ?? Array.Empty<string>())
            {
                Console.Error.WriteLine($"teardown: {note}");
            }
        }

        var failures = result.Outcomes.Where(o => o.Status == StatementStatus.Failed).ToList();
        if (failures.Count > 0)
        {
            // A failed demo is recorded again (Terry, 2026-10-05); its recording is of no use.
            File.Delete(recordingPath);
            Console.WriteLine($"FAILED: {result.ScriptName}, {failures.Count} failure(s); recording deleted.");
            return failures.Any(o => o.Failure is AutomationLaunchException) ? EnvironmentFailure : StepFailure;
        }

        if (!recordingComplete)
        {
            Console.Error.WriteLine("video: the recording did not end cleanly; no video made.");
            return EnvironmentFailure;
        }

        if (recorder is null || !File.Exists(recordingPath))
        {
            Console.Error.WriteLine("video: the script never launched StoryCAD, so there is nothing recorded.");
            return ScriptError;
        }

        var plan = Plan(parsed, profile, recorder.ClockOffset, recordingPath,
            ffmpeg.Duration(recordingPath) ?? clock.Elapsed - recorder.ClockOffset,
            assetsDirectory, ffmpeg, voiceId, clipFolder);
        var mp4 = Path.Combine(output, Path.GetFileNameWithoutExtension(scriptPath) + ".mp4");
        try
        {
            VideoAssembler.Assemble(ffmpeg, plan, work, mp4);
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine($"video: assembly failed: {ex.Message}");
            return EnvironmentFailure;
        }

        Console.WriteLine($"video: {mp4}");
        return Passed;
    }

    /// <summary>
    ///     The outline files in the assets folder. The run copies them into {scratch}/outlines,
    ///     so the intro video opens Terry's filled-in copy of a sample; the shipped sample has
    ///     empty tabs (#1626, Terry, 2026-10-20).
    /// </summary>
    internal static IReadOnlyList<string> AssetOutlines(string assetsDirectory)
        => Directory.GetFiles(assetsDirectory, "*.stbx").OrderBy(f => f, StringComparer.Ordinal).ToList();

    /// <summary>The window size in screen pixels that gives StoryCAD a 1920x1080 layout at this DPI.</summary>
    internal static Size CaptureSize(int dpi)
    {
        // Even sizes, because the H.264 encoder needs them.
        static int Even(double pixels) => (int)Math.Round(pixels / 2) * 2;
        return new Size(Even(VideoAssembler.Width * dpi / 96.0), Even(VideoAssembler.Height * dpi / 96.0));
    }

    /// <summary>
    ///     The width of the window border in screen pixels. The border lets the wallpaper show
    ///     through (2 px on every side at 150%, seen on Brigid, 2026-10-13), so the recording
    ///     starts inside it.
    /// </summary>
    internal static int BorderInset(int dpi) => (int)Math.Ceiling(dpi / 96.0);

    /// <summary>
    ///     The plan for assembly: each show card covers its step, each clip starts at its cue,
    ///     all moved from the runner's clock to the recording's time. A card that starts under
    ///     <see cref="CardStartCover" /> into the recording starts at 0; nothing else moves.
    /// </summary>
    internal static VideoPlan Plan(ScriptParseResult parsed, PresentationProfile profile, TimeSpan offset,
        string recordingPath, TimeSpan recordingLength, string assetsDirectory, Ffmpeg? ffmpeg,
        string? voiceId, string clipFolder)
    {
        var cards = new List<VideoCard>();
        var step = -1;
        foreach (var s in parsed.Statements)
        {
            if (s.Verb == ScriptVerb.Step)
            {
                step++;
            }
            else if (s.Verb is ScriptVerb.ShowText or ScriptVerb.ShowImage && step < profile.Steps.Count)
            {
                var span = profile.Steps[step];
                var start = span.Start - offset;
                cards.Add(new VideoCard(start < CardStartCover ? TimeSpan.Zero : start, span.End - offset,
                    s.Verb == ScriptVerb.ShowText ? s.Text : null,
                    s.Verb == ScriptVerb.ShowImage ? Path.GetFullPath(Path.Combine(assetsDirectory, s.Text!)) : null));
            }
        }

        var clips = new List<ClipPlacement>();
        foreach (var cue in profile.Cues)
        {
            var path = string.IsNullOrWhiteSpace(voiceId) ? null : Path.Combine(clipFolder, VoiceClips.ClipName(voiceId, cue.Text));
            if (path is not null && File.Exists(path))
            {
                clips.Add(new ClipPlacement(path, cue.Start - offset));
            }
            else
            {
                Console.WriteLine($"video: no clip, silent: \"{cue.Text}\"");
            }
        }

        var logo = Path.Combine(assetsDirectory, LogoFile);
        var creditPath = Path.Combine(assetsDirectory, MusicCreditFile);
        var credit = File.Exists(creditPath) ? File.ReadLines(creditPath).FirstOrDefault()?.Trim() : null;
        var music = Directory.GetFiles(assetsDirectory, MusicFilePattern).OrderBy(f => f, StringComparer.Ordinal).FirstOrDefault();
        return new VideoPlan
        {
            RecordingPath = recordingPath,
            RecordingLength = recordingLength,
            Title = parsed.Statements.FirstOrDefault(s => s.Verb == ScriptVerb.Title)?.Text,
            Closing = parsed.Statements.FirstOrDefault(s => s.Verb == ScriptVerb.Closing)?.Text,
            Cards = cards,
            Clips = clips,
            LogoPath = File.Exists(logo) ? Path.GetFullPath(logo) : null,
            MusicPath = music is null ? null : Path.GetFullPath(music),
            MusicLength = music is null ? null : ffmpeg?.Duration(music),
            MusicCredit = string.IsNullOrEmpty(credit) ? null : credit,
        };
    }
}
