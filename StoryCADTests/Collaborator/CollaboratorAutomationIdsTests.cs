using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCADLib.Collaborator;
using StoryCollaborator.Workflows;

namespace StoryCADTests.Collaborator;

/// <summary>
///     Tests of <see cref="CollaboratorAutomationIds"/> (Collaborator #197): the ids follow
///     devdocs/automation_naming_convention.md and do not collide.
/// </summary>
[TestClass]
public class CollaboratorAutomationIdsTests
{
    private static readonly Regex AsciiPascalCase = new("^[A-Z][A-Za-z0-9]*$");

    /// <summary>Every band title the pane can show: Starred plus each element-type group.</summary>
    private static IEnumerable<string> BandTitles() =>
        WorkflowMenuComposer.Compose(WorkflowRegistry.All, Array.Empty<string>(),
                StoryCollaborator.Collaborator.GroupTitle)
            .Select(b => b.Title)
            .Append(WorkflowMenuComposer.StarredBandTitle)
            .Distinct();

    private static IEnumerable<string> AllIds()
    {
        foreach (var workflow in WorkflowRegistry.All)
        {
            yield return CollaboratorAutomationIds.WorkflowNavItem(workflow.Label);
            yield return CollaboratorAutomationIds.WorkflowStarButton(workflow.Label);
            yield return CollaboratorAutomationIds.WorkflowCheck(workflow.Label);
        }

        foreach (var title in BandTitles())
            yield return CollaboratorAutomationIds.GroupNavItem(title);

        foreach (var id in CollaboratorAutomationIds.FixedIds)
            yield return id;
    }

    private static void AssertIdShape(string id, string suffix)
    {
        Assert.IsTrue(AsciiPascalCase.IsMatch(id), $"'{id}' is not ASCII PascalCase");
        Assert.IsTrue(id.StartsWith("Workflow", StringComparison.Ordinal), $"'{id}' lacks the Workflow prefix");
        Assert.IsTrue(id.EndsWith(suffix, StringComparison.Ordinal), $"'{id}' lacks the {suffix} suffix");
        Assert.IsTrue(id.Length > "Workflow".Length + suffix.Length, $"'{id}' has an empty middle part");
    }

    [TestMethod]
    public void WorkflowNavItem_EveryRegistryLabel_GivesAsciiPascalCaseNavItemId()
    {
        foreach (var workflow in WorkflowRegistry.All)
            AssertIdShape(CollaboratorAutomationIds.WorkflowNavItem(workflow.Label), "NavItem");
    }

    [TestMethod]
    public void WorkflowStarButton_EveryRegistryLabel_GivesAsciiPascalCaseButtonId()
    {
        foreach (var workflow in WorkflowRegistry.All)
            AssertIdShape(CollaboratorAutomationIds.WorkflowStarButton(workflow.Label), "StarButton");
    }

    [TestMethod]
    public void WorkflowCheck_EveryRegistryLabel_GivesAsciiPascalCaseCheckId()
    {
        foreach (var workflow in WorkflowRegistry.All)
            AssertIdShape(CollaboratorAutomationIds.WorkflowCheck(workflow.Label), "Check");
    }

    [TestMethod]
    public void WorkflowNavItem_PremiseLabel_ReturnsDocumentedId()
    {
        Assert.AreEqual("WorkflowPremiseNavItem", CollaboratorAutomationIds.WorkflowNavItem("Premise"));
    }

    [TestMethod]
    public void GroupNavItem_EveryBandTitle_GivesAsciiPascalCaseNavItemId()
    {
        foreach (var title in BandTitles())
            AssertIdShape(CollaboratorAutomationIds.GroupNavItem(title), "NavItem");
    }

    [TestMethod]
    public void GroupNavItem_TitleWithSpace_RemovesSpace()
    {
        Assert.AreEqual("WorkflowGroupStoryWorldNavItem", CollaboratorAutomationIds.GroupNavItem("Story World"));
    }

    [TestMethod]
    public void GroupNavItem_StarredBand_ReturnsDocumentedId()
    {
        Assert.AreEqual("WorkflowGroupStarredNavItem",
            CollaboratorAutomationIds.GroupNavItem(WorkflowMenuComposer.StarredBandTitle));
    }

    [TestMethod]
    public void FixedIds_All_AreAsciiPascalCaseWithRoleSuffix()
    {
        foreach (var id in CollaboratorAutomationIds.FixedIds)
        {
            var suffix = new[] { "NavItem", "Combo", "TextBox", "Check" }
                .FirstOrDefault(s => id.EndsWith(s, StringComparison.Ordinal));
            Assert.IsNotNull(suffix, $"'{id}' has no role suffix");
            AssertIdShape(id, suffix);
        }
    }

    [TestMethod]
    public void AllIds_RowsStarsChecksGroupsGapsSettings_AreUnique()
    {
        var duplicates = AllIds()
            .GroupBy(id => id, StringComparer.Ordinal)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        Assert.AreEqual(0, duplicates.Count, $"Duplicate ids: {string.Join(", ", duplicates)}");
    }

#if WINDOWS10_0_22621_0_OR_GREATER // the runner, and its lint, exist on this target only
    /// <summary>
    ///     The automation runner cannot reference StoryCADLib, so its lint repeats the id shapes.
    ///     Every id built here must pass that lint, or scripts cannot use it.
    /// </summary>
    [TestMethod]
    public void AllIds_InAutomationScript_PassLint()
    {
        var linter = new StoryCADAutomation.Scripting.ScriptLinter(new StoryCADAutomation.Scripting.XamlUiFacts
        {
            AutomationIds = new HashSet<string>(),
            MenuPaths = new HashSet<string>(),
            TabLabels = new HashSet<string>(),
        });
        var script = "script \"Ids\"\n" + string.Concat(AllIds().Select(id => $"expect {id} exists\n"));
        var parsed = StoryCADAutomation.Scripting.ScriptParser.Parse(script);
        Assert.IsTrue(parsed.Success, string.Join("; ", parsed.Errors));

        var errors = linter.Lint(parsed.Statements)
            .Where(d => d.Severity == StoryCADAutomation.Scripting.DiagnosticSeverity.Error)
            .ToList();
        Assert.AreEqual(0, errors.Count, string.Join("; ", errors));
    }
#endif
}
