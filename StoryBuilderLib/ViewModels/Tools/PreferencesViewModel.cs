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
    public bool Init = true;
    private string _backupDir;
    public string BackupDir
    {
        get => _backupDir;
        set => SetProperty(ref _backupDir, value);
    }

    private string _name;
    public string Name
    {
        get => _name;
        set => _name = value;
    }
    private string _email;
    public string Email
    {
        get => _email;
        set => _email = value;
    }
    private bool _errorConsent;
    public bool ErrorConsent
    {
        get => _errorConsent;
        set => _errorConsent = value;
    }
    private bool _newsConsent;
    public bool NewsConsent
    {
        get => _newsConsent;
        set => _newsConsent = value;
    }
    private bool _backup;
    public bool Backup
    {
        get => _backup;
        set => _backup = value;
    }
    private int _backupInterval;
    public int BackupInterval
    {
        get => _backupInterval;
        set => _backupInterval = value;
    }
    private string _projectDir;
    public string ProjectDir
    {
        get => _projectDir;
        set => _projectDir = value;
    }

    private bool _wrapNodeNames;
    public bool WrapNodeNames
    {
        get => _wrapNodeNames;
        set => _wrapNodeNames = value;
    }

    private bool _backupOnOpen;
    public bool BackupUpOnOpen
    {
        get => _backupOnOpen;
        set => _backupOnOpen = value;
    }

    private bool _autoSave;
    public bool AutoSave
    {
        get => _autoSave;
        set => _autoSave = value;
    }
    private int _autoSaveInterval;
    public int AutoSaveInterval
    {
        get => _autoSaveInterval;
        set => _autoSaveInterval = value;
    }

    private BrowserType _preferredSearchEngine;
    public int PreferredSearchEngine
    {
        get => (int)_preferredSearchEngine;
        set => _preferredSearchEngine = (BrowserType)value;
    }

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
        if (AutoSaveInterval is > 31 or < 4) { AutoSaveInterval = 20; }
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