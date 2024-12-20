﻿using Microsoft.UI.Xaml;
using CommunityToolkit.Mvvm.ComponentModel;


namespace StoryCAD.Models.Tools;

/// <summary>
/// PreferencesModel contains product and licensing information, and
/// (especially) user preferences. 
/// 
/// The model is maintained from a Shell Preferences() method
/// which is launched from a Command tied to a View button, and by
/// PreferencesViewModel using a ContentDialog as the view. The backing
/// store is a file, StoryCAD.prf, which is  contained with other installation files.
///
/// If no .prf file is present, StoryCAD.Services.Install.InstallationService's
/// InstallFiles() method will create one.
/// 
/// </summary>
public class PreferencesModel : ObservableObject
{
	#region Properties

	//User information
	public string FirstName { get; set; }
	public string LastName { get; set; }
	public string Email { get; set; }
	public bool ErrorCollectionConsent { get; set; }
	public bool Newsletter { get; set; }

	/// <summary>
	/// This switch tracks whether this is a new 
	/// installation and if Initialization should be shown.
	/// </summary>
	public bool PreferencesInitialized { get; set; }
	public int LastSelectedTemplate { get; set; } //This is the Last Template Selected by the user.


	/// <summary>
	/// This is the users theme preference
	/// It can be light, dark or default (System theme)
	/// </summary>
	public ElementTheme ThemePreference;


	public TextWrapping WrapNodeNames { get; set; }

	// Backup Information
	public bool AutoSave { get; set; }
	public int AutoSaveInterval { get; set; }
	public bool BackupOnOpen { get; set; }
	public bool TimedBackup { get; set; }
	public int TimedBackupInterval { get; set; }

	//Directories

	public string ProjectDirectory { get; set; }

	public string BackupDirectory { get; set; }

	// Recent files (set automatically)
	public string LastFile1 { get; set; }
	public string LastFile2 { get; set; }
	public string LastFile3 { get; set; }
	public string LastFile4 { get; set; }
	public string LastFile5 { get; set; }

	//Version Tracking
	public string Version { get; set; }

	// Backend server log status
	public bool RecordPreferencesStatus { get; set; }  // Last preferences change was logged successfully or not
	public bool RecordVersionStatus { get; set; }      // Last version change was logged successfully or notx
	public BrowserType PreferredSearchEngine { get; set; }      // Last version change was logged successfully or not

	// Last version change was logged successfully or not
	public int SearchEngineIndex { get; set; }

	/// <summary>
	/// Hides the rating prompt until the next update
	/// </summary>
	public bool HideRatingPrompt = false;

	/// <summary>
	/// Total ammount of time StoryCAD has been used/open on the system
	/// </summary>
	public long CumulativeTimeUsed = 0;

	/// <summary>
	/// DateTime of last review
	/// </summary>
	public DateTime LastReviewDate;

	/// <summary>
	/// Should the startup dialog (HelpPage) be shown
	/// </summary>
	public bool ShowStartupDialog;
  
	/// <summary>
	/// Do we want to log more in depth
	/// </summary>
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