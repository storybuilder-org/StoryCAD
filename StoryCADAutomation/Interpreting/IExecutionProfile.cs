using StoryCADAutomation.Driver;

namespace StoryCADAutomation.Interpreting;

/// <summary>
///     The per-profile realization choices from the design's "Execution profiles" table
///     (devdocs/issue_1421_dsl_design.md): click realization, pause factor, assertion failure
///     handling, narrate rendering. Same script, two realizations; the profile is a runner
///     flag, never a script statement. <see cref="TestProfile" /> is the v1 implementation;
///     the presentation profile (issue #1421 task 7) implements this same interface — window
///     sizing and pacing flags stay runner concerns.
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

    /// <summary>Applies the profile pacing factor to a pause duration.</summary>
    TimeSpan ScalePause(double seconds);

    /// <summary>Renders a narrate statement (log line in test, caption overlay in presentation).</summary>
    void Narrate(string text, Action<string> log);
}
