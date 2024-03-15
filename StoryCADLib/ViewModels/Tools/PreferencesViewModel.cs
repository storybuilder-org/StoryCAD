using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCAD.DAL;
using StoryCAD.Models;
using StoryCAD.Models.Tools;
using StoryCAD.Services.Backend;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml;
using Windows.UI.ViewManagement;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using StoryCAD.Services.Ratings;

namespace StoryCAD.ViewModels.Tools;


/// <summary>
/// This view model handles the Services.Dialogs.Tools.PreferencesDialog.
/// It'S based on ObservableValidator, a Community.Toolkit.Mvvm class
/// which adds validation support to the ObservableRecipient class:
///
/// https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/observablevalidator
///
/// See also:
/// https://xamlbrewer.wordpress.com/2021/06/07/data-validation-with-the-microsoft-mvvm-toolkit/
/// https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations?view=net-7.0
/// 
/// </summary>
public class PreferencesViewModel : ObservableValidator
{
    public PreferencesModel CurrentModel = Ioc.Default.GetRequiredService<AppState>().Preferences;
    public string Errors => string.Join(Environment.NewLine, from ValidationResult e in GetErrors(null) select e.ErrorMessage);

    #region Fields
    private bool _changed { get; set; }

    #endregion

    #region Properties
    
    //User information tab

    private string _firstName;
    [Required(ErrorMessage = "First name is required.")]
    [MinLength(2, ErrorMessage = "First name should be longer than one character")]
    public string FirstName
    {
        get => _firstName;
        set => SetProperty(ref _firstName, value, false);
    }

    private string _lastName;
    [Required(ErrorMessage = "First name is required.")]
    [MinLength(2, ErrorMessage = "First name should be longer than one character")]
    public string LastName
    {
        get => _lastName;
        set => SetProperty(ref _lastName, value, false);
    }

