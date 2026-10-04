using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.UIA3;
using Mouse = FlaUI.Core.Input.Mouse;
using MouseButton = FlaUI.Core.Input.MouseButton;

namespace StoryCADAutomation.Driver;

/// <summary>
///     FlaUI/UIA3 realization of <see cref="IUiDriver" /> for the StoryCAD WinAppSDK head:
///     contained launch (per-run scratch app-data folder with seeded preferences, scrubbed child
///     environment, refusals, kill-on-close job object), process-rooted element location with
///     per-verb readiness waits, and pointer/keyboard primitives gated on a foreground check.
///     Implements issue #1421 Code task 3; the interpreter and runner tasks build on top.
/// </summary>
public sealed class StoryCADDriver : IUiDriver
{
    /// <summary>
    ///     Environment variables removed from the child process. The child must not inherit
    ///     debug hooks or backend credentials from the operator's shell:
    ///     COLLAB_DEBUG=1 on a Debug build throws a Debugger.Launch() prompt that hangs
    ///     unattended runs (issue #1461); the rest keep Collaborator, the backend DB, and AI
    ///     endpoints unreachable. UNO_DISPLAY_SCALE_OVERRIDE is honored only in the
    ///     Skia/desktop-head branch (StoryCADLib/Models/Windowing.cs:618), not the WinAppSDK
    ///     head this driver launches; scrubbing it is defensive.
    /// </summary>
    private static readonly string[] ScrubbedEnvironmentVariables =
    {
        "COLLAB_DEBUG",
        "COLLAB_DEV_ENABLED",
        "COLLAB_PROXY_URL",
        "COLLAB_PROXY_TOKEN",
        "COLLAB_TEMPLATE_DIR",
        "OPENAI_API_KEY",
        "STORYCAD_TEST_CONNECTION",
        "UNO_DISPLAY_SCALE_OVERRIDE",
    };

    private const string RootDirectoryOverrideVariable = "STORYCAD_ROOT_DIR";
    private const string SaveDialogFileNameId = "1001";
    private const string OpenDialogFileNameId = "1148";

    private readonly DriverOptions _options;
    private readonly Process _process;
    private readonly KillOnCloseJob _job;
    private readonly UIA3Automation _automation;
    private readonly ScratchArea _scratch;
    private readonly ElementLocator _locator;
    private int _tornDown;

    private StoryCADDriver(
        DriverOptions options, Process process, KillOnCloseJob job, UIA3Automation automation,
        ScratchArea scratch, DisplayFacts display)
    {
        _options = options;
        _process = process;
        _job = job;
        _automation = automation;
        _scratch = scratch;
        Display = display;
        _locator = new ElementLocator(automation, process.Id, EnsureRunnable);
    }

    /// <inheritdoc />
    public string ScratchDirectory => _scratch.Root;

    /// <inheritdoc />
    public DisplayFacts Display { get; }

