using StoryCADLib.Collaborator.ViewModels;

namespace StoryCADLib.Services.Collaborator;

public class CollaboratorArgs
{
    //public WorkflowStepViewModel WorkflowStepVM;

    public delegate void OnDone();

    /// <summary>
    ///     Reference to the Collaborator window
    /// </summary>
    public Window CollaboratorWindow;

    public OnDone onDoneCallback;
    public StoryModel StoryModel;

    public WorkflowViewModel WorkflowVm;
}
