using Windows.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using StoryBuilder.DAL;
using StoryBuilder.Models;
using StoryBuilder.Models.Tools;
using StoryBuilder.Services.Backend;
using System;

namespace StoryBuilder.ViewModels.Tools;

public class InitVM : ObservableRecipient
{
    public InitVM()
    {
       _path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "StoryBuilder", "Projects");
       _backupDir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "StoryBuilder", "Backups");
    } 

    private string _errorMessage;
    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }
    private string _name;
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    private string _email;
    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    private string _path;
    public string Path
    {
        get => _path;
        set => SetProperty(ref _path, value);
    }

    private string _backupDir;
    public string BackupPath
    {
        get => _backupDir;
        set => SetProperty(ref _backupDir, value);
    }

    private bool _errorlogging;
    public bool ErrorLogging
    {
        get => _errorlogging;
        set => SetProperty(ref _errorlogging, value);
    }

    private bool _news;
    public bool News
    {
        get => _news;
        set => SetProperty(ref _news, value);
    }

    public async void Save()
    {
        //Creates new preferences model and sets the values
        PreferencesModel _prf = new()
        {
            Email = Email,
            ErrorCollectionConsent = ErrorLogging,
            Newsletter = News,
            ProjectDirectory = Path,
            BackupDirectory = BackupPath,
            Name = Name,
            PreferencesInitialized = true //Makes sure this window isn't shown to the user again
        };

        //Create paths
        System.IO.Directory.CreateDirectory(Path);
        System.IO.Directory.CreateDirectory(BackupPath);

        //Updates the file, then rereads into memory.
        PreferencesIo _prfIo = new(_prf, System.IO.Path.Combine(ApplicationData.Current.RoamingFolder.Path,"Storybuilder"));
        await _prfIo.UpdateFile();
        PreferencesIo _loader = new(GlobalData.Preferences, System.IO.Path.Combine(ApplicationData.Current.RoamingFolder.Path, "Storybuilder"));
        await _loader.UpdateModel();
        BackendService _backend = Ioc.Default.GetRequiredService<BackendService>();
        if (!GlobalData.Preferences.RecordPreferencesStatus) { await _backend.PostPreferences(GlobalData.Preferences); }
    }
}