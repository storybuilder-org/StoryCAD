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
    private readonly Lazy<Task> _initializationTask;

    public Dictionary<string, List<KeyQuestionModel>> KeyQuestionsSource { get; private set; }
    public SortedDictionary<string, ObservableCollection<string>> StockScenesSource { get; private set; }
    public SortedDictionary<string, TopicModel> TopicsSource { get; private set; }
    public List<PlotPatternModel> MasterPlotsSource { get; private set; }
    public List<PlotPatternModel> BeatSheetSource { get; private set; }
    public SortedDictionary<string, DramaticSituationModel> DramaticSituationsSource { get; private set; }

    public ToolsData() 
    {
        // Initialize collections with empty collections to prevent null reference exceptions
        KeyQuestionsSource = new Dictionary<string, List<KeyQuestionModel>>();
        StockScenesSource = new SortedDictionary<string, ObservableCollection<string>>();
        TopicsSource = new SortedDictionary<string, TopicModel>();
        MasterPlotsSource = new List<PlotPatternModel>();
        BeatSheetSource = new List<PlotPatternModel>();
        DramaticSituationsSource = new SortedDictionary<string, DramaticSituationModel>();

        // Use lazy initialization to avoid blocking constructor
        _initializationTask = new Lazy<Task>(InitializeAsync);
    }

    /// <summary>
    /// Ensures data is loaded before accessing properties. Call this method before using any tools data.
    /// </summary>
    public async Task EnsureInitializedAsync()
    {
        await _initializationTask.Value.ConfigureAwait(false);
    }

    private async Task InitializeAsync()
    {
        try
        {
            _log.Log(LogLevel.Info, "Loading Tools.ini data");
            ToolLoader loader = Ioc.Default.GetService<ToolLoader>();
            
            List<object> Tools = await loader.Init().ConfigureAwait(false);
            KeyQuestionsSource = (Dictionary<string, List<KeyQuestionModel>>)Tools[0];
            StockScenesSource = (SortedDictionary<string, ObservableCollection<string>>)Tools[1];
            TopicsSource = (SortedDictionary<string, TopicModel>)Tools[2];
            MasterPlotsSource = (List<PlotPatternModel>)Tools[3];
            BeatSheetSource = (List<PlotPatternModel>)Tools[4];
            DramaticSituationsSource = (SortedDictionary<string, DramaticSituationModel>)Tools[5];
            
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
