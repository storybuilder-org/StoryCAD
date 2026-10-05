using System.Diagnostics;
using StoryCADAutomation.Driver;
using StoryCADAutomation.Scripting;

namespace StoryCADAutomation.Interpreting;

/// <summary>
///     Executes a parsed script against <see cref="IUiDriver" />: one handler per verb, verb
///     dispatch only — waits, patterns, and choreography live in the driver, and the handler
///     picks the realization (design, Components). No UIA or FlaUI type appears here (the
///     macOS-seam rule); a future macOS driver runs the same interpreter unchanged.
/// </summary>
public sealed class ScriptInterpreter
{
    /// <summary>
    ///     The close verb is the File > Exit path (design, Session verbs). Leaf id from the
    ///     #1420 inventory: Shell.xaml's ExitMenuItem under FileMenuButton; the driver's menu
    ///     choreography opens the parent.
    /// </summary>
    private const string ExitMenuItemId = "ExitMenuItem";

    /// <summary>
    ///     The single built-in substitution: the runner's per-run scratch directory. Not a
    ///     variable system (design, First script).
    /// </summary>
    private const string ScratchToken = "{scratch}";

    /// <summary>How long `expect ... text` waits for the expected value (the design's 5 s implicit wait).</summary>
    private static readonly TimeSpan ExpectTextTimeout = TimeSpan.FromSeconds(5);

    private readonly Func<IUiDriver> _launchDriver;
    private readonly InterpreterOptions _options;

    /// <param name="launchDriver">
    ///     Launch realization for the launch verb; the runner binds app path and driver
    ///     options here (launching is backend-specific and stays off the driver interface).
    /// </param>
    public ScriptInterpreter(Func<IUiDriver> launchDriver, InterpreterOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(launchDriver);
        _launchDriver = launchDriver;
        _options = options ?? new InterpreterOptions();
    }

    /// <summary>
    ///     The live driver once launch has run. The interpreter never tears it down: the
    ///     runner owns teardown because it collects the app's NLog files and failure
    ///     diagnostics from scratch before the sweep (design, Reports).
    /// </summary>
    public IUiDriver? Driver { get; private set; }

    /// <summary>
    ///     Runs the script front to back. Default policy is abort-on-first-failure with the
    ///     remaining statements reported skipped (design, Failure policy); cancellation (the
    ///     runner's Ctrl+C path) also skips the remainder, checked between statements — a
    ///     statement mid-wait finishes its own timeout first.
    /// </summary>
    public ScriptRunResult Execute(ScriptParseResult script, CancellationToken cancellation = default)
    {
        ArgumentNullException.ThrowIfNull(script);
        if (!script.Success)
        {
            throw new ArgumentException(
                "Refusing to execute a script with parse errors (exit 2 before any launch).",
                nameof(script));
        }

        var outcomes = new List<StatementOutcome>(script.Statements.Count);
        string? currentStep = null;
        var aborted = false;
        foreach (var statement in script.Statements)
        {
            if (aborted || cancellation.IsCancellationRequested)
            {
                Emit(outcomes, new StatementOutcome(
                    statement, StatementStatus.Skipped, currentStep, DateTimeOffset.UtcNow, TimeSpan.Zero, null));
                continue;
            }

            var started = DateTimeOffset.UtcNow;
            var stopwatch = Stopwatch.StartNew();
            Exception? failure = null;
            try
            {
                currentStep = ExecuteStatement(statement, currentStep);
            }
            catch (Exception ex)
            {
                failure = ex;
            }

            Emit(outcomes, new StatementOutcome(
                statement,
                failure is null ? StatementStatus.Passed : StatementStatus.Failed,
                currentStep, started, stopwatch.Elapsed, failure));

            if (failure is not null)
            {
                _options.Log($"FAIL line {statement.Line}: {statement.Source} :: {failure.Message}");
                aborted = !ShouldContinueAfter(statement, failure);
            }
        }

        return new ScriptRunResult { ScriptName = script.ScriptName, Outcomes = outcomes };
    }

    private void Emit(List<StatementOutcome> outcomes, StatementOutcome outcome)
    {
        outcomes.Add(outcome);
        _options.OnStatement?.Invoke(outcome);
    }

