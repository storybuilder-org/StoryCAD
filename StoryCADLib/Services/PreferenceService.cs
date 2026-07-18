using StoryCADLib.DAL;
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

    /// <summary>
    ///     Explicit startup step (issue #90 D8 as amended): if <see cref="PreferencesModel.StoreUserGuid" />
    ///     is empty, generates a GUID and persists it via <see cref="PreferencesIo.WritePreferences" /> as
    ///     its own serialized operation, so the GUID exists before any purchase or activation attempt.
    ///     Called once, right after <see cref="PreferencesIo.ReadPreferences" /> in the startup path;
    ///     <c>ReadPreferences()</c> itself stays a pure read and never triggers this.
    /// </summary>
    /// <param name="writePreferences">
    ///     Injectable write delegate (defaults to <c>new PreferencesIo().WritePreferences</c>), so tests
    ///     can verify persistence without a second disk-writing path.
    /// </param>
    public async Task EnsureUserGuidProvisionedAsync(Func<PreferencesModel, Task> writePreferences = null)
    {
        if (!string.IsNullOrEmpty(Model.StoreUserGuid))
        {
            return;
        }

        Model.StoreUserGuid = Guid.NewGuid().ToString();
        writePreferences ??= new PreferencesIo().WritePreferences;
        await writePreferences(Model);
    }
}
