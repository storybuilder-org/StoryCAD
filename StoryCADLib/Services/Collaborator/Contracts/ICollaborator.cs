#pragma warning disable CS8632 // Nullable annotations used without nullable context
using StoryCADLib.Models;
using StoryCADLib.Services.API;

namespace StoryCADLib.Services.Collaborator.Contracts;

/// <summary>
///     Simplified interface for Collaborator plugin functionality.
///     The plugin is self-contained and handles all workflow operations internally.
/// </summary>
public interface ICollaborator
{
    /// <summary>
///     Opens a Collaborator session for the provided story.
/// </summary>
/// <param name="api">API surfaced to the plugin for interacting with StoryCAD data</param>
/// <param name="model">The story model Collaborator should operate on</param>
/// <returns>The window hosting Collaborator's UI</returns>
Task<Window> OpenAsync(IStoryCADAPI api, StoryModel model);

/// <summary>
///     Signals that the host is closing Collaborator and retrieves a session summary.
/// </summary>
CollaboratorResult Close();

/// <summary>
///     Disposes of resources used by the Collaborator plugin.
/// </summary>
void Dispose();
}
