namespace StoryCADAutomation.Interpreting;

/// <summary>
///     Execution knobs the runner wires from CLI flags (design, Runner). Defaults realize the
///     test profile with the design's abort-on-first-failure policy.
/// </summary>
public sealed class InterpreterOptions
{
    /// <summary>Profile realization; test by default, presentation with the runner's --profile flag.</summary>
    public IExecutionProfile Profile { get; init; } = new TestProfile();

    /// <summary>
    ///     Report failures but run the remaining steps (the --keep-going flag): a failure skips
    ///     the rest of its step. Session-fatal failures (app death, launch failure) abort regardless.
    /// </summary>
    public bool KeepGoing { get; init; }

    /// <summary>
    ///     How long close and expect-exit wait for the app process to end. Distinct from the
    ///     driver's element-readiness timeout: shutdown flushes autosave/backup state and the
    ///     job-object teardown is the backstop, not this wait.
    /// </summary>
    public TimeSpan ExitTimeout { get; init; } = TimeSpan.FromSeconds(15);

    /// <summary>
    ///     How long "wait &lt;target&gt; condense" waits: a Collaborator run takes from about 10 s to
    ///     about 4 minutes (#1626, Terry, 2026-10-24).
    /// </summary>
    public TimeSpan CondenseTimeout { get; init; } = TimeSpan.FromMinutes(5);

    /// <summary>
    ///     Menu item id to the ids clicked to reach it, from the XAML (runner wires
    ///     <c>XamlUiFacts.MenuOpeners</c>). An id missing here must already be showing in an open flyout.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> MenuOpeners { get; init; } =
        new Dictionary<string, IReadOnlyList<string>>();

    /// <summary>Folder the screenshot verb writes into (runner --out).</summary>
    public string OutputDirectory { get; init; } = "automation-output";

    /// <summary>Log sink for step lines, narrate output, and failures; the runner redirects it.</summary>
    public Action<string> Log { get; init; } = static line => Console.Out.WriteLine(line);

    /// <summary>
    ///     Called after every statement, while the driver is still live: the runner captures
    ///     failure diagnostics (screenshot, UIA subtree) and timeline entries here.
    /// </summary>
    public Action<StatementOutcome>? OnStatement { get; init; }
}
