using StoryCADLib.Models.Tools;

namespace StoryCADLib.Services;

/// <summary>
///     This service provides the users preferences.
/// </summary>
public class PreferenceService
{
    /// <summary>
    ///     User preferences model that's currently loaded.
    /// </summary>
    public PreferencesModel Model = new();
}
