using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using StoryBuilder.DAL;
using StoryBuilder.Models;
using StoryBuilder.Services.Logging;
using StoryBuilder.Services.Messages;
using StoryBuilder.Services.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;

namespace StoryBuilder.ViewModels
{
    public class SceneViewModel : ObservableRecipient, INavigable
    {
        #region Fields

        private readonly StoryReader _rdr;
        private readonly StoryWriter _wtr;
        private readonly LogService _logger;
        StatusMessage _smsg;
        private bool _changeable; // process property changes for this story element
        private bool _changed;    // this story element has changed

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
                if (_changeable && _name != value) // Name changed?
                {
                    _logger.Log(LogLevel.Info, $"Requesting Name change from {_name} to {value}");
                    NameChangeMessage msg = new(_name, value);
                    Messenger.Send(new NameChangedMessage(msg));
                }
                SetProperty(ref _name, value);
            }
        }

        // Besides its GUID, each Scene has a unique (to this story) 
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

        private int _existingCastIndex;
        public int ExistingCastIndex
        {
            get => _existingCastIndex;
            set
            {
                SetProperty(ref _existingCastIndex, value);
                string emsg = $"Existing cast member {CastMembers[value].Name} selected";
                StatusMessage smsg = new(emsg, 200);
                Messenger.Send(new StatusChangedMessage(smsg));
            }
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

        private ObservableCollection<StoryElement> _castMembers;
        public ObservableCollection<StoryElement> CastMembers
        {
            get => _castMembers;
            set => SetProperty(ref _castMembers, value);
        }

        private string _newCastMember;
        public string NewCastMember
        {
            get => _newCastMember;
            set => SetProperty(ref _newCastMember, value);
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

        // Besides its GUID, each Scene has a unique (to this story) 
        // integer id number (useful in lists of scenes.)

        private SceneModel _model;
        public SceneModel Model
        {
            get => _model;
            set => SetProperty(ref _model, value);
        }

        #endregion

        #region Relay Commands

        public RelayCommand AddCastCommand { get; }
        public RelayCommand RemoveCastCommand { get; }

        #endregion

        #region Methods

        public async Task Activate(object parameter)
        {
            Model = (SceneModel)parameter;
            await LoadModel();   // Load the ViewModel from the Story
        }

        public async Task Deactivate(object parameter)
        {
            await SaveModel();    // Save the ViewModel back to the Story
        }
        private void OnPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            if (_changeable)
            {
                _changed = true;
                ShellViewModel.ShowChange();
            }
        }

        private async Task LoadModel()
        {
            _changeable = false;
            _changed = false;

            Uuid = Model.Uuid;
            Name = Model.Name;
            Id = Model.Id;
            Description = Model.Description;
            Viewpoint = Model.Viewpoint;
            Date = Model.Date;
            Time = Model.Time;
            Setting = Model.Setting;
            SceneType = Model.SceneType;
            CastMembers.Clear();
            foreach (string member in Model.CastMembers)
            {
                StoryElement element = StringToStoryElement(member);
                if (element != null)        // found
                     CastMembers.Add(StringToStoryElement(member));
            }
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
            
            _changeable = true;
        }

        internal async Task SaveModel()
        {
            _changeable = false;
            if (_changed)
            {
                // Story.Uuid is read-only and cannot be assigned
                Model.Name = Name;
                Model.Description = Description;
                Model.Viewpoint = Viewpoint;
                Model.Date = Date;
                Model.Time = Time;
                Model.Setting = Setting;
                Model.SceneType = SceneType;
                Model.CastMembers.Clear();
                foreach (StoryElement element in CastMembers)
                    Model.CastMembers.Add(element.ToString());
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

                //_logger.Log(LogLevel.Info, string.Format("Requesting IsDirty change to true"));
                //Messenger.Send(new IsChangedMessage(Changed));
            }
        }

        private bool CastMemberExists(string uuid)
        {
            foreach (StoryElement element in CastMembers) {if (uuid == element.Uuid.ToString()) {return true;}}
            return false;
        }

        private void AddCastMember()
        {
            // Edit the character to add
            StoryElement element = StringToStoryElement(NewCastMember);
            if (NewCastMember == null)
            {
                Messenger.Send(new StatusChangedMessage(new StatusMessage("Select a character to add to scene cast, 200", 200)));
                return;
            }
            if (CastMemberExists(NewCastMember))
            {
                _smsg = new StatusMessage("Character is already in scene cast", 200);
                Messenger.Send(new StatusChangedMessage(_smsg));
                return;
            }

            CastMembers.Add(element);
            string msg = $"New cast member {element.Name} added";
            _smsg = new StatusMessage(msg, 200);
            Messenger.Send(new StatusChangedMessage(_smsg));
            _logger.Log(LogLevel.Info, msg);
        }

        private void RemoveCastMember()
        {
            if (ExistingCastIndex == -1)
            {
                _smsg = new StatusMessage("Select a scene cast member to remove", 200);
                Messenger.Send(new StatusChangedMessage(_smsg));
                return;
            }
            StoryElement element = CastMembers[ExistingCastIndex];
            CastMembers.RemoveAt(ExistingCastIndex);
            string msg = $"Cast member {element.Name} removed";
            _smsg = new StatusMessage(msg, 200);
            Messenger.Send(new StatusChangedMessage(_smsg));
            _logger.Log(LogLevel.Info, msg);
        }

        private StoryElement StringToStoryElement(string value)
        {
            if (value == null)
                return null;
            if (value.Equals(string.Empty))
                return null;

            // Look for the StoryElement corresponding to the passed guid
            // (This is the normal approach)
            // Get the current StoryModel's StoryElementsCollection
            StoryElementCollection elements = GlobalData.StoryModel.StoryElements;
            if (Guid.TryParse(value, out Guid guid))
            {
                if (elements.StoryElementGuids.ContainsKey(guid)) { return elements.StoryElementGuids[guid]; }

            }

            // legacy: locate the StoryElement from its Name
            foreach (StoryElement element in elements)  // Character or Setting??? Search both?
            {
                if (element.Type == StoryItemType.Character | element.Type == StoryItemType.Setting)
                {
                    if (value.Trim().Equals(element.Name.Trim()))
                        return element;
                }
            }
            // not found
            string msg = $"Story Element not found name {value}";
            _logger.Log(LogLevel.Warn, msg);
            return null;   
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

        public SceneViewModel()
        {
            _logger = Ioc.Default.GetService<LogService>();
            _wtr = Ioc.Default.GetService<StoryWriter>();
            _rdr = Ioc.Default.GetService<StoryReader>();

            Viewpoint = string.Empty;
            Date = string.Empty;
            Time = string.Empty;
            Setting = string.Empty;
            SceneType = string.Empty;
            CastMembers = new ObservableCollection<StoryElement>();
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

            Dictionary<string, ObservableCollection<string>> lists = GlobalData.ListControlSource;
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

            AddCastCommand = new RelayCommand(AddCastMember, () => true);
            RemoveCastCommand = new RelayCommand(RemoveCastMember, () => true);

            PropertyChanged += OnPropertyChanged;
        }

        #endregion
    }
}
