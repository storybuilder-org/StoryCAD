using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using StoryBuilder.Models;
using StoryBuilder.Models.Tools;
using StoryBuilder.Services.Logging;
using StoryBuilder.ViewModels;
using System;
using System.ComponentModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.ViewManagement;

namespace StoryBuilder.Views;

public sealed partial class Shell
{
    public ShellViewModel ShellVm => Ioc.Default.GetService<ShellViewModel>();
    public UnifiedVM UnifiedVm => Ioc.Default.GetService<UnifiedVM>();
    public PreferencesModel Preferences = GlobalData.Preferences;

    private TreeViewNode dragTargetNode;
    private StoryNodeItem dragTargetStoryNode;
    private StoryNodeItem dragSourceStoryNode;
    private bool dragIsValid;
    private LogService Logger;
    private readonly object dragLock = new object();

    public Shell()
    {
        try
        {
            InitializeComponent();
            Logger = Ioc.Default.GetService<LogService>();
            DataContext = ShellVm;
            GlobalData.GlobalDispatcher = DispatcherQueue.GetForCurrentThread();
            Loaded += Shell_Loaded;
        }                         
        catch (Exception ex)
        {
            // A shell initialization error is fatal
            Logger.LogException(LogLevel.Error, ex, ex.Message);
            Logger.Flush();
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
        if (GlobalData.ShowDotEnvWarning) { await ShellVm.ShowDotEnvWarningAsync(); }

        if (!await Ioc.Default.GetRequiredService<WebViewModel>().CheckWebviewState())
        {
            ShellVm._canExecuteCommands = false;
            await Ioc.Default.GetRequiredService<WebViewModel>().ShowWebviewDialog();
            ShellVm._canExecuteCommands = true;
        }
        if (GlobalData.LoadedWithVersionChange ) { await ShellVm.ShowChangelog(); }

        //If StoryBuilder was loaded from a .STBX File then instead of showing the Unified menu
        //We will instead load the file instead.
        if (GlobalData.FilePathToLaunch == null) { await ShellVm.OpenUnifiedMenu(); }
        else { await ShellVm.OpenFile(GlobalData.FilePathToLaunch);}
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
        if (ShellVm.RightClickedTreeviewItem != null) { ShellVm.RightClickedTreeviewItem.Background = null; } //Remove old right clicked nodes background

        TreeViewItem item = (TreeViewItem)sender;
        item.Background = new SolidColorBrush(new UISettings().GetColorValue(UIColorType.Accent));

        ShellVm.RightTappedNode = (StoryNodeItem)item.DataContext;
        ShellVm.RightClickedTreeviewItem = item; //We can't set the background through right-tapped node so we set a reference to the node itself to reset the background later
        ShellVm.ShowFlyoutButtons();
    }

    /// <summary>
    /// Treat a treeview item as if it were a button
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
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
        if (ShellVm.DataSource == null || ShellVm.DataSource.Count == 0) { return; }
        foreach (StoryNodeItem node in ShellVm.DataSource[0]) { node.Background = null; }
    }

    #region Drag and Drop exits 

    // Drag and drop functionality for NavigationTree allows TreeViewItem nodes
    // StoryNodeItems) to be moved from their current location to a new target
    // folder. The following code implements the following features:
    //
    // When a drag operation starts on a source node, TreeView_DragItemsStarting is
    // invoked. It edits that the source (node to drag) is valid and stores a reference
    // to the node. 
    //
    // As the drag operation proceeds, TreeViewItem_OnDragEnter is invoked for the
    // target node over which the user is currently hovering as long as the mouse is
    // moving. It edits that the target node is valid and stores a reference to the
    // target node.
    //
    //    When the user drops the source node on a target node, TreeView_DragItemsCompleted
    //    is invoked. It validates that the source and target are good and locks the 
    //    actual node move to prevent side effects (such as AutoSave timer firing) and
    //    then performs the following steps:                  
    //       Remove the source node from its original parent's collection of children nodes.
    //       Add the source node as a child node to the target node's Children nodes.
    //       Update the source node's parent reference to point to the target node.
    //    Drag and drop is then re-enabled for subsequent drag and drop operations.
  

