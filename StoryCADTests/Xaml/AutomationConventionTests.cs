using System.Xml.Linq;
using static StoryCADXamlScan.AutomationXamlScan;

namespace StoryCADTests.Xaml;

/// <summary>
///     Static XAML-scanning fitness function for issue #1420 (AutomationProperties annotation pass).
///     Parses the convention-scope XAML files as plain XML (no UI, no [UITestMethod]) and checks
///     them against devdocs/automation_naming_convention.md. See devdocs/issue_1420_implementation_plan.md
///     for the TDD cycle these tests drive.
///     As of Unit 9, all 39 convention-scope files are fully annotated: the batch-by-batch
///     <c>AnnotatedFiles</c> allowlist that used to gate
///     <see cref="Coverage_AllScopeFiles_EveryInteractiveControlHasAutomationId"/> is retired, and that
///     test now scans every file the scan finds, making it (together with the other
///     five tests below, which already scanned all scope files from day one) the permanent fitness
///     function the approved design requires. New XAML dropped into a scope directory is covered
///     automatically; there is nothing left to add to a list.
///     The scan machinery (scope directories, element tables, file enumeration, XAML loading)
///     lives in the StoryCADXamlScan library since issue #1421, shared with the StoryCADAutomation
///     script linter so the two cannot drift.
/// </summary>
[TestClass]
public class AutomationConventionTests
{
    /// <summary>
    ///     Every interactive element outside a DataTemplate, in every file under the convention
    ///     scope directories, must carry a non-empty AutomationId.
    ///     This is the permanent fitness function required by the approved design: Unit 9 retired
    ///     the growing <c>AnnotatedFiles</c> allowlist once all 39 scope files were annotated
    ///     (devdocs/issue_1420_implementation_plan.md, "TDD structure").
    /// </summary>
    [TestMethod]
    public void Coverage_AllScopeFiles_EveryInteractiveControlHasAutomationId()
    {
        var violations = new List<string>();

        foreach (var relPath in ScopeFiles())
        {
            var doc = LoadXaml(relPath);
            foreach (var element in doc.Descendants())
            {
                if (!InteractiveElementNames.Contains(element.Name.LocalName))
                {
                    continue;
                }

                if (IsInsideDataTemplate(element))
                {
                    continue;
                }

                var id = GetAttributeValue(element, AutomationIdAttribute);
                if (string.IsNullOrEmpty(id))
                {
                    violations.Add($"{relPath}:{LineOf(element)} <{element.Name.LocalName}> is missing AutomationProperties.AutomationId");
                }
            }
        }

        Assert.IsTrue(violations.Count == 0,
            $"{violations.Count} interactive control(s) missing AutomationId:\n{string.Join("\n", violations)}");
    }

    /// <summary>No element with a DataTemplate ancestor may carry an AutomationId, in any scope file.</summary>
    [TestMethod]
    public void TemplateSafety_AllFiles_NoAutomationIdInsideDataTemplate()
    {
        var violations = new List<string>();

        foreach (var relPath in ScopeFiles())
        {
            var doc = LoadXaml(relPath);
            foreach (var element in doc.Descendants())
            {
                var id = GetAttributeValue(element, AutomationIdAttribute);
                if (string.IsNullOrEmpty(id))
                {
                    continue;
                }

                if (IsInsideDataTemplate(element))
                {
                    violations.Add($"{relPath}:{LineOf(element)} <{element.Name.LocalName}> AutomationId=\"{id}\" is inside a DataTemplate");
                }
            }
        }

        Assert.IsTrue(violations.Count == 0,
            $"{violations.Count} AutomationId(s) found inside a DataTemplate:\n{string.Join("\n", violations)}");
    }

    /// <summary>Every AutomationId, anywhere in scope, must end with the suffix mapped to its element type.</summary>
    [TestMethod]
    public void Suffix_AllFiles_AutomationIdEndsWithRoleSuffix()
    {
        var violations = new List<string>();

        foreach (var relPath in ScopeFiles())
        {
            var doc = LoadXaml(relPath);
            foreach (var element in doc.Descendants())
            {
                var id = GetAttributeValue(element, AutomationIdAttribute);
                if (string.IsNullOrEmpty(id))
                {
                    continue;
                }

                var localName = element.Name.LocalName;
                if (!SuffixByElementName.TryGetValue(localName, out var suffix))
                {
                    violations.Add($"{relPath}:{LineOf(element)} <{localName}> AutomationId=\"{id}\" has no suffix mapping in the convention table (propose one)");
                    continue;
                }

                if (!id.EndsWith(suffix, StringComparison.Ordinal))
                {
                    violations.Add($"{relPath}:{LineOf(element)} <{localName}> AutomationId=\"{id}\" does not end with required suffix \"{suffix}\"");
                }
            }
        }

        Assert.IsTrue(violations.Count == 0,
            $"{violations.Count} AutomationId(s) with wrong or missing suffix:\n{string.Join("\n", violations)}");
    }

