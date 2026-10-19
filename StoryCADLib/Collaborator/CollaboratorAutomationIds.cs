using System.Text;

namespace StoryCADLib.Collaborator;

/// <summary>
///     AutomationIds for the Collaborator controls that are built in C# rather than XAML
///     (Collaborator #197). Rules follow devdocs/automation_naming_convention.md: literal
///     ASCII PascalCase with the role suffix. Ids come from fixed text only (a workflow label
///     or a band title), never a story title or a gap count, so a test script can find a
///     control when the visible text changes.
/// </summary>
public static class CollaboratorAutomationIds
{
    public const string OutlineGapsNavItem = "WorkflowOutlineGapsNavItem";

    public const string SettingsTersenessCombo = "WorkflowSettingsTersenessCombo";
    public const string SettingsGenreTextBox = "WorkflowSettingsGenreTextBox";
    public const string SettingsLikesTextBox = "WorkflowSettingsLikesTextBox";
    public const string SettingsDislikesTextBox = "WorkflowSettingsDislikesTextBox";
    public const string SettingsLoggingCombo = "WorkflowSettingsLoggingCombo";
    public const string SettingsShowCostCheck = "WorkflowSettingsShowCostCheck";

    /// <summary>Every fixed id above, for uniqueness checks.</summary>
    public static IReadOnlyList<string> FixedIds { get; } = new[]
    {
        OutlineGapsNavItem,
        SettingsTersenessCombo,
        SettingsGenreTextBox,
        SettingsLikesTextBox,
        SettingsDislikesTextBox,
        SettingsLoggingCombo,
        SettingsShowCostCheck
    };

    /// <summary>A workflow row in the pane, for example <c>WorkflowPremiseNavItem</c>.</summary>
    public static string WorkflowNavItem(string workflowLabel) =>
        $"Workflow{Identifier(workflowLabel)}NavItem";

    /// <summary>The star button on a workflow row, for example <c>WorkflowPremiseStarButton</c>.</summary>
    public static string WorkflowStarButton(string workflowLabel) =>
        $"Workflow{Identifier(workflowLabel)}StarButton";

    /// <summary>A workflow check box in the Customize Workflows dialog, for example <c>WorkflowPremiseCheck</c>.</summary>
    public static string WorkflowCheck(string workflowLabel) =>
        $"Workflow{Identifier(workflowLabel)}Check";

    /// <summary>A group row in the pane, for example <c>WorkflowGroupStoryWorldNavItem</c> for "Story World".</summary>
    public static string GroupNavItem(string bandTitle) =>
        $"WorkflowGroup{Identifier(bandTitle)}NavItem";

    /// <summary>
    ///     Keeps ASCII letters and digits only and capitalizes the start of each word, so
    ///     "Story World" becomes "StoryWorld" and "Premise" stays "Premise".
    /// </summary>
    private static string Identifier(string text)
    {
        var result = new StringBuilder();
        var startOfWord = true;
        foreach (var c in text ?? string.Empty)
        {
            if (!char.IsAsciiLetterOrDigit(c))
            {
                startOfWord = true;
                continue;
            }

            result.Append(startOfWord ? char.ToUpperInvariant(c) : c);
            startOfWord = false;
        }

        return result.ToString();
    }
}
