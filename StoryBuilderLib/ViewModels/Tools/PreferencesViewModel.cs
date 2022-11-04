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
using System;

namespace StoryBuilder.ViewModels.Tools;

public class PreferencesViewModel : ObservableRecipient
{
    public bool init = true;
    private string _backupdir;
    public string BackupDir
    {
        get => _backupdir;
        set => SetProperty(ref _backupdir, value);
    }

    private string _Name;
    public string Name
    {
        get => _Name;
        set => _Name = value;
    }
    private string _Email;
    public string Email
    {
        get => _Email;
        set => _Email = value;
    }
    private bool _ErrorConsent;
    public bool ErrorConsent
    {
        get => _ErrorConsent;
        set => _ErrorConsent = value;
    }
    private bool _NewsConsent;
    public bool NewsConsent
    {
        get => _NewsConsent;
        set => _NewsConsent = value;
    }
    private bool _Backup;
    public bool Backup
    {
        get => _Backup;
        set => _Backup = value;
    }
    private int _BackupInterval;
    public int BackupInterval
    {
        get => _BackupInterval;
        set => _BackupInterval = value;
    }
    private string _ProjectDir;
    public string ProjectDir
    {
        get => _ProjectDir;
        set => _ProjectDir = value;
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
        PreferencesModel prf = new();
        PreferencesIo prfIO = new(prf, Path.Combine(ApplicationData.Current.RoamingFolder.Path, "Storybuilder"));
        await prfIO.UpdateModel();

        prf.Name = Name;
        prf.Email = Email;
        prf.ErrorCollectionConsent = ErrorConsent;
        prf.ProjectDirectory = ProjectDir;
        prf.BackupDirectory = BackupDir;
        prf.TimedBackupInterval = BackupInterval;
        prf.TimedBackup = Backup;
        prf.Newsletter = NewsConsent;
        prf.PreferencesInitialized = init;
        prf.BackupOnOpen = BackupUpOnOpen;
        prf.AutoSave = AutoSave;
        prf.PreferredSearchEngine = (BrowserType)PreferredSearchEngine;
        if (AutoSaveInterval > 31 || AutoSaveInterval < 4) { AutoSaveInterval = 20; }
        else { prf.AutoSaveInterval = AutoSaveInterval; }

        if (WrapNodeNames) { prf.WrapNodeNames = TextWrapping.WrapWholeWords; }
        else { prf.WrapNodeNames = TextWrapping.NoWrap; }


        await prfIO.UpdateFile();
        PreferencesIo loader = new(GlobalData.Preferences, Path.Combine(ApplicationData.Current.RoamingFolder.Path, "Storybuilder"));
        await loader.UpdateModel();
        BackendService backend = Ioc.Default.GetService<BackendService>();
        GlobalData.Preferences.RecordPreferencesStatus = false;  // indicate need to update
        await backend.PostPreferences(GlobalData.Preferences);
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