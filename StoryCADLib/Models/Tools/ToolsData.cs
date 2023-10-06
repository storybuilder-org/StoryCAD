using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StoryCAD.Models.Tools;

/// <summary>
/// This stores the tools for StoryCAD's Lists.ini.
/// Previously tools were stored in GlobalData.
/// </summary>
public class ToolsData
{
    public Dictionary<string, List<KeyQuestionModel>> KeyQuestionsSource;
    public SortedDictionary<string, ObservableCollection<string>> StockScenesSource;
    public SortedDictionary<string, TopicModel> TopicsSource;
    public List<MasterPlotModel> MasterPlotsSource;
    public SortedDictionary<string, DramaticSituationModel> DramaticSituationsSource;
}
