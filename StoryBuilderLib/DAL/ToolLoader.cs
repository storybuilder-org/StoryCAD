using StoryBuilder.Models;
using StoryBuilder.Models.Tools;
using StoryBuilder.Services.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

namespace StoryBuilder.DAL
{
    public class ToolLoader
    {
        public readonly PreferencesModel Preferences;
        public readonly LogService Logger;

        private IList<string> lines;
        private string installFolder;
        public async Task Init(string path)
        {
            try
            {
                StorageFolder toolFolder = await StorageFolder.GetFolderFromPathAsync(path);
                installFolder = toolFolder.Path;
                StorageFile iniFile = await toolFolder.GetFileAsync("Tools.ini");
                lines = await FileIO.ReadLinesAsync(iniFile);

                // Populate tool data source collections
                GlobalData.KeyQuestionsSource = LoadKeyQuestions();
                GlobalData.StockScenesSource = LoadStockScenes();
                GlobalData.TopicsSource = LoadTopics();
                GlobalData.MasterPlotsSource = LoadMasterPlots();
                GlobalData.DramaticSituationsSource = LoadDramaticSituations();
                GlobalData.QuotesSource = LoadQuotes();
                Clear();
            }
            catch
            {
                
                Logger.Log(LogLevel.Error, "Error in ToolLoader.Init");
            }
        }

        public Dictionary<string, List<KeyQuestionModel>> LoadKeyQuestions()
        {
            string previousKey = string.Empty;
            KeyQuestionModel current = null;
            string section = string.Empty;
            string keyword = string.Empty;
            string keyvalue = string.Empty;
            string element = string.Empty;
            string topic = string.Empty;
            Dictionary<string, List<KeyQuestionModel>> questions = new();
            foreach (string line in lines)
            {
                ParseLine(line, ref section, ref keyword, ref keyvalue);
                //   Process the parsed values
                switch (section)
                {
                    case "Key Questions":
                        switch (keyword)
                        {
                            case "$SECTION$":
                                break;
                            case "Element":  // new list of questions for each StoryElement (Overview, Problem, etc.)
                                element = keyvalue;
                                questions.Add(element, new List<KeyQuestionModel>());
                                break;
                            case "Topic":
                                topic = keyvalue;
                                break;
                            default:
                                string questionKey = keyword;
                                if (!questionKey.Equals(previousKey))
                                {
                                    current = new KeyQuestionModel
                                    {
                                        Key = questionKey,
                                        Element = element,
                                        Topic = topic,
                                        Question = keyvalue
                                    };
                                    questions[element].Add(current);
                                    previousKey = questionKey;
                                }
                                else
                                {
                                    current.Question = current.Question + " " + keyvalue;
                                }
                                break;
                        }
                        break;
                }
            }
            return questions;
        }

        public SortedDictionary<string, ObservableCollection<string>> LoadStockScenes()
        {
            string stockSceneCategory = string.Empty;
            string section = string.Empty;
            string keyword = string.Empty;
            string keyvalue = string.Empty;
            SortedDictionary<string, ObservableCollection<string>> stockScenes = new();
            foreach (string line in lines)
            {
                ParseLine(line, ref section, ref keyword, ref keyvalue);
                //   Process the parsed values
                switch (section)
                {
                    case "Stock Scenes":
                        switch (keyword)
                        {
                            case "":
                                break;
                            case "Title":
                                stockScenes.Add(keyvalue, new ObservableCollection<string>());
                                stockSceneCategory = keyvalue;
                                break;
                            case "Scene":
                                stockScenes[stockSceneCategory].Add(keyvalue);
                                break;
                        }
                        break;
                }
            }
            return stockScenes;
        }

        public SortedDictionary<string, TopicModel> LoadTopics()
        {
            string topicName = string.Empty;
            TopicModel currentTopic = null;
            SubTopicModel currentSubTopic = null;
            SortedDictionary<string, TopicModel> topics = new();
            string section = string.Empty;
            string keyword = string.Empty;
            string keyvalue = string.Empty;
            foreach (string line in lines)
            {
                ParseLine(line, ref section, ref keyword, ref keyvalue);
                //   Process the parsed values
                switch (section)
                {
                    case "Topic Information":
                        switch (keyword)
                        {
                            case "":
                                break;
                            case "Topic":
                                topicName = keyvalue;
                                currentSubTopic = null;
                                break;
                            case "Notepad":
                                string path = keyvalue.IndexOf('\\') >= 0 ? keyvalue : Path.Combine(installFolder, keyvalue);
                                topics.Add(topicName, new TopicModel(topicName, path));
                                break;
                            case "Subtopic":
                                if (currentSubTopic == null)
                                {
                                    currentTopic = new TopicModel(topicName);
                                    topics.Add(topicName, currentTopic);
                                }
                                currentSubTopic = new SubTopicModel(keyvalue);
                                currentTopic.SubTopics.Add(currentSubTopic);
                                break;
                            case "Remarks":
                                if (currentSubTopic.SubTopicNotes.Equals(string.Empty))
                                    currentSubTopic.SubTopicNotes = keyvalue;
                                else
                                {
                                    if (!currentSubTopic.SubTopicNotes.EndsWith(" "))
                                        currentSubTopic.SubTopicNotes += " ";
                                    currentSubTopic.SubTopicNotes += keyvalue;
                                }
                                break;
                        }
                        break;
                }
            }
            return topics;
        }

