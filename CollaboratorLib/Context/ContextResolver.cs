using StoryCADLib.Models;

namespace CollaboratorLib.Context;

/// <summary>
/// Determines what context to gather based on workflow and element type.
/// Context is inferred from current state - no workflow coordination or enforced ordering.
/// </summary>
public class ContextResolver
{
    // Workflow labels that need full context
    private static readonly HashSet<string> FullContextWorkflows = new(StringComparer.OrdinalIgnoreCase)
    {
        "GMC",
        "Critique"
    };

    // Workflow labels that only need story constraints
    private static readonly HashSet<string> MinimalContextWorkflows = new(StringComparer.OrdinalIgnoreCase)
    {
        "Premise"
    };

    /// <summary>
    /// Get the context specification for a workflow and element type combination.
    /// </summary>
    /// <param name="workflowLabel">The workflow being executed (e.g., "GMC", "Premise")</param>
    /// <param name="elementType">The type of element being processed</param>
    /// <returns>ContextSpec indicating what context to gather</returns>
    public ContextSpec GetContextFor(string workflowLabel, StoryItemType elementType)
    {
        // GMC workflow on Problem needs full context for temporal awareness
        if (FullContextWorkflows.Contains(workflowLabel) && elementType == StoryItemType.Problem)
        {
            return ContextSpec.Full;
        }

        // Premise workflow only needs story constraints
        if (MinimalContextWorkflows.Contains(workflowLabel))
        {
            return ContextSpec.Default;
        }

        // Scene workflows need character and setting context
        if (elementType == StoryItemType.Scene)
        {
            return new ContextSpec
            {
                IncludeStoryConstraints = true,
                IncludeBeatHierarchy = false,
                IncludeCharacterContext = true,
                IncludePrecedingEvents = true,
                MaxPrecedingBeats = 2
            };
        }

        // Character workflows need relationship context (future enhancement)
        if (elementType == StoryItemType.Character)
        {
            return new ContextSpec
            {
                IncludeStoryConstraints = true,
                IncludeBeatHierarchy = false,
                IncludeCharacterContext = false, // Don't include self
                IncludePrecedingEvents = false
            };
        }

        // Default: story constraints only
        return ContextSpec.Default;
    }

    /// <summary>
    /// Check if a workflow should receive any context enrichment.
    /// </summary>
    public bool ShouldEnrichContext(string workflowLabel)
    {
        // All workflows get at least story constraints
        return true;
    }
}
