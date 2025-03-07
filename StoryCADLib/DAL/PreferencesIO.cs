using System.Text.Json;
using Windows.Storage;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using StoryCAD.Models.Tools;
using StoryCAD.Services;
using Octokit;
using Application = Microsoft.UI.Xaml.Application;

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
			StorageFolder _preferencesFolder = await StorageFolder.GetFolderFromPathAsync(_state.RootDirectory);

			//TODO: Remove this if block after June 2025.
			//Legacy read and migration code.
			if (File.Exists(Path.Combine(_state.RootDirectory, "StoryCAD.prf")))
			{
				_log.Log(LogLevel.Info, "StoryCAD.prf file found, migrating preferences");

				//Read old preferences file
				PreferencesModel _OldModel = await ReadOldPreferences();
				_log.Log(LogLevel.Info, "StoryCAD.prf read");

				//Delete old preferences file
				StorageFile _preferencesFile = await _preferencesFolder.GetFileAsync("StoryCAD.prf");
				await _preferencesFile.DeleteAsync();
				_log.Log(LogLevel.Info, "StoryCAD.prf deleted, writing Preferences.json");
				await WritePreferences(_OldModel);
			}

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

			if (!Ioc.Default.GetRequiredService<AppState>().StoryCADTestsMode)
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
		catch (Exception e)
		{
			_log.LogException(LogLevel.Error,e, 
				$"Preferences read error {e.Data} {e.StackTrace} {e.Message}");
			return new();
		}
	}

	/// <summary>
	/// Update the model from the .prf file's contents
	/// Deprecated, 
	/// TODO: Remove this function on or after June 2025 as its deprecated.
	/// </summary>
	private async Task<PreferencesModel> ReadOldPreferences()
	{
		//Tries to read file
		StorageFolder _preferencesFolder = await StorageFolder.GetFolderFromPathAsync(_state.RootDirectory);
		IStorageFile _preferencesFile = (IStorageFile)await _preferencesFolder.TryGetItemAsync("StoryCAD.prf");
		PreferencesModel _model = new();
		if (_preferencesFile != null) //Checks if file exists
		{
			_preferences = await FileIO.ReadLinesAsync(_preferencesFile);

			//Update the model from the file
			//TODO: Use bool.parse.
			foreach (string _line in _preferences)
			{
				string[] _tokens = _line.Split(new[] { '=' });
				switch (_tokens[0])
				{
					case "FirstName":
						_model.FirstName = _tokens[1];
						break;
					case "LastName":
						_model.LastName = _tokens[1];
						break;
					case "Email":
						_model.Email = _tokens[1];
						break;

					case "Initalised":
						if (_tokens[1] == "True")
						{
							_model.PreferencesInitialized = true;
						}
						else
						{
							_model.PreferencesInitialized = false;
						}

						break;

					case "ErrorCollectionConsent":
						if (_tokens[1] == "True")
						{
							_model.ErrorCollectionConsent = true;
						}
						else
						{
							_model.ErrorCollectionConsent = false;
						}

						break;

					case "Newsletter":
						if (_tokens[1] == "True")
							_model.Newsletter = true;
						else
							_model.Newsletter = false;
						break;

					case "BackupOnOpen":
						if (_tokens[1] == "True")
							_model.BackupOnOpen = true;
						else
							_model.BackupOnOpen = false;
						break;

					case "TimedBackup":
						if (_tokens[1] == "True")
						{
							_model.TimedBackup = true;
						}
						else
						{
							_model.TimedBackup = false;
						}

						break;

					case "TimedBackupInterval":
						_model.TimedBackupInterval = Convert.ToInt32(_tokens[1]);
						break;

					case "ProjectDirectory":
						_model.ProjectDirectory = _tokens[1];
						break;
					case "BackupDirectory":
						_model.BackupDirectory = _tokens[1];
						break;
					case "LastFile1":
						_model.LastFile1 = _tokens[1];
						break;
					case "LastFile2":
						_model.LastFile2 = _tokens[1];
						break;
					case "LastFile3":
						_model.LastFile3 = _tokens[1];
						break;
					case "LastFile4":
						_model.LastFile4 = _tokens[1];
						break;
					case "LastFile5":
						_model.LastFile5 = _tokens[1];
						break;
					case "LastTemplate":
						_model.LastSelectedTemplate = Convert.ToInt32(_tokens[1]);
						break;
					case "Version":
						_model.Version = _tokens[1];
						break;
					case "RecordPreferencesStatus":
						if (_tokens[1] == "True")
							_model.RecordPreferencesStatus = true;
						else
							_model.RecordPreferencesStatus = false;
						break;
					case "RecordVersionStatus":
						if (_tokens[1] == "True")
							_model.RecordVersionStatus = true;
						else
							_model.RecordVersionStatus = false;
						break;
					case "WrapNodeNames":
						if (_tokens[1] == "True")
						{
							_model.WrapNodeNames = TextWrapping.WrapWholeWords;
						}
						else
						{
							_model.WrapNodeNames = TextWrapping.NoWrap;
						}

						break;
					case "AutoSave":
						if (_tokens[1] == "True")
						{
							_model.AutoSave = true;
						}
						else
						{
							_model.AutoSave = false;
						}

						break;
					case "AutoSaveInterval":
						_model.AutoSaveInterval = Convert.ToInt32(_tokens[1]);
						break;
					case "SearchEngine":
						object val;
						Enum.TryParse(typeof(BrowserType), _tokens[1].ToCharArray(), true, out val);
						_model.PreferredSearchEngine = (BrowserType)val;
						break;
					case "Theme":
						_model.ThemePreference = (ElementTheme)(Convert.ToInt16(_tokens[1]));
						break;
					case "DontAskForReview":
						if (_tokens[1] == "True")
						{
							_model.HideRatingPrompt = true;
						}
						else
						{
							_model.HideRatingPrompt = false;
						}

						break;
					case "CummulativeTimeUsed":
						_model.CumulativeTimeUsed = Convert.ToInt32(_tokens[1]);
						break;
					case "LastReviewDate":
						_model.LastReviewDate = Convert.ToDateTime(_tokens[1]);
						break;
					case "AdvancedLogging":
						_model.AdvancedLogging = _tokens[1] == "True";
						break;
					case "StartupPage":
						_model.ShowStartupDialog = _tokens[1] == "True";
						break;
				}
			}

			_log.Log(LogLevel.Info, "PreferencesModel updated from StoryCAD.prf.");
		}
		else
		{
			// The file doesn't exist.
			_log.Log(LogLevel.Info, "StoryCAD.prf not found; default created.");
		}

		return _model;
	}

	/// <summary>
	/// This writes the file to disk using given preferences model.
	/// </summary>
	public async Task WritePreferences(PreferencesModel Model)
	{
		try
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
				new JsonSerializerOptions{ WriteIndented = true});

			//Log stuff
			_log.Log(LogLevel.Info, $"Serialised preferences as {_newPreferences}");
			await FileIO.WriteTextAsync(_preferencesFile, _newPreferences); //Writes file to disk.
			_log.Log(LogLevel.Info, "Preferences write complete.");
		}
		catch (Exception ex)
		{
			_log.LogException(LogLevel.Error, ex, $"Error writing preferences: {ex.Message}");
		}
	}
}