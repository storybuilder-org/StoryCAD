namespace StoryCADAutomation.Driver;

/// <summary>
///     Failure classification per the design's exit-code contract (devdocs/issue_1421_dsl_design.md
///     "Runner" and "Failure policy"). The driver only classifies; mapping to process exit codes
///     belongs to the runner task.
/// </summary>
public enum FailureClass
{
    /// <summary>
    ///     A step could not be executed: readiness timeout, foreground lost, pattern refused,
    ///     or the app died mid-run. The runner maps this class to exit code 1. App death
    ///     mid-script is deliberately in this class, never a retryable environment error.
    /// </summary>
    StepFailure,

    /// <summary>
    ///     The environment refused the run or the app never reached a usable main window:
    ///     .env present, instance already running, display mismatch, launch crash. The runner
    ///     maps this class to exit code 3 (one whole-script retry permitted, per the design).
    /// </summary>
    LaunchFailure,
}

/// <summary>Base type for driver failures; carries the exit-code class the runner will map.</summary>
public abstract class AutomationException : Exception
{
    protected AutomationException(FailureClass failureClass, string message, Exception? inner = null)
        : base(message, inner)
    {
        FailureClass = failureClass;
    }

    /// <summary>Which exit-code class this failure belongs to.</summary>
    public FailureClass FailureClass { get; }
}

/// <summary>Environment or launch failure: the app never reached a usable state (exit-3 class).</summary>
public sealed class AutomationLaunchException : AutomationException
{
    public AutomationLaunchException(string message, Exception? inner = null)
        : base(FailureClass.LaunchFailure, message, inner)
    {
    }
}

/// <summary>A step failed after a successful launch (exit-1 class).</summary>
public sealed class AutomationStepException : AutomationException
{
    public AutomationStepException(string message, bool appExited = false, Exception? inner = null)
        : base(FailureClass.StepFailure, message, inner)
    {
        AppExited = appExited;
    }

    /// <summary>
    ///     True when the failure is the app process dying mid-run. Distinguished from
    ///     "launch never succeeded" so the runner can report it as a step failure with
    ///     full diagnostics rather than a retryable environment error.
    /// </summary>
    public bool AppExited { get; }
}
