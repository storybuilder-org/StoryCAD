using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI.ViewManagement;
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
    /// It can be light, dark or default
    /// </summary>
    public ElementTheme ThemePreference;


    // Visual changes
    public SolidColorBrush PrimaryColor { get; set; } //Sets UI Color
    public SolidColorBrush SecondaryColor { get; set; } //Sets node color.

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

        PrimaryColor = new SolidColorBrush(new UISettings().GetColorValue(UIColorType.Accent));
        SecondaryColor = new SolidColorBrush(new UISettings().GetColorValue(UIColorType.Accent));
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
        RecordVersionStatus = false;      // Last version change was logged successfully or notx
        PreferredSearchEngine = BrowserType.DuckDuckGo;
    }
    #endregion
}