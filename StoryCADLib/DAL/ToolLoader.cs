using System.Collections.ObjectModel;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using StoryCAD.Models.Tools;

namespace StoryCAD.DAL;

public class ToolLoader
{
    private readonly ILogService _logger;
    private ToolsJsonData _toolsData;

    public ToolLoader(ILogService logger)
    {
        _logger = logger;
    }

    public async Task<List<object>> Init()
    {
        try
        {
            await using var internalResourceStream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("StoryCADLib.Assets.Install.Tools.json");
            using StreamReader reader = new(internalResourceStream);
            var json = await reader.ReadToEndAsync();
            _toolsData = JsonSerializer.Deserialize<ToolsJsonData>(json);

            // Populate tool data source collections
            List<object> Tools = new()
            {
                LoadKeyQuestions(),
                LoadStockScenes(),
                LoadTopics(),
                LoadMasterPlots(),
                LoadBeatsheets(),
                LoadDramaticSituations(),
                LoadMaleFirstNames(),
                LoadFemaleFirstNames(),
                LoadLastNames(),
                LoadRelationships()
            };

            Clear();
            return Tools;
        }
        catch (Exception _ex)
        {
            _logger.LogException(LogLevel.Error, _ex, "Error Initializing tool loader");
        }

        return null;
    }

    public Dictionary<string, List<KeyQuestionModel>> LoadKeyQuestions()
    {
        var questions = new Dictionary<string, List<KeyQuestionModel>>();

        if (_toolsData?.KeyQuestions != null)
        {
            foreach (var kvp in _toolsData.KeyQuestions)
            {
                questions[kvp.Key] = new List<KeyQuestionModel>();
                foreach (var q in kvp.Value)
                {
                    questions[kvp.Key].Add(new KeyQuestionModel
                    {
                        Key = q.Key,
                        Element = kvp.Key,
                        Topic = q.Topic,
                        Question = q.Question
                    });
                }
            }
        }

        return questions;
    }

    public SortedDictionary<string, ObservableCollection<string>> LoadStockScenes()
    {
        var stockScenes = new SortedDictionary<string, ObservableCollection<string>>();

        if (_toolsData?.StockScenes != null)
        {
            foreach (var kvp in _toolsData.StockScenes)
            {
                stockScenes[kvp.Key] = new ObservableCollection<string>(kvp.Value);
            }
        }

        return stockScenes;
    }

    public SortedDictionary<string, TopicModel> LoadTopics()
    {
        var topics = new SortedDictionary<string, TopicModel>();

        if (_toolsData?.Topics != null)
        {
            foreach (var kvp in _toolsData.Topics)
            {
                var topicData = kvp.Value;

                if (topicData.Type == "notepad")
                {
                    var path = topicData.Filename.IndexOf('\\') >= 0
                        ? topicData.Filename
                        : Path.Combine(Path.GetTempPath(), topicData.Filename);
                    topics[kvp.Key] = new TopicModel(kvp.Key, path);
                }
                else if (topicData.Type == "inline" && topicData.SubTopics != null)
                {
                    var topicModel = new TopicModel(kvp.Key);
                    foreach (var subTopic in topicData.SubTopics)
                    {
                        topicModel.SubTopics.Add(new SubTopicModel(subTopic.Name)
                        {
                            SubTopicNotes = subTopic.Notes ?? string.Empty
                        });
                    }

                    topics[kvp.Key] = topicModel;
                }
            }
        }

        return topics;
    }

    public List<PlotPatternModel> LoadMasterPlots()
    {
        var masterPlots = new List<PlotPatternModel>();

        if (_toolsData?.MasterPlots != null)
        {
            foreach (var plot in _toolsData.MasterPlots)
            {
                var plotModel = new PlotPatternModel(plot.Name)
                {
                    PlotPatternNotes = plot.Notes ?? string.Empty
                };

                if (plot.Scenes != null)
                {
                    foreach (var scene in plot.Scenes)
                    {
                        plotModel.PlotPatternScenes.Add(new PlotPatternScene(scene.Name)
                        {
                            Notes = scene.Notes ?? string.Empty
                        });
                    }
                }

                masterPlots.Add(plotModel);
            }
        }

        return masterPlots;
    }

    public List<PlotPatternModel> LoadBeatsheets()
    {
        var beatSheets = new List<PlotPatternModel>();

        if (_toolsData?.BeatSheets != null)
        {
            foreach (var beat in _toolsData.BeatSheets)
            {
                var beatModel = new PlotPatternModel(beat.Name)
                {
                    PlotPatternNotes = beat.Notes ?? string.Empty
                };

                if (beat.Scenes != null)
                {
                    foreach (var scene in beat.Scenes)
                    {
                        beatModel.PlotPatternScenes.Add(new PlotPatternScene(scene.Name)
                        {
                            Notes = scene.Notes ?? string.Empty
                        });
                    }
                }

                beatSheets.Add(beatModel);
            }
        }

        return beatSheets;
    }

