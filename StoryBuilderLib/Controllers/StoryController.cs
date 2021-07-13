using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.Storage;
using Microsoft.UI.Xaml;
using StoryBuilder.Models;
using StoryBuilder.Models.Tools;
using StoryBuilder.ViewModels.Tools;

namespace StoryBuilder.Controllers
{
    /// <summary>
    /// Copyright 2019 Seven Valleys Software
    /// All Rights Reserved
    ///
    /// The StoryController class mediates between StoryModel and the various
    /// ViewModels.
    /// </summary>
    //TODO: Rename and relocate as StoryService, a microservice (possibly) 
    /* Much of this method (Add, Update, Show methods, etc) has been moved
       to the ViewModels. Others of this have been moved to the ShellViewModel.
       The mediation between the StoryModel and viewmodels mentioned above should be
       eliminated. What's left is some miscellaneous stuff. If there's a need,
       keep what's left as a service. Otherwise, eliminate.
     */
    public class StoryController
    {
        #region public Fields and Properties

        // The currently open and active StoryModel instance. 
        public StoryModel StoryModel;

        public PreferencesModel Preferences;
        
        // A defect in preview WinUI 3 Win32 code is that ContentDialog controls don't have an
        // established XamlRoot. A workaround is to assign the dialog's XamlRoot to 
        // the root of a containing page. 
        // The Shell page's XamlRoot is stored here and accessed wherever needed. 
        public XamlRoot XamlRoot;

        /// <summary>
        /// The ComboBox and ListBox source bindings in viewmodels point to lists in this Dictionary. 
        /// Each list has a unique key related to the ComboBox or ListBox use.
        /// </summary>
        public Dictionary<string, ObservableCollection<string>> ListControlSource;

        /// <summary>
        /// Some controls and all tools have tjeor own specific data model. The following 
        /// data types hold data for user controls and tool forms.
        /// </summary>
        public SortedDictionary<string, ConflictCategoryModel> ConflictTypes;
        public Dictionary<string, List<KeyQuestionModel>> KeyQuestionsSource;
        public SortedDictionary<string, ObservableCollection<string>> StockScenesSource;
        public SortedDictionary<string, TopicModel> TopicsSource;
        public List<MasterPlotModel> MasterPlotsSource;
        public SortedDictionary<string, DramaticSituationModel> DramaticSituationsSource;
        public ObservableCollection<Quotation> QuotesSource;

        // File references. These are used to coordinate LoadModel()
        // and SaveModel() behavior in Story Element ViewModels and 
        // Open, Save, and SaveAs behavior in the ShellViewModel.
        // File and folder data
        public string PreferencesFile;
        public StorageFolder ProjectFolder;
        public StorageFolder FilesFolder;
        public StorageFile ProjectFile;
        public string ProjectFilename;
        public string ProjectPath;

        // LoadStatus decides where LoadModel() and SaveModel() get
        // and put RTF text data.
        public LoadStatus LoadStatus;

        #endregion

        //TODO: Move updates to the respective viewmodels   
        #region Update other entities   

        public void UpdateCharacterRelationsip(CharacterRelationshipsViewModel viewModel, CharacterRelationshipsModel model)
        {
            // TODO: Both UpdateCharacterRelationship and ShowCharacterRelationship are lists and need keyed.
            // TODO: Use Dictionary<FirstChar|SecondChar, CharacterRelationshipModel>?
            if (!viewModel.Changed)
                return;
            model.Id = viewModel.Id;
            model.FirstChar = viewModel.FirstChar;
            model.FirstTrait = viewModel.FirstTrait;
            model.SecondChar = viewModel.SecondChar;
            model.SecondTrait = viewModel.SecondTrait;
            model.Relationship = viewModel.Relationship;
            model.Remarks = viewModel.Remarks;
            // TODO: Set StoryModel.Changed  to true;

        }

        #endregion

        #region Miscellaneous

        #endregion

        #region Constructor

        public StoryController()
        {
            Preferences = new PreferencesModel();
            ListControlSource = new Dictionary<string, ObservableCollection<string>>();
        }

        #endregion

    }
}
