using StoryCADAutomation.Driver;

namespace StoryCADAutomation.Interpreting;

/// <summary>
///     The per-profile realization choices from the design's "Execution profiles" table
///     (devdocs/issue_1421_dsl_design.md): click realization, pause factor, assertion failure
///     handling, narrate rendering, typing speed. Same script, two realizations; the profile is a runner
///     flag, never a script statement. <see cref="TestProfile" /> and
///     <see cref="PresentationProfile" /> implement it; window sizing stays a runner concern.
/// </summary>
public interface IExecutionProfile
{
    /// <summary>Profile name for logs and reports ("test", "presentation").</summary>
    string Name { get; }

    /// <summary>
    ///     Whether a failed expect verb logs and continues instead of aborting the script.
    ///     Session-fatal failures (app death, launch failure) abort regardless.
    /// </summary>
    bool ContinueOnAssertionFailure { get; }

    /// <summary>Realizes the click verb against the driver.</summary>
    void Click(IUiDriver driver, ElementAddress target);

    /// <summary>
    ///     Runs before a verb acts on a target: nothing in test, an eased cursor move in
    ///     presentation. One hook for every targeted verb, so verbs keep one realization
    ///     (#1421 review M7, settled 2026-10-05).
    /// </summary>
    void Approach(IUiDriver driver, ElementAddress target);

    /// <summary><see cref="Approach" /> for a target inside a named dialog window.</summary>
    void ApproachInWindow(IUiDriver driver, string windowTitle, ElementAddress target);

    /// <summary>Realizes the type verb: all at once in test, one character at a time in presentation.</summary>
    void Type(IUiDriver driver, string text);

    /// <summary>Realizes the set verb: Value pattern in test, visible typing in presentation.</summary>
    void Set(IUiDriver driver, ElementAddress target, string value);

    /// <summary>Applies the profile pacing factor to a pause duration.</summary>
    TimeSpan ScalePause(double seconds);

    /// <summary>Renders a narrate statement (log line in test, a subtitle cue in presentation).</summary>
    void Narrate(string text, Action<string> log);
}
