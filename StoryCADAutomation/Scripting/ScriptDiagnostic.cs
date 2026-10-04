namespace StoryCADAutomation.Scripting;

/// <summary>
///     Finding severity. Errors fail `check` (the runner's exit 2); warnings are reported and
///     pass. The design's lint section fixes which rule carries which severity
///     (devdocs/issue_1421_dsl_design.md "Script lint as a fitness function").
/// </summary>
public enum DiagnosticSeverity
{
    Error,
    Warning,
}

/// <summary>One parse error or lint finding, tied to a script line.</summary>
public sealed record ScriptDiagnostic(DiagnosticSeverity Severity, int Line, string Message)
{
    /// <summary>Compiler-style rendering for logs and CI output.</summary>
    public override string ToString()
        => $"line {Line}: {(Severity == DiagnosticSeverity.Error ? "error" : "warning")}: {Message}";
}
