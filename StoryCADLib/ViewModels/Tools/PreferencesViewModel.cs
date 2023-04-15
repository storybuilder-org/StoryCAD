using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCAD.DAL;
using StoryCAD.Models;
using StoryCAD.Models.Tools;
using StoryCAD.Services.Backend;

namespace StoryCAD.ViewModels.Tools;

public class PreferencesViewModel : ObservableRecipient
{
    public PreferencesModel CurrentModel = GlobalData.Preferences;

    /// <summary>
    /// Saves the users preferences to disk.
    /// </summary>
    public async Task SaveAsync()
    {
        PreferencesIo _prfIo = new(CurrentModel, Path.Combine(ApplicationData.Current.RoamingFolder.Path, "StoryCAD"));
        await _prfIo.SaveModel();
        await _prfIo.LoadModel();
        GlobalData.Preferences = CurrentModel;

        BackendService _backend = Ioc.Default.GetRequiredService<BackendService>();
        GlobalData.Preferences.RecordPreferencesStatus = false;  // indicate need to update
        await _backend.PostPreferences(GlobalData.Preferences);
    }
}