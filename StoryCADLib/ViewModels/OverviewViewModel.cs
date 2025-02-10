using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using StoryCAD.Services.Messages;
using StoryCAD.Services.Navigation;

namespace StoryCAD.ViewModels;

/// <summary>
/// OverviewModel contains overview information for the entire story, such as title, author, and so on.
/// It's a good place to capture the original idea which prompted the story.
///
/// There is only one OverviewModel instance for each story. It's also the root of the Shell Page's
/// StoryExplorer TreeView.
/// </summary>
public class OverviewViewModel : ObservableRecipient, INavigable
{
    #region Fields

    private readonly LogService _logger;
    private readonly StoryModel _shellModel = ShellViewModel.GetModel();
    private bool _changeable; // process property changes for this story element
    private bool _changed;    // this story element has changed

    // Premise synchronization fields

    // True if StoryProblem has been set, in which case any
    // Overview Premise changes must also be made to that
    // ProblemModel's Premise property.
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
                NameChangeMessage msg = new(_name, value);
                Messenger.Send(new NameChangedMessage(msg));
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

    // Overview data

    private string _dateCreated;
    public string DateCreated
    {
        get => _dateCreated;
        set => SetProperty(ref _dateCreated, value);
    }

    private string _author;
    public string Author
    {
        get => _author;
        set => SetProperty(ref _author, value);

    }

    private string _dateModified;
    public string DateModified
    {
        get => _dateModified;
        set => SetProperty(ref _dateModified, value);
    }


    private string _storyIdea;
    public string StoryIdea
    {
        get => _storyIdea;
        set => SetProperty(ref _storyIdea, value);
    }

    // Concept data

    private string _concept;
    public string Concept
    {
        get => _concept;
        set => SetProperty(ref _concept, value);
    }

    // Premise data

    private string _premise;

    public string Premise
    {
        get => _premise;
        set => SetProperty(ref _premise, value);
    }

    private Guid _storyProblem;  // The Guid of a Problem StoryElement

    public Guid StoryProblem
    {
        get => _storyProblem;
        set
        {
            SetProperty(ref _storyProblem, value);
            if (value == Guid.Empty)
                _syncPremise = false;
            else
                _syncPremise = true;  
        }
    }

    private StoryElement _selectedProblem;
    public StoryElement SelectedProblem
    {
        get => _selectedProblem;
        set
        {
            if (SetProperty(ref _selectedProblem, value))
            {
                // Update StoryProblem GUID when SelectedProblem changes
                StoryProblem = _selectedProblem?.Uuid ?? Guid.Empty;
            }
        }
    }

    // Structure data
    private string _storyType;
    public string StoryType
    {
        get => _storyType;
        set => SetProperty(ref _storyType, value);
    }

    private string _storyGenre;
    public string StoryGenre
    {
        get => _storyGenre;
        set => SetProperty(ref _storyGenre, value);
    }

    private string _viewpoint;
    public string Viewpoint
    {
        get => _viewpoint;
        set => SetProperty(ref _viewpoint, value);
    }

    private Guid _viewpointCharacter;
    public Guid ViewpointCharacter
    {
        get => _viewpointCharacter;
        set => SetProperty(ref _viewpointCharacter, value);
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

    private string _voice;
    public string Voice
    {
        get => _voice;
        set => SetProperty(ref _voice, value);
    }

    private string _literaryTechnique;
    public string LiteraryTechnique
    {
        get => _literaryTechnique;
        set => SetProperty(ref _literaryTechnique, value);
    }

    private string _tense;
    public string Tense
    {
        get => _tense;
        set => SetProperty(ref _tense, value);
    }

    private string _style;
    public string Style
    {
        get => _style;
        set => SetProperty(ref _style, value);
    }

    private string _structureNotes;
    public string StructureNotes
    {
        get => _structureNotes;
        set => SetProperty(ref _structureNotes, value);
    }

    private string _tone;
    public string Tone
    {
        get => _tone;
        set => SetProperty(ref _tone, value);
    }

    // Notes data

    private string _notes;
    public string Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
    }

    // The StoryModel is passed when OverviewPage is navigated to
    private OverviewModel _model;
    public OverviewModel Model
    {
        get => _model;
        set => _model = value;
    }

    #endregion

    #region Methods

    public void Activate(object parameter)
    {
        Model = (OverviewModel)parameter;
        LoadModel();
    }

    public void Deactivate(object parameter)
    {
        SaveModel();
    }

