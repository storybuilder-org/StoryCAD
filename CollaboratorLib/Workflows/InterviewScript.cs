using System;
using System.Collections.Generic;
using System.Text;
using StoryCADLib.Models;

namespace StoryCollaborator.Workflows;

/// <summary>
/// The fields an interview can pursue (#119). Field cursor, not a cue line.
///
/// The client owns which Character field is in play and, since the 2026-09-07 revision
/// (design section 25), the order it walks them in: see <see cref="Models.InterviewPlan"/>.
/// It does not own a spoken line index. Question text and field purpose stay on the
/// Worker (ADR-005). Everything here is public already: the Character form's property
/// ids, the headers the Character page shows for them, and which of them are blank.
///
/// One table. The id list, the labels and the blank check used to be three lists that
/// nothing tied together (review, 2026-09-07); a target added to one and not the others
/// would number in the choice list and never be marked empty.
/// </summary>
public static class InterviewScript
{
    /// <summary>
    /// Want against need (Terry, 2026-09-07): a target that is not a Character field.
    /// The Problem names the want; the interview listens for the need. Notes only.
    /// </summary>
    public const string WantNeed = "WantNeed";

    /// <summary>One target: the property id, the Character page's header for it, and how to tell it is blank.</summary>
    private sealed record Target(string Id, string Label, Func<CharacterModel, bool>? IsBlank);

    /// <summary>
    /// Terry's field order, Flaw first and the sketch last, then WantNeed. Labels are the
    /// Character page headers, so the choice list says what the form says.
    /// </summary>
    private static readonly IReadOnlyList<Target> Table = new[]
    {
        new Target("Flaw", "Flaw", c => Blank(c.Flaw)),
        new Target("BackStory", "Backstory", c => Blank(c.BackStory)),
        new Target("Values", "Values", c => Blank(c.Values)),
        new Target("Enneagram", "Enneagram", c => Blank(c.Enneagram)),
        new Target("Focus", "Focus", c => Blank(c.Focus)),
        new Target("PsychNotes", "Psych Notes", c => Blank(c.PsychNotes)),
        new Target("Abnormality", "Abnormality", c => Blank(c.Abnormality)),
        new Target("Intelligence", "Intelligence", c => Blank(c.Intelligence)),
        new Target("TraitList", "Traits", c => c.TraitList is not { Count: > 0 }),
        new Target("Role", "Role", c => Blank(c.Role)),
        new Target("StoryRole", "Story Role", c => Blank(c.StoryRole)),
        new Target("Archetype", "Archetype", c => Blank(c.Archetype)),
        new Target("Description", "Character Sketch", c => Blank(c.Description)),
        new Target(WantNeed, "Want and need", null)
    };

    /// <summary>The form fields, in Terry's order. The plan when nobody chooses.</summary>
    public static readonly IReadOnlyList<string> Fields = BuildIds(formOnly: true);

    /// <summary>Every id a plan may hold: the form fields, then WantNeed.</summary>
    public static readonly IReadOnlyList<string> Targets = BuildIds(formOnly: false);

    /// <summary>
    /// Other ways a writer names a target in chat, keyed by <see cref="Normalize"/>d text.
    /// Ids and labels resolve without this table.
    /// </summary>
    private static readonly Dictionary<string, string> Aliases = new()
    {
        ["history"] = "BackStory",
        ["backstory"] = "BackStory",
        ["psychologicalnotes"] = "PsychNotes",
        ["psychology"] = "PsychNotes",
        ["psychological"] = "PsychNotes",
        ["trait"] = "TraitList",
        ["sketch"] = "Description",
        ["wantvsneed"] = WantNeed,
        ["wantversusneed"] = WantNeed,
        ["wantagainstneed"] = WantNeed,
        ["wantandneed"] = WantNeed
    };

    /// <summary>Where Terry's order opens.</summary>
    public static string First => Fields[0];

    /// <summary>
    /// The field after this one in Terry's order, or null after the sketch.
    /// An unknown field ends the walk cleanly.
    /// </summary>
    public static string? NextField(string field)
    {
        var index = IndexOf(Fields, field);
        if (index < 0 || index + 1 >= Fields.Count)
            return null;

        return Fields[index + 1];
    }

    /// <summary>The Character page's header for a target id. An unknown id comes back as itself.</summary>
    public static string Label(string field)
    {
        var target = Find(field);
        return target?.Label ?? field;
    }

    /// <summary>The form fields with nothing on them. WantNeed is not a field and is never blank.</summary>
    public static IReadOnlySet<string> BlankFields(CharacterModel? character)
    {
        var blank = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (character == null)
            return blank;

        foreach (var target in Table)
        {
            if (target.IsBlank != null && target.IsBlank(character))
                blank.Add(target.Id);
        }
        return blank;
    }

    /// <summary>
    /// The id as the plan spells it, or null when the token names no target. Accepts the
    /// id, the page header, or an alias, in any case, with or without spaces.
    /// </summary>
    public static string? Canonical(string? token)
    {
        var key = Normalize(token);
        if (key.Length == 0)
            return null;

        foreach (var target in Table)
        {
            if (Normalize(target.Id) == key || Normalize(target.Label) == key)
                return target.Id;
        }

        return Aliases.TryGetValue(key, out var alias) ? alias : null;
    }

    /// <summary>Letters and digits only, lower case, so "Back story" and "backstory" agree.</summary>
    public static string Normalize(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        var sb = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            if (char.IsLetterOrDigit(c))
                sb.Append(char.ToLowerInvariant(c));
        }
        return sb.ToString();
    }

    /// <summary>Position of a field in a list, ignoring case, or -1.</summary>
    public static int IndexOf(IReadOnlyList<string> list, string? field)
    {
        for (var i = 0; i < list.Count; i++)
        {
            if (string.Equals(list[i], field, StringComparison.OrdinalIgnoreCase))
                return i;
        }
        return -1;
    }

    private static Target? Find(string? id)
    {
        foreach (var target in Table)
        {
            if (string.Equals(target.Id, id, StringComparison.OrdinalIgnoreCase))
                return target;
        }
        return null;
    }

    private static bool Blank(string? value) => string.IsNullOrWhiteSpace(value);

    private static IReadOnlyList<string> BuildIds(bool formOnly)
    {
        var ids = new List<string>();
        foreach (var target in Table)
        {
            if (!formOnly || target.IsBlank != null)
                ids.Add(target.Id);
        }
        return ids;
    }
}
