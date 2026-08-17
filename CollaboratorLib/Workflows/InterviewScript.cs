using System;
using System.Collections.Generic;
using System.Linq;

namespace StoryCollaborator.Workflows;

/// <summary>
/// The shape of the interview (#119): which fields are worked, in what order, and how many
/// questions each one has.
///
/// The question text is not here and must not be. It is Terry's craft material and this
/// repo is public (ADR-005), so the Worker holds every line and this side holds only the
/// field ids, which are StoryCAD property names, and the counts.
///
/// The cursor lives on the client because the Worker cannot be trusted with it. Asked to
/// track position, emit a line verbatim and judge an answer in one pass, the model drifted:
/// it reported line 3 while asking line 4's question, and re-asked answered lines at block
/// boundaries. Now the client computes the next position and hands both candidates over, so
/// the model's only job is to decide whether the writer answered.
/// </summary>
public static class InterviewScript
{
    /// <summary>One field's block: the id the Worker keys on, and how many lines it has.</summary>
    public sealed record Block(string Field, int Lines);

    /// <summary>
    /// Terry's order. Flaw first because it is the field a form is most often missing, and
    /// the sketch last because its questions recap the blocks before it.
    /// </summary>
    public static readonly IReadOnlyList<Block> Blocks = new List<Block>
    {
        new("Flaw", 4),
        new("BackStory", 2),
        new("Values", 2),
        new("Enneagram", 3),
        new("Focus", 2),
        new("PsychNotes", 2),
        new("Abnormality", 2),
        new("Intelligence", 2),
        new("TraitList", 3),
        new("Role", 2),
        new("StoryRole", 2),
        new("Archetype", 2),
        new("Description", 2)
    };

    /// <summary>Where the interview opens.</summary>
    public static (string Field, int Line) First => (Blocks[0].Field, 1);

    /// <summary>
    /// The position after this one, or null when the last block's last line is answered.
    /// An unknown field is treated as the end rather than throwing: a garbled cursor should
    /// close the interview cleanly, not crash a session the writer has answers in.
    /// </summary>
    public static (string Field, int Line)? Next(string field, int line)
    {
        var index = Blocks.ToList().FindIndex(
            b => string.Equals(b.Field, field, StringComparison.OrdinalIgnoreCase));

        if (index < 0)
            return null;

        if (line < Blocks[index].Lines)
            return (Blocks[index].Field, line + 1);

        return index + 1 < Blocks.Count
            ? (Blocks[index + 1].Field, 1)
            : null;
    }

    /// <summary>Questions asked if every line is answered once. Used for progress only.</summary>
    public static int TotalLines => Blocks.Sum(b => b.Lines);

    /// <summary>
    /// The one position where Terry scripts the follow-up rather than leaving it to
    /// judgement: BackStory line 2, "What happened the first time that habit in you
    /// worked?", answered with only a year or a place.
    /// </summary>
    private static readonly (string Field, int Line) ScriptedFollowUp = ("BackStory", 2);

    /// <summary>
    /// Longest answer still treated as "only a year or a place". A proxy for Terry's
    /// condition, and a cheap one, but the cost of reading it wrong is one extra good
    /// question rather than anything the writer has to undo.
    /// </summary>
    private const int ThinAnswerWords = 6;

    /// <summary>
    /// True when this answer should get Terry's scripted follow-up.
    ///
    /// Evaluated here rather than by the model. Asked to judge the condition itself it
    /// fired the follow-up on a full answer and re-asked the line on a bare year, and
    /// carrying the extra decision knocked its verdicts out of step with the question
    /// it printed. The Worker still owns the wording (ADR-005); it is only told when.
    /// </summary>
    public static bool WantsScriptedFollowUp(string field, int line, string? answer, bool followUpUsed)
    {
        if (followUpUsed || string.IsNullOrWhiteSpace(answer))
            return false;

        if (!string.Equals(field, ScriptedFollowUp.Field, StringComparison.OrdinalIgnoreCase)
            || line != ScriptedFollowUp.Line)
            return false;

        var words = answer.Split(
            new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

        return words.Length <= ThinAnswerWords;
    }
}
