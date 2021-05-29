using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Reflection;
using Windows.Storage;
using StoryBuilder.Models.Tools;

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
            IList<string> newPreferences = new List<string>();
            // match the file's contents and update
            foreach (string line in _preferences)
            {
                string newline = string.Empty;
                if (line.Contains("="))
                {
                    string[] tokens = line.Split(new char[] { '=' });
                    string key = tokens[0];
                    string value = string.Empty;
                    switch (key)
                    {
                        case "ProductName":
                            value = _model.ProductName;
                            break;
                        case "ProductVersion":
                            value = _model.ProductVersion;
                            break;
                        case "LicenseOwner":
                            value = _model.LicenseOwner;
                            break;
                        case "LicenseNumber":
                            value = _model.LicenseNumber;
                            break;
                        case "QuoteOnStartup":
                            if (_model.QuoteOnStartup == true)
                                value = "Y";
                            else
                                value = "N";
                            break;
                        case "ScreenFont":
                            value = _model.ScreenFont;
                            break;
                        case "PrinterFont":
                            value = _model.PrinterFont;
                            break;
                        case "BackupOnOpen":
                            value = _model.BackupOnOpen;
                            break;
                        case "TimedBackup":
                            if (_model.TimedBackup == true)
                                value = "Y";
                            else
                                value = "N";
                            break;
                        case "TimedBackupInterval":
                            value = Convert.ToString(_model.TimedBackupInterval);
                            break;
                        case "Installationirectory":
                            value = _model.InstallationDirectory;
                            break;
                        case "ProjectDirectory":
                            value = _model.ProjectDirectory;
                            break;
                        case "BackupDirectory":
                            value = _model.BackupDirectory;
                            break;
                        case "LogDirectory":
                            value = _model.LogDirectory;
                            break;
                        case "LastFile1":
                            value = _model.LastFile1;
                            break;
                        case "LastFile2":
                            value = _model.LastFile2;
                            break;
                        case "LastFile3":
                            value = _model.LastFile3;
                            break;
                        case "LastFile4":
                            value = _model.LastFile4;
                            break;
                        case "LastFile5":
                            value = _model.LastFile5;
                            break;
                    }
                    newline = key + "=" + value;
                }
                else
                    newline = line;
                newPreferences.Add(newline);
            }
            StorageFolder preferencesFolder = await StorageFolder.GetFolderFromPathAsync(_path);
            StorageFile preferencesFile = await preferencesFolder.CreateFileAsync("StoryBuilder.prf",
                CreationCollisionOption.ReplaceExisting);
            await FileIO.AppendLinesAsync(preferencesFile, newPreferences);
        }
    }

}