    /// <summary>
    ///     Every AutomationId value, anywhere in scope, must be globally unique. Consumes the
    ///     library's occurrence enumeration (the same one the script linter's bare-id validation
    ///     uses), so this test also keeps that shared surface exercised.
    /// </summary>
    [TestMethod]
    public void Uniqueness_AllFiles_AutomationIdsGloballyUnique()
    {
        var duplicates = AutomationIdOccurrences()
            .GroupBy(o => o.Id, StringComparer.Ordinal)
            .Where(g => g.Count() > 1)
            .ToList();

        var violations = duplicates
            .Select(g =>
                $"\"{g.Key}\" used {g.Count()} times: {string.Join(", ", g.Select(o => $"{o.RelativePath}:{o.Line} <{o.ElementName}>"))}")
            .ToList();

        Assert.IsTrue(violations.Count == 0,
            $"{violations.Count} duplicate AutomationId value(s):\n{string.Join("\n", violations)}");
    }

    /// <summary>
    ///     Every AutomationId value, anywhere in scope, must be a non-empty literal ASCII
    ///     letters/digits string (no bindings). A present-but-empty attribute is a violation,
    ///     not a skip.
    /// </summary>
    [TestMethod]
    public void Literalness_AllFiles_AutomationIdIsLiteral()
    {
        var violations = new List<string>();

        foreach (var relPath in ScopeFiles())
        {
            var doc = LoadXaml(relPath);
            foreach (var element in doc.Descendants())
            {
                var id = GetAttributeValue(element, AutomationIdAttribute);
                if (id == null)
                {
                    continue; // attribute absent; coverage test owns that case
                }

                if (id.Length == 0)
                {
                    violations.Add($"{relPath}:{LineOf(element)} <{element.Name.LocalName}> has an empty AutomationProperties.AutomationId");
                    continue;
                }

                if (id.Contains('{') || !id.All(c => c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9'))
                {
                    violations.Add($"{relPath}:{LineOf(element)} <{element.Name.LocalName}> AutomationId=\"{id}\" is not a literal ASCII letters/digits string");
                }
            }
        }

        Assert.IsTrue(violations.Count == 0,
            $"{violations.Count} non-literal or empty AutomationId value(s):\n{string.Join("\n", violations)}");
    }

    /// <summary>
    ///     AutomationProperties attributes must be unconditional (ADR-001): no win:/skia:/other
    ///     namespace prefix on any AutomationProperties.* attribute in any scope file. A prefixed
    ///     attribute would give Windows and macOS different identifiers and slip past the
    ///     namespace-filtered lookup the scan's attribute getter uses.
    /// </summary>
    [TestMethod]
    public void Unconditional_AllFiles_NoNamespacedAutomationProperties()
    {
        var violations = new List<string>();

        foreach (var relPath in ScopeFiles())
        {
            var doc = LoadXaml(relPath);
            foreach (var element in doc.Descendants())
            {
                foreach (var attribute in element.Attributes())
                {
                    if (attribute.Name.LocalName.StartsWith("AutomationProperties.", StringComparison.Ordinal)
                        && attribute.Name.Namespace != XNamespace.None)
                    {
                        violations.Add($"{relPath}:{LineOf(element)} <{element.Name.LocalName}> {attribute.Name.LocalName} carries namespace \"{attribute.Name.NamespaceName}\" (must be unconditional)");
                    }
                }
            }
        }

        Assert.IsTrue(violations.Count == 0,
            $"{violations.Count} namespaced AutomationProperties attribute(s):\n{string.Join("\n", violations)}");
    }

    /// <summary>
    ///     No Style Setter may target an AutomationProperties.* property, in any scope file,
    ///     whatever the Value. The Windows Runtime does not evaluate bindings in Setter.Value
    ///     (Setter class docs; unoplatform/uno#4826), so a bound Name silently never gets set;
    ///     a literal Value would name every realized item identically. Both are always wrong:
    ///     bind Name inline on the template element instead.
    /// </summary>
    [TestMethod]
    public void SetterSafety_AllFiles_NoAutomationPropertiesInStyleSetters()
    {
        var violations = new List<string>();

        foreach (var relPath in ScopeFiles())
        {
            var doc = LoadXaml(relPath);
            foreach (var element in doc.Descendants())
            {
                if (element.Name.LocalName != "Setter")
                {
                    continue;
                }

                var property = element.Attributes()
                    .FirstOrDefault(a => a.Name.LocalName == "Property")
                    ?.Value;
                if (property != null && property.StartsWith("AutomationProperties.", StringComparison.Ordinal))
                {
                    violations.Add($"{relPath}:{LineOf(element)} <Setter Property=\"{property}\"> sets AutomationProperties via a Style Setter (bindings in Setter.Value are never evaluated; a literal names every item identically)");
                }
            }
        }

        Assert.IsTrue(violations.Count == 0,
            $"{violations.Count} AutomationProperties Style Setter(s):\n{string.Join("\n", violations)}");
    }
}
