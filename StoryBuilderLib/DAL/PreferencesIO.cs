using StoryBuilder.Models.Tools;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StoryBuilder.Models;
using Windows.Storage;

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
            StorageFile preferencesFile = await preferencesFolder.GetFileAsync("StoryBuilder.prf");
            _preferences = await FileIO.ReadLinesAsync(preferencesFile);

            // match the file's contents and update
            foreach (string line in _preferences)
            {
                string[] tokens = line.Split(new char[] { '=' });
                switch (tokens[0])
                {
                    case "ProductName":
                        _model.ProductName = tokens[1];
                        break;
                    case "ProductVersion":
                        _model.ProductVersion = tokens[1];
                        break;
                    case "LicenseOwner":
                        _model.LicenseOwner = tokens[1];
                        break;
                    case "LicenseNumber":
                        _model.LicenseNumber = tokens[1];
                        break;
                    case "QuoteOnStartup":
                        if (tokens[1] == "Y")
                            _model.QuoteOnStartup = true;
                        else
                            _model.QuoteOnStartup = false;
                        break;
                    case "ScreenFont":
                        _model.ScreenFont = tokens[1];
                        break;
                    case "PrinterFont":
                        _model.PrinterFont = tokens[1];
                        break;
                    case "BackupOnOpen":
                        _model.BackupOnOpen = tokens[1];
                        break;
                    case "TimedBackup":
                        if (tokens[1] == "Y")
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
        }

        /// <summary>
        /// Update the StoryBuilder.prf file from PreferencesModel
        /// </summary>
        /// <returns></returns>
        public async Task UpdateFile()
        {
            _model = GlobalData.Preferences;

            IList<string> newPreferences = new List<string>();
            // create the file's contents from the model
            newPreferences.Add("[StoryBuilder]");
            newPreferences.Add("Copyright 2020 - Seven Valleys Software");
            newPreferences.Add("ProductName=StoryBuilder");
            newPreferences.Add("ProductVersion=2.00");
            newPreferences.Add(string.Empty);
            newPreferences.Add("[User Info]");
            newPreferences.Add("LicenseOwner=" + _model.LicenseOwner);
            newPreferences.Add(string.Empty);
            newPreferences.Add("[Backup Options]");
            newPreferences.Add("BackupOnOpen=" + _model.BackupOnOpen);
            newPreferences.Add("TimedBackup=" + (_model.TimedBackup ? "Y" : "N"));
            newPreferences.Add("TimedBackupInterval=" + _model.TimedBackupInterval.ToString());
            newPreferences.Add(string.Empty);
            newPreferences.Add("[Other Options]");
            newPreferences.Add("QuoteOnStartup=" + (_model.QuoteOnStartup ? "Y" : "N"));
            newPreferences.Add("ScreenFont=" + _model.ScreenFont);
            newPreferences.Add("PrinterFont=" + _model.PrinterFont);
            newPreferences.Add(string.Empty);
            newPreferences.Add("[File Preferences]");
            newPreferences.Add("ProjectDirectory=" + _model.ProjectDirectory);
            newPreferences.Add("BackupDirectory=" + _model.BackupDirectory);
            newPreferences.Add("LogDirectory=" + _model.LogDirectory);
            newPreferences.Add("LastFile1=" + _model.LastFile1);
            newPreferences.Add("LastFile2=" + _model.LastFile2);
            newPreferences.Add("LastFile3=" + _model.LastFile3);
            newPreferences.Add("LastFile4=" + _model.LastFile4);
            newPreferences.Add("LastFIle5=" + _model.LastFile5);

            StorageFolder preferencesFolder = await StorageFolder.GetFolderFromPathAsync(_path);
            StorageFile preferencesFile = await preferencesFolder.CreateFileAsync("StoryBuilder.prf",
                CreationCollisionOption.ReplaceExisting);
            await FileIO.AppendLinesAsync(preferencesFile, newPreferences);

        }
    }
}
