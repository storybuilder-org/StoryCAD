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
using StoryCAD.ViewModels;
using Windows.UI.ViewManagement;
using Microsoft.UI.Dispatching;
using Windows.ApplicationModel.DataTransfer;
using Microsoft.UI;
using Application = Microsoft.UI.Xaml.Application;
using Page = Microsoft.UI.Xaml.Controls.Page;

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
    private LogService Logger;

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
        } 
        //Remove old right clicked nodes background
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
        Logger.Log(LogLevel.Trace, $"OnDragDragItemsStarting event");

        // args.Items[0] is the TreeViewItem you're dragging.
        // With SelectionMode="Single" there will be only the one.
        Type type = args.Items[0].GetType();
        if (!type.Name.Equals("StoryNodeItem"))
        {
            Logger.Log(LogLevel.Warn, $"Invalid dragSource type: {type.Name}");
            args.Data.RequestedOperation = DataPackageOperation.None;
            ShellVm.ShowMessage(LogLevel.Warn, "Invalid drag source", false);
            return;
        }

        dragSourceStoryNode = args.Items[0] as StoryNodeItem;
        StoryNodeItem _parent = dragSourceStoryNode.Parent;
        if (_parent == null)
        {
            Logger.Log(LogLevel.Warn, $"dragSource is not below root");
            args.Data.RequestedOperation = DataPackageOperation.None;
            ShellVm.ShowMessage(LogLevel.Warn, "Invalid drag source", false);
            return;
        }
        while (!_parent.IsRoot) // find the root
        {
            _parent = _parent.Parent;
        }
        if (_parent != null && _parent.Type == StoryItemType.TrashCan)
        {
            Logger.Log(LogLevel.Warn, $"dragSource root is TrashCan");
            args.Data.RequestedOperation = DataPackageOperation.None;
            ShellVm.ShowMessage(LogLevel.Warn, "Invalid drag source", false);
            NavigationTree.AllowDrop = false;
            return;
        }
        // Source node is valid for move
        Logger.Log(LogLevel.Trace, $"dragSource Name: {dragSourceStoryNode.Name}");
        args.Data.RequestedOperation = DataPackageOperation.Move;
    }

    private void TreeViewItem_OnDragOver(object sender, DragEventArgs args)
    {
        if (ShellVm.invalid_dnd_state)
        {
            args.AcceptedOperation = DataPackageOperation.None;
            args.Handled = true;
            NavigationTree.AllowDrop = false;
            return;
        }

        var dragTargetItem = sender as TreeViewItem;
        if (dragTargetItem == null)
        {
            args.Data.RequestedOperation = DataPackageOperation.None;
            ShellVm.ShowMessage(LogLevel.Warn, "Invalid drag target.", false);
            return;
        }

        var dragTargetNode = NavigationTree.NodeFromContainer(dragTargetItem);
        var dragTargetStoryNode = dragTargetNode?.Content as StoryNodeItem;

        if (dragSourceStoryNode == null || dragTargetStoryNode == null || ShellVm.IsInvalidMove(dragSourceStoryNode, dragTargetStoryNode, args))
        {
            
            args.AcceptedOperation = DataPackageOperation.None;
			if (args.Data != null) 
			{
				args.Data.RequestedOperation = DataPackageOperation.None;
			}

            args.DataView.ReportOperationCompleted(DataPackageOperation.None);
            args.Handled = true;
            NavigationTree.AllowDrop = false;
            ShellVm.invalid_dnd_state = true;
            ShellVm.ShowMessage(LogLevel.Warn, "Cannot move a node there.", false);

            return;
        }

        args.Data.RequestedOperation = DataPackageOperation.Move;
    }

    private void TreeView_OnDragLeave(object sender, DragEventArgs e)
    {
        if (ShellVm.invalid_dnd_state)
        {
            e.AcceptedOperation = DataPackageOperation.None;
            e.Data.RequestedOperation = DataPackageOperation.None;
            e.Handled = true;
            NavigationTree.AllowDrop = false;
        }

        // If the drag target identified in OnDragOver was valid, 
        // the drag operation is allowed. Indicate a change too place
        // and report it. 
        // If the drag target was not valid, the drag operation didn't
        // take place. Re-enable move operations for another try.
        if (e.Data.RequestedOperation == DataPackageOperation.Move)
        {
            ShellViewModel.ShowChange();
            ShellVm.ShowMessage(LogLevel.Info, "Drag and drop successful", true);
        }
        e.Data.RequestedOperation = DataPackageOperation.Move;  // re-enable moves

        //Reset DNDBlock and ensure AllowDrop is enabled
        ShellVm.invalid_dnd_state = false;
        NavigationTree.AllowDrop = true;
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