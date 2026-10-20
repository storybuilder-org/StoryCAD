using System.Diagnostics;
using System.Globalization;
using StoryCADAutomation.Driver;
using StoryCADAutomation.Interpreting;
using StoryCADAutomation.Manual;
using StoryCADAutomation.Scripting;
using StoryCADAutomation.Video;
using StoryCADAutomation.Voice;
using StoryCADAutomation.YouTube;

namespace StoryCADAutomation;

/// <summary>
///     Runner for #1421: `run &lt;script.scs&gt;` with results on the console, in the test profile or
///     the presentation profile; `check` to lint scripts without launching the app; `voice` to
///     make the narrator clips and `video` to make a finished .mp4 (#1421 Milestone 5); `labels`
///     to compare bold text in manual pages with the XAML labels (#1422); `youtube auth` to
///     authorize the StoryCAD channel once and `youtube upload` to upload a video with its
///     meta.json (#1626). Folder runs for `run` belong to #1422.
/// </summary>
internal static class Program
{
    // Exit-code contract per devdocs/issue_1421_dsl_design.md "Runner": 0 all steps passed,
    // 1 step failure(s), 2 script parse/lint error, 3 environment or launch failure.
    // 64 is a usage error, deliberately outside that contract.
    private const int Passed = 0;
    private const int StepFailure = 1;
    private const int ScriptError = 2;
    private const int LaunchFailure = 3;
    private const int UsageError = 64;

