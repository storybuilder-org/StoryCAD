using StoryCADLib.Services.Collaborator.Contracts;

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

    /// <summary>
    ///     Reference to the API for accessing story data
    /// </summary>
    public IStoryCADAPI StoryApi;

    // Note: WorkflowViewModel is now in Collaborator project
    // This will need to be changed to object or removed
    public object WorkflowVm;
}
