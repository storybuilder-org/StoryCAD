using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;
using StoryBuilder.Services.Logging;
using StoryBuilder.Models.Tools;
using StoryBuilder.Services.Json;
using StoryBuilder.Services.Parse;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

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

                    case "QuoteOnStartup":
                        if (tokens[1] == "True") { _model.QuoteOnStartup = true; }
                        else { _model.QuoteOnStartup = false; }
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
                        _model.BackupOnOpen = tokens[1];
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
                        if (tokens[1] != _model.Version) {/*Report change here*/}
                        break;
                    case "ParsePreferencesStatus":
                         if (tokens[1] == "True") 
                            _model.ParsePreferencesStatus = true; 
                        else 
                            _model.ParsePreferencesStatus = false; 
                        break;
                    case "ParseVersionStatus":
                        if (tokens[1] == "True")
                            _model.ParseVersionStatus = true;
                        else
                            _model.ParseVersionStatus= false;
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
            _model.PrimaryColor = new SolidColorBrush(Microsoft.UI.Colors.LightGray);
            _model.SecondaryColor = new SolidColorBrush(Microsoft.UI.Colors.Black);
        }
        else
        {
            _model.PrimaryColor = new SolidColorBrush(Microsoft.UI.Colors.DarkSlateGray);
            _model.SecondaryColor = new SolidColorBrush(Microsoft.UI.Colors.White);
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
        NewPreferences.Add("QuoteOnStartup=" + _model.QuoteOnStartup);
        NewPreferences.Add("BackupOnOpen=" + _model.BackupOnOpen);
        NewPreferences.Add("ErrorCollectionConsent=" + _model.ErrorCollectionConsent);
        NewPreferences.Add("TimedBackup=" + _model.TimedBackup);
        NewPreferences.Add("LastTemplate=" + _model.LastSelectedTemplate);
        NewPreferences.Add("Version=" + _model.Version);
        NewPreferences.Add("ParsePreferencesStaus=" + _model.ParsePreferencesStatus);
        NewPreferences.Add("ParseVersionStaus=" + _model.ParseVersionStatus);

        await FileIO.WriteLinesAsync(preferencesFile, NewPreferences); //Write the Preferences file to disk.
    }
}