namespace StoryCADLib.DAL;

/// <summary>
///     Maps list values that were respelled in Lists.json (#1422) from the old
///     spelling to the new one when an outline is read. Without this, a fixed
///     drop-down (such as Value Exchange) shows a blank for the old value, and an
///     editable one shows the misspelling.
///     Only an exact match of a whole property value changes. Keep old spellings
///     here for as long as outlines saved before the respelling can exist.
/// </summary>
public static class ListValueMigration
{
    public static readonly IReadOnlyDictionary<string, string> Renamed = new Dictionary<string, string>
    {
        ["Burgler"] = "Burglar",
        ["CardSharp"] = "Card Sharp",
        ["AbsentMinded"] = "Absent-minded",
        ["Competative"] = "Competitive",
        ["Carefulness - Carlessness"] = "Carefulness - Carelessness",
        ["Approachability - Inaccessability"] = "Approachability - Inaccessibility",
        ["Balance - Inbalance"] = "Balance - Imbalance",
        ["Competence - Incompetance"] = "Competence - Incompetence",
        ["Neewspaper office"] = "Newspaper office",
        ["Prarie"] = "Prairie",
        ["Novellete"] = "Novelette",
        ["TV Screenplan"] = "TV Screenplay",
        ["Omniscent"] = "Omniscient",
        ["Opponent is a physical agressor"] = "Opponent is a physical aggressor",
        ["We often dispise what's most useful to us."] = "We often despise what's most useful to us.",
        ["Sacrifices a loved one's  confidence or respect"] = "Sacrifices a loved one's confidence or respect",
        ["Suspensful Moments"] = "Suspenseful Moments",
    };

    /// <summary>
    ///     Replaces old spellings in the element's settable string properties.
    ///     The element's Name is user text and never changes.
    /// </summary>
    public static void Apply(StoryElement element)
    {
        if (element == null)
        {
            return;
        }

        foreach (var property in element.GetType().GetProperties())
        {
            if (property.PropertyType != typeof(string) || !property.CanRead || !property.CanWrite
                || property.GetIndexParameters().Length > 0 || property.Name == nameof(StoryElement.Name))
            {
                continue;
            }

            if (property.GetValue(element) is string value && Renamed.TryGetValue(value, out var newValue))
            {
                property.SetValue(element, newValue);
            }
        }
    }
}
