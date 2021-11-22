using CommunityToolkit.Mvvm.ComponentModel;
using StoryBuilder.Models;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace StoryBuilder.ViewModels
{
    public class PrintViewModel : ObservableRecipient
    {
        // See: http://stackoverflow.com/questions/4935195/forcing-a-data-bound-windows-forms-checkbox-to-change-property-value-immediately
        #region Fields
        private bool _changed;

        //TODO: For checkbox bindings see: http://caliburnmicro.codeplex.com/discussions/223752
        private bool _allReportsSwitch;
        private bool _destinationpreviewswitch;
        private bool _destinationprinterswitch;
        private bool _destinationDiskSwitch;
        private bool _storyNotesSwitch;
        private bool _listOfProblemsSwitch;
        private bool _allproblemsswitch;
        private ObservableCollection<ProblemModel> _selectedProblemsList;
        private bool _listOfCharactersSwitch;
        private bool _allCharactersSwitch;
        private ObservableCollection<CharacterModel> _selectedCharactersList;
        private bool _characterRelationshipsSwitch;
        private bool _listOfSettingsSwitch;
        private bool _allSettingsSwitch;
        private ObservableCollection<SettingModel> _selectedSettingsList;
        private bool _listOfPlotPointsSwitch;
        private bool _allPlotPointsSwitch;
        private ObservableCollection<SceneModel> _selectedSceneList;
        private bool _questionResponsesSwitch;

        #endregion

        #region Properties

        public bool Changed
        {
            get { return _changed; }
            set { _changed = value; }
        }

        public bool AllReportsSwitch
        {
            get => _allReportsSwitch;
            set => SetProperty(ref _allReportsSwitch, value);
        }

        public bool DestinationPreviewSwitch
        {
            get => _destinationpreviewswitch;
            set => SetProperty(ref _destinationpreviewswitch, value);
        }

        public bool DestinationPrinterSwitch
        {
            get => _destinationprinterswitch;
            set => SetProperty(ref _destinationprinterswitch, value);
        }

        public bool DestinationDiskSwitch
        {
            get => _destinationDiskSwitch;
            set => SetProperty(ref _destinationDiskSwitch, value);
        }

        public bool StoryNotesSwitch
        {
            get => _storyNotesSwitch;
            set => SetProperty(ref _storyNotesSwitch, value);
        }

        public bool ListOfProblemsSwitch
        {
            get => _listOfProblemsSwitch;
            set => SetProperty(ref _listOfProblemsSwitch, value);
        }

        public bool AllProblemsSwitch
        {
            get => _allproblemsswitch;
            set => SetProperty(ref _allproblemsswitch, value);
        }

        public ObservableCollection<ProblemModel> SelectedProblemsList
        {
            get => _selectedProblemsList;
            set => SetProperty(ref _selectedProblemsList, value);
        }

        public bool ListOfCharactersSwitch
        {
            get => _listOfCharactersSwitch;
            set => SetProperty(ref _listOfCharactersSwitch, value);
        }

        public bool AllCharactersSwitch
        {
            get => _allCharactersSwitch;
            set => SetProperty(ref _allCharactersSwitch, value);
        }

        public ObservableCollection<CharacterModel> SelectedCharactersList
        {
            get => _selectedCharactersList;
            set => SetProperty(ref _selectedCharactersList, value);
        }

        public bool CharacterRelationshipsSwitch
        {
            get => _characterRelationshipsSwitch;
            set => SetProperty(ref _characterRelationshipsSwitch, value);
        }

        public bool ListOfSettingsSwitch
        {
            get => _listOfSettingsSwitch;
            set => SetProperty(ref _listOfSettingsSwitch, value);
        }

        public bool AllSettingsSwitch
        {
            get => _allSettingsSwitch;
            set => SetProperty(ref _allSettingsSwitch, value);
        }

        public ObservableCollection<SettingModel> SelectedSettingsList
        {
            get => _selectedSettingsList;
            set => SetProperty(ref _selectedSettingsList, value);
        }

        public bool ListOfPlotPointsSwitch
        {
            get => _listOfPlotPointsSwitch;
            set => SetProperty(ref _listOfPlotPointsSwitch, value);
        }

        public bool AllPlotPointsSwitch
        {
            get { return _allPlotPointsSwitch; }
            set => SetProperty(ref _allPlotPointsSwitch, value);
        }

        public ObservableCollection<SceneModel> SelectedPlotPointsList
        {
            get => _selectedSceneList;
            set => SetProperty(ref _selectedSceneList, value);
        }

        public bool QuestionResponsesSwitch
        {
            get => _questionResponsesSwitch;
            set => SetProperty(ref _questionResponsesSwitch, value);
        }

        #endregion

        #region Combobox and ListBox sources

        // NOTE: This isn't right, but is a stopgap until I can bind to SelectedItemsCollection
        public ObservableCollection<ProblemModel> SelectedProblemsListSource;
        public ObservableCollection<CharacterModel> SelectedCharactersListSource;
        public ObservableCollection<SettingModel> SelectedSettingsListSource;
        public ObservableCollection<SceneModel> SelectedSceneListSource;


        #endregion

        #region Constructor

        public PrintViewModel(ObservableCollection<ProblemModel> problems,
                              ObservableCollection<CharacterModel> characters,
                              ObservableCollection<SettingModel> settings,
                              ObservableCollection<SceneModel> scenes)
        {
            SelectedProblemsListSource = problems;
            SelectedCharactersListSource = characters;
            SelectedSettingsListSource = settings;
            SelectedSceneListSource = scenes;

            _selectedProblemsList = new ObservableCollection<ProblemModel>();
            _selectedProblemsList.CollectionChanged += OnCollectionChanged;
            _selectedCharactersList = new ObservableCollection<CharacterModel>();
            _selectedCharactersList.CollectionChanged += OnCollectionChanged;
            _selectedSettingsList = new ObservableCollection<SettingModel>();
            _selectedSettingsList.CollectionChanged += OnCollectionChanged;
            _selectedSceneList = new ObservableCollection<SceneModel>();
            _selectedSceneList.CollectionChanged += OnCollectionChanged;
        }

        #endregion

        #region ObservableCollection event handler
        void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            _changed = true;
        }
        #endregion
    }
}
