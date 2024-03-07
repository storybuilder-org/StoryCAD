using System;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using StoryCAD.Models;
using StoryCAD.Models.Tools;
using StoryCAD.Services.Logging;
using StoryCAD.Services.Messages;
using StoryCAD.ViewModels;
using Windows.UI.ViewManagement;
using Microsoft.UI.Dispatching;
using StoryCAD.Services;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Microsoft.UI;
using Windows.Storage.Provider;

namespace StoryCAD.Views;

public sealed partial class Shell
{
    public ShellViewModel ShellVm => Ioc.Default.GetService<ShellViewModel>();
    public Windowing Windowing => Ioc.Default.GetService<Windowing>();
    public UnifiedVM UnifiedVm => Ioc.Default.GetService<UnifiedVM>();
    public PreferencesModel Preferences = Ioc.Default.GetRequiredService<AppState>().Preferences;

    private TreeViewItem dragTargetItem;
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
            Ioc.Default.GetRequiredService<Windowing>().GlobalDispatcher = DispatcherQueue.GetForCurrentThread();
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
        Windowing.XamlRoot = Content.XamlRoot;
        Ioc.Default.GetService<AppState>().StartUpTimer.Stop();
        ShellVm.ShowHomePage();
        ShellVm.ShowConnectionStatus();
        Ioc.Default.GetRequiredService<Windowing>().UpdateWindowTitle();
        if (!Ioc.Default.GetService<AppState>().EnvPresent) { await ShellVm.ShowDotEnvWarningAsync(); }

        if (!await Ioc.Default.GetRequiredService<WebViewModel>().CheckWebviewState())
        {
            ShellVm._canExecuteCommands = false;
            await Ioc.Default.GetRequiredService<WebViewModel>().ShowWebviewDialog();
            ShellVm._canExecuteCommands = true;
        }

        //Shows changelog if the app has been updated since the last launch.
        if (Ioc.Default.GetRequiredService<AppState>().LoadedWithVersionChange)
        {
            await new Services.Dialogs.Changelog().ShowChangeLog();
        }

        //If StoryCAD was loaded from a .STBX File then instead of showing the Unified menu
        //We will instead load the file instead.
        Logger.Log(LogLevel.Info, $"Filepath to launch {ShellVm.FilePathToLaunch}");
        if (ShellVm.FilePathToLaunch == null) { await ShellVm.OpenUnifiedMenu(); }
        else { await ShellVm.OpenFile(ShellVm.FilePathToLaunch);}
    }

    /// <summary>
    /// Makes the TreeView lose its selection when there is no corresponding main menu item.
    /// </summary>
    /// <remarks>But I don't know why...</remarks>
    private void SplitViewFrame_OnNavigated(object sender, NavigationEventArgs e)
    {
        NavigationTree.SelectionMode = TreeViewSelectionMode.None;
        NavigationTree.SelectionMode = TreeViewSelectionMode.Single;
        (SplitViewFrame.Content as FrameworkElement).RequestedTheme = Windowing.RequestedTheme;
        SplitViewFrame.Background = Windowing.RequestedTheme == ElementTheme.Light ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Colors.Black);
        if (!(SplitViewFrame.Content as FrameworkElement).BaseUri.ToString().Contains("HomePage"))
        {
            (SplitViewFrame.Content as Page).Margin = new(0, 0, 0, 5);
        }
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
        if (ShellVm.LastClickedTreeviewItem != null) 
        {
            ShellVm.LastClickedTreeviewItem.Background = null;
            ShellVm.LastClickedTreeviewItem.IsSelected = false;
            ShellVm.LastClickedTreeviewItem.BorderBrush = null;
        } //Remove old right clicked nodes background
      
        TreeViewItem item = (TreeViewItem)sender;
        item.Background = new SolidColorBrush(new UISettings().GetColorValue(UIColorType.Accent));

        ShellVm.RightTappedNode = (StoryNodeItem)item.DataContext;
        ShellVm.LastClickedTreeviewItem = item; //We can't set the background through righttappednode so we set a reference to the node itself to reset the background later
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
        dragIsValid = true;

        // Report status
        Logger.Log(LogLevel.Trace, $"OnDragItemsStarting exit");
    }

    private void TreeViewItem_OnDragEnter(object sender, DragEventArgs args)
    {
        Logger.Log(LogLevel.Info, $"OnDragEnter enter");
        // Assume the worst
        dragIsValid = true;

        // sender is the node you're dragging over (the prospective target)
        Type type = sender.GetType();
        if (!type.Name.Equals("TreeViewItem"))
        {
            dragIsValid = false;
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
            dragIsValid = false;
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
        else
        {
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

    private void TreeViewItem_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (ShellVm.LastClickedTreeviewItem != null && ShellVm.LastClickedTreeviewItem != (TreeViewItem)sender)
        {
            ShellVm.LastClickedTreeviewItem.Background = null;
            ShellVm.LastClickedTreeviewItem.IsSelected = false;
            ShellVm.LastClickedTreeviewItem.BorderBrush = null;
        }
        ShellVm.LastClickedTreeviewItem = (TreeViewItem)sender;
    }
}