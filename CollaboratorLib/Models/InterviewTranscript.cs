using System.Collections.Generic;
using System.Text;

namespace StoryCollaborator.Models;

/// <summary>
/// The interview so far (#119): what Collaborator asked and what the writer typed back,
/// play-acting the character.
///
/// Rides in args on every turn, because /v1/workflow has no messages array, and is the
/// text saved into the outline when the session ends.
///
/// The question is stored, not just an id. Terry's test is that the saved text is what
/// appeared on screen, and the first build kept only a section id and the model's reply,
/// so the questions could not be reconstructed at save time.
/// </summary>
public sealed class InterviewTranscript
{
    /// <summary>
    /// One exchange. Field and Line are the cursor the Worker tagged the question with,
    /// so a later pass can tell which form field an answer was aimed at.
    /// </summary>
    public sealed record Turn(string Field, int Line, string Question, string Answer);

    private readonly List<Turn> _turns = new();

    public IReadOnlyList<Turn> Turns => _turns;

    public bool IsEmpty => _turns.Count == 0;

    /// <summary>
    /// Records an answered question. An unanswered question is not a turn: the writer may
    /// close the panel on a question they never replied to, and a dangling prompt in the
    /// saved record reads as something they refused rather than something they never saw.
    /// </summary>
    public void Add(string field, int line, string question, string answer)
    {
        if (string.IsNullOrWhiteSpace(question) || string.IsNullOrWhiteSpace(answer))
            return;

        _turns.Add(new Turn(
            string.IsNullOrWhiteSpace(field) ? "Unknown" : field.Trim(),
            line,
            question.Trim(),
            answer.Trim()));
    }

    /// <summary>
    /// What the Worker sees. Tagged with the cursor so it can tell which lines are already
    /// asked, and whether it has already spent its one follow-up on a line.
    /// </summary>
    public string ToPromptText()
    {
        if (_turns.Count == 0)
            return string.Empty;

        var sb = new StringBuilder();
        foreach (var turn in _turns)
        {
            sb.AppendLine($"[{turn.Field}:{turn.Line}] Q: {turn.Question}");
            sb.AppendLine($"A: {turn.Answer}");
            sb.AppendLine();
        }
        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// What lands in the outline. The questions as asked and the answers as typed, in
    /// order, with nothing added and nothing summarized. Plain text only, because a Notes
    /// element renders no markup.
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
