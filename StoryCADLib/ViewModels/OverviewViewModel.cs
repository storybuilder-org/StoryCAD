using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Resources;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml.Controls;
using StoryCAD.Models;
using StoryCAD.Services.Logging;
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
    private bool _changeable; // process property changes for this story element
    private bool _changed;    // this story element has changed

    // Premise synchronization fields

    // True if StoryProblem has been set, in which case any
    // Overview Premise changes must also be made to that
    // ProblemModel's Premise property.
    private bool _syncPremise;

    // If a StoryProblem is set, this is the ProblemModel it points to
    private ProblemModel _storyProblemModel;

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

    private string _storyProblem;  // The Guid of a Problem StoryElement
    public string StoryProblem
    {
        get => _storyProblem;
        set 
        {   
            SetProperty(ref _storyProblem, value);
            if (value.Equals(string.Empty))
            {
                _syncPremise = false;
                return;
            }
            _storyProblemModel = (ProblemModel) StoryElement.StringToStoryElement(_storyProblem);
            _syncPremise = true;     // Set Premise to read-only
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

    private string _viewpointCharacter;
    public string ViewpointCharacter
    {
        get => _viewpointCharacter;
        set => SetProperty(ref _viewpointCharacter, value);
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

    // The StoryModel is passed when CharacterPage is navigated to
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
        Voice = Model.Voice;
        LiteraryTechnique = Model.LiteraryDevice;
        Tense = Model.Tense;
        Style = Model.Style;
        Tone = Model.Tone;
        Style = Model.Style;
        StoryProblem = Model.StoryProblem;
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
            // Story.Uuid is read-only and cannot be assigned
            Model.Name = Name;
            IsTextBoxFocused = false;
            Model.DateCreated = DateCreated;
            Model.Author = Author;
            Model.DateModified = DateModified;
            Model.StoryType = StoryType;
            Model.StoryGenre = StoryGenre;
            Model.Viewpoint = Viewpoint;
            Model.ViewpointCharacter = ViewpointCharacter;
            Model.Voice = Voice;
            Model.LiteraryDevice = LiteraryTechnique;
            Model.Style = Style;
            Model.Tense = Tense;
            Model.Style = Style;
            Model.Tone = Tone;
            Model.StoryProblem = StoryProblem;
            Model.StoryIdea = StoryIdea;
            Model.Concept = Concept;
            Model.Premise = Premise;
            if (_syncPremise)
            {
                _storyProblemModel.Premise = Premise;
            }
            Model.StructureNotes = StructureNotes;
            Model.Notes = Notes;
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

    #endregion

    #region Constructor

    public OverviewViewModel()
    {
        _logger = Ioc.Default.GetService<LogService>();

        try
        {
            Dictionary<string, ObservableCollection<string>> _lists = GlobalData.ListControlSource;
            StoryTypeList = _lists["StoryType"];
            GenreList = _lists["Genre"];
            ViewpointList = _lists["Viewpoint"];
            LiteraryTechniqueList = _lists["LiteraryTechnique"];
            VoiceList = _lists["Voice"];
            TenseList = _lists["Tense"];
            StyleList = _lists["LiteraryStyle"];
            ToneList = _lists["Tone"];
        }
        catch (Exception e)
        {
            _logger!.LogException(LogLevel.Fatal, e, "Error loading lists in Problem view model");
            ShowError();
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
        StoryProblem = string.Empty;
        _syncPremise = false;     
        StructureNotes = string.Empty;
        Notes = string.Empty;

        PropertyChanged += OnPropertyChanged;
    }

    async void ShowError()
    {
        await new ContentDialog()
        {
            XamlRoot = GlobalData.XamlRoot,
            Title = "Error loading resources",
            Content = "An error has occurred, please reinstall or update StoryCAD to continue.",
            CloseButtonText = "Close"
        }.ShowAsync();
        throw new MissingManifestResourceException();

    }
    #endregion
}