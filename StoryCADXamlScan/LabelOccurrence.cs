namespace StoryCADXamlScan;

/// <summary>
///     One user-visible label attribute in a convention-scope XAML file: Text, Header, Label,
///     or AutomationProperties.Name. Consumed by the StoryCADAutomation script linter's
///     menu/tab text-path checks (devdocs/issue_1421_dsl_design.md "Script lint as a fitness
///     function"). RelativePath is repo-root-relative with forward slashes; Attribute is the
///     attribute's local name without any conditional prefix.
/// </summary>
public sealed record LabelOccurrence(string RelativePath, int Line, string ElementName, string Attribute, string Value);
