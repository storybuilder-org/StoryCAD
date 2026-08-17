namespace StoryCADLib.Models.StoryWorld;

/// <summary>
/// Host World Type → six axis fields. Shared by the StoryWorld form and Collaborator Accept
/// (Collaborator #201). Selecting WorldType does not run through UpdateElementProperty auto-fill.
/// </summary>
public sealed record WorldTypeAxes(
    string Ontology,
    string WorldRelation,
    string RuleTransparency,
    string ScaleOfDifference,
    string AgencySource,
    string ToneLogic);

/// <summary>
/// Gestalt map for the eight Lists.json WorldType values.
/// </summary>
public static class WorldTypeAxisMap
{
    private static readonly Dictionary<string, WorldTypeAxes> Map =
        new(StringComparer.Ordinal)
        {
            ["Consensus Reality"] = new(
                "Mundane", "Primary World", "Explicit Rules", "Cosmetic",
                "Human-Centric", "Rational"),
            ["Enchanted Reality"] = new(
                "Supernatural", "Primary World", "Implicit Rules", "Cosmetic",
                "Systemic Forces", "Symbolic"),
            ["Hidden World"] = new(
                "Supernatural", "Layered", "Explicit Rules", "Structural",
                "Nonhuman Intelligences", "Rational"),
            ["Divergent World"] = new(
                "Scientific Speculative", "Divergent Earth", "Explicit Rules", "Structural",
                "Human-Centric", "Rational"),
            ["Constructed World"] = new(
                "Hybrid", "Secondary World", "Explicit Rules", "Cosmological",
                "Human-Centric", "Rational"),
            ["Mythic World"] = new(
                "Symbolic", "Secondary World", "Symbolic Rules", "Cosmological",
                "Fate / Providence", "Mythic"),
            ["Estranged World"] = new(
                "Scientific Speculative", "Secondary World", "Explicit Rules", "Cosmological",
                "Systemic Forces", "Dark / Entropic"),
            ["Broken World"] = new(
                "Scientific Speculative", "Divergent Earth", "Explicit Rules", "Structural",
                "Human-Centric", "Dark / Entropic"),
        };

    /// <summary>The eight WorldType keys in map order.</summary>
    public static IReadOnlyCollection<string> WorldTypes => Map.Keys;

    public static bool TryGet(string worldType, out WorldTypeAxes axes)
    {
        if (string.IsNullOrWhiteSpace(worldType))
        {
            axes = null!;
            return false;
        }

        return Map.TryGetValue(worldType.Trim(), out axes!);
    }
}
