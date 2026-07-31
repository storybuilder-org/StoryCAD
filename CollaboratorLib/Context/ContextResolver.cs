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
    /// High-tier character craft: problem stakes (GMC) in StoryContext.
    /// </summary>
    private static readonly HashSet<string> CharacterHighDetailWorkflows = new(StringComparer.OrdinalIgnoreCase)
    {
        "Flaw",
        "Backstory",
        "RoleAndStoryRole"
    };

    /// <summary>
    /// Medium-tier character craft: links (and GMC when useful).
    /// </summary>
    private static readonly HashSet<string> CharacterMediumDetailWorkflows = new(StringComparer.OrdinalIgnoreCase)
    {
        "PsychologicalMakeup",
        "Relationship"
    };

    /// <summary>
    /// Get the context specification for a workflow and element type combination.
    /// </summary>
    public ContextSpec GetContextFor(string workflowLabel, StoryItemType elementType)
    {
        if (FullContextWorkflows.Contains(workflowLabel) && elementType == StoryItemType.Problem)
        {
            return ContextSpec.Full;
        }

        // Premise: constraints only (omit cast map to keep ideation prompts light)
        if (MinimalContextWorkflows.Contains(workflowLabel))
        {
            return new ContextSpec
            {
                IncludeStoryConstraints = true,
                IncludeCastProblemMap = false,
                CharacterProblemDetail = CharacterProblemDetail.None
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
                IncludeCastProblemMap = true,
                CharacterProblemDetail = CharacterProblemDetail.None
            };
        }

        if (elementType == StoryItemType.Character)
        {
            var detail = CharacterProblemDetail.LinksOnly;
            if (CharacterHighDetailWorkflows.Contains(workflowLabel))
                detail = CharacterProblemDetail.LinksAndGmc;
            else if (CharacterMediumDetailWorkflows.Contains(workflowLabel))
                detail = CharacterProblemDetail.LinksAndGmc;
            // PhysicalAppearance, SocialFactors, InnerOuterTraits stay LinksOnly

            return new ContextSpec
            {
                IncludeStoryConstraints = true,
                IncludeBeatHierarchy = false,
                IncludeCharacterContext = false,
                IncludePrecedingEvents = false,
                IncludeCastProblemMap = true,
                CharacterProblemDetail = detail
            };
        }

        // Default: constraints + spine map (problem/setting and other targets)
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
