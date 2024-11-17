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
    LogService _log = Ioc.Default.GetService<LogService>();

    public Dictionary<string, List<KeyQuestionModel>> KeyQuestionsSource;
    public SortedDictionary<string, ObservableCollection<string>> StockScenesSource;
    public SortedDictionary<string, TopicModel> TopicsSource;
    public List<PlotPatternModel> MasterPlotsSource;
    public List<PlotPatternModel> BeatSheetSource;
    public SortedDictionary<string, DramaticSituationModel> DramaticSituationsSource;

    public ToolsData() {
        try
        {
            _log.Log(LogLevel.Info, "Loading Tools.ini data");
            ToolLoader loader = Ioc.Default.GetService<ToolLoader>();
            Task.Run(async () =>
            {
               List<object> Tools = await loader.Init();
                KeyQuestionsSource = (Dictionary<string, List<KeyQuestionModel>>)Tools[0];
                StockScenesSource = (SortedDictionary<string, ObservableCollection<string>>)Tools[1];
                TopicsSource = (SortedDictionary<string, TopicModel>)Tools[2];
                MasterPlotsSource = (List<PlotPatternModel>)Tools[3];
                BeatSheetSource = (List<PlotPatternModel>)Tools[4];
                DramaticSituationsSource = (SortedDictionary<string, DramaticSituationModel>)Tools[5];
            }).Wait();
            _log.Log(LogLevel.Info, $"""
                                    {KeyQuestionsSource.Keys.Count} Key Questions created
                                    {StockScenesSource.Keys.Count} Stock Scenes created
                                    {TopicsSource.Count} Topics created
                                    {MasterPlotsSource.Count} Master Plots created
                                    {BeatSheetSource.Count} Master Plots created
                                    {DramaticSituationsSource.Count} Dramatic Situations created
                                    """);

        }
        catch (Exception ex)
        {
            _log.LogException(LogLevel.Error, ex, "Error loading Tools.ini");
            Application.Current.Exit();
        }
    }
}
