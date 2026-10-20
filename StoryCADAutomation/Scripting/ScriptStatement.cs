using StoryCADAutomation.Driver;

namespace StoryCADAutomation.Scripting;

/// <summary>
///     One parsed script line. Which of the optional slots are populated is fixed per verb by
///     the parser's grammar (see <see cref="ScriptParser" />); handlers read the slots their
///     verb guarantees and never re-parse. Immutable, UIA-free (the macOS-seam rule).
/// </summary>
public sealed record ScriptStatement
{
    /// <summary>The verb this line resolved to.</summary>
    public required ScriptVerb Verb { get; init; }

    /// <summary>1-based line number in the script file, for diagnostics and reports.</summary>
    public required int Line { get; init; }

    /// <summary>The raw line as written (trimmed), for reports and failure messages.</summary>
    public required string Source { get; init; }

    /// <summary>Primary target: click/set/expect subjects, the drag source, the dialog click target.</summary>
    public ElementAddress? Target { get; init; }

    /// <summary>Drag destination ("drag ... to ...").</summary>
    public ElementAddress? DragTarget { get; init; }

    /// <summary>
    ///     The string argument: script/step/narrate names, typed text, set/select values, chords,
    ///     window and dialog titles, file-dialog paths, menu/context-menu/tab text paths, and
    ///     expect text/tree-contains operands.
    /// </summary>
    public string? Text { get; init; }

    /// <summary>
    ///     Pause duration in seconds, before profile pacing is applied; on a step line, the
    ///     optional hold seconds added at the end of the step (video design, section 6).
    /// </summary>
    public double? Seconds { get; init; }

    /// <summary>Window action ("window minimize|maximize|restore|size"); null on other verbs.</summary>
    public WindowAction? WindowAction { get; init; }

    /// <summary>
    ///     Window size ("window size WxH"): the visible frame in 100%-scale pixels, the units of
    ///     --window.
    /// </summary>
    public System.Drawing.Size? WindowSize { get; init; }

    /// <summary>Toggle direction: true = on.</summary>
    public bool? On { get; init; }

    /// <summary>
    ///     Screenshot crop ("screenshot ... crop x,y,w,h"): a rectangle in 100%-scale pixels from
    ///     the visible window's top-left corner, the same units as --window.
    /// </summary>
    public System.Drawing.Rectangle? Crop { get; init; }

    /// <summary>
    ///     Screenshot elements ("screenshot ... of target [target ...]"): the capture is the union
    ///     of their on-screen rectangles, so a close-up follows the layout instead of fixed pixels.
    /// </summary>
    public IReadOnlyList<ElementAddress>? CaptureTargets { get; init; }

    /// <summary>
    ///     Screenshot margin ("... of ... margin N"): 100%-scale pixels added on every side of the
    ///     element union; 0 when the line has no margin.
    /// </summary>
    public int? CaptureMargin { get; init; }
}
