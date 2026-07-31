namespace CollaboratorLib.Context;

/// <summary>
/// How much problem detail to include for a character-target context slice (issue #107).
/// </summary>
public enum CharacterProblemDetail
{
    /// <summary>No per-character problem section.</summary>
    None = 0,

    /// <summary>Problem names and roles only.</summary>
    LinksOnly = 1,

    /// <summary>Links plus GMC fields and opposing force name when available.</summary>
    LinksAndGmc = 2
}

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
    /// Include outline-level cast–problem map and spine gap lines (issue #107).
    /// Default on so most runs see plot-spine honesty; Premise turns this off.
    /// </summary>
    public bool IncludeCastProblemMap { get; init; } = true;

    /// <summary>
    /// Per-character problem slice when the target element is a Character.
    /// </summary>
    public CharacterProblemDetail CharacterProblemDetail { get; init; } = CharacterProblemDetail.None;

    /// <summary>
    /// Default spec with story constraints and cast–problem map (no beats / character GMC slice).
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
        IncludeCastProblemMap = true,
        CharacterProblemDetail = CharacterProblemDetail.None
    };
}
