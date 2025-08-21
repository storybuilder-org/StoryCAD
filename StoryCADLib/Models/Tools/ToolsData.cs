using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using StoryCAD.DAL;

namespace StoryCAD.Models.Tools;

/// <summary>
/// This stores the tools for StoryCAD's Lists.ini.
/// Previously tools were stored in GlobalData.
/// </summary>
public class ToolsData
{
    private readonly ILogService _log;

    public Dictionary<string, List<KeyQuestionModel>> KeyQuestionsSource;
    public SortedDictionary<string, ObservableCollection<string>> StockScenesSource;
    public SortedDictionary<string, TopicModel> TopicsSource;
    public List<PlotPatternModel> MasterPlotsSource;
    public List<PlotPatternModel> BeatSheetSource;
    public SortedDictionary<string, DramaticSituationModel> DramaticSituationsSource;
    public ObservableCollection<string> MaleFirstNamesSource;
    public ObservableCollection<string> FemaleFirstNamesSource;
    public ObservableCollection<string> LastNamesSource;
    public ObservableCollection<string> RelationshipsSource;

    public ToolsData(ILogService log) {
        _log = log;
        try
        {
            _log.Log(LogLevel.Info, "Loading Tools.ini data");
            ToolLoader loader = new ToolLoader(_log);
            Task.Run(async () =>
            {
               List<object> Tools = await loader.Init();
                KeyQuestionsSource = (Dictionary<string, List<KeyQuestionModel>>)Tools[0];
                StockScenesSource = (SortedDictionary<string, ObservableCollection<string>>)Tools[1];
                TopicsSource = (SortedDictionary<string, TopicModel>)Tools[2];
                MasterPlotsSource = (List<PlotPatternModel>)Tools[3];
                BeatSheetSource = (List<PlotPatternModel>)Tools[4];
                DramaticSituationsSource = (SortedDictionary<string, DramaticSituationModel>)Tools[5];
                MaleFirstNamesSource = (ObservableCollection<string>)Tools[6];
                FemaleFirstNamesSource = (ObservableCollection<string>)Tools[7];
                LastNamesSource = (ObservableCollection<string>)Tools[8];
                RelationshipsSource = (ObservableCollection<string>)Tools[9];
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
            _log.LogException(LogLevel.Error, ex, "Error loading Tools.ini");
            Application.Current.Exit();
        }
    }
}
