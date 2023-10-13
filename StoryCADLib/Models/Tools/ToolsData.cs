using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using StoryCAD.DAL;
using StoryCAD.Services.Logging;

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
    public List<MasterPlotModel> MasterPlotsSource;
    public SortedDictionary<string, DramaticSituationModel> DramaticSituationsSource;

    public ToolsData() {
        try
        {
            _log.Log(LogLevel.Info, "Loading Tools.ini data");
            ToolLoader loader = Ioc.Default.GetService<ToolLoader>();
            ToolsData toolsdata = Ioc.Default.GetService<ToolsData>();
            Task.Run(async () =>
            {
                await loader.Init(Ioc.Default.GetRequiredService<AppState>().RootDirectory);
            }).Wait();
            _log.Log(LogLevel.Info, $"{toolsdata.KeyQuestionsSource.Keys.Count} Key Questions created");
            _log.Log(LogLevel.Info, $"{toolsdata.StockScenesSource.Keys.Count} Stock Scenes created");
            _log.Log(LogLevel.Info, $"{toolsdata.TopicsSource.Count} Topics created");
            _log.Log(LogLevel.Info, $"{toolsdata.MasterPlotsSource.Count} Master Plots created");
            _log.Log(LogLevel.Info, $"{toolsdata.DramaticSituationsSource.Count} Dramatic Situations created");

        }
        catch (Exception ex)
        {
            _log.LogException(LogLevel.Error, ex, "Error loading Tools.ini");
            Application.Current.Exit();
        }
    }
}
