using StoryCADLib.Models;

namespace StoryCollaborator.Models;

/// <summary>
/// Result of a workflow execution.
/// </summary>
public class WorkflowResult
{
    /// <summary>
    /// Whether the workflow executed successfully.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if execution failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Status messages collected during execution.
    /// </summary>
    public List<string> StatusMessages { get; } = new();

    /// <summary>
    /// Properties that were updated during execution.
    /// Key format: "ElementLabel.PropertyName"
    /// </summary>
    public Dictionary<string, object> UpdatedProperties { get; } = new();

    /// <summary>
    /// Typed pending updates extracted from AI response; consumed by ApplyUpdates.
    /// </summary>
    public List<PendingUpdate> PendingUpdates { get; } = new();

    /// <summary>
    /// The raw AI response text (for debugging).
    /// </summary>
    public string? RawResponse { get; set; }

    /// <summary>
    /// The assembled prompt sent to the LLM (template with values substituted).
    /// </summary>
    public string? AssembledPrompt { get; set; }

    /// <summary>
    /// SHA-256 hex of the proxy template before argument substitution, from the X-Template-Hash response header.
    /// Null when the direct fallback path was used instead of the proxy.
    /// </summary>
    public string? RemoteTemplateHash { get; set; }

    /// <summary>
    /// Cost reported by the proxy's <c>collab_cost</c> SSE event.
    /// Null when the proxy sent no cost event (old Worker, unpriced model, direct-OpenAI fallback path).
    /// </summary>
    public ProxyCostInfo? Cost { get; set; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static WorkflowResult Succeeded()
    {
        return new WorkflowResult { Success = true };
    }

    /// <summary>
    /// Creates a failed result with error message.
    /// </summary>
    public static WorkflowResult Failed(string errorMessage)
    {
        return new WorkflowResult
        {
            Success = false,
            ErrorMessage = errorMessage
        };
    }
}
