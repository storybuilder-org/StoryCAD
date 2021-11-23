using StoryBuilder.ViewModels;
using System.Collections.ObjectModel;

//using StoryBuilder.Models.Tools;

namespace StoryBuilder.Models
{
    public class StoryModel
    {
        // TODO: Note sorting filtering and grouping depend on ICollectionView (for TreeView?)
        // TODO: See http://msdn.microsoft.com/en-us/library/ms752347.aspx#binding_to_collections
        // TODO: Maybe http://stackoverflow.com/questions/15593166/binding-treeview-with-a-observablecollection

        /// <summary>
        /// If any of the story entities have been modified by other than retrieval from the Story
        /// (that is, by a user modification from a View) 'Changed' is set to true. That is, a change
        /// to OverViewModel, or any ProblemModel, CharacterModel, SettingModel, PlotPointModel, or
        /// FolderModel, or adding a new node, will result in Changed being set to true. 
        /// 
        /// This amounts to a 'dirty' bit that indicates the StoryModel needs to be written to its backing store. 
        /// </summary>
        public bool Changed;

        #region StoryExplorer and NarratorView (TreeView) properties

        /// A StoryModel is a collection of StoryElements (an overview, problems, characters, settings,
        /// and scenes, plus containers).
        /// 
        public StoryElementCollection StoryElements;

        /// StoryModel also contains two persisted TreeView representations, a Story Explorer tree which
        /// contains all Story Elements (the StoryOverview and all Problem, Character, Setting, PlotPoint
        /// and Folder elements) and a Narrator View which contains just Section (chapter, etc) and
        /// selected PlotPoint elements. 
        /// 
        /// One of these persisted TreeViews is actively bound in the Shell page view to a StoryNodeItem tree 
        /// based on  whichever of these two TreeView representations is presently selected.
        ///
        public ObservableCollection<StoryNodeItem> ExplorerView = new ObservableCollection<StoryNodeItem>();
        public ObservableCollection<StoryNodeItem> NarratorView = new ObservableCollection<StoryNodeItem>();
   
        #endregion

        #region Constructor
        public StoryModel()
        {
            StoryElements = new StoryElementCollection();

            ExplorerView = new ObservableCollection<StoryNodeItem>();
            NarratorView = new ObservableCollection<StoryNodeItem>();

            Changed = false;
        }
        #endregion
    }
}