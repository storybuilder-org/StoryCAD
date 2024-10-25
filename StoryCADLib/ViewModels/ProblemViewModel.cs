using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using StoryCAD.Controls;
using StoryCAD.Models.Tools;
using StoryCAD.Services.Messages;
using StoryCAD.Services.Navigation;
using StoryCAD.ViewModels.Tools;

namespace StoryCAD.ViewModels;

public class ProblemViewModel : ObservableRecipient, INavigable
{
    #region Fields

    private readonly LogService _logger;
    private bool _changeable;
    private bool _changed;

    // Premise sync fields

    private OverviewModel _overviewModel;

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
    private string _storyQuestion;
    public string StoryQuestion
    {
        get => _storyQuestion;
        set => SetProperty(ref _storyQuestion, value);
    }

    // Problem protagonist data

    private string _protagonist;  // The Guid of a Character StoryElement
    public string Protagonist
    {
        get => _protagonist;
        set => SetProperty(ref _protagonist, value);
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

    private string _antagonist;  // The Guid of a Character StoryElement
    public string Antagonist
    {
        get => _antagonist;
        set => SetProperty(ref _antagonist, value);
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
	/// Name of PlotPatternModel used in structure tab
	/// </summary>
    public string StructureModelTitle
    {
	    get => _structureModelTitle;
	    set => SetProperty(ref _structureModelTitle, value);
	}

	private string _structureDescription;
	/// <summary>
	/// Name of PlotPatternModel used in structure tab
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
    /// PlotPatternModel used in structure tab
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

    private ObservableCollection<StructureBeatViewModel> structureBeats;
    public ObservableCollection<StructureBeatViewModel> StructureBeats
    {
	    get => structureBeats;
	    set => SetProperty(ref structureBeats, value);
    }

	public RelayCommand ConflictCommand { get; }

	private string _boundStructure;
	/// <summary>
	/// A problem cannot be bound to more than one structure
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
	#endregion

	#region Methods

	public void Activate(object parameter)
    {
        Model = (ProblemModel)parameter;
        LoadModel();
    }

    /// <summary>
    /// Saves this VM back to the story.
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
                _logger.Log(LogLevel.Info, $"ProblemViewModel.OnPropertyChanged: {args.PropertyName} changed");
            _changed = true;
            ShellViewModel.ShowChange();
        }
    }

    private void LoadModel()
    {
        _changeable = false;
        _changed = false;

        Uuid = Model.Uuid;
        Name = Model.Name;
        if (Name.Equals("New Problem"))
            IsTextBoxFocused = true;    
        ProblemType = Model.ProblemType;
        ConflictType = Model.ConflictType;
        ProblemCategory = Model.ProblemCategory;
        Subject = Model.Subject;
        StoryQuestion = Model.StoryQuestion;
        ProblemSource = Model.ProblemSource;
        // Character instances like Protagonist and Antagonist are 
        // read and written as the CharacterModel's StoryElement Guid 
        // string. A binding converter, StringToStoryElementConverter,
        // provides the UI the corresponding StoryElement itself.
        Protagonist = Model.Protagonist ?? string.Empty;
        ProtGoal = Model.ProtGoal;
        ProtMotive = Model.ProtMotive;
        ProtConflict = Model.ProtConflict;
        Antagonist = Model.Antagonist ?? string.Empty;
        AntagGoal = Model.AntagGoal;
        AntagMotive = Model.AntagMotive;
        AntagConflict = Model.AntagConflict;
        Outcome = Model.Outcome;
        Method = Model.Method;
        Theme = Model.Theme;
        Premise = Model.Premise;
        Notes = Model.Notes;
        StoryModel model = ShellViewModel.GetModel();
        Guid root = model.ExplorerView[0].Uuid;
        _overviewModel = (OverviewModel) model.StoryElements.StoryElementGuids[root];
        ProblemModel storyProblem = (ProblemModel) StoryElement.StringToStoryElement(_overviewModel.StoryProblem);
        if (storyProblem != null) { _syncPremise = true; }
        else _syncPremise = false;

		StructureModelTitle = Model.StructureTitle;
		StructureDescription = Model.StructureDescription;
		StructureBeats = Model.StructureBeats;
		BoundStructure = Model.BoundStructure;
		
		//Ensure correct set of Elements are loaded for Structure Lists
		Problems = Ioc.Default.GetService<ShellViewModel>().StoryModel.StoryElements.Problems;
		Scenes = Ioc.Default.GetService<ShellViewModel>().StoryModel.StoryElements.Scenes;
		_changeable = true;
	}

    internal void SaveModel()
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
            Model.Protagonist = Protagonist ?? string.Empty;
            Model.ProtGoal = ProtGoal;
            Model.ProtMotive = ProtMotive;
            Model.ProtConflict = ProtConflict;
            Model.Antagonist = Antagonist ?? string.Empty;
            Model.AntagGoal = AntagGoal;
            Model.AntagMotive = AntagMotive;
            Model.AntagConflict = AntagConflict;
            Model.Outcome = Outcome;
            Model.Method = Method;
            Model.Theme = Theme;
            Model.StoryQuestion = StoryQuestion;
			Model.Premise = Premise;
			Model.StructureTitle = StructureModelTitle;
			Model.StructureDescription = StructureDescription;
			Model.StructureBeats = StructureBeats;
			if (_syncPremise) { _overviewModel.Premise = Premise; }
            Model.Notes = Notes;
            Model.BoundStructure = BoundStructure;
		}
		catch (Exception ex)
        {
            Ioc.Default.GetRequiredService<LogService>().LogException(LogLevel.Error,
                ex, $"Failed to save problem model - {ex.Message}");
        }
    }

    /// <summary>
    /// Opens conflict builder
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
            CloseButtonText = "Close",
        };
        Conflict _selectedConflict = new();
        _conflictDialog.Content = _selectedConflict;
        ContentDialogResult _result = await Ioc.Default.GetService<Windowing>().ShowContentDialog(_conflictDialog);

        if (_selectedConflict.ExampleText == null) {_selectedConflict.ExampleText = "";}
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
	    string value = (sender as ComboBox).SelectedValue.ToString();
		if (!_changeable)
	    {
		    SetProperty(ref _structureModelTitle, value);
		    return;
	    }

		//Show dialog if structure has been set previously
		ContentDialogResult Result;
	    if (!string.IsNullOrEmpty(StructureModelTitle))
	    {
		    Result = await Ioc.Default.GetRequiredService<Windowing>()
			    .ShowContentDialog(new()
			    {
				    Title = "This will clear selected story beats",
				    PrimaryButtonText = "Confirm",
				    SecondaryButtonText = "Cancel"
			    });
	    }
	    else { Result = ContentDialogResult.Primary; }


	    if (Result == ContentDialogResult.Primary && !string.IsNullOrEmpty(value))
	    {
		    //Update value 
		    SetProperty(ref _structureModelTitle, value);

		    //Resolve master plot model if not empty
		    PlotPatternModel BeatSheet = Ioc.Default.GetRequiredService<BeatSheetsViewModel>().BeatSheets[value];

			StructureDescription = BeatSheet.PlotPatternNotes;

		    //Set model
		    StructureBeats.Clear();

		    foreach (var item in BeatSheet.PlotPatternScenes)
		    {
			    StructureBeats.Add(new StructureBeatViewModel
			    {
				    Title = item.SceneTitle,
				    Description = item.Notes,
			    });
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

    #region Constructors

    public ProblemViewModel()
    {
        _logger = Ioc.Default.GetService<LogService>();

        ProblemType = string.Empty;
        ConflictType = string.Empty;
        Subject = string.Empty;
        ProblemSource = string.Empty;
        StoryQuestion = string.Empty;
        Protagonist = null;
        ProtGoal = string.Empty;
        ProtMotive = string.Empty;
        ProtConflict = string.Empty;
        Antagonist = string.Empty;
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
        StructureBeats = new();
        BoundStructure = string.Empty;
		try
        {
            Dictionary<string, ObservableCollection<string>> _lists = Ioc.Default.GetService<ListData>().ListControlSource;
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
}