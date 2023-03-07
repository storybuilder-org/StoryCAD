using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using StoryBuilder.DAL;
using StoryBuilder.Models;
using StoryBuilder.Models.Tools;
using StoryBuilder.Services.Backend;

namespace StoryBuilder.ViewModels.Tools;

public class PreferencesViewModel : ObservableRecipient
{
    #region Properties
    
    /// <summary>
    /// Init is an 'undocumented' preference that controls
    /// if the PreferencesInitialization Page is shown at boot.
    ///
    /// As this is the preferences page, the user has already gone
    /// through preferences however Init can be set to false via
    /// the Dev Menu (Shown if a debugger is attached.)
    /// </summary>
    public bool Init = true;

    public PreferencesModel CurrentModel = GlobalData.Preferences;

    #endregion

    /// <summary>
    /// Saves the users preferences to disk.
    /// </summary>
    public async Task SaveAsync()
    {
        PreferencesModel _prf = new();
        PreferencesIo _prfIo = new(_prf, Path.Combine(ApplicationData.Current.RoamingFolder.Path, "Storybuilder"));
        await _prfIo.UpdateModel();

        _prf.Name = Name;
        _prf.Email = Email;
        _prf.ErrorCollectionConsent = ErrorConsent;
        _prf.ProjectDirectory = ProjectDir;
        _prf.BackupDirectory = BackupDir;
        _prf.TimedBackupInterval = BackupInterval;
        _prf.TimedBackup = Backup;
        _prf.Newsletter = NewsConsent;
        _prf.PreferencesInitialized = Init;
        _prf.BackupOnOpen = BackupUpOnOpen;
        _prf.AutoSave = AutoSave;
        _prf.PreferredSearchEngine = (BrowserType)PreferredSearchEngine;
        if (AutoSaveInterval is > 60 or < 14) { AutoSaveInterval = 30; }
        else { _prf.AutoSaveInterval = AutoSaveInterval; }

        if (WrapNodeNames) { _prf.WrapNodeNames = TextWrapping.WrapWholeWords; }
        else { _prf.WrapNodeNames = TextWrapping.NoWrap; }


        await _prfIo.UpdateFile();
        PreferencesIo _loader = new(GlobalData.Preferences, Path.Combine(ApplicationData.Current.RoamingFolder.Path, "Storybuilder"));
        await _loader.UpdateModel();
        BackendService _backend = Ioc.Default.GetRequiredService<BackendService>();
        GlobalData.Preferences.RecordPreferencesStatus = false;  // indicate need to update
        await _backend.PostPreferences(GlobalData.Preferences);
    }

    /// <summary>
    /// Loads the model from the global one
    /// it then updates the UI with the value
    /// </summary>
    public PreferencesViewModel()
    {
        PreferencesModel _model = GlobalData.Preferences;
        ErrorConsent = _model.ErrorCollectionConsent;
        Email = _model.Email;
        Name = _model.Name;
        ProjectDir = _model.ProjectDirectory;
        BackupInterval = _model.TimedBackupInterval;
        Backup = _model.TimedBackup;
        NewsConsent = _model.Newsletter;
        BackupDir = _model.BackupDirectory;
        BackupUpOnOpen = _model.BackupOnOpen;
        AutoSave = _model.AutoSave;
        AutoSaveInterval = _model.AutoSaveInterval;
        PreferredSearchEngine = (int)_model.PreferredSearchEngine;

        if (_model.WrapNodeNames == TextWrapping.WrapWholeWords) { WrapNodeNames = true; }
        else { WrapNodeNames = false; }
    }
}