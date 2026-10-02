namespace StoryCollaborator.Models;

/// <summary>
/// Optional craft recommendation for a workflow output field (issue #116).
/// Not every short-list field is craft-backed — only entries listed here.
/// Preferred values must already exist in StoryCAD Lists.json.
/// </summary>
public sealed record CraftFieldHint(
    string WorkflowId,
    string ElementLabel,
    string Property,
    IReadOnlyList<string> PreferredValues,
    string Explanation);

/// <summary>
/// Static craft hints. Keep small and explicit; do not auto-derive from all short-lists.
/// </summary>
public static class CraftFieldHints
{
    public static readonly CraftFieldHint InnerProblemConflictType = new(
        WorkflowId: "InnerOuterProblems",
        ElementLabel: "InnerProblem",
        Property: "ConflictType",
        PreferredValues: new[] { "Person vs. Self" },
        Explanation:
            "Inner problems are often Person vs. Self because the main struggle is inside the character. " +
            "Another list value can still be right when the dilemma is about a person or group " +
            "(for example orders, loyalty, or shame in front of others). Keep your value, or use the craft default.");

    private static readonly CraftFieldHint[] All =
    {
        InnerProblemConflictType
    };

    public static CraftFieldHint? Find(string workflowId, string elementLabel, string property)
    {
        foreach (var hint in All)
        {
            if (string.Equals(hint.WorkflowId, workflowId, StringComparison.OrdinalIgnoreCase)
                && string.Equals(hint.ElementLabel, elementLabel, StringComparison.OrdinalIgnoreCase)
                && string.Equals(hint.Property, property, StringComparison.OrdinalIgnoreCase))
            {
                return hint;
            }
        }

        return null;
    }
}
