using StoryCollaborator.Workflows;

namespace StoryCollaborator.Models;

/// <summary>
/// Which field is in play, and how many Keep-asking turns already landed on it (#119).
/// </summary>
public sealed class InterviewCursor
{
    public string Field { get; private set; } = string.Empty;

    /// <summary>Keep-asking outcomes already applied on this field.</summary>
    public int TurnsOnField { get; private set; }

    /// <summary>True before the first question has been asked.</summary>
    public bool NotStarted => Field.Length == 0;

    /// <summary>Puts the cursor on Flaw.</summary>
    public void Start()
    {
        Field = InterviewScript.First;
        TurnsOnField = 0;
    }

    /// <summary>
    /// Applies the interviewer verdict. Does not take a next line.
    /// Next field comes from <see cref="InterviewScript.NextField"/>.
    /// </summary>
    public void Apply(InterviewVerdict verdict)
    {
        switch (verdict)
        {
            case InterviewVerdict.KeepAsking:
                TurnsOnField++;
                break;

            case InterviewVerdict.GotIt:
            case InterviewVerdict.NotThis:
                var next = InterviewScript.NextField(Field);
                if (next != null)
                    MoveTo(next);
                break;

            default:
                break;
        }
    }

    /// <summary>Back to before the interview started.</summary>
    public void Reset()
    {
        Field = string.Empty;
        TurnsOnField = 0;
    }

    private void MoveTo(string field)
    {
        Field = field;
        TurnsOnField = 0;
    }
}
