using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

namespace StoryBuilder.DAL
{
    public class ListLoader
    {
        #region Public Methods

        public async Task<Dictionary<string, ObservableCollection<string>>>  Init(string path)
        {
            Dictionary<string, ObservableCollection<string>> lists = new Dictionary<string, ObservableCollection<string>>();
            
            StorageFolder controlFolder = await StorageFolder.GetFolderFromPathAsync(path);
            StorageFile iniFile = await controlFolder.GetFileAsync("Controls.ini");
            //See if the .INI file exists
            //string iniFile = Path.Combine(Controller.Controller.SystemDirectory, @"\STORYB.INI");
            // string iniFile = @"C:\STORYB\STORYB.INI";
            //if (!File.Exists(iniFile))
            //    throw new Exception(@"STORY.INI initialization file not found");

            // Read the Application .INI file. Each record is the format 'KeyWord=Keyvalue'.
            // As each record is read, it's moved to the corresponding initialization
            // structure field or loaded as an initialization value for a contol
            string text = await FileIO.ReadTextAsync(iniFile);
            StringReader sr = new StringReader(text);
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                line = line.TrimEnd();
                if (line.Equals(string.Empty))
                    continue;
                if (line.StartsWith(";")) // Comment
                    continue;
                if (line.Contains("="))
                {
                    string[] tokens = line.Split(new[] { '=' });
                    string keyword = tokens[0];
                    string keyvalue = tokens[1];
                    if (!lists.ContainsKey(keyword))
                        lists.Add(keyword, new ObservableCollection<string>());
                    lists[keyword].Add(keyvalue);
                }
            }
            return lists;
        }
    #endregion
    }
}
