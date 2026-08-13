using System.Collections.Generic;
using System.Text;

namespace StoryCollaborator.Models;

/// <summary>
/// The interview so far (#119). Rides in args on every turn, because /v1/workflow has
/// no messages array; and is the source of the Notes text Summarize appends.
/// </summary>
public sealed class InterviewTranscript
{
    /// <summary>
    /// One exchange. Label is the section id for a scripted turn, or the writer's own
    /// wording for a free question.
    /// </summary>
    public sealed record Turn(string Label, string Reply, bool IsFreeQuestion);

    private readonly List<Turn> _turns = new();

    public IReadOnlyList<Turn> Turns => _turns;

    public bool IsEmpty => _turns.Count == 0;

    public void AddSection(string sectionId, string reply)
    {
        if (string.IsNullOrWhiteSpace(sectionId) || string.IsNullOrWhiteSpace(reply))
            return;
        _turns.Add(new Turn(sectionId.Trim(), reply.Trim(), IsFreeQuestion: false));
    }

    public void AddFreeQuestion(string question, string reply)
    {
        if (string.IsNullOrWhiteSpace(question) || string.IsNullOrWhiteSpace(reply))
            return;
        _turns.Add(new Turn(question.Trim(), reply.Trim(), IsFreeQuestion: true));
    }

    /// <summary>
    /// What the Worker sees. Section turns are named by id so the template knows which
    /// ground is already covered and does not ask it again.
    /// </summary>
    public string ToPromptText()
    {
        if (_turns.Count == 0)
            return string.Empty;

        var sb = new StringBuilder();
        foreach (var turn in _turns)
        {
            sb.AppendLine(turn.IsFreeQuestion
                ? $"[writer asked] {turn.Label}"
                : $"[section {turn.Label}]");
            sb.AppendLine(turn.Reply);
            sb.AppendLine();
        }
        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// What lands in Notes. Terry: record the questions along with the answers.
    /// Plain text only — Notes renders no markup.
    /// </summary>
    public string ToNotesText(string characterName)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Interview with {characterName}");
        sb.AppendLine();

        foreach (var turn in _turns)
        {
            sb.AppendLine(turn.IsFreeQuestion
                ? $"Asked: {turn.Label}"
                : $"Section: {turn.Label}");
            sb.AppendLine(turn.Reply);
            sb.AppendLine();
        }
        return sb.ToString().TrimEnd();
    }
}
