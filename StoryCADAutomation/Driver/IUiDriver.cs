namespace StoryCADAutomation.Driver;

/// <summary>
///     The driver seam the interpreter programs against (devdocs/issue_1421_dsl_design.md
///     "macOS seam"): methods name intent, parameters are addresses and plain values, and no
///     UIA or FlaUI type appears in a signature. A future macOS backend implements this same
///     interface; scripts and interpreter don't change. Launching is backend-specific and
///     stays on the concrete driver (<see cref="StoryCADDriver.Launch" />).
/// </summary>
public interface IUiDriver : IDisposable
{
    /// <summary>Per-run scratch root; the future {scratch} script substitution resolves here.</summary>
    string ScratchDirectory { get; }

    /// <summary>Display facts asserted at launch, for run reports.</summary>
    DisplayFacts Display { get; }

    /// <summary>True once the app process has ended (crash, close, or teardown).</summary>
    bool HasExited { get; }

    /// <summary>Notes from the last teardown (e.g. scratch files left behind); empty until torn down.</summary>
    IReadOnlyList<string> TeardownNotes { get; }

    // --- waits and state ---------------------------------------------------------------

    /// <summary>Implicit-wait primitive: readiness, not existence. Returns a snapshot of the ready element.</summary>
    ElementState WaitUntilReady(ElementAddress target, ReadinessRequirement need, TimeSpan? timeout = null);

    /// <summary>Found-only wait, for assertions that check state without waiting for usability.</summary>
    ElementState WaitUntilFound(ElementAddress target, TimeSpan? timeout = null);

    /// <summary>Settle-then-check absence: fails if the target appears at any point in the settle window.</summary>
    void VerifyNeverAppears(ElementAddress target, TimeSpan? settle = null);

    /// <summary>Waits for app process exit (the expect-exit verb).</summary>
    bool WaitForExit(TimeSpan timeout);

    /// <summary>
    ///     Waits for the window titled <paramref name="windowTitle" />, then invokes
    ///     <paramref name="target" /> found inside that window only (the dialog verb).
    /// </summary>
    void InvokeInWindow(string windowTitle, ElementAddress target, TimeSpan? timeout = null);

    /// <summary>Saves an image of the app's main window as a PNG at <paramref name="pngPath" />.</summary>
    void CaptureMainWindow(string pngPath);

    /// <summary>Saves an image of the open ContentDialog only as a PNG at <paramref name="pngPath" />.</summary>
    void CaptureOpenDialog(string pngPath);

    /// <summary>The app's exit code once it has exited; null while it runs or when unreadable.</summary>
    int? ExitCode { get; }

    /// <summary>
    ///     Waits until a window with the exact title exists: a top-level window of the app
    ///     process, or an in-window dialog surfacing as a Window-typed element (ContentDialog).
    ///     Serves the wait-window, expect window, and dialog verbs.
    /// </summary>
    void WaitForWindowTitle(string title, TimeSpan? timeout = null);

    // --- pattern realizations ------------------------------------------------------------

    /// <summary>Which click realization suits this control; the per-control strategy hook.</summary>
    ClickStrategy ResolveClickStrategy(ElementAddress target, TimeSpan? timeout = null);

    /// <summary>Clicks via the UIA Invoke pattern.</summary>
    void Invoke(ElementAddress target, TimeSpan? timeout = null);

    /// <summary>Selects via the UIA SelectionItem pattern.</summary>
    void Select(ElementAddress target, TimeSpan? timeout = null);

    /// <summary>Selects an item by name inside a container (ComboBox/ListView selection verb).</summary>
    void SelectItem(ElementAddress container, string itemName, TimeSpan? timeout = null);

    /// <summary>Sets a toggleable control to the requested state.</summary>
    void Toggle(ElementAddress target, bool on, TimeSpan? timeout = null);

    /// <summary>Expands via the ExpandCollapse pattern; no-op when already expanded.</summary>
    void Expand(ElementAddress target, TimeSpan? timeout = null);

