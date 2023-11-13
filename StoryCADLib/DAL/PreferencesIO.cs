using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using StoryCAD.Models;
using StoryCAD.Models.Tools;
using StoryCAD.Services.Logging;
using Windows.UI.ViewManagement;
using System.Diagnostics;

namespace StoryCAD.DAL;

/// <summary>
/// This data access module handles file I/O and updates
/// for the StoryCAD.prf file and PreferencesModel object. 
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
    /// <param name="path">The folder path StoryCAD.prf resides in</param>
    public PreferencesIo(PreferencesModel model, string path)
    {
        _model = model;
        _path = path;
    }

    /// <summary>
    /// Update the model from the .prf file's contents 
    /// </summary>
    public async Task ReadPreferences()
    {
        //Tries to read file
        StorageFolder _preferencesFolder = await StorageFolder.GetFolderFromPathAsync(_path);
        IStorageFile _preferencesFile = (IStorageFile)await _preferencesFolder.TryGetItemAsync("StoryCAD.prf");

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
                    //TODO: Remove this case after March 2024.
                    //This case can only ever execute on PRF files created pre-2.13
                    case "Name":
                        string[] names = _tokens[1].Split(" ");
                        if (names.Length == 2)
                        {
                            _model.FirstName = names[0];
                            _model.LastName = names[1];
                        }
                        else
                        {
                            _model.FirstName = _tokens[1];
                            _model.LastName = "";
                        }
                        break;
                    case "FirstName":
                        _model.FirstName = _tokens[1];
                        break;
                    case "LastName":
                        _model.LastName = _tokens[1];
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
                            _model.RecordVersionStatus = false;
                        break;
                    case "WrapNodeNames":
                        if (_tokens[1] == "True") { _model.WrapNodeNames = TextWrapping.WrapWholeWords; }
                        else { _model.WrapNodeNames = TextWrapping.NoWrap; }
                        break;
                    case "AutoSave":
                        if (_tokens[1] == "True") { _model.AutoSave = true; }
                        else { _model.AutoSave = false; }
                        break;
                    case "AutoSaveInterval":
                        _model.AutoSaveInterval = Convert.ToInt32(_tokens[1]);
                        break;
                    case "SearchEngine":
                        object val;
                        BrowserType.TryParse(typeof(BrowserType), _tokens[1].ToCharArray(), true, out val);
                        _model.PreferredSearchEngine = (BrowserType)val;
                        break;
                }
            }
            _log.Log(LogLevel.Info, "PreferencesModel updated from StoryCAD.prf.");
        }
        else
        {
            // The file doesn't exist.
            _log.Log(LogLevel.Info, "StoryCAD.prf not found; default created.");
        }

        if (Ioc.Default.GetService<Windowing>().RequestedTheme == ElementTheme.Light)
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

    /// <summary>
    /// This writes the file to disk using given
    /// preferences model.
    /// </summary>
    public async Task WritePreferences()
    {
        _log.Log(LogLevel.Info, "Updating prf from model.");
        StorageFolder _preferencesFolder = await StorageFolder.GetFolderFromPathAsync(_path);
        StorageFile _preferencesFile = await _preferencesFolder.CreateFileAsync("StoryCAD.prf", CreationCollisionOption.ReplaceExisting);

        string _newPreferences = $"""
            FirstName={_model.FirstName}
            LastName={_model.LastName}
            Email={_model.Email}
            ErrorCollectionConsent={_model.ErrorCollectionConsent}
            Newsletter={_model.Newsletter}
            Initalised={_model.PreferencesInitialized}
            LastTemplate={_model.LastSelectedTemplate}
            WrapNodeNames={_model.WrapNodeNames}
            LastFile1={_model.LastFile1}
            LastFile2={_model.LastFile2}
            LastFile3={_model.LastFile3}
            LastFile4={_model.LastFile4}
            LastFile5={_model.LastFile5}
            ProjectDirectory={_model.ProjectDirectory}
            BackupDirectory={_model.BackupDirectory}
            AutoSave={_model.AutoSave}
            AutoSaveInterval={_model.AutoSaveInterval} 
            BackupOnOpen={_model.BackupOnOpen}
            TimedBackup={_model.TimedBackup}
            TimedBackupInterval={_model.TimedBackupInterval}
            Version={_model.Version}
            RecordPreferencesStatus={_model.RecordPreferencesStatus}
            RecordVersionStatus={_model.RecordVersionStatus}
            SearchEngine={_model.PreferredSearchEngine}
            """;

        _newPreferences += (_model.WrapNodeNames == TextWrapping.WrapWholeWords ? Environment.NewLine + "WrapNodeNames=True" : Environment.NewLine + "WrapNodeNames=False");
        await FileIO.WriteTextAsync(_preferencesFile, _newPreferences); //Writes file to disk.
    }
}