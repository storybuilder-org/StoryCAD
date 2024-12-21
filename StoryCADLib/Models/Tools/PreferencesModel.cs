using Microsoft.UI.Xaml;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json.Serialization;
using Newtonsoft.Json;


namespace StoryCAD.Models.Tools;

/// <summary>
/// PreferencesModel contains StoryCAD User preferences.
/// 
/// The model is maintained from a Shell Preferences() method
/// which is launched from a Command tied to a View button, and by
/// PreferencesViewModel using a ContentDialog as the view.
///
/// The StoryCAD user preferences are stored as Preferences.json within AppState.RootDirectory.
///
/// If Preferences.json doesn't exist, it will be created once the user hits done within
/// the preferences initialisation screen. 
/// </summary>
public class PreferencesModel : ObservableObject
{
	#region Properties

	/// <summary>
	/// This is the user's first name
	/// </summary>
	[JsonProperty("FirstName")]
	public string FirstName { get; set; }
	/// <summary>
	/// This is the users surname
	/// </summary>
	[JsonProperty("LastName")]
	public string LastName { get; set; }

	/// <summary>
	/// This is the users email
	/// (Used to get in contact for errors and newsletter if enabled)
	/// </summary>
	[JsonProperty("Email")]
	public string Email { get; set; }

	/// <summary>
	/// Disables Elmah.io integration if false
	/// </summary>
	[JsonProperty("ElmahConsent")]
	public bool ErrorCollectionConsent { get; set; }

	/// <summary>
	/// If set to true user email will be added to the newsletter list
	/// </summary>
	[JsonProperty("NewsletterConsent")]
	public bool Newsletter { get; set; }

	/// <summary>
	/// This switch tracks whether this is the first time StoryCAD is opened
	/// </summary>
	[JsonProperty("Initialized")]
	public bool PreferencesInitialized { get; set; }

	/// <summary>
	/// Tracks the last used template, for new outline creation
	/// </summary>
	[JsonProperty("LastTemplate")]
	public int LastSelectedTemplate { get; set; }

	/// <summary>
	/// This is the users theme preference
	/// </summary>
	[JsonProperty("Theme")]
	public ElementTheme ThemePreference { get; set; }

	/// <summary>
	/// If set to wrap, node names will wrap in the tree.
	/// If set to disabled, node names will cut off.
	/// </summary>
	[JsonProperty("NodeWrap")]
	public TextWrapping WrapNodeNames { get; set; }

	/// <summary>
	/// StoryCAD will automatically save the outline if true
	/// </summary>
	[JsonProperty("Autosave")]
	public bool AutoSave { get; set; }

	/// <summary>
	/// Controls how often autosave is ran
	/// (Ignored if AutoSave is off)
	/// </summary>
	[JsonProperty("AutosaveInterval")]
	public int AutoSaveInterval { get; set; }

	/// <summary>
	/// StoryCAD will create a backup of the outline when opened if true
	/// </summary>
	[JsonProperty("BackupOnOpen")]
	public bool BackupOnOpen { get; set; }

	/// <summary>
	/// StoryCAD will create a backup of the currently opened outline if true.
	/// </summary>
	[JsonProperty("TimedBackup")]
	public bool TimedBackup { get; set; }

	/// <summary>
	/// Controls timed backup frequency.
	/// </summary>
	[JsonProperty("TimedBackupInterval")]
	public int TimedBackupInterval { get; set; }

	/// <summary>
	/// Default location where outlines are stored.
	/// </summary>
	[JsonProperty("OutlineDirectory")]
	public string ProjectDirectory { get; set; }

	/// <summary>
	/// Location where backups are stored if enabled
	/// </summary>
	[JsonProperty("BackupDirectory")]
	public string BackupDirectory { get; set; }

	// Recent files (set automatically)
	public string LastFile1 { get; set; }	
	public string LastFile2 { get; set; }
	public string LastFile3 { get; set; }
	public string LastFile4 { get; set; }
	public string LastFile5 { get; set; }

	/// <summary>
	/// Tracks last version of StoryCAD that was opened
	/// </summary>
	[JsonProperty("Version")]
	public string Version { get; set; }

	// Backend server log status
	public bool RecordPreferencesStatus { get; set; }  // Last preferences change was logged successfully or not
	public bool RecordVersionStatus { get; set; }      // Last version change was logged successfully or not

	[JsonProperty("PreferredSearchEngine")]
	public BrowserType PreferredSearchEngine { get; set; }      // Last version change was logged successfully or not

	/// <summary>
	/// Search engine to use.
	/// </summary>
	[JsonProperty("SearchEngineIndex")]
	public int SearchEngineIndex { get; set; }

	/// <summary>
	/// Hides the rating prompt until the next update
	/// </summary>
	[JsonProperty("ShowRatings")]
	public bool HideRatingPrompt = false;

	/// <summary>
	/// Total amount of time StoryCAD has been used/open on the system
	/// </summary>
	[JsonProperty("TimeUsed")]

	public long CumulativeTimeUsed = 0;

	/// <summary>
	/// DateTime of last review
	/// </summary>
	[JsonProperty("LastReview")]
	public DateTime LastReviewDate;

	/// <summary>
	/// Should the startup dialog (HelpPage) be shown
	/// </summary>
	[JsonProperty("ShowStartupDialog")]
	public bool ShowStartupDialog;

	/// <summary>
	/// Do we want to log more in depth
	/// </summary>
	[JsonProperty("AdvancedLogging")]
	public bool AdvancedLogging { get; set; }
	#endregion
		
	#region Constructor
	public PreferencesModel()
	{
		FirstName = string.Empty;
		LastName = string.Empty;
		Email = string.Empty;
		ErrorCollectionConsent = false;
		Newsletter = false;
		PreferencesInitialized = false;
		LastSelectedTemplate = 0;
		WrapNodeNames = TextWrapping.WrapWholeWords;

		LastFile1 = string.Empty;
		LastFile2 = string.Empty;
		LastFile3 = string.Empty;
		LastFile4 = string.Empty;
		LastFile5 = string.Empty;

		AutoSave = true;
		AutoSaveInterval = 15;
		BackupOnOpen = false;
		TimedBackup = false;
		TimedBackupInterval = 5;


		Version = string.Empty;
		RecordPreferencesStatus = false;
		RecordVersionStatus = false;      // Last version change was logged successfully or not
		PreferredSearchEngine = BrowserType.DuckDuckGo;
		ThemePreference = ElementTheme.Default; // Use system theme
		HideRatingPrompt = false;
		CumulativeTimeUsed = 0;
		AdvancedLogging = false;
		LastReviewDate = DateTime.MinValue;
		ShowStartupDialog = true;
	}
	#endregion
}