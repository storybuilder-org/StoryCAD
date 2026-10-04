namespace StoryCADAutomation.Scripting;

/// <summary>
///     Outcome of parsing one script. Statements and errors can both be non-empty: the parser
///     recovers per line, so a script with one bad line still yields every good statement and
///     the runner can report all errors at once instead of one per invocation.
/// </summary>
public sealed class ScriptParseResult
{
    /// <summary>The run name from the script "..." statement, or null when that line is missing or bad.</summary>
    public required string? ScriptName { get; init; }

    /// <summary>Parsed statements in file order, including the structural script/step statements.</summary>
    public required IReadOnlyList<ScriptStatement> Statements { get; init; }

    /// <summary>Parse errors with line numbers; empty means the script parsed clean.</summary>
    public required IReadOnlyList<ScriptDiagnostic> Errors { get; init; }

    /// <summary>True when the script parsed without errors and is safe to lint and execute.</summary>
    public bool Success => Errors.Count == 0;
}
