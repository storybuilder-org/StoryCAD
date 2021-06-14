using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using Microsoft.Toolkit.Mvvm.Messaging;
using StoryBuilder.Controllers;
using StoryBuilder.DAL;
using StoryBuilder.Models;
using StoryBuilder.Services.Logging;
using StoryBuilder.Services.Messages;
using StoryBuilder.Services.Navigation;
using Microsoft.UI.Xaml.Data;

namespace StoryBuilder.ViewModels
{
    public class PlotPointViewModel : ObservableRecipient, INavigable
    {
        #region Fields

        private readonly StoryReader _rdr;
        private readonly StoryWriter _wtr;
        private readonly StoryController _story;
        private readonly LogService _logger;
        private bool _changeable;

        #endregion

        #region Properties

        // StoryElement data

        private Guid _uuid;
        public Guid Uuid
        {
            get => _uuid;
            set => SetProperty(ref _uuid, value);
        }

        private string _name;
        public string Name
        {
            get => _name;
            set
            {
                if (_changeable && (_name != value)) // Name changed?
                {
                    _logger.Log(LogLevel.Info, string.Format("Requesting Name change from {0} to {1}", _name, value));
                    var msg = new NameChangeMessage(_name, value);
                    Messenger.Send(new NameChangedMessage(msg));
                }
                SetProperty(ref _name, value);
            }
        }

        // Besides its GUID, each Plot Point has a unique (to this story) 
        // integer id number (useful in lists of scenes.)

        private int _id;
        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        //  Scene general data

        private string _description;
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        private string _viewpoint;
        public string Viewpoint
        {
            get => _viewpoint;
            set => SetProperty(ref _viewpoint, value);
        }

        private string _date;
        public string Date
        {
            get => _date;
            set => SetProperty(ref _date, value);
        }

        private string _time;
        public string Time
        {
            get => _time;
            set => SetProperty(ref _time, value);
        }

        private string _setting;
        public string Setting
        {
            get => _setting;
            set => SetProperty(ref _setting, value);
        }

        private string _sceneType;
        public string SceneType 
        {
            get => _sceneType;
            set => SetProperty(ref _sceneType, value);
        }

        private string _char1;
        public string Char1
        {
            get => _char1;
            set => SetProperty(ref _char1, value);
        }

        private string _char2;
        public string Char2
        {
            get => _char2;
            set => SetProperty(ref _char2, value);
        }

        private string _char3;
        public string Char3
        {
            get => _char3;
            set => SetProperty(ref _char3, value);
        }

        private string _role1;
        public string Role1
        {
            get => _role1;
            set => SetProperty(ref _role1, value);
        }

        private string _role2;
        public string Role2
        {
            get => _role2;
            set => SetProperty(ref _role2, value);
        }

        private string _role3;

        public string Role3
        {
            get => _role3;
            set => SetProperty(ref _role3, value);
        }

        private string _remarks;
        public string Remarks
        {
            get => _remarks;
            set => SetProperty(ref _remarks, value);
        }

        // Scene development data (from Lisa Cron's Story Genius)

        private string _scenePurpose;
        public string ScenePurpose
        {
            get => _scenePurpose;
            set => SetProperty(ref _scenePurpose, value);
        }

        private string _valueExchange;
        public string ValueExchange
        {
            get => _valueExchange;
            set => SetProperty(ref _valueExchange, value);
        }


        private string _events;
        public string Events
        {
            get => _events;
            set => SetProperty(ref _events, value);
        }

        private string _consequences;
        public string Consequences
        {
            get => _consequences;
            set => SetProperty(ref _consequences, value);
        }

        private string _significance;
        public string Significance  
        {
            get => _significance;
            set => SetProperty(ref _significance, value);
        }

        private string _realization;
        public string Realization   
        {
            get => _realization;
            set => SetProperty(ref _realization, value);
        }

        //  Scene Conflict data

        private string _protagonist;
        public string Protagonist
        {
            get => _protagonist;
            set => SetProperty(ref _protagonist, value);
        }

        private string _protagEmotion;
        public string ProtagEmotion
        {
            get => _protagEmotion;
            set => SetProperty(ref _protagEmotion, value);
        }