    /// <inheritdoc />
    public bool HasExited
    {
        get
        {
            try
            {
                return _process.HasExited;
            }
            catch (Exception)
            {
                return true; // disposed during teardown counts as exited
            }
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<string> TeardownNotes { get; private set; } = Array.Empty<string>();

    // --- launch ---------------------------------------------------------------------------

    /// <summary>
    ///     Prepares the environment and launches StoryCAD, turning the manual preconditions of
    ///     devdocs/tools/uia_header_probe.ps1 into code. Every refusal and failure here is the
    ///     launch class (the runner's exit 3); once this returns, later app death is a step
    ///     failure (exit 1).
    /// </summary>
    public static StoryCADDriver Launch(DriverOptions options)
    {
        NativeMethods.EnsureDpiAwareness();

        var appPath = Path.GetFullPath(options.AppPath);
        if (!File.Exists(appPath))
        {
            throw new AutomationLaunchException(
                $"StoryCAD executable not found: {appPath}. Build the WinAppSDK head first.");
        }

        var sourceDir = Path.GetDirectoryName(appPath)!;
        if (File.Exists(Path.Combine(sourceDir, ".env")))
        {
            // Backend isolation: with .env present the app can register users and post
            // telemetry; automated runs must never do either (design, Runner section).
            throw new AutomationLaunchException(
                $".env present in {sourceDir} - refusing to launch. Automated runs must not reach the backend; " +
                "move .env away for the run and restore it after.");
        }

        var processName = Path.GetFileNameWithoutExtension(appPath);
        var running = Process.GetProcessesByName(processName);
        try
        {
            if (running.Length > 0)
            {
                // Single-instance app: a leftover instance swallows the new launch via
                // AppInstance redirection (StoryCAD/App.xaml.cs) and our child exits 0.
                throw new AutomationLaunchException(
                    $"{processName} is already running (PID {string.Join(", ", running.Select(p => p.Id))}); " +
                    "the app is single-instance, close it first.");
            }
        }
        finally
        {
            foreach (var p in running)
            {
                p.Dispose();
            }
        }

        var display = DisplayFacts.ReadPrimary();
        VerifyDisplay(display, options);

        var scratch = ScratchArea.Create(options.ScratchRoot);
        try
        {
            // Version must match StoryCADLib's assembly version (AppState.Version) or the
            // changelog dialog opens and blocks the file-open menu.
            var libPath = Path.Combine(sourceDir, "StoryCADLib.dll");
            if (!File.Exists(libPath))
            {
                throw new AutomationLaunchException(
                    $"StoryCADLib.dll not found beside the exe ({libPath}); is {appPath} really the WinAppSDK head output?");
            }

            var libVersion = AssemblyName.GetAssemblyName(libPath).Version?.ToString()
                             ?? throw new AutomationLaunchException($"StoryCADLib.dll at {libPath} has no assembly version.");
            scratch.SeedPreferences(libVersion);

            var startInfo = new ProcessStartInfo
            {
                FileName = appPath,
                WorkingDirectory = sourceDir,
                UseShellExecute = false,
            };
            foreach (var name in ScrubbedEnvironmentVariables)
            {
                startInfo.Environment.Remove(name);
            }

            // The app reads Preferences.json and writes logs under this folder instead of its
            // exe folder, so the developer's own bin preferences are never touched. Name must
            // match AppState.RootDirectoryOverrideVariable (StoryCADLib/Models/AppState.cs).
            startInfo.Environment[RootDirectoryOverrideVariable] = scratch.AppDataDirectory;

            var job = new KillOnCloseJob();
            Process? process = null;
            UIA3Automation? automation = null;
            try
            {
                process = Process.Start(startInfo)
                          ?? throw new AutomationLaunchException($"Process.Start returned null for {startInfo.FileName}.");
                job.Assign(process);

                WaitForMainWindow(process, options.MainWindowTimeout);

                automation = new UIA3Automation();
                WaitForUiaWindow(automation, process, options.UiaWindowTimeout);

                return new StoryCADDriver(options, process, job, automation, scratch, display);
            }
            catch
            {
                automation?.Dispose();
                try
                {
                    if (process is { HasExited: false })
                    {
                        job.TerminateAll();
                    }
                }
                catch (Exception)
                {
                    // Job disposal below is the backstop kill.
                }

                job.Dispose();
                process?.Dispose();
                throw;
            }
        }
        catch (Exception ex)
        {
            scratch.Sweep();
            if (ex is AutomationException)
            {
                throw;
            }

            throw new AutomationLaunchException($"Launch failed: {ex.Message}", ex);
        }
    }

    private static void VerifyDisplay(DisplayFacts display, DriverOptions options)
    {
        if (display.WidthPixels < options.MinDesktopWidth || display.HeightPixels < options.MinDesktopHeight)
        {
            throw new AutomationLaunchException(
                $"Desktop is {display}, below the required minimum {options.MinDesktopWidth}x{options.MinDesktopHeight}; " +
                "refusing to run rather than click blind (design, CI hosting).");
        }

        if (options.RequiredDpiScalePercent is int required && display.ScalePercent != required)
        {
            throw new AutomationLaunchException(
                $"Desktop is {display}, but the run requires {required}% scale; " +
                "refusing to run rather than click blind (design, CI hosting).");
        }
    }

    private static void WaitForMainWindow(Process process, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (true)
        {
            Thread.Sleep(250);
            process.Refresh();
            if (process.HasExited)
            {
                throw new AutomationLaunchException(
                    $"StoryCAD exited with code {process.ExitCode} before showing a window; launch never succeeded. " +
                    "A startup crash or a single-instance redirect to a leftover process are the usual causes.");
            }

            if (process.MainWindowHandle != IntPtr.Zero)
            {
                return;
            }

            if (DateTime.UtcNow >= deadline)
            {
                throw new AutomationLaunchException(
                    $"Timed out after {timeout.TotalSeconds:0}s waiting for the StoryCAD main window.");
            }
        }
    }

    private static void WaitForUiaWindow(UIA3Automation automation, Process process, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (true)
        {
            try
            {
                if (automation.GetDesktop().FindAllChildren(cf => cf.ByProcessId(process.Id)).Length > 0)
                {
                    return;
                }
            }
            catch (Exception)
            {
                // Desktop enumeration hiccup; retry until the deadline.
            }

            if (DateTime.UtcNow >= deadline)
            {
                throw new AutomationLaunchException(
                    "StoryCAD has a native window but it never appeared in the UIA tree.");
            }

            Thread.Sleep(250);
        }
    }

    // --- waits and state --------------------------------------------------------------------

    /// <inheritdoc />
    public ElementState WaitUntilReady(ElementAddress target, ReadinessRequirement need, TimeSpan? timeout = null)
        => ElementLocator.Snapshot(_locator.WaitUntilReady(target, need, Timeout(timeout)));

    /// <inheritdoc />
    public ElementState WaitUntilFound(ElementAddress target, TimeSpan? timeout = null)
        => ElementLocator.Snapshot(_locator.WaitUntilReady(target, ReadinessRequirement.Exists, Timeout(timeout)));

    /// <inheritdoc />
    public void VerifyNeverAppears(ElementAddress target, TimeSpan? settle = null)
        => _locator.VerifyAbsent(target, settle ?? _options.AbsenceSettle);

    /// <inheritdoc />
    public bool WaitForExit(TimeSpan timeout)
    {
        try
        {
            return _process.WaitForExit((int)timeout.TotalMilliseconds);
        }
        catch (Exception)
        {
            return true; // already dead or disposed
        }
    }

    /// <inheritdoc />
    public void WaitForWindowTitle(string title, TimeSpan? timeout = null)
        => _locator.WaitForWindow(title, Timeout(timeout));

    // --- pattern realizations ------------------------------------------------------------

    /// <inheritdoc />
    public ClickStrategy ResolveClickStrategy(ElementAddress target, TimeSpan? timeout = null)
        => ElementLocator.RecommendClickStrategy(
            _locator.WaitUntilReady(target, ReadinessRequirement.Interactive, Timeout(timeout)));

    /// <inheritdoc />
    public void Invoke(ElementAddress target, TimeSpan? timeout = null)
    {
        var element = _locator.WaitUntilReady(target, ReadinessRequirement.Invoke, Timeout(timeout));
        element.Patterns.Invoke.Pattern.Invoke();
        Wait.UntilInputIsProcessed();
    }

    /// <inheritdoc />
    public void Select(ElementAddress target, TimeSpan? timeout = null)
    {
        var element = _locator.WaitUntilReady(target, ReadinessRequirement.SelectionItem, Timeout(timeout));
        element.Patterns.SelectionItem.Pattern.Select();
        Wait.UntilInputIsProcessed();
    }

    /// <inheritdoc />
    public void SelectItem(ElementAddress container, string itemName, TimeSpan? timeout = null)
    {
        var t = Timeout(timeout);
        var containerElement = _locator.WaitUntilReady(container, ReadinessRequirement.Interactive, t);

        // A ComboBox realizes its items only while its popup is open; expand first. The popup
        // may be a separate top-level window, so the item search is scoped to the container's
        // descendants plus windows that appeared after the expand, never the whole process
        // (a same-named SelectionItem elsewhere must not be picked up; batch-2 review SF-2).
        var windowsBefore = _locator.TopLevelWindows();
        var expanded = false;
        try
        {
            if (containerElement.Patterns.ExpandCollapse.IsSupported)
            {
                containerElement.Patterns.ExpandCollapse.Pattern.Expand();
                expanded = true;
                Wait.UntilInputIsProcessed();
            }
        }
        catch (Exception)
        {
            // Non-expandable containers (ListView) land here; their items are already realized.
        }

        try
        {
            var item = _locator.WaitUntilReadyWithin(
                containerElement, windowsBefore, ElementAddress.FromName(itemName),
                ReadinessRequirement.SelectionItem, t);
            item.Patterns.SelectionItem.Pattern.Select();
            Wait.UntilInputIsProcessed();
        }
        catch (Exception)
        {
            // The failure path must not leak an open popup into the next step.
            if (expanded)
            {
                try
                {
                    containerElement.Patterns.ExpandCollapse.Pattern.Collapse();
                    Wait.UntilInputIsProcessed();
                }
                catch (Exception)
                {
                    // Popup may already be gone; the original failure is the one to surface.
                }
            }

            throw;
        }
    }

    /// <inheritdoc />
    public void Toggle(ElementAddress target, bool on, TimeSpan? timeout = null)
    {
        var element = _locator.WaitUntilReady(target, ReadinessRequirement.Toggle, Timeout(timeout));
        var pattern = element.Patterns.Toggle.Pattern;
        // Cycle at most twice: Off -> On -> Off covers the Indeterminate third state.
        for (var i = 0; i < 2; i++)
        {
            var isOn = pattern.ToggleState.ValueOrDefault == FlaUI.Core.Definitions.ToggleState.On;
            if (isOn == on)
            {
                return;
            }

            pattern.Toggle();
            Wait.UntilInputIsProcessed();
        }

        var finalOn = pattern.ToggleState.ValueOrDefault == FlaUI.Core.Definitions.ToggleState.On;
        if (finalOn != on)
        {
            throw new AutomationStepException($"{target} would not settle on toggle state {(on ? "on" : "off")}.");
        }
    }

    /// <inheritdoc />
    public void Expand(ElementAddress target, TimeSpan? timeout = null)
    {
        var element = _locator.WaitUntilReady(target, ReadinessRequirement.ExpandCollapse, Timeout(timeout));
        if (element.Patterns.ExpandCollapse.Pattern.ExpandCollapseState.ValueOrDefault
            != FlaUI.Core.Definitions.ExpandCollapseState.Expanded)
        {
            element.Patterns.ExpandCollapse.Pattern.Expand();
            Wait.UntilInputIsProcessed();
        }
    }

    /// <inheritdoc />
    public void Collapse(ElementAddress target, TimeSpan? timeout = null)
    {
        var element = _locator.WaitUntilReady(target, ReadinessRequirement.ExpandCollapse, Timeout(timeout));
        if (element.Patterns.ExpandCollapse.Pattern.ExpandCollapseState.ValueOrDefault
            != FlaUI.Core.Definitions.ExpandCollapseState.Collapsed)
        {
            element.Patterns.ExpandCollapse.Pattern.Collapse();
            Wait.UntilInputIsProcessed();
        }
    }

    // --- pointer realizations ---------------------------------------------------------------

    /// <inheritdoc />
    public void ClickPointer(ElementAddress target, TimeSpan? timeout = null)
    {
        var point = ReadyClickPoint(target, timeout);
        EnsureAppForeground($"click {target}");
        EasedMoveTo(point);
        Mouse.Click(MouseButton.Left);
        Wait.UntilInputIsProcessed();
    }

    /// <inheritdoc />
    public void DoubleClickPointer(ElementAddress target, TimeSpan? timeout = null)
    {
        var point = ReadyClickPoint(target, timeout);
        EnsureAppForeground($"double-click {target}");
        EasedMoveTo(point);
        Mouse.DoubleClick(MouseButton.Left);
        Wait.UntilInputIsProcessed();
    }

    /// <inheritdoc />
    public void RightClickPointer(ElementAddress target, TimeSpan? timeout = null)
    {
        // Right-click is always real pointer: context menus need it (design, Pointer verbs).
        var point = ReadyClickPoint(target, timeout);
        EnsureAppForeground($"right-click {target}");
        EasedMoveTo(point);
        Mouse.Click(MouseButton.Right);
        Wait.UntilInputIsProcessed();
    }

    /// <inheritdoc />
    public void Drag(ElementAddress from, ElementAddress to, TimeSpan? timeout = null)
    {
        // Resolve both ends before pressing so a missing drop target never leaves the mouse
        // button held down.
        var source = ReadyClickPoint(from, timeout);
        var destination = ReadyClickPoint(to, timeout);
        EnsureAppForeground($"drag {from} to {to}");
        EasedMoveTo(source);
        Mouse.Down(MouseButton.Left);
        try
        {
            Thread.Sleep(150); // let the press register before movement starts
            EasedMoveTo(destination);
            Thread.Sleep(150); // hover so drop-target logic sees the position
        }
        finally
        {
            Mouse.Up(MouseButton.Left);
        }

        Wait.UntilInputIsProcessed();
    }

    // --- keyboard and focus ------------------------------------------------------------------

    /// <inheritdoc />
    public void TypeText(string text)
    {
        // Keyboard input lands on whatever has focus, so it gets the same containment gate
        // as real pointer input.
        EnsureAppForeground("type");
        Keyboard.Type(text);
        Wait.UntilInputIsProcessed();
    }

    /// <inheritdoc />
    public void PressChord(string chord)
    {
        var (modifiers, key) = KeyChord.Parse(chord);
        EnsureAppForeground($"press \"{chord}\"");
        var pressed = new List<FlaUI.Core.WindowsAPI.VirtualKeyShort>();
        try
        {
            foreach (var modifier in modifiers)
            {
                Keyboard.Press(modifier);
                pressed.Add(modifier);
            }

            Keyboard.Press(key);
            pressed.Add(key);
        }
        finally
        {
            // Release in reverse order even on failure: a stuck Ctrl would corrupt every
            // subsequent input on the operator's desktop.
            for (var i = pressed.Count - 1; i >= 0; i--)
            {
                Keyboard.Release(pressed[i]);
            }
        }

        Wait.UntilInputIsProcessed();
    }

    /// <inheritdoc />
    public void Focus(ElementAddress target, TimeSpan? timeout = null)
    {
        var element = _locator.WaitUntilReady(target, ReadinessRequirement.Focusable, Timeout(timeout));
        element.Focus();
        Wait.UntilInputIsProcessed();
    }

    // --- content helpers ---------------------------------------------------------------------

    /// <inheritdoc />
    public void SetText(ElementAddress target, string value, TimeSpan? timeout = null)
    {
        // WaitReadyRevealing expands the enclosing collapsed Expander first: collapsed
        // Expanders keep RichEdit content out of the UIA tree (#1420 expand-before-type).
        var element = _locator.WaitReadyRevealing(target, ReadinessRequirement.SetValue, Timeout(timeout));
        try
        {
            element.Focus(); // mimic a user edit so focus-driven bindings fire on later focus moves
        }
        catch (Exception)
        {
            // Focus is best-effort; SetValue below is the actual write.
        }

        element.Patterns.Value.Pattern.SetValue(value);
        Wait.UntilInputIsProcessed();
    }

    /// <inheritdoc />
    public void ActivateTreeRow(string treePath, TimeSpan? timeout = null)
    {
        // Invoke, never SelectionItem: UIA selection on a tree row reports success without
        // navigating; only Invoke or a real click navigates (2026-06-12 finding). The root
        // outline row exposes only ExpandCollapse (#1420), so navigating to it needs the
        // pointer realization instead of this helper.
        Invoke(ElementAddress.FromTreePath(treePath), timeout);
    }

    /// <inheritdoc />
    public void InvokeMenuItem(ElementAddress leaf, TimeSpan? timeout = null)
    {
        var element = _locator.WaitMenuItemReady(leaf, ReadinessRequirement.Invoke, Timeout(timeout));
        element.Patterns.Invoke.Pattern.Invoke();
        Wait.UntilInputIsProcessed();
    }

    /// <inheritdoc />
    public void CompleteSaveFileDialog(string path, TimeSpan? timeout = null)
        => CompleteFileDialog(SaveDialogFileNameId, path, timeout);

    /// <inheritdoc />
    public void CompleteOpenFileDialog(string path, TimeSpan? timeout = null)
        => CompleteFileDialog(OpenDialogFileNameId, path, timeout);

    /// <summary>
    ///     Drives the Win32 common item dialog. Checked live on Brigid, 2026-10-04 (#1421 review
    ///     M1): the dialog is a "#32770" window owned by PickerHost.exe and nested under
    ///     StoryCAD's main window in the UIA tree. Its file-name box is an Edit with id "1001"
    ///     (Save) or "1148" (Open), and its OK button is id "1", which UIA reports as a Pane, so
    ///     it is pressed through its default action. Every search is scoped to the dialog: a
    ///     process-wide search for "1" matched other UI. No overwrite-confirm handling: dialog
    ///     paths are {scratch}-rooted and scratch is fresh per run.
    /// </summary>
    private void CompleteFileDialog(string fileNameId, string path, TimeSpan? timeout)
    {
        var t = Timeout(timeout);
        var dialog = WaitFor(t, "a file dialog to open", FindFileDialog);
        var fileName = WaitFor(t, $"the file-name box (Edit {fileNameId}) in the file dialog",
            () => dialog.FindFirstDescendant(cf =>
                cf.ByControlType(ControlType.Edit).And(cf.ByAutomationId(fileNameId))));
        fileName.Patterns.Value.Pattern.SetValue(path);
        Wait.UntilInputIsProcessed();

        var confirm = WaitFor(t, "the OK button (id 1) in the file dialog",
            () => dialog.FindFirstChild(cf => cf.ByAutomationId("1")));
        if (confirm.Patterns.Invoke.IsSupported)
        {
            confirm.Patterns.Invoke.Pattern.Invoke();
        }
        else
        {
            confirm.Patterns.LegacyIAccessible.Pattern.DoDefaultAction();
        }

        Wait.UntilInputIsProcessed();
        var closing = Stopwatch.StartNew();
        while (FindFileDialog() is not null)
        {
            if (closing.Elapsed > t)
            {
                throw new AutomationStepException(
                    $"the file dialog was still open {t.TotalSeconds:0}s after confirming '{path}'.");
            }

            Thread.Sleep(200);
        }
    }

    private AutomationElement? FindFileDialog()
    {
        foreach (var window in _automation.GetDesktop().FindAllChildren(cf => cf.ByProcessId(_process.Id)))
        {
            var dialog = window.FindFirstDescendant(cf => cf.ByClassName("#32770"));
            if (dialog is not null)
            {
                return dialog;
            }
        }

        return null;
    }

    private static AutomationElement WaitFor(TimeSpan timeout, string what, Func<AutomationElement?> find)
    {
        var waiting = Stopwatch.StartNew();
        while (true)
        {
            if (find() is { } found)
            {
                return found;
            }

            if (waiting.Elapsed > timeout)
            {
                throw new AutomationStepException($"timed out after {timeout.TotalSeconds:0}s waiting for {what}.");
            }

            Thread.Sleep(200);
        }
    }

    // --- session --------------------------------------------------------------------------

    /// <inheritdoc />
    public void Teardown()
    {
        if (Interlocked.Exchange(ref _tornDown, 1) == 1)
        {
            return;
        }

        var notes = new List<string>();

        // A file dialog belongs to PickerHost.exe, not to the app, so killing the app's job leaves
        // it on the desktop, pointing at a scratch folder that is about to be deleted (seen on
        // Brigid, 2026-10-04). Stop its process first; the dialog must be found while the app
        // window that parents it still exists.
        try
        {
            if (!_process.HasExited && FindFileDialog() is { } dialog)
            {
                using var picker = Process.GetProcessById(dialog.Properties.ProcessId.Value);
                if (string.Equals(picker.ProcessName, "PickerHost", StringComparison.OrdinalIgnoreCase))
                {
                    picker.Kill();
                    notes.Add($"Closed a file dialog left open (PickerHost, process {picker.Id}).");
                }
            }
        }
        catch (Exception ex)
        {
            notes.Add($"Closing a leftover file dialog failed: {ex.Message}");
        }

        try
        {
            if (!_process.HasExited)
            {
                _job.TerminateAll();
                _process.WaitForExit(5000);
            }
        }
        catch (Exception ex)
        {
            notes.Add($"Terminating the app failed: {ex.Message}");
        }

        try
        {
            _automation.Dispose();
        }
        catch (Exception ex)
        {
            notes.Add($"UIA disposal failed: {ex.Message}");
        }

        try
        {
            _job.Dispose(); // kill-on-close backstop even if TerminateAll failed
        }
        catch (Exception ex)
        {
            notes.Add($"Job handle close failed: {ex.Message}");
        }

        try
        {
            _process.Dispose();
        }
        catch (Exception)
        {
            // Nothing left to release.
        }

        if (_options.SweepScratchOnTeardown)
        {
            notes.AddRange(_scratch.Sweep());
        }

        TeardownNotes = notes;
    }

    /// <inheritdoc />
    public void Dispose() => Teardown();

    // --- internals ----------------------------------------------------------------------

    private TimeSpan Timeout(TimeSpan? requested) => requested ?? _options.ReadinessTimeout;

    /// <summary>
    ///     Poll-loop guard: classifies mid-run app death as the step-failure class (exit 1),
    ///     never the launch class; the design's failure policy forbids treating it as a
    ///     retryable environment error.
    /// </summary>
    private void EnsureRunnable()
    {
        if (Volatile.Read(ref _tornDown) == 1)
        {
            throw new AutomationStepException("Driver already torn down; no further steps can run.");
        }

        if (HasExited)
        {
            throw new AutomationStepException("StoryCAD exited mid-run.", appExited: true);
        }
    }

    /// <summary>
    ///     Input containment (design, Components): real input lands on whatever owns the
    ///     pixels, so every real-pointer or keyboard action verifies a StoryCAD window is
    ///     foreground and fails the step otherwise. Deliberately does not steal foreground.
    /// </summary>
    private void EnsureAppForeground(string action)
    {
        var foregroundPid = NativeMethods.ForegroundWindowProcessId();
        if (foregroundPid != (uint)_process.Id)
        {
            throw new AutomationStepException(
                $"Refusing real input ({action}): the foreground window belongs to process {foregroundPid}, " +
                $"not StoryCAD ({_process.Id}).");
        }
    }

    private Point ReadyClickPoint(ElementAddress target, TimeSpan? timeout)
    {
        var element = _locator.WaitUntilReady(target, ReadinessRequirement.ClickablePoint, Timeout(timeout));
        try
        {
            if (element.TryGetClickablePoint(out var point))
            {
                return point;
            }

            var rect = element.BoundingRectangle;
            if (!rect.IsEmpty)
            {
                return new Point(rect.Left + rect.Width / 2, rect.Top + rect.Height / 2);
            }
        }
        catch (Exception)
        {
            // Fall through to the step failure below.
        }

        throw new AutomationStepException($"{target} is ready but has no clickable point.");
    }

    /// <summary>
    ///     Real-pointer moves are eased (smoothstep) rather than teleported: hover states and
    ///     drag logic in the app see a plausible pointer, and presentation recordings read as
    ///     human motion. Duration scales with distance, bounded to keep test mode fast.
    /// </summary>
    private static void EasedMoveTo(Point target)
    {
        var start = Mouse.Position;
        var distance = Math.Sqrt(Math.Pow(target.X - start.X, 2) + Math.Pow(target.Y - start.Y, 2));
        if (distance < 1)
        {
            Mouse.Position = target;
            return;
        }

        var durationMs = Math.Clamp((int)(distance / 3.0), 120, 450);
        const int stepMs = 8;
        var steps = Math.Max(durationMs / stepMs, 2);
        for (var i = 1; i <= steps; i++)
        {
            var t = (double)i / steps;
            var eased = t * t * (3 - 2 * t); // smoothstep
            Mouse.Position = new Point(
                (int)Math.Round(start.X + (target.X - start.X) * eased),
                (int)Math.Round(start.Y + (target.Y - start.Y) * eased));
            Thread.Sleep(stepMs);
        }

        Mouse.Position = target;
    }
}
