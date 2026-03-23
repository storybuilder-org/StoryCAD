using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using StoryCADLib.Models;
using StoryCADLib.Models.Tools;
using StoryCADLib.Services;
using StoryCADLib.Services.Messages;
using StoryCADLib.Services.Outline;
using StoryCADLib.ViewModels.Tools;

namespace StoryCADLib.ViewModels.SubViewModels;

/// <summary>
///     Owns all beat sheet template data, editing state, and commands for the Structure tab.
///     Created by ProblemViewModel, not registered in IoC.
///
///     Referenced data classes:
///       StructureBeat (ViewModels/Tools/) — A single beat in a beat sheet. Has Title, Description,
///         and a Guid linking it to a scene or problem. Serialized as part of ProblemModel.StructureBeats
///         in the story file. Resolves its linked element lazily via IoC for display (ElementName, ElementIcon,
///         ElementDescription).
///       PlotPatternModel (Models/Tools/) — A beat sheet template (e.g., "Save the Cat", "Hero's Journey").
///         Has a name, notes, and a list of PlotPatternScenes. Loaded from ToolsData at startup.
///       PlotPatternScene (Models/Tools/) — A single scene/beat within a template. Has SceneTitle and Notes.
///       SavedBeatsheet (Models/) — Serialization wrapper for saving/loading custom beat sheets to .stbeat files.
///         Contains a Description and a list of StructureBeats.
///
///     OutlineService usage:
///       All beat mutations (assign, unassign, create, delete, save, load) delegate to OutlineService
///       so that both the UI and the StoryCAD API share the same logic. This ViewModel adds only
///       UI concerns (guards with status messages, confirmation dialogs, selection management).
/// </summary>
[Microsoft.UI.Xaml.Data.Bindable]
public class BeatSheetsViewModel : ObservableObject
{
    #region Fields

    private readonly ILogService _logger;
    private readonly AppState _appState;
    private readonly OutlineService _outlineService;
    private readonly Windowing _windowing;
    private readonly Action _notifyDirty;

    private StoryModel _storyModel;
    private ProblemModel _problemModel;
    private bool _isLoading;
    private bool _suppressDirty;

    #endregion

    #region Constructor

    public BeatSheetsViewModel(Action notifyDirty)
    {
        _logger = Ioc.Default.GetRequiredService<ILogService>();
        _appState = Ioc.Default.GetRequiredService<AppState>();
        _outlineService = Ioc.Default.GetRequiredService<OutlineService>();
        _windowing = Ioc.Default.GetRequiredService<Windowing>();
        _notifyDirty = notifyDirty;

        // Load beat sheet templates
        var toolSource = Ioc.Default.GetRequiredService<ToolsData>();
        var beatSheetNames = new List<string>();
        BeatSheets = new Dictionary<string, PlotPatternModel>();
        foreach (var plot in toolSource.BeatSheetSource)
        {
            beatSheetNames.Add(plot.PlotPatternName);
            BeatSheets.Add(plot.PlotPatternName, plot);
        }
        beatSheetNames.Sort();
        PlotPatternNames = new ObservableCollection<string>();
        foreach (var name in beatSheetNames)
        {
            PlotPatternNames.Add(name);
        }
        PlotPatternName = toolSource.MasterPlotsSource[0].PlotPatternName;

        StructureBeats = new ObservableCollection<StructureBeat>();
        SelectedElementSource = "Scene";

        PropertyChanged += OnPropertyChanged;

        // Commands (order matches button layout top to bottom)
        AssignBeatCommand = new AsyncRelayCommand(AssignBeatAsync);
        UnbindElementCommand = new RelayCommand(UnbindElement);
        CreateBeatCommand = new RelayCommand(CreateBeat);
        DeleteBeatCommand = new AsyncRelayCommand(DeleteBeatAsync);
        MoveUpCommand = new RelayCommand(MoveUp);
        MoveDownCommand = new RelayCommand(MoveDown);
        SaveBeatSheetCommand = new AsyncRelayCommand(SaveBeatSheetAsync);
    }

    #endregion

    #region Properties

    // Beat sheet template properties