    public SortedDictionary<string, DramaticSituationModel> LoadDramaticSituations()
    {
        var dramaticSituations = new SortedDictionary<string, DramaticSituationModel>();

        if (_toolsData?.DramaticSituations != null)
        {
            foreach (var kvp in _toolsData.DramaticSituations)
            {
                var situation = kvp.Value;
                var model = new DramaticSituationModel(situation.Name)
                {
                    Notes = situation.Notes ?? string.Empty
                };

                // Assign roles
                if (situation.Roles != null && situation.Roles.Count > 0)
                {
                    if (situation.Roles.Count > 0)
                    {
                        model.Role1 = situation.Roles[0];
                    }

                    if (situation.Roles.Count > 1)
                    {
                        model.Role2 = situation.Roles[1];
                    }

                    if (situation.Roles.Count > 2)
                    {
                        model.Role3 = situation.Roles[2];
                    }

                    if (situation.Roles.Count > 3)
                    {
                        model.Role4 = situation.Roles[3];
                    }
                }

                // Assign descriptions
                if (situation.Descriptions != null && situation.Descriptions.Count > 0)
                {
                    if (situation.Descriptions.Count > 0)
                    {
                        model.Description1 = situation.Descriptions[0];
                    }

                    if (situation.Descriptions.Count > 1)
                    {
                        model.Description2 = situation.Descriptions[1];
                    }

                    if (situation.Descriptions.Count > 2)
                    {
                        model.Description3 = situation.Descriptions[2];
                    }

                    if (situation.Descriptions.Count > 3)
                    {
                        model.Description4 = situation.Descriptions[3];
                    }
                }

                // Examples are stored in Notes for now (the model doesn't have an Examples property)

                dramaticSituations[kvp.Key] = model;
            }
        }

        return dramaticSituations;
    }

    public ObservableCollection<string> LoadMaleFirstNames()
    {
        if (_toolsData?.MaleFirstNames != null)
        {
            return new ObservableCollection<string>(_toolsData.MaleFirstNames);
        }

        return new ObservableCollection<string>();
    }

    public ObservableCollection<string> LoadFemaleFirstNames()
    {
        if (_toolsData?.FemaleFirstNames != null)
        {
            return new ObservableCollection<string>(_toolsData.FemaleFirstNames);
        }

        return new ObservableCollection<string>();
    }

    public ObservableCollection<string> LoadLastNames()
    {
        if (_toolsData?.LastNames != null)
        {
            return new ObservableCollection<string>(_toolsData.LastNames);
        }

        return new ObservableCollection<string>();
    }

    public ObservableCollection<string> LoadRelationships()
    {
        if (_toolsData?.Relationships != null)
        {
            return new ObservableCollection<string>(_toolsData.Relationships);
        }

        return new ObservableCollection<string>();
    }

    public void Clear()
    {
        _toolsData = null;
    }

    // JSON data classes
    private class ToolsJsonData
    {
        [JsonPropertyName("keyQuestions")] public Dictionary<string, List<KeyQuestionData>> KeyQuestions { get; set; }

        [JsonPropertyName("stockScenes")] public Dictionary<string, List<string>> StockScenes { get; set; }

        [JsonPropertyName("topics")] public Dictionary<string, TopicData> Topics { get; set; }

        [JsonPropertyName("masterPlots")] public List<PlotPatternData> MasterPlots { get; set; }

        [JsonPropertyName("beatSheets")] public List<PlotPatternData> BeatSheets { get; set; }

        [JsonPropertyName("dramaticSituations")]
        public Dictionary<string, DramaticSituationData> DramaticSituations { get; set; }

        [JsonPropertyName("MaleFirstNames")] public List<string> MaleFirstNames { get; set; }

        [JsonPropertyName("FemaleFirstNames")] public List<string> FemaleFirstNames { get; set; }

        [JsonPropertyName("LastNames")] public List<string> LastNames { get; set; }

        [JsonPropertyName("Relationships")] public List<string> Relationships { get; set; }
    }

    private class KeyQuestionData
    {
        [JsonPropertyName("key")] public string Key { get; set; }

        [JsonPropertyName("topic")] public string Topic { get; set; }

        [JsonPropertyName("question")] public string Question { get; set; }
    }

    private class TopicData
    {
        [JsonPropertyName("type")] public string Type { get; set; }

        [JsonPropertyName("filename")] public string Filename { get; set; }

        [JsonPropertyName("subTopics")] public List<SubTopicData> SubTopics { get; set; }
    }

    private class SubTopicData
    {
        [JsonPropertyName("name")] public string Name { get; set; }

        [JsonPropertyName("notes")] public string Notes { get; set; }
    }

    private class PlotPatternData
    {
        [JsonPropertyName("name")] public string Name { get; set; }

        [JsonPropertyName("notes")] public string Notes { get; set; }

        [JsonPropertyName("scenes")] public List<PlotPointData> Scenes { get; set; }
    }

    private class PlotPointData
    {
        [JsonPropertyName("name")] public string Name { get; set; }

        [JsonPropertyName("notes")] public string Notes { get; set; }
    }

    private class DramaticSituationData
    {
        [JsonPropertyName("name")] public string Name { get; set; }

        [JsonPropertyName("roles")] public List<string> Roles { get; set; }

        [JsonPropertyName("descriptions")] public List<string> Descriptions { get; set; }

        [JsonPropertyName("examples")] public List<string> Examples { get; set; }

        [JsonPropertyName("notes")] public string Notes { get; set; }
    }
}