        private string _protagGoal;
        public string ProtagGoal
        {
            get => _protagGoal;
            set => SetProperty(ref _protagGoal, value);
        }

        private string _antagonist;
        public string Antagonist
        {
            get => _antagonist;
            set => SetProperty(ref _antagonist, value);
        }

        private string _antagEmotion;
        public string AntagEmotion
        {
            get => _antagEmotion;
            set => SetProperty(ref _antagEmotion, value);
        }

        private string _antagGoal;
        public string AntagGoal
        {
            get => _antagGoal;
            set => SetProperty(ref _antagGoal, value);
        }

        private string _opposition;
        public string Opposition
        {
            get => _opposition;
            set => SetProperty(ref _opposition, value);
        }

        private string _outcome;
        public string Outcome
        {
            get => _outcome;
            set => SetProperty(ref _outcome, value);
        }

        //  Scene Sequel data

        private string _emotion;
        public string Emotion
        {
            get => _emotion;
            set => SetProperty(ref _emotion, value);
        }

        private string _newGoal;
        public string NewGoal
        {
            get => _newGoal;
            set => SetProperty(ref _newGoal, value);
        }

        private string _review;
        public string Review
        {
            get => _review;
            set => SetProperty(ref _review, value);
        }

        //  Scene notes data

        private string _notes;
        public string Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }

        // Besides its GUID, each Plot Point has a unique (to this story) 
        // integer id number (useful in lists of scenes.)

        // The StoryModel is passed when CharacterPage is navigated to
        private PlotPointModel _model;
        public PlotPointModel Model
        {
            get => _model;
            set => SetProperty(ref _model, value);
        }

        // The Changed bit tracks any change to this ViewModel.
        private bool _changed;
        public bool Changed
        {
            get => _changed;
            set => SetProperty(ref _changed, value);
        }

        #endregion

        #region Methods

        public async Task Activate(object parameter)
        {
            Model = (PlotPointModel) parameter;
            await LoadModel();   // Load the ViewModel from the Story
        }

        public async Task Deactivate(object parameter)
        {
            await SaveModel();    // Save the ViewModel back to the Story
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            if (_changeable)
                Changed = true;
        }

        private async Task LoadModel()
        {
            Uuid = Model.Uuid;
            Name = Model.Name;
            Id = Model.Id;
            Description = Model.Description;
            Viewpoint = Model.Viewpoint;
            Date = Model.Date;
            Time = Model.Time;
            Setting = Model.Setting;
            SceneType = Model.SceneType;
            Char1 = Model.Char1;
            Char2 = Model.Char2;
            Char3 = Model.Char3;
            Role1 = Model.Role1;
            Role2 = Model.Role2;
            Role3 = Model.Role3;
            ScenePurpose = Model.ScenePurpose;
            ValueExchange = Model.ValueExchange;
            Protagonist = Model.Protagonist;
            ProtagEmotion = Model.ProtagEmotion;
            ProtagGoal = Model.ProtagGoal;
            Antagonist = Model.Antagonist;
            AntagEmotion = Model.AntagEmotion;
            AntagGoal = Model.AntagGoal;
            Opposition = Model.Opposition;
            Outcome = Model.Outcome;
            Emotion = Model.Emotion;
            NewGoal = Model.NewGoal;
            
            // Read RTF files
            Remarks = await _rdr.GetRtfText(Model.Remarks, Uuid);

            Events = await _rdr.GetRtfText(Model.Events, Uuid);
            Consequences = await _rdr.GetRtfText(Model.Consequences, Uuid);
            Significance = await _rdr.GetRtfText(Model.Significance, Uuid);
            Realization = await _rdr.GetRtfText(Model.Realization, Uuid);

            Review = await _rdr.GetRtfText(Model.Review, Uuid);
            
            Notes = await _rdr.GetRtfText(Model.Notes, Uuid);


            Changed = false;
            PropertyChanged += OnPropertyChanged;
            _changeable = true;
        }

