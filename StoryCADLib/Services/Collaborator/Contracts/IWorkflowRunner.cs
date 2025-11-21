namespace StoryCADLib.Services.Collaborator.Contracts;

/// <summary>
///     Public interface for workflow execution
///     NOTE: Methods commented out pending removal of this interface.
///     The dependent types (WorkflowModel, WorkflowResult, WorkflowValidation) have been removed.
/// </summary>
public interface IWorkflowRunner
{
    // Methods commented out because their dependent types have been removed
    // This interface is not currently used and is pending deletion

    // Task<WorkflowResult> RunAsync(WorkflowModel workflow, StoryElement element, object viewModel);
    // WorkflowValidation ValidateOutput(string jsonOutput, WorkflowModel workflow);
}