    private string _plotPatternName;
    public string PlotPatternName
    {
        get => _plotPatternName;
        set
        {
            SetProperty(ref _plotPatternName, value);
            if (BeatSheets.ContainsKey(value))
            {
                PlotPatternNotes = BeatSheets[value].PlotPatternNotes;
            }
        }
    }

    private string _plotPatternNotes;
    public string PlotPatternNotes
    {
        get => _plotPatternNotes;
        set => SetProperty(ref _plotPatternNotes, value);
    }

    public readonly ObservableCollection<string> PlotPatternNames;

    public readonly Dictionary<string, PlotPatternModel> BeatSheets;

    // Beat editing properties

    private ObservableCollection<StructureBeat> _structureBeats;
    public ObservableCollection<StructureBeat> StructureBeats
    {
        get => _structureBeats;
        set => SetProperty(ref _structureBeats, value);
    }

    public IReadOnlyList<string> ElementSource { get; } = new[] { "Scene", "Problem" };

    private string _selectedElementSource;
    public string SelectedElementSource
    {
        get => _selectedElementSource;
        set
        {
            if (SetProperty(ref _selectedElementSource, value))
            {
                CurrentElementSource = value == "Scene" ? Scenes : Problems;
            }
        }
    }

    private StructureBeat _selectedBeat;
    public StructureBeat SelectedBeat
    {
        get => _selectedBeat;
        set
        {
            if (SetProperty(ref _selectedBeat, value))
            {
                CurrentElementDescription = _selectedBeat?.ElementDescription;
                _suppressDirty = true;
                SelectedBeatDescription = _selectedBeat?.Description;
                _suppressDirty = false;
            }
        }
    }

    private int _selectedBeatIndex = -1;
    public int SelectedBeatIndex
    {
        get => _selectedBeatIndex;
        set => SetProperty(ref _selectedBeatIndex, value);
    }

    private ObservableCollection<StoryElement> _scenes;
    public ObservableCollection<StoryElement> Scenes
    {
        get => _scenes;
        set => SetProperty(ref _scenes, value);
    }

    private ObservableCollection<StoryElement> _problems;
    public ObservableCollection<StoryElement> Problems
    {
        get => _problems;
        set => SetProperty(ref _problems, value);
    }

    private ObservableCollection<StoryElement> _currentElementSource;
    public ObservableCollection<StoryElement> CurrentElementSource
    {
        get => _currentElementSource;
        set => SetProperty(ref _currentElementSource, value);
    }

    private StoryElement _selectedListElement;
    public StoryElement SelectedListElement
    {
        get => _selectedListElement;
        set
        {
            if (SetProperty(ref _selectedListElement, value))
            {
                CurrentElementDescription = _selectedListElement?.Description;
            }
        }
    }

    private string _currentElementDescription;
    public string CurrentElementDescription
    {
        get => _currentElementDescription;
        set => SetProperty(ref _currentElementDescription, value);
    }

    private string _selectedBeatDescription;
    public string SelectedBeatDescription
    {
        get => _selectedBeatDescription;
        set
        {
            if (SetProperty(ref _selectedBeatDescription, value))
            {
                // Write back to the beat model
                if (SelectedBeat != null)
                {
                    SelectedBeat.Description = value;
                    if (!_suppressDirty)
                        _notifyDirty();
                }
            }
        }
    }

    private string _structureModelTitle;
    public string StructureModelTitle
    {
        get => _structureModelTitle;
        set => SetProperty(ref _structureModelTitle, value);
    }

    private string _structureDescription;
    public string StructureDescription
    {
        get => _structureDescription;
        set => SetProperty(ref _structureDescription, value);
    }

    private Guid _boundStructure;
    public Guid BoundStructure
    {
        get => _boundStructure;
        set => SetProperty(ref _boundStructure, value);
    }

    #endregion

    #region Commands (order matches button layout top to bottom)

    public IRelayCommand AssignBeatCommand { get; }
    public RelayCommand UnbindElementCommand { get; }
    public RelayCommand CreateBeatCommand { get; }
    public IRelayCommand DeleteBeatCommand { get; }
    public RelayCommand MoveUpCommand { get; }
    public RelayCommand MoveDownCommand { get; }
    public IRelayCommand SaveBeatSheetCommand { get; }

