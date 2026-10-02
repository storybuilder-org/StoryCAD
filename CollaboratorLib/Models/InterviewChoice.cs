using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using StoryCollaborator.Workflows;

namespace StoryCollaborator.Models;

/// <summary>How the writer answered the choice list.</summary>
public enum InterviewChoiceKind
{
    /// <summary>They named targets. <see cref="InterviewChoice.Targets"/> holds them in order.</summary>
    Targets,

    /// <summary>"You choose": the Worker names the targets on the opening reply.</summary>
    ModelChooses,

    /// <summary>Nothing in the reply names a target. Show the list again.</summary>
    Unreadable
}

/// <summary>
/// The writer's answer to "what do you want to explore?" (#119, design section 25.5).
///
/// Chat-typed, not a control: the interview runs from the chat pane and the #148 list
/// picker does not exist yet. Numbers, page headers, or "you choose".
///
/// A list ("1, 3", "Flaw and Backstory") is taken as written. A sentence ("focus on her
/// flaw and where it came from") keeps what it can place, except that Focus, Role, Values,
/// Intelligence and Description are ordinary English words as well as headers and count
/// only in a list, never inside a sentence (review, 2026-09-07: "focus on her flaw" was
/// read as Focus then Flaw). Named targets win over a "you choose" phrase in the same
/// reply: "Flaw and Backstory, then you pick the rest" is a plan of two, not a hand-off.
/// A reply with nothing readable repeats the list.
/// </summary>
public sealed record InterviewChoice(InterviewChoiceKind Kind, IReadOnlyList<string> Targets)
{
    private static readonly IReadOnlyList<string> None = Array.Empty<string>();

    private static readonly Regex ChoosePattern = new(
        @"\b(you|your)\s*(choose|pick|decide|call)\b|\bchoose for me\b|\bup to you\b"
        + @"|\bsurprise me\b|\bdealer'?s choice\b|\bwhatever you (think|want|like)\b|\bno preference\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex EverythingPattern = new(
        @"^\s*(all|everything|all of them|the lot|the whole form)\s*[.!]?\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // "want and need" would otherwise be split on its own "and".
    private static readonly Regex WantNeedPattern = new(
        @"\bwant\s*(and|&|vs\.?|versus|against|/)\s*need\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex Splitter = new(
        @"[,;/\n]+|\band\b|\bthen\b|\s+",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>Headers that are also everyday words. Trusted in a list, not in a sentence.</summary>
    private static readonly HashSet<string> EverydayWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "Focus", "Role", "Values", "Intelligence", "Description"
    };

    /// <summary>
    /// Headers with a space in them ("Story Role", "Psych Notes"), matched with or without
    /// the space and swapped for their id before the reply is split into words. Without
    /// this "story role" reads as Role.
    /// </summary>
    private static readonly IReadOnlyList<(Regex Pattern, string Id)> MultiWordLabels = BuildMultiWordLabels();

    public static InterviewChoice Parse(string? reply)
    {
        var text = (reply ?? string.Empty).Trim();
        if (text.Length == 0)
            return new InterviewChoice(InterviewChoiceKind.Unreadable, None);

        if (EverythingPattern.IsMatch(text))
            return new InterviewChoice(InterviewChoiceKind.Targets, InterviewScript.Fields);

        var prepared = WantNeedPattern.Replace(text, InterviewScript.WantNeed);
        foreach (var (pattern, id) in MultiWordLabels)
            prepared = pattern.Replace(prepared, id);

        var found = new List<string>();
        var everyday = new List<string>();
        var unplaced = 0;
        foreach (var raw in Splitter.Split(prepared))
        {
            var token = raw.Trim().TrimStart('#').TrimEnd('.', ')', ':');
            if (token.Length == 0)
                continue;

            string? id = null;
            if (int.TryParse(token, out var number))
            {
                if (number >= 1 && number <= InterviewScript.Targets.Count)
                    id = InterviewScript.Targets[number - 1];
            }
            else
            {
                id = InterviewScript.Canonical(token);
            }

            if (id == null)
            {
                unplaced++;
                continue;
            }

            if (found.Contains(id))
                continue;

            found.Add(id);
            if (EverydayWords.Contains(token))
                everyday.Add(id);
        }

        // A sentence, not a list: the everyday words in it were probably just words.
        if (unplaced > 0)
        {
            foreach (var id in everyday)
                found.Remove(id);
        }

        if (found.Count > 0)
            return new InterviewChoice(InterviewChoiceKind.Targets, found);

        if (ChoosePattern.IsMatch(text))
            return new InterviewChoice(InterviewChoiceKind.ModelChooses, None);

        return new InterviewChoice(InterviewChoiceKind.Unreadable, None);
    }

    /// <summary>
    /// The list the writer answers: every target by its page header, numbered, blank
    /// fields marked. Headers are public; the Worker's purpose text is not.
    /// </summary>
    public static string ListText(string characterName, IReadOnlySet<string>? blankFields)
    {
        var sb = new StringBuilder();
        sb.AppendLine(
            $"What do you want to explore about {characterName}? Reply with numbers or names, "
            + "in the order you want them, or say: you choose.");

        for (var i = 0; i < InterviewScript.Targets.Count; i++)
        {
            var id = InterviewScript.Targets[i];
            var blank = blankFields != null && blankFields.Contains(id);
            sb.AppendLine($"{i + 1}. {InterviewScript.Label(id)}{(blank ? " (empty)" : string.Empty)}");
        }

        return sb.ToString().TrimEnd();
    }

    private static IReadOnlyList<(Regex, string)> BuildMultiWordLabels()
    {
        // Two-word ways of saying a target that are not its header. Local rather than a
        // static field: static initializers run in declaration order, and a field read
        // by MultiWordLabels' initializer would have to be declared above it.
        var aliases = new (string Phrase, string Id)[]
        {
            ("back story", "BackStory"),
            ("psychological notes", "PsychNotes"),
            ("trait list", "TraitList"),
            ("want need", InterviewScript.WantNeed)
        };

        var list = new List<(Regex, string)>();
        foreach (var id in InterviewScript.Targets)
            Add(InterviewScript.Label(id), id);
        foreach (var (phrase, id) in aliases)
            Add(phrase, id);
        return list;

        void Add(string phrase, string id)
        {
            var words = phrase.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length < 2)
                return;

            var pattern = @"\b" + string.Join(@"\s*", Array.ConvertAll(words, Regex.Escape)) + @"\b";
            list.Add((new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled), id));
        }
    }
}
