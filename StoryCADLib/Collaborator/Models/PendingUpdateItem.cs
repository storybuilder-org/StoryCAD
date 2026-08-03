namespace StoryCADLib.Collaborator.Models;

/// <summary>
/// Display model for one pending workflow property update in the Collaborator panel (issue #116).
/// Mutable properties (not init-only) so WinUI XamlTypeInfo can generate setters.
/// </summary>
public sealed class PendingUpdateItem
{
    /// <summary>ElementLabel.PropertyName (raw key used by accept/skip callbacks).</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Element display name with spaces (e.g. "Outer Problem").</summary>
    public string ElementName { get; set; } = string.Empty;

    /// <summary>Property name with spaces (e.g. "Structure Title").</summary>
    public string PropertyDisplayName { get; set; } = string.Empty;

    /// <summary>Row title; falls back to the raw key when display fields are unset.</summary>
    public string DisplayName => string.IsNullOrEmpty(PropertyDisplayName) ? Key : PropertyDisplayName;

    /// <summary>Right-column caption: element plus kind (e.g. "Problem · New").</summary>
    public string ElementAndKind =>
        string.IsNullOrEmpty(ElementName) ? SummaryLine : $"{ElementName} · {SummaryLine}";

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
