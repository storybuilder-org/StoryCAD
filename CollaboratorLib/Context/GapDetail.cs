using StoryCADLib.Models;

namespace CollaboratorLib.Context;

/// <summary>
/// One outline element with at least one required-field gap (issue #107 phase 6).
/// </summary>
public sealed class GapDetail
{
    public required Guid ElementGuid { get; init; }
    public required string ElementName { get; init; }
    public required StoryItemType ElementType { get; init; }

    /// <summary>Missing required property names (model names, e.g. ProtGoal).</summary>
    public required IReadOnlyList<string> MissingProperties { get; init; }

    /// <summary>Display labels for missing properties (e.g. Character Sketch).</summary>
    public required IReadOnlyList<string> MissingPropertyLabels { get; init; }

    /// <summary>
    /// Distinct Collaborator workflow labels that help fill these properties
    /// (e.g. ProblemBuilder, StoryFunction). Empty if only host-element edit applies.
    /// </summary>
    public required IReadOnlyList<string> HelperWorkflowLabels { get; init; }
}
