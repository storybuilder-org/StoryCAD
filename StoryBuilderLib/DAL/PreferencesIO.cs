using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using StoryBuilder.Models.Tools;
using StoryBuilder.Services.Logging;

namespace StoryBuilder.DAL;

/// <summary>
/// This data access module handles file I/O and updates
/// for the StoryBuilder.prf file and PreferencesModel  object. 
/// </summary>
public class PreferencesIO
{
    private IList<string> _preferences;
    private PreferencesModel _model;
    private string _path;
    private LogService _log = Ioc.Default.GetService<LogService>();

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="model">The PreferencesModel to read/write</param>
    /// <param name="path">The folder path StoryBuilder.prf resides in</param>
    public PreferencesIO(PreferencesModel model, string path)
    {
        _model = model;
        _path = path;
    }

    /// <summary>
    /// Update the model from the .prf file's contents 
    /// </summary>
    public async Task UpdateModel()
    {
        //Tries to read file
        StorageFolder preferencesFolder = await StorageFolder.GetFolderFromPathAsync(_path);
        IStorageFile preferencesFile = (IStorageFile) await preferencesFolder.TryGetItemAsync("StoryBuilder.prf");

        if (preferencesFile != null) //Checks if file exists
        {
            _preferences = await FileIO.ReadLinesAsync(preferencesFile);

            //Update the model from the file
            foreach (string line in _preferences)
            {
                string[] tokens = line.Split(new[] { '=' });
                switch (tokens[0])
                {
                    case "Name":
                        _model.Name = tokens[1];
                        break;

                    case "Email":
                        _model.Email = tokens[1];
                        break;

                    case "Initalised":
                        if (tokens[1] == "True") { _model.PreferencesInitialised = true; }
                        else { _model.PreferencesInitialised = false; }
                        break;

                    case "ErrorCollectionConsent":
                        if (tokens[1] == "True") { _model.ErrorCollectionConsent = true; }
                        else { _model.ErrorCollectionConsent = false; }
                        break;

                    case "Newsletter":
                        if (tokens[1] == "True")
                            _model.Newsletter = true;
                        else
                            _model.Newsletter = false;
                        break;

                    case "BackupOnOpen":
                        if (tokens[1] == "True")
                            _model.BackupOnOpen = true;
                        else
                            _model.BackupOnOpen = false;
                        break;

                    case "TimedBackup":
                        if (tokens[1] == "True") { _model.TimedBackup = true; }
                        else { _model.TimedBackup = false; }
                        break;

                    case "TimedBackupInterval":
                        _model.TimedBackupInterval = Convert.ToInt32(tokens[1]);
                        break;

                    case "ProjectDirectory":
                        _model.ProjectDirectory = tokens[1];
                        break;
                    case "BackupDirectory":
                        _model.BackupDirectory = tokens[1];
                        break;
                    case "LastFile1":
                        _model.LastFile1 = tokens[1];
                        break;
                    case "LastFile2":
                        _model.LastFile2 = tokens[1];
                        break;
                    case "LastFile3":
                        _model.LastFile3 = tokens[1];
                        break;
                    case "LastFile4":
                        _model.LastFile4 = tokens[1];
                        break;
                    case "LastFile5":
                        _model.LastFile5 = tokens[1];
                        break;
                    case "LastTemplate":
                        _model.LastSelectedTemplate = Convert.ToInt32(tokens[1]);
                        break;
                    case "Version":
                        _model.Version = tokens[1];
                        break;
                    case "RecordPreferencesStatus":
                         if (tokens[1] == "True") 
                            _model.RecordPreferencesStatus = true; 
                         else 
                            _model.RecordPreferencesStatus = false; 
                         break;
                    case "RecordVersionStatus":
                        if (tokens[1] == "True")
                            _model.RecordVersionStatus = true;
                        else
                            _model.RecordVersionStatus= false;
                        break;
                    case "WrapNodeNames":
                        if (tokens[1] == "True") { _model.WrapNodeNames = TextWrapping.Wrap; }
                        else {_model.WrapNodeNames = TextWrapping.NoWrap;}
                        break;
                    case "AutoSave":
                        if (tokens[1] == "True") { _model.AutoSave = true; }
                        else { _model.AutoSave = false; }
                        break;
                    case "AutoSaveInterval":
                        _model.AutoSaveInterval = Convert.ToInt32(tokens[1]);
                        break;
                }
            }
            _log.Log(LogLevel.Info, "PreferencesModel updated from StoryBuilder.prf.");
        }
        else
        {
            // The file doesn't exist.
            _log.Log(LogLevel.Info, "StoryBuilder.prf not found; default created.");
        }

        if (Application.Current.RequestedTheme == ApplicationTheme.Light)
        {
            _model.PrimaryColor = new SolidColorBrush(Colors.LightGray);
            _model.SecondaryColor = new SolidColorBrush(Colors.Black);
        }
        else
        {
            _model.PrimaryColor = new SolidColorBrush(Colors.DarkSlateGray);
            _model.SecondaryColor = new SolidColorBrush(Colors.White);
        }
    }

    public async Task UpdateFile()
    {
        _log.Log(LogLevel.Info, "Updating prf from model.");
        StorageFolder preferencesFolder = await StorageFolder.GetFolderFromPathAsync(_path);
        StorageFile preferencesFile = await preferencesFolder.CreateFileAsync("StoryBuilder.prf", CreationCollisionOption.ReplaceExisting);

        //Updates file
        List<string> NewPreferences = new();
        NewPreferences.Add("Newsletter=" + _model.Newsletter);
        NewPreferences.Add("Initalised=" + _model.PreferencesInitialised);
        NewPreferences.Add("Name=" + _model.Name);
        NewPreferences.Add("Email=" + _model.Email);
        NewPreferences.Add("TimedBackupInterval=" + _model.TimedBackupInterval);
        NewPreferences.Add("ProjectDirectory=" + _model.ProjectDirectory);
        NewPreferences.Add("BackupDirectory=" + _model.BackupDirectory);
        NewPreferences.Add("LastFile1=" + _model.LastFile1);
        NewPreferences.Add("LastFile2=" + _model.LastFile2);
        NewPreferences.Add("LastFile3=" + _model.LastFile3);
        NewPreferences.Add("LastFile4=" + _model.LastFile4);
        NewPreferences.Add("LastFile5=" + _model.LastFile5);
        NewPreferences.Add("BackupOnOpen=" + _model.BackupOnOpen);
        NewPreferences.Add("ErrorCollectionConsent=" + _model.ErrorCollectionConsent);
        NewPreferences.Add("TimedBackup=" + _model.TimedBackup);
        NewPreferences.Add("LastTemplate=" + _model.LastSelectedTemplate);
        NewPreferences.Add("Version=" + _model.Version);
        NewPreferences.Add("RecordPreferencesStatus=" + _model.RecordPreferencesStatus); //These are spelt wrong but correcting them will cause data loss. (Save it for a major update)
        NewPreferences.Add("RecordVersionStatus=" + _model.RecordVersionStatus);
        NewPreferences.Add("AutoSave=" + _model.AutoSave);
        NewPreferences.Add("AutoSaveInterval=" + _model.AutoSaveInterval);

        if (_model.WrapNodeNames == TextWrapping.WrapWholeWords) { NewPreferences.Add("WrapNodeNames=True"); }
        else { NewPreferences.Add("WrapNodeNames=False"); }

        await FileIO.WriteLinesAsync(preferencesFile, NewPreferences); //Writes file to disk.
    }
}