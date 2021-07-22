using System.Collections.Generic;
using System.Collections.ObjectModel;
using StoryBuilder.Models.Tools;
using Microsoft.UI.Xaml;

namespace StoryBuilder.Models
{
    /// <summary>
    /// GlobalData provides access to the application data provided by the
    /// DAL loader classes ListLoader, ControlLoader, and ToolLoader, 
    /// 
    /// It also provides acces the Preferences instance and other global items.
    /// </summary>
    public static class GlobalData
    {
        /// The ComboBox and ListBox source bindings in viewmodels point to lists in this Dictionary. 
        /// Each list has a unique key related to the ComboBox or ListBox use.
        public static Dictionary<string, ObservableCollection<string>> ListControlSource;

        /// Tools that copy data into StoryElements must access and update the viewmodel currently 
        /// active viewmodel at the time the tool is invoked. The viewmodel type is identified
        /// by the navigation service page key.
        public static string PageKey;
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

        // Preferences data
        public static PreferencesModel Preferences;

        // A defect in preview WinUI 3 Win32 code is that ContentDialog controls don't have an
        // established XamlRoot. A workaround is to assign the dialog's XamlRoot to 
        // the root of a containing page. 
        // The Shell page's XamlRoot is stored here and accessed wherever needed. 
        public static XamlRoot XamlRoot;
    }
}