    private void OnPropertyChanged(object sender, PropertyChangedEventArgs args)
    {
        if (_changeable)
        {
            if (!_changed)
                _logger.Log(LogLevel.Info, $"OverviewViewModel.OnPropertyChanged: {args.PropertyName} changed");
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
        DateCreated = Model.DateCreated;
        Author = Model.Author;
        DateModified = Model.DateModified;
        StoryType = Model.StoryType;
        StoryGenre = Model.StoryGenre;
        Viewpoint = Model.Viewpoint;
        ViewpointCharacter = Model.ViewpointCharacter;
        SelectedViewpointCharacter = Characters.FirstOrDefault(p => p.Uuid == ViewpointCharacter);
        Voice = Model.Voice;
        LiteraryTechnique = Model.LiteraryDevice;
        Tense = Model.Tense;
        Style = Model.Style;
        Tone = Model.Tone;
        Style = Model.Style;
        StoryProblem = Model.StoryProblem;
        // Set SelectedProblem based on StoryProblem GUID
        SelectedProblem = Problems.FirstOrDefault(p => p.Uuid == StoryProblem);
        StoryIdea = Model.StoryIdea;
        Concept = Model.Concept;
        Premise = Model.Premise;
        StructureNotes = Model.StructureNotes;
        Notes = Model.Notes;

        _changeable = true;
    }

    internal void SaveModel()
    {
        try
        {
	        if (Model != null) //ensure model isn't null.
	        {
		        // Story.Uuid is read-only and cannot be assigned
		        Model.Name = Name ?? "";
		        IsTextBoxFocused = false;
		        Model.DateCreated = DateCreated ?? "";
		        Model.Author = Author ?? "";
		        Model.DateModified = DateModified;
		        Model.StoryType = StoryType ?? "";
		        Model.StoryGenre = StoryGenre ?? "";
		        Model.Viewpoint = Viewpoint ?? "";
                Model.ViewpointCharacter = ViewpointCharacter;
		        Model.Voice = Voice ?? "";
		        Model.LiteraryDevice = LiteraryTechnique ?? "";
		        Model.Style = Style ?? "";
		        Model.Tense = Tense ?? "";
		        Model.Style = Style ?? "";
		        Model.Tone = Tone ?? "";
		        Model.StoryProblem = StoryProblem;
		        Model.StoryIdea = StoryIdea ?? "";
		        Model.Concept = Concept ?? "";
		        Model.Premise = Premise ?? "";
		        if (_syncPremise)
		        {
                    ProblemModel storyProblemModel = (ProblemModel) StoryElement.GetByGuid(StoryProblem);
                    storyProblemModel.Premise = Premise;
                }
		        Model.StructureNotes = StructureNotes ?? "";
		        Model.Notes = Notes ?? "";
			}
	        else
	        {
				_logger.Log(LogLevel.Fatal, "OverViewModel.Model is null, THIS SHOULDN'T HAPPEN.");
				Model = new OverviewModel("Overview", ShellViewModel.GetModel());
	        }

        }
        catch (Exception ex)
        {
            Ioc.Default.GetRequiredService<LogService>().LogException(LogLevel.Error,
                ex, $"Failed to save overview model - {ex.Message}");
        }

    }
    
    #endregion

    #region ComboBox ItemsSource collections

    public ObservableCollection<string> StoryTypeList;
    public ObservableCollection<string> GenreList;
    public ObservableCollection<string> ViewpointList;
    public ObservableCollection<string> LiteraryTechniqueList;
    public ObservableCollection<string> VoiceList;
    public ObservableCollection<string> TenseList;
    public ObservableCollection<string> StyleList;
    public ObservableCollection<string> ToneList;
    public ObservableCollection<StoryElement> Problems { get; } 
    public ObservableCollection<StoryElement> Characters { get; } 
    
    
    #endregion

    #region Constructor

    public OverviewViewModel()
    {
        _logger = Ioc.Default.GetService<LogService>();
        Problems = _shellModel.StoryElements.Problems;
        Characters = _shellModel.StoryElements.Characters;
        
        try
        {
            StoryProblem = Problems[0].Uuid;          // Set to "(none") (first Problem)
            ViewpointCharacter = Characters[0].Uuid;  // Set to "(none") (first Character)


            Dictionary<string, ObservableCollection<string>> lists= Ioc.Default.GetService<ListData>()!.ListControlSource;
            StoryTypeList = lists["StoryType"];
            GenreList = lists["Genre"];
            ViewpointList = lists["Viewpoint"];
            LiteraryTechniqueList = lists["LiteraryTechnique"];
            VoiceList = lists["Voice"];
            TenseList = lists["Tense"];
            StyleList = lists["LiteraryStyle"];
            ToneList = lists["Tone"];
        }
        catch (Exception e)
        {
            _logger!.LogException(LogLevel.Fatal, e, "Error loading lists in Problem view model");
            Ioc.Default.GetService<Windowing>()!.ShowResourceErrorMessage();
        }

        DateCreated = string.Empty;
        Author = string.Empty;
        DateModified = string.Empty;
        StoryType = string.Empty;
        StoryGenre = string.Empty;
        LiteraryTechnique = string.Empty;
        Viewpoint = string.Empty;
        Style = string.Empty;
        Tone = string.Empty;
        StoryIdea = string.Empty;
        Concept = string.Empty;
        Premise = string.Empty;
        _syncPremise = false;     
        StructureNotes = string.Empty;
        Notes = string.Empty;

        PropertyChanged += OnPropertyChanged;
    }
    #endregion
}