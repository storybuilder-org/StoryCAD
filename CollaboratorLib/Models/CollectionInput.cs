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

    /// <summary>
    /// Collaborator #217 rule 5: when set, the label of the gathered element whose sheet these
    /// candidates are for, and the collection offers only that Problem's free elements (on no
    /// sheet, not in the trash, not the target, the Story Problem, or an ancestor). Null offers
    /// every element of the type.
    /// </summary>
    public string? FreeElementsFor { get; init; }
}
