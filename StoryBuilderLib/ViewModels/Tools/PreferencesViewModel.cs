using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using StoryBuilder.DAL;
using StoryBuilder.Models;
using StoryBuilder.Models.Tools;
using StoryBuilder.Services.Backend;

namespace StoryBuilder.ViewModels.Tools;

public class PreferencesViewModel : ObservableRecipient
{
    public PreferencesModel CurrentModel = GlobalData.Preferences;

    /// <summary>
    /// Saves the users preferences to disk.
    /// </summary>
    public async Task SaveAsync()
    {
        PreferencesIo _prfIo = new(CurrentModel, Path.Combine(ApplicationData.Current.RoamingFolder.Path, "Storybuilder"));
        await _prfIo.SaveModel();
        await _prfIo.LoadModel();
        GlobalData.Preferences = CurrentModel;

        BackendService _backend = Ioc.Default.GetRequiredService<BackendService>();
        GlobalData.Preferences.RecordPreferencesStatus = false;  // indicate need to update
        await _backend.PostPreferences(GlobalData.Preferences);
    }
}