        internal async Task SaveModel()
        {
            PropertyChanged -= OnPropertyChanged;
            _changeable = false;

            if (Changed)
            {
                // Story.Uuid is read-only and cannot be assigned
                Model.Name = Name;
                Model.Description = Description;
                Model.Viewpoint = Viewpoint;
                Model.Date = Date;
                Model.Time = Time;
                Model.Setting = Setting;
                Model.SceneType = SceneType;
                Model.Char1 = Char1;
                Model.Char2 = Char2;
                Model.Char3 = Char3;
                Model.Role1 = Role1;
                Model.Role2 = Role2;
                Model.Role3 = Role3;
                Model.ScenePurpose = ScenePurpose;
                Model.ValueExchange = ValueExchange;
                Model.Protagonist = Protagonist;
                Model.ProtagEmotion = ProtagEmotion;
                Model.ProtagGoal = ProtagGoal;
                Model.Antagonist = Antagonist;
                Model.AntagEmotion = AntagEmotion;
                Model.AntagGoal = AntagGoal;
                Model.Opposition = Opposition;
                Model.Outcome = Outcome;
                Model.Emotion = Emotion;
                Model.NewGoal = NewGoal;

                // Write RTF files
                Model.Remarks = await _wtr.PutRtfText(Remarks, Model.Uuid, "remarks.rtf");
                Model.Events = await _wtr.PutRtfText(Events, Model.Uuid, "events.rtf");
                Model.Consequences = await _wtr.PutRtfText(Consequences, Model.Uuid, "consequences.rtf");
                Model.Significance = await _wtr.PutRtfText(Significance, Model.Uuid, "significance.rtf");
                Model.Realization = await _wtr.PutRtfText(Realization, Model.Uuid, "realization.rtf");
                Model.Review = await _wtr.PutRtfText(Review, Model.Uuid, "review.rtf");
                Model.Notes = await _wtr.PutRtfText(Notes, Model.Uuid, "notes.rtf");

                _logger.Log(LogLevel.Info, string.Format("Requesting IsDirty change to true"));
                Messenger.Send(new IsDirtyChangedMessage(Changed));
            }
        }

        #endregion

        #region ComboBox ItemsSource collections

        // ListLoader collections
        public ObservableCollection<string> ViewpointList;
        public ObservableCollection<string> SceneTypeList;
        public ObservableCollection<string> ScenePurposeList;
        public ObservableCollection<string> StoryRoleList;
        public ObservableCollection<string> EmotionList;
        public ObservableCollection<string> GoalList;
        public ObservableCollection<string> OppositionList;
        public ObservableCollection<string> OutcomeList;
        public ObservableCollection<string> ValueExchangeList;

        #endregion  

        #region Constructors

        public PlotPointViewModel()
        {
            _story = Ioc.Default.GetService<StoryController>();
            _logger = Ioc.Default.GetService<LogService>();
            _wtr = Ioc.Default.GetService<StoryWriter>();
            _rdr = Ioc.Default.GetService<StoryReader>();

            Viewpoint = string.Empty;
            Date = string.Empty;
            Time = string.Empty;
            Setting = string.Empty;
            SceneType = string.Empty;
            Char1 = string.Empty;
            Char2 = string.Empty;
            Char3 = string.Empty;
            Role1 = string.Empty;
            Role2 = string.Empty;
            Role3 = string.Empty;
            ScenePurpose = string.Empty;
            ValueExchange = string.Empty;
            Remarks = string.Empty;
            Protagonist = string.Empty;
            ProtagEmotion = string.Empty;
            ProtagGoal = string.Empty;
            Antagonist = string.Empty;
            AntagEmotion = string.Empty;
            AntagGoal = string.Empty;
            Opposition = string.Empty;
            Outcome = string.Empty;
            Emotion = string.Empty;
            NewGoal = string.Empty;
            Events = string.Empty;
            Consequences = string.Empty;
            Significance = string.Empty;
            Realization = string.Empty;
            Review = string.Empty;
            Notes = string.Empty;

            Dictionary<string, ObservableCollection<string>> lists = _story.ListControlSource;
            ViewpointList = lists["Viewpoint"];
            SceneTypeList = lists["SceneType"];
            ScenePurposeList = lists["ScenePurpose"];
            StoryRoleList = lists["StoryRole"];
            EmotionList = lists["Emotion"];
            GoalList = lists["Goal"];
            OppositionList = lists["Opposition"];
            OutcomeList = lists["Outcome"];
            ViewpointList = lists["Viewpoint"];
            ValueExchangeList = lists["ValueExchange"];
        }

        #endregion
     }
}
