using Microsoft.UI.Xaml;
using StoryCAD.Models;
using System.Threading.Tasks;

namespace StoryCAD.Services.Collaborator.Contracts;

/// <summary>
/// Public interface for Collaborator plugin functionality.
/// Implemented by external plugins to provide AI-assisted workflows.
/// </summary>
public interface ICollaborator
{
    /// <summary>
    /// Creates the Collaborator window with the provided context (typically the API)
    /// </summary>
    /// <param name="context">Context object containing API and other initialization data</param>
    /// <returns>The created Window instance</returns>
    Window CreateWindow(object? context);

    /// <summary>
    /// Loads the workflow view model for a specific element type
    /// </summary>
    /// <param name="elementType">The type of story element to create workflow for</param>
    void LoadWorkflowViewModel(StoryItemType elementType);

    /// <summary>
    /// Loads the wizard view model for guided story creation
    /// </summary>
    void LoadWizardViewModel();

    /// <summary>
    /// Loads a workflow model for a specific element and workflow
    /// </summary>
    /// <param name="element">The story element to work with</param>
    /// <param name="workflow">The workflow identifier</param>
    void LoadWorkflowModel(StoryElement element, string workflow);

    /// <summary>
    /// Processes the current workflow asynchronously
    /// </summary>
    /// <returns>Task representing the async operation</returns>
    Task ProcessWorkflowAsync();

    /// <summary>
    /// Handles the send button click event asynchronously
    /// </summary>
    /// <returns>Task representing the async operation</returns>
    Task SendButtonClickedAsync();

    /// <summary>
    /// Saves the current workflow outputs to the story model
    /// </summary>
    void SaveOutputs();
}