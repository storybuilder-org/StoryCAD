namespace StoryCADAutomation.Interpreting;

/// <summary>
///     Execution knobs the runner wires from CLI flags (design, Runner). Defaults realize the
///     test profile with the design's abort-on-first-failure policy.
/// </summary>
public sealed class InterpreterOptions
{
    /// <summary>Profile realization; test by default, presentation arrives with #1421 task 7.</summary>
    public IExecutionProfile Profile { get; init; } = new TestProfile();

    /// <summary>
    ///     Report failures but run remaining steps (the --keep-going flag). Session-fatal
    ///     failures (app death, launch failure) abort regardless.
    /// </summary>
    public bool KeepGoing { get; init; }

    /// <summary>
    ///     How long close and expect-exit wait for the app process to end. Distinct from the
    ///     driver's element-readiness timeout: shutdown flushes autosave/backup state and the
    ///     job-object teardown is the backstop, not this wait.
    /// </summary>
    public TimeSpan ExitTimeout { get; init; } = TimeSpan.FromSeconds(15);

    /// <summary>Log sink for step lines, narrate output, and failures; the runner redirects it.</summary>
    public Action<string> Log { get; init; } = static line => Console.Out.WriteLine(line);

    /// <summary>
    ///     Called after every statement, while the driver is still live: the runner captures
    ///     failure diagnostics (screenshot, UIA subtree) and timeline entries here.
    /// </summary>
    public Action<StatementOutcome>? OnStatement { get; init; }
}
