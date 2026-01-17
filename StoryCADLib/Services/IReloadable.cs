namespace StoryCADLib.Services;

/// <summary>
///     Interface for ViewModels that can reload their data from the Model.
///     Used to refresh UI after external model changes (e.g., Collaborator plugin).
/// </summary>
public interface IReloadable
{
    /// <summary>
    ///     Reloads ViewModel properties from the underlying Model.
    ///     Called after external code modifies the Model directly.
    /// </summary>
    void ReloadFromModel();
}
