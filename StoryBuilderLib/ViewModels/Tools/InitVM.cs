using Windows.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using StoryBuilder.DAL;
using StoryBuilder.Models;
using StoryBuilder.Models.Tools;
using StoryBuilder.Services.Backend;
using System;

namespace StoryBuilder.ViewModels.Tools;

/// <summary>
/// This is the PreferencesInitialization ViewModel, this data
/// </summary>
public class InitVM : ObservableRecipient
{

    /// <summary>
    /// This is the constructor for InitVM.
    /// It sets the paths for Path and Backup Path to
    /// \Userfolder\Documents\StoryBuilder\ and then Projects or backups respectively.
    ///
    /// For example this would give the following path for me
    /// C:\Users\Jake\Documents\StoryBuilder\Projects
    /// </summary>
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

    private bool _errorLogging;
    public bool ErrorLogging
    {
        get => _errorLogging;
        set => SetProperty(ref _errorLogging, value);
    }

    private bool _news;
    public bool News
    {
        get => _news;
        set => SetProperty(ref _news, value);
    }

    /// <summary>
    /// This saves and checks the input of the user.
    /// It creates a new preferences model and stores the
    /// inputs of the user such as path, name, email ect.
    ///
    /// This will then be saved and the backup path and project
    /// path folders will be created.
    /// </summary>
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