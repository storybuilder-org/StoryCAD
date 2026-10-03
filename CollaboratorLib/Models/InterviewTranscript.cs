using System.Collections.Generic;
using System.Text;

namespace StoryCollaborator.Models;

/// <summary>
/// The interview so far (#119): what Collaborator asked and what the writer typed back.
/// </summary>
public sealed class InterviewTranscript
{
    /// <summary>One exchange. Field is the form id the turn was aimed at.</summary>
    public sealed record Turn(string Field, string Question, string Answer);

    private readonly List<Turn> _turns = new();

    public IReadOnlyList<Turn> Turns => _turns;

    public bool IsEmpty => _turns.Count == 0;

    /// <summary>
    /// Records an answered question. An unanswered question is not a turn.
    /// </summary>
    public void Add(string field, string question, string answer)
    {
        if (string.IsNullOrWhiteSpace(question) || string.IsNullOrWhiteSpace(answer))
            return;

        _turns.Add(new Turn(
            string.IsNullOrWhiteSpace(field) ? "Unknown" : field.Trim(),
            question.Trim(),
            answer.Trim()));
    }

    /// <summary>
    /// Extends the last answer. After a failed or empty Worker reply the writer's answer is
    /// on record with no question after it; their next message continues that answer
    /// rather than opening a second turn on the same question. False when there is
    /// nothing to extend or nothing to add.
    /// </summary>
    public bool ExtendLastAnswer(string more)
    {
        if (_turns.Count == 0 || string.IsNullOrWhiteSpace(more))
            return false;

        var last = _turns[^1];
        _turns[^1] = last with { Answer = last.Answer + " " + more.Trim() };
        return true;
    }

    /// <summary>What the Worker sees. Tags are [Field] only.</summary>
    public string ToPromptText()
    {
        if (_turns.Count == 0)
            return string.Empty;

        var sb = new StringBuilder();
        foreach (var turn in _turns)
        {
            sb.AppendLine($"[{turn.Field}] Q: {turn.Question}");
            sb.AppendLine($"A: {turn.Answer}");
            sb.AppendLine();
        }
        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// What lands in the outline. Questions as asked and answers as typed.
    /// </summary>
    public string ToNotesText(string characterName)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Interview with {characterName}");
        sb.AppendLine();

        foreach (var turn in _turns)
        {
            sb.AppendLine($"Q: {turn.Question}");
            sb.AppendLine($"A: {turn.Answer}");
            sb.AppendLine();
        }
        return sb.ToString().TrimEnd();
    }
}
