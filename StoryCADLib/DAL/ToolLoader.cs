using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCAD.Models;
using StoryCAD.Models.Tools;
using StoryCAD.Services.Logging;
using System.Reflection;

namespace StoryCAD.DAL;

public class ToolLoader
{
    public readonly LogService Logger = Ioc.Default.GetRequiredService<LogService>();

    private IList<string> _lines;
    public async Task<List<object>> Init()
    {
        try
        {
            await using Stream internalResourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("StoryCAD.Assets.Install.Tools.ini");
            using StreamReader reader = new(internalResourceStream);
            _lines = (await reader.ReadToEndAsync()).Split("\n");

            // Populate tool data source collections
            List<object> Tools = new()
            {
                LoadKeyQuestions(),
                LoadStockScenes(),
                LoadTopics(),
                LoadMasterPlots(),
                LoadDramaticSituations()
            };

            Clear();
            return Tools;
        }
        catch (Exception _ex) { Logger.LogException(LogLevel.Error, _ex, "Error Initializing tool loader"); }
        return null;
    }

    public Dictionary<string, List<KeyQuestionModel>> LoadKeyQuestions()
    {
        string _previousKey = string.Empty;
        KeyQuestionModel _current = null;
        string _section = string.Empty;
        string _keyword = string.Empty;
        string _keyValue = string.Empty;
        string _element = string.Empty;
        string _topic = string.Empty;
        Dictionary<string, List<KeyQuestionModel>> _questions = new();
        foreach (string _line in _lines)
        {
            ParseLine(_line, ref _section, ref _keyword, ref _keyValue);
            //   Process the parsed values
            switch (_section)
            {
                case "Key Questions":
                    switch (_keyword)
                    {
                        case "$SECTION$":
                            break;
                        case "Element":  // new list of questions for each StoryElement (Overview, Problem, etc.)
                            _element = _keyValue;
                            _questions.Add(_element, new List<KeyQuestionModel>());
                            break;
                        case "Topic":
                            _topic = _keyValue;
                            break;
                        default:
                            if (!_keyword.Equals(_previousKey))
                            {
                                _current = new KeyQuestionModel
                                {
                                    Key = _keyword,
                                    Element = _element,
                                    Topic = _topic,
                                    Question = _keyValue
                                };
                                _questions[_element].Add(_current);
                                _previousKey = _keyword;
                            }
                            else
                            {
                                _current!.Question = _current.Question + " " + _keyValue;
                            }
                            break;
                    }
                    break;
            }
        }
        return _questions;
    }

    public SortedDictionary<string, ObservableCollection<string>> LoadStockScenes()
    {
        string _stockSceneCategory = string.Empty;
        string _section = string.Empty;
        string _keyword = string.Empty;
        string _keyvalue = string.Empty;
        SortedDictionary<string, ObservableCollection<string>> _stockScenes = new();
        foreach (string _line in _lines)
        {
            ParseLine(_line, ref _section, ref _keyword, ref _keyvalue);
            //   Process the parsed values
            switch (_section)
            {
                case "Stock Scenes":
                    switch (_keyword)
                    {
                        case "":
                            break;
                        case "Title":
                            _stockScenes.Add(_keyvalue, new ObservableCollection<string>());
                            _stockSceneCategory = _keyvalue;
                            break;
                        case "Scene":
                            _stockScenes[_stockSceneCategory].Add(_keyvalue);
                            break;
                    }
                    break;
            }
        }
        return _stockScenes;
    }

    public SortedDictionary<string, TopicModel> LoadTopics()
    {
        string _topicName = string.Empty;
        TopicModel _currentTopic = null;
        SubTopicModel _currentSubTopic = null;
        SortedDictionary<string, TopicModel> _topics = new();
        string _section = string.Empty;
        string _keyword = string.Empty;
        string _keyvalue = string.Empty;
        foreach (string _line in _lines)
        {
            ParseLine(_line, ref _section, ref _keyword, ref _keyvalue);
            //   Process the parsed values
            switch (_section)
            {
                case "Topic Information":
                    switch (_keyword)
                    {
                        case "":
                            break;
                        case "Topic":
                            _topicName = _keyvalue;
                            _currentSubTopic = null;
                            break;
                        case "Notepad":
                            string _path = _keyvalue.IndexOf('\\') >= 0 ? _keyvalue : Path.Combine(Path.GetTempPath(), _keyvalue);
                            _topics.Add(_topicName, new TopicModel(_topicName, _path));
                            break;
                        case "Subtopic":
                            if (_currentSubTopic == null)
                            {
                                _currentTopic = new TopicModel(_topicName);
                                _topics.Add(_topicName, _currentTopic);
                            }
                            _currentSubTopic = new SubTopicModel(_keyvalue);
                            _currentTopic!.SubTopics.Add(_currentSubTopic);
                            break;
                        case "Remarks":
                            if (_currentSubTopic!.SubTopicNotes.Equals(string.Empty))
                                _currentSubTopic.SubTopicNotes = _keyvalue;
                            else
                            {
                                if (!_currentSubTopic.SubTopicNotes.EndsWith(" "))
                                    _currentSubTopic.SubTopicNotes += " ";
                                _currentSubTopic.SubTopicNotes += _keyvalue;
                            }
                            break;
                    }
                    break;
            }
        }
        return _topics;
    }

