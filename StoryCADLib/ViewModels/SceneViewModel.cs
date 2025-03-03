using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using StoryCAD.Services.Messages;
using StoryCAD.Services.Navigation;
using StoryCAD.ViewModels.SubViewModels;

namespace StoryCAD.ViewModels;

public class SceneViewModel : ObservableRecipient, INavigable
{
    #region Fields
    OutlineViewModel OutlineVM = Ioc.Default.GetRequiredService<OutlineViewModel>();
    private readonly LogService _logger;
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
                NameChangeMessage _msg = new(_name, value);
                Messenger.Send(new NameChangedMessage(_msg));
            }
            SetProperty(ref _name, value);
        }
    }

    private bool _isTextBoxFocused;
    public bool IsTextBoxFocused
    {
        get => _isTextBoxFocused;
        set => SetProperty(ref _isTextBoxFocused, value);
    }

    private bool _allCharacters;

    public bool AllCharacters
    {
        get => _allCharacters;
        // Don't trigger OnPropertyChanged
        //set => _allCharacters = value;
        set => SetProperty(ref _allCharacters, value);
    }

    //  Scene general data
    private string _description;
    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    private Guid _viewpointCharacter;
    public Guid ViewpointCharacter
    {
        get => _viewpointCharacter;
        set
        {
            SetProperty(ref _viewpointCharacter, value);
            if (value.Equals(Guid.Empty))
                return;
            AddCastMember(StoryElement.GetByGuid(value));  // Insure the character is in the cast list
        }
    }
    private StoryElement _selectedViewpointCharacter;
    public StoryElement SelectedViewpointCharacter
    {
        get => _selectedViewpointCharacter;
        set
        {
            if (SetProperty(ref _selectedViewpointCharacter, value))
            {
                // Update StoryProblem GUID when SelectedProblem changes
                ViewpointCharacter = _selectedViewpointCharacter?.Uuid ?? Guid.Empty;
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

    private Guid _setting;
    public Guid Setting
    {
        get => _setting;
        set => SetProperty(ref _setting, value);
    }

    private StoryElement _selectedSetting;
    public StoryElement SelectedSetting
    {
        get => _selectedSetting;
        set
        {
            if (SetProperty(ref _selectedSetting, value))
            {
                // Update Setting GUID when SelectedSetting changes
                Setting = _selectedSetting?.Uuid ?? Guid.Empty;
            }
        }
    }


    private string _sceneType;
    public string SceneType
    {
        get => _sceneType;
        set => SetProperty(ref _sceneType, value);
    }

    // CastSource is assigned to either CastList or CharacterList

    private ObservableCollection<StoryElement> _castSource;
    public ObservableCollection<StoryElement> CastSource
    {
        get => _castSource;
        set => SetProperty(ref _castSource, value);
    }
    private ObservableCollection<StoryElement> _castList;
    public ObservableCollection<StoryElement> CastList
    {
        get => _castList;
        set => SetProperty(ref _castList, value);
    }
    private ObservableCollection<StoryElement> _characterList;
    public ObservableCollection<StoryElement> CharacterList
    {
        get => _characterList;
        set => SetProperty(ref _characterList, value);
    }

    // The Scene tab's Scene Sketch 
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

    private Guid _protagonist;
    public Guid Protagonist
    {
        get => _protagonist;
        set => SetProperty(ref _protagonist, value);
    }

    private StoryElement _selectedProtagonist;   
    public StoryElement SelectedProtagonist
    {
        get => _selectedProtagonist;
        set
        {
            if (SetProperty(ref _selectedProtagonist, value))
            {
                // Update Protagonist GUID when SelectedProtagonist changes
                Protagonist = _selectedProtagonist?.Uuid ?? Guid.Empty;
            }
        }
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

    private Guid _antagonist;
    public Guid Antagonist
    {
        get => _antagonist;
        set => SetProperty(ref _antagonist, value);
    }

    private StoryElement _selectedAntagonist;   
    public StoryElement SelectedAntagonist
    {
        get => _selectedAntagonist;
        set
        {
            if (SetProperty(ref _selectedAntagonist, value))
            {
                // Update Antagonist GUID when SelectedAntagonist changes
                Antagonist = _selectedAntagonist?.Uuid ?? Guid.Empty;
            }
        }
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

    //  Scene Sequel data (from Scene and Sequel)

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
        set => _model = value;
    }

    private string vpCharTip;
    public string VpCharTip
    {
        get => vpCharTip;
        set => SetProperty(ref vpCharTip, value);
    }

    private bool _vpCharTipIsOpen;
    public bool VpCharTipIsOpen
    {
        get => _vpCharTipIsOpen;
        set => SetProperty(ref _vpCharTipIsOpen, value);
    }

    private ObservableCollection<StoryElement> _characters;
    public ObservableCollection<StoryElement> Characters
    {
        get => _characters;
        set => SetProperty(ref _characters, value);
    }
    
    private ObservableCollection<StoryElement> _settings;
    public ObservableCollection<StoryElement> Settings
    {
        get => _settings;
        set => SetProperty(ref _settings, value);
    }
    
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
            if (!_changed)
                _logger.Log(LogLevel.Info, $"SceneViewModel.OnPropertyChanged: {args.PropertyName} changed");
            _changed = true;
            ShellViewModel.ShowChange();
        }
    }

    private void LoadModel()
    {
        _changeable = false;
        _changed = false;
        Characters = OutlineVM.StoryModel.StoryElements.Characters;
        Settings = OutlineVM.StoryModel.StoryElements.Settings;

        Uuid = Model.Uuid;
        Name = Model.Name;
        if (Name.Equals("New Scene"))
            IsTextBoxFocused = true;
        Description = Model.Description;
        Date = Model.Date;
        Time = Model.Time;
        Setting = Model.Setting;
        SelectedSetting = Settings.FirstOrDefault(p => p.Uuid == Setting);
        SceneType = Model.SceneType;

        // The list of cast members is loaded from the Model
        LoadCastList();
        // CharacterList is the StoryModel's list of all Character StoryElements
        CharacterList = OutlineVM.StoryModel.StoryElements.Characters;
        ViewpointCharacter = Model.ViewpointCharacter; // Add viewpoint character if missing
        SelectedViewpointCharacter = Characters.FirstOrDefault(p => p.Uuid == ViewpointCharacter);
        // Now set the correct view and initialize the cast elements    
        AllCharacters = CastList.Count == 0;
        InitializeCharacterList();

        // The ScenePurposes ObservableCollection<StringSelection>
        // supports multiple selected values (strings) because
        // a Scene can and should do more than one thing. It
        // uses a CheckBox to indicate that a purpose is true for
        // this Scene.
        // If a purpose is saved in the model, set it as selected.
        ScenePurposes.Clear();
        foreach (string purpose in ScenePurposeList)
        {
            if (Model.ScenePurpose.Contains(purpose))
                ScenePurposes.Add(new StringSelection(purpose, true));
            else
                ScenePurposes.Add(new StringSelection(purpose, false));
        }

        ValueExchange = Model.ValueExchange;
        Protagonist = Model.Protagonist;
        SelectedProtagonist = Characters.FirstOrDefault(p => p.Uuid == Protagonist);
        ProtagEmotion = Model.ProtagEmotion;
        ProtagGoal = Model.ProtagGoal;
        Antagonist = Model.Antagonist;
        SelectedAntagonist = Characters.FirstOrDefault(p => p.Uuid == Antagonist);
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
        UpdateViewpointCharacterTip();
        _changeable = true;
    }


    /// <summary>
    /// The scene tab's CastSource ListView is assigned to either a list of selected cast members
    /// (CastList), or a list of all the characters in the StoryModel (CharacterList).
    /// the Scene tab's also contains a ToggleSwitch which is bound to a RelayCommand and allows
    /// the user to switch between the two lists.
    /// This method (called from LoadModel) initialize these lists for the current scene.
    /// </summary>
    private void LoadCastList()
    {
        // Load the SceneViewModel's cast list from the Model
        CastList.Clear();
        foreach (Guid memberGuid in Model.CastMembers)
        {
            StoryElement element = StoryElement.GetByGuid(memberGuid);
            if (element != null && element.Type == StoryItemType.Character)
            {
                CastList.Add(element);
            }
        }
    }

    private void InitializeCharacterList()
    {
        CastSource = null;    // Insure CastList setter invokes OnPropertyChanged   
        if (AllCharacters)    //Display all characters from the StoryModel's character list
        {
            CastSource = CharacterList;
        }
        else  // Display only the selected cast members
        {
            CastSource = CastList;
        }
        foreach (StoryElement _element in CastSource)
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

        // Story.Uuid is read-only and cannot be assigned
        Model.Name = Name;
        IsTextBoxFocused = false;
        Model.Description = Description;
        Model.ViewpointCharacter = ViewpointCharacter;
        Model.Date = Date;
        Model.Time = Time;
        Model.Setting = Setting;
        Model.SceneType = SceneType;
        Model.CastMembers.Clear();
        foreach (StoryElement element in CastList)
            Model.CastMembers.Add(element.Uuid);
        Model.ScenePurpose.Clear();
        foreach (StringSelection purpose in ScenePurposes)
            if (purpose.Selection)
                Model.ScenePurpose.Add(purpose.StringName);
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

    public void AddScenePurpose(StringSelection selectedPurpose)
    {
        OnPropertyChanged();
        string msg = $"Add Scene Purpose {selectedPurpose.StringName}";
        Messenger.Send(new StatusChangedMessage(new(msg, LogLevel.Info, true)));
    }

    public void RemoveScenePurpose(StringSelection selectedPurpose)
    {
        OnPropertyChanged();
        string msg = $"Remove Scene Purpose {selectedPurpose.StringName}";
        Messenger.Send(new StatusChangedMessage(new(msg, LogLevel.Info, true)));
    }

    /// <summary>
    /// This method toggles the Scene Cast list from only the selected cast members
    /// to all characters (and vice versa.) The CharactersList is used to add or
    /// remove cast members.
    /// </summary>
    public void SwitchCastView(object sender, RoutedEventArgs e)
    {
        bool _changeState = _changeable;
        _changeable = false;
        ToggleSwitch toggleSwitch = sender as ToggleSwitch;
        _allCharacters = toggleSwitch.IsOn;
        InitializeCharacterList();
        if (AllCharacters)
        {
            Messenger.Send(new StatusChangedMessage(new("Add / Remove Cast Members", LogLevel.Info, true)));
        }
        else
        {
            Messenger.Send(new StatusChangedMessage(new("Show Selected Cast Members", LogLevel.Info, true)));
        }
        _changeable = _changeState; 
    }

    private bool CastMemberExists(StoryElement character)
    {
        foreach (StoryElement element in CastList)
        {
            if (element.Uuid == character.Uuid)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Add a Character to the cast list if it doesn't already exist.
    ///
    /// Placeholder (Guid.Empty) Characters are not added to the cast list.
    /// </summary>
    /// <param name="element"></param>
    public void AddCastMember(StoryElement element)
    {
        if (element.Uuid == Guid.Empty)
            return;
        // If the Character is already in the cast list, don't add it again
        if (CastMemberExists(element))
            return;
        CastList.Add(element);
        OnPropertyChanged();
        Messenger.Send(new StatusChangedMessage(new($"New cast member {element.Name} added", LogLevel.Info, true)));
    }

    public void RemoveCastMember(StoryElement element)
    {
        for (int _i = 0; _i <= CastList.Count - 1; _i++)
            if (CastList[_i].Uuid == element.Uuid)
            {
                CastList.RemoveAt(_i);
                OnPropertyChanged();
                Messenger.Send(
                    new StatusChangedMessage(new($"Cast member {element.Name} removed", LogLevel.Info, true)));
                return;
            }
    }

    /// <summary>
    /// Builds VpCharTip, the bound content of the ViewpointCharacterTip TeachingTip,
    /// by retrieving and parsing the OverviewModel's Viewpoint and ViewpointCharacter.
    /// 
    /// For example, if the Viewpoint is 'First person', the scene's viewpoint character
    /// should match the overview's viewpoint character, indicating the entire story is
    /// told in first person.
    /// 
    /// This is presented as a suggestion, not a strict rule.
    /// </summary>
    private void UpdateViewpointCharacterTip()
    {
        VpCharTipIsOpen = false;

        var shellModel = Ioc.Default.GetRequiredService<OutlineViewModel>().StoryModel;
        var node = shellModel.ExplorerView.FirstOrDefault();
        if (node == null)
        {
            _logger.Log(LogLevel.Warn, "No node found in ExplorerView.");
            return;
        }

        if (!shellModel.StoryElements.StoryElementGuids.TryGetValue(node.Uuid, out var element) || element is not OverviewModel overview)
        {
            _logger.Log(LogLevel.Warn, $"OverviewModel not found for node with UUID: {node.Uuid}");
            return;
        }

        string viewpointTip = string.IsNullOrEmpty(overview.Viewpoint)
            ? "No story viewpoint selected"
            : $"Story viewpoint = {overview.Viewpoint}";

        string viewpointCharacterTip = "No story viewpoint character selected";

        VpCharTip = $"{Environment.NewLine}{viewpointTip}\n{viewpointCharacterTip}";

        // Display the TeachingTip if a viewpoint character is selected
        if (overview.ViewpointCharacter != Guid.Empty)
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
        Setting = Guid.Empty;
        SceneType = string.Empty;
        CastList = new ObservableCollection<StoryElement>();
        ViewpointCharacter = Guid.Empty;
        ScenePurposes = new ObservableCollection<StringSelection>();
        ValueExchange = string.Empty;
        Remarks = string.Empty;
        Protagonist = Guid.Empty;
        ProtagEmotion = string.Empty;
        ProtagGoal = string.Empty;
        Antagonist = Guid.Empty;
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

        try
        {
            Dictionary<string, ObservableCollection<string>> _lists = Ioc.Default.GetService<ListData>().ListControlSource;
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
        }
        catch (Exception e)
        {
            _logger.LogException(LogLevel.Fatal, e, "Error loading lists in Problem view model");
            Ioc.Default.GetRequiredService<Windowing>().ShowResourceErrorMessage();
        }


        // Initialize cast member lists / display
        CastList = new ObservableCollection<StoryElement>();
        CharacterList = OutlineVM.StoryModel.StoryElements.Characters;
        CastSource = AllCharacters ? CharacterList : CastList;
        AllCharacters = true;

        PropertyChanged += OnPropertyChanged;
    }

    #endregion
}