using Microsoft.Extensions.Logging;
using StoryCADLib.Models;
using StoryCADLib.Services.Collaborator.Contracts;
using StoryCollaborator.Models;
using StoryCollaborator.Workflows;

namespace StoryCollaborator.Services;

/// <summary>
/// Manages workflow execution and state.
/// </summary>
public class WorkflowService
{
    private readonly ILogger<WorkflowService> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly SessionService _sessionService;

    private StoryModel? _storyModel;
    private IStoryCADAPI? _storyApi;
    private Workflow? _currentWorkflow;
    private StoryElement? _currentElement;

    public WorkflowService(ILogger<WorkflowService> logger, ILoggerFactory loggerFactory, SessionService sessionService)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
        _sessionService = sessionService;
    }

    /// <summary>
    /// Gets the current workflow being executed.
    /// </summary>
    public Workflow? CurrentWorkflow => _currentWorkflow;

    /// <summary>
    /// Gets the current story element being processed.
    /// </summary>
    public StoryElement? CurrentElement => _currentElement;

    /// <summary>
    /// Sets the story context for workflow execution.
    /// </summary>
    public void SetContext(IStoryCADAPI api, StoryModel model)
    {
        _storyApi = api ?? throw new ArgumentNullException(nameof(api));
        _storyModel = model ?? throw new ArgumentNullException(nameof(model));
    }

    /// <summary>
    /// Loads a workflow by its label and sets the target element.
    /// </summary>
    public void LoadWorkflow(string workflowLabel, StoryElement element)
    {
        _currentElement = element;
        _currentWorkflow = WorkflowRegistry.Get(workflowLabel);

        if (_currentWorkflow == null)
        {
            _logger?.LogWarning("Workflow not found: {WorkflowLabel}", workflowLabel);
            return;
        }

        _logger?.LogInformation("Workflow loaded: {WorkflowTitle}", _currentWorkflow.Title);
        _sessionService?.RecordMessage($"Workflow loaded: {_currentWorkflow.Title}");
    }

    /// <summary>
    /// Executes the current workflow with pre-gathered elements.
    /// </summary>
    /// <param name="gatheredElements">Elements gathered by Collaborator before execution</param>
    /// <returns>WorkflowResult with success status and messages</returns>
    public async Task<WorkflowResult> ExecuteWorkflowAsync(Dictionary<string, StoryElement> gatheredElements)
    {
        if (_currentWorkflow == null)
        {
            _logger?.LogWarning("No workflow loaded to execute");
            return WorkflowResult.Failed("No workflow loaded");
        }

        if (_storyModel == null || _storyApi == null)
        {
            _logger?.LogWarning("Story context not set - call SetContext first");
            return WorkflowResult.Failed("Story context not set");
        }

        if (gatheredElements == null)
        {
            _logger?.LogWarning("No gathered elements provided");
            return WorkflowResult.Failed("No gathered elements provided");
        }

        try
        {
            var runnerLogger = _loggerFactory?.CreateLogger<WorkflowRunner>();
            var workflowRunner = new WorkflowRunner(_storyModel, _currentWorkflow, _storyApi, runnerLogger);
            var result = await workflowRunner.RunAsync(gatheredElements);

            if (result.Success)
            {
                _logger?.LogInformation("Workflow executed: {WorkflowTitle}", _currentWorkflow.Title);
                _sessionService?.RecordMessage($"Workflow executed: {_currentWorkflow.Title}");
            }
            else
            {
                _logger?.LogWarning("Workflow failed: {WorkflowTitle} - {Error}",
                    _currentWorkflow.Title, result.ErrorMessage);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Workflow execution failed: {WorkflowTitle}", _currentWorkflow.Title);
            return WorkflowResult.Failed($"Execution exception: {ex.Message}");
        }
    }

    /// <summary>
    /// Executes the current workflow without pre-gathered elements (legacy support).
    /// </summary>
    [Obsolete("Use ExecuteWorkflowAsync(Dictionary<string, StoryElement>) with pre-gathered elements")]
    public async Task ExecuteWorkflowAsync()
    {
        // Legacy method - doesn't actually execute, just logs
        if (_currentWorkflow == null)
        {
            _logger?.LogWarning("No workflow loaded to execute");
            return;
        }

        _logger?.LogWarning("ExecuteWorkflowAsync() called without gathered elements - use overload with Dictionary<string, StoryElement>");
        await Task.CompletedTask;
    }

    /// <summary>
    /// Processes a chat message in the context of the current workflow.
    /// </summary>
    public async Task<string> ProcessChatAsync(string userMessage)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
            return string.Empty;

        // TODO: Implement chat processing when Semantic Kernel integration is complete
        await Task.CompletedTask;
        return "Chat processing not yet implemented";
    }

    /// <summary>
    /// Saves workflow outputs to the story elements.
    /// </summary>
    public void SaveOutputs()
    {
        if (_currentWorkflow == null)
        {
            _logger?.LogWarning("No workflow loaded - nothing to save");
            return;
        }

        // TODO: Implement saving outputs to story elements
        _sessionService?.RecordMessage($"Outputs saved for workflow: {_currentWorkflow.Title}");
    }

    /// <summary>
    /// Clears the current workflow state.
    /// </summary>
    public void ClearWorkflow()
    {
        _currentWorkflow = null;
        _currentElement = null;
    }
}
