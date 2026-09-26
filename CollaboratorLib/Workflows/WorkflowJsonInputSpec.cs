namespace StoryCollaborator.Workflows;

/// <summary>
/// Issue #260: the property lists a migrated workflow's "input" object writes. Set on
/// <see cref="Workflow.JsonInput"/>; null means the workflow sends no "input" field yet and
/// Elements and Args stay its only carriers until it migrates.
/// </summary>
public sealed class WorkflowJsonInputSpec
{
    /// <summary>Properties written onto the workflow's own target element (FlawBackstory's top-level "character" key).</summary>
    public IReadOnlyList<string> TargetProperties { get; init; } = Array.Empty<string>();

    /// <summary>Properties written onto each related Problem, before protagonistCharacter/antagonistCharacter are added.</summary>
    public IReadOnlyList<string> RelatedProblemProperties { get; init; } = Array.Empty<string>();

    /// <summary>Properties written onto a resolved protagonistCharacter/antagonistCharacter.</summary>
    public IReadOnlyList<string> ResolvedCharacterProperties { get; init; } = Array.Empty<string>();
}
