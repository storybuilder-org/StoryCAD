using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace StoryCAD.Models;

/// <summary>
///     StoryElementCollection is an ObservableCollection of StoryElement
///     instances, which automatically maintains several derivative
///     collections when StoryElementCollection has elements added or
///     removed.
/// </summary>
public class StoryElementCollection : ObservableCollection<StoryElement>
{
    public ObservableCollection<StoryElement> Characters;

    public ObservableCollection<StoryElement> Scenes;
    public ObservableCollection<StoryElement> Settings;
    public Dictionary<Guid, StoryElement> StoryElementGuids;

    public StoryElementCollection()
    {
        base.CollectionChanged += OnStoryElementsChanged;
        StoryElementGuids = new Dictionary<Guid, StoryElement>();
        Characters = new ObservableCollection<StoryElement>();
        Settings = new ObservableCollection<StoryElement>();
        Scenes = new ObservableCollection<StoryElement>();
        Problems!.Add(new StoryElement { ElementType = StoryItemType.Problem, Name = "(none)" });
        Characters!.Add(new StoryElement { ElementType = StoryItemType.Character, Name = "(none)" });
        Settings!.Add(new StoryElement { ElementType = StoryItemType.Setting, Name = "(none)" });
        Scenes!.Add(new StoryElement { ElementType = StoryItemType.Scene, Name = "(none)" });
    }

    public ObservableCollection<StoryElement> Problems { get; } = new();

    /// <summary>
    ///     The CollectionChanged event updates StoryElementGuids, Characters,
    ///     Settings, and Problems whenever adds, deletes, or resets of the
    ///     StoryElementCollection itself occurs.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnStoryElementsChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        StoryElement _element;

        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                //TODO: Assert that NewItems count is always 1, or make this a loop
                _element = (StoryElement)e.NewItems![0];
                StoryElementGuids.Add(_element!.Uuid, _element);
                // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
                switch (_element.ElementType)
                {
                    case StoryItemType.Character:
                        Characters.Add(_element);
                        break;
                    case StoryItemType.Setting:
                        Settings.Add(_element);
                        break;
                    case StoryItemType.Problem:
                        Problems.Add(_element);
                        break;
                    case StoryItemType.Scene:
                        Scenes.Add(_element);
                        break;
                }

                break;

            case NotifyCollectionChangedAction.Move:
                break;

            case NotifyCollectionChangedAction.Remove:
                //TODO: Assert that OldItems count is always 1, or make this a loop
                //TODO: Maybe replace the index of with just remove
                _element = (StoryElement)e.OldItems![0];
                StoryElementGuids.Remove(_element!.Uuid);
                int _i;
                // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
                switch (_element.ElementType)
                {
                    case StoryItemType.Character:
                        _i = Characters.IndexOf(_element);
                        Characters.RemoveAt(_i);
                        break;
                    case StoryItemType.Setting:
                        _i = Settings.IndexOf(_element);
                        Settings.RemoveAt(_i);
                        break;
                    case StoryItemType.Problem:
                        _i = Problems.IndexOf(_element);
                        Problems.RemoveAt(_i);
                        break;
                }

                break;

            case NotifyCollectionChangedAction.Replace:
                break;

            case NotifyCollectionChangedAction.Reset:
                break;

            default:
                throw new NotImplementedException();
        }
    }
}
