namespace CollaboratorLib.Context;

/// <summary>
/// Outline-progress Guess for StoryContext and Outline gaps (#107).
/// </summary>
public sealed class DevelopmentGuess
{
    public required StoryContextBuilder.DevelopmentPhase Earliest { get; init; }

    public required IReadOnlyList<StoryContextBuilder.DevelopmentPhase> OpenSteps { get; init; }

    public required string PromptLine { get; init; }

    public required string GapsSentence { get; init; }
}
