using System;
using System.Collections.Generic;

namespace StoryCollaborator.Workflows;

/// <summary>
/// Field cursor, not a cue line (#119).
///
/// The client owns which Character field is in play. It does not own a spoken
/// line index. Question text stays on the Worker (ADR-005).
/// </summary>
public static class InterviewScript
{
    /// <summary>
    /// Terry's field order. Flaw first. Description last.
    /// </summary>
    public static readonly IReadOnlyList<string> Fields = new[]
    {
        "Flaw",
        "BackStory",
        "Values",
        "Enneagram",
        "Focus",
        "PsychNotes",
        "Abnormality",
        "Intelligence",
        "TraitList",
        "Role",
        "StoryRole",
        "Archetype",
        "Description"
    };

    /// <summary>Where the interview opens.</summary>
    public static string First => Fields[0];

    /// <summary>
    /// The field after this one, or null after Description.
    /// An unknown field ends the interview cleanly.
    /// </summary>
    public static string? NextField(string field)
    {
        var index = -1;
        for (var i = 0; i < Fields.Count; i++)
        {
            if (string.Equals(Fields[i], field, StringComparison.OrdinalIgnoreCase))
            {
                index = i;
                break;
            }
        }

        if (index < 0 || index + 1 >= Fields.Count)
            return null;

        return Fields[index + 1];
    }
}
