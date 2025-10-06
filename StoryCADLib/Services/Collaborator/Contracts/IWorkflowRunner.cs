namespace StoryCADLib.Services.Collaborator.Contracts;

/// <summary>
///     Public interface for workflow execution
/// </summary>
public interface IWorkflowRunner
{
    /// <summary>
    ///     Runs a workflow asynchronously
    /// </summary>
    /// <param name="workflow">The workflow model to execute</param>
    /// <param name="element">The story element being processed</param>
    /// <param name="viewModel">The view model context</param>
    /// <returns>The result of the workflow execution</returns>
    Task<WorkflowResult> RunAsync(WorkflowModel workflow, StoryElement element, object viewModel);

    /// <summary>
    ///     Validates the output of a workflow
    /// </summary>
    /// <param name="jsonOutput">The JSON output to validate</param>
    /// <param name="workflow">The workflow model for validation context</param>
    /// <returns>Validation result</returns>
    WorkflowValidation ValidateOutput(string jsonOutput, WorkflowModel workflow);
}
