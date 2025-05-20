using System.Collections.ObjectModel;
using System.ComponentModel;
using Windows.Data.Xml.Dom;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using StoryCAD.Services;
using StoryCAD.Services.Outline;
using StoryCAD.ViewModels.SubViewModels;

namespace StoryCAD.ViewModels;

/// <summary> 
/// StoryNodeItem is the equivalent of a ViewModel for a single TreeViewItem in the 
/// NavigationTree displayed on the Shell Page. Each TreeViewItem is the visual representation
/// of one node in a TreeView, and each TreeViewItem binds to a StoryNodeItem through a
/// TreeViewItemDataTemplate contained in Shell. The NavigationTree's TreeViewItems are bound to
/// an ObservableCollection of StoryNodeItems contained in the ShellViewModel, which is the
/// Shell DataContext.
/// 
/// Like any other ViewModel, a StoryNodeItem is an intermediary between the data model 
/// (in this case, StoryModel) and the storyView (in this case, Shell). The model is composed of
/// instances of StoryElements such as ProblemModel, CharacterModel, etc. These model 
/// components are not visual, and have no Children, IsSelected or IsExpanded, and so on. 
/// A StoryElement instance is displayed and modified on a Page such as ProblemPage or
/// CharacterPage which is contained in Shell's SplitView.Content frame. In order to do so,
/// it needs a TreeViewItem on the tree which binds to a StoryNodeItem, which is the visual
/// representation of the StoryElement.
/// 
/// StoryCAD's data model is called StoryModel. StoryModel  contains two ObservableCollection
/// lists of StoryNodeItems (and their counterpart StoryElements), a StoryExplorer collection which
/// contains all Story Elements (the StoryOverview and all Problem, Character, Setting, Scene
/// and Folder elements and their TreeView nodes, and a NarratorView storyView which contains just
/// Section (chapter, act, etc.) and Scene elements and which represents the story as it's being narrated.
/// 
/// In the Shell, the user can switch between the two views by loading one or the other ObservableCollection.
/// NavigationTree will thus point to one or the other of the collections as its bindings.  
/// </summary>
public class StoryNodeItem : INotifyPropertyChanged
{
    private readonly ILogService _logger;
    private readonly OutlineViewModel _outlineVM = Ioc.Default.GetService<OutlineViewModel>();

    // is it INavigable?
    public event PropertyChangedEventHandler PropertyChanged;

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
    /// Type of node
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
    /// Icon for this node, dependent on node type.
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
    /// Parent node of this node
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
    /// Child nodes of this node
    /// </summary>
    public ObservableCollection<StoryNodeItem> Children
    {
        get
        {
            if (_children == null) { _children = new ObservableCollection<StoryNodeItem>(); }
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
    /// Is this node expanded
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
    /// Binds to background color
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
    /// Binds to boarder color.
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
    /// Denotes if this note is root
    /// There should only be three root nodes
    /// 
    /// Narrative Root
    /// Overview node
    /// Trash node
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
    public override string ToString() { return Name; }


    /// <summary>
    /// This method allows a dept-first search (DFS) or 'pre-order traversal' of
    /// a BinderItem tree or subtree with a simple C# foreach.
    /// 
    /// In a DFS, you visit the root first and then search deeper into the tree
    /// visiting each node and then the node’s children.
    ///
    /// To use the enumerator, you code a foreach loop anywhere in your program:
    /// 
    /// foreach (TreeNode node in root)
    /// {
    ///     //perform action on node in DFS order here
    /// }
    ///
    ///  ref: http://www.timlabonne.com/2013/07/performing-a-dfs-over-a-rooted-tree-with-a-c-foreach-loop/
    ///  </summary>
    /// <returns>BinderItem for current node, then for each child in a loop</returns>
    public IEnumerator<StoryNodeItem> GetEnumerator()
    {
        yield return this;
        foreach (StoryNodeItem _child in Children)
        {
            // ReSharper disable once GenericEnumeratorNotDisposed
            IEnumerator<StoryNodeItem> _childEnumerator = _child.GetEnumerator();
            while (_childEnumerator.MoveNext())
                yield return _childEnumerator.Current;
        }
    }

    #endregion

    #region Constructors

    public StoryNodeItem(ILogService logger, StoryElement node, StoryNodeItem parent, StoryItemType type = StoryItemType.Unknown)
    {
        _logger = logger;
        Uuid = node.Uuid;
        Name = node.Name;
        if (type == StoryItemType.Unknown) { Type = node.ElementType; }
        else { Type = type; }
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
    public StoryNodeItem(StoryElement node, StoryNodeItem parent,
        StoryItemType type = StoryItemType.Unknown) : this(Ioc.Default.GetRequiredService<ILogService>(), node, parent, type)
    {
    }

    public StoryNodeItem(ILogService logger, StoryNodeItem parent, IXmlNode node)
    {
        _logger = logger;
        
        XmlNamedNodeMap _attrs = node.Attributes;
        if (_attrs != null)
        {
            Uuid = new Guid(((string)_attrs.GetNamedItem("UUID")?.NodeValue)!);
            string _nodeType = (string)_attrs.GetNamedItem("Type")?.NodeValue;
            switch (_nodeType!.ToLower()) //Fixes differences in casing between versions.
            {
                case "storyoverview":
                    Type = StoryItemType.StoryOverview;
                    Symbol = Symbol.View;
                    break;
                case "character":
                    Type = StoryItemType.Character;
                    Symbol = Symbol.Contact;
                    break;
                case "plotpoint":   // Legacy: PlotPoint was renamed to Scene   
                    Type = StoryItemType.Scene;
                    Symbol = Symbol.AllApps;
                    break;
                case "scene":
                    Type = StoryItemType.Scene;
                    Symbol = Symbol.AllApps;
                    break;
                case "problem":
                    Type = StoryItemType.Problem;
                    Symbol = Symbol.Help;
                    break;
                case "setting":
                    Type = StoryItemType.Setting;
                    Symbol = Symbol.Globe;
                    break;
                case "separator":   // Legacy: Separator was renamed Folder
                    Type = StoryItemType.Folder;
                    Symbol = Symbol.Folder;
                    break;
                case "folder":
                    Type = StoryItemType.Folder;
                    Symbol = Symbol.Folder;
                    break;
                case "section":
                    Type = StoryItemType.Section;
                    Symbol = Symbol.Folder;
                    break;
                case "web":
                    Type = StoryItemType.Web;
                    Symbol = Symbol.PreviewLink;
                    break;
                case "trashcan":
                    Type = StoryItemType.TrashCan;
                    Symbol = Symbol.Delete;
                    break;
                case "notes":
                    Type = StoryItemType.Notes;
                    Symbol = Symbol.TwoPage;
                    break;
            }

            Name = (string)_attrs.GetNamedItem("Name")?.NodeValue;
            if ((string)_attrs.GetNamedItem("IsExpanded")?.NodeValue == "True")
                IsExpanded = true;
            if ((string)_attrs.GetNamedItem("IsSelected")?.NodeValue == "True")
                IsSelected = true;
            if ((string)_attrs.GetNamedItem("IsRoot")?.NodeValue == "True")
                IsRoot = true;
        }

        Children = new();
        Parent = parent;
        Parent?.Children.Add(this);  // (if parent != null)
    }

    // Overloaded constructor without logger
    public StoryNodeItem(StoryNodeItem parent, IXmlNode node) : this(Ioc.Default.GetRequiredService<ILogService>(), parent, node)
    {
    }

    #endregion

    public bool Delete(StoryViewType view)
    {
        if (Type is StoryItemType.TrashCan || IsRoot)
            return false;

        // Remove any references to this element before deleting
        var outlineService = Ioc.Default.GetService<OutlineService>();
        if (outlineService != null && _outlineVM?.StoryModel != null)
        {
            outlineService.RemoveReferenceToElement(Uuid, _outlineVM.StoryModel);
        }

        MoveToTrash(view);
        return true;
    }

    /// <summary>
    /// Moves this node to the trash. 
    /// </summary>
    /// <param name="view"></param>
    private void MoveToTrash(StoryViewType view)
    {
        //Remove from current parent
        if (IsRoot || Parent is null)
            throw new InvalidOperationException("Root cannot be detached.");

        if (!Parent.Children.Remove(this))
            throw new InvalidOperationException("Parent/child link out of sync.");

        Parent = null;

        //Add to trash
        if (view != StoryViewType.ExplorerView) return;

        var trash = _outlineVM.StoryModel.StoryElements
            .First(e => e.ElementType == StoryItemType.TrashCan)
            .Node;

        trash.Children.Add(this);
        Parent = trash;
    }


    private void NotifyPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }


    /// <summary>
    /// Search up the StoryNodeItem tree to its
    /// root from a specified node and return its StoryItemType. 
    /// 
    /// This allows code to determine which TreeView a StoryNodeItem is in.
    /// </summary>
    /// <param name="startNode">The node to begin searching from</param>
    /// <returns>The StoryItemType of the root node</returns>
    public static StoryItemType RootNodeType(StoryNodeItem startNode)
    {
        try
        {
            StoryNodeItem node = startNode;
            while (!node.IsRoot)
                node = node.Parent;
            return node.Type;
        }
        catch (Exception ex)
        {
            Ioc.Default.GetService<LogService>().LogException(
                LogLevel.Error, ex, $"Root node type exception, this shouldn't happen {ex.Message} {ex.Message}");
            return StoryItemType.Unknown;
        }
    }
}