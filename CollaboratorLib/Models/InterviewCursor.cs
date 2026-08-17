using StoryCollaborator.Workflows;

namespace StoryCollaborator.Models;

/// <summary>
/// Where the interview is, and what has already been spent on the current question (#119).
///
/// A type of its own because the three pieces interact: a follow-up holds position but
/// spends its one chance, a retry holds position but only so many times, and either
/// moving on resets both. Kept as fields on the Collaborator it was three flags nobody
/// could test without standing up a session.
/// </summary>
public sealed class InterviewCursor
{
    /// <summary>
    /// Re-asks allowed before the interview moves on regardless. Two, so a question is
    /// put at most three times.
    ///
    /// Observed live before this existed: Leonard Kraskin's Flaw line 4, a double-bind,
    /// asked five times running. Each verdict was defensible on its own -- "yes" and
    /// "it costs me" pick neither fork -- and the writer still had no way past it.
    /// Terry's rule is "repeat the question, or drop to the lead-in that has not been
    /// answered yet", but at the last line of a block every lead-in is usually answered,
    /// and a strict reading leaves the writer facing the same sentence forever.
    /// </summary>
    public const int MaxRetries = 2;

    public string Field { get; private set; } = string.Empty;

    public int Line { get; private set; }

    /// <summary>The one follow-up allowed on this question is spent.</summary>
    public bool FollowUpUsed { get; private set; }

    /// <summary>Times this question has been re-asked without moving on.</summary>
    public int Retries { get; private set; }

    /// <summary>True before the first question has been asked.</summary>
    public bool NotStarted => Field.Length == 0;

    /// <summary>Puts the cursor on the interview's first question.</summary>
    public void Start()
    {
        var (field, line) = InterviewScript.First;
        MoveTo(field, line);
    }

    /// <summary>
    /// Applies the interviewer's verdict.
    ///
    /// Retry only holds position while retries remain; past the cap the interview moves
    /// on. What the writer typed is already in the transcript either way, so moving on
    /// costs the record nothing and being stuck costs it everything.
    /// </summary>
    public void Apply(InterviewVerdict verdict, (string Field, int Line)? next)
    {
        switch (verdict)
        {
            case InterviewVerdict.FollowUp:
                FollowUpUsed = true;
                Retries = 0;
                break;

            case InterviewVerdict.Retry when Retries < MaxRetries:
                Retries++;
                break;

            default:
                if (next != null)
                    MoveTo(next.Value.Field, next.Value.Line);
                else
                    ClearSpend();
                break;
        }
    }

    /// <summary>The position after this one, or null at the end of the script.</summary>
    public (string Field, int Line)? Next() => InterviewScript.Next(Field, Line);

    /// <summary>True when this answer earns Terry's scripted follow-up.</summary>
    public bool WantsScriptedFollowUp(string? answer) =>
        InterviewScript.WantsScriptedFollowUp(Field, Line, answer, FollowUpUsed);

    private void MoveTo(string field, int line)
    {
        Field = field;
        Line = line;
        ClearSpend();
    }

    /// <summary>Back to before the interview started. Called when a session ends.</summary>
    public void Reset()
    {
        Field = string.Empty;
        Line = 0;
        ClearSpend();
    }

    private void ClearSpend()
    {
        FollowUpUsed = false;
        Retries = 0;
    }
}
