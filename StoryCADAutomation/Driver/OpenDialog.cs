namespace StoryCADAutomation.Driver;

/// <summary>
///     A ContentDialog that is open now. <see cref="Key" /> tells one dialog from the next even
///     when both hold the same control id (one Collaborator picker after another, #1422);
///     <see cref="Texts" /> are the names of the dialog's text elements in tree order, the
///     title first.
/// </summary>
public sealed record OpenDialog(string Key, IReadOnlyList<string> Texts);
