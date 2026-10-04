using System.Diagnostics;
using StoryCADAutomation.Driver;
using StoryCADAutomation.Interpreting;
using StoryCADAutomation.Scripting;

namespace StoryCADAutomation;

/// <summary>
///     Minimal runner for #1421 milestone 1: `run &lt;script.scs&gt; [--app &lt;exe&gt;]`, test profile,
///     results on the console. Report files, --keep-going, launch retry, folder runs and a
///     separate `check` command are deferred until a script needs them.
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
        if (args.Length < 2 || args[0] != "run")
        {
            return Usage();
        }

        var scriptPath = args[1];
        string? appPath = null;
        for (var i = 2; i < args.Length; i++)
        {
            if (args[i] == "--app" && i + 1 < args.Length)
            {
                appPath = args[++i];
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

        // Lint gates every run, so no script reaches the app with an unknown id or a dialog
        // path outside {scratch}. The interpreter also checks dialog paths at run time (B1).
        var parsed = ScriptParser.ParseFile(scriptPath);
        var diagnostics = parsed.Errors
            .Concat(new ScriptLinter(XamlUiFacts.LoadFromXamlScan()).Lint(parsed.Statements))
            .ToList();
        foreach (var diagnostic in diagnostics)
        {
            Console.Error.WriteLine(diagnostic);
        }

        if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
        {
            return ScriptError;
        }

        // No display-scale requirement for local runs (Brigid is 3840x2160 at 150%). The design's
        // 100% check exists for CI and video capture; the CI milestone sets it again.
        var driverOptions = new DriverOptions { AppPath = appPath ?? DefaultAppPath(), RequiredDpiScalePercent = null };
        var interpreter = new ScriptInterpreter(
            () => StoryCADDriver.Launch(driverOptions),
            new InterpreterOptions { OnStatement = PrintOutcome });

        var stopwatch = Stopwatch.StartNew();
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

    private static void PrintOutcome(StatementOutcome outcome)
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
    private static string DefaultAppPath()
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

    private static int Usage()
    {
        Console.Error.WriteLine("StoryCAD automation runner (issue #1421)");
        Console.Error.WriteLine();
        Console.Error.WriteLine("Usage:");
        Console.Error.WriteLine("  StoryCADAutomation run <script.scs> [--app <path to StoryCAD.exe>]");
        Console.Error.WriteLine();
        Console.Error.WriteLine("Exit codes: 0 all steps passed, 1 step failure(s), 2 script parse/lint error,");
        Console.Error.WriteLine("            3 environment or launch failure.");
        return UsageError;
    }
}
