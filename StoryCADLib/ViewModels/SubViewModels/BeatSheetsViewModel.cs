using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using StoryCADLib.Models;
using StoryCADLib.Models.Tools;
using StoryCADLib.Services;
using StoryCADLib.Services.Messages;
using StoryCADLib.Services.Outline;
using StoryCADLib.ViewModels.Tools;

namespace StoryCADLib.ViewModels.SubViewModels;

/// <summary>
///     Owns the beat sheet data, editing state, and commands for one Problem.
///     The Structure tab has one, created by ProblemViewModel. The beat tree window has one for
///     each beat column (issue #1546). Not registered in IoC.
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
///       StructureElementListViewModel — The list of Scenes and Problems to pick from.
///
///     OutlineService usage:
///       The rules of the structure are in OutlineService, so that the UI and the StoryCAD API share them.
///       This ViewModel adds only UI concerns (guards with status messages, dialogs, selection).
///       It keeps no copy of the beats or of the parent of the Problem: StructureBeats is the collection
///       of the model, and each change is written to the model at once. Two screens can then show the
///       same Problem. After each change the ViewModel sends StructureChangedMessage.
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

    public BeatSheetsViewModel(Action notifyDirty, StructureElementListViewModel elementList = null)
    {
        _logger = Ioc.Default.GetRequiredService<ILogService>();
        _appState = Ioc.Default.GetRequiredService<AppState>();
        _outlineService = Ioc.Default.GetRequiredService<OutlineService>();
        _windowing = Ioc.Default.GetRequiredService<Windowing>();
        _notifyDirty = notifyDirty;

        ElementList = elementList ?? new StructureElementListViewModel();
        ElementList.PropertyChanged += OnElementListPropertyChanged;

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
        ElementsOnNoBeat = new ObservableCollection<StoryElement>();

        PropertyChanged += OnPropertyChanged;

        // Commands (order matches button layout top to bottom)
        AssignBeatCommand = new AsyncRelayCommand(AssignBeatAsync);
        UnbindElementCommand = new RelayCommand(UnbindElement);
        AddElementCommand = new AsyncRelayCommand(AddSelectedElementAsync);
        RemoveElementCommand = new AsyncRelayCommand(RemoveSelectedElementAsync);
        CreateBeatCommand = new RelayCommand(CreateBeat);
        DeleteBeatCommand = new AsyncRelayCommand(DeleteBeatAsync);
        MoveUpCommand = new RelayCommand(MoveUp);
        MoveDownCommand = new RelayCommand(MoveDown);
        SaveBeatSheetCommand = new AsyncRelayCommand(SaveBeatSheetAsync);

        WeakReferenceMessenger.Default.Register<BeatSheetsViewModel, StructureChangedMessage>(
            this, static (r, m) => r.OnStructureChanged(m));
    }

    #endregion

    #region Properties

    /// <summary>
    ///     The list of Scenes and Problems to pick from. The beat tree window gives the same list
    ///     to each of its beat columns.
    /// </summary>
    public StructureElementListViewModel ElementList { get; }

    /// <summary>
    ///     Gives the XamlRoot for the dialogs of this ViewModel. Null means the main window.
    /// </summary>
    public Func<XamlRoot> DialogRoot { get; set; }

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

    // Header of a beat column

    private string _problemName;
    public string ProblemName
    {
        get => _problemName;
        private set => SetProperty(ref _problemName, value);
    }

    private string _problemCategory;
    public string ProblemCategory
    {
        get => _problemCategory;
        private set => SetProperty(ref _problemCategory, value);
    }

    /// <summary>
    ///     The GUID of the Problem that this ViewModel shows, or Guid.Empty before LoadBeats.
    /// </summary>
    public Guid ProblemGuid => _problemModel?.Uuid ?? Guid.Empty;

    // Beat editing properties

    private ObservableCollection<StructureBeat> _structureBeats;
    public ObservableCollection<StructureBeat> StructureBeats
    {
        get => _structureBeats;
        set => SetProperty(ref _structureBeats, value);
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
                if (_selectedBeat != null)
                {
                    SelectedNoBeatElement = null;
                }
            }
        }
    }

    private int _selectedBeatIndex = -1;
    public int SelectedBeatIndex
    {
        get => _selectedBeatIndex;
        set => SetProperty(ref _selectedBeatIndex, value);
    }

    /// <summary>
    ///     The Scenes and Problems that belong to the Problem and are on none of its beats.
    /// </summary>
    public ObservableCollection<StoryElement> ElementsOnNoBeat { get; }

    private StoryElement _selectedNoBeatElement;
    public StoryElement SelectedNoBeatElement
    {
        get => _selectedNoBeatElement;
        set
        {
            if (SetProperty(ref _selectedNoBeatElement, value) && value != null)
            {
                SelectedBeat = null;
                SelectedBeatIndex = -1;
                CurrentElementDescription = value.Description;
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
        set
        {
            if (_isLoading || value == _structureModelTitle)
                return;

            if (value == "Load Custom Beat Sheet from file...")
            {
                SetProperty(ref _structureModelTitle, "Custom Beat Sheet");
                LoadBeatSheet();
                return;
            }

            if (!string.IsNullOrEmpty(_structureModelTitle))
            {
                _ = ConfirmAndApplyBeatSheet(value);
                return;
            }

            ApplyBeatSheet(value);
        }
    }

    private string _structureDescription;
    public string StructureDescription
    {
        get => _structureDescription;
        set
        {
            if (SetProperty(ref _structureDescription, value) && !_isLoading && _problemModel != null)
            {
                // Write through, so a second screen on the same Problem reads the new text.
                _problemModel.StructureDescription = value;
                WeakReferenceMessenger.Default.Send(new StructureChangedMessage(_problemModel.Uuid, this));
            }
        }
    }

    #endregion

    #region Commands (order matches button layout top to bottom)

    public IRelayCommand AssignBeatCommand { get; }
    public RelayCommand UnbindElementCommand { get; }
    public IRelayCommand AddElementCommand { get; }
    public IRelayCommand RemoveElementCommand { get; }
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

        // #1641: raise PropertyChanged so the Beat Sheet ComboBox follows a reload after
        // Collaborator Accept. _isLoading keeps the setter logic and the dirty flag out.
        SetProperty(ref _structureModelTitle, model.StructureTitle, nameof(StructureModelTitle));
        StructureDescription = model.StructureDescription;
        StructureBeats = model.StructureBeats;
        ProblemName = model.Name;
        ProblemCategory = model.ProblemCategory;

        SelectedBeat = null;
        SelectedBeatIndex = -1;
        SelectedNoBeatElement = null;

        // The element list is made again for each load: an element can be new, renamed or in the trash.
        ElementList.Load(storyModel);
        FillElementsOnNoBeat();
        _isLoading = false;
    }

    /// <summary>
    ///     Reads the Problem from the model again, after another screen changed it.
    ///     Keeps the selection. Writes nothing and sends no message.
    /// </summary>
    public void Refresh()
    {
        if (_problemModel == null)
        {
            return;
        }

        _isLoading = true;
        SetProperty(ref _structureModelTitle, _problemModel.StructureTitle, nameof(StructureModelTitle));
        StructureDescription = _problemModel.StructureDescription;
        if (!ReferenceEquals(StructureBeats, _problemModel.StructureBeats))
        {
            StructureBeats = _problemModel.StructureBeats;
        }

        ProblemName = _problemModel.Name;
        ProblemCategory = _problemModel.ProblemCategory;
        FillElementsOnNoBeat();
        _isLoading = false;
    }

    /// <summary>
    ///     Saves beat-related data back to the ProblemModel during deactivation.
    ///     The beats and the parent of the Problem are not written here: the beats are the
    ///     collection of the model, and OutlineService keeps the parent.
    /// </summary>
    public void SaveBeats(ProblemModel model)
    {
        model.StructureTitle = StructureModelTitle;
        model.StructureDescription = StructureDescription;
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
                _notifyDirty();
                break;
        }
    }

    private void OnElementListPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(StructureElementListViewModel.SelectedListElement) &&
            ElementList.SelectedListElement != null)
        {
            CurrentElementDescription = ElementList.SelectedListElement.Element.Description;
        }
    }

    private void OnStructureChanged(StructureChangedMessage message)
    {
        if (_problemModel == null || ReferenceEquals(message.Source, this))
        {
            return;
        }

        if (message.ProblemGuid == _problemModel.Uuid)
        {
            Refresh();
        }
        else
        {
            // A Problem that moved to another Problem leaves the list of this one.
            FillElementsOnNoBeat();
        }
    }

    /// <summary>
    ///     Runs after each change that this ViewModel makes to the model.
    /// </summary>
    private void AfterChange()
    {
        _notifyDirty();
        if (_problemModel == null)
        {
            return;
        }

        FillElementsOnNoBeat();
        WeakReferenceMessenger.Default.Send(new StructureChangedMessage(_problemModel.Uuid, this));
    }

    private void FillElementsOnNoBeat()
    {
        var selected = SelectedNoBeatElement?.Uuid ?? Guid.Empty;
        ElementsOnNoBeat.Clear();
        if (_problemModel == null || _storyModel == null)
        {
            return;
        }

        foreach (var guid in _outlineService.GetElementsOnNoBeat(_problemModel))
        {
            if (_storyModel.StoryElements.StoryElementGuids.TryGetValue(guid, out var element) &&
                !_outlineService.IsInTrash(element))
            {
                ElementsOnNoBeat.Add(element);
            }
        }

        if (selected != Guid.Empty)
        {
            _selectedNoBeatElement = ElementsOnNoBeat.FirstOrDefault(e => e.Uuid == selected);
            OnPropertyChanged(nameof(SelectedNoBeatElement));
        }
    }

    /// <summary>
    ///     Runs one call to OutlineService. A refusal of the service shows as a status message.
    /// </summary>
    private bool CallService(Action call)
    {
        try
        {
            call();
            return true;
        }
        catch (InvalidOperationException ex)
        {
            WeakReferenceMessenger.Default.Send(
                new StatusChangedMessage(new StatusMessage(ex.Message, LogLevel.Warn)));
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Warn, "Failed to change the structure (Structure Tab) " + ex.Message);
        }

        return false;
    }

    private Task<ContentDialogResult> ShowDialog(ContentDialog dialog)
    {
        return _windowing.ShowContentDialog(dialog, DialogRoot?.Invoke());
    }

    private int IndexOfSelectedBeat()
    {
        if (SelectedBeat == null)
        {
            return -1;
        }

        return SelectedBeatIndex >= 0 && SelectedBeatIndex < StructureBeats.Count &&
               ReferenceEquals(StructureBeats[SelectedBeatIndex], SelectedBeat)
            ? SelectedBeatIndex
            : StructureBeats.IndexOf(SelectedBeat);
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

        var item = ElementList.SelectedListElement;
        if (item == null)
        {
            WeakReferenceMessenger.Default.Send(
                new StatusChangedMessage(new StatusMessage("Select an element", LogLevel.Warn)));
            return;
        }

        await PlaceOnBeatAsync(item.Element.Uuid, IndexOfSelectedBeat(), -1);

        // Keep the beat just assigned selected so its description stays in view, and show the
        // assigned element's description. Clearing SelectedBeat here emptied both panes.
        CurrentElementDescription = item.Element.Description;
    }

    /// <summary>
    ///     Puts an element on a beat of this Problem. The button and the drag operation both use it.
    /// </summary>
    /// <param name="element">The GUID of a Scene or a Problem.</param>
    /// <param name="index">The index of the target beat.</param>
    /// <param name="fromIndex">The index of the beat of this Problem that the element comes from, or -1.</param>
    internal async Task PlaceOnBeatAsync(Guid element, int index, int fromIndex)
    {
        if (_problemModel == null || index < 0 || index >= StructureBeats.Count ||
            StructureBeats[index].Guid == element)
        {
            return;
        }

        var storyElement = _storyModel.StoryElements.FirstOrDefault(g => g.Uuid == element);
        var otherBeat = -1;
        for (var i = 0; i < StructureBeats.Count; i++)
        {
            if (i != index && StructureBeats[i].Guid == element)
            {
                otherBeat = i;
                break;
            }
        }

        // A Scene that is on another beat of this Problem: move it, or put it on both?
        if (storyElement is SceneModel && otherBeat >= 0 && !_appState.Headless)
        {
            var res = await ShowDialog(new ContentDialog
            {
                Title = "Already on a beat",
                Content = $"This scene is already on the beat \"{StructureBeats[otherBeat].Title}\" of this problem.",
                PrimaryButtonText = "Move here",
                SecondaryButtonText = "Put here as well",
                CloseButtonText = "Cancel"
            });

            if (res == ContentDialogResult.None)
                return;

            if (res == ContentDialogResult.Primary)
            {
                var from = fromIndex >= 0 && fromIndex < StructureBeats.Count &&
                           StructureBeats[fromIndex].Guid == element
                    ? fromIndex
                    : otherBeat;
                if (CallService(() => _outlineService.MoveBeatElement(_storyModel, _problemModel, from, index)))
                {
                    AfterChange();
                }

                return;
            }
        }

        // A Problem belongs to one Problem and is on one beat: confirm the move.
        // A Problem that waits on no beat of this Problem needs no question.
        if (storyElement is ProblemModel problem && !_appState.Headless)
        {
            var existingParent = _outlineService.GetExistingBeatAssignment(problem);
            var otherParent = existingParent != null && existingParent.Uuid != _problemModel.Uuid;
            if (otherParent || otherBeat >= 0)
            {
                var res = await ShowDialog(new ContentDialog
                {
                    Title = "Already assigned!",
                    Content = otherParent
                        ? $"This problem is already assigned to a different structure ({existingParent.Name}) " +
                          $"Would you like to assign it here instead?"
                        : "This problem is already on another beat of this problem. " +
                          "Would you like to move it here?",
                    PrimaryButtonText = "Assign here",
                    SecondaryButtonText = "Cancel"
                });

                if (res != ContentDialogResult.Primary)
                    return;
            }
        }

        if (CallService(() => _outlineService.AssignElementToBeat(_storyModel, _problemModel, index, element)))
        {
            AfterChange();
        }
    }

    internal void UnbindElement()
    {
        if (SelectedBeat == null || SelectedBeat.Guid == Guid.Empty || _problemModel == null)
            return;

        // The element stays in the Problem, on no beat. The beat stays selected.
        var index = IndexOfSelectedBeat();
        if (CallService(() => _outlineService.UnasignBeat(_storyModel, _problemModel, index)))
        {
            CurrentElementDescription = null;
            AfterChange();
        }
    }

    private async Task AddSelectedElementAsync()
    {
        var item = ElementList.SelectedListElement;
        if (item == null)
        {
            WeakReferenceMessenger.Default.Send(
                new StatusChangedMessage(new StatusMessage("Select an element", LogLevel.Warn)));
            return;
        }

        await AddElementAsync(item.Element.Uuid);
    }

    /// <summary>
    ///     Gives an element to this Problem, on no beat.
    /// </summary>
    internal async Task AddElementAsync(Guid element)
    {
        if (_problemModel == null)
        {
            return;
        }

        if (_problemModel.StructureElements.Contains(element))
        {
            WeakReferenceMessenger.Default.Send(
                new StatusChangedMessage(new StatusMessage("The element is already in this Problem.", LogLevel.Warn)));
            return;
        }

        var storyElement = _storyModel.StoryElements.FirstOrDefault(g => g.Uuid == element);
        if (storyElement is ProblemModel problem && !_appState.Headless)
        {
            var existingParent = _outlineService.GetExistingBeatAssignment(problem);
            if (existingParent != null)
            {
                var res = await ShowDialog(new ContentDialog
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

        if (CallService(() => _outlineService.AddStructureElement(_storyModel, _problemModel, element)))
        {
            AfterChange();
        }
    }

    private async Task RemoveSelectedElementAsync()
    {
        var element = SelectedNoBeatElement?.Uuid ?? SelectedBeat?.Guid ?? Guid.Empty;
        if (element == Guid.Empty)
        {
            WeakReferenceMessenger.Default.Send(
                new StatusChangedMessage(new StatusMessage("Select an element of this Problem", LogLevel.Warn)));
            return;
        }

        await RemoveElementAsync(element);
    }

    /// <summary>
    ///     Takes an element out of this Problem, and off each of its beats.
    /// </summary>
    internal Task RemoveElementAsync(Guid element)
    {
        if (_problemModel != null &&
            CallService(() => _outlineService.RemoveStructureElement(_storyModel, _problemModel, element)))
        {
            CurrentElementDescription = null;
            AfterChange();
        }

        return Task.CompletedTask;
    }

    private void CreateBeat()
    {
        if (string.IsNullOrEmpty(StructureModelTitle))
        {
            WeakReferenceMessenger.Default.Send(
                new StatusChangedMessage(new StatusMessage("Select a beat sheet first", LogLevel.Warn)));
            return;
        }
        var newBeat = new StructureBeat("New Beat", "Describe your beat here");
        if (SelectedBeatIndex >= 0 && SelectedBeatIndex < StructureBeats.Count)
        {
            StructureBeats.Insert(SelectedBeatIndex + 1, newBeat);
            SelectedBeatIndex = SelectedBeatIndex + 1;
        }
        else
        {
            StructureBeats.Add(newBeat);
            SelectedBeatIndex = StructureBeats.Count - 1;
        }
        SelectedBeat = newBeat;
        WeakReferenceMessenger.Default.Send(
            new StatusChangedMessage(new StatusMessage("Beat added", LogLevel.Info)));
        AfterChange();
    }

    private async Task DeleteBeatAsync()
    {
        if (SelectedBeat == null)
            return;

        var index = IndexOfSelectedBeat();
        var held = SelectedBeat.Guid;
        var onOtherBeat = held != Guid.Empty &&
                          StructureBeats.Where((b, i) => i != index).Any(b => b.Guid == held);
        var keepElement = true;

        if (!_appState.Headless)
        {
            var beatTitle = SelectedBeat.Title ?? "this beat";
            if (held == Guid.Empty || onOtherBeat)
            {
                var result = await ShowDialog(new ContentDialog
                {
                    Title = "Delete Beat",
                    Content = $"Delete beat '{beatTitle}'?",
                    PrimaryButtonText = "Delete",
                    SecondaryButtonText = "Cancel"
                });

                if (result != ContentDialogResult.Primary)
                    return;
            }
            else
            {
                // The beat has an element that is on no other beat: keep it in the Problem, or remove it?
                var result = await ShowDialog(new ContentDialog
                {
                    Title = "Delete Beat",
                    Content = $"Delete beat '{beatTitle}'? It has the element \"{SelectedBeat.ElementName}\".",
                    PrimaryButtonText = "Keep in the Problem",
                    SecondaryButtonText = "Remove from the Problem",
                    CloseButtonText = "Cancel"
                });

                if (result == ContentDialogResult.None)
                    return;

                keepElement = result == ContentDialogResult.Primary;
            }
        }

        if (_problemModel == null)
        {
            return;
        }

        if (CallService(() => _outlineService.DeleteBeat(_storyModel, _problemModel, index, keepElement)))
        {
            WeakReferenceMessenger.Default.Send(
                new StatusChangedMessage(new StatusMessage("Beat deleted", LogLevel.Info)));
            AfterChange();
        }
    }

    private void MoveUp()
    {
        if (SelectedBeat == null)
            return;

        if (SelectedBeatIndex > 0)
        {
            StructureBeats.Move(SelectedBeatIndex, SelectedBeatIndex - 1);
            AfterChange();
        }
    }

    private void MoveDown()
    {
        if (SelectedBeat == null)
            return;

        if (SelectedBeatIndex < StructureBeats.Count - 1)
        {
            StructureBeats.Move(SelectedBeatIndex, SelectedBeatIndex + 1);
            AfterChange();
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

            ApplyLoadedBeatSheet(_outlineService.LoadBeatsheet(filePath.Path));
        }
        catch (Exception)
        {
            WeakReferenceMessenger.Default.Send(
                new StatusChangedMessage(new StatusMessage("Failed to Load Beatsheet", LogLevel.Error)));
        }
    }

    /// <summary>
    ///     Replaces the beats with the beats of a beat sheet file. The elements that were on
    ///     the beats stay in the Problem, on no beat.
    /// </summary>
    internal void ApplyLoadedBeatSheet(SavedBeatsheet saved)
    {
        ReplaceBeats("Custom Beat Sheet", saved.Description, saved.Beats);
    }

    private async Task ConfirmAndApplyBeatSheet(string value)
    {
        if (!_appState.Headless)
        {
            var result = await ShowDialog(new ContentDialog
            {
                Title = "Change the beat sheet?",
                Content = "The scenes and problems stay in this problem, on no beat. " +
                          "Changes to the beats are lost.",
                PrimaryButtonText = "Confirm",
                SecondaryButtonText = "Cancel"
            });

            if (result != ContentDialogResult.Primary)
            {
                // Revert ComboBox without re-triggering setter logic
                _isLoading = true;
                SetProperty(ref _structureModelTitle, _structureModelTitle);
                _isLoading = false;
                return;
            }
        }

        ApplyBeatSheet(value);
    }

    private void ApplyBeatSheet(string value)
    {
        if (BeatSheets.ContainsKey(value))
        {
            var beatSheet = BeatSheets[value];
            ReplaceBeats(value, beatSheet.PlotPatternNotes,
                beatSheet.PlotPatternScenes.Select(item => new StructureBeat(item.SceneTitle, item.Notes)));
            return;
        }

        // A name that is not a beat sheet of Tools.json, such as "Custom Beat Sheet": only the name changes.
        SetProperty(ref _structureModelTitle, value, nameof(StructureModelTitle));
        if (_problemModel != null)
        {
            _problemModel.StructureTitle = value;
        }

        AfterChange();
    }

    /// <summary>
    ///     Gives the Problem a new set of beats through OutlineService.SetBeatSheet.
    /// </summary>
    private void ReplaceBeats(string title, string description, IEnumerable<StructureBeat> beats)
    {
        var newBeats = new ObservableCollection<StructureBeat>(beats);
        if (_problemModel == null)
        {
            // Before LoadBeats there is no Problem: keep the beats in this ViewModel only.
            StructureBeats.Clear();
            foreach (var beat in newBeats)
            {
                StructureBeats.Add(beat);
            }
        }
        else
        {
            if (!CallService(() =>
                    _outlineService.SetBeatSheet(_storyModel, _problemModel, description, title, newBeats)))
            {
                return;
            }

            if (!ReferenceEquals(StructureBeats, _problemModel.StructureBeats))
            {
                StructureBeats = _problemModel.StructureBeats;
            }
        }

        _isLoading = true;
        SetProperty(ref _structureModelTitle, title, nameof(StructureModelTitle));
        StructureDescription = description;
        _isLoading = false;

        SelectedBeat = null;
        SelectedBeatIndex = -1;
        AfterChange();
    }

    #endregion
}
