using System;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using StoryBuilder.Models;
using StoryBuilder.Models.Tools;
using StoryBuilder.Services.Logging;
using StoryBuilder.ViewModels;

namespace StoryBuilder.Views;

public sealed partial class Shell
{
    public ShellViewModel ShellVm => Ioc.Default.GetService<ShellViewModel>();
    public UnifiedVM UnifiedVm => Ioc.Default.GetService<UnifiedVM>();
    public PreferencesModel Preferences = GlobalData.Preferences;

    private TreeViewItem dragTargetItem;    
    private TreeViewNode dragTargetNode;
    private StoryNodeItem dragTargetStoryNode;
    private TreeViewItem dragSourceItem = null;
    private TreeViewNode dragSourceNode;
    private StoryNodeItem dragSourceStoryNode;

    public Shell()
    {
        try
        {
            InitializeComponent();
            DataContext = ShellVm;
            Loaded += Shell_Loaded;
        }                         
        catch (Exception ex)
        {
            // A shell initialization error is fatal
            LogService log = Ioc.Default.GetService<LogService>();
            log.LogException(LogLevel.Error, ex, ex.Message);
            log.Flush();
            Application.Current.Exit();  // Win32
        }
        ShellVm.SplitViewFrame = SplitViewFrame;
    }

    private async void Shell_Loaded(object sender, RoutedEventArgs e)
    {
        // The Shell_Loaded event is processed in order to obtain and save the XamlRool  
        // and pass it on to ContentDialogs as a WinUI work-around. See
        // https://docs.microsoft.com/en-us/windows/winui/api/microsoft.ui.xaml.controls.contentdialog?view=winui-3.0-preview
        GlobalData.XamlRoot = Content.XamlRoot;
        ShellVm.ShowHomePage();
        ShellVm.ShowConnectionStatus();
        await ShellVm.OpenUnifiedMenu();
    }

    /// <summary>
    /// Makes the TreeView lose its selection when there is no corresponding main menu item.
    /// </summary>
    /// <remarks>But I don't know why...</remarks>
    private void SplitViewFrame_OnNavigated(object sender, NavigationEventArgs e)
    {
        NavigationTree.SelectionMode = TreeViewSelectionMode.None;
        NavigationTree.SelectionMode = TreeViewSelectionMode.Single;
    }

