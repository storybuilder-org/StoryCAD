using StoryCADLib.Models;

namespace StoryCollaborator.Models;

/// <summary>
/// Declared request for many elements of one kind (issue #106).
/// Not inferred from output WriteVia modes.
/// </summary>
public sealed class CollectionInput
{
    /// <summary>Key on the request / placeholder name (e.g. CharacterChoices).</summary>
    public required string RequestName { get; init; }

    public StoryItemType ElementType { get; init; }

    public ElementProjection Projection { get; init; }
}
