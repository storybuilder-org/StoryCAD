namespace StoryCADXamlScan;

/// <summary>
///     One AutomationProperties.AutomationId occurrence in a convention-scope XAML file.
///     RelativePath is repo-root-relative with forward slashes; Line is the element's
///     line in that file; ElementName is the element's local (namespace-ignored) name.
/// </summary>
public sealed record AutomationIdOccurrence(string RelativePath, int Line, string ElementName, string Id);
