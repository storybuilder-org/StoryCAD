using System.Collections.ObjectModel;
using System.ComponentModel;
using StoryCADLib.Services;
using StoryCADLib.Services.Outline;
using LogLevel = StoryCADLib.Services.Logging.LogLevel;

namespace StoryCADLib.ViewModels;

/// <summary>
///     StoryNodeItem is the equivalent of a ViewModel for a single TreeViewItem in the
///     NavigationTree displayed on the Shell Page. Each TreeViewItem is the visual representation
///     of one node in a TreeView, and each TreeViewItem binds to a StoryNodeItem through a
///     TreeViewItemDataTemplate contained in Shell. The NavigationTree's TreeViewItems are bound to
///     an ObservableCollection of StoryNodeItems contained in the ShellViewModel, which is the
///     Shell DataContext.
///     Like any other ViewModel, a StoryNodeItem is an intermediary between the data model
///     (in this case, StoryModel) and the storyView (in this case, Shell). The model is composed of
///     instances of StoryElements such as ProblemModel, CharacterModel, etc. These model
///     components are not visual, and have no Children, IsSelected or IsExpanded, and so on.
///     A StoryElement instance is displayed and modified on a Page such as ProblemPage or
///     CharacterPage which is contained in Shell's SplitView.Content frame. In order to do so,
///     it needs a TreeViewItem on the tree which binds to a StoryNodeItem, which is the visual
///     representation of the StoryElement.
///     StoryCAD's data model is called StoryModel. StoryModel  contains two ObservableCollection
///     lists of StoryNodeItems (and their counterpart StoryElements), a StoryExplorer collection which
///     contains all Story Elements (the StoryOverview and all Problem, Character, Setting, Scene
///     and Folder elements and their TreeView nodes, and a NarratorView storyView which contains just
///     Section (chapter, act, etc.) and Scene elements and which represents the story as it's being narrated.
///     In the Shell, the user can switch between the two views by loading one or the other ObservableCollection.
///     NavigationTree will thus point to one or the other of the collections as its bindings.
/// </summary>
public class StoryNodeItem : INotifyPropertyChanged
{
    private readonly AppState _appState;
    private readonly ILogService _logger;

    // is it INavigable?
    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>
    ///     Deletes a node from a view. Behavior depends on the view type:
    ///     - ExplorerView: Moves element to trash using OutlineService.MoveToTrash()
    ///     - NarratorView: Just removes from narrative view (element remains in ExplorerView)
    /// </summary>
    /// <param name="view">The view type from which to delete</param>
    /// <returns>True if delete was successful, false if element cannot be deleted</returns>
    public bool Delete(StoryViewType view)
    {
        if (Type is StoryItemType.TrashCan || IsRoot)
        {
            return false;
        }

        if (view == StoryViewType.ExplorerView)
        {
            // For Explorer view: delegate to OutlineService for proper trash handling
            // This ensures reference cleanup, locking, and change tracking
            var element = _appState.CurrentDocument!.Model.StoryElements.StoryElementGuids[Uuid];
            var outlineService = Ioc.Default.GetRequiredService<OutlineService>();
            outlineService.MoveToTrash(element, _appState.CurrentDocument.Model);
        }
        else if (view == StoryViewType.NarratorView)
        {
            // For Narrator view: just remove from this logical view
            // The node remains in its original location in Explorer view
            RemoveFromNarratorView();
        }

        return true;
    }

    /// <summary>
    ///     Removes this node from the Narrator view without trashing it.
    ///     The element remains in its original location in Explorer view.
    /// </summary>
    private void RemoveFromNarratorView()
    {
        if (IsRoot || Parent is null)
        {
            throw new InvalidOperationException("Root cannot be detached.");
        }

        if (!Parent.Children.Remove(this))
        {
            throw new InvalidOperationException("Parent/child link out of sync.");
        }

        // No parent reassignment needed - this just removes from the NarratorView tree
        // The element still exists in ExplorerView
    }

