#pragma warning disable CS8632 // Nullable annotations used without nullable context
namespace StoryCADLib.Services.Collaborator.Contracts;

/// <summary>
///     Simplified interface for Collaborator plugin functionality.
///     The plugin is self-contained and handles all workflow operations internally.
/// </summary>
public interface ICollaborator
{
    /// <summary>
    ///     Creates and returns the Collaborator window.
    ///     The plugin handles all workflow selection, processing, and data updates internally.
    /// </summary>
    /// <param name="context">The IStoryCADAPI instance for data access</param>
    /// <returns>The created Window instance that hosts the Collaborator UI</returns>
    Window CreateWindow(object? context);

    /// <summary>
    ///     Disposes of resources used by the Collaborator plugin.
    /// </summary>
    void Dispose();
}
