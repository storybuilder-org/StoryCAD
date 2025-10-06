using System.Text.Json;
using Microsoft.UI;
using StoryCADLib.Models.Tools;
using StoryCADLib.Services;
using StoryCADLib.Services.Backup;
using StoryCADLib.Services.Locking;
using Application = Microsoft.UI.Xaml.Application;

namespace StoryCADLib.DAL;

/// <summary>
///     Object that reads/writes StoryCAD Preference Files
/// </summary>
public class PreferencesIo
{
    private readonly AppState _appState;
    private readonly AutoSaveService _autoSaveService;
    private readonly BackupService _backupService;
    private readonly ILogService _log;
    private readonly PreferenceService _preferenceService;
    private readonly Windowing _windowing;

    public PreferencesIo(ILogService log, AppState appState, AutoSaveService autoSaveService,
        BackupService backupService, PreferenceService preferenceService, Windowing windowing)
    {
        _log = log;
        _appState = appState;
        _autoSaveService = autoSaveService;
        _backupService = backupService;
        _preferenceService = preferenceService;
        _windowing = windowing;
    }

    // Constructor for backward compatibility - will be removed later
    public PreferencesIo() : this(
        Ioc.Default.GetService<ILogService>(),
        Ioc.Default.GetService<AppState>(),
        Ioc.Default.GetRequiredService<AutoSaveService>(),
        Ioc.Default.GetRequiredService<BackupService>(),
        Ioc.Default.GetRequiredService<PreferenceService>(),
        Ioc.Default.GetRequiredService<Windowing>())
    {
    }

    public async Task<PreferencesModel> ReadPreferences()
    {
        try
        {
            using (var serializationLock = new SerializationLock(_log))
            {
                var _preferencesFolder = await StorageFolder.GetFolderFromPathAsync(_appState.RootDirectory);

                PreferencesModel _model = new();

                //Check if we have a preferences.json
                if (File.Exists(Path.Combine(_preferencesFolder.Path, "Preferences.json")))
                {
                    //Read file into memory
                    _log.Log(LogLevel.Info, "Preferences.json found, reading it.");
                    var _preferencesFile = await _preferencesFolder.GetFileAsync("Preferences.json");
                    var _preferencesJson = await FileIO.ReadTextAsync(_preferencesFile);
                    _log.Log(LogLevel.Info, $"Preferences Contents: {_preferencesJson}");

                    //Update _model, with new values.
                    _model = JsonSerializer.Deserialize<PreferencesModel>(_preferencesJson);
                    _preferenceService.Model = _model;
                    _log.Log(LogLevel.Info, "Preferences deserialized.");
                }
                else
                {
                    _log.Log(LogLevel.Info, "Preferences.json not found; default created.");
                }

                if (!_appState.Headless)
                {
                    //Handle UI Theme stuff
                    var window = _windowing;
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
            _log.LogException(LogLevel.Error, e,
                $"Preferences read error {e.Data} {e.StackTrace} {e.Message}");
            return new PreferencesModel();
        }
    }

    /// <summary>
    ///     This writes the file to disk using given preferences model.
    /// </summary>
    public async Task WritePreferences(PreferencesModel Model)
    {
        try
        {
            using (var serializationLock = new SerializationLock(_log))
            {
                //Get/Create file.
                _log.Log(LogLevel.Info, "Writing preferences model to disk.");
                var _preferencesFolder = await StorageFolder.GetFolderFromPathAsync(_appState.RootDirectory);
                _log.Log(LogLevel.Info, $"Saving to folder {_preferencesFolder.Path}");

                var _preferencesFile =
                    await _preferencesFolder.CreateFileAsync("Preferences.json",
                        CreationCollisionOption.ReplaceExisting);
                //Write file
                _log.Log(LogLevel.Info, $"Saving Preferences to file {_preferencesFile.Path}");
                var _newPreferences = JsonSerializer.Serialize(Model,
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