    private static int Main(string[] args)
    {
        if (args.Length == 2 && args[0] == "check")
        {
            return Check(args[1]);
        }

        if (args.Length >= 2 && args[0] == "labels")
        {
            return Labels(args[1..]);
        }

        if (args.Length == 6 && args[0] == "video" && args[2] == "--out" && args[4] == "--assets")
        {
            return File.Exists(args[1]) ? VideoCommand.Run(args[1], args[3], args[5]) : MissingScript(args[1]);
        }

        if (args.Length == 2 && args[0] == "youtube" && args[1] == "auth")
        {
            return YouTubeCommand.Auth();
        }

        if (args.Length == 5 && args[0] == "youtube" && args[1] == "upload" && args[3] == "--meta")
        {
            return YouTubeCommand.Upload(args[2], args[4]);
        }

        if (args.Length == 4 && args[0] == "voice" && args[2] == "--out")
        {
            return Voice(args[1], args[3]);
        }

        if (args.Length < 2 || args[0] != "run")
        {
            return Usage();
        }

        var scriptPath = args[1];
        string? appPath = null;
        var outputDirectory = "automation-output";
        var ci = false;
        var presentation = false;
        var keepGoing = false;
        var pacing = 1.0;
        (int Width, int Height)? window = null;
        string? assetsDirectory = null;
        for (var i = 2; i < args.Length; i++)
        {
            if (args[i] == "--app" && i + 1 < args.Length)
            {
                appPath = args[++i];
            }
            else if (args[i] == "--assets" && i + 1 < args.Length)
            {
                assetsDirectory = args[++i];
            }
            else if (args[i] == "--out" && i + 1 < args.Length)
            {
                outputDirectory = args[++i];
            }
            else if (args[i] == "--ci")
            {
                ci = true;
            }
            else if (args[i] == "--keep-going")
            {
                keepGoing = true;
            }
            else if (args[i] == "--profile" && i + 1 < args.Length && args[i + 1] is "test" or "presentation")
            {
                presentation = args[++i] == "presentation";
            }
            else if (args[i] == "--pacing" && i + 1 < args.Length &&
                     double.TryParse(args[i + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out pacing) &&
                     double.IsFinite(pacing) && pacing > 0)
            {
                i++;
            }
            else if (args[i] == "--window" && i + 1 < args.Length && TryParseWindowSize(args[i + 1], out var size))
            {
                window = size;
                i++;
            }
            else
            {
                Console.Error.WriteLine($"unknown option '{args[i]}'");
                return Usage();
            }
        }

        if (!File.Exists(scriptPath))
        {
            Console.Error.WriteLine($"script not found: {scriptPath}");
            return UsageError;
        }

        if (assetsDirectory is not null && !Directory.Exists(assetsDirectory))
        {
            Console.Error.WriteLine($"assets folder not found: {assetsDirectory}");
            return UsageError;
        }

        // --assets seeds {scratch}/outlines as video does, so a script that opens an outline from
        // the assets folder can be tested before a recording is spent on it (#1626, 2026-10-20).
        var seedOutlines = assetsDirectory is null ? Array.Empty<string>() : VideoCommand.AssetOutlines(assetsDirectory);

        // Lint gates every run, so no script reaches the app with an unknown id or a dialog
        // path outside {scratch}. The interpreter also checks dialog paths at run time (B1).
        var parsed = ScriptParser.ParseFile(scriptPath);
        var facts = XamlUiFacts.LoadFromXamlScan();
        if (PrintDiagnostics(parsed, new ScriptLinter(facts)))
        {
            return ScriptError;
        }

        // The design's display check (1920x1080 or larger at 100% scale) runs only with --ci.
        // Local machines differ: Brigid is 3840x2160 at 150%.
        // CI keeps the scratch folder (the app's logs) so the workflow can upload it after a
        // failure; the CI machine is thrown away after the job.
        var driverOptions = ci
            ? new DriverOptions { AppPath = appPath ?? DefaultAppPath(), SweepScratchOnTeardown = false, AllowCrashReporting = true, SeedOutlines = seedOutlines }
            : new DriverOptions { AppPath = appPath ?? DefaultAppPath(), RequiredDpiScalePercent = null, MinDesktopWidth = 0, MinDesktopHeight = 0, SeedOutlines = seedOutlines };
        var stopwatch = Stopwatch.StartNew();
        var presentationProfile = presentation ? new PresentationProfile(pacing, stopwatch, ClipLengths(outputDirectory)) : null;
        var interpreter = new ScriptInterpreter(
            () =>
            {
                var driver = StoryCADDriver.Launch(driverOptions);
                if (window is { } w)
                {
                    try
                    {
                        // --window is in 100%-scale pixels, so a script gives the same layout and
                        // the same screenshots on any display scaling (#1422 evaluation). The size is
                        // the visible frame, which is exactly what a screenshot captures.
                        var scale = driver.MainWindowDpi / 96.0;
                        driver.PlaceVisibleFrame((int)Math.Round(w.Width * scale), (int)Math.Round(w.Height * scale));
                    }
                    catch (Exception ex)
                    {
                        // The interpreter never receives this driver, so nothing else would close the app.
                        driver.Dispose();
                        throw new AutomationLaunchException($"--window {w.Width}x{w.Height} failed: {ex.Message}", ex);
                    }
                }

                return driver;
            },
            new InterpreterOptions
            {
                OnStatement = PrintOutcome,
                KeepGoing = keepGoing,
                OutputDirectory = outputDirectory,
                MenuOpeners = facts.MenuOpeners,
                Profile = (IExecutionProfile?)presentationProfile ?? new TestProfile(),
            });

        ScriptRunResult result;
        try
        {
            result = interpreter.Execute(parsed);
        }
        finally
        {
            interpreter.Driver?.Dispose();
            foreach (var note in interpreter.Driver?.TeardownNotes ?? Array.Empty<string>())
            {
                Console.Error.WriteLine($"teardown: {note}");
            }
        }

        var failures = result.Outcomes.Where(o => o.Status == StatementStatus.Failed).ToList();
        var seconds = stopwatch.Elapsed.TotalSeconds;
        if (failures.Count == 0)
        {
            Console.WriteLine($"PASSED: {result.ScriptName} in {seconds:0.0}s");
            return Passed;
        }

        Console.WriteLine($"FAILED: {result.ScriptName}, {failures.Count} failure(s) in {seconds:0.0}s");
        return failures.Any(o => o.Failure is AutomationLaunchException) ? LaunchFailure : StepFailure;
    }

    /// <summary>
    ///     `check`: lints one script, or every .scs under a folder, without launching the app.
    ///     Exit 0 when no script has an error, 2 otherwise. CI runs it as a required check.
    /// </summary>
    private static int Check(string target)
    {
        string[] scripts;
        if (Directory.Exists(target))
        {
            scripts = Directory.GetFiles(target, "*.scs", SearchOption.AllDirectories);
            Array.Sort(scripts, StringComparer.Ordinal);
        }
        else if (File.Exists(target))
        {
            scripts = new[] { target };
        }
        else
        {
            Console.Error.WriteLine($"no script or folder at {target}");
            return UsageError;
        }

        var linter = new ScriptLinter(XamlUiFacts.LoadFromXamlScan());
        var failed = 0;
        foreach (var script in scripts)
        {
            Console.WriteLine($"check {script}");
            if (PrintDiagnostics(ScriptParser.ParseFile(script), linter))
            {
                failed++;
            }
        }

        Console.WriteLine($"{scripts.Length} script(s) checked, {failed} with errors.");
        return failed == 0 ? Passed : ScriptError;
    }

    /// <summary>
    ///     `labels`: lists bold text in manual pages (files, or every .md under a folder) that
    ///     matches no XAML label (#1422 D4). Advisory, because the manual also uses bold for
    ///     emphasis: exit 0 when every phrase matches, 1 when any does not.
    /// </summary>
    private static int Labels(string[] targets)
    {
        var pages = new List<string>();
        foreach (var target in targets)
        {
            if (Directory.Exists(target))
            {
                var found = Directory.GetFiles(target, "*.md", SearchOption.AllDirectories);
                Array.Sort(found, StringComparer.Ordinal);
                pages.AddRange(found);
            }
            else if (File.Exists(target))
            {
                pages.Add(target);
            }
            else
            {
                Console.Error.WriteLine($"no manual page or folder at {target}");
                return UsageError;
            }
        }

        var check = new LabelCheck(XamlUiFacts.LoadFromXamlScan());
        var misses = 0;
        foreach (var page in pages)
        {
            foreach (var miss in check.Misses(File.ReadAllText(page)))
            {
                Console.WriteLine($"{page}:{miss.Line}: **{miss.Text}** matches no XAML label");
                misses++;
            }
        }

        Console.WriteLine($"{pages.Count} page(s) checked, {misses} bold phrase(s) match no XAML label.");
        return misses == 0 ? Passed : StepFailure;
    }

    /// <summary>
    ///     `voice`: makes the missing narrator clips for a script in &lt;out&gt;/voice/ (video design,
    ///     section 8.1). Exit 3 when the API key or the voice id is not set, or ElevenLabs fails.
    /// </summary>
    internal static int Voice(string scriptPath, string outputDirectory)
    {
        if (!File.Exists(scriptPath))
        {
            Console.Error.WriteLine($"script not found: {scriptPath}");
            return UsageError;
        }

        // The variables come first: without them the command stops with exit 3 (section 8.1 rule 4).
        var apiKey = Environment.GetEnvironmentVariable(VoiceClips.ApiKeyVariable);
        var voiceId = Environment.GetEnvironmentVariable(VoiceClips.VoiceIdVariable);
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(voiceId))
        {
            Console.Error.WriteLine($"voice needs {VoiceClips.ApiKeyVariable} and {VoiceClips.VoiceIdVariable} set.");
            return LaunchFailure;
        }

        var parsed = ScriptParser.ParseFile(scriptPath);
        if (PrintDiagnostics(parsed, new ScriptLinter(XamlUiFacts.LoadFromXamlScan())))
        {
            return ScriptError;
        }

        var texts = parsed.Statements.Where(s => s.Verb == ScriptVerb.Narrate).Select(s => s.Text!);
        var folder = VoiceClips.Folder(outputDirectory);
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
            var (made, kept) = VoiceClips.MakeAsync(texts, folder, voiceId, apiKey, http, Console.WriteLine)
                .GetAwaiter().GetResult();
            Console.WriteLine($"voice: {made} clip(s) made, {kept} kept, in {folder}");
            return Passed;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or IOException
                                       or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"voice failed: {ex.Message}");
            return LaunchFailure;
        }
    }

    /// <summary>
    ///     Clip lengths from &lt;out&gt;/voice/ for the voice in STORYCAD_VOICE_ID; null without it,
    ///     so every narrate line uses the reading time.
    /// </summary>
    private static Func<string, TimeSpan?>? ClipLengths(string outputDirectory)
    {
        var voiceId = Environment.GetEnvironmentVariable(VoiceClips.VoiceIdVariable);
        return string.IsNullOrWhiteSpace(voiceId) ? null : VoiceClips.Lengths(VoiceClips.Folder(outputDirectory), voiceId);
    }

    /// <summary>Prints parse errors and lint findings; returns true when any is an error.</summary>
    internal static bool PrintDiagnostics(ScriptParseResult parsed, ScriptLinter linter)
    {
        var diagnostics = parsed.Errors.Concat(linter.Lint(parsed.Statements)).ToList();
        foreach (var diagnostic in diagnostics)
        {
            Console.Error.WriteLine(diagnostic);
        }

        return diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);
    }

    internal static void PrintOutcome(StatementOutcome outcome)
    {
        // Failures are already logged by the interpreter with their message.
        if (outcome.Status == StatementStatus.Failed)
        {
            return;
        }

        var tag = outcome.Status == StatementStatus.Passed ? "ok  " : "skip";
        Console.WriteLine($"  {tag} line {outcome.Statement.Line}: {outcome.Statement.Source}");
    }

    /// <summary>The WinAppSDK Debug x64 build output in the checkout this runner was built from.</summary>
    internal static string DefaultAppPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "StoryCAD.sln")))
        {
            dir = dir.Parent;
        }

        if (dir == null)
        {
            throw new InvalidOperationException(
                $"Could not locate StoryCAD.sln above {AppContext.BaseDirectory}; pass --app <path to StoryCAD.exe>.");
        }

        return Path.Combine(dir.FullName, "StoryCAD", "bin", "x64", "Debug",
            "net10.0-windows10.0.22621", "win-x64", "StoryCAD.exe");
    }

    /// <summary>Parses "1920x1080"; both sides must be positive.</summary>
    internal static bool TryParseWindowSize(string text, out (int Width, int Height) size)
    {
        size = default;
        var parts = text.Split('x', 'X');
        if (parts.Length == 2 &&
            int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var width) &&
            int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var height) &&
            width > 0 && height > 0)
        {
            size = (width, height);
            return true;
        }

        return false;
    }

    private static int MissingScript(string scriptPath)
    {
        Console.Error.WriteLine($"script not found: {scriptPath}");
        return UsageError;
    }

    private static int Usage()
    {
        Console.Error.WriteLine("StoryCAD automation runner (issue #1421)");
        Console.Error.WriteLine();
        Console.Error.WriteLine("Usage:");
        Console.Error.WriteLine("  StoryCADAutomation run <script.scs> [--app <path to StoryCAD.exe>] [--out <dir>] [--ci]");
        Console.Error.WriteLine("                        [--profile test|presentation] [--pacing <factor>] [--window <WxH>]");
        Console.Error.WriteLine("                        [--keep-going]   (--window: visible frame in 100%-scale pixels; --keep-going: after a failure, run the next step)");
        Console.Error.WriteLine("                        [--assets <dir>]   (copies the folder's .stbx files into {scratch}/outlines, as video does)");
        Console.Error.WriteLine("  StoryCADAutomation check <script.scs | folder>");
        Console.Error.WriteLine("  StoryCADAutomation labels <page.md | folder> [...]   (exit 1 lists bold text matching no XAML label)");
        Console.Error.WriteLine("  StoryCADAutomation voice <script.scs> --out <dir>");
        Console.Error.WriteLine("  StoryCADAutomation video <script.scs> --out <dir> --assets <dir>");
        Console.Error.WriteLine(@"  StoryCADAutomation youtube auth   (once; needs %USERPROFILE%\.storycad-youtube\client_secret.json)");
        Console.Error.WriteLine("  StoryCADAutomation youtube upload <video.mp4> --meta <meta.json>   (privacy defaults to unlisted)");
        Console.Error.WriteLine();
        Console.Error.WriteLine("Exit codes: 0 all steps passed, 1 step failure(s), 2 script parse/lint error,");
        Console.Error.WriteLine("            3 environment or launch failure.");
        return UsageError;
    }
}
