namespace StoryCADXamlScan;

/// <summary>
///     One menu path's labels as Windows UIA reports them, outermost first: the owning button,
///     any sub-menu items, then the item. RelativePath and Line locate the item in the XAML.
/// </summary>
public sealed record MenuLabelPath(string RelativePath, int Line, IReadOnlyList<string> Labels);
