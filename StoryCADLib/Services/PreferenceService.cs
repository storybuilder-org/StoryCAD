using StoryCAD.DAL;
using StoryCAD.Models.Tools;
using Microsoft.UI.Xaml;

namespace StoryCAD.Services;

/// <summary>
/// This service provides the users preferences.
/// </summary>
public class PreferenceService
{

	/// <summary>
	/// User preferences model that's currently loaded.
	/// </summary>
	public PreferencesModel Model = new();


	/// <summary>
	/// Loads the users preferences from the default location.
	/// </summary>
	public async Task LoadPreferences()
	{
		LogService Logger = Ioc.Default.GetService<LogService>();
		try
		{
			Logger.Log(LogLevel.Info, "Loading Preferences");
			PreferencesIo loader = new(Model, Ioc.Default.GetRequiredService<AppState>().RootDirectory);
			await loader.ReadPreferences();
			Logger.Log(LogLevel.Info, "Loading Preferences");
		}
		catch (Exception ex)
		{
			Logger.LogException(LogLevel.Error, ex, "Error loading Preferences");
			Application.Current.Exit();  // Win32; 
		}
	}
}