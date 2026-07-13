using StoryCADAutomation.Scripting;

namespace StoryCADAutomation.Interpreting;

/// <summary>Per-statement execution result.</summary>
public enum StatementStatus
{
    Passed,
    Failed,

    /// <summary>Not executed: an earlier failure aborted the script, or the run was cancelled.</summary>
    Skipped,
}

/// <summary>
///     One executed (or skipped) statement, with the timing the runner needs for
///     timeline.json and the step name that maps it to a JUnit testcase. Failure carries the
///     original exception so the runner can classify exit codes via
///     <see cref="Driver.AutomationException.FailureClass" />.
/// </summary>
public sealed record StatementOutcome(
    ScriptStatement Statement,
    StatementStatus Status,
    string? StepName,
    DateTimeOffset StartedUtc,
    TimeSpan Duration,
    Exception? Failure);

/// <summary>Whole-script execution result: pure data, composed into reports by the runner.</summary>
public sealed class ScriptRunResult
{
    /// <summary>The run name from the script statement.</summary>
    public required string? ScriptName { get; init; }

    /// <summary>One outcome per statement, in script order.</summary>
    public required IReadOnlyList<StatementOutcome> Outcomes { get; init; }

    /// <summary>True when no statement failed (skipped statements do not count as failures).</summary>
    public bool AllPassed => Outcomes.All(o => o.Status != StatementStatus.Failed);
}
