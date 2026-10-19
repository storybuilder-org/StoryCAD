using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace StoryCADTests.Collaborator;

/// <summary>
///     Source check for Collaborator #197. The XAML convention scan in
///     <see cref="StoryCADTests.Xaml.AutomationConventionTests"/> cannot see controls that C# code
///     creates, so this test reads the C# source of CollaboratorLib and StoryCADLib/Collaborator.
///     Rule: each <c>new</c> of an interactive control is assigned to a variable, and the same file
///     calls <c>SetAutomationId</c> with that variable as its first argument. A control that is not
///     assigned to a variable fails unless the exemption list names it.
/// </summary>
[TestClass]
public class CollaboratorAutomationIdCoverageTests
{
    private static readonly string[] ScopeDirectories = { "CollaboratorLib", "StoryCADLib/Collaborator" };

    // Target-typed `new()` is not matched; write the control type so this check can see it.
    private static readonly Regex NewInteractiveControl = new(
        @"\bnew\s+(?:Microsoft\.UI\.Xaml\.Controls\.(?:Primitives\.)?)?" +
        @"(Button|CheckBox|ComboBox|TextBox|NavigationViewItem|ToggleButton|HyperlinkButton|RadioButton|ToggleSwitch|AutoSuggestBox)" +
        @"\s*[({]");

    // `var x = new`, `Type x = new`, or `x = new` at the end of the text before the match.
    private static readonly Regex AssignedVariable = new(@"\b(\w+)\s*=\s*$");

    /// <summary>
    ///     File (repo-relative) plus text on the line of the <c>new</c>, mapped to the reason the
    ///     control needs no id.
    /// </summary>
    private static readonly Dictionary<(string File, string LineText), string> Exemptions = new()
    {
        [("StoryCADLib/Collaborator/ViewModels/WorkflowShellViewModel.cs", "Tag = \"Workflow\"")] =
            "LoadWorkflowMenuAsync placeholder: only unit tests call it; Collaborator.RebuildWorkflowMenu fills the live menu."
    };

    [TestMethod]
    public void CollaboratorSource_EveryInteractiveControlNew_HasSetAutomationIdCall()
    {
        var root = RepoRoot();
        var violations = new List<string>();
        var usedExemptions = new HashSet<(string, string)>();

        foreach (var file in SourceFiles(root))
        {
            var relPath = Path.GetRelativePath(root, file).Replace('\\', '/');
            var text = File.ReadAllText(file);

            foreach (Match match in NewInteractiveControl.Matches(text))
            {
                var lineStart = text.LastIndexOf('\n', match.Index) + 1;
                var lineEnd = text.IndexOf('\n', match.Index);
                var line = text[lineStart..(lineEnd < 0 ? text.Length : lineEnd)];
                var lineNumber = text.Take(match.Index).Count(c => c == '\n') + 1;

                var exemption = Exemptions.Keys.FirstOrDefault(
                    k => k.File == relPath && line.Contains(k.LineText, StringComparison.Ordinal));
                if (exemption != default)
                {
                    usedExemptions.Add(exemption);
                    continue;
                }

                var variable = AssignedVariable.Match(text[lineStart..match.Index]);
                if (!variable.Success)
                {
                    violations.Add($"{relPath}:{lineNumber} new {match.Groups[1].Value} is not assigned to a variable, so no SetAutomationId call can name it");
                    continue;
                }

                var name = variable.Groups[1].Value;
                var call = new Regex(@"\bSetAutomationId\(\s*" + Regex.Escape(name) + @"\s*,");
                if (!call.IsMatch(text))
                {
                    violations.Add($"{relPath}:{lineNumber} new {match.Groups[1].Value} '{name}' has no SetAutomationId({name}, ...) call in the file");
                }
            }
        }

        var staleExemptions = Exemptions.Keys.Where(k => !usedExemptions.Contains(k)).ToList();

        Assert.IsTrue(violations.Count == 0,
            $"{violations.Count} C#-built interactive control(s) missing AutomationId:\n{string.Join("\n", violations)}");
        Assert.IsTrue(staleExemptions.Count == 0,
            $"Exemptions that match no control; remove them:\n{string.Join("\n", staleExemptions)}");
    }

    private static IEnumerable<string> SourceFiles(string root)
    {
        foreach (var relDir in ScopeDirectories)
        {
            var dir = Path.Combine(root, relDir.Replace('/', Path.DirectorySeparatorChar));
            if (!Directory.Exists(dir))
                throw new DirectoryNotFoundException($"Scope directory not found: {dir}");

            foreach (var file in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
            {
                if (!file.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                    continue;

                var parts = Path.GetRelativePath(dir, file).Split(Path.DirectorySeparatorChar);
                if (parts.Any(p => p.Equals("obj", StringComparison.OrdinalIgnoreCase) ||
                                   p.Equals("bin", StringComparison.OrdinalIgnoreCase)))
                    continue;

                yield return file;
            }
        }
    }

    /// <summary>Walks up from the test assembly to the folder that holds StoryCAD.sln.</summary>
    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "StoryCAD.sln")))
            dir = dir.Parent;

        return dir?.FullName
               ?? throw new InvalidOperationException($"Could not locate StoryCAD.sln above {AppContext.BaseDirectory}");
    }
}
