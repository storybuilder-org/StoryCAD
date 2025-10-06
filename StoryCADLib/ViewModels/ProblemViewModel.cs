using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using StoryCAD.Controls;
using StoryCAD.Models.Tools;
using StoryCAD.Services;
using StoryCAD.Services.Messages;
using StoryCAD.Services.Navigation;
using StoryCAD.Services.Outline;
using StoryCAD.ViewModels.SubViewModels;
using StoryCAD.ViewModels.Tools;

namespace StoryCAD.ViewModels;

public class ProblemViewModel : ObservableRecipient, INavigable, ISaveable
{
    #region Constructors

    public ProblemViewModel(ILogService logger, AppState appState, BeatSheetsViewModel beatSheetsViewModel)
    {
        _logger = logger;
        _beatSheetsViewModel = beatSheetsViewModel;
        _appState = appState;
        ProblemType = string.Empty;
        ConflictType = string.Empty;
        Subject = string.Empty;
        ProblemSource = string.Empty;
        Description = string.Empty;
        Protagonist = Guid.Empty;
        ProtGoal = string.Empty;
        ProtMotive = string.Empty;
        ProtConflict = string.Empty;
        Antagonist = Guid.Empty;
        AntagGoal = string.Empty;
        AntagMotive = string.Empty;
        AntagConflict = string.Empty;
        Outcome = string.Empty;
        Method = string.Empty;
        Theme = string.Empty;
        Premise = string.Empty;
        _syncPremise = false;
        Notes = string.Empty;
        StructureModelTitle = string.Empty;
        StructureDescription = string.Empty;
        StructureBeats = new ObservableCollection<StructureBeatViewModel>();
        BoundStructure = string.Empty;
        try
        {
            var _lists = Ioc.Default.GetService<ListData>().ListControlSource;
            ProblemTypeList = _lists["ProblemType"];
            ConflictTypeList = _lists["ConflictType"];
            ProblemCategoryList = _lists["ProblemCategory"];
            SubjectList = _lists["ProblemSubject"];
            ProblemSourceList = _lists["ProblemSource"];
            GoalList = _lists["Goal"];
            MotiveList = _lists["Motive"];
            ConflictList = _lists["Conflict"];
            OutcomeList = _lists["Outcome"];
            MethodList = _lists["Method"];
            ThemeList = _lists["Theme"];
        }
        catch (Exception e)
        {
            _logger!.LogException(LogLevel.Fatal, e, "Error loading lists in Problem view model");
            Ioc.Default.GetRequiredService<Windowing>().ShowResourceErrorMessage();
        }

        ConflictCommand = new RelayCommand(ConflictTool, () => true);

        PropertyChanged += OnPropertyChanged;
    }

    #endregion

    /// <summary>
    ///     Creates a new story beat.
    /// </summary>
    public void CreateBeat(object sender, RoutedEventArgs e)
    {
        StructureBeats.Add(new StructureBeatViewModel("New Beat", "Describe your beat here"));
    }

    /// <summary>
    ///     Deletes this beat
    /// </summary>
    public void DeleteBeat(object sender, RoutedEventArgs e)
    {
        try
        {
            Ioc.Default.GetService<OutlineService>()!.DeleteBeat(_storyModel, Model, SelectedBeatIndex);
        }
        catch (Exception ex)
        {
            Messenger.Send(new StatusChangedMessage(new StatusMessage(ex.Message, LogLevel.Warn, true)));
        }
    }

    /// <summary>
    ///     Moves this beat up
    /// </summary>
    public void MoveUp(object sender, RoutedEventArgs e)
    {
        if (SelectedBeat == null)
        {
            Messenger.Send(new StatusChangedMessage(new StatusMessage("Select a beat", LogLevel.Warn)));
            return;
        }

        if (SelectedBeatIndex > 0)
        {
            StructureBeats.Move(SelectedBeatIndex, SelectedBeatIndex - 1);
        }
        else
        {
            Messenger.Send(
                new StatusChangedMessage(new StatusMessage("This is already the first beat.", LogLevel.Warn, true)));
        }
    }