    /// <summary>
    /// Navigates to the specified source page type.
    /// </summary>
    public bool Navigate(Type sourcePageType, object parameter = null)
    {
        return SplitViewFrame.Navigate(sourcePageType, parameter);
    }
    private void TreeViewItem_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        TreeViewItem item = (TreeViewItem)sender;
        ShellVm.RightTappedNode = (StoryNodeItem)item.DataContext;
        ShellVm.ShowFlyoutButtons();
    }

    private void TreeViewItem_Invoked(TreeView sender, TreeViewItemInvokedEventArgs args)
    {
        ShellVm.TreeViewNodeClicked(args.InvokedItem);
        args.Handled = true;
    }

    private void AddButton_ContextRequested(UIElement sender, ContextRequestedEventArgs args)
    {
        FlyoutShowOptions myOption = new();
        myOption.ShowMode = FlyoutShowMode.Transient;
        AddStoryElementCommandBarFlyout.ShowAt(NavigationTree, myOption);
    }

    /// <summary>
    /// This is called when the user clicks the save pen
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void SaveIconPressed(object sender, PointerRoutedEventArgs e) { await ShellVm.SaveFile(); }

    private void Search(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        ShellVm.SearchNodes();
    }

    private void ClearNodes(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (ShellVm.DataSource == null || ShellVm.DataSource.Count ==0) { return; }
        foreach (StoryNodeItem node in ShellVm.DataSource[0]) { node.Background = null; }
    }

    // Drag and Drop related
    private void TreeViewItem_OnDragEnter(object sender, DragEventArgs args)
    {
        System.Diagnostics.Debug.WriteLine($"OnDragEnter event");

        // args.OriginalSource is the TreeViewItem you're dragging
        Type type = args.OriginalSource.GetType();
        if (!type.Name.Equals("TreeViewItem"))
        {
            System.Diagnostics.Debug.WriteLine($"Invalid dragSource type: {type.Name}");
            //args.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.None;
            args.Handled = true;
            base.OnDragEnter(args);
            return;
        }
        dragSourceItem = args.OriginalSource as TreeViewItem;
        dragSourceNode = NavigationTree.NodeFromContainer(dragSourceItem);
        dragSourceStoryNode = dragSourceNode.Content as StoryNodeItem;
        System.Diagnostics.Debug.WriteLine($"dragSource Name: {dragSourceStoryNode.Name}");
        System.Diagnostics.Debug.WriteLine($"dragSource type: {dragSourceStoryNode.Type.ToString()}");

        // Insure that the source is not in the trashcan
        var node = dragSourceNode;
        while (node.Depth != 0)
        {
            node = node.Parent;
        }
        var root = node.Content as StoryNodeItem;
        if (root.Type == StoryItemType.TrashCan)
        {
            System.Diagnostics.Debug.WriteLine($"dragSource root is TrashCan");
            //args.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.None;
            args.Handled = true;
            base.OnDragEnter(args);
            return;
        }

        // sender is the object being dragged
        Type type = sender.GetType();
        if (!type.Name.Equals("TreeViewItem"))
        {
            System.Diagnostics.Debug.WriteLine($"Invalid dragTarget type: {type.Name}");
            args.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.None;
            args.Handled = true;
            base.OnDragEnter(args);
            return;
        }
        dragTargetItem = sender as TreeViewItem;
        dragTargetNode = NavigationTree.NodeFromContainer(dragTargetItem);
        dragTargetStoryNode = dragTargetNode.Content as StoryNodeItem;

        // Insure that the source is not in the trashcan
        //while (node.Depth != 0) 
        //{ 
        //    node = node.Parent; 
        //}

        //// disallow starting a drag from the trashcan
        //// make all trashcan entries invalid targets



        base.OnDragEnter(args);
    }

    private void TreeViewItem_OnDrop(object sender, DragEventArgs args)
    {
        System.Diagnostics.Debug.WriteLine($"OnDrop event");
        base.OnDrop(args);
    }

    //private void TreeViewItem_DropCompleted(UIElement sender, DropCompletedEventArgs args)
    //{
    //    string x = args.ToString();
    //}

    private void TreeViewItem_OnDragOver(object sender, DragEventArgs args)
    {
        System.Diagnostics.Debug.WriteLine($"OnDragOver event");
        //// sender is the item you are currently hovering over 
        //Type type = sender.GetType();
        //if (!type.Name.Equals("TreeViewItem"))
        //{
        //    System.Diagnostics.Debug.WriteLine($" Invalid target type {type.Name}");
        //    args.Handled = true;
        //    args.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.None;
        //    return;
        //}
        //TreeViewItem item = (TreeViewItem)sender;
        //item.AllowDrop = false;
        ////Console.WriteLine($"Entered for {item.Content}");

        //var node = NavigationTree.NodeFromContainer(item);

        //System.Diagnostics.Debug.WriteLine($"node  = {node.Content}");
        //while (node.Depth != 0) 
        //{ 
        //    node = node.Parent; 
        //}
        //StoryNodeItem storyNode = (StoryNodeItem) node.Content;
        //System.Diagnostics.Debug.WriteLine($"StoryNode Root = {storyNode.Name}");

        //if (storyNode.Type == StoryItemType.TrashCan)
        //{
        //    System.Diagnostics.Debug.WriteLine($"Root = Trash can");
        //    item.AllowDrop = false;
        //    args.Handled = true;
        //    args.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.None;      
        //    return;
        //}
        //System.Diagnostics.Debug.WriteLine($"Allowing drop for {storyNode.Name}");
        //item.AllowDrop = true;
        ////args.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Move;
        base.OnDragOver(args);
    }
    
    //private void OnDragItemsStarting(TreeView sender, TreeViewDragItemsStartingEventArgs args)
    //{
    //    TreeViewNode trashNode = sender.RootNodes[1];
    //    var trashItem = NavigationTree.ItemFromContainer(trashNode);
    //}

    private void TreeViewItem_OnDragStarting(UIElement sender, DragStartingEventArgs args)
    {
        System.Diagnostics.Debug.WriteLine($"OnDragStarting event");
    }

    //private void OnDragItemsCompleted(TreeView sender, TreeViewDragItemsCompletedEventArgs args)
    //{


    //}
}