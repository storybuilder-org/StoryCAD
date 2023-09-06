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

namespace StoryCAD.Views;

public sealed partial class Shell
{
    public ShellViewModel ShellVm => Ioc.Default.GetService<ShellViewModel>();
    public UnifiedVM UnifiedVm => Ioc.Default.GetService<UnifiedVM>();
    public PreferencesModel Preferences = GlobalData.Preferences;

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
        Ioc.Default.GetRequiredService<LogService>().GetSystemInfo();
        GlobalData.XamlRoot = Content.XamlRoot;
        GlobalData.StartUpTimer.Stop();
        ShellVm.ShowHomePage();
        ShellVm.ShowConnectionStatus();
        ShellVm.UpdateWindowTitle();
        if (GlobalData.ShowDotEnvWarning) { await ShellVm.ShowDotEnvWarningAsync(); }

        if (!await Ioc.Default.GetRequiredService<WebViewModel>().CheckWebviewState())
        {
            ShellVm._canExecuteCommands = false;
            await Ioc.Default.GetRequiredService<WebViewModel>().ShowWebviewDialog();
            ShellVm._canExecuteCommands = true;
        }

        //Shows changelog if the app has been updated since the last launch.
        if (GlobalData.LoadedWithVersionChange)
        {
            await new Services.Dialogs.Changelog().ShowChangeLog();
        }

        //If StoryCAD was loaded from a .STBX File then instead of showing the Unified menu
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
            return;
        }
        // Source node is valid for move
        Logger.Log(LogLevel.Trace, $"dragSource Name: {dragSourceStoryNode.Name}");
        args.Data.RequestedOperation = DataPackageOperation.Move;
    }

    private void TreeViewItem_OnDragOver(object sender, DragEventArgs args)
    {
        // sender is the node you're dragging over (the prospective target)
        Type type = sender.GetType();
        if (!type.Name.Equals("TreeViewItem"))
        {
            Logger.Log(LogLevel.Warn, $"Invalid dragTarget type: {type.Name}");
            args.Data.RequestedOperation = DataPackageOperation.None;
            ShellVm.ShowMessage(LogLevel.Warn, "Invalid drag target", false);
            return;
        }
        dragTargetItem = sender as TreeViewItem;
        dragTargetNode = NavigationTree.NodeFromContainer(dragTargetItem);
        Logger.Log(LogLevel.Trace, $"dragTarget Depth: {dragTargetNode.Depth}");

        var node = dragTargetNode;
        // Insure the target is not a root or above (yes, there's a -1)
        // (you can't move a root)
        if (node.Depth < 1)
        {
            Logger.Log(LogLevel.Warn, $"dragTarget is not below root");
            args.Data.RequestedOperation = DataPackageOperation.None;
            ShellVm.ShowMessage(LogLevel.Warn, "Invalid drag target", false);
            return;
        }

        dragTargetStoryNode = dragTargetNode.Content as StoryNodeItem;
        Logger.Log(LogLevel.Trace, $"dragTarget Name: {dragTargetStoryNode.Name}");
        Logger.Log(LogLevel.Trace, $"dragTarget type: {dragTargetStoryNode.Type.ToString()}");

        // Insure that the target is not in the trashcan
        while (node.Depth != 0)
        {
            node = node.Parent;
        }
        var root = node.Content as StoryNodeItem;
        if (root.Type == StoryItemType.TrashCan)
        {
            Logger.Log(LogLevel.Warn, $"dragTarget root is TrashCan");
            args.Data.RequestedOperation = DataPackageOperation.None;
            ShellVm.ShowMessage(LogLevel.Warn, "Invalid drag target", false);
            return;
        }

        if (args.Data == null)
        {
            ShellVm.ShowMessage(LogLevel.Info, "Drag and drop failed", true);
            Logger.Log(LogLevel.Error, "Failed to drag n drop - arg.data is null (may be a file and not a story element)");
            return;
        }

        // Move is valid, allow the drop operation.  
        args.Data.RequestedOperation = DataPackageOperation.Move;
    }

    private void TreeView_OnDragLeave(object sender, DragEventArgs e)
    {
        if (e.Data == null)
        {
            ShellVm.ShowMessage(LogLevel.Info, "Drag and drop failed", true);
            Logger.Log(LogLevel.Error, "Failed to drag n drop - e.data is null (may be a file and not a story element)");
            return;
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
        else
        {
            e.Data.RequestedOperation = DataPackageOperation.Move;  // re-enable moves
        }
    }

    private void TreeViewItem_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (ShellVm.LastClickedTreeviewItem != null) 
        {
            ShellVm.LastClickedTreeviewItem.Background = null;
            ShellVm.LastClickedTreeviewItem.IsSelected = false;
            ShellVm.LastClickedTreeviewItem.BorderBrush = null;
        }
        ShellVm.LastClickedTreeviewItem = (TreeViewItem)sender;
    }
}