    private void TreeView_DragItemsStarting(TreeView sender, TreeViewDragItemsStartingEventArgs args)
    {
        Logger.Log(LogLevel.Trace, $"OnDragItemsStarting enter");
        // Assume the worst
        args.Data.RequestedOperation = DataPackageOperation.None;
        dragIsValid = false;  // set in case OnDragEnter is not fired 
        
        // args.Items[0] is the TreeViewItem you're dragging.
        // With SelectionMode="Single" there will be only the one.
        Type type = args.Items[0].GetType();
        if (!type.Name.Equals("StoryNodeItem"))
        {
            ShellVm.ShowMessage(LogLevel.Warn, "Invalid drag source", true);
            args.Cancel = true;
            return;
        }

        dragSourceStoryNode = args.Items[0] as StoryNodeItem;
        StoryNodeItem _parent = dragSourceStoryNode!.Parent;
        if (_parent == null)
        {
            ShellVm.ShowMessage(LogLevel.Warn, "Invalid drag source", true);
            args.Cancel = true;
            return;
        }

        while (!_parent.IsRoot) // find the drag source's root
        {
            _parent = _parent.Parent;
        }

        if (_parent.Type == StoryItemType.TrashCan)
        {
            ShellVm.ShowMessage(LogLevel.Warn, "Can't drag from Trashcan", true);
            args.Cancel = true;
            return;
        }

        // Source node is valid for move
        dragIsValid = true;

        // Report status
        Logger.Log(LogLevel.Trace, $"OnDragItemsStarting exit");
    }

    private void TreeViewItem_OnDragEnter(object sender, DragEventArgs args)
    {
        Logger.Log(LogLevel.Info, $"OnDragEnter enter");
        // Assume the worst
        dragIsValid = false;

        // sender is the node you're dragging over (the prospective target)
        Type type = sender.GetType();
        if (!type.Name.Equals("TreeViewItem"))
        {
            ShellVm.ShowMessage(LogLevel.Warn, "Invalid drag target type", true);
            args.Handled = true;
            return;
        }
        var dragTargetItem = sender as TreeViewItem;
        dragTargetNode = NavigationTree.NodeFromContainer(dragTargetItem);
        
        // Find the node's root
        var node = dragTargetNode;
        while (node.Depth != 0) { node = node.Parent; }

        var root = node.Content as StoryNodeItem;
        if (root!.Type == StoryItemType.TrashCan)
        {
            ShellVm.ShowMessage(LogLevel.Warn, "Drag to Trashcan invalid", true);
            args.Handled = true;
            return;
        }

        // The drag target can be the root (Story Overview) node or any node below it
        if (dragTargetNode.Depth < 0)
        {
            ShellVm.ShowMessage(LogLevel.Warn, "Drag target is not root or below", true);
            args.Handled = true;
            return;
        }
        
        dragTargetStoryNode = dragTargetNode.Content as StoryNodeItem;
        // Target node is valid for move
        dragIsValid = true;
        args.Handled = true;
        Logger.Log(LogLevel.Info, $"OnDragEnter exit");
    }

    private void TreeView_DragItemsCompleted(TreeView sender, TreeViewDragItemsCompletedEventArgs args)
    {
        Logger.Log(LogLevel.Trace, $"DragItemsCompleted entry");
        Logger.Log(LogLevel.Trace, $"dragIsValid: {dragIsValid.ToString()}");

        if (dragSourceStoryNode! == null)
        {
            ShellVm.ShowMessage(LogLevel.Warn, "Invalid source node", true);
        }
        else if (dragTargetStoryNode! == null)
        {
            ShellVm.ShowMessage(LogLevel.Warn, "Invalid target node", true);
        }
        else if (dragSourceStoryNode.Uuid == dragTargetStoryNode.Uuid)
        {
            ShellVm.ShowMessage(LogLevel.Warn, "Drag to self", true);
        }
        else {
            if (dragIsValid)
            {
                Logger.Log(LogLevel.Trace, $"Source: {dragSourceStoryNode.Name}");
                Logger.Log(LogLevel.Trace, $"Target: {dragTargetStoryNode.Name}");
                lock (dragLock)
                {
                    try
                    {
                        // Remove the source node from its original parent's children collection
                        StoryNodeItem sourceParent = dragSourceStoryNode.Parent;
                        sourceParent.Children.Remove(dragSourceStoryNode);

                        // Add the source node to the target node's children collection.
                        dragTargetStoryNode.Children.Insert(0, dragSourceStoryNode);
                        
                        // Set the source node's parent to the target node (the new parent) 
                        dragSourceStoryNode.Parent = dragTargetStoryNode;
                    }
                    catch (Exception ex)
                    {
                        Logger.LogException(LogLevel.Error, ex, "Error in drag/drop drop operation");
                    }
                }

                // Refresh the UI and report the move
                ShellViewModel.ShowChange();
                ShellVm.ShowMessage(LogLevel.Info, "Drag and drop successful", true);
            }
        }

        NavigationTree.CanDrag = true;
        NavigationTree.AllowDrop = true;
        Logger.Log(LogLevel.Trace, $"OnDragItemsCompleted exit");
    }

    #endregion
}