    private bool ShouldContinueAfter(ScriptStatement statement, Exception failure)
    {
        // Session-fatal failures end the run under every policy: after app death or a failed
        // launch there is nothing left to drive (design, Failure policy).
        if (failure is AutomationLaunchException or AutomationStepException { AppExited: true })
        {
            return false;
        }

        if (_options.KeepGoing)
        {
            return true;
        }

        return IsAssertion(statement.Verb) && _options.Profile.ContinueOnAssertionFailure;
    }

    /// <summary>Runs the profile's approach (a cursor glide in presentation) and returns the driver.</summary>
    private IUiDriver Approach(ElementAddress target)
    {
        var d = RequireDriver();
        _options.Profile.Approach(d, target);
        return d;
    }

    private static bool IsAssertion(ScriptVerb verb) => verb is
        ScriptVerb.ExpectExists or ScriptVerb.ExpectText or ScriptVerb.ExpectEnabled or
        ScriptVerb.ExpectDisabled or ScriptVerb.ExpectTreeContains or ScriptVerb.ExpectWindow or
        ScriptVerb.ExpectNo or ScriptVerb.ExpectExit;

    /// <summary>Dispatches one statement; returns the (possibly updated) current step name.</summary>
    private string? ExecuteStatement(ScriptStatement s, string? currentStep)
    {
        switch (s.Verb)
        {
            case ScriptVerb.Script:
                _options.Log($"script: {s.Text}");
                return currentStep;
            case ScriptVerb.Step:
                _options.Log($"step: {s.Text}");
                return s.Text;

            case ScriptVerb.Launch:
                if (Driver is not null)
                {
                    throw new AutomationStepException("launch may run only once per script; the app is already running.");
                }

                Driver = _launchDriver();
                return currentStep;
            case ScriptVerb.Close:
                {
                    var d = RequireDriver();
                    d.InvokeMenuItem(ElementAddress.FromAutomationId(ExitMenuItemId));
                    if (!d.WaitForExit(_options.ExitTimeout))
                    {
                        throw new AutomationStepException(
                            $"the app did not exit within {_options.ExitTimeout.TotalSeconds:0}s of File > Exit; " +
                            "close expects no unsaved changes (design, Session verbs).");
                    }

                    RequireCleanExitCode(d);
                    return currentStep;
                }

            case ScriptVerb.ExpectExit:
                {
                    var d = RequireDriver();
                    if (!d.WaitForExit(_options.ExitTimeout))
                    {
                        throw new AutomationStepException(
                            $"the app process was still running after {_options.ExitTimeout.TotalSeconds:0}s.");
                    }

                    RequireCleanExitCode(d);
                    return currentStep;
                }

            case ScriptVerb.Click:
                _options.Profile.Click(RequireDriver(), s.Target!);
                return currentStep;
            case ScriptVerb.DoubleClick:
                RequireDriver().DoubleClickPointer(s.Target!);
                return currentStep;
            case ScriptVerb.RightClick:
                RequireDriver().RightClickPointer(s.Target!);
                return currentStep;
            case ScriptVerb.Drag:
                RequireDriver().Drag(s.Target!, s.DragTarget!);
                return currentStep;
            case ScriptVerb.Press:
                RequireDriver().PressChord(s.Text!);
                return currentStep;
            case ScriptVerb.Type:
                _options.Profile.Type(RequireDriver(), s.Text!);
                return currentStep;
            case ScriptVerb.Focus:
                Approach(s.Target!).Focus(s.Target!);
                return currentStep;

            case ScriptVerb.Set:
                _options.Profile.Set(RequireDriver(), s.Target!, s.Text!);
                return currentStep;
            case ScriptVerb.Select:
                Approach(s.Target!).SelectItem(s.Target!, s.Text!);
                return currentStep;
            case ScriptVerb.Toggle:
                Approach(s.Target!).Toggle(s.Target!, s.On!.Value);
                return currentStep;

            case ScriptVerb.OpenNode:
                Approach(s.Target!).ActivateTreeRow(s.Target!.Value);
                return currentStep;
            case ScriptVerb.Expand:
                Approach(s.Target!).Expand(s.Target!);
                return currentStep;
            case ScriptVerb.Collapse:
                Approach(s.Target!).Collapse(s.Target!);
                return currentStep;
            case ScriptVerb.Menu:
                ExecuteMenu(s);
                return currentStep;
            case ScriptVerb.ContextMenu:
                {
                    // Right-click is always real pointer (context menus need it), then the flyout
                    // path walks by item name: sub-items expand, the leaf invokes.
                    var d = RequireDriver();
                    d.RightClickPointer(s.Target!);
                    WalkFlyoutPath(d, s.Text!.Split('/'));
                    return currentStep;
                }

            case ScriptVerb.Tab:
                // TabViewItem realizes selection via SelectionItem; id preferred, header text fallback.
                {
                    var tab = s.Target ?? ElementAddress.FromName(s.Text!);
                    Approach(tab).Select(tab);
                    return currentStep;
                }

            case ScriptVerb.SaveFileDialog:
                {
                    var d = RequireDriver();
                    d.CompleteSaveFileDialog(ResolveScratchPath(s.Text!, d.ScratchDirectory));
                    return currentStep;
                }

            case ScriptVerb.OpenFileDialog:
                {
                    var d = RequireDriver();
                    d.CompleteOpenFileDialog(ResolveScratchPath(s.Text!, d.ScratchDirectory));
                    return currentStep;
                }

            case ScriptVerb.Dialog:
                {
                    // Invoke inside the named dialog only (review M4); the profile only moves the cursor.
                    var d = RequireDriver();
                    _options.Profile.ApproachInWindow(d, s.Text!, s.Target!);
                    d.InvokeInWindow(s.Text!, s.Target!);
                    return currentStep;
                }

            case ScriptVerb.Wait:
                // Explicit wait beyond the implicit one; same readiness bar, no pattern demand.
                RequireDriver().WaitUntilReady(s.Target!, ReadinessRequirement.Interactive);
                return currentStep;
            case ScriptVerb.WaitWindow:
                RequireDriver().WaitForWindowTitle(s.Text!);
                return currentStep;
            case ScriptVerb.Pause:
                {
                    var scaled = _options.Profile.ScalePause(s.Seconds!.Value);
                    if (scaled > TimeSpan.Zero)
                    {
                        Thread.Sleep(scaled);
                    }

                    return currentStep;
                }

            case ScriptVerb.ExpectExists:
                // Assertions use the found-only wait: they check state, never usability
                // (design, Verb set v1).
                RequireDriver().WaitUntilFound(s.Target!);
                return currentStep;
            case ScriptVerb.ExpectText:
                {
                    // Polls, because text often changes after the action that caused it: the status
                    // bar shows the previous message for a moment after a save. Reads the Value or
                    // Text pattern, else the UIA Name, which is how a TextBlock exposes its text
                    // (#1421 review M5).
                    var d = RequireDriver();
                    var expected = NormalizeText(s.Text);
                    var polling = Stopwatch.StartNew();
                    while (true)
                    {
                        var state = d.WaitUntilFound(s.Target!);
                        var actual = string.IsNullOrEmpty(state.Text) ? state.Name : state.Text;
                        if (string.Equals(NormalizeText(actual), expected, StringComparison.Ordinal))
                        {
                            return currentStep;
                        }

                        if (polling.Elapsed >= ExpectTextTimeout)
                        {
                            throw new AutomationStepException(
                                $"{s.Target} text is \"{actual}\", expected \"{s.Text}\" " +
                                $"(polled for {ExpectTextTimeout.TotalSeconds:0}s).");
                        }

                        Thread.Sleep(200);
                    }
                }

            case ScriptVerb.ExpectEnabled:
                {
                    var state = RequireDriver().WaitUntilFound(s.Target!);
                    if (!state.IsEnabled)
                    {
                        throw new AutomationStepException($"{s.Target} is disabled, expected enabled.");
                    }

                    return currentStep;
                }

            case ScriptVerb.ExpectDisabled:
                {
                    var state = RequireDriver().WaitUntilFound(s.Target!);
                    if (state.IsEnabled)
                    {
                        throw new AutomationStepException($"{s.Target} is enabled, expected disabled.");
                    }

                    return currentStep;
                }

            case ScriptVerb.ExpectTreeContains:
                RequireDriver().WaitUntilFound(ElementAddress.FromTreePath(s.Text!));
                return currentStep;
            case ScriptVerb.ExpectWindow:
                RequireDriver().WaitForWindowTitle(s.Text!);
                return currentStep;
            case ScriptVerb.ExpectNo:
                RequireDriver().VerifyNeverAppears(s.Target!);
                return currentStep;

            case ScriptVerb.Narrate:
                _options.Profile.Narrate(s.Text!, _options.Log);
                return currentStep;

            case ScriptVerb.Screenshot:
            case ScriptVerb.ScreenshotDialog:
                {
                    // Checked here as well as in the lint, like dialog paths: a bare file name only.
                    if (Path.GetFileName(s.Text!) != s.Text || !s.Text!.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new AutomationStepException($"screenshot name '{s.Text}' must be a plain .png file name.");
                    }

                    var path = Path.Combine(Path.GetFullPath(_options.OutputDirectory), s.Text);
                    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                    if (s.Verb == ScriptVerb.ScreenshotDialog)
                    {
                        RequireDriver().CaptureOpenDialog(path);
                    }
                    else
                    {
                        RequireDriver().CaptureMainWindow(path);
                    }

                    _options.Log($"screenshot: {path}");
                    return currentStep;
                }

            default:
                throw new AutomationStepException($"no handler for verb {s.Verb} (line {s.Line}).");
        }
    }

