using CommunityToolkit.Mvvm.ComponentModel;
using StoryBuilder.DAL;
using StoryBuilder.Models;
using StoryBuilder.Models.Tools;
using System.Threading.Tasks;
using Windows.Storage;
using Microsoft.UI.Xaml;
using Microsoft.VisualBasic;

namespace StoryBuilder.ViewModels.Tools;

public class PreferencesViewModel : ObservableRecipient
{
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

    /// <summary>
    /// Saves the users preferences to disk.
    /// </summary>
    public async Task SaveAsync()
    {
        PreferencesModel prf = new();
        PreferencesIO prfIO = new(prf, System.IO.Path.Combine(ApplicationData.Current.RoamingFolder.Path, "Storybuilder"));
        await prfIO.UpdateModel();
    
        prf.Name = Name;
        prf.Email = Email;
        prf.ErrorCollectionConsent = ErrorConsent;
        prf.ProjectDirectory = ProjectDir;
        prf.BackupDirectory = BackupDir;
        prf.TimedBackupInterval = BackupInterval;
        prf.TimedBackup = Backup;
        prf.Newsletter = NewsConsent;

        if (WrapNodeNames) {prf.WrapNodeNames = TextWrapping.WrapWholeWords;}
        else {prf.WrapNodeNames = TextWrapping.NoWrap;}

        await prfIO.UpdateFile();
        PreferencesIO loader = new(GlobalData.Preferences, System.IO.Path.Combine(ApplicationData.Current.RoamingFolder.Path, "Storybuilder"));
        await loader.UpdateModel();
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

        if (_model.WrapNodeNames == TextWrapping.WrapWholeWords) {WrapNodeNames = true;}
        else {WrapNodeNames = false; }
    }
}