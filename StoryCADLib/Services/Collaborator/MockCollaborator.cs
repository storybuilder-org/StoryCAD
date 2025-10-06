using StoryCADLib.Services.Collaborator.Contracts;

namespace StoryCADLib.Services.Collaborator;

/// <summary>
///     Mock implementation of ICollaborator for testing purposes.
///     This can be used when the actual CollaboratorLib DLL is not available.
/// </summary>
public class MockCollaborator : ICollaborator
{
    private readonly ILogService _logger;
    private StoryElement _currentElement;
    private StoryItemType _currentElementType;
    private string _currentWorkflow;

    public MockCollaborator(ILogService logger)
    {
        _logger = logger;
        _logger?.Log(LogLevel.Info, "MockCollaborator initialized");
    }

    /// <summary>
    ///     Creates a mock window for testing
    /// </summary>
    public Window CreateWindow(object context)
    {
        _logger?.Log(LogLevel.Info, "MockCollaborator.CreateWindow called");

        // In testing/mock mode, we don't actually create a window
        // Return null or a dummy window depending on testing needs
        return null;
    }

    /// <summary>
    ///     Mock implementation of LoadWorkflowViewModel
    /// </summary>
    public void LoadWorkflowViewModel(StoryItemType elementType)
    {
        _logger?.Log(LogLevel.Info, $"MockCollaborator.LoadWorkflowViewModel called with elementType: {elementType}");
        _currentElementType = elementType;

        // Mock implementation: Just log the call
        // In a real mock, this might set up test data
    }

    /// <summary>
    ///     Mock implementation of LoadWizardViewModel
    /// </summary>
    public void LoadWizardViewModel()
    {
        _logger?.Log(LogLevel.Info, "MockCollaborator.LoadWizardViewModel called");

        // Mock implementation: Just log the call
    }

    /// <summary>
    ///     Mock implementation of LoadWorkflowModel
    /// </summary>
    public void LoadWorkflowModel(StoryElement element, string workflow)
    {
        _logger?.Log(LogLevel.Info,
            $"MockCollaborator.LoadWorkflowModel called with element: {element?.Name}, workflow: {workflow}");
        _currentElement = element;
        _currentWorkflow = workflow;

        // Mock implementation: Store the parameters for later use
    }

    /// <summary>
    ///     Mock implementation of ProcessWorkflowAsync
    /// </summary>
    public async Task ProcessWorkflowAsync()
    {
        _logger?.Log(LogLevel.Info, "MockCollaborator.ProcessWorkflowAsync called");

        // Simulate some async work
        await Task.Delay(100);

        // Mock implementation: Could update _currentElement with test data
        if (_currentElement != null)
        {
            _logger?.Log(LogLevel.Info, $"Mock processing workflow for element: {_currentElement.Name}");
        }
    }

    /// <summary>
    ///     Mock implementation of SendButtonClickedAsync
    /// </summary>
    public async Task SendButtonClickedAsync()
    {
        _logger?.Log(LogLevel.Info, "MockCollaborator.SendButtonClickedAsync called");

        // Simulate some async work
        await Task.Delay(50);

        // Mock implementation: Simulate sending data
        _logger?.Log(LogLevel.Info, "Mock send operation completed");
    }

    /// <summary>
    ///     Mock implementation of SaveOutputs
    /// </summary>
    public void SaveOutputs()
    {
        _logger?.Log(LogLevel.Info, "MockCollaborator.SaveOutputs called");

        // Mock implementation: Simulate saving outputs
        if (_currentElement != null)
        {
            _logger?.Log(LogLevel.Info, $"Mock saving outputs for element: {_currentElement.Name}");
        }
    }

    /// <summary>
    ///     Helper method to get current state (for testing)
    /// </summary>
    public (StoryElement element, string workflow, StoryItemType elementType) GetCurrentState()
    {
        return (_currentElement, _currentWorkflow, _currentElementType);
    }
}
