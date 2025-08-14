namespace StoryCAD.Services.Collaborator.Contracts;

/// <summary>
/// Validation result for workflow output
/// </summary>
public class WorkflowValidation
{
    /// <summary>
    /// Whether the output is valid
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Validation message describing any issues
    /// </summary>
    public string ValidationMessage { get; set; }
}