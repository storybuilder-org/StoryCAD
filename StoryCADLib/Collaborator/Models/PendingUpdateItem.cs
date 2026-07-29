namespace StoryCADLib.Collaborator.Models;

/// <summary>
/// Display model for one pending workflow property update in the Collaborator panel (issue #116).
/// Mutable properties (not init-only) so WinUI XamlTypeInfo can generate setters.
/// </summary>
public sealed class PendingUpdateItem
{
    /// <summary>ElementLabel.PropertyName</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Proposed value (truncated for display).</summary>
    public string ProposedDisplay { get; set; } = string.Empty;

    /// <summary>Current outline value, or empty.</summary>
    public string CurrentDisplay { get; set; } = string.Empty;

    /// <summary>Fill, Refresh, or Protect (display label).</summary>
    public string KindLabel { get; set; } = string.Empty;

    /// <summary>True when Accept All will not apply this row without Review.</summary>
    public bool IsProtected { get; set; }

    /// <summary>Optional craft recommendation text when kind is Protect.</summary>
    public string CraftExplanation { get; set; }

    /// <summary>One-line list summary.</summary>
    public string SummaryLine { get; set; } = string.Empty;

    public bool HasCraftExplanation => !string.IsNullOrWhiteSpace(CraftExplanation);

    public bool HasCurrentValue => !string.IsNullOrWhiteSpace(CurrentDisplay);
}
