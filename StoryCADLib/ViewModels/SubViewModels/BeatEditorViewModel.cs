using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using StoryCADLib.Models;
using StoryCADLib.Services;
using StoryCADLib.Services.Messages;
using StoryCADLib.Services.Outline;
using StoryCADLib.ViewModels.Tools;

namespace StoryCADLib.ViewModels.SubViewModels;

/// <summary>
///     Owns all beat-editing state and commands for the Structure tab.
///     Created by ProblemViewModel, not registered in IoC.
/// </summary>
[Microsoft.UI.Xaml.Data.Bindable]
public class BeatEditorViewModel : ObservableObject
{
    #region Fields

    private readonly ILogService _logger;
    private readonly AppState _appState;
    private readonly BeatSheetsViewModel _beatSheetsViewModel;
    private readonly OutlineService _outlineService;
    private readonly Windowing _windowing;
    private readonly Action _notifyDirty;

    private StoryModel _storyModel;
    private ProblemModel _problemModel;

    #endregion

    #region Constructor

    public BeatEditorViewModel(Action notifyDirty)
    {
        _logger = Ioc.Default.GetRequiredService<ILogService>();
        _appState = Ioc.Default.GetRequiredService<AppState>();
        _beatSheetsViewModel = Ioc.Default.GetRequiredService<BeatSheetsViewModel>();
        _outlineService = Ioc.Default.GetRequiredService<OutlineService>();
        _windowing = Ioc.Default.GetRequiredService<Windowing>();
        _notifyDirty = notifyDirty;

        StructureBeats = new ObservableCollection<StructureBeat>();
        SelectedElementSource = "Scene";

        PropertyChanged += OnPropertyChanged;

        // Commands
        CreateBeatCommand = new RelayCommand(CreateBeat);
        DeleteBeatCommand = new AsyncRelayCommand(DeleteBeatAsync, () => SelectedBeat != null);
        MoveUpCommand = new RelayCommand(MoveUp, () => SelectedBeat != null && SelectedBeatIndex > 0);
        MoveDownCommand = new RelayCommand(MoveDown, () => SelectedBeat != null && SelectedBeatIndex < StructureBeats.Count - 1);
        UnbindElementCommand = new RelayCommand(UnbindElement, CanUnbindElement);
    }

    #endregion

    #region Properties

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
        set => SetProperty(ref _selectedElementSource, value);
    }

    private StructureBeat _selectedBeat;
    public StructureBeat SelectedBeat
    {
        get => _selectedBeat;
        set => SetProperty(ref _selectedBeat, value);
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
        set => SetProperty(ref _selectedListElement, value);
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
                    SelectedBeat.Description = value;
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

    private string _boundStructure;
    public string BoundStructure
    {
        get => _boundStructure;
        set => SetProperty(ref _boundStructure, value);
    }

    #endregion

    #region Commands

    public RelayCommand CreateBeatCommand { get; }
    public IRelayCommand DeleteBeatCommand { get; }
    public RelayCommand MoveUpCommand { get; }
    public RelayCommand MoveDownCommand { get; }
    public RelayCommand UnbindElementCommand { get; }

    #endregion

    #region Methods

    /// <summary>
    ///     Sets the StoryModel context for beat operations that need it.
    /// </summary>
    public void SetStoryModel(StoryModel storyModel)
    {
        _storyModel = storyModel;
    }

    /// <summary>
    ///     Sets the ProblemModel context for beat operations that need it.
    /// </summary>
    public void SetProblemModel(ProblemModel problemModel)
    {
        _problemModel = problemModel;
    }

    /// <summary>
    ///     Loads beat-related data from the ProblemModel during navigation.
    /// </summary>
    public void LoadBeats(ProblemModel model, StoryModel storyModel)
    {
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
        if (args.PropertyName == nameof(SelectedElementSource))
        {
            CurrentElementSource = SelectedElementSource == "Scene" ? Scenes : Problems;
        }

        if (args.PropertyName == nameof(SelectedBeat))
        {
            CurrentElementDescription = SelectedBeat?.ElementDescription;
            SelectedBeatDescription = SelectedBeat?.Description;
        }

        if (args.PropertyName == nameof(SelectedListElement))
        {
            CurrentElementDescription = SelectedListElement?.Description;
        }
    }

    private void CreateBeat()
    {
        StructureBeats.Add(new StructureBeat("New Beat", "Describe your beat here"));
    }

    private async Task DeleteBeatAsync()
    {
        if (SelectedBeat == null)
            return;

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

    private bool CanUnbindElement()
    {
        return SelectedBeat != null && SelectedBeat.Guid != Guid.Empty;
    }

    private void UnbindElement()
    {
        if (SelectedBeat == null || SelectedBeat.Guid == Guid.Empty)
            return;

        _outlineService.UnasignBeat(_storyModel, _problemModel, SelectedBeatIndex);
        SelectedBeat.Guid = Guid.Empty;

        // Clear selection
        SelectedBeat = null;
        SelectedBeatIndex = -1;
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

    #endregion
}
