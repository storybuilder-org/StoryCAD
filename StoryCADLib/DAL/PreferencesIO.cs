using System.Text.Json;
using Microsoft.UI;
using StoryCADLib.Models.Tools;
using StoryCADLib.Services;
using StoryCADLib.Services.Locking;
using Application = Microsoft.UI.Xaml.Application;

namespace StoryCADLib.DAL;

/// <summary>
///     Object that reads/writes StoryCAD Preference Files
/// </summary>
public class PreferencesIo
{
    private readonly AppState _appState;
    private readonly ILogService _log;
    private readonly PreferenceService _preferenceService;
    private readonly Windowing _windowing;
    private string PreferencesFilePath => Path.Combine(_appState.RootDirectory, "Preferences.json");

    private PreferencesIo(ILogService log, AppState appState, PreferenceService preferenceService, Windowing windowing)
    {
        _log = log;
        _appState = appState;
        _preferenceService = preferenceService;
        _windowing = windowing;
    }

    //TODO: Constructor for backward compatibility - will be removed later
    public PreferencesIo() : this(
        Ioc.Default.GetService<ILogService>(),
        Ioc.Default.GetService<AppState>(),
        Ioc.Default.GetRequiredService<PreferenceService>(),
        Ioc.Default.GetRequiredService<Windowing>())
    {
    }

    public async Task<PreferencesModel> ReadPreferences()
    {
        try
        {
            PreferencesModel _model = null;
            await SerializationLock.RunExclusiveAsync(async _ =>
            {
                //Check if we have a preferences.json
                if (File.Exists(PreferencesFilePath))
                {
                    //Read file into memory
                    _log.Log(LogLevel.Info, "Preferences.json found, reading it.");
                    StorageFile _preferencesFile = await StorageFile.GetFileFromPathAsync(PreferencesFilePath);
                    var _preferencesJson = await FileIO.ReadTextAsync(_preferencesFile);
                    _log.Log(LogLevel.Debug, $"Preferences Contents: {_preferencesJson}");

                    //Update _model, with new values.
                    _model = JsonSerializer.Deserialize<PreferencesModel>(_preferencesJson);

                    // TODO: Remove this migration in May 2026
                    // Migrate WrapWholeWords to Wrap (WrapWholeWords doesn't break long words)
                    if (_model.WrapNodeNames == TextWrapping.WrapWholeWords)
                    {
                        _model.WrapNodeNames = TextWrapping.Wrap;
                    }

                    _preferenceService.Model = _model;
                    _log.Log(LogLevel.Info, "Preferences deserialized.");
                }
                else
                {
                    _model = new();
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
            }, CancellationToken.None, _log);

            return _model;
        }
        catch (Exception e)
        {
            _log.LogException(LogLevel.Error, e, $"Preferences read error {e.Data} {e.StackTrace} {e.Message}");
            return new PreferencesModel();
        }
    }

    /// <summary>
    ///     This writes the file to disk using given preferences model.
    /// </summary>
    public async Task WritePreferences(PreferencesModel _model)
    {
        try
        {
            await SerializationLock.RunExclusiveAsync(async _ =>
            {
                //Write file
                _log.Log(LogLevel.Info, $"Saving Preferences to file {PreferencesFilePath}");
                var _newPreferences = JsonSerializer.Serialize(_model, new JsonSerializerOptions { WriteIndented = true });

                StorageFolder _localFolder = await StorageFolder.GetFolderFromPathAsync(_appState.RootDirectory);
                StorageFile _preferencesFile = await _localFolder.CreateFileAsync("Preferences.json",
                    CreationCollisionOption.OpenIfExists);

                //Log and write.
                _log.Log(LogLevel.Debug, $"Serialised preferences as {_newPreferences}");
                await FileIO.WriteTextAsync(_preferencesFile, _newPreferences); //Writes file to disk
                _log.Log(LogLevel.Info, "Preferences write complete.");
            }, CancellationToken.None, _log);
        }
        catch (Exception ex)
        {
            _log.LogException(LogLevel.Error, ex, $"Error writing preferences: {ex.Message}");
        }
    }
}
