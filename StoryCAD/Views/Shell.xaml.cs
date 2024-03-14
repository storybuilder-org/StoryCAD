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
    public LogService Logger;

    private object lastDropTarget;
    private StoryNodeItem dragTargetStoryNode;
    private StoryNodeItem dragSourceStoryNode;

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
    
#region DragandDropOperations
   
    ///<summary>
    /// Initiates a drag operation for an item in a TreeView. It logs the drag start event, validates the drag source, and sets up for a move operation if valid. If the source is invalid, the drag operation is canceled.
    ///</summary>
    /// <param name="sender">The TreeView that is the source of the drag event.</param>
    /// <param name="args">Event data containing information about the drag items starting operation.</param>
    private void TreeView_DragItemsStarting(TreeView sender, TreeViewDragItemsStartingEventArgs args)
    {
        Logger.Log(LogLevel.Trace, "OnDragItemsStarting event");
        if (args.Items[0] is not StoryNodeItem dragSourceStoryNode)
        {
            SetInvalidDragDropState("Invalid drag source", LogLevel.Warn, true);
            args.Cancel = true; // Cancel the drag operation
            return;
        }

        // Initialization for a valid drag operation
        this.dragSourceStoryNode = dragSourceStoryNode;
        args.Data.RequestedOperation = DataPackageOperation.Move;
    }

    
    ///<summary>
    /// Handles the drag-over event on a TreeViewItem to determine if the dragged item can be dropped onto the target item.
    /// It validates the target item and checks if the move operation is valid. If not valid, the operation is canceled and the event is marked as handled.
    ///</summary>
    /// <param name="sender">The object that is the source of the event, typically a TreeViewItem.</param>
    /// <param name="args">Event data that provides information about the drag operation.</param>
    private void TreeViewItem_OnDragOver(object sender, DragEventArgs args)
    {
        // OnDragOver fires repeatedly for even small movements within the
        // target area, even when it's still on the same TreeViewItem.
        // Check if the current target node is the same as the last target node.
        // The event needs processed only if it's a different node.
        if (sender == lastDropTarget)
        {
            // If yes, do nothing
            return;
        }
        else
            lastDropTarget = sender;
        
        Logger.Log(LogLevel.Trace, "OnDragOver event");

        // Test if TreeViewItem?
        var currentDropTargetItem = sender as TreeViewItem;
     
        if (currentDropTargetItem == null)
        {
            SetInvalidDragDropState("Invalid drag target.");
            args.Handled = true;
            return;
        }

        GetCurrentTargetStoryNode(currentDropTargetItem); // Get the drag target's StoryNodeItem

        // Use IsInvalidMove to check validity
        // The first two tests are unnecessary?
        if (dragSourceStoryNode == null || dragTargetStoryNode == null || ShellVm.IsInvalidMove(dragSourceStoryNode, dragTargetStoryNode))
        {
            SetInvalidDragDropState("Cannot move a node here.");
            args.Handled = true;
            return;
        }

        // if the drag location is valid, indicate so. The actual move only 
        // occurs when the user releases the mouse button.
        Logger.Log(LogLevel.Trace, "Drop location is valid");
        args.Data.RequestedOperation = DataPackageOperation.Move;
    }

    /// <summary>
    /// The drag target (OnDragOver's sender argument) is typically a TreeViewItem.
    /// The TreeView (NavigationTree) is bound to an ObservableCollection of
    /// StoryNodeItems (ShellViewModel.DataSource). Each StoryNodeItem
    /// can be thought of as the ViewModel to a corresponding TreeViewItem's View.
    ///
    /// This routine finds the corresponding StoryNodeItem.
    /// </summary>
    /// <param name="dragTargetItem"></param>
    private void GetCurrentTargetStoryNode(TreeViewItem dragTargetItem)
    {
        var dragTargetNode = NavigationTree.NodeFromContainer(dragTargetItem);
        dragTargetStoryNode = dragTargetNode?.Content as StoryNodeItem;
        Logger.Log(LogLevel.Trace, $"  Target node:{dragTargetStoryNode?.Name ?? "null"}");
    }

    ///<summary>
    /// Processes the drag leave event on a TreeView. It re-enables dropping for
    /// future operations and resets any invalid drag-and-drop state flags.
    ///</summary>
    /// <param name="sender">The TreeView that is the source of the event.</param>
    /// <param name="e">Event data that provides information about the drag operation.</param>
    private void TreeView_OnDragLeave(object sender, DragEventArgs e)
    {
        // Is the if statement even necessary?
        Logger.Log(LogLevel.Trace, "OnDragLeave event");
        if (ShellVm.invalid_dnd_state)
        {
            e.Handled = true;
            NavigationTree.AllowDrop = true; // Re-enable for next operation
            ShellVm.invalid_dnd_state = false;
        }
    }
    
    
    /// <summary>
    /// Sets the application's drag-and-drop state to invalid, logs the specified message at the given log level, 
    /// optionally displays a message to the user, and disables drag-and-drop in the navigation tree.
    /// </summary>
    /// <param name="message">The message to log and optionally display.</param>
    /// <param name="logLevel">The severity level of the log message. Defaults to LogLevel.Warn.</param>
    /// <param name="showMessage">Determines whether the message should be displayed to the user. Defaults to false.</param>
    private void SetInvalidDragDropState(string message, LogLevel logLevel = LogLevel.Warn, bool showMessage = false)
    {
        ShellVm.invalid_dnd_state = true;
        if (showMessage)
        {
            ShellVm.ShowMessage(logLevel, message, false);
        }
        Logger.Log(logLevel, message);
        NavigationTree.AllowDrop = false;
    }

    #endregion
 
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