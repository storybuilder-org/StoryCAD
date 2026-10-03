namespace CollaboratorLib.Context;

/// <summary>
/// Specifies what context to gather for a workflow invocation.
/// Determined by ContextResolver based on workflow and element type.
/// </summary>
public record ContextSpec
{
    /// <summary>
    /// Include story constraints from Overview (Type, Genre, Premise).
    /// </summary>
    public bool IncludeStoryConstraints { get; init; } = true;

    /// <summary>
    /// Include beat sheet hierarchy showing problem structure and sequence.
    /// </summary>
    public bool IncludeBeatHierarchy { get; init; }

    /// <summary>
    /// Include character details for Protagonist/Antagonist references (problem-target runs).
    /// </summary>
    public bool IncludeCharacterContext { get; init; }

    /// <summary>
    /// Include preceding events from beat sheet for temporal awareness.
    /// </summary>
    public bool IncludePrecedingEvents { get; init; }

    /// <summary>
    /// Maximum number of preceding beats to include (token budget control).
    /// </summary>
    public int MaxPrecedingBeats { get; init; } = 3;

    /// <summary>
    /// Include outline-wide required-field gap GUIDs (issue #107).
    /// Default on for all runs; turn off only for explicit minimal prompts.
    /// </summary>
    public bool IncludeGaps { get; init; } = true;

    /// <summary>
    /// Default: story constraints + gap GUID list.
    /// </summary>
    public static ContextSpec Default => new();

    /// <summary>
    /// Full context spec for workflows needing complete awareness.
    /// </summary>
    public static ContextSpec Full => new()
    {
        IncludeStoryConstraints = true,
        IncludeBeatHierarchy = true,
        IncludeCharacterContext = true,
        IncludePrecedingEvents = true,
        MaxPrecedingBeats = 3,
        IncludeGaps = true
    };
}
