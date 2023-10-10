using Windows.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCAD.DAL;
using StoryCAD.Models;
using StoryCAD.Models.Tools;
using StoryCAD.Services.Backend;
using System;
using System.ComponentModel;

namespace StoryCAD.ViewModels.Tools;

/// <summary>
/// This is the PreferencesInitialization ViewModel, this data
/// </summary>
public class InitVM : ObservableRecipient
{

    /// <summary>
    /// This is the constructor for InitVM.
    /// It sets the paths for Path and Backup Path to
    /// \UserFolder\Documents\StoryCAD\ and then Projects or backups respectively.
    ///
    /// For example this would give the following path for me
    /// C:\Users\Jake\Documents\StoryCAD\Projects
    /// </summary>
    public InitVM()
    {
        ProjectDir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "StoryCAD", "Projects");
        BackupDir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "StoryCAD", "Backups");

        PropertyChanged += OnPropertyChanged;
    }

    private void OnPropertyChanged(object sender, PropertyChangedEventArgs e) {}

    public PreferencesModel Preferences = new();

    private string _errorMessage;
    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    private string _ProjectDir;
    public string ProjectDir
    {
        get => _ProjectDir;
        set => SetProperty(ref _ProjectDir, value);
    }

    private string _backupDir;
    public string BackupDir
    {
        get => _backupDir;
        set => SetProperty(ref _backupDir, value);
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
        //Create paths 
        System.IO.Directory.CreateDirectory(ProjectDir);
        System.IO.Directory.CreateDirectory(BackupDir);

        //Save paths
        Preferences.ProjectDirectory = ProjectDir;
        Preferences.BackupDirectory = BackupDir;

        //Make sure prefs init page isn't shown again
        Preferences.PreferencesInitialized = true; 

        //Updates the file, then rereads into memory.
        PreferencesIo _prfIo = new(Preferences, Ioc.Default.GetRequiredService<Developer>().RootDirectory);
        await _prfIo.WritePreferences ();
        PreferencesIo _loader = new(GlobalData.Preferences, Ioc.Default.GetRequiredService<Developer>().RootDirectory);
        await _loader.ReadPreferences();
        BackendService _backend = Ioc.Default.GetRequiredService<BackendService>();
        if (!GlobalData.Preferences.RecordPreferencesStatus) { await _backend.PostPreferences(GlobalData.Preferences); }
    }
}