    #endregion

    #region Methods

    /// <summary>
    ///     Loads beat-related data from the ProblemModel during navigation.
    /// </summary>
    public void LoadBeats(ProblemModel model, StoryModel storyModel)
    {
        _isLoading = true;
        _storyModel = storyModel;
        _problemModel = model;

        StructureModelTitle = model.StructureTitle;
        StructureDescription = model.StructureDescription;
        StructureBeats = model.StructureBeats;
        BoundStructure = model.BoundStructure;

        SelectedBeat = null;
        SelectedBeatIndex = -1;

        // Populate element sources
        Problems = storyModel.StoryElements.Problems;
        Scenes = storyModel.StoryElements.Scenes;
        CurrentElementSource = Scenes;
        _isLoading = false;
    }

    /// <summary>
    ///     Saves beat-related data back to the ProblemModel during deactivation.
    /// </summary>
    public void SaveBeats(ProblemModel model)
    {
        model.StructureTitle = StructureModelTitle;
        model.StructureDescription = StructureDescription;
        model.StructureBeats = StructureBeats;
        model.BoundStructure = BoundStructure;
    }

    private void OnPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs args)
    {
        if (_isLoading)
            return;

        switch (args.PropertyName)
        {
            case nameof(StructureModelTitle):
            case nameof(StructureDescription):
            case nameof(StructureBeats):
            case nameof(BoundStructure):
                _notifyDirty();
                break;
        }
    }

    // Command methods (order matches button layout top to bottom)

    internal async Task AssignBeatAsync()
    {
        if (SelectedBeat == null)
        {
            WeakReferenceMessenger.Default.Send(
                new StatusChangedMessage(new StatusMessage("Select a beat", LogLevel.Warn)));
            return;
        }

        if (SelectedListElement == null)
        {
            WeakReferenceMessenger.Default.Send(
                new StatusChangedMessage(new StatusMessage("Select an element", LogLevel.Warn)));
            return;
        }

        var desiredBind = SelectedListElement.Uuid;

        if (desiredBind == _problemModel.Uuid)
        {
            WeakReferenceMessenger.Default.Send(
                new StatusChangedMessage(new StatusMessage("Cannot assign a problem as a beat on itself", LogLevel.Warn)));
            return;
        }

        try
        {
            // If assigning a problem, check if it's already bound elsewhere and confirm
            var element = _storyModel.StoryElements.FirstOrDefault(g => g.Uuid == desiredBind);
            if (element is ProblemModel problem)
            {
                var existingParent = _outlineService.GetExistingBeatAssignment(problem);
                if (existingParent != null && !_appState.Headless)
                {
                    var res = await _windowing.ShowContentDialog(new ContentDialog
                    {
                        Title = "Already assigned!",
                        Content =
                            $"This problem is already assigned to a different structure ({existingParent.Name}) " +
                            $"Would you like to assign it here instead?",
                        PrimaryButtonText = "Assign here",
                        SecondaryButtonText = "Cancel"
                    });

                    if (res != ContentDialogResult.Primary)
                        return;
                }
            }

            _outlineService.AssignElementToBeat(_storyModel, _problemModel, SelectedBeatIndex, desiredBind);
            SelectedBeat = null;
            SelectedBeatIndex = -1;
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Warn, "Failed to bind valid element (Structure Tab) " + ex.Message);
        }
    }

    internal void UnbindElement()
    {
        if (SelectedBeat == null || SelectedBeat.Guid == Guid.Empty)
            return;

        _outlineService.UnasignBeat(_storyModel, _problemModel, SelectedBeatIndex);
        SelectedBeat.Guid = Guid.Empty;

        // Clear selection
        SelectedBeat = null;
        SelectedBeatIndex = -1;
    }

    private void CreateBeat()
    {
        StructureBeats.Add(new StructureBeat("New Beat", "Describe your beat here"));
    }

    private async Task DeleteBeatAsync()
    {
        if (SelectedBeat == null)
            return;

        if (!_appState.Headless)
        {
            var beatTitle = SelectedBeat.Title ?? "this beat";
            var message = SelectedBeat.Guid != Guid.Empty
                ? $"Delete beat '{beatTitle}'? This will also remove its scene/problem assignment."
                : $"Delete beat '{beatTitle}'?";

            var result = await _windowing.ShowContentDialog(new ContentDialog
            {
                Title = "Delete Beat",
                Content = message,
                PrimaryButtonText = "Delete",
                SecondaryButtonText = "Cancel"
            });

            if (result != ContentDialogResult.Primary)
                return;
        }

        try
        {
            _outlineService.DeleteBeat(_storyModel, _problemModel, SelectedBeatIndex);
        }
        catch (Exception ex)
        {
            WeakReferenceMessenger.Default.Send(
                new StatusChangedMessage(new StatusMessage(ex.Message, LogLevel.Warn, true)));
        }
    }

    private void MoveUp()
    {
        if (SelectedBeat == null)
            return;

        if (SelectedBeatIndex > 0)
        {
            StructureBeats.Move(SelectedBeatIndex, SelectedBeatIndex - 1);
        }
    }

    private void MoveDown()
    {
        if (SelectedBeat == null)
            return;

        if (SelectedBeatIndex < StructureBeats.Count - 1)
        {
            StructureBeats.Move(SelectedBeatIndex, SelectedBeatIndex + 1);
        }
    }

    private async Task SaveBeatSheetAsync()
    {
        try
        {
            var filePath = await _windowing.ShowFileSavePicker("Save", ".stbeat");
            if (filePath == null)
                return;

            _outlineService.SaveBeatsheet(filePath.Path, StructureDescription, StructureBeats.ToList());
        }
        catch (Exception)
        {
            WeakReferenceMessenger.Default.Send(
                new StatusChangedMessage(new StatusMessage("Failed to save Beatsheet", LogLevel.Error)));
        }
    }

    // Supporting methods

    public async void LoadBeatSheet()
    {
        try
        {
            var filePath = await _windowing.ShowFilePicker("Load", ".stbeat");
            if (filePath == null)
                return;

            var model = _outlineService.LoadBeatsheet(filePath.Path);
            StructureDescription = model.Description;
            StructureBeats = new ObservableCollection<StructureBeat>(model.Beats);
        }
        catch (Exception)
        {
            WeakReferenceMessenger.Default.Send(
                new StatusChangedMessage(new StatusMessage("Failed to Load Beatsheet", LogLevel.Error)));
        }
    }

    public async void UpdateSelectedBeat(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading)
            return;

        var value = (sender as ComboBox).SelectedValue.ToString();

        ContentDialogResult result;
        if (!string.IsNullOrEmpty(StructureModelTitle))
        {
            result = await _windowing.ShowContentDialog(new ContentDialog
            {
                Title = "This will clear selected story beats",
                PrimaryButtonText = "Confirm",
                SecondaryButtonText = "Cancel"
            });

            if (result == ContentDialogResult.Primary)
            {
                for (var i = StructureBeats.Count - 1; i >= 0; i--)
                {
                    _outlineService.DeleteBeat(_storyModel, _problemModel, i);
                }
            }
            else
            {
                var comboBox = sender as ComboBox;
                _isLoading = true;
                comboBox.SelectedValue = StructureModelTitle;
                _isLoading = false;
                return;
            }
        }
        else
        {
            result = ContentDialogResult.Primary;
        }

        if (value == "Load Custom Beat Sheet from file...")
        {
            value = "Custom Beat Sheet";
            StructureModelTitle = value;
            LoadBeatSheet();
        }

        if (result == ContentDialogResult.Primary && !string.IsNullOrEmpty(value))
        {
            StructureModelTitle = value;

            var beatSheet = BeatSheets[value];
            StructureDescription = beatSheet.PlotPatternNotes;

            StructureBeats.Clear();
            foreach (var item in beatSheet.PlotPatternScenes)
            {
                StructureBeats.Add(new StructureBeat(item.SceneTitle, item.Notes));
            }
        }
    }


    #endregion
}