    [EmailAddress(ErrorMessage = "Must be a valid email address")]
    [MinLength(2, ErrorMessage = "Name should be longer than one character")]
    private string _email;
    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value, false);
    }

    private bool _errorCollectionConsent;
    public bool ErrorCollectionConsent
    {
        get => _errorCollectionConsent;
        set => SetProperty(ref _errorCollectionConsent, value);
    }

    private bool _newsletter;
    public bool Newsletter
    {
        get => _newsletter;
        set => SetProperty(ref _newsletter, value);
    }
   
    /// <summary>
    /// This switch tracks whether this is a new 
    /// installation and if Initialization should be shown.
    /// </summary>
    public bool PreferencesInitialized { get; set; }

    /// <summary>
    /// This is the Last Template Selected by the user.
    /// </summary>
    private int _lastSelectedTemplate;
    public int LastSelectedTemplate
    {
        get => _lastSelectedTemplate;
        set => SetProperty(ref _lastSelectedTemplate, value);
    }

    public TextWrapping WrapNodeNames { get; set; }

    // Backup Information

    public bool _autoSave;
    public bool AutoSave
    {
        get => _autoSave;
        set => SetProperty(ref _autoSave, value);
    }

    
    private int _autoSaveInterval;
    /// <summary>
    /// AutoSaveInterval in seconds
    /// </summary>
    [Range(15, 60, ErrorMessage = "Value for {0} must be between {1} and {2} seconds.")]
    public int AutoSaveInterval
    {
        get => _autoSaveInterval;
        set => SetProperty(ref _autoSaveInterval, value, false);
    }
    public bool BackupOnOpen { get; set; }
    public bool TimedBackup { get; set; }

    private int _timedBackupInterval;
    /// <summary>
    /// TimedBackupInterval in minutes
    /// </summary>
    [Range(10, 60, ErrorMessage = "Value for {0} must be between {1} and {2} minutes.")]
    public int TimedBackupInterval
    {
        get => _timedBackupInterval;
        set => SetProperty(ref _timedBackupInterval, value, false);
    }

    //Directories

    private string _projectDirectory;
    [FilePath(ErrorMessage = "Project Directory must be a valid filepath")]
    public string ProjectDirectory
    {
        get => _projectDirectory;
        set => SetProperty(ref _projectDirectory, value, false);
    }


    private string _backupDirectory;
    [FilePath(ErrorMessage = "Backup Directory must be a valid filepath")]
    public string BackupDirectory
    {
        get => _backupDirectory;
        set => SetProperty(ref _backupDirectory, value, false);
    }
    
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
    public bool RecordVersionStatus { get; set; }      // Last version change was logged successfully or not
    public BrowserType PreferredSearchEngine { get; set; }      // Last version change was logged successfully or not

    public int SearchEngineIndex
    {
        get => (int)PreferredSearchEngine;
        set => PreferredSearchEngine = (BrowserType)value;
    } // Last version change was logged successfully or not

    private ElementTheme PreferedTheme;
    public int PreferredThemeIndex
    {
        get => (int)PreferedTheme;
        set => PreferedTheme = (ElementTheme)value;
    }

    #endregion

    #region Methods

    internal void LoadModel()
    {
        FirstName = CurrentModel.FirstName;
        LastName = CurrentModel.LastName;
        Email = CurrentModel.Email;
        ErrorCollectionConsent = CurrentModel.ErrorCollectionConsent;
        Newsletter = CurrentModel.Newsletter;
        PreferencesInitialized = CurrentModel.PreferencesInitialized;
        LastSelectedTemplate = CurrentModel.LastSelectedTemplate;
        WrapNodeNames = CurrentModel.WrapNodeNames;

        LastFile1 = CurrentModel.LastFile1;
        LastFile2 = CurrentModel.LastFile2;
        LastFile3 = CurrentModel.LastFile3;
        LastFile4 = CurrentModel.LastFile4;
        LastFile5 = CurrentModel.LastFile5;

        ProjectDirectory = CurrentModel.ProjectDirectory;
        BackupDirectory = CurrentModel.BackupDirectory;  
        AutoSave = CurrentModel.AutoSave;
        AutoSaveInterval = CurrentModel.AutoSaveInterval;
        BackupOnOpen = CurrentModel.BackupOnOpen;
        TimedBackup = CurrentModel.TimedBackup;
        TimedBackupInterval = CurrentModel.TimedBackupInterval;

        Version = CurrentModel.Version;
        RecordPreferencesStatus = CurrentModel.RecordPreferencesStatus;
        RecordVersionStatus = CurrentModel.RecordVersionStatus;
        PreferredSearchEngine = CurrentModel.PreferredSearchEngine;
        PreferedTheme = CurrentModel.ThemePreference;
    }

    internal void SaveModel()
    {
        CurrentModel.FirstName = FirstName;
        CurrentModel.LastName = LastName;
        CurrentModel.Email = Email;
        CurrentModel.ErrorCollectionConsent = ErrorCollectionConsent;
        CurrentModel.Newsletter = Newsletter;
        CurrentModel.PreferencesInitialized = PreferencesInitialized;
        CurrentModel.LastSelectedTemplate = LastSelectedTemplate;
        CurrentModel.WrapNodeNames = WrapNodeNames;

        CurrentModel.LastFile1 = LastFile1;
        CurrentModel.LastFile2 = LastFile2;
        CurrentModel.LastFile3 = LastFile3;
        CurrentModel.LastFile4 = LastFile4;
        CurrentModel.LastFile5 = LastFile5;

        CurrentModel.ProjectDirectory = ProjectDirectory;
        CurrentModel.BackupDirectory = BackupDirectory;
        CurrentModel.AutoSave = AutoSave;
        CurrentModel.AutoSaveInterval = AutoSaveInterval;
        CurrentModel.BackupOnOpen = BackupOnOpen;
        CurrentModel.TimedBackup = TimedBackup;
        CurrentModel.TimedBackupInterval = TimedBackupInterval;
        CurrentModel.Version = Version;
        CurrentModel.RecordPreferencesStatus = RecordPreferencesStatus;
        CurrentModel.RecordVersionStatus = RecordVersionStatus;
        CurrentModel.PreferredSearchEngine = PreferredSearchEngine;

        if (CurrentModel.ThemePreference != PreferedTheme)
        {
            Ioc.Default.GetService<Windowing>().RequestedTheme = CurrentModel.ThemePreference;
            Ioc.Default.GetService<Windowing>().UpdateUIToTheme();
        }
        CurrentModel.ThemePreference = PreferedTheme;
    }

    /// <summary>
    /// Saves the users preferences to disk.
    /// </summary>
    public async Task SaveAsync()
    {
        PreferencesIo _prfIo = new(CurrentModel, Ioc.Default.GetRequiredService<AppState>().RootDirectory);
        await _prfIo.WritePreferences();
        await _prfIo.ReadPreferences();
        AppState State = Ioc.Default.GetService<AppState>();

        State.Preferences = CurrentModel;

        BackendService _backend = Ioc.Default.GetRequiredService<BackendService>();
        State.Preferences.RecordPreferencesStatus = false;  // indicate need to update
        await _backend.PostPreferences(State.Preferences);
    }

    private void OnPropertyChanged(object sender, PropertyChangedEventArgs args)
    {
        _changed = true;
    }

    private void Preferences_ErrorsChanged(object sender, DataErrorsChangedEventArgs e)
    {
        OnPropertyChanged(nameof(Errors)); // Update Errors on every Error change, so I can bind to it.
    }

	/// <summary>
	/// Shows the MS Store prompt
	/// </summary>
    public void ShowRatingPrompt(object sender, RoutedEventArgs e)
    {
	    Ioc.Default.GetService<RatingService>().OpenRatingPrompt();
    }

	#endregion

	#region Constructor

	public PreferencesViewModel()
    {
        this.ErrorsChanged += Preferences_ErrorsChanged;
    }

    #endregion
}