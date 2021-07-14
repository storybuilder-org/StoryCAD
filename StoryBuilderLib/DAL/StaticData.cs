using System.Collections.Generic;
using System.Collections.ObjectModel;
using StoryBuilder.Models.Tools;

namespace StoryBuilder.DAL
{
    /// <summary>
    /// StaticData provides access to the application data provided by the
    /// DAL loader classes ListLoader, ControlLoader, and ToolLoader.
    /// 
    /// It also provides acces the Preferences instance and a few other items.
    /// </summary>
    public static class StaticData
    {
        /// The ComboBox and ListBox source bindings in viewmodels point to lists in this Dictionary. 
        /// Each list has a unique key related to the ComboBox or ListBox use.
        public static Dictionary<string, ObservableCollection<string>> ListControlSource;
        /// <summary>
        /// Some controls and all tools have their own specific data model. The following 
        /// data types hold data for user controls and tool forms.
        /// </summary>
        /// User Controls
        public static SortedDictionary<string, ConflictCategoryModel> ConflictTypes;
        // Tools
        public static Dictionary<string, List<KeyQuestionModel>> KeyQuestionsSource;
        public static SortedDictionary<string, ObservableCollection<string>> StockScenesSource;
        public static SortedDictionary<string, TopicModel> TopicsSource;
        public static List<MasterPlotModel> MasterPlotsSource;
        public static SortedDictionary<string, DramaticSituationModel> DramaticSituationsSource;
        //TODO: Use QuotesSource
        public static ObservableCollection<Quotation> QuotesSource;
    }
}