    /// <summary>
    ///     Copies this node to the Narrator view. Only scenes can be copied.
    ///     If the scene already exists in the narrator view, no action is taken.
    /// </summary>
    /// <param name="model">The story model containing the narrator view</param>
    /// <returns>True if copied, false if already exists or node is not a scene</returns>
    public bool CopyToNarratorView(StoryModel model)
    {
        if (Type != StoryItemType.Scene)
        {
            _logger?.Log(LogLevel.Warn, $"Cannot copy non-scene node '{Name}' to narrator view");
            return false;
        }

        if (IsInNarratorView(model))
        {
            _logger?.Log(LogLevel.Info, $"Scene '{Name}' already exists in narrator view");
            return false;
        }

        var outlineService = Ioc.Default.GetRequiredService<OutlineService>();
        var sceneElement = (SceneModel)outlineService.GetStoryElementByGuid(model, Uuid);
        _ = new StoryNodeItem(sceneElement, model.NarratorView[0]);

        _logger?.Log(LogLevel.Info, $"Copied scene '{Name}' to narrator view");
        return true;
    }

    /// <summary>
    ///     Checks if this node exists in the narrator view by searching for its UUID.
    /// </summary>
    /// <param name="model">The story model containing the narrator view</param>
    /// <returns>True if this node exists in narrator view</returns>
    public bool IsInNarratorView(StoryModel model)
    {
        if (model?.NarratorView == null || model.NarratorView.Count == 0)
        {
            return false;
        }

        return GetAllNodesFlat(model.NarratorView[0]).Any(node => node.Uuid == Uuid);
    }

    /// <summary>
    ///     Gets all scene nodes in this subtree (recursive depth-first search).
    /// </summary>
    /// <returns>List of all scene nodes under this node (including this node if it's a scene)</returns>
    public List<StoryNodeItem> GetAllScenes()
    {
        var scenes = new List<StoryNodeItem>();

        if (Type == StoryItemType.Scene)
        {
            scenes.Add(this);
        }

        foreach (var child in Children)
        {
            scenes.AddRange(child.GetAllScenes());
        }

        return scenes;
    }

    /// <summary>
    ///     Copies all scene children of this node to the narrator view.
    ///     Only copies scenes that don't already exist in the narrator view.
    /// </summary>
    /// <param name="model">The story model</param>
    /// <returns>Number of scenes copied</returns>
    public int CopyAllScenesToNarratorView(StoryModel model)
    {
        var copied = 0;
        var scenes = GetAllScenes();

        foreach (var scene in scenes)
        {
            if (scene.CopyToNarratorView(model))
            {
                copied++;
            }
        }

        _logger?.Log(LogLevel.Info, $"Copied {copied} scenes from '{Name}' to narrator view");
        return copied;
    }

    /// <summary>
    ///     Flattens a tree structure into a list (depth-first traversal).
    ///     Helper method for searching through node hierarchies.
    /// </summary>
    /// <param name="root">The root node to start from</param>
    /// <returns>Flattened list of all nodes in the subtree</returns>
    private static List<StoryNodeItem> GetAllNodesFlat(StoryNodeItem root)
    {
        var nodes = new List<StoryNodeItem> { root };

        foreach (var child in root.Children)
        {
            nodes.AddRange(GetAllNodesFlat(child));
        }

        return nodes;
    }


    private void NotifyPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }


    /// <summary>
    ///     Search up the StoryNodeItem tree to its
    ///     root from a specified node and return its StoryItemType.
    ///     This allows code to determine which TreeView a StoryNodeItem is in.
    /// </summary>
    /// <param name="startNode">The node to begin searching from</param>
    /// <returns>The StoryItemType of the root node</returns>
    public static StoryItemType RootNodeType(StoryNodeItem startNode)
    {
        if (startNode == null)
        {
            Ioc.Default.GetRequiredService<ILogService>().LogException(
                LogLevel.Error, new ArgumentNullException(nameof(startNode)),
                "RootNodeType called with null startNode parameter");
            return StoryItemType.Unknown;
        }

        try
        {
            var node = startNode;
            var maxIterations = 1000; // Prevent infinite loops
            var iterations = 0;

            while (!node.IsRoot)
            {
                if (node.Parent == null)
                {
                    Ioc.Default.GetRequiredService<ILogService>().LogException(
                        LogLevel.Error, new InvalidOperationException("Broken parent chain"),
                        $"Node '{node.Name}' (Type: {node.Type}) is not root but has no parent");
                    return StoryItemType.Unknown;
                }

                if (++iterations > maxIterations)
                {
                    Ioc.Default.GetRequiredService<ILogService>().LogException(
                        LogLevel.Error, new InvalidOperationException("Infinite loop detected"),
                        $"RootNodeType exceeded maximum iterations traversing from node '{startNode.Name}'");
                    return StoryItemType.Unknown;
                }

                node = node.Parent;
            }

            return node.Type;
        }
        catch (Exception ex)
        {
            Ioc.Default.GetRequiredService<ILogService>().LogException(
                LogLevel.Error, ex, $"Root node type exception, this shouldn't happen {ex.Message}");
            return StoryItemType.Unknown;
        }
    }

    // StoryElement data

    #region Properties

    private Guid _uuid;

    public Guid Uuid
    {
        get => _uuid;
        set
        {
            if (_uuid != value)
            {
                _uuid = value;
                NotifyPropertyChanged("Uuid");
            }
        }
    }

    private string _name;

    public string Name
    {
        get => _name;
        set
        {
            if (_name != value)
            {
                _name = value;
                NotifyPropertyChanged("Name");
            }
        }
    }

    private StoryItemType _type;

    /// <summary>
    ///     Type of node
    /// </summary>
    public StoryItemType Type
    {
        get => _type;
        set
        {
            if (_type != value)
            {
                _type = value;
                NotifyPropertyChanged("Type");
            }
        }
    }

    // TreeViewItem data

    private Symbol _symbol;

    /// <summary>
    ///     Icon for this node, dependent on node type.
    /// </summary>
    public Symbol Symbol
    {
        get => _symbol;
        set
        {
            if (_symbol != value)
            {
                _symbol = value;
                NotifyPropertyChanged("Symbol");
            }
        }
    }

    private StoryNodeItem _parent;

    /// <summary>
    ///     Parent node of this node
    /// </summary>
    public StoryNodeItem Parent
    {
        get => _parent;
        set
        {
            if (_parent != value)
            {
                _parent = value;
                NotifyPropertyChanged("Parent");
            }
        }
    }

    private ObservableCollection<StoryNodeItem> _children;

    /// <summary>
    ///     Child nodes of this node
    /// </summary>
    public ObservableCollection<StoryNodeItem> Children
    {
        get
        {
            if (_children == null)
            {
                _children = new ObservableCollection<StoryNodeItem>();
            }

            return _children;
        }
        set
        {
            if (_children != value)
            {
                _children = value;
                NotifyPropertyChanged("Children");
            }
        }
    }

    private bool _isExpanded;

    /// <summary>
    ///     Is this node expanded
    /// </summary>
    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded != value)
            {
                _isExpanded = value;
                NotifyPropertyChanged("IsExpanded");
            }
        }
    }

    /// <summary>
    ///     Binds to background color
    /// </summary>
    private SolidColorBrush _background;

    public SolidColorBrush Background
    {
        get => _background;
        set
        {
            if (_background != value)
            {
                _background = value;
                NotifyPropertyChanged("Background");
            }
        }
    }

    /// <summary>
    ///     Binds to boarder color.
    /// </summary>
    private SolidColorBrush _boarderBrush;

    public SolidColorBrush boarderBrush
    {
        get => _boarderBrush;
        set
        {
            if (_boarderBrush != value)
            {
                _boarderBrush = value;
                NotifyPropertyChanged("Background");
            }
        }
    }

    private bool _isSelected;

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                NotifyPropertyChanged("IsSelected");
            }
        }
    }

    public TextWrapping TextWrapping => Ioc.Default.GetRequiredService<PreferenceService>().Model.WrapNodeNames;

    private bool _isRoot;

    /// <summary>
    ///     Denotes if this note is root
    ///     There should only be three root nodes
    ///     Narrative Root
    ///     Overview node
    ///     Trash node
    /// </summary>
    public bool IsRoot
    {
        get => _isRoot;
        set
        {
            if (_isRoot != value)
            {
                _isRoot = value;
                NotifyPropertyChanged("IsRoot");
            }
        }
    }

    #endregion

    #region Public Methods

    public override string ToString()
    {
        return Name;
    }


    /// <summary>
    ///     This method allows a dept-first search (DFS) or 'pre-order traversal' of
    ///     a BinderItem tree or subtree with a simple C# foreach.
    ///     In a DFS, you visit the root first and then search deeper into the tree
    ///     visiting each node and then the node’s children.
    ///     To use the enumerator, you code a foreach loop anywhere in your program:
    ///     foreach (TreeNode node in root)
    ///     {
    ///     //perform action on node in DFS order here
    ///     }
    ///     ref: http://www.timlabonne.com/2013/07/performing-a-dfs-over-a-rooted-tree-with-a-c-foreach-loop/
    /// </summary>
    /// <returns>BinderItem for current node, then for each child in a loop</returns>
    public IEnumerator<StoryNodeItem> GetEnumerator()
    {
        yield return this;
        foreach (var _child in Children)
        {
            // ReSharper disable once GenericEnumeratorNotDisposed
            var _childEnumerator = _child.GetEnumerator();
            while (_childEnumerator.MoveNext())
            {
                yield return _childEnumerator.Current;
            }
        }
    }

    #endregion

    #region Constructors

    public StoryNodeItem(ILogService logger, AppState appstate, StoryElement node, StoryNodeItem parent,
        StoryItemType type = StoryItemType.Unknown)
    {
        _logger = logger;
        _appState = appstate;
        Uuid = node.Uuid;
        Name = node.Name;
        if (type == StoryItemType.Unknown)
        {
            Type = node.ElementType;
        }
        else
        {
            Type = type;
        }

        switch (_type)
        {
            case StoryItemType.StoryOverview:
                Symbol = Symbol.View;
                break;
            case StoryItemType.Character:
                Symbol = Symbol.Contact;
                break;
            case StoryItemType.Scene:
                Symbol = Symbol.AllApps;
                break;
            case StoryItemType.Problem:
                Symbol = Symbol.Help;
                break;
            case StoryItemType.Setting:
                Symbol = Symbol.Globe;
                break;
            case StoryItemType.Folder:
                Symbol = Symbol.Folder;
                break;
            case StoryItemType.Section:
                Symbol = Symbol.Folder;
                break;
            case StoryItemType.Web:
                Symbol = Symbol.PreviewLink;
                break;
            case StoryItemType.TrashCan:
                Symbol = Symbol.Delete;
                break;
            case StoryItemType.Notes:
                Symbol = Symbol.TwoPage;
                break;
        }

        Parent = parent;
        Children = new ObservableCollection<StoryNodeItem>();

        IsExpanded = false;
        IsRoot = false;

        //If there's no parent this is a root node.
        if (Parent == null)
        {
            IsRoot = true;
            return;
        }

        Parent.Children.Add(this);
    }

    // Overloaded constructor without logger
    //public StoryNodeItem(StoryElement node, StoryNodeItem parent,
    //    StoryItemType type = StoryItemType.Unknown) : this(Ioc.Default.GetRequiredService<ILogService>(), node, parent, type)
    public StoryNodeItem(StoryElement node, StoryNodeItem parent, StoryItemType type = StoryItemType.Unknown)
    {
        _logger = Ioc.Default.GetRequiredService<ILogService>();
        _appState = Ioc.Default.GetRequiredService<AppState>();
        Uuid = node.Uuid;
        Name = node.Name;
        if (type == StoryItemType.Unknown)
        {
            Type = node.ElementType;
        }
        else
        {
            Type = type;
        }

        switch (_type)
        {
            case StoryItemType.StoryOverview:
                Symbol = Symbol.View;
                break;
            case StoryItemType.Character:
                Symbol = Symbol.Contact;
                break;
            case StoryItemType.Scene:
                Symbol = Symbol.AllApps;
                break;
            case StoryItemType.Problem:
                Symbol = Symbol.Help;
                break;
            case StoryItemType.Setting:
                Symbol = Symbol.Globe;
                break;
            case StoryItemType.Folder:
                Symbol = Symbol.Folder;
                break;
            case StoryItemType.Section:
                Symbol = Symbol.Folder;
                break;
            case StoryItemType.Web:
                Symbol = Symbol.PreviewLink;
                break;
            case StoryItemType.TrashCan:
                Symbol = Symbol.Delete;
                break;
            case StoryItemType.Notes:
                Symbol = Symbol.TwoPage;
                break;
        }

        Parent = parent;
        Children = new ObservableCollection<StoryNodeItem>();

        IsExpanded = false;
        IsRoot = false;

        //If there's no parent this is a root node.
        if (Parent == null)
        {
            IsRoot = true;
            return;
        }

        Parent.Children.Add(this);
    }

    #endregion
}