    /// <summary>Collapses via the ExpandCollapse pattern; no-op when already collapsed.</summary>
    void Collapse(ElementAddress target, TimeSpan? timeout = null);

    // --- pointer realizations ------------------------------------------------------------

    /// <summary>Real-pointer click: eased move, then click. Requires the app to be foreground.</summary>
    void ClickPointer(ElementAddress target, TimeSpan? timeout = null);

    /// <summary>Real-pointer double click.</summary>
    void DoubleClickPointer(ElementAddress target, TimeSpan? timeout = null);

    /// <summary>Real-pointer right click (context menus need real input; there is no pattern realization).</summary>
    void RightClickPointer(ElementAddress target, TimeSpan? timeout = null);

    /// <summary>Real-pointer drag: press on source, eased move, release on target.</summary>
    void Drag(ElementAddress from, ElementAddress to, TimeSpan? timeout = null);

    /// <summary>
    ///     Eased pointer move to the target, no click. The presentation profile calls it before
    ///     a verb acts, so a recording shows the cursor reach each control (#1421 Milestone 4).
    /// </summary>
    void GlideTo(ElementAddress target, TimeSpan? timeout = null);

    /// <summary><see cref="GlideTo" /> for a target inside the named dialog window.</summary>
    void GlideToInWindow(string windowTitle, ElementAddress target, TimeSpan? timeout = null);

    /// <summary>Restores the main window and sets its outer size in physical pixels (runner --window).</summary>
    void ResizeMainWindow(int width, int height);

    // --- keyboard and focus ----------------------------------------------------------------

    /// <summary>Types text into whatever has keyboard focus. Requires the app to be foreground.</summary>
    void TypeText(string text);

    /// <summary>Presses a logical chord such as "Primary+S" (Primary = Ctrl on this backend).</summary>
    void PressChord(string chord);

    /// <summary>Gives an element keyboard focus via UIA (not real input).</summary>
    void Focus(ElementAddress target, TimeSpan? timeout = null);

    // --- content helpers (the #1420 runtime facts baked in) --------------------------------

    /// <summary>
    ///     Writes a text control via the Value pattern, expanding the enclosing collapsed
    ///     Expander first (collapsed Expanders keep RichEdit content out of the UIA tree).
    /// </summary>
    void SetText(ElementAddress target, string value, TimeSpan? timeout = null);

    /// <summary>
    ///     Navigates by activating a tree row via Invoke, never SelectionItem: UIA selection
    ///     on a tree row reports success without navigating (2026-06-12 finding).
    /// </summary>
    void ActivateTreeRow(string treePath, TimeSpan? timeout = null);

    /// <summary>
    ///     Invokes a menu leaf, opening parent menus as needed: menu items exist in the UIA
    ///     tree only while their flyout is open, and the design assigns the parent-opening
    ///     choreography to the driver ("menu SaveStoryMenuItem — the driver opens parent
    ///     menus"). The text-path menu form is interpreter-side composition of Expand/Invoke
    ///     and does not need this.
    /// </summary>
    /// <param name="openers">
    ///     Ids to click first, owning button outermost (from the XAML). When given, the driver
    ///     opens exactly those menus; when null, it falls back to trying each menu in turn.
    /// </param>
    void InvokeMenuItem(ElementAddress leaf, IReadOnlyList<string>? openers = null, TimeSpan? timeout = null);

    /// <summary>
    ///     Drives an already-opening native save picker to completion (the save-file-dialog
    ///     verb; the driver owns the native-dialog choreography per the design's Dialogs
    ///     section). The path must be fully resolved; {scratch} substitution is the
    ///     interpreter's job.
    /// </summary>
    void CompleteSaveFileDialog(string path, TimeSpan? timeout = null);

    /// <inheritdoc cref="CompleteSaveFileDialog" />
    void CompleteOpenFileDialog(string path, TimeSpan? timeout = null);

    // --- session -----------------------------------------------------------------------

    /// <summary>
    ///     Cancel-safe teardown: callable from a Ctrl+C path, tolerant of an already-dead
    ///     process, never throws, sweeps scratch. Idempotent.
    /// </summary>
    void Teardown();
}
