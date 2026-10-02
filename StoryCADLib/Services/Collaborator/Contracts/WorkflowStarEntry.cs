namespace StoryCADLib.Services.Collaborator.Contracts;

/// <summary>
///     One workflow as shown in the Customize workflows dialog. Collaborator owns the workflow
///     registry and StoryCADLib does not reference it, so Collaborator projects each workflow
///     into this contract for the dialog to render.
/// </summary>
public sealed class WorkflowStarEntry
{
    /// <summary>Registry label; what gets persisted when the workflow is starred.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Display title, as shown in the navigation pane.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>One-line description, shown under the title in the dialog.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Element-type group header, used to section the dialog list.</summary>
    public string GroupTitle { get; set; } = string.Empty;

    /// <summary>Whether the workflow is currently starred.</summary>
    public bool IsStarred { get; set; }
}