    private void ExecuteMenu(ScriptStatement s)
    {
        var d = RequireDriver();
        if (s.Target is not null)
        {
            // Leaf-id form: the driver opens parent menus (design, Navigation verbs).
            d.InvokeMenuItem(s.Target);
            return;
        }

        // Text-path form: the path itself names the parents, so this is plain composition —
        // the first segment is a top-level flyout-owning button, addressed by UIA Name.
        var segments = s.Text!.Split('/');
        if (segments.Length == 1)
        {
            d.Invoke(ElementAddress.FromName(segments[0]));
            return;
        }

        d.Expand(ElementAddress.FromName(segments[0]));
        WalkFlyoutPath(d, segments[1..]);
    }

    /// <summary>Walks flyout segments by name: sub-menu items expand, the leaf invokes.</summary>
    private static void WalkFlyoutPath(IUiDriver driver, string[] segments)
    {
        for (var i = 0; i < segments.Length - 1; i++)
        {
            driver.Expand(ElementAddress.FromName(segments[i]));
        }

        driver.Invoke(ElementAddress.FromName(segments[^1]));
    }

    /// <summary>
    ///     Substitutes {scratch} and normalizes to a full backend path. Scripts write
    ///     forward slashes (design, Dialogs).
    /// </summary>
    internal static string ResolveScratchPath(string scriptPath, string scratchDirectory)
    {
        var resolved = Path.GetFullPath(scriptPath
            .Replace(ScratchToken, scratchDirectory, StringComparison.Ordinal)
            .Replace('/', Path.DirectorySeparatorChar));

        // Checked here as well as in the lint: this is the last stop before the runner types a
        // path into a real file dialog, so a script that skipped `check` still cannot reach a
        // user folder (#1421 review B1). The trailing separator stops "run1" matching "run10".
        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(scratchDirectory))
                   + Path.DirectorySeparatorChar;
        if (!resolved.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            throw new AutomationStepException(
                $"dialog path '{scriptPath}' resolves to '{resolved}', outside the scratch folder '{root}'; " +
                "file dialogs may only use paths under {scratch}.");
        }

        return resolved;
    }

    /// <summary>
    ///     An exit counts as clean only with exit code 0, so a crash during File > Exit fails the
    ///     step instead of passing as "the app went away" (#1421 milestone 1 review).
    /// </summary>
    private static void RequireCleanExitCode(IUiDriver driver)
    {
        if (driver.ExitCode is not 0)
        {
            throw new AutomationStepException(
                $"the app exited with code {driver.ExitCode?.ToString() ?? "unknown"}, expected 0.");
        }
    }

    /// <summary>Line endings unified and trailing whitespace dropped before text comparison.</summary>
    private static string NormalizeText(string? value)
        => (value ?? string.Empty).Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').TrimEnd();

    private IUiDriver RequireDriver()
        => Driver ?? throw new AutomationStepException("no app is running; the launch statement must come first.");
}
