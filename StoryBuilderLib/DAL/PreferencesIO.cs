using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;
using StoryBuilder.Services.Logging;
using StoryBuilder.Models.Tools;
using CommunityToolkit.Mvvm.DependencyInjection;

namespace StoryBuilder.DAL
{
    /// <summary>
    /// This data access module handles file I/O and updates
    /// for the StoryBuilder.prf file and PreferencesModel  object. 
    /// </summary>
    public class PreferencesIO
    {
        private IList<string> _preferences;
        private PreferencesModel _model;
        private string _path;
        private LogService _log;

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
            //read the file into _preferences
            StorageFolder preferencesFolder = await StorageFolder.GetFolderFromPathAsync(_path);
            // Read the preferences file as a list of lines
            var preferencesFile = await preferencesFolder.TryGetItemAsync("StoryBuilder.prf") as IStorageFile;
            if (preferencesFile != null)
            {
                // The file exists
                _preferences = await FileIO.ReadLinesAsync(preferencesFile);

                // match the file's contents and update
                foreach (string line in _preferences)
                {
                    string[] tokens = line.Split(new char[] { '=' });
                    switch (tokens[0])
                    {
                        case "Name":
                            _model.Name = tokens[1];
                            break;

                        case "Email":
                            _model.Email = tokens[1];
                            break;

                        case "QuoteOnStartup": 
                            if (tokens[1] == "Y" || tokens[1] == "True")
                                _model.QuoteOnStartup = true;
                            else
                                _model.QuoteOnStartup = false;
                            break;

                        case "Initalised":
                            if (tokens[1] == "Y" || tokens[1] == "True")
                                _model.Initalised = true;
                            else
                                _model.Initalised = false;
                            break;

                        case "ErrorCollectionConsent":
                            if (tokens[1] == "Y" || tokens[1] == "True")
                                _model.ErrorCollectionConsent = true;
                            else
                                _model.ErrorCollectionConsent = false;
                            break;

                        case "Newsletter":
                            if (tokens[1] == "Y" || tokens[1] == "True")
                                _model.Newsletter = true;
                            else
                                _model.Newsletter = false;
                            break;

                        case "ForceDarkmode":
                            if (tokens[1] == "Y" || tokens[1] == "True")
                                _model.ForceDarkmode = true;
                            else
                                _model.ForceDarkmode = false;
                            break;

                        case "BackupOnOpen":
                            _model.BackupOnOpen = tokens[1];
                            break;

                        case "TimedBackup":
                            if (tokens[1] == "Y" || tokens[1] == "True")
                                _model.TimedBackup = true;
                            else
                                _model.TimedBackup = false;
                            break;

                        case "TimedBackupInterval":
                            _model.TimedBackupInterval = Convert.ToInt32(tokens[1]);
                            break;
                        case "InstallationDirectory":
                            _model.InstallationDirectory = tokens[1];
                            break;
                        case "ProjectDirectory":
                            _model.ProjectDirectory = tokens[1];
                            break;
                        case "BackupDirectory":
                            _model.BackupDirectory = tokens[1];
                            break;
                        case "LogDirectory":
                            _model.LogDirectory = tokens[1];
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
                    }
                }
                _log = Ioc.Default.GetService<LogService>();
                _log.Log(LogLevel.Info, "PreferencesModel updated from StoryBuilder.prf.");
            }
            else
            {
                // The file doesn't exist. } 
                _log = Ioc.Default.GetService<LogService>();
                _log.Log(LogLevel.Info, "StoryBuilder.prf not found; default created.");
            }
        }


        public async Task UpdateFile()
        {
            StorageFolder preferencesFolder = await StorageFolder.GetFolderFromPathAsync(_path);
            StorageFile preferencesFile = await preferencesFolder.CreateFileAsync("StoryBuilder.prf", CreationCollisionOption.ReplaceExisting);
            IList<string> NewPreferences = new List<string>();

            if (_model.Initalised == true) { NewPreferences.Add("Initalised=Y"); }
            else { NewPreferences.Add("Initalised=N"); }

            if (_model.TimedBackup == true) { NewPreferences.Add("TimedBackup=Y"); }
            else { NewPreferences.Add("TimedBackup=N"); }
            
            if (_model.ErrorCollectionConsent == true) { NewPreferences.Add("ErrorCollectionConsent=Y"); }
            else { NewPreferences.Add("ErrorCollectionConsent=N"); }

            if (_model.Newsletter == true) { NewPreferences.Add("Newsletter=Y"); }
            else { NewPreferences.Add("Newsletter=N"); }

            if (_model.ForceDarkmode == true) { NewPreferences.Add("ForceDarkmode=Y"); }
            else { NewPreferences.Add("ForceDarkmode=N"); }

            NewPreferences.Add("Name=" + _model.Name);
            NewPreferences.Add("Email=" + _model.Email);
            NewPreferences.Add("TimedBackupInterval=" + _model.TimedBackupInterval);
            NewPreferences.Add("InstallationDirectory=" + _model.InstallationDirectory);
            NewPreferences.Add("ProjectDirectory=" + _model.ProjectDirectory);
            NewPreferences.Add("BackupDirectory=" + _model.BackupDirectory);
            NewPreferences.Add("LastFile1=" + _model.LastFile1);
            NewPreferences.Add("LastFile2=" + _model.LastFile2);
            NewPreferences.Add("LastFile3=" + _model.LastFile3);
            NewPreferences.Add("LastFile4=" + _model.LastFile4);
            NewPreferences.Add("LastFile5=" + _model.LastFile5);

            if (_model.QuoteOnStartup == true) { NewPreferences.Add("QuoteOnStartup=Y"); }
            else { NewPreferences.Add("QuoteOnStartup=N"); }
            NewPreferences.Add("BackupOnOpen=" + _model.BackupOnOpen);
            NewPreferences.Add("LogDirectory=" + _model.LogDirectory);

            await FileIO.WriteLinesAsync(preferencesFile, NewPreferences);
        }
    }
}
