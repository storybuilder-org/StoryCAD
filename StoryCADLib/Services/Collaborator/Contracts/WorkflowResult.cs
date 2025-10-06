namespace StoryCADLib.Services.Collaborator.Contracts;

/// <summary>
///     Result of workflow execution
/// </summary>
public class WorkflowResult
{
    /// <summary>
    ///     Whether the workflow executed successfully
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    ///     Message describing the result or any errors
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    ///     Story elements that were updated by the workflow
    /// </summary>
    public List<StoryElement> UpdatedElements { get; set; } = new();

    /// <summary>
    ///     New story elements created by the workflow
    /// </summary>
    public List<StoryElement> NewElements { get; set; } = new();
}
