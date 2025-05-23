using System.Text.Json;
using Windows.Storage;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using StoryCAD.Models.Tools;
using StoryCAD.Services;
using Octokit;
using Application = Microsoft.UI.Xaml.Application;
using StoryCAD.Services.Backup;
using StoryCAD.Services.Locking;

namespace StoryCAD.DAL;
/// <summary>
/// Object that reads/writes StoryCAD Preference Files
/// </summary>
public class PreferencesIo
{
	//TODO: Remove this variable after June 2025.
	private IList<string> _preferences;

	private LogService _log = Ioc.Default.GetService<LogService>();
	private AppState _state = Ioc.Default.GetService<AppState>();


	public async Task<PreferencesModel> ReadPreferences()
	{
		try
		{
            var save = Ioc.Default.GetRequiredService<AutoSaveService>();
            var back = Ioc.Default.GetRequiredService<BackupService>();
            using (var serializationLock = new SerializationLock(save, back, _log))
            {
                StorageFolder _preferencesFolder = await StorageFolder.GetFolderFromPathAsync(_state.RootDirectory);

                PreferencesModel _model = new();

                //Check if we have a preferences.json
                if (File.Exists(Path.Combine(_preferencesFolder.Path, "Preferences.json")))
                {
                    //Read file into memory
                    _log.Log(LogLevel.Info, "Preferences.json found, reading it.");
                    StorageFile _preferencesFile = await _preferencesFolder.GetFileAsync("Preferences.json");
                    string _preferencesJson = await FileIO.ReadTextAsync(_preferencesFile);
                    _log.Log(LogLevel.Info, $"Preferences Contents: {_preferencesJson}");

                    //Update _model, with new values.
                    _model = JsonSerializer.Deserialize<PreferencesModel>(_preferencesJson);
                    Ioc.Default.GetRequiredService<PreferenceService>().Model = _model;
                    _log.Log(LogLevel.Info, "Preferences deserialized.");
                }
                else
                {
                    _log.Log(LogLevel.Info, "Preferences.json not found; default created.");
                }

                if (!Ioc.Default.GetRequiredService<AppState>().Headless)
                {
                    //Handle UI Theme stuff
                    Windowing window = Ioc.Default.GetRequiredService<Windowing>();
                    if (_model.ThemePreference == ElementTheme.Default)
                    {
                        window.RequestedTheme = Application.Current.RequestedTheme == ApplicationTheme.Dark
                            ? ElementTheme.Dark
                            : ElementTheme.Light;
                    }
                    else
                    {
                        window.RequestedTheme = _model.ThemePreference;
                    }

                    if (window.RequestedTheme == ElementTheme.Light)
                    {
                        window.PrimaryColor = new SolidColorBrush(Colors.LightGray);
                        window.SecondaryColor = new SolidColorBrush(Colors.Black);
                    }
                    else
                    {
                        window.PrimaryColor = new SolidColorBrush(Colors.DarkSlateGray);
                        window.SecondaryColor = new SolidColorBrush(Colors.White);
                    }
                }

                return _model;
            }
        }
		catch (Exception e)
		{
			_log.LogException(LogLevel.Error,e, 
				$"Preferences read error {e.Data} {e.StackTrace} {e.Message}");
			return new();
		}
	}

	/// <summary>
	/// This writes the file to disk using given preferences model.
	/// </summary>
	public async Task WritePreferences(PreferencesModel Model)
    {
		try
        {
            var save = Ioc.Default.GetRequiredService<AutoSaveService>();
            var back = Ioc.Default.GetRequiredService<BackupService>();
            using (var serializationLock = new SerializationLock(save, back, _log))
            {
                //Get/Create file.
                _log.Log(LogLevel.Info, "Writing preferences model to disk.");
                StorageFolder _preferencesFolder = await StorageFolder.GetFolderFromPathAsync(_state.RootDirectory);
                _log.Log(LogLevel.Info, $"Saving to folder {_preferencesFolder.Path}");

                StorageFile _preferencesFile =
                    await _preferencesFolder.CreateFileAsync("Preferences.json",
                        CreationCollisionOption.ReplaceExisting);
                //Write file
                _log.Log(LogLevel.Info, $"Saving Preferences to file {_preferencesFile.Path}");
                string _newPreferences = JsonSerializer.Serialize(Model,
                    new JsonSerializerOptions { WriteIndented = true });

                //Log stuff
                _log.Log(LogLevel.Info, $"Serialised preferences as {_newPreferences}");
                await FileIO.WriteTextAsync(_preferencesFile, _newPreferences); //Writes file to disk
                _log.Log(LogLevel.Info, "Preferences write complete.");
            }
		}
		catch (Exception ex)
		{
			_log.LogException(LogLevel.Error, ex, $"Error writing preferences: {ex.Message}");
		}
	}
}