    /// <summary>
    ///     Moves this beat up
    /// </summary>
    public void MoveDown(object sender, RoutedEventArgs e)
    {
        if (SelectedBeat == null)
        {
            Messenger.Send(new StatusChangedMessage(new StatusMessage("Select a beat", LogLevel.Warn)));
            return;
        }

        var max = StructureBeats.Count;

        if (SelectedBeatIndex < max - 1)
        {
            StructureBeats.Move(SelectedBeatIndex, SelectedBeatIndex + 1);
        }
        else
        {
            Messenger.Send(
                new StatusChangedMessage(new StatusMessage("This is already the last beat.", LogLevel.Warn, true)));
        }
    }

    /// <summary>
    ///     Assigns a new beat
    /// </summary>
    public async void AssignBeat(object sender, ItemClickEventArgs e)
    {
        if (SelectedBeat == null)
        {
            Messenger.Send(new StatusChangedMessage(new StatusMessage("Select a beat", LogLevel.Warn)));
            return;
        }

        //Get the element we want to bind.
        var DesiredBind = (e.ClickedItem as StoryElement).Uuid;

        var OutlineVM = Ioc.Default.GetService<OutlineViewModel>();
        try
        {
            //Find element being bound.

            var Element = _appState.CurrentDocument!.Model.StoryElements.First(g => g.Uuid == DesiredBind);
            var ElementIndex = _appState.CurrentDocument.Model.StoryElements.IndexOf(Element);

            //Check if problem is being dropped and enforce rule.
            if (Element.ElementType == StoryItemType.Problem)
            {
                var problem = (ProblemModel)Element;
                //Enforce rule that problems can only be bound to one structure beat model
                if (!string.IsNullOrEmpty(problem.BoundStructure)) //Check element is actually bound elsewhere
                {
                    var ContainingStructure = (ProblemModel)_appState.CurrentDocument.Model.StoryElements
                        .First(g => g.Uuid == Guid.Parse(problem.BoundStructure));
                    //Show dialog asking to rebind.
                    var res = await Ioc.Default.GetRequiredService<Windowing>().ShowContentDialog(new ContentDialog
                    {
                        Title = "Already assigned!",
                        Content =
                            $"This problem is already assigned to a different structure ({ContainingStructure.Name}) " +
                            $"Would you like to assign it here instead?",
                        PrimaryButtonText = "Assign here",
                        SecondaryButtonText = "Cancel"
                    });

                    //Do nothing if user clicks don't rebind.
                    if (res != ContentDialogResult.Primary)
                    {
                        return;
                    }

                    removeBindData(ContainingStructure, problem);
                }

                //If its a problem Bind
                if (problem.Uuid == Uuid)
                {
                    BoundStructure = Uuid.ToString();
                }
                else
                {
                    problem.BoundStructure = Uuid.ToString();
                    _appState.CurrentDocument.Model.StoryElements[ElementIndex] = problem;
                }
            }

            SelectedBeat.Guid = DesiredBind;
            SelectedBeat = null;
            SelectedBeatIndex = -1;
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Warn, "Failed to bind valid element (Structure Tab) " + ex.Message);
        }
    }

    /// <summary>
    ///     Unbinds an element from the selected beat.
    /// </summary>
    public void UnbindElement(object sender, RoutedEventArgs e)
    {
        if (SelectedBeat == null)
        {
            Messenger.Send(new StatusChangedMessage(new StatusMessage("Select a beat", LogLevel.Warn)));
            return;
        }

        if (SelectedBeat.Guid == Guid.Empty)
        {
            Messenger.Send(new StatusChangedMessage(new StatusMessage("Nothing is bound to this beat", LogLevel.Warn)));
            return;
        }

        Ioc.Default.GetRequiredService<OutlineService>().UnasignBeat(
            _appState.CurrentDocument!.Model, Model, SelectedBeatIndex);
        SelectedBeat.Guid = Guid.Empty;

        // clear selection so you force the user to re-select next time
        SelectedBeat = null;
        SelectedBeatIndex = -1;
    }


    /// <summary>
    ///     Helper to remove bind data
    /// </summary>
    private void removeBindData(ProblemModel ContainingStructure, ProblemModel problem)
    {
        var OutlineVM = Ioc.Default.GetService<OutlineViewModel>();
        if (problem.BoundStructure.Equals(Uuid.ToString())) //Rebind from VM
        {
            var oldStructure = ContainingStructure.StructureBeats.First(g => g.Guid == problem.Uuid);
            var index = StructureBeats.IndexOf(oldStructure);
            StructureBeats[index].Guid = Guid.Empty;
        }
        else //Remove from old structure and update story elements.
        {
            var oldStructure = ContainingStructure.StructureBeats.First(g => g.Guid == problem.Uuid);
            var index = ContainingStructure.StructureBeats.IndexOf(oldStructure);
            ContainingStructure.StructureBeats[index].Guid = Guid.Empty;
            var ContainingStructIndex = _appState.CurrentDocument.Model.StoryElements.IndexOf(ContainingStructure);
            _appState.CurrentDocument.Model.StoryElements[ContainingStructIndex] = ContainingStructure;
        }
    }

    public async void SaveBeatSheet()
    {
        try
        {
            var FilePath = await Ioc.Default.GetRequiredService<Windowing>()
                .ShowFileSavePicker("Save", ".stbeat");

            //Picker error/canceled.
            if (FilePath == null)
            {
                return;
            }


            Ioc.Default.GetService<OutlineService>()
                .SaveBeatsheet(FilePath.Path, StructureDescription, StructureBeats.ToList());
        }
        catch (Exception)
        {
            Messenger.Send(new StatusChangedMessage(new StatusMessage("Failed to save Beatsheet", LogLevel.Error)));
        }
    }

    public async void LoadBeatSheet()
    {
        try
        {
            var FilePath = await Ioc.Default.GetRequiredService<Windowing>().ShowFilePicker("Load", ".stbeat");
            var model = Ioc.Default.GetService<OutlineService>().LoadBeatsheet(FilePath.Path);
            StructureDescription = model.Description;
            StructureBeats = new ObservableCollection<StructureBeatViewModel>(model.Beats);
        }
        catch (Exception)
        {
            Messenger.Send(new StatusChangedMessage(new StatusMessage("Failed to Load Beatsheet", LogLevel.Error)));
        }
    }

    #region Fields

    private readonly ILogService _logger;
    private readonly AppState _appState;
    private readonly BeatSheetsViewModel _beatSheetsViewModel;
    private OverviewModel _overviewModel;
    private StoryModel _storyModel;
    private bool _changeable;
    private bool _changed;

    // Premise sync fields


    // True if _overviewModel.StoryProblem has been set, in which
    // case any ProblemViewModel Premise changes must also be made
    // to the _overviewModel.Premise property.
    private bool _syncPremise;

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

    // Problem problem data
    private string _problemType;

    public string ProblemType
    {
        get => _problemType;
        set => SetProperty(ref _problemType, value);
    }

    private string _problemCategory;

    public string ProblemCategory
    {
        get => _problemCategory;
        set => SetProperty(ref _problemCategory, value);
    }

    private string _subject;

    public string Subject
    {
        get => _subject;
        set => SetProperty(ref _subject, value);
    }

    private string _problemSource;

    public string ProblemSource
    {
        get => _problemSource;
        set => SetProperty(ref _problemSource, value);
    }

    private string _conflictType;

    public string ConflictType
    {
        get => _conflictType;
        set => SetProperty(ref _conflictType, value);
    }

    // Description property (migrated from StoryQuestion)
    private string _description;

    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    // Problem protagonist data

    private Guid _protagonist; // The Guid of a Character  

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

    private string _protGoal;

    public string ProtGoal
    {
        get => _protGoal;
        set => SetProperty(ref _protGoal, value);
    }

    private string _protMotive;

    public string ProtMotive
    {
        get => _protMotive;
        set => SetProperty(ref _protMotive, value);
    }

    private string _protConflict;

    public string ProtConflict
    {
        get => _protConflict;
        set => SetProperty(ref _protConflict, value);
    }

    // Problem antagonist data

    private Guid _antagonist; // The Guid of a Character StoryElement

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

    private string _antagGoal;

    public string AntagGoal
    {
        get => _antagGoal;
        set => SetProperty(ref _antagGoal, value);
    }

    private string _antagMotive;

    public string AntagMotive
    {
        get => _antagMotive;
        set => SetProperty(ref _antagMotive, value);
    }

    private string _antagConflict;

    public string AntagConflict
    {
        get => _antagConflict;
        set => SetProperty(ref _antagConflict, value);
    }

    // Problem resolution data

    private string _outcome;

    public string Outcome
    {
        get => _outcome;
        set => SetProperty(ref _outcome, value);
    }

    private string _method;

    public string Method
    {
        get => _method;
        set => SetProperty(ref _method, value);
    }

    private string _theme;

    public string Theme
    {
        get => _theme;
        set => SetProperty(ref _theme, value);
    }

    private string _premise;

    public string Premise
    {
        get => _premise;
        set => SetProperty(ref _premise, value);
    }

    // Problem notes data

    private string _notes;

    public string Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
    }
    // Problem StructureModelTitle data

    private string _structureModelTitle;

    /// <summary>
    ///     Name of PlotPatternModel used in structure tab
    /// </summary>
    public string StructureModelTitle
    {
        get => _structureModelTitle;
        set => SetProperty(ref _structureModelTitle, value);
    }

    private string _structureDescription;

    /// <summary>
    ///     Name of PlotPatternModel used in structure tab
    /// </summary>
    public string StructureDescription
    {
        get => _structureDescription;
        set
        {
            //Set default text
            if (string.IsNullOrEmpty(value))
            {
                value = "Select a story beat sheet above to get started!";
            }

            SetProperty(ref _structureDescription, value);
        }
    }

    private PlotPatternModel _structureModel;

    /// <summary>
    ///     PlotPatternModel used in structure tab
    /// </summary>
    public PlotPatternModel StructureModel
    {
        get => _structureModel;
        set => SetProperty(ref _structureModel, value);
    }

    // The ProblemModel is passed when ProblemPage is navigated to
    private ProblemModel _model;

    public ProblemModel Model
    {
        get => _model;
        set => SetProperty(ref _model, value);
    }

    private ObservableCollection<StructureBeatViewModel> _structureBeats;

    public ObservableCollection<StructureBeatViewModel> StructureBeats
    {
        get => _structureBeats;
        set => SetProperty(ref _structureBeats, value);
    }

    public RelayCommand ConflictCommand { get; }

    private string _boundStructure;

    /// <summary>
    ///     A problem cannot be bound to more than one structure
    /// </summary>
    public string BoundStructure
    {
        get => _boundStructure;
        set => SetProperty(ref _boundStructure, value);
    }

    private ObservableCollection<StoryElement> _Scenes;

    public ObservableCollection<StoryElement> Scenes
    {
        get => _Scenes;
        set => SetProperty(ref _Scenes, value);
    }

    private ObservableCollection<StoryElement> _problems;

    public ObservableCollection<StoryElement> Problems
    {
        get => _problems;
        set => SetProperty(ref _problems, value);
    }

    private ObservableCollection<StoryElement> _characters;

    public ObservableCollection<StoryElement> Characters
    {
        get => _characters;
        set => SetProperty(ref _characters, value);
    }

    private bool _isBeatSheetReadOnly;

    /// <summary>
    ///     Controls if the beat sheet is read-only or not.
    /// </summary>
    public bool IsBeatSheetReadOnly
    {
        get => _isBeatSheetReadOnly;
        set => SetProperty(ref _isBeatSheetReadOnly, value);
    }

    private Visibility _beatsheetEditButtonsVisibility;

    /// <summary>
    ///     Controls if the beat sheet edit buttons are visible or not.
    /// </summary>
    public Visibility BeatsheetEditButtonsVisibility
    {
        get => _beatsheetEditButtonsVisibility;
        set => SetProperty(ref _beatsheetEditButtonsVisibility, value);
    }

    public int _selectedBeatIndex;

    /// <summary>
    ///     Selected Beat Index
    /// </summary>
    public int SelectedBeatIndex
    {
        get => _selectedBeatIndex;
        set => SetProperty(ref _selectedBeatIndex, value);
    }

    private StructureBeatViewModel _selectedBeat;

    /// <summary>
    ///     Selected Beat Item
    /// </summary>
    public StructureBeatViewModel SelectedBeat
    {
        get => _selectedBeat;
        set => SetProperty(ref _selectedBeat, value);
    }

    #endregion

    #region Methods

    public void Activate(object parameter)
    {
        Model = (ProblemModel)parameter;
        LoadModel();
    }

    /// <summary>
    ///     Saves this VM back to the story.
    /// </summary>
    /// <param name="parameter"></param>
    public void Deactivate(object parameter)
    {
        SaveModel();
    }

    public void OnPropertyChanged(object sender, PropertyChangedEventArgs args)
    {
        if (_changeable)
        {
            if (!_changed)
            {
                _logger.Log(LogLevel.Info, $"ProblemViewModel.OnPropertyChanged: {args.PropertyName} changed");
            }

            _changed = true;
            ShellViewModel.ShowChange();
        }
    }

    private void LoadModel()
    {
        _changeable = false;
        _changed = false;
        Characters = _appState.CurrentDocument!.Model.StoryElements.Characters;
        Uuid = Model.Uuid;
        Name = Model.Name;
        if (Name.Equals("New Problem"))
        {
            IsTextBoxFocused = true;
        }

        ProblemType = Model.ProblemType;
        ConflictType = Model.ConflictType;
        ProblemCategory = Model.ProblemCategory;
        Subject = Model.Subject;
        Description = Model.Description;
        ProblemSource = Model.ProblemSource;
        Protagonist = Model.Protagonist;
        SelectedProtagonist = Characters.FirstOrDefault(p => p.Uuid == Protagonist);
        ProtGoal = Model.ProtGoal;
        ProtMotive = Model.ProtMotive;
        ProtConflict = Model.ProtConflict;
        Antagonist = Model.Antagonist;
        SelectedAntagonist = Characters.FirstOrDefault(p => p.Uuid == Antagonist);
        AntagGoal = Model.AntagGoal;
        AntagMotive = Model.AntagMotive;
        AntagConflict = Model.AntagConflict;
        Outcome = Model.Outcome;
        Method = Model.Method;
        Theme = Model.Theme;
        Premise = Model.Premise;
        Notes = Model.Notes;
        _storyModel = _appState.CurrentDocument.Model;
        var root = _storyModel.ExplorerView[0].Uuid;
        var outlineService = Ioc.Default.GetService<OutlineService>();
        _overviewModel = (OverviewModel)outlineService!.GetStoryElementByGuid(_storyModel, root);
        if (_overviewModel.StoryProblem != Guid.Empty)
        {
            _syncPremise = true;
        }
        else
        {
            _syncPremise = false;
        }

        StructureModelTitle = Model.StructureTitle;
        StructureDescription = Model.StructureDescription;
        StructureBeats = Model.StructureBeats;
        BoundStructure = Model.BoundStructure;

        SelectedBeat = null;
        SelectedBeatIndex = -1;

        //Ensure correct set of Elements are loaded for Structure Lists
        Problems = _storyModel.StoryElements.Problems;
        Scenes = _storyModel.StoryElements.Scenes;

        //Enable/disable edit buttons based on selection
        if (StructureModelTitle == "Custom Beat Sheet")
        {
            BeatsheetEditButtonsVisibility = Visibility.Visible;
            IsBeatSheetReadOnly = false;
        }
        else
        {
            BeatsheetEditButtonsVisibility = Visibility.Collapsed;
            IsBeatSheetReadOnly = true;
        }

        _changeable = true;
    }

    public void SaveModel()
    {
        try
        {
            IsTextBoxFocused = false;
            Model.Name = Name;
            Model.ProblemType = ProblemType;
            Model.ConflictType = ConflictType;
            Model.ProblemCategory = ProblemCategory;
            Model.Subject = Subject;
            Model.ProblemSource = ProblemSource;
            Model.Protagonist = Protagonist;
            Model.ProtGoal = ProtGoal;
            Model.ProtMotive = ProtMotive;
            Model.ProtConflict = ProtConflict;
            Model.Antagonist = Antagonist;
            Model.AntagGoal = AntagGoal;
            Model.AntagMotive = AntagMotive;
            Model.AntagConflict = AntagConflict;
            Model.Outcome = Outcome;
            Model.Method = Method;
            Model.Theme = Theme;
            Model.Description = Description;
            Model.Premise = Premise;
            Model.StructureTitle = StructureModelTitle;
            Model.StructureDescription = StructureDescription;
            Model.StructureBeats = StructureBeats;
            if (_syncPremise)
            {
                _overviewModel.Premise = Premise;
            }

            Model.Notes = Notes;
            Model.BoundStructure = BoundStructure;
        }
        catch (Exception ex)
        {
            _logger.LogException(LogLevel.Error,
                ex, $"Failed to save problem model - {ex.Message}");
        }
    }

    /// <summary>
    ///     Opens conflict builder
    /// </summary>
    public async void ConflictTool()
    {
        _logger.Log(LogLevel.Info, "Displaying Conflict Finder tool dialog");

        //Creates and shows content
        ContentDialog _conflictDialog = new()
        {
            Title = "Conflict builder",
            PrimaryButtonText = "Copy to Protagonist",
            SecondaryButtonText = "Copy to Antagonist",
            CloseButtonText = "Close"
        };
        Conflict _selectedConflict = new();
        _conflictDialog.Content = _selectedConflict;
        var _result = await Ioc.Default.GetService<Windowing>().ShowContentDialog(_conflictDialog);

        if (_selectedConflict.ExampleText == null)
        {
            _selectedConflict.ExampleText = "";
        }

        switch (_result)
        {
            // Copy to Protagonist conflict
            case ContentDialogResult.Primary:
                ProtConflict = _selectedConflict.ExampleText;
                _logger.Log(LogLevel.Info, "Conflict Finder finished (copied to protagonist)");
                break;
            // Copy to Antagonist conflict
            case ContentDialogResult.Secondary:
                AntagConflict = _selectedConflict.ExampleText;
                _logger.Log(LogLevel.Info, "Conflict Finder finished (copied to antagonist)");
                break;
            default:
                _logger.Log(LogLevel.Info, "Conflict Finder canceled");
                break;
        }
    }

    public async void UpdateSelectedBeat(object sender, SelectionChangedEventArgs e)
    {
        if (!_changeable)
        {
            return;
        }

        var value = (sender as ComboBox).SelectedValue.ToString();

        //Show dialog if structure has been set previously
        ContentDialogResult Result;
        if (!string.IsNullOrEmpty(StructureModelTitle))
        {
            Result = await Ioc.Default.GetRequiredService<Windowing>()
                .ShowContentDialog(new ContentDialog
                {
                    Title = "This will clear selected story beats",
                    PrimaryButtonText = "Confirm",
                    SecondaryButtonText = "Cancel"
                });

            //Delete beats (This handles binds)
            for (var i = StructureBeats.Count - 1; i >= 0; i--)
            {
                SelectedBeat = StructureBeats[i];
                SelectedBeatIndex = i;
                DeleteBeat(null, null);
            }
        }
        else
        {
            Result = ContentDialogResult.Primary;
        }

        if (value == "Load Custom Beat Sheet from file...")
        {
            value = "Custom Beat Sheet";
            StructureModelTitle = value;
            LoadBeatSheet();
        }

        //Enable/disable edit buttons based on selection
        if (value == "Custom Beat Sheet")
        {
            BeatsheetEditButtonsVisibility = Visibility.Visible;
            IsBeatSheetReadOnly = false;
        }
        else
        {
            BeatsheetEditButtonsVisibility = Visibility.Collapsed;
            IsBeatSheetReadOnly = true;
        }

        if (Result == ContentDialogResult.Primary && !string.IsNullOrEmpty(value))
        {
            //Update value 
            SetProperty(ref _structureModelTitle, value);

            //Resolve master plot model if not empty
            var BeatSheet = _beatSheetsViewModel.BeatSheets[value];

            StructureDescription = BeatSheet.PlotPatternNotes;

            //Set model
            StructureBeats.Clear();

            foreach (var item in BeatSheet.PlotPatternScenes)
            {
                StructureBeats.Add(new StructureBeatViewModel(item.SceneTitle, item.Notes));
            }
        }
    }

    #endregion

    #region Control initialization sources

    // ListControls sources
    public ObservableCollection<string> ProblemTypeList;
    public ObservableCollection<string> ConflictTypeList;
    public ObservableCollection<string> ProblemCategoryList;
    public ObservableCollection<string> SubjectList;
    public ObservableCollection<string> ProblemSourceList;
    public ObservableCollection<string> GoalList;
    public ObservableCollection<string> MotiveList;
    public ObservableCollection<string> ConflictList;
    public ObservableCollection<string> OutcomeList;
    public ObservableCollection<string> MethodList;
    public ObservableCollection<string> ThemeList;

    #endregion;
}
