using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace StoryBuilder.Models
{
    /// <summary>
    /// StoryElementCollection is an ObservableCollection of StoryElement
    /// instances, which automatically maintains several derivative 
    /// collections when StoryElementCollection has elements added or
    /// removed.
    /// </summary>
    public class StoryElementCollection : ObservableCollection<StoryElement>
    {
        public Dictionary<Guid, StoryElement> StoryElementGuids;
        public ObservableCollection<StoryElement> Characters;
        public ObservableCollection<StoryElement> Settings;
        public ObservableCollection<StoryElement> Problems;

        public StoryElementCollection()
        {
            base.CollectionChanged += OnStoryElementsChanged;
            StoryElementGuids = new Dictionary<Guid, StoryElement>();
            Characters = new ObservableCollection<StoryElement>();
            Settings = new ObservableCollection<StoryElement>();
            Problems = new ObservableCollection<StoryElement>();
        }

        /// <summary>
        /// The CollectionChanged event updates StoryElementGuids, Characters,
        /// Settings, and Problems whenever adds, deletes, or resets of the
        /// StoryElementCollection itself occurs.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnStoryElementsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            StoryElement element;

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    //TODO: Assert that NewItems count is always 1, or make this a loop
                    element = (StoryElement)e.NewItems[0];
                    StoryElementGuids.Add(element.Uuid, element);
                    switch (element.Type)
                    {
                        case StoryItemType.Character:
                            Characters.Add(element);
                            break;
                        case StoryItemType.Setting:
                            Settings.Add(element);
                            break;
                        case StoryItemType.Problem:
                            Problems.Add(element);
                            break;
                    }
                    break;

                case NotifyCollectionChangedAction.Move:
                    break;

                case NotifyCollectionChangedAction.Remove:
                    //TODO: Assert that OldItems count is always 1, or make this a loop
                    element = (StoryElement)e.OldItems[0];
                    StoryElementGuids.Remove(element.Uuid);
                    int i;
                    switch (element.Type)
                    {
                        case StoryItemType.Character:
                            i = Characters.IndexOf(element);
                            Characters.RemoveAt(i);
                            break;
                        case StoryItemType.Setting:
                            i = Settings.IndexOf(element);
                            Settings.RemoveAt(i);
                            break;
                        case StoryItemType.Problem:
                            i = Problems.IndexOf(element);
                            Problems.RemoveAt(i);
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
}
