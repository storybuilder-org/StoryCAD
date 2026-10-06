namespace StoryCADXamlScan;

/// <summary>
///     A menu item's AutomationId and the ids clicked to reach it, outermost first: the
///     owning toolbar button, then any sub-menu items.
/// </summary>
public sealed record MenuItemPath(string RelativePath, string Id, IReadOnlyList<string> Openers);
