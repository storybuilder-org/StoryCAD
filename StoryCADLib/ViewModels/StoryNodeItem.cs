using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Windows.Data.Xml.Dom;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using StoryCAD.Models;
using StoryCAD.Services.Logging;

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
/// it needs a TreeViewItem on the tree which binds to a StoryNodeItem, which
/// 
/// StoryCAD's data model is called StoryModel. StoryModel  contains two ObservableCollection
/// lists of StoryNodeItems (and their counterpart StoryElements), a StoryExplorer collection which
/// contains all Story Elements (the StoryOverview and all Problem, Character, Setting, Scene
/// and Folder elements) and a NarratorView storyView which contains just Section (chapter, etc) and
/// selected Scene elements and which represents the story as it's being narrated.
/// 
/// In the Shell, the user can switch between the two views by loading one or the other model.
/// NavigationTree will this point to one or the other of the collections. These two 'submodels' 
/// or hierarchies consist of StoreNodeModel instances, and contain a similar structure to StoryNodeItem.
/// 
/// </summary>
public class StoryNodeItem : DependencyObject, INotifyPropertyChanged
{
    private LogService _logger = Ioc.Default.GetRequiredService<LogService>();
    private ShellViewModel _shellVM = Ioc.Default.GetRequiredService<ShellViewModel>();

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

    public TextWrapping TextWrapping => GlobalData.Preferences.WrapNodeNames;

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

    public StoryNodeItem(StoryElement node, StoryNodeItem parent, StoryItemType type = StoryItemType.Unknown)
    {
        Uuid = node.Uuid;
        Name = node.Name;
        if (type == StoryItemType.Unknown) { Type = node.Type; }
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
        
        if (Parent == null) { return; }
        Parent.Children.Add(this);
    }

    public StoryNodeItem(StoryNodeItem parent, IXmlNode node)
    {
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

        Children = new ObservableCollection<StoryNodeItem>();
        Parent = parent;
        Parent?.Children.Add(this);  // (if parent != null)
    }
    #endregion

    public void Delete(StoryViewType storyView)
    {
        _logger.Log(LogLevel.Trace, $"Starting to delete element {Name} ({Uuid}) from {storyView}");
        //Sanity check
        if (Type == StoryItemType.TrashCan || IsRoot)
        {
            Ioc.Default.GetRequiredService<ShellViewModel>().ShowMessage(LogLevel.Warn, "This element can't be deleted.",false);
            _logger.Log(LogLevel.Info, "User tried to delete Root or Trashcan node.");
        }

        //Set source collection to either narrative view or explorer view, we use the first item [0] so we don't delete from trash.
        StoryNodeItem _sourceCollection;
        if (storyView == StoryViewType.ExplorerView) { _sourceCollection = _shellVM.StoryModel.ExplorerView[0]; }
        else { _sourceCollection = _shellVM.StoryModel.NarratorView[0]; }

        if (_sourceCollection.Children.Contains(this))
        {
            //Delete node from selected view.
            _logger.Log(LogLevel.Info, "Node found in root, deleting it.");
            _sourceCollection.Children.Remove(this);
            TrashItem(storyView); //Add to appropriate trash node.
        }
        else
        {
            foreach (StoryNodeItem _childItem in _sourceCollection.Children)
            {
                _logger.Log(LogLevel.Info, "Recursing tree to find node.");
                RecursiveDelete(_childItem, storyView);
            }
        }
    }

    private void RecursiveDelete(StoryNodeItem parentItem, StoryViewType storyView)
    {
        _logger.Log(LogLevel.Info, $"Starting recursive delete instance for parent {parentItem.Name} ({parentItem.Uuid}) in {storyView}");
        try
        {
            if (parentItem.Children.Contains(this)) //Checks parent contains child we are looking.
            {
                _logger.Log(LogLevel.Info, "StoryNodeItem found, deleting it.");
                parentItem.Children.Remove(this); //Deletes child.
                TrashItem(storyView); //Add to appropriate trash node.
            }
            else //If child isn't in parent, recurse again.
            {
                _logger.Log(LogLevel.Info, "StoryNodeItem not found, recursing again");
                foreach (StoryNodeItem _childItem in parentItem.Children)
                {
                    _logger.Log(LogLevel.Debug, $"ChildItem is {_childItem.Name} {_childItem.Uuid}");
                    RecursiveDelete(_childItem, storyView);
                }
            }
        }
        catch (Exception _ex) { _logger.LogException(LogLevel.Error, _ex, "Error deleting node in Recursive delete"); }
    }

    private void TrashItem(StoryViewType storyView)
    {
        if (storyView == StoryViewType.ExplorerView)
        {
            _shellVM.StoryModel.ExplorerView[1].Children.Add(this);
            Parent = _shellVM.StoryModel.ExplorerView[1];
        }
        //Narrative view nodes are not added to trash.
    }

    private void NotifyPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}