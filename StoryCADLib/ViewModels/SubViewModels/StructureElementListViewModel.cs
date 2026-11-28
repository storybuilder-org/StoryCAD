using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using StoryCADLib.Services.Messages;
using StoryCADLib.Services.Outline;
using StoryCADLib.ViewModels.Tools;

namespace StoryCADLib.ViewModels.SubViewModels;

/// <summary>
///     The list of all Scenes or all Problems of the outline, with the use of each one (issue #1546).
///     The Structure tab has one. The beat tree window has one for all its beat columns.
///     Created by its owner, not registered in IoC.
/// </summary>
[Microsoft.UI.Xaml.Data.Bindable]
public class StructureElementListViewModel : ObservableObject
{
    private readonly OutlineService _outlineService;
    private StoryModel _storyModel;

    public StructureElementListViewModel()
    {
        _outlineService = Ioc.Default.GetRequiredService<OutlineService>();
        _scenes = new ObservableCollection<StructureElementItem>();
        _problems = new ObservableCollection<StructureElementItem>();
        SelectedElementSource = "Scene";

        WeakReferenceMessenger.Default.Register<StructureElementListViewModel, StructureChangedMessage>(
            this, static (r, _) => r.RefreshUse());
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

    private ObservableCollection<StructureElementItem> _scenes;

    public ObservableCollection<StructureElementItem> Scenes
    {
        get => _scenes;
        set => SetProperty(ref _scenes, value);
    }

    private ObservableCollection<StructureElementItem> _problems;

    public ObservableCollection<StructureElementItem> Problems
    {
        get => _problems;
        set => SetProperty(ref _problems, value);
    }

    private ObservableCollection<StructureElementItem> _currentElementSource;

    public ObservableCollection<StructureElementItem> CurrentElementSource
    {
        get => _currentElementSource;
        set => SetProperty(ref _currentElementSource, value);
    }

    private StructureElementItem _selectedListElement;

    public StructureElementItem SelectedListElement
    {
        get => _selectedListElement;
        set => SetProperty(ref _selectedListElement, value);
    }

    /// <summary>
    ///     Makes the two lists again from the model. The model lists start with a "(none)"
    ///     placeholder (Guid.Empty) for drop-downs; it is not an element to assign.
    ///     An element in the trash is not in the lists.
    /// </summary>
    public void Load(StoryModel model)
    {
        _storyModel = model;
        var selected = SelectedListElement?.Element.Uuid ?? Guid.Empty;

        Scenes = Build(model.StoryElements.Scenes);
        Problems = Build(model.StoryElements.Problems);
        CurrentElementSource = SelectedElementSource == "Scene" ? Scenes : Problems;
        SelectedListElement = selected == Guid.Empty
            ? null
            : CurrentElementSource.FirstOrDefault(i => i.Element.Uuid == selected);
    }

    /// <summary>
    ///     Reads the use of each item again, after a change to the structure.
    /// </summary>
    public void RefreshUse()
    {
        if (_storyModel == null)
        {
            return;
        }

        foreach (var item in Scenes.Concat(Problems))
        {
            item.Use = _outlineService.GetStructureUse(_storyModel, item.Element.Uuid);
        }
    }

    private ObservableCollection<StructureElementItem> Build(IEnumerable<StoryElement> elements)
    {
        return new ObservableCollection<StructureElementItem>(elements
            .Where(e => e.Uuid != Guid.Empty && !_outlineService.IsInTrash(e))
            .Select(e => new StructureElementItem(e, _outlineService.GetStructureUse(_storyModel, e.Uuid))));
    }
}
