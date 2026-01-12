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
    ///     Opens a Collaborator session for the provided story using a host-supplied frame for navigation.
    /// </summary>
/// <param name="api">API surfaced to the plugin for interacting with StoryCAD data</param>
/// <param name="model">The story model Collaborator should operate on</param>
/// <param name="hostWindow">Host-created window for Collaborator UI</param>
/// <param name="hostFrame">Host-created frame (with region) for navigation</param>
/// <returns>The window hosting Collaborator's UI</returns>
Task<Window> OpenAsync(IStoryCADAPI api, StoryModel model, Window hostWindow, Frame hostFrame);

/// <summary>
///     Signals that the host is closing Collaborator and retrieves a session summary.
/// </summary>
CollaboratorResult Close();

/// <summary>
///     Sets Collaborator settings. Can be called before or after OpenAsync.
/// </summary>
/// <param name="settings">Settings to apply</param>
void SetSettings(CollaboratorSettings settings);

/// <summary>
///     Gets the current Collaborator settings.
/// </summary>
/// <returns>Current settings</returns>
CollaboratorSettings GetSettings();

/// <summary>
///     Disposes of resources used by the Collaborator plugin.
/// </summary>
void Dispose();
}
