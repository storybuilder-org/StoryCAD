using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Windows.ApplicationModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using StoryBuilder.Models;
using StoryBuilder.Services.Logging;
using StoryBuilder.Services.Messages;
using StoryBuilder.Services.Navigation;

namespace StoryBuilder.ViewModels;

public class SceneViewModel : ObservableRecipient, INavigable
{
    #region Fields
    
    private readonly LogService _logger;
    private bool _changeable; // process property changes for this story element
    private bool _changed;    // this story element has changed
    private bool _toggleEnabled = false;
    private const bool ShowCastMembers = false;
    private const bool ShowAllCharacters = true;

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
                NameChangeMessage _msg = new(_name, value);
                Messenger.Send(new NameChangedMessage(_msg));
            }
            SetProperty(ref _name, value);
        }
    }

    private bool _showCastSelection;
    public bool ShowCastSelection
    {
        get => _showCastSelection;
        set => SetProperty(ref _showCastSelection, value);
    }
    //  Scene general data

    private string _description;
    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    private string _viewpointCharacter;
    public string ViewpointCharacter
    {
        get => _viewpointCharacter;
        set
        {
            SetProperty(ref _viewpointCharacter, value);
            if (value.Equals(string.Empty))
                return;
            if (!CastMemberExists(value))
            {
                CastMembers.Add(StringToStoryElement(value));
            }
        }
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

    private string _remarks;
    public string Remarks
    {
        get => _remarks;
        set => SetProperty(ref _remarks, value);
    }

    // Scene development data (from Lisa Cron's Story Genius)
    private ObservableCollection<StringSelection> _scenePurposes;
    public ObservableCollection<StringSelection> ScenePurposes 
    {
        get => _scenePurposes;
        set => SetProperty(ref _scenePurposes, value);
    }

    // The current CurrentPurpose
    private StringSelection _currentPurpose;
    public StringSelection CurrentPurpose
    {
        get => _currentPurpose;
        set => SetProperty(ref _currentPurpose, value);
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

    private SceneModel _model;
    public SceneModel Model
    {
        get => _model;
        set => SetProperty(ref _model, value);
    }

    private string _vpCharTip;
    public string VpCharTip
    {
        get => _vpCharTip;
        set => SetProperty(ref _vpCharTip, value);
    }

    private bool _vpCharTipIsOpen;
    public bool VpCharTipIsOpen
    {
        get => _vpCharTipIsOpen;
        set => SetProperty(ref _vpCharTipIsOpen, value);
    }

    #endregion

    #region Cast collection properties

    private ObservableCollection<StoryElement> _castMembers;
    public ObservableCollection<StoryElement> CastMembers
    {
        get => _castMembers;
        set => SetProperty(ref _castMembers, value);
    }
    private ObservableCollection<StoryElement> _characterList;
    public ObservableCollection<StoryElement> CharacterList
    {
        get => _characterList;
        set => SetProperty(ref _characterList, value);
    }

    private ObservableCollection<StoryElement> _castSource;
    public ObservableCollection<StoryElement> CastSource
    {
        get => _castSource;
        set => SetProperty(ref _castSource, value);
    }
    #endregion

    #region Relay Commands
    public RelayCommand SwitchCastViewCommand { get; }

    #endregion

    #region Methods

    public void Activate(object parameter)
    {
        Model = (SceneModel)parameter;
        LoadModel();   // Load the ViewModel from the Story
    }

    public void Deactivate(object parameter)
    {
        SaveModel();    // Save the ViewModel back to the Story
    }

    public void OnPropertyChanged(object sender, PropertyChangedEventArgs args)
    {
        if (_changeable)
        {
            _changed = true;
            ShellViewModel.ShowChange();
        }
    }

    private void LoadModel()
    {
        _changeable = false;
        _changed = false;
        _toggleEnabled = false;

        Uuid = Model.Uuid;
        Name = Model.Name;
        Description = Model.Description;
        Date = Model.Date;
        Time = Model.Time;
        Setting = Model.Setting;
        SceneType = Model.SceneType;
        // The Scene tab's cast list switches between either a list of
        // selected cast members or all Characters in the StoryModel.
        // The current choice is CastSource.
        // Initialize these lists for this scene.
        CastMembers.Clear();
        foreach (string _member in Model.CastMembers)
        {
            StoryElement _element = StringToStoryElement(_member);
            if (_element != null)
            {
                CastMembers.Add(StringToStoryElement(_member));
            }
        }
        CharacterList = ShellViewModel.ShellInstance.StoryModel.StoryElements.Characters;
        InitializeCharacterList();
        if (CastMembers.Count > 0)
        {
            CastSource = CastMembers;
            ShowCastSelection = ShowCastMembers;
        }
        else
        {
            CastSource = CharacterList;
            ShowCastSelection = ShowAllCharacters;
        }

        ViewpointCharacter = Model.ViewpointCharacter;

        // The ScenePurposes ObservableCollection<StringSelection>
        // supports multiple selected values (strings) because
        // a Scene can and should do more than one thing. It
        // uses a CheckBox to indicate that a purpose is true for
        // this Scene.
        // If a purpose is saved in the model, set it as selected.
        ScenePurposes.Clear();
        foreach (string purpose in ScenePurposeList) {
            if (Model.ScenePurposes.Contains(purpose))
                ScenePurposes.Add(new StringSelection(purpose, true));
            else
                ScenePurposes.Add(new StringSelection(purpose, false));
        }

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
        Remarks = Model.Remarks;
        Events = Model.Events;
        Consequences = Model.Consequences;
        Significance = Model.Significance;
        Realization = Model.Realization;
        Review = Model.Review;
        Notes = Model.Notes;
        GetOverviewViewpoint();

        _toggleEnabled = true;
        _changeable = true;
    }

    private void InitializeCharacterList()
    {
        foreach (StoryElement _element in CharacterList)
        {
            if (CastMemberExists(_element))
                _element.IsSelected = true;
            else
                _element.IsSelected = false;
        }
    }


    internal void SaveModel()
    {
        _changeable = false;
        if (_changed)
        {
            // Story.Uuid is read-only and cannot be assigned
            Model.Name = Name;
            Model.Description = Description;
            Model.ViewpointCharacter = ViewpointCharacter;
            Model.Date = Date;
            Model.Time = Time;
            Model.Setting = Setting;
            Model.SceneType = SceneType;
            Model.CastMembers.Clear();
            foreach (StoryElement _element in CastMembers)
                Model.CastMembers.Add(_element.ToString());
            Model.ScenePurposes.Clear();
            foreach (StringSelection _purpose in ScenePurposes)
                if (_purpose.Selection)
                    Model.ScenePurposes.Add(_purpose.StringName);
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
            Model.Remarks = Remarks;
            Model.Events = Events;
            Model.Consequences = Consequences;
            Model.Significance = Significance;
            Model.Realization = Realization;
            Model.Review = Review;
            Model.Notes = Notes;

            //_logger.Log(LogLevel.Info, string.Format("Requesting IsDirty change to true"));
            //Messenger.Send(new IsChangedMessage(Changed));
            _changeable = true;
        }
    }

    //public void AddScenePurpose(StringSelection selectedPurpose)
    //{
    //    if (_changeable == false)
    //        return;
    //    foreach (StringSelection _purpose in ScenePurposes)
    //    {
    //        if (_purpose.Value == selectedPurpose.Value)
    //        {
    //            _purpose.Selection = true;
    //            OnPropertyChanged();
    //            Messenger.Send(new StatusChangedMessage(new($"Scene purpose {selectedPurpose.Value} added", LogLevel.Info, true)));
    //        return;
    //        }
    //    }
    //}

    //public void RemoveScenePurpose(StringSelection selectedPurpose)
    //{
    //    if (_changeable == false)
    //        return;
    //    foreach (StringSelection selection in ScenePurposes)
    //        if (selection.Value.Equals(selectedPurpose.Value))
    //        {
    //            selection.Selection = false;
    //            OnPropertyChanged();
    //            Messenger.Send(new StatusChangedMessage(new($"Purpose {selection.Value} removed", LogLevel.Info, true)));
    //            return;
    //        }
    //}

    /// <summary>
    /// This method toggles the Scene Cast list from only the selected cast members
    /// to all characters (and vice versa.) The CharactersList is used to add or
    /// remove cast members.
    /// </summary>
    public void SwitchCastView()
    {
        if (_toggleEnabled)
        {
            if (ShowCastSelection == ShowAllCharacters)
            {
                CastSource = CastMembers;
                Messenger.Send(new StatusChangedMessage(new("Add / Remove Cast Members", LogLevel.Info, true)));
            }
            else if (ShowCastSelection == ShowCastMembers)
            {
                CastSource = CharacterList;
                Messenger.Send(new StatusChangedMessage(new("Show Selected Cast Members", LogLevel.Info, true)));
            }
        }
    }

    private bool CastMemberExists(string uuid)
    {
        return CastMembers.Any(element => uuid == element.Uuid.ToString());
    }

    private bool CastMemberExists(StoryElement element)
    {
        return CastMembers.Any(castMember => castMember.Uuid == element.Uuid);
    }

    public void AddCastMember(StoryElement element)
    {
        if (_changeable == false)
            return;
        // Edit the character to add
        if (CastMemberExists(element))
            return;

        CastMembers.Add(element);
        OnPropertyChanged();
        Messenger.Send(new StatusChangedMessage(new($"New cast member {element.Name} added", LogLevel.Info, true)));
    }

    public void RemoveCastMember(StoryElement element)
    {
        for (int _i = 0; _i < CastMembers.Count - 1; _i++)
            if (CastMembers[_i].Uuid == element.Uuid)
            {
                CastMembers.RemoveAt(_i);
                OnPropertyChanged();
                Messenger.Send(
                    new StatusChangedMessage(new($"Cast member {element.Name} removed", LogLevel.Info, true)));
                return;
            }
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
        StoryModel _shellModel = ShellViewModel.GetModel();
        StoryElementCollection _elements = _shellModel.StoryElements;
        if (Guid.TryParse(value, out Guid _guid))
        {
            if (_elements.StoryElementGuids.ContainsKey(_guid)) { return _elements.StoryElementGuids[_guid]; }

        }

        // legacy: locate the StoryElement from its Name
        foreach (StoryElement _element in _elements)  // Character or Setting??? Search both?
        {
            if (_element.Type == StoryItemType.Character | _element.Type == StoryItemType.Setting)
            {
                if (value.Trim().Equals(_element.Name.Trim()))
                    return _element;
            }
        }
        // not found
        string _msg = $"Story Element not found name {value}";
        _logger.Log(LogLevel.Warn, _msg);
        return null;
    }

    /// <summary>
    /// Build VpCharTip, the bound content of the ViewpointCharacterTip TeachingTip,
    /// by finding and parsing OverViewModel's Viewpoint and ViewPointCharacter.
    /// 
    /// For example, if the Viewpoint is 'First person', the scene's viewpoint character
    /// should be the same as the overview's viewpoint character (the entire story's
    /// told in first person.) 
    /// 
    /// This is presented as a suggestion, not a hard-and-fast rule.
    /// </summary>
    private void GetOverviewViewpoint()
    {
        string _viewpointText = "No story viewpoint selected";
        string _viewpointName = "No story viewpoint character selected";

        VpCharTipIsOpen = false;
        StoryModel _shellModel = ShellViewModel.GetModel();
        StoryNodeItem _node = _shellModel.ExplorerView[0];
        OverviewModel _overview = (OverviewModel)_shellModel.StoryElements.StoryElementGuids[_node.Uuid];
        //string viewpoint = overview?.Viewpoint;
        string _viewpoint = _overview?.Viewpoint != null ? _overview.Viewpoint : string.Empty;
        if (!_viewpoint.Equals(string.Empty))
            _viewpointText = "Story viewpoint = " + _viewpoint;
        string _viewpointChar = _overview?.ViewpointCharacter != null ? _overview.ViewpointCharacter : string.Empty;
        if (!_viewpointChar.Equals(string.Empty))
        {
            if (Guid.TryParse(_viewpointChar, out Guid _guid))
                _viewpointName = "Story viewpoint character = " + _shellModel.StoryElements.StoryElementGuids[_guid].Name;
            else
                _viewpointName = "Story viewpoint character not found";
        }
        StringBuilder _tip = new();
        _tip.AppendLine(string.Empty);
        _tip.AppendLine(_viewpointText);
        _tip.AppendLine(_viewpointName);
        VpCharTip = _tip.ToString();

        // The TeachingTip should only display if there's no scene Viewpoint Character selected
        if (ViewpointCharacter.Equals(string.Empty))
        {
            VpCharTipIsOpen = true;
            _logger.Log(LogLevel.Warn, "ViewpointCharacterTip displayed");
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

    public SceneViewModel()
    {
        _logger = Ioc.Default.GetService<LogService>();

        Date = string.Empty;
        Time = string.Empty;
        Setting = string.Empty;
        SceneType = string.Empty;
        CastMembers = new ObservableCollection<StoryElement>();
        ViewpointCharacter = string.Empty;
        ScenePurposes = new ObservableCollection<StringSelection>();
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
        ScenePurposes = new ObservableCollection<StringSelection>();

        Dictionary<string, ObservableCollection<string>> _lists = GlobalData.ListControlSource;
        ViewpointList = _lists["Viewpoint"];
        SceneTypeList = _lists["SceneType"];
        ScenePurposeList = _lists["ScenePurpose"];
        foreach (string purpose in ScenePurposeList)
            ScenePurposes.Add(new StringSelection(purpose, false));
        StoryRoleList = _lists["StoryRole"];
        EmotionList = _lists["Emotion"];
        GoalList = _lists["Goal"];
        OppositionList = _lists["Opposition"];
        OutcomeList = _lists["Outcome"];
        ViewpointList = _lists["Viewpoint"];
        ValueExchangeList = _lists["ValueExchange"];

        // Initialize cast member lists / display
        CastMembers = new ObservableCollection<StoryElement>();
        CharacterList = ShellViewModel.ShellInstance.StoryModel.StoryElements.Characters;
        CastSource = CharacterList;
        _showCastSelection = ShowAllCharacters;

        SwitchCastViewCommand = new RelayCommand(SwitchCastView, () => true);

        PropertyChanged += OnPropertyChanged;
    }

    #endregion
}