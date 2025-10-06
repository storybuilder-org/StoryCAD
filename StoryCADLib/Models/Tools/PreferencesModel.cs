using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace StoryCAD.Models.Tools;

/// <summary>
///     PreferencesModel contains StoryCAD User preferences.
///     The model is maintained from a Shell Preferences() method
///     which is launched from a Command tied to a View button, and by
///     PreferencesViewModel using a ContentDialog as the view.
///     The StoryCAD user preferences are stored as Preferences.json within AppState.RootDirectory.
///     If Preferences.json doesn't exist, it will be created once the user hits done within
///     the preferences initialisation screen.
/// </summary>
public class PreferencesModel : ObservableObject
{
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
        RecentFiles = new List<string>();

        AutoSave = true;
        AutoSaveInterval = 15;
        BackupOnOpen = false;
        TimedBackup = false;
        TimedBackupInterval = 5;


        Version = string.Empty;
        RecordPreferencesStatus = false;
        RecordVersionStatus = false; // Last version change was logged successfully or not
        PreferredSearchEngine = BrowserType.DuckDuckGo;
        ThemePreference = ElementTheme.Default; // Use system theme
        HideRatingPrompt = false;
        CumulativeTimeUsed = 0;
        AdvancedLogging = false;
        HideKeyFileWarning = false;
        LastReviewDate = DateTime.MinValue;
        ShowStartupDialog = true;
    }

    #endregion

    #region Properties

    /// <summary>
    ///     This is the user's first name
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("FirstName")]
    public string FirstName { get; set; }

    /// <summary>
    ///     This is the user's surname
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("LastName")]
    public string LastName { get; set; }

    /// <summary>
    ///     This is the user's email
    ///     (Used to get in contact for errors and newsletter if enabled)
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("Email")]
    public string Email { get; set; }

    /// <summary>
    ///     Disables Elmah.io integration if false
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("ElmahConsent")]
    public bool ErrorCollectionConsent { get; set; }

    /// <summary>
    ///     If set to true user email will be added to the newsletter list
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("NewsletterConsent")]
    public bool Newsletter { get; set; }

    /// <summary>
    ///     This switch tracks whether this is the first time StoryCAD is opened
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("Initialized")]
    public bool PreferencesInitialized { get; set; }

    /// <summary>
    ///     Tracks the last used template, for new outline creation
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("LastTemplate")]
    public int LastSelectedTemplate { get; set; }

    /// <summary>
    ///     This is the user's theme preference
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("Theme")]
    public ElementTheme ThemePreference { get; set; }

    /// <summary>
    ///     If set to wrap, node names will wrap in the tree.
    ///     If set to disabled, node names will cut off.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("NodeWrap")]
    public TextWrapping WrapNodeNames { get; set; }

    /// <summary>
    ///     StoryCAD will automatically save the outline if true
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("Autosave")]
    public bool AutoSave { get; set; }

    /// <summary>
    ///     Controls how often autosave is run
    ///     (Ignored if AutoSave is off)
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("AutosaveInterval")]
    public int AutoSaveInterval { get; set; }

    /// <summary>
    ///     StoryCAD will create a backup of the outline when opened if true
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("BackupOnOpen")]
    public bool BackupOnOpen { get; set; }

    /// <summary>
    ///     StoryCAD will create a backup of the currently opened outline if true.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("TimedBackup")]
    public bool TimedBackup { get; set; }

    /// <summary>
    ///     Controls timed backup frequency.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("TimedBackupInterval")]
    public int TimedBackupInterval { get; set; }

    /// <summary>
    ///     Default location where outlines are stored.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("OutlineDirectory")]
    public string ProjectDirectory { get; set; }

    /// <summary>
    ///     Location where backups are stored if enabled
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("BackupDirectory")]
    public string BackupDirectory { get; set; }


    /// <summary>
    ///     Recently opened files
    ///     (Capped at 25)
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("RecentFiles")]
    public List<string> RecentFiles { get; set; }

    /// <summary>
    ///     Tracks last version of StoryCAD that was opened
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("Version")]
    public string Version { get; set; }

    // Backend server log status
    [JsonInclude]
    [JsonPropertyName("RecordPreferencesStatus")]
    public bool RecordPreferencesStatus { get; set; } // Last preferences change was logged successfully or not

    [JsonInclude]
    [JsonPropertyName("RecordVersionStatus")]
    public bool RecordVersionStatus { get; set; } // Last version change was logged successfully or not

    [JsonInclude]
    [JsonPropertyName("PreferredSearchEngine")]
    public BrowserType PreferredSearchEngine { get; set; } // Preferred search engine

    /// <summary>
    ///     Search engine to use.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("SearchEngineIndex")]
    public int SearchEngineIndex { get; set; }

    /// <summary>
    ///     Hides the rating prompt until the next update
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("ShowRatings")]
    public bool HideRatingPrompt { get; set; }

    /// <summary>
    ///     Total amount of time StoryCAD has been used/open on the system
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("TimeUsed")]
    public long CumulativeTimeUsed { get; set; }

    /// <summary>
    ///     DateTime of last review
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("LastReview")]
    public DateTime LastReviewDate { get; set; }

    /// <summary>
    ///     Should the startup dialog (HelpPage) be shown
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("ShowStartupDialog")]
    public bool ShowStartupDialog { get; set; }

    /// <summary>
    ///     Do we want to log more in depth
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("AdvancedLogging")]
    public bool AdvancedLogging { get; set; }

    /// <summary>
    ///     Hide the key file missing warning dialog.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("HideKeyFileWarning")]
    public bool HideKeyFileWarning { get; set; }

    #endregion
}
