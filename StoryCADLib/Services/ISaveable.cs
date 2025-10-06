namespace StoryCADLib.Services;

/// <summary>
///     Interface for ViewModels that can save their edits back to the Model.
///     Implemented by element ViewModels (Character, Problem, Scene, etc.) that need
///     to flush UI edits before save operations.
/// </summary>
public interface ISaveable
{
    /// <summary>
    ///     Flushes ViewModel edits to the underlying Model.
    ///     Called before save operations to ensure all UI changes are persisted.
    /// </summary>
    void SaveModel();
}
