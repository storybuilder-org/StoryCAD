using StoryBuilder.Controllers;
using StoryBuilder.Models;
using StoryBuilder.Models.Tools;
using StoryBuilder.Services.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;

namespace StoryBuilder.DAL
{
    public class ControlLoader
    {
        public readonly StoryController Story;
        public readonly PreferencesModel Preferences;
        public readonly LogService Logger;

        public ControlLoader()
        {
        }

        private IList<string> lines;
        private string installFolder;
        public async Task Init(string path, StoryController story)
        {
            try
            {
                StorageFolder toolFolder = await StorageFolder.GetFolderFromPathAsync(path);
                installFolder = toolFolder.Path;
                StorageFile iniFile = await toolFolder.GetFileAsync("Controls.ini");
                lines = await FileIO.ReadLinesAsync(iniFile, Windows.Storage.Streams.UnicodeEncoding.Utf8);
            }
            catch (Exception ex) 
            {
                Console.WriteLine(ex.Message);
            }
            // Populate UserControl data source collections
            GlobalData.ConflictTypes = LoadConflictTypes();
            GlobalData.RelationTypes = LoadRelationTypes();
            //story.KeyQuestionsSource = LoadKeyQuestions();
            //story.StockScenesSource = LoadStockScenes();
            //story.TopicsSource = LoadTopics();
            //story.MasterPlotsSource = LoadMasterPlots();
            //story.DramaticSituationsSource = LoadDramaticSituations();
            //story.QuotesSource = LoadQuotes();
            Clear();
        }

        public SortedDictionary<string, ConflictCategoryModel> LoadConflictTypes()
        {
            ConflictCategoryModel currentConflictType = null;
            SortedDictionary<string, ConflictCategoryModel> conflictTypes = new SortedDictionary<string, ConflictCategoryModel>();
            string currentSubtype = string.Empty;

            string section = string.Empty;
            string keyword = string.Empty;
            string keyvalue = string.Empty;
            foreach (string line in lines)
            {
                ParseLine(line, ref section, ref keyword, ref keyvalue);
                //   Process the parsed values
                switch (section)
                {
                    case "Conflict Types":
                        switch (keyword)
                        {
                            case "":
                                break;
                            case "Type":
                                currentConflictType = new ConflictCategoryModel(keyvalue);
                                conflictTypes.Add(keyvalue, currentConflictType);
                                break;
                            case "Subtype":
                                currentSubtype = keyvalue;
                                currentConflictType.SubCategories.Add(keyvalue);
                                currentConflictType.Examples.Add(currentSubtype, new List<string>());
                                break;
                            case "Example":
                                currentConflictType.Examples[currentSubtype].Add(keyvalue);
                                break;
                        }
                        break;
                }
            }
            return conflictTypes;
        }

        public List<RelationType> LoadRelationTypes()
        {
           List<RelationType> relationships = new List<RelationType>();

            string section = string.Empty;
            string keyword = string.Empty;
            string keyvalue = string.Empty;
            foreach (string line in lines)
            {
                ParseLine(line, ref section, ref keyword, ref keyvalue);
                //   Process the parsed values
                switch (section)
                {
                    case "RelationTypes":
                        switch (keyword)
                        {
                            case "":
                                break;
                            case "RelationType":
                                string[] tokens = keyvalue.Split(',');
                                if (tokens.Length != 3)
                                    continue;
                                relationships.Add(new RelationType(tokens[0], tokens[1]));
                                break;
                        }
                        break;
                }
            }
            return relationships;
        }

        /// <summary>
        /// Parse a line from the Controls.ini file into section, keyword, and keyvalue.
        /// 
        /// Parsed tokens are passed by reference and left unchanged if not found in
        /// the parse. So for example, section is not modified if parsing a 
        /// keyword=keyvalue line.
        /// </summary>
        /// <param name="line">The line to be parsed</param>
        /// <param name="section">The [section] section name, if present</param>
        /// <param name="keyword">The keyword=keyvalue keyword parameter, if present</param>
        /// <param name="keyvalue">The </param>
        private static void ParseLine(string line, ref string section, ref string keyword, ref string keyvalue)
        {
            line = line.TrimEnd();
            if (line.Equals(string.Empty))
            {
                keyword = string.Empty;
                keyvalue = string.Empty;
                return;
            }
            if (line.StartsWith(";")) // Comment
                return;
            if (line.StartsWith("["))
            {
                string[] tokens = line.Split(new[] { '[', ']' });
                section = tokens[1];
                keyword = "$SECTION$";
                keyvalue = string.Empty;
                return;
            }
            if (line.Contains("="))
            {
                string[] tokens = line.Split(new[] { '=' });
                keyword = tokens[0];
                keyvalue = tokens[1].TrimEnd();
                return;
            }
            if (line.StartsWith("="))
            {
                keyword = string.Empty;
                keyvalue = line.Substring(1).TrimEnd();
            }

        }

        public void Clear()
        {
            lines = null;
        }

        private void LogEntry(string line)
        {
            // TODO: Code LogEntry details (requires logging framework)
            throw new NotImplementedException();
        }
    }
}
