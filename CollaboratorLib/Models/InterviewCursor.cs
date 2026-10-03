using StoryCollaborator.Workflows;

namespace StoryCollaborator.Models;

/// <summary>
/// Which field is in play, how many Keep-asking turns already landed on it, and the
/// plan it is walking (#119).
/// </summary>
public sealed class InterviewCursor
{
    private InterviewPlan _plan = InterviewPlan.Default();

    public string Field { get; private set; } = string.Empty;

    /// <summary>Keep-asking outcomes already applied on this field.</summary>
    public int TurnsOnField { get; private set; }

    /// <summary>True before the first question has been asked.</summary>
    public bool NotStarted => Field.Length == 0;

    /// <summary>The order this session walks (design section 25).</summary>
    public InterviewPlan Plan => _plan;

    /// <summary>The field after this one in the plan, or null on the last.</summary>
    public string? NextField => NotStarted ? null : _plan.Next(Field);

    /// <summary>Puts the cursor on the first field of Terry's order.</summary>
    public void Start() => Start(InterviewPlan.Default());

    /// <summary>Puts the cursor on the first target of the plan.</summary>
    public void Start(InterviewPlan plan)
    {
        _plan = plan;
        Field = plan.First;
        TurnsOnField = 0;
    }

    /// <summary>
    /// Applies the interviewer verdict. Does not take a next line.
    /// Next field comes from the plan.
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
                var next = _plan.Next(Field);
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
        _plan = InterviewPlan.Default();
    }

    private void MoveTo(string field)
    {
        Field = field;
        TurnsOnField = 0;
    }
}
