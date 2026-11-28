using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using StoryCADLib.Services.Messages;
using StoryCADLib.Services.Outline;
using StoryCADLib.ViewModels.SubViewModels;

namespace StoryCADLib.ViewModels;

/// <summary>
///     One line of the beat tree window: it joins the place of a child Problem in the beat column
///     of its parent to the beat column of the child. BeatIndex is -1 for a child on no beat.
/// </summary>
public record BeatTreeConnector(Guid ParentGuid, Guid ChildGuid, int BeatIndex);

/// <summary>
///     The view model of the beat tree window (issue #1546): one beat column for the Story Problem
///     and one for each Problem below it. Each beat column is a BeatSheetsViewModel, the class that
///     the Structure tab of the Problem form uses. BeatTreeWindowService makes this object;
///     it is not registered in IoC.
/// </summary>
[Microsoft.UI.Xaml.Data.Bindable]
public class BeatTreeViewModel : ObservableRecipient
{
    private readonly OutlineService _outlineService;
    private StoryModel _storyModel;

    public BeatTreeViewModel()
    {
        _outlineService = Ioc.Default.GetRequiredService<OutlineService>();
        ElementList = new StructureElementListViewModel();
        Columns = new ObservableCollection<BeatSheetsViewModel>();

        WeakReferenceMessenger.Default.Register<BeatTreeViewModel, StructureChangedMessage>(
            this, static (r, _) => r.Rebuild());
    }

    /// <summary>The beat columns: the Story Problem first, then each Problem after the Problem that holds it.</summary>
    public ObservableCollection<BeatSheetsViewModel> Columns { get; }

    /// <summary>The list of Scenes and Problems. All beat columns use this one list.</summary>
    public StructureElementListViewModel ElementList { get; }

    private BeatSheetsViewModel _activeColumn;

    /// <summary>The beat column that has the selection. The buttons of the window act on it.</summary>
    public BeatSheetsViewModel ActiveColumn
    {
        get => _activeColumn;
        set => SetProperty(ref _activeColumn, value);
    }

    private IReadOnlyList<BeatTreeConnector> _connectors = new List<BeatTreeConnector>();

    public IReadOnlyList<BeatTreeConnector> Connectors
    {
        get => _connectors;
        private set => SetProperty(ref _connectors, value);
    }

    private bool _showBeatDescriptions;

    public bool ShowBeatDescriptions
    {
        get => _showBeatDescriptions;
        set => SetProperty(ref _showBeatDescriptions, value);
    }

    private bool _showElementDescriptions;

    /// <summary>True shows the description of the Scene or Problem on each card and in each list "On no beat".</summary>
    public bool ShowElementDescriptions
    {
        get => _showElementDescriptions;
        set => SetProperty(ref _showElementDescriptions, value);
    }

    private string _emptyText = string.Empty;

    /// <summary>The text for an outline with no Story Problem. If not, an empty string.</summary>
    public string EmptyText
    {
        get => _emptyText;
        private set => SetProperty(ref _emptyText, value);
    }

    private Func<XamlRoot> _dialogRoot;

    /// <summary>Gives the XamlRoot of the beat tree window. Each beat column shows its dialogs there.</summary>
    public Func<XamlRoot> DialogRoot
    {
        get => _dialogRoot;
        set
        {
            _dialogRoot = value;
            foreach (var column in Columns)
            {
                column.DialogRoot = value;
            }
        }
    }

    /// <summary>
    ///     Reads the outline: the element list, then the beat columns and the connectors.
    ///     A beat column from an earlier call is kept, so that it keeps its selection.
    /// </summary>
    public void Load(StoryModel model)
    {
        _storyModel = model;
        ElementList.Load(model);
        Rebuild();
    }

    /// <summary>
    ///     Makes the beat columns and the connectors again. Runs after each change to the structure.
    /// </summary>
    private void Rebuild()
    {
        if (_storyModel == null)
        {
            return;
        }

        var story = FindStoryProblem();
        if (story == null)
        {
            Columns.Clear();
            Connectors = new List<BeatTreeConnector>();
            ActiveColumn = null;
            EmptyText = "Set the Story Problem on the Story Overview form to see the beat tree.";
            return;
        }

        EmptyText = string.Empty;
        var existing = Columns.ToDictionary(c => c.ProblemGuid);
        var ordered = new List<BeatSheetsViewModel>();
        var connectors = new List<BeatTreeConnector>();
        // The set makes sure that an outline with a loop gets each Problem one time.
        var seen = new HashSet<Guid>();
        AddColumn(story, existing, ordered, connectors, seen);

        if (!ordered.SequenceEqual(Columns))
        {
            Columns.Clear();
            foreach (var column in ordered)
            {
                Columns.Add(column);
            }
        }

        Connectors = connectors;
        if (ActiveColumn == null || !ordered.Contains(ActiveColumn))
        {
            ActiveColumn = ordered[0];
        }
    }

    private void AddColumn(ProblemModel problem, Dictionary<Guid, BeatSheetsViewModel> existing,
        List<BeatSheetsViewModel> ordered, List<BeatTreeConnector> connectors, HashSet<Guid> seen)
    {
        seen.Add(problem.Uuid);
        if (existing.TryGetValue(problem.Uuid, out var column))
        {
            column.Refresh();
        }
        else
        {
            column = new BeatSheetsViewModel(ShellViewModel.ShowChange, ElementList) { DialogRoot = _dialogRoot };
            column.LoadBeats(problem, _storyModel);
        }

        ordered.Add(column);

        // The Problems on the beats, in the sequence of the beats, then the Problems on no beat.
        var children = new List<(ProblemModel Child, int BeatIndex)>();
        for (var i = 0; i < problem.StructureBeats.Count; i++)
        {
            if (FindProblem(problem.StructureBeats[i].Guid) is { } onBeat)
            {
                children.Add((onBeat, i));
            }
        }

        foreach (var guid in _outlineService.GetElementsOnNoBeat(problem))
        {
            if (FindProblem(guid) is { } onNoBeat)
            {
                children.Add((onNoBeat, -1));
            }
        }

        foreach (var (child, beatIndex) in children)
        {
            if (seen.Contains(child.Uuid))
            {
                continue;
            }

            connectors.Add(new BeatTreeConnector(problem.Uuid, child.Uuid, beatIndex));
            AddColumn(child, existing, ordered, connectors, seen);
        }
    }

    private ProblemModel FindStoryProblem()
    {
        var overview = _storyModel.StoryElements.OfType<OverviewModel>().FirstOrDefault();
        return overview == null ? null : FindProblem(overview.StoryProblem);
    }

    /// <summary>The Problem with a given GUID, or null when the GUID is empty, is not a Problem, or is in the trash.</summary>
    private ProblemModel FindProblem(Guid guid)
    {
        return guid != Guid.Empty &&
               _storyModel.StoryElements.StoryElementGuids.TryGetValue(guid, out var element) &&
               element is ProblemModel problem && !_outlineService.IsInTrash(problem)
            ? problem
            : null;
    }
}
