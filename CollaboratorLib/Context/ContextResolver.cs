using StoryCADLib.Models;

namespace CollaboratorLib.Context;

/// <summary>
/// Determines what context to gather based on workflow and element type.
/// Context is inferred from current state - no workflow coordination or enforced ordering.
/// </summary>
public class ContextResolver
{
    private static readonly HashSet<string> FullContextWorkflows = new(StringComparer.OrdinalIgnoreCase)
    {
        "GMC",
        "Critique"
    };

    private static readonly HashSet<string> MinimalContextWorkflows = new(StringComparer.OrdinalIgnoreCase)
    {
        "Premise"
    };

    /// <summary>
    /// Character craft workflows that receive the RelatedProblems collection (issue #107).
    /// Relationship is deferred (not problem-based).
    /// </summary>
    public static readonly HashSet<string> RelatedProblemsWorkflows = new(StringComparer.OrdinalIgnoreCase)
    {
        // #182 DefineCharacter; #183 StoryFunction; #184 FlawBackstory.
        "DefineCharacter",
        "StoryFunction",
        "FlawBackstory"
    };

    public const string RelatedProblemsRequestName = "RelatedProblems";

    /// <summary>
    /// StoryWorld workflows that receive all Setting elements as RelatedSettings (#201).
    /// </summary>
    public static readonly HashSet<string> RelatedSettingsWorkflows = new(StringComparer.OrdinalIgnoreCase)
    {
        "DefineStoryWorld"
    };

    public const string RelatedSettingsRequestName = "RelatedSettings";

    /// <summary>
    /// StoryWorld workflows that receive Notes + Web under the StoryWorld node (#201).
    /// </summary>
    public static readonly HashSet<string> RelatedResearchWorkflows = new(StringComparer.OrdinalIgnoreCase)
    {
        "DefineStoryWorld"
    };

    public const string RelatedResearchRequestName = "RelatedResearch";

    /// <summary>
    /// Get the context specification for a workflow and element type combination.
    /// </summary>
    public ContextSpec GetContextFor(string workflowLabel, StoryItemType elementType)
    {
        if (FullContextWorkflows.Contains(workflowLabel) && elementType == StoryItemType.Problem)
        {
            return ContextSpec.Full;
        }

        // Premise: constraints + gaps (gap pass is outline-wide for every run)
        if (MinimalContextWorkflows.Contains(workflowLabel))
        {
            return new ContextSpec
            {
                IncludeStoryConstraints = true,
                IncludeGaps = true
            };
        }

        if (elementType == StoryItemType.Scene)
        {
            return new ContextSpec
            {
                IncludeStoryConstraints = true,
                IncludeBeatHierarchy = false,
                IncludeCharacterContext = true,
                IncludePrecedingEvents = true,
                MaxPrecedingBeats = 2,
                IncludeGaps = true
            };
        }

        if (elementType == StoryItemType.Character)
        {
            return new ContextSpec
            {
                IncludeStoryConstraints = true,
                IncludeBeatHierarchy = false,
                IncludeCharacterContext = false,
                IncludePrecedingEvents = false,
                IncludeGaps = true
            };
        }

        if (elementType == StoryItemType.StoryWorld)
        {
            return new ContextSpec
            {
                IncludeStoryConstraints = true,
                IncludeBeatHierarchy = false,
                IncludeCharacterContext = false,
                IncludePrecedingEvents = false,
                IncludeGaps = true
            };
        }

        return ContextSpec.Default;
    }

    /// <summary>
    /// Check if a workflow should receive any context enrichment.
    /// </summary>
    public bool ShouldEnrichContext(string workflowLabel)
    {
        return true;
    }
}