    public List<MasterPlotModel> LoadMasterPlots()
    {
        MasterPlotModel _currentMasterPlot = null;
        MasterPlotScene _currentMasterPlotScene = null;
        List<MasterPlotModel> _masterPlots = new();
        string _section = string.Empty;
        string _keyword = string.Empty;
        string _keyvalue = string.Empty;
        foreach (string _line in _lines)
        {
            ParseLine(_line, ref _section, ref _keyword, ref _keyvalue);
            //   Process the parsed values
            switch (_section)
            {
                case "MasterPlots":
                    switch (_keyword)
                    {
                        case "":
                            break;
                        case "MasterPlot":
                            _currentMasterPlot = new MasterPlotModel(_keyvalue);
                            _masterPlots.Add(_currentMasterPlot);
                            break;
                        case "Remarks":
                            // ReSharper disable PossibleNullReferenceException
                            if (_currentMasterPlot.MasterPlotNotes.Equals(string.Empty))
                                _currentMasterPlot.MasterPlotNotes = _keyvalue;
                            else
                            {
                                _currentMasterPlot.MasterPlotNotes += Environment.NewLine;
                                _currentMasterPlot.MasterPlotNotes += _keyvalue;
                            }
                            break;
                        case "PlotPoint":
                        case "Scene":
                            _currentMasterPlotScene = new MasterPlotScene(_keyvalue);
                            _currentMasterPlot.MasterPlotScenes.Add(_currentMasterPlotScene);
                            break;
                        case "Notes":
                            if (_currentMasterPlotScene.Notes.Equals(string.Empty))
                                _currentMasterPlotScene.Notes = _keyvalue;
                            else
                            {
                                _currentMasterPlotScene.Notes += Environment.NewLine;
                                _currentMasterPlotScene.Notes += _keyvalue;
                            }
                            // ReSharper restore PossibleNullReferenceException
                            break;
                    }
                    break;
            }
        }
        return _masterPlots;
    }

    public SortedDictionary<string, DramaticSituationModel> LoadDramaticSituations()
    {
        DramaticSituationModel _currentDramaticSituationModel = null;
        SortedDictionary<string, DramaticSituationModel> _dramaticSituations = new();
        string _section = string.Empty;
        foreach (string _line in _lines)
        {
            string _keyword = string.Empty;
            string _keyvalue = string.Empty;
            ParseLine(_line, ref _section, ref _keyword, ref _keyvalue);
            //   Process the parsed values
            switch (_section)
            {
                case "Dramatic Situations":
                    switch (_keyword)
                    {
                        case "":
                            break;
                        case "Situation":
                            _currentDramaticSituationModel = new DramaticSituationModel(_keyvalue);
                            _dramaticSituations.Add(_keyvalue, _currentDramaticSituationModel);
                            break;
                        case "Role1":
                            // ReSharper disable PossibleNullReferenceException
                            _currentDramaticSituationModel.Role1 = _keyvalue;
                            break;
                        case "Role2":
                            _currentDramaticSituationModel.Role2 = _keyvalue;
                            break;
                        case "Role3":
                            _currentDramaticSituationModel.Role3 = _keyvalue;
                            break;
                        case "Role4":
                            _currentDramaticSituationModel.Role4 = _keyvalue;
                            break;
                        case "Desc1":
                            _currentDramaticSituationModel.Description1 = _keyvalue;
                            break;
                        case "Desc2":
                            _currentDramaticSituationModel.Description2 = _keyvalue;
                            break;
                        case "Desc3":
                            _currentDramaticSituationModel.Description3 = _keyvalue;
                            break;
                        case "Desc4":
                            _currentDramaticSituationModel.Description4 = _keyvalue;
                            break;
                        case "Example":
                            //TODO: Process Example lines
                            break;
                        case "Notes":
                            _currentDramaticSituationModel.Notes += _keyvalue;
                            // ReSharper restore PossibleNullReferenceException
                            break;
                    }
                    break;
            }
        }
        return _dramaticSituations;
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
            string[] _tokens = line.Split('[', ']');
            section = _tokens[1];
            keyword = "$SECTION$";
            keyvalue = string.Empty;
            return;
        }
        if (line.Contains("="))
        {
            string[] _tokens = line.Split(new[] { '=' });
            keyword = _tokens[0];
            keyvalue = _tokens[1].TrimEnd();
            return;
        }
        if (line.StartsWith("="))
        {
            keyword = string.Empty;
            keyvalue = line[1..].TrimEnd();
        }

    }

    public void Clear() { _lines = null; }
}