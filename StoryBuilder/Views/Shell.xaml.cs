using System;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using StoryBuilder.Models;
using StoryBuilder.Models.Tools;
using StoryBuilder.Services.Logging;
using StoryBuilder.Services.Messages;
using StoryBuilder.ViewModels;
using Windows.UI.ViewManagement;
using Microsoft.UI.Dispatching;
using StoryBuilder.Services;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using System.Diagnostics;

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
        //args.Data.RequestedOperation = DataPackageOperation.Move;
        dragIsValid = true;

        // Report status
        Logger.Log(LogLevel.Trace, $"OnDragItemsStarting exit");
    }

    private void TreeViewItem_OnDragEnter(object sender, DragEventArgs args)
    {
        Logger.Log(LogLevel.Info, $"OnDragEnter enter");
        // Assume the worst
        dragIsValid = false;
        args.Data.RequestedOperation = DataPackageOperation.None;

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

        // A moved node is inserted above the target node, so you can't move to the root.
        if (dragTargetNode.Depth < 1)
        {
            ShellVm.ShowMessage(LogLevel.Warn, "Drag target is not below root", true);
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

        if (dragSourceStoryNode == null)
        {
            ShellVm.ShowMessage(LogLevel.Warn, "Invalid source node", true);
        }
        else if (dragTargetStoryNode == null)
        {
            ShellVm.ShowMessage(LogLevel.Warn, "Invalid target node", true);
        }
        else if (dragIsValid)
        {
            var sourceParent = dragSourceStoryNode.Parent;

            // Remove the source node from its original parent's children collection
            sourceParent.Children.Remove(dragSourceStoryNode);

            // Add the source node to the target node's parent's children collection.
            // Insert() places the source immediately before the target.
            var targetParent = dragTargetStoryNode.Parent;
            var targetIndex = targetParent.Children.IndexOf(dragTargetStoryNode);
            targetParent.Children.Insert(targetIndex, dragSourceStoryNode);

            // Update the source node's parent
            dragSourceStoryNode.Parent = dragTargetStoryNode.Parent;

            Bindings.Update();

            // Refresh the UI and report the move
            ShellViewModel.ShowChange();
            ShellVm.ShowMessage(LogLevel.Info, "Drag and drop successful", true);
        }
        
        NavigationTree.CanDrag = true;
        NavigationTree.AllowDrop = true;
        Logger.Log(LogLevel.Trace, $"OnDragItemsCompleted exit");
    }
}