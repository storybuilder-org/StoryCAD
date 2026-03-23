using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using StoryCADLib.Controls;
using StoryCADLib.Models.Tools;
using StoryCADLib.Services;
using StoryCADLib.Services.Messages;
using StoryCADLib.Services.Navigation;
using StoryCADLib.Services.Outline;
using StoryCADLib.ViewModels.SubViewModels;
using StoryCADLib.ViewModels.Tools;

namespace StoryCADLib.ViewModels;

public class ProblemViewModel : ObservableRecipient, INavigable, ISaveable, IReloadable
{
    #region Constructors

    public ProblemViewModel(ILogService logger, AppState appState)
    {
        _logger = logger;
        _appState = appState;

        // Create BeatSheetsViewModel (owns all beat sheet state and commands)
        BeatSheetsVm = new BeatSheetsViewModel(() =>
        {
            if (_changeable)
            {
                _changed = true;
                ShellViewModel.ShowChange();
            }
        });

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

    public async void AssignBeat(object sender, ItemClickEventArgs e)
    {
        if (BeatSheetsVm.SelectedBeat == null)
        {
            Messenger.Send(new StatusChangedMessage(new StatusMessage("Select a beat", LogLevel.Warn)));
            return;
        }

        // Set SelectedListElement so Element Description panel updates
        // (IsItemClickEnabled suppresses ListView selection)
        BeatSheetsVm.SelectedListElement = e.ClickedItem as StoryElement;

        var DesiredBind = (e.ClickedItem as StoryElement).Uuid;

        try
        {
            var Element = _appState.CurrentDocument!.Model.StoryElements.First(g => g.Uuid == DesiredBind);
            var ElementIndex = _appState.CurrentDocument.Model.StoryElements.IndexOf(Element);

            if (Element.ElementType == StoryItemType.Problem)
            {
                var problem = (ProblemModel)Element;
                if (!string.IsNullOrEmpty(problem.BoundStructure))
                {
                    var ContainingStructure = (ProblemModel)_appState.CurrentDocument.Model.StoryElements
                        .First(g => g.Uuid == Guid.Parse(problem.BoundStructure));
                    var res = await Ioc.Default.GetRequiredService<Windowing>().ShowContentDialog(new ContentDialog
                    {
                        Title = "Already assigned!",
                        Content =
                            $"This problem is already assigned to a different structure ({ContainingStructure.Name}) " +
                            $"Would you like to assign it here instead?",
                        PrimaryButtonText = "Assign here",
                        SecondaryButtonText = "Cancel"
                    });

                    if (res != ContentDialogResult.Primary)
                    {
                        return;
                    }

                    removeBindData(ContainingStructure, problem);
                }

                if (problem.Uuid == Uuid)
                {
                    BeatSheetsVm.BoundStructure = Uuid.ToString();
                }
                else
                {
                    problem.BoundStructure = Uuid.ToString();
                    _appState.CurrentDocument.Model.StoryElements[ElementIndex] = problem;
                }
            }

            BeatSheetsVm.SelectedBeat.Guid = DesiredBind;
            BeatSheetsVm.SelectedBeat = null;
            BeatSheetsVm.SelectedBeatIndex = -1;
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Warn, "Failed to bind valid element (Structure Tab) " + ex.Message);
        }
    }

    private void removeBindData(ProblemModel ContainingStructure, ProblemModel problem)
    {
        if (problem.BoundStructure.Equals(Uuid.ToString()))
        {
            var oldStructure = ContainingStructure.StructureBeats.First(g => g.Guid == problem.Uuid);
            var index = BeatSheetsVm.StructureBeats.IndexOf(oldStructure);
            BeatSheetsVm.StructureBeats[index].Guid = Guid.Empty;
        }
        else
        {
            var oldStructure = ContainingStructure.StructureBeats.First(g => g.Guid == problem.Uuid);
            var index = ContainingStructure.StructureBeats.IndexOf(oldStructure);
            ContainingStructure.StructureBeats[index].Guid = Guid.Empty;
            var ContainingStructIndex = _appState.CurrentDocument.Model.StoryElements.IndexOf(ContainingStructure);
            _appState.CurrentDocument.Model.StoryElements[ContainingStructIndex] = ContainingStructure;
        }
    }

    // SaveBeatSheet/LoadBeatSheet stay on ProblemViewModel for now (file picker dialog dependency)
    public async void SaveBeatSheet()
    {
        try
        {
            var FilePath = await Ioc.Default.GetRequiredService<Windowing>()
                .ShowFileSavePicker("Save", ".stbeat");

            if (FilePath == null)
                return;

            Ioc.Default.GetService<OutlineService>()
                .SaveBeatsheet(FilePath.Path, BeatSheetsVm.StructureDescription, BeatSheetsVm.StructureBeats.ToList());
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
            if (FilePath == null)
                return;

            var model = Ioc.Default.GetService<OutlineService>().LoadBeatsheet(FilePath.Path);
            BeatSheetsVm.StructureDescription = model.Description;
            BeatSheetsVm.StructureBeats = new ObservableCollection<StructureBeat>(model.Beats);
        }
        catch (Exception)
        {
            Messenger.Send(new StatusChangedMessage(new StatusMessage("Failed to Load Beatsheet", LogLevel.Error)));
        }
    }

    #region Fields

    private readonly ILogService _logger;
    private readonly AppState _appState;
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

    /// <summary>
    ///     Owns all beat sheet state, template data, and editing commands for the Structure tab.
    /// </summary>
    public BeatSheetsViewModel BeatSheetsVm { get; }

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
    // StructureModel is kept on ProblemViewModel (not beat-related state)
    private PlotPatternModel _structureModel;
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

    public RelayCommand ConflictCommand { get; }

    // Characters stays on ProblemViewModel (used by Protagonist/Antagonist tabs too)
    private ObservableCollection<StoryElement> _characters;
    public ObservableCollection<StoryElement> Characters
    {
        get => _characters;
        set => SetProperty(ref _characters, value);
    }

    #endregion

    #region Methods

    public void Activate(object parameter)
    {
        var param = parameter as ProblemModel;
        _logger.Log(LogLevel.Info, $"ProblemViewModel.Activate: parameter={param?.Name} (Uuid={param?.Uuid})");
        _changeable = false;  // Disable change tracking before setting Model
        _changed = false;
        Model = (ProblemModel)parameter;
        _logger.Log(LogLevel.Info, $"ProblemViewModel.Activate: Model set to {Model?.Name} (Uuid={Model?.Uuid})");
        LoadModel();
    }

    /// <summary>
    ///     Saves this VM back to the story.
    /// </summary>
    /// <param name="parameter"></param>
    public void Deactivate(object parameter)
    {
        _logger.Log(LogLevel.Info, $"ProblemViewModel.Deactivate: Saving to Model={Model?.Name} (Uuid={Model?.Uuid})");
        SaveModel();
    }

    public void OnPropertyChanged(object sender, PropertyChangedEventArgs args)
    {
        // Ignore Model property changes - these happen during navigation, not user edits
        if (args.PropertyName == nameof(Model))
            return;

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

    public void LoadModel()
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
        ProtGoal = Model.ProtGoal;
        ProtMotive = Model.ProtMotive;
        ProtConflict = Model.ProtConflict;
        Antagonist = Model.Antagonist;
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

        // Delegate beat-related loading to BeatSheetsViewModel
        BeatSheetsVm.LoadBeats(Model, _storyModel);

        _changeable = true;

        // Set UI-bound selection properties after enabling change tracking
        // These can trigger property cascades from UI binding updates
        var wasChangeable = _changeable;
        _changeable = false;
        SelectedProtagonist = Characters.FirstOrDefault(p => p.Uuid == Protagonist);
        SelectedAntagonist = Characters.FirstOrDefault(p => p.Uuid == Antagonist);
        _changeable = wasChangeable;
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
            if (_syncPremise)
            {
                _overviewModel.Premise = Premise;
            }

            Model.Notes = Notes;

            // Delegate beat-related saving to BeatSheetsViewModel
            BeatSheetsVm.SaveBeats(Model);
        }
        catch (Exception ex)
        {
            _logger.LogException(LogLevel.Error,
                ex, $"Failed to save problem model - {ex.Message}");
        }
    }

    public void ReloadFromModel()
    {
        if (Model != null)
        {
            LoadModel();
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
        if (!string.IsNullOrEmpty(BeatSheetsVm.StructureModelTitle))
        {
            Result = await Ioc.Default.GetRequiredService<Windowing>()
                .ShowContentDialog(new ContentDialog
                {
                    Title = "This will clear selected story beats",
                    PrimaryButtonText = "Confirm",
                    SecondaryButtonText = "Cancel"
                });

            // Only delete beats if user confirmed (fixes data-loss bug)
            if (Result == ContentDialogResult.Primary)
            {
                // Clear beats directly via OutlineService (no per-beat confirmation dialog)
                var outlineService = Ioc.Default.GetService<OutlineService>();
                for (var i = BeatSheetsVm.StructureBeats.Count - 1; i >= 0; i--)
                {
                    outlineService.DeleteBeat(_storyModel, Model, i);
                }
            }
            else
            {
                // Revert ComboBox to the current title
                var comboBox = sender as ComboBox;
                _changeable = false;
                comboBox.SelectedValue = BeatSheetsVm.StructureModelTitle;
                _changeable = true;
                return;
            }
        }
        else
        {
            Result = ContentDialogResult.Primary;
        }

        if (value == "Load Custom Beat Sheet from file...")
        {
            value = "Custom Beat Sheet";
            BeatSheetsVm.StructureModelTitle = value;
            LoadBeatSheet();
        }

        if (Result == ContentDialogResult.Primary && !string.IsNullOrEmpty(value))
        {
            //Update value
            BeatSheetsVm.StructureModelTitle = value;

            //Resolve master plot model if not empty
            var BeatSheet = BeatSheetsVm.BeatSheets[value];

            BeatSheetsVm.StructureDescription = BeatSheet.PlotPatternNotes;

            //Set model
            BeatSheetsVm.StructureBeats.Clear();

            foreach (var item in BeatSheet.PlotPatternScenes)
            {
                BeatSheetsVm.StructureBeats.Add(new StructureBeat(item.SceneTitle, item.Notes));
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
