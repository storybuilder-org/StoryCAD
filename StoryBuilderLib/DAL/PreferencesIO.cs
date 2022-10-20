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
public class PreferencesIo
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
    public PreferencesIo(PreferencesModel model, string path)
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
        StorageFolder _preferencesFolder = await StorageFolder.GetFolderFromPathAsync(_path);
        IStorageFile _preferencesFile = (IStorageFile) await _preferencesFolder.TryGetItemAsync("StoryBuilder.prf");

        if (_preferencesFile != null) //Checks if file exists
        {
            _preferences = await FileIO.ReadLinesAsync(_preferencesFile);

            //Update the model from the file
            //TODO: Use bool.parse.
            foreach (string _line in _preferences)
            {
                string[] _tokens = _line.Split(new[] { '=' });
                switch (_tokens[0])
                {
                    case "Name":
                        _model.Name = _tokens[1];
                        break;

                    case "Email":
                        _model.Email = _tokens[1];
                        break;

                    case "Initalised":
                        if (_tokens[1] == "True") { _model.PreferencesInitialized = true; }
                        else { _model.PreferencesInitialized = false; }
                        break;

                    case "ErrorCollectionConsent":
                        if (_tokens[1] == "True") { _model.ErrorCollectionConsent = true; }
                        else { _model.ErrorCollectionConsent = false; }
                        break;

                    case "Newsletter":
                        if (_tokens[1] == "True")
                            _model.Newsletter = true;
                        else
                            _model.Newsletter = false;
                        break;

                    case "BackupOnOpen":
                        if (_tokens[1] == "True")
                            _model.BackupOnOpen = true;
                        else
                            _model.BackupOnOpen = false;
                        break;

                    case "TimedBackup":
                        if (_tokens[1] == "True") { _model.TimedBackup = true; }
                        else { _model.TimedBackup = false; }
                        break;

                    case "TimedBackupInterval":
                        _model.TimedBackupInterval = Convert.ToInt32(_tokens[1]);
                        break;

                    case "ProjectDirectory":
                        _model.ProjectDirectory = _tokens[1];
                        break;
                    case "BackupDirectory":
                        _model.BackupDirectory = _tokens[1];
                        break;
                    case "LastFile1":
                        _model.LastFile1 = _tokens[1];
                        break;
                    case "LastFile2":
                        _model.LastFile2 = _tokens[1];
                        break;
                    case "LastFile3":
                        _model.LastFile3 = _tokens[1];
                        break;
                    case "LastFile4":
                        _model.LastFile4 = _tokens[1];
                        break;
                    case "LastFile5":
                        _model.LastFile5 = _tokens[1];
                        break;
                    case "LastTemplate":
                        _model.LastSelectedTemplate = Convert.ToInt32(_tokens[1]);
                        break;
                    case "Version":
                        _model.Version = _tokens[1];
                        break;
                    case "RecordPreferencesStatus":
                         if (_tokens[1] == "True") 
                            _model.RecordPreferencesStatus = true; 
                         else 
                            _model.RecordPreferencesStatus = false; 
                         break;
                    case "RecordVersionStatus":
                        if (_tokens[1] == "True")
                            _model.RecordVersionStatus = true;
                        else
                            _model.RecordVersionStatus= false;
                        break;
                    case "WrapNodeNames":
                        if (_tokens[1] == "True") { _model.WrapNodeNames = TextWrapping.Wrap; }
                        else {_model.WrapNodeNames = TextWrapping.NoWrap;}
                        break;
                    case "AutoSave":
                        if (_tokens[1] == "True") { _model.AutoSave = true; }
                        else { _model.AutoSave = false; }
                        break;
                    case "AutoSaveInterval":
                        _model.AutoSaveInterval = Convert.ToInt32(_tokens[1]);
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
        }
        else
        {
            _model.PrimaryColor = new SolidColorBrush(Colors.DarkSlateGray);
        }
    }

    public async Task UpdateFile()
    {
        _log.Log(LogLevel.Info, "Updating prf from model.");
        StorageFolder _preferencesFolder = await StorageFolder.GetFolderFromPathAsync(_path);
        StorageFile _preferencesFile = await _preferencesFolder.CreateFileAsync("StoryBuilder.prf", CreationCollisionOption.ReplaceExisting);

        //Updates file
        List<string> _newPreferences = new()
        {
            "Newsletter=" + _model.Newsletter,
            "Initalised=" + _model.PreferencesInitialized,
            "Name=" + _model.Name,
            "Email=" + _model.Email,
            "TimedBackupInterval=" + _model.TimedBackupInterval,
            "ProjectDirectory=" + _model.ProjectDirectory,
            "BackupDirectory=" + _model.BackupDirectory,
            "LastFile1=" + _model.LastFile1,
            "LastFile2=" + _model.LastFile2,
            "LastFile3=" + _model.LastFile3,
            "LastFile4=" + _model.LastFile4,
            "LastFile5=" + _model.LastFile5,
            "BackupOnOpen=" + _model.BackupOnOpen,
            "ErrorCollectionConsent=" + _model.ErrorCollectionConsent,
            "TimedBackup=" + _model.TimedBackup,
            "LastTemplate=" + _model.LastSelectedTemplate,
            "Version=" + _model.Version,
            "RecordPreferencesStatus=" + _model.RecordPreferencesStatus, //TODO: Fix spelling error
            "RecordVersionStatus=" + _model.RecordVersionStatus,
            "AutoSave=" + _model.AutoSave,
            "AutoSaveInterval=" + _model.AutoSaveInterval
        };

        if (_model.WrapNodeNames == TextWrapping.WrapWholeWords) { _newPreferences.Add("WrapNodeNames=True"); }
        else { _newPreferences.Add("WrapNodeNames=False"); }

        await FileIO.WriteLinesAsync(_preferencesFile, _newPreferences); //Writes file to disk.
    }
}