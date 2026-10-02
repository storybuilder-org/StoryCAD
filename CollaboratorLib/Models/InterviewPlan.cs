using System.Collections.Generic;
using System.Text;
using StoryCollaborator.Workflows;

namespace StoryCollaborator.Models;

/// <summary>
/// The fields one interview pursues, in order (#119, design section 25).
///
/// Replaces the fixed walk from Flaw to Description. The writer names the targets in
/// chat, or says "you choose" and the Worker names them on the opening reply; either
/// way the client holds the list from then on and the model only ever reports a
/// verdict. Unknown ids are dropped and an empty list falls back to Terry's order, so a
/// garbled TARGETS line can narrow the interview but never leave it with no field.
/// </summary>
public sealed class InterviewPlan
{
    public IReadOnlyList<string> Targets { get; }

    private InterviewPlan(IReadOnlyList<string> targets)
    {
        Targets = targets;
    }

    /// <summary>Terry's order, every form field.</summary>
    public static InterviewPlan Default() => new(InterviewScript.Fields);

    /// <summary>
    /// Known ids kept in the order given, duplicates and unknowns dropped. Nothing left
    /// means the default.
    /// </summary>
    public static InterviewPlan From(IEnumerable<string>? ids) =>
        FromKnown(ids, maxCount: 0) ?? Default();

    /// <summary>
    /// Known ids kept in the order given, or null when none of them is a target. For the
    /// Worker's TARGETS line: the client must be able to tell "it chose nothing usable"
    /// from "it chose", rather than walk all thirteen fields under a first question aimed
    /// at something else (review, 2026-09-07). A positive maxCount keeps that many: the
    /// Worker is asked for up to three.
    /// </summary>
    public static InterviewPlan? FromKnown(IEnumerable<string>? ids, int maxCount)
    {
        var kept = new List<string>();
        if (ids != null)
        {
            foreach (var raw in ids)
            {
                var id = InterviewScript.Canonical(raw);
                if (id != null && !kept.Contains(id))
                    kept.Add(id);
                if (maxCount > 0 && kept.Count == maxCount)
                    break;
            }
        }

        return kept.Count == 0 ? null : new InterviewPlan(kept);
    }

    /// <summary>True when nobody narrowed the interview.</summary>
    public bool IsDefault => ReferenceEquals(Targets, InterviewScript.Fields);

    public string First => Targets[0];

    /// <summary>The target after this one, or null on the last or an unknown one.</summary>
    public string? Next(string field)
    {
        var index = InterviewScript.IndexOf(Targets, field);
        if (index < 0 || index + 1 >= Targets.Count)
            return null;

        return Targets[index + 1];
    }

    /// <summary>The ids as the Worker receives them: comma-separated, plan order.</summary>
    public string ToArg() => string.Join(",", Targets);

    /// <summary>
    /// What the writer is told when the Worker chose: form labels, plan order. The ids
    /// never reach the chat.
    /// </summary>
    public string Describe()
    {
        if (IsDefault)
            return "Starting with Flaw and working through the form.";

        var sb = new StringBuilder("Starting with ");
        for (var i = 0; i < Targets.Count; i++)
        {
            if (i > 0)
                sb.Append(", then ");
            sb.Append(InterviewScript.Label(Targets[i]));
        }
        return sb.Append('.').ToString();
    }
}
