using System.Collections.ObjectModel;
using StoryCADLib.DAL;
using StoryCADLib.Models.Resources;

namespace StoryCADLib.Models.Tools;

/// <summary>
///     This stores the tools for StoryCAD's Tools.json.
///     Previously tools were stored in GlobalData.
/// </summary>
public class ToolsData
{
    private readonly ILogService _log;
    public List<PlotPatternModel> BeatSheetSource;
    public SortedDictionary<string, DramaticSituationModel> DramaticSituationsSource;
    public ObservableCollection<string> FemaleFirstNamesSource;

    public Dictionary<string, List<KeyQuestionModel>> KeyQuestionsSource;
    public ObservableCollection<string> LastNamesSource;
    public ObservableCollection<string> MaleFirstNamesSource;
    public List<PlotPatternModel> MasterPlotsSource;
    public ObservableCollection<string> RelationshipsSource;
    public SortedDictionary<string, ObservableCollection<string>> StockScenesSource;
    public SortedDictionary<string, TopicModel> TopicsSource;

    public ToolsData(ILogService log, JSONResourceLoader resourceLoader)
    {
        _log = log;
        try
        {
            _log.Log(LogLevel.Info, "Loading Tools.json data");
            Task.Run(async () =>
            {
                var toolsData = await resourceLoader.LoadResource<ToolsJson>("Tools.json");
                KeyQuestionsSource = LoadKeyQuestions(toolsData);
                StockScenesSource = LoadStockScenes(toolsData);
                TopicsSource = LoadTopics(toolsData);
                MasterPlotsSource = LoadMasterPlots(toolsData);
                BeatSheetSource = LoadBeatsheets(toolsData);
                DramaticSituationsSource = LoadDramaticSituations(toolsData);
                MaleFirstNamesSource = LoadNames(toolsData.MaleFirstNames);
                FemaleFirstNamesSource = LoadNames(toolsData.FemaleFirstNames);
                LastNamesSource = LoadNames(toolsData.LastNames);
                RelationshipsSource = LoadNames(toolsData.Relationships);
            }).Wait();
            _log.Log(LogLevel.Info, $"""
                                     {KeyQuestionsSource.Keys.Count} Key Questions created
                                     {StockScenesSource.Keys.Count} Stock Scenes created
                                     {TopicsSource.Count} Topics created
                                     {MasterPlotsSource.Count} Master Plots created
                                     {BeatSheetSource.Count} Beat Sheets created
                                     {DramaticSituationsSource.Count} Dramatic Situations created
                                     {MaleFirstNamesSource.Count} Male First Names loaded
                                     {FemaleFirstNamesSource.Count} Female First Names loaded
                                     {LastNamesSource.Count} Last Names loaded
                                     {RelationshipsSource.Count} Relationships loaded
                                     """);
        }
        catch (Exception ex)
        {
            _log.LogException(LogLevel.Error, ex, "Error loading Tools.json");
            Application.Current.Exit();
        }
    }

    private static Dictionary<string, List<KeyQuestionModel>> LoadKeyQuestions(ToolsJson toolsData)
    {
        var questions = new Dictionary<string, List<KeyQuestionModel>>();

        if (toolsData?.KeyQuestions != null)
        {
            foreach (var kvp in toolsData.KeyQuestions)
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

    private static SortedDictionary<string, ObservableCollection<string>> LoadStockScenes(ToolsJson toolsData)
    {
        var stockScenes = new SortedDictionary<string, ObservableCollection<string>>();

        if (toolsData?.StockScenes != null)
        {
            foreach (var kvp in toolsData.StockScenes)
            {
                stockScenes[kvp.Key] = new ObservableCollection<string>(kvp.Value);
            }
        }

        return stockScenes;
    }

    private static SortedDictionary<string, TopicModel> LoadTopics(ToolsJson toolsData)
    {
        var topics = new SortedDictionary<string, TopicModel>();

        if (toolsData?.Topics != null)
        {
            foreach (var kvp in toolsData.Topics)
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

    private static List<PlotPatternModel> LoadMasterPlots(ToolsJson toolsData)
    {
        var masterPlots = new List<PlotPatternModel>();

        if (toolsData?.MasterPlots != null)
        {
            foreach (var plot in toolsData.MasterPlots)
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

    private static List<PlotPatternModel> LoadBeatsheets(ToolsJson toolsData)
    {
        var beatSheets = new List<PlotPatternModel>();

        if (toolsData?.BeatSheets != null)
        {
            foreach (var beat in toolsData.BeatSheets)
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

    private static SortedDictionary<string, DramaticSituationModel> LoadDramaticSituations(ToolsJson toolsData)
    {
        var dramaticSituations = new SortedDictionary<string, DramaticSituationModel>();

        if (toolsData?.DramaticSituations != null)
        {
            foreach (var kvp in toolsData.DramaticSituations)
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
                        model.Role1 = situation.Roles[0];
                    if (situation.Roles.Count > 1)
                        model.Role2 = situation.Roles[1];
                    if (situation.Roles.Count > 2)
                        model.Role3 = situation.Roles[2];
                    if (situation.Roles.Count > 3)
                        model.Role4 = situation.Roles[3];
                }

                // Assign descriptions
                if (situation.Descriptions != null && situation.Descriptions.Count > 0)
                {
                    if (situation.Descriptions.Count > 0)
                        model.Description1 = situation.Descriptions[0];
                    if (situation.Descriptions.Count > 1)
                        model.Description2 = situation.Descriptions[1];
                    if (situation.Descriptions.Count > 2)
                        model.Description3 = situation.Descriptions[2];
                    if (situation.Descriptions.Count > 3)
                        model.Description4 = situation.Descriptions[3];
                }

                dramaticSituations[kvp.Key] = model;
            }
        }

        return dramaticSituations;
    }

    private static ObservableCollection<string> LoadNames(List<string> names)
    {
        return names != null
            ? new ObservableCollection<string>(names)
            : new ObservableCollection<string>();
    }
}
