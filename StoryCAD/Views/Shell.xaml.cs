using System;
using System.Data.Common;
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
using Windows.Foundation;
using Windows.UI.ViewManagement;
using Windows.ApplicationModel.DataTransfer;
using Microsoft.UI;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ABI.Windows.Graphics.Printing.Workflow;
using StoryCAD.Exceptions;
using Application = Microsoft.UI.Xaml.Application;
using DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue;
using Page = Microsoft.UI.Xaml.Controls.Page;

namespace StoryCAD.Views;

public sealed partial class Shell
{
    public ShellViewModel ShellVm => Ioc.Default.GetService<ShellViewModel>();
    public Windowing Windowing => Ioc.Default.GetService<Windowing>();
    public UnifiedVM UnifiedVm => Ioc.Default.GetService<UnifiedVM>();
    public PreferencesModel Preferences = Ioc.Default.GetRequiredService<AppState>().Preferences;
    public LogService Logger;

    private readonly object dragLock = new object();
    private bool dragSourceIsValid;
    private bool dragTargetAllowed;
    private bool dragTargetIsValid;
    private StoryNodeItem dragSourceStoryNode;
    private StoryNodeItem dragTargetStoryNode;

    public Shell()
    {
        try
        {
            InitializeComponent();
            AllowDrop = false;
            Logger = Ioc.Default.GetService<LogService>();
            DataContext = ShellVm;
            Ioc.Default.GetRequiredService<Windowing>().GlobalDispatcher = DispatcherQueue.GetForCurrentThread();
            Loaded += Shell_Loaded;
        }
        catch (Exception ex)
        {
            // A shell initialization error is fatal
            Logger!.LogException(LogLevel.Error, ex, ex.Message);
            Logger.Flush();
            Application.Current.Exit();  // Win32
        }
        ShellVm.SplitViewFrame = SplitViewFrame;
    }

    private async void Shell_Loaded(object sender, RoutedEventArgs e)
    {
        Windowing.XamlRoot = Content.XamlRoot;
        Ioc.Default.GetService<AppState>()!.StartUpTimer.Stop();
        ShellVm.ShowHomePage();
        ShellVm.ShowConnectionStatus();
        Ioc.Default.GetRequiredService<Windowing>().UpdateWindowTitle();
        if (!Ioc.Default.GetService<AppState>()!.EnvPresent) { await ShellVm.ShowDotEnvWarningAsync(); }

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
        else { await ShellVm.OpenFile(ShellVm.FilePathToLaunch); }
    }