        public List<MasterPlotModel> LoadMasterPlots()
        {
            MasterPlotModel currentMasterPlot = null;
            MasterPlotScene currentMasterPlotScene = null;
            List<MasterPlotModel> masterPlots = new();
            string section = string.Empty;
            string keyword = string.Empty;
            string keyvalue = string.Empty;
            foreach (string line in lines)
            {
                ParseLine(line, ref section, ref keyword, ref keyvalue);
                //   Process the parsed values
                switch (section)
                {
                    case "MasterPlots":
                        switch (keyword)
                        {
                            case "":
                                break;
                            case "MasterPlot":
                                currentMasterPlot = new MasterPlotModel(keyvalue);
                                masterPlots.Add(currentMasterPlot);
                                break;
                            case "Remarks":
                                // ReSharper disable PossibleNullReferenceException
                                if (currentMasterPlot.MasterPlotNotes.Equals(string.Empty))
                                    currentMasterPlot.MasterPlotNotes = keyvalue;
                                else
                                {
                                    currentMasterPlot.MasterPlotNotes += Environment.NewLine;
                                    currentMasterPlot.MasterPlotNotes += keyvalue;
                                }
                                break;
                            case "PlotPoint":
                            case "Scene":
                                currentMasterPlotScene = new MasterPlotScene(keyvalue);
                                currentMasterPlot.MasterPlotScenes.Add(currentMasterPlotScene);
                                break;
                            case "Notes":
                                if (currentMasterPlotScene.Notes.Equals(string.Empty))
                                    currentMasterPlotScene.Notes = keyvalue;
                                else
                                {
                                    currentMasterPlotScene.Notes += Environment.NewLine;
                                    currentMasterPlotScene.Notes += keyvalue;
                                }
                                // ReSharper restore PossibleNullReferenceException
                                break;
                        }
                        break;
                }
            }
            return masterPlots;
        }

        public SortedDictionary<string, DramaticSituationModel> LoadDramaticSituations()
        {
            DramaticSituationModel currentDramaticSituationModel = null;
            SortedDictionary<string, DramaticSituationModel> dramaticSituations = new();
            string section = string.Empty;
            foreach (string line in lines)
            {
                string keyword = string.Empty;
                string keyvalue = string.Empty;
                ParseLine(line, ref section, ref keyword, ref keyvalue);
                //   Process the parsed values
                switch (section)
                {
                    case "Dramatic Situations":
                        switch (keyword)
                        {
                            case "":
                                break;
                            case "Situation":
                                currentDramaticSituationModel = new DramaticSituationModel(keyvalue);
                                dramaticSituations.Add(keyvalue, currentDramaticSituationModel);
                                break;
                            case "Role1":
                                // ReSharper disable PossibleNullReferenceException
                                currentDramaticSituationModel.Role1 = keyvalue;
                                break;
                            case "Role2":
                                currentDramaticSituationModel.Role2 = keyvalue;
                                break;
                            case "Role3":
                                currentDramaticSituationModel.Role3 = keyvalue;
                                break;
                            case "Role4":
                                currentDramaticSituationModel.Role4 = keyvalue;
                                break;
                            case "Desc1":
                                currentDramaticSituationModel.Description1 = keyvalue;
                                break;
                            case "Desc2":
                                currentDramaticSituationModel.Description2 = keyvalue;
                                break;
                            case "Desc3":
                                currentDramaticSituationModel.Description3 = keyvalue;
                                break;
                            case "Desc4":
                                currentDramaticSituationModel.Description4 = keyvalue;
                                break;
                            case "Example":
                                //TODO: Process Example lines
                                break;
                            case "Notes":
                                currentDramaticSituationModel.Notes += keyvalue;
                                // ReSharper restore PossibleNullReferenceException
                                break;
                        }
                        break;
                }
            }
            return dramaticSituations;
        }

        public ObservableCollection<Quotation> LoadQuotes()
        {
            ObservableCollection<Quotation> quotes = new();
            Quotation currentQuote = null;
            string section = string.Empty;
            string keyword = string.Empty;
            string keyvalue = string.Empty;
            foreach (string line in lines)
            {
                ParseLine(line, ref section, ref keyword, ref keyvalue);
                //   Process the parsed values
                switch (section)
                {
                    case "Quotes":
                        switch (keyword)
                        {
                            case "":
                                break;
                            case "Author":
                                currentQuote = new Quotation { Author = keyvalue, Quote = string.Empty };
                                quotes.Add(currentQuote);
                                break;
                            case "Quote":
                                // ReSharper disable PossibleNullReferenceException
                                if (currentQuote.Quote.Equals(string.Empty))
                                    currentQuote.Quote = keyvalue.TrimEnd();
                                else
                                {
                                    currentQuote.Quote += Environment.NewLine;
                                    currentQuote.Quote += keyvalue.TrimEnd();
                                }
                                // ReSharper restore PossibleNullReferenceException
                                break;
                        }
                        break;
                }
            }
            return quotes;
        }
         
        /// <summary>
        /// Parse a line from the TOOLS.INI file into section, keyword, and keyvalue.
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
                keyvalue = line[1..].TrimEnd();
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
