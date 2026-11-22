using StoryCADLib.Models;

namespace CollaboratorLib.Context;

/// <summary>
/// Collaborator #261: one missing piece on the Outline gaps page. A Problem with no Scene in
/// its beat tree, or a Character that no Problem seats as Protagonist or Antagonist.
/// </summary>
public sealed class MissingPiece
{
    public Guid ElementGuid { get; init; }
    public string ElementName { get; init; } = string.Empty;
    public StoryItemType ElementType { get; init; }

    /// <summary>The sentence the page shows after the element name.</summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>The workflow the line links to.</summary>
    public string WorkflowLabel { get; init; } = string.Empty;
}