    /// <summary>
    /// Makes the TreeView lose its selection when there is no corresponding main menu item.
    /// </summary>
    /// <remarks>But I don't know why...</remarks>
    private void SplitViewFrame_OnNavigated(object sender, NavigationEventArgs e)
    {
        NavigationTree.SelectionMode = TreeViewSelectionMode.None;
        NavigationTree.SelectionMode = TreeViewSelectionMode.Single;
        ((SplitViewFrame.Content as FrameworkElement)!).RequestedTheme = Windowing.RequestedTheme;
        SplitViewFrame.Background = Windowing.RequestedTheme == ElementTheme.Light ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Colors.Black);
        if (!((FrameworkElement)SplitViewFrame.Content).BaseUri.ToString().Contains("HomePage"))
        {
            ((Page)SplitViewFrame.Content).Margin = new(0, 0, 0, 5);
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
        //Remove old right-clicked node's background
        TreeViewItem item = (TreeViewItem)sender;
        item.Background = new SolidColorBrush(new UISettings().GetColorValue(UIColorType.Accent));

        ShellVm.RightTappedNode = (StoryNodeItem)item.DataContext;
        ShellVm.LastClickedTreeviewItem = item; //We can't set the background through RightTappedNode so
                                                //we set a reference to the node itself to reset the background later
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
    /// Initiates a drag operation for an item in a TreeView. It validates the drag source,
    /// and sets up for a move operation if valid. If the source is invalid, the drag operation is canceled.
    ///</summary>
    /// <param name="sender">The TreeView that is the source of the drag event.</param>
    /// <param name="args">Event data containing information about the drag items starting operation.</param>
    private void TreeView_DragItemsStarting(TreeView sender, TreeViewDragItemsStartingEventArgs args)
    {
        Logger.Log(LogLevel.Trace, "OnDragItemsStarting enter");
        // Don't let the default move or copy events take place
        args.Data.RequestedOperation = DataPackageOperation.None;
        // Assume the worst
        dragTargetAllowed = false;  // target may not be in TreeView (i.e, AllowDrop = false)
        dragSourceIsValid = false;
        dragTargetIsValid = false;

        try
        {
            var source = args.Items[0];
            dragSourceStoryNode = source as StoryNodeItem;
            dragSourceIsValid = ShellVm.ValidateDragSource(source);
            if (dragSourceIsValid)
            {
                args.Data.RequestedOperation = DataPackageOperation.None;
            }
            else
            {
                args.Cancel = true;
                NavigationTree.CanReorderItems = false;
                //ShellVm.RefreshNavigationTree();
            }

        }
        catch (InvalidDragSourceException ex)
        {
            Logger.LogException(LogLevel.Warn, ex, "Exception in DragItemsStarting");
            ShellVm.ShowMessage(LogLevel.Warn, ex.Message, false);
            args.Cancel = true;
            //ShellVm.RefreshNavigationTree();
        }

        Logger.Log(LogLevel.Trace, $"dragSourceIsValid: {dragSourceIsValid.ToString()}");
        Logger.Log(LogLevel.Trace, $"dragTargetAllowed: {dragTargetAllowed.ToString()}");
        Logger.Log(LogLevel.Trace, "OnDragItemsStarting exit");
    }

    /// <summary>
    /// Handles the DragEnter event for TreeViewItem, setting the operation based on the sender's validity.
    /// </summary>
    /// <param name="sender">The source of the event, expected to be a TreeViewItem.</param>
    /// <param name="args">Event data containing drag-and-drop information.</param>
    private void TreeViewItem_DragEnter(object sender, DragEventArgs args)
    {
        Logger.Log(LogLevel.Trace, "OnDragEnter enter");
        // Assume the worst:
        dragTargetIsValid = false;
        // If we're in DragEnter, the target is in the TreeView
        dragTargetAllowed = true;

        try
        {
            StoryNodeItem node = GetNodeFromTreeViewItem(sender, args);
            if (node == null)
            {
                // Handle the null case: log warning, set dragTargetIsValid, etc.
                ShellVm.ShowMessage(LogLevel.Warn, "Failed to retrieve StoryNodeItem.", true);
                dragTargetIsValid = false;
                NavigationTree.CanReorderItems = false;
                args.Handled = true;
                //ShellVm.RefreshNavigationTree();
                return;
            }

            //if (node.Type == StoryItemType.TrashCan)
            //{
            //    ShellVm.ShowMessage(LogLevel.Warn, "Drag to Trashcan invalid", true);
            //    dragTargetIsValid = false;
            //    var item = sender as TreeViewItem;
            //    NavigationTree.CanReorderItems = false;
            //    args.Handled = true;
            //    return;
            //}


            // If we have a StoryNodeItem, perform other edits.
            dragTargetIsValid = ShellVm.ValidateDragTarget(dragTargetStoryNode);
            if (!dragTargetIsValid)
            {
                NavigationTree.CanReorderItems = false;
                args.Handled = true;
                //ShellVm.RefreshNavigationTree();
            }
        }
        catch (Exception ex)
        {
            Logger.LogException(LogLevel.Warn, ex, "Exception in DragEnter");
            ShellVm.ShowMessage(LogLevel.Warn, ex.Message, false);
            args.Handled = true;
            NavigationTree.CanReorderItems = false;
            //ShellVm.RefreshNavigationTree();
        }

        // if the drag location is valid, indicate so. The actual move only 
        // occurs when the user releases the mouse button (the Drop event).
        // We can't get Drop to fire, so we use DragItemsCompleted instead.
        args.AcceptedOperation = DataPackageOperation.None;
        args.Handled = true;

        Logger.Log(LogLevel.Trace, $"dragTargetAllowed: {dragTargetAllowed.ToString()}");
        Logger.Log(LogLevel.Trace, $"dragTargetIsValid: {dragTargetIsValid.ToString()}");
        Logger.Log(LogLevel.Trace, "OnDragEnter exit");
    }

    /// <summary>
    /// Gets the StoryNodeItem that a TreeViewNode, represented by the sender, is bound to. This method is typically
    /// called from a DragEnter event. It checks if the sender is a TreeViewItem and attempts to retrieve the corresponding
    /// StoryNodeItem. If the sender is not a TreeViewItem, a null value is returned, indicating the failure to retrieve the
    /// StoryNodeItem. It's the caller's responsibility to handle this null return appropriately, possibly by logging a warning,
    /// marking the drag as invalid, and refreshing the navigation tree.
    /// </summary>
    /// <param name="sender">The source of the DragEnter event, expected to be a TreeViewItem.</param>
    /// <param name="args">The DragEventArgs associated with the DragEnter event.</param>
    /// <returns>The StoryNodeItem that the TreeViewNode is bound to, or null if the sender is not a TreeViewItem or if the
    /// content cannot be successfully retrieved or cast.</returns>
    private StoryNodeItem GetNodeFromTreeViewItem(object sender, DragEventArgs args)
    {
        Logger.Log(LogLevel.Trace, $"GetNodeFromTreeViewItem entry");
        // Ensure the sender is a TreeViewItem. If not, return null.
        if (!(sender is TreeViewItem dragTargetItem))
        {
            return null; // Indicating that the sender is not a TreeViewItem.
        }

        // Attempt to get the node and its content.
        var dragTargetNode = NavigationTree.NodeFromContainer(dragTargetItem);
        dragTargetStoryNode = dragTargetNode?.Content as StoryNodeItem;

        // If the node is a TrashCan, update the TreeViewItem to not allow
        // a drop.


        // Return the retrieved StoryNodeItem, or null if the cast failed or the node was not found.
        Logger.Log(LogLevel.Trace, $"GetNodeFromTreeViewItem exit");
        return dragTargetStoryNode;
    }


    /// <summary>
    /// Handles the DragLeave event for a TreeView control. This method calculates whether the dragged item
    /// has left the bounds of the TreeView, considering adjustments for border thickness, padding, and margin.
    /// It logs the relevant layout properties and the position of the mouse relative to the TreeView to help
    /// diagnose issues related to drag-and-drop operations. If the dragged item is detected outside the
    /// adjusted bounds, it logs and prints a confirmation message.
    /// </summary>
    /// <param name="sender">The source of the event, typically the TreeView control.</param>
    /// <param name="e">Drag event arguments containing the state and information about the drag event,
    /// including the current position of the mouse pointer.</param>
    /// <remarks>
    /// This method assumes that the sender is a TreeView and that it will receive border thickness,
    /// padding, and margin values directly from the TreeView's properties. Ensure that the logger is
    /// appropriately configured to capture trace-level messages. The effectiveness of the boundary
    /// checks performed by this method is dependent on accurate settings for the TreeView's layout properties.
    /// </remarks>
    private void TreeViewItem_DragLeave(object sender, DragEventArgs e)
    {
        lock (dragLock)
        {
            Logger.Log(LogLevel.Trace, $"DragLeave entry");
            // Retrieve border thickness, padding, and margin directly
            Thickness borderThickness = NavigationTree.BorderThickness;
            Thickness padding = NavigationTree.Padding;
            Thickness margin = NavigationTree.Margin;

            // Calculate total horizontal and vertical adjustments
            double horizontalAdjustment = borderThickness.Left + borderThickness.Right + padding.Left + padding.Right + margin.Left + margin.Right;
            double verticalAdjustment = borderThickness.Top + borderThickness.Bottom + padding.Top + padding.Bottom + margin.Top + margin.Bottom;

            // Log the retrieved values
            Logger.Log(LogLevel.Trace, $"Border Thickness: {borderThickness}, Padding: {padding}, Margin: {margin}");
            Logger.Log(LogLevel.Trace, $"Total Horizontal Adjustment: {horizontalAdjustment}, Total Vertical Adjustment: {verticalAdjustment}");

            // Get the position of the mouse relative to the NavigationTree
            Point position = e.GetPosition(NavigationTree);

            // Log the position of the mouse
            Logger.Log(LogLevel.Trace, $"Mouse Position Relative to NavigationTree: X={position.X}, Y={position.Y}");

            // Adjust NavigationTree bounds to consider borders, padding, and margins
            double adjustedWidth = NavigationTree.ActualWidth + horizontalAdjustment;
            double adjustedHeight = NavigationTree.ActualHeight + verticalAdjustment;
            Logger.Log(LogLevel.Trace, $"AdjustedWidth: {adjustedWidth}");
            Logger.Log(LogLevel.Trace, $"AdjustedHeight: {adjustedWidth}");
            Logger.Log(LogLevel.Trace, $"position.X: {position.X}");
            Logger.Log(LogLevel.Trace, $"position.Y: {position.Y}");
            
            // Check if the position is outside the bounds of the adjusted NavigationTree
            if (position.X < -horizontalAdjustment / 2 || position.Y < -verticalAdjustment / 2 ||
                position.X > NavigationTree.ActualWidth + horizontalAdjustment / 2 || position.Y > NavigationTree.ActualHeight + verticalAdjustment / 2)
            {
                Logger.Log(LogLevel.Trace, "Dragged item outside of NavigationTree");
                Console.WriteLine("Dragged item outside of NavigationTree");
            }
            else
            {
                Logger.Log(LogLevel.Trace, "Dragged item still inside TreeView bounds");
            }
            Logger.Log(LogLevel.Trace, $"DragLeave exit");
        }
    }

    /// <summary>
    /// Handles the DragItemsCompleted event for TreeViewItem. This event will complete the drag and drop
    /// move if both the source and target for the drag and drop are valid.
    ///
    /// We set 
    /// </summary>
    /// <param name="sender">The TreeView itself.</param>
    /// <param name="args">Event data containing drag-and-drop information.</param>
    private void TreeView_DragItemsCompleted(TreeView sender, TreeViewDragItemsCompletedEventArgs args)
    {
        Logger.Log(LogLevel.Trace, $"DragItemsCompleted entry");
        Logger.Log(LogLevel.Trace, $"dragSourceIsValid: {dragSourceIsValid.ToString()}");
        Logger.Log(LogLevel.Trace, $"dragTargetAllowed: {dragTargetAllowed.ToString()}");
        Logger.Log(LogLevel.Trace, $"dragTargetIsValid: {dragTargetIsValid.ToString()}");
        Logger.Log(LogLevel.Trace, $"DropResult: {args.DropResult.ToString()}");

        try
        {
            if (dragSourceIsValid && dragTargetIsValid)
            {
                if (!dragTargetAllowed)
                {
                    ShellVm.ShowMessage(LogLevel.Warn, "Drag out of Navigation Tre not allowed", true);
                }
                else
                {
                    Logger.Log(LogLevel.Trace, $"Source and Target are valid.");
                    ShellVm.MoveStoryNode(dragSourceStoryNode, dragSourceStoryNode);
                }
            }
        }
        catch (InvalidDragDropOperationException ex)
        {
            Logger.LogException(LogLevel.Warn, ex, "Error completing drag and drop");
            ShellVm.ShowMessage(LogLevel.Warn, ex.Message, false);
            //ShellVm.RefreshNavigationTree();
        }

        NavigationTree.CanDrag = true;
        NavigationTree.AllowDrop = true;
        NavigationTree.CanReorderItems = true;
        Logger.Log(LogLevel.Trace, $"OnDragItemsCompleted exit");
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