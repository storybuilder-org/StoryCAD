using CommunityToolkit.Mvvm.DependencyInjection;
using Elmah.Io.Client;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using StoryBuilder.Models;
using StoryBuilder.Services.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Windows.Data.Xml.Dom;
using Windows.Web.AtomPub;
using ABI.Windows.ApplicationModel.VoiceCommands;

namespace StoryBuilder.ViewModels;

/// <summary> 
/// StoryNodeItem is the equivalent of a ViewModel for a single TreeViewItem in the 
/// NavigationTree displayed on the Shell Page. Each TreeViewItem is the visual representation
/// of one node in a TreeView, and each TreeViewItem binds to a StoryNodeItem through a
/// TreeViewItemDataTemplate contained in Shell. The NavigationTree's TreeViewItems are bound to
/// an ObservableCollection of StoryNodeItems contained in the ShellViewModel, which is the
/// Shell DataContext.
/// 
/// Like any other ViewModel, a StoryNodeItem is an intermediary between the data model 
/// (in this case, StoryModel) and the View (in this case, Shell). The model is composed of
/// instances of StoryElements such as ProblemModel, CharacterModel, etc. These model 
/// components are not visual, and have no Children, IsSelected or IsExpanded, and so on. 
/// A StoryElement instance is displayed and modified on a Page such as ProblemPage or
/// CharacterPage which is contained in Shell's SplitView.Content frame.
/// 
/// StoryBuilder's data model is called StoryModel. StoryModel  contains two ObservableCollection
/// lists of StoryNodeItems (and their counterpart StoryElements), a StoryExplorer collection which
/// contains all Story Elements (the StoryOverview and all Problem, Character, Setting, Scene
/// and Folder elements) and a Narrator View which contains just Section (chapter, etc) and
/// selected Scene elements and which represents the story as it's being narrated.
/// 
/// In the Shell, the user can switch between the two views by loading one or the other model.
/// NavigationTree will this point to one or the other of the collections. These two 'submodels' 
/// or hierarchies consist of StoreNodeModel instances, and contain a similar structure to StoryNodeItem.
/// 
/// </summary>
public class StoryNodeItem : DependencyObject, INotifyPropertyChanged
{
    private LogService logger = Ioc.Default.GetRequiredService<LogService>();
    private ShellViewModel shellvm = Ioc.Default.GetRequiredService<ShellViewModel>();

    // is it INavigable?
    #region Properties

    public event PropertyChangedEventHandler PropertyChanged;

    // StoryElement data

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

    private int _id;
    public int Id
    {
        get => _id;
        set
        {
            if (_id != value)
            {
                _id = value;
                NotifyPropertyChanged("Id");
            }
        }
    }

    private StoryItemType _type;
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
        //get => (bool)GetValue(IsExpandedProperty);
        //set
        //{
        //    SetValue(IsExpandedProperty, value);
        //    NotifyPropertyChanged("IsExpanded");
        //}
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

    //// Use a DependencyProperty as the backing store for IsExpanded. 
    //public static readonly DependencyProperty IsExpandedProperty =
    //    DependencyProperty.Register("IsExpanded", typeof(bool), typeof(StoryNodeItem), new PropertyMetadata(false));

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

    public TextWrapping TextWrapping
    {
        get => GlobalData.Preferences.WrapNodeNames;
    }

//public bool IsSelected
//{
//    get => (bool)GetValue(IsSelectedProperty);
//    set
//    {
//        SetValue(IsSelectedProperty, value);
//        NotifyPropertyChanged("IsSelected");
//    }
//}



    //// Use a DependencyProperty as the backing store for IsSelected
    //public static readonly DependencyProperty IsSelectedProperty =
    //    DependencyProperty.Register("IsSelected", typeof(bool), typeof(StoryNodeItem), new PropertyMetadata(false));


    private bool _isRoot;
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
        foreach (StoryNodeItem child in Children)
        {
            // ReSharper disable once GenericEnumeratorNotDisposed
            IEnumerator<StoryNodeItem> childEnumerator = child.GetEnumerator();
            while (childEnumerator.MoveNext())
                yield return childEnumerator.Current;
        }
    }

    #endregion

    #region Constructors

    public StoryNodeItem(StoryModel model, StoryNodeItem node, StoryNodeItem parent)
    {
        _uuid = node.Uuid;
        StoryElement element = model.StoryElements.StoryElementGuids[Uuid];
        Name = element.Name;
        _type = node.Type;
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
        }

        Parent = parent;
        Children = new ObservableCollection<StoryNodeItem>();

        IsExpanded = node.IsExpanded;
        IsRoot = node.IsRoot;
        //IsSelected = node.IsSelected;
    }

    public StoryNodeItem(StoryNodeItem parent, StoryElement model)
    {
        Uuid = model.Uuid;
        Name = model.Name;
        Type = model.Type;
        switch (model.Type)
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
            case StoryItemType.TrashCan:
                Symbol = Symbol.Delete;
                break;
        }
        Parent = parent;
        Children = new ObservableCollection<StoryNodeItem>();
        IsExpanded = false;
        //IsSelected = false;
        if (Parent == null)
            return;
        Parent.Children.Add(this);
    }

    public StoryNodeItem(StoryElement node, StoryNodeItem parent)
    {
        _uuid = node.Uuid;
        _name = node.Name;
        _type = node.Type;
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
            case StoryItemType.TrashCan:
                Symbol = Symbol.Delete;
                break;
        }

        Parent = parent;
        Children = new ObservableCollection<StoryNodeItem>();

        IsExpanded = false;
        IsRoot = false;
        //IsSelected = false;
        if (Parent == null)
            return;
        Parent.Children.Add(this);
    }

    public StoryNodeItem(StoryNodeItem parent, IXmlNode node)
    {
        XmlNamedNodeMap attrs = node.Attributes;
        if (attrs != null)
        {
            Uuid = new Guid((string)attrs.GetNamedItem("UUID")?.NodeValue);
            string type = (string)attrs.GetNamedItem("Type")?.NodeValue;
            switch (type)
            {
                case "StoryOverView":
                    Type = StoryItemType.StoryOverview;
                    Symbol = Symbol.View;
                    break;
                case "Character":
                    Type = StoryItemType.Character;
                    Symbol = Symbol.Contact;
                    break;
                case "PlotPoint":   // Legacy: PlotPoint was renamed to Scene   
                    Type = StoryItemType.Scene;
                    Symbol = Symbol.AllApps;
                    break;
                case "Scene":
                    Type = StoryItemType.Scene;
                    Symbol = Symbol.AllApps;
                    break;
                case "Problem":
                    Type = StoryItemType.Problem;
                    Symbol = Symbol.Help;
                    break;
                case "Setting":
                    Type = StoryItemType.Setting;
                    Symbol = Symbol.Globe;
                    break;
                case "Separator":   // Legacy: Separator was renamed Folder
                    Type = StoryItemType.Folder;
                    Symbol = Symbol.Folder;
                    break;
                case "Folder":
                    Type = StoryItemType.Folder;
                    Symbol = Symbol.Folder;
                    break;
                case "Section":
                    Type = StoryItemType.Section;
                    Symbol = Symbol.Folder;
                    break;
                case "TrashCan":
                    Type = StoryItemType.TrashCan;
                    Symbol = Symbol.Delete;
                    break;
            }

            Name = (string)attrs.GetNamedItem("Name")?.NodeValue;
            if ((string)attrs.GetNamedItem("IsExpanded")?.NodeValue == "True")
                IsExpanded = true;
            if ((string)attrs.GetNamedItem("IsSelected")?.NodeValue == "True")
                IsSelected = true;
            if ((string)attrs.GetNamedItem("IsRoot")?.NodeValue == "True")
                IsRoot = true;
        }

        Children = new ObservableCollection<StoryNodeItem>();
        Parent = parent;
        Parent?.Children.Add(this);  // (if parent != null)
    }
    #endregion

    public void Delete(ViewType View)
    {
        logger.Log(LogLevel.Trace, $"Starting to delete element {Name} ({Uuid}) from {View}");
        //Sanity check
        if (Type == StoryItemType.TrashCan || IsRoot)
        {
            Ioc.Default.GetService<ShellViewModel>().ShowMessage(LogLevel.Warn, "This element can't be deleted.",false);
            logger.Log(LogLevel.Info, "User tried to delete Root or Trashcan node.");
        }

        //Set source collection to either narrative view or explorer view, we use the first item [0] so we don't delete from trash.
        StoryNodeItem SourceCollection;
        if (View == ViewType.Explorer) { SourceCollection = shellvm.StoryModel.ExplorerView[0]; }
        else { SourceCollection = shellvm.StoryModel.NarratorView[0]; }

        if (SourceCollection.Children.Contains(this))
        {
            //Delete node from selected view.
            logger.Log(LogLevel.Info, "Node found in root, deleting it.");
            SourceCollection.Children.Remove(this);
            TrashItem(View); //Add to appropriate trash node.
        }
        else
        {
            foreach (StoryNodeItem childItem in SourceCollection.Children)
            {
                logger.Log(LogLevel.Info, "Recursing tree to find node.");
                RecursiveDelete(childItem, View);
            }
        }
    }

    private void RecursiveDelete(StoryNodeItem ParentItem, ViewType View)
    {
        logger.Log(LogLevel.Info, $"Starting recursive delete instance for parent {ParentItem.Name} ({ParentItem.Uuid}) in {View}");
        try
        {
            if (ParentItem.Children.Contains(this)) //Checks parent contains child we are looking.
            {
                logger.Log(LogLevel.Info, "StoryNodeItem found, deleting it.");
                ParentItem.Children.Remove(this); //Deletes child.
                TrashItem(View); //Add to appropriate trash node.
            }
            else //If child isn't in parent, recurse again.
            {
                logger.Log(LogLevel.Info, "StoryNodeItem not found, recursing again");
                foreach (StoryNodeItem ChildItem in ParentItem.Children)
                {
                    logger.Log(LogLevel.Debug, $"ChildItem is {ChildItem.Name} {ChildItem.Uuid}");
                    RecursiveDelete(ChildItem, View);
                }
            }
        }
        catch (Exception ex) { logger.LogException(LogLevel.Error, ex, "Error deleting node in Recursive delete"); }
    }

    private void TrashItem(ViewType View)
    {
        if (View == ViewType.Explorer)
        {
            shellvm.StoryModel.ExplorerView[1].Children.Add(this);
            Parent = shellvm.StoryModel.ExplorerView[1];
        }
        else
        {
            shellvm.StoryModel.NarratorView[1].Children.Add(this);
            Parent = shellvm.StoryModel.NarratorView[1];
        }
    }

    private void NotifyPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}