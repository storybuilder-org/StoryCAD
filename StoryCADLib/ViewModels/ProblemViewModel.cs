using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Security.Cryptography;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using StoryCAD.Controls;
using StoryCAD.Models.Tools;
using StoryCAD.Services.Messages;
using StoryCAD.Services.Navigation;
using StoryCAD.Services.Outline;
using StoryCAD.ViewModels.SubViewModels;
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

    private Guid _protagonist;  // The Guid of a Character  
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

    private Guid _antagonist;  // The Guid of a Character StoryElement
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

    private ObservableCollection<StructureBeatViewModel> _structureBeats;
    public ObservableCollection<StructureBeatViewModel> StructureBeats
    {
	    get => _structureBeats;
	    set => SetProperty(ref _structureBeats, value);
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
    private ObservableCollection<StoryElement> _characters;
    public ObservableCollection<StoryElement> Characters
    {
        get => _characters;
        set => SetProperty(ref _characters, value);
    }

    private bool _isBeatSheetReadOnly;
    /// <summary>
    /// Controls if the beat sheet is read-only or not.
    /// </summary>
    public bool IsBeatSheetReadOnly
    {
        get => _isBeatSheetReadOnly;
        set => SetProperty(ref _isBeatSheetReadOnly, value);
    }

    private Visibility _beatsheetEditButtonsVisibility;

    /// <summary>
    /// Controls if the beat sheet edit buttons are visible or not.
    /// </summary>
    public Visibility BeatsheetEditButtonsVisibility
    {
        get => _beatsheetEditButtonsVisibility;
        set => SetProperty(ref _beatsheetEditButtonsVisibility, value);
    }

    public int _selectedBeatIndex;
    /// <summary>
    /// Selected Beat Index
    /// </summary>
    public int SelectedBeatIndex
    {
        get => _selectedBeatIndex;
        set => SetProperty(ref _selectedBeatIndex, value);
    }
    private StructureBeatViewModel _selectedBeat;

    /// <summary>
    /// Selected Beat Item
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
        StoryModel story_model = Ioc.Default.GetRequiredService<OutlineViewModel>().StoryModel;

        _changeable = false;
        _changed = false;
        Characters = story_model.StoryElements.Characters;

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
        Guid root = story_model.ExplorerView[0].Uuid;
        _overviewModel = (OverviewModel)story_model.StoryElements.StoryElementGuids[root];
        if (_overviewModel.StoryProblem != Guid.Empty)
            _syncPremise = true; 
        else
            _syncPremise = false;

		StructureModelTitle = Model.StructureTitle;
		StructureDescription = Model.StructureDescription;
		StructureBeats = Model.StructureBeats;
		BoundStructure = Model.BoundStructure;
		
		//Ensure correct set of Elements are loaded for Structure Lists
		Problems = story_model.StoryElements.Problems;
        Scenes = story_model.StoryElements.Scenes;

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
		if (!_changeable)
	    {
		    return;
	    }

	    string value = (sender as ComboBox).SelectedValue.ToString();

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

            //Delete beats (This handles binds)
            for (int i = StructureBeats.Count - 1; i >= 0; i--)
            {
                SelectedBeat = StructureBeats[i];
                SelectedBeatIndex = i;
                DeleteBeat(null, null);
            }
        }
	    else { Result = ContentDialogResult.Primary; }

        //Enable/disable edit buttons based on selection
        if (value == "Custom Beatsheet")
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
        

        Characters = Ioc.Default.GetRequiredService<OutlineViewModel>().StoryModel.StoryElements.Characters;
        ProblemType = string.Empty;
        ConflictType = string.Empty;
        Subject = string.Empty;
        ProblemSource = string.Empty;
        StoryQuestion = string.Empty;
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

    /// <summary>
    /// Creates a new story beat.
    /// </summary>
    public void CreateBeat(object sender, RoutedEventArgs e)
    {
        StructureBeats.Add(new StructureBeatViewModel
        {
            Title = "New Beat",
            Description = "Describe your beat here"
        });
    }

    /// <summary>
    /// Deletes this beat
    /// </summary>
    public void DeleteBeat(object sender, RoutedEventArgs e)
    {
        if (SelectedBeat.Element.ElementType == StoryItemType.Problem)
        {
            // If this beat is bound to a problem, unbind it first.
            if (SelectedBeat.Element.Uuid == Uuid)
            {
                BoundStructure = String.Empty;
            }
            else
            {
                (Ioc.Default.GetRequiredService<OutlineViewModel>().StoryModel.
                    StoryElements.StoryElementGuids[SelectedBeat.Guid] as ProblemModel)
                    .BoundStructure = Guid.Empty.ToString();
            }
        }

        StructureBeats.Remove(SelectedBeat);
    }

    /// <summary>
    /// Moves this beat up
    /// </summary>
    public void MoveUp(object sender, RoutedEventArgs e)
    {
        if (SelectedBeatIndex > 0)
        {
            StructureBeats.Move(SelectedBeatIndex, SelectedBeatIndex - 1);
        }
        else
        {
            Ioc.Default.GetRequiredService<ShellViewModel>().
                ShowMessage(LogLevel.Warn, "This is already the first beat.", true);
        }
    }
    /// <summary>
    /// Moves this beat up
    /// </summary>
    public void MoveDown(object sender, RoutedEventArgs e)
    {   
        var max = StructureBeats.Count;

        if (SelectedBeatIndex < max-1)
        {
            StructureBeats.Move(SelectedBeatIndex, SelectedBeatIndex + 1);
        }
        else
        {
            Ioc.Default.GetRequiredService<ShellViewModel>()
                .ShowMessage(LogLevel.Warn, "This is already the last beat.", true);
        }
    }

    /// <summary>
    /// Assigns a new beat
    /// </summary>
    public async void AssignBeat(object sender, SelectionChangedEventArgs e)
    {
        //Get the element we want to bind.
        Guid DesiredBind = (e.AddedItems[0] as StoryElement).Uuid;

        OutlineViewModel OutlineVM = Ioc.Default.GetService<OutlineViewModel>();
        try 
        {
            //Find element being bound.
            StoryElement Element = OutlineVM.StoryModel.StoryElements.First(g => g.Uuid == DesiredBind);
            int ElementIndex = OutlineVM.StoryModel.StoryElements.IndexOf(Element);

            //Check if problem is being dropped and enforce rule.
            if (Element.ElementType == StoryItemType.Problem)
            {
                ProblemModel problem = (ProblemModel)Element;
                //Enforce rule that problems can only be bound to one structure beat model
                if (!string.IsNullOrEmpty(problem.BoundStructure)) //Check element is actually bound elsewhere
                {
                    ProblemModel ContainingStructure = (ProblemModel)OutlineVM.StoryModel.StoryElements
                        .First(g => g.Uuid == Guid.Parse(problem.BoundStructure));
                    //Show dialog asking to rebind.
                    var res = await Ioc.Default.GetRequiredService<Windowing>().ShowContentDialog(new()
                    {
                        Title = "Already assigned!",
                        Content = $"This problem is already assigned to a different structure ({ContainingStructure.Name}) " +            
                        $"Would you like to assign it here instead?",
                        PrimaryButtonText = "Assign here",
                        SecondaryButtonText = "Cancel"
                    });

                    //Do nothing if user clicks don't rebind.
                    if (res != ContentDialogResult.Primary) { return; }
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
                    OutlineVM.StoryModel.StoryElements[ElementIndex] = problem;
                }
            }

            SelectedBeat.Guid = DesiredBind;
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Warn, "Failed to bind valid element (Structure Tab) " + ex.Message);
        }
    }

    /// <summary>
    /// Unbinds an element from the selected beat.
    /// </summary>
    public void UnbindElement(object sender, RoutedEventArgs e)
    {
        //Get the beat we want to bind to
        var BeatVM = (sender as Button).DataContext as StructureBeatViewModel;

        //Get index of the beat we are unbinding from
        int BeatIndex = StructureBeats.IndexOf(BeatVM);

        StoryElement boundElement = Ioc.Default.GetRequiredService<OutlineViewModel>()
            .StoryModel.StoryElements.StoryElementGuids[BeatVM.Guid];
        if (boundElement.ElementType == StoryItemType.Problem)
        {
            removeBindData(Model, boundElement as ProblemModel);
        }
        BeatVM.Guid = Guid.Empty;
    }

   
    /// <summary>
    /// Helper to remove bind data 
    /// </summary>
    internal void removeBindData(ProblemModel ContainingStructure, ProblemModel problem)
    {
        OutlineViewModel OutlineVM = Ioc.Default.GetService<OutlineViewModel>();
        if (problem.BoundStructure.Equals(Uuid.ToString())) //Rebind from VM
        {
            StructureBeatViewModel oldStructure = ContainingStructure.StructureBeats.First(g => g.Guid == problem.Uuid);
            int index = StructureBeats.IndexOf(oldStructure);
            StructureBeats[index].Guid = Guid.Empty;
        }
        else //Remove from old structure and update story elements.
        {
            StructureBeatViewModel oldStructure = ContainingStructure.StructureBeats.First(g => g.Guid == problem.Uuid);
            int index = ContainingStructure.StructureBeats.IndexOf(oldStructure);
            ContainingStructure.StructureBeats[index].Guid = Guid.Empty;
            int ContainingStructIndex = OutlineVM.StoryModel.StoryElements.IndexOf(ContainingStructure);
            OutlineVM.StoryModel.StoryElements[ContainingStructIndex] = ContainingStructure;
        }
    }
}