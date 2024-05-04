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
using Microsoft.UI.Input;
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
    private Point lastPointerPosition;
    private bool dragSourceIsValid;
    private bool isDraggingOverTreeViewItem;
    private bool isOutsideTreeView;
    private bool dragTargetIsValid;
    private StoryNodeItem dragSourceStoryNode;
    private TreeViewItem dragTargetItem;  // used to determine drag and drop direction if peer 
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
        isOutsideTreeView = false; // target may not be in TreeView (i.e, AllowDrop = false)
        dragSourceIsValid = false;
        dragTargetIsValid = false;
        ShellVm.ShowMessage(LogLevel.Info, "Drag starting", false);

        try
        {
            var source = args.Items[0];
            dragSourceStoryNode = source as StoryNodeItem;
            dragSourceIsValid = ShellVm.ValidateDragSource(source);
            if (dragSourceIsValid)
            {
                args.Data.RequestedOperation = DataPackageOperation.None;
                NavigationTree.CanReorderItems = true;
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
        Logger.Log(LogLevel.Trace, $"isDraggingOverTreeViewItem: {isDraggingOverTreeViewItem.ToString()}");
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
        isDraggingOverTreeViewItem = true;

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
            
            // If we have a StoryNodeItem, perform other edits.
            dragTargetIsValid = ShellVm.ValidateDragTarget(dragTargetStoryNode);
            if (!dragTargetIsValid)
            {
                NavigationTree.CanReorderItems = false;
                args.Handled = true;
                //ShellVm.RefreshNavigationTree();
            }
            
            GetMoveDirection(lastPointerPosition, dragTargetItem);
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

        Logger.Log(LogLevel.Trace, $"isDraggingOverTreeViewItem: {isDraggingOverTreeViewItem.ToString()}");
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
        var item = sender as TreeViewItem;
        if (item is null)
        {
            return null; // Indicating that the sender is not a TreeViewItem.
        }
        dragTargetItem = item;

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

        Logger.Log(LogLevel.Trace, $"DragLeave entry");
        isDraggingOverTreeViewItem = false;
        Logger.Log(LogLevel.Trace, $"DragLeave exit");
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
        Logger.Log(LogLevel.Trace, $"isOutsideTreeView: {isOutsideTreeView.ToString()}");
        Logger.Log(LogLevel.Trace, $"isDraggingOverTreeViewItem: {isDraggingOverTreeViewItem.ToString()}");
        Logger.Log(LogLevel.Trace, $"dragTargetIsValid: {dragTargetIsValid.ToString()}");
        Logger.Log(LogLevel.Trace, $"DropResult: {args.DropResult.ToString()}");

        try
        {
            if (isOutsideTreeView) 
            {
                ShellVm.ShowMessage(LogLevel.Warn, "Drag out of Navigation Tree not allowed", true);
            }
            else if (dragSourceIsValid && dragTargetIsValid)
            {
                Logger.Log(LogLevel.Trace, $"Source and Target are valid.");
                ShellVm.MoveStoryNode(dragSourceStoryNode, dragTargetStoryNode, GetMoveDirection(lastPointerPosition, dragTargetItem));
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
        dragSourceIsValid = false;
        isDraggingOverTreeViewItem = false;
        isOutsideTreeView = false;
        dragTargetIsValid = false;
        Logger.Log(LogLevel.Trace, $"OnDragItemsCompleted exit");
    }

    /// <summary>
    /// This is the PointerMoved event for the NavigationTree TreeView.
    /// it gets the current mouse position relative to the Tree4View.
    ///
    /// The mouse position is then used to identify if the mouse was moved
    /// outside the TreeView bounds by calling IsOutsideBoundary. This
    /// method 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void NavigationTree_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        lastPointerPosition = e.GetCurrentPoint((UIElement)sender).Position;
        Logger.Log(LogLevel.Trace, $"Raw pointer position: X={lastPointerPosition.X}, Y={lastPointerPosition.Y}");
        isOutsideTreeView = IsOutsideBoundary(lastPointerPosition, sender as TreeView);
        if (isOutsideTreeView)
        {
            NavigationTree.CanReorderItems = false;
            e.Handled = true;
        }
    }

    /// <summary>
    /// This routine checks if the current mouse position is no longer within the NavigationTree
    /// bounding box.  The mouse position is a Point relative to the TreeView returned from the
    /// TreeView's MouseMoved event.
    ///
    /// The drag operation  which drives the mouse movement is not valid for a drop operation if
    /// the mouse is no longer over the TreeView and thus valid to identify the current TreeViewItem
    /// and the corresponding StoryNodeItem it's bound to. In that case PointerMoved will block
    /// the TreeView node reordering and show the event as handled, triggering the
    /// TreeViewItems_DragItemCompleted event, which will report the error and not perform the drop.
    ///
    /// If the mouse position is valid, it's saved, along with the current TreeViewItem as found
    /// in the TreeViewItem_DragEnter event, for use in TreeViewItems_DragItemCompleted, to complete
    /// the move. In that case it's used in a method similar to IsOutsideBoundary, GetMoveDIrection,
    /// which compares the mouse location relative the current TreeViewItem to see if the mouse is
    /// above, below or over the target TreeViewItem. If the node move is as a peer of the source
    /// (in the list of Children of the two nodes' common Parent), the relative position of the
    /// cursor icon decides where to insert before or after the target node.
    /// </summary>
    /// <param name="position">The Point which is the current mouse position relative to the tree</param>
    /// <param name="treeView">the NavigationTree TreeView UI element</param>
    /// <returns>bool True if the mouse is no longer within NavigationTree's bounding box</returns>
    private bool IsOutsideBoundary(Point position, TreeView treeView)
    {
        Logger.Log(LogLevel.Trace, $"IsOutsideBoundary entry");
        // Calculate the bounds of the TreeView considering its actual size
        var bounds = new Rect(0, 0, treeView.ActualWidth, treeView.ActualHeight);
        double adjustedWidth = NavigationTree.ActualWidth;
        double adjustedHeight = NavigationTree.ActualHeight;

        // Get the TreeView's position relative to the window
        var window = Ioc.Default.GetRequiredService<Windowing>().MainWindow as Window;
        GeneralTransform treeViewTransform = treeView.TransformToVisual(window.Content);
        Point treeViewPosition = treeViewTransform.TransformPoint(new Point(0, 0));

        // Calculate the pointer's position relative to the TreeView
        double pointerXRelativeToTreeView = position.X - treeViewPosition.X;
        double pointerYRelativeToTreeView = position.Y - treeViewPosition.Y;

        // Check if the pointer is outside the TreeView
        bool isOutsideX = pointerXRelativeToTreeView < 0 || pointerXRelativeToTreeView > treeView.ActualWidth;
        bool isOutsideY = pointerYRelativeToTreeView < 0 || pointerYRelativeToTreeView > treeView.ActualHeight;

        Logger.Log(LogLevel.Trace, $"AdjustedWidth: {adjustedWidth}");
        Logger.Log(LogLevel.Trace, $"AdjustedHeight: {adjustedWidth}");        
        Logger.Log(LogLevel.Trace, $"Mouse Position Relative to NavigationTree: X={lastPointerPosition.X}, Y={lastPointerPosition.Y}");
        // Check if the point lies outside the bounds
        bool result = isOutsideX || isOutsideY;
        Logger.Log(LogLevel.Trace, $"Result = {result.ToString()}" );
        return result;
    }

    /// <summary>
    /// The drag cursor icon, which gives feedback on the drag operation, is always 'move'
    /// rather than 'copy', since we're just reordering TreeView nodes, but the drag
    /// direction can be either up or down, and thus the cursor icon may be above or below
    /// the target TreeViewItem. If we're dragging to reorder a node in a list (under the
    /// same parent node), we'll want to know whether to insert the source node before or
    /// after the target node.
    ///
    /// We already have the current mouse position, from NavigationTree_PointerMoved, and
    /// use it to decide if the mouse is outside its bounds (The NavigationTree rectangle)
    /// and thus an invalid move. But if the move is still in bounds, we can use the
    /// mouse position relative to the target TreeViewItem (which we have from
    /// TreeViewItem_DragEnter) to see if we should insert above or below the target node. 
    /// </summary>
    /// <param name="position">The last mouse position when the drop occured</param>
    /// <param name="targetTreeViewItem">The target TreeViewItem (the last TreeViewItem visited)
    /// <returns>DragAndDropDirection (above or below target item)</returns>
private DragAndDropDirection GetMoveDirection(Point position, TreeViewItem targetTreeViewItem)
{
    Logger.Log(LogLevel.Trace, $"GetMoveDirection entry");
    Logger.Log(LogLevel.Trace, $"TreeViewItem Height: {targetTreeViewItem.ActualHeight}");
    Logger.Log(LogLevel.Trace, $"pointer position.X: {position.X}");
    Logger.Log(LogLevel.Trace, $"pointer position.Y: {position.Y}");
    
    // Get the window and transform both the targetTreeViewItem and the pointer position to the same relative coordinates
    var window = Ioc.Default.GetRequiredService<Windowing>().MainWindow as Window;
    GeneralTransform targetTreeViewItemTransform = targetTreeViewItem.TransformToVisual(window.Content);
    Point targetTreeViewItemPosition = targetTreeViewItemTransform.TransformPoint(new Point(0, 0));

    // Transform the pointer position to the window.Content coordinates
    GeneralTransform pointerTransform = window.Content.TransformToVisual(window.Content);
    Point transformedPointerPosition = pointerTransform.TransformPoint(position);

    // Calculate the pointer's position relative to the targetTreeViewItem
    double pointerYRelativeToTreeViewItem = transformedPointerPosition.Y - targetTreeViewItemPosition.Y;

    // Determine if the pointer is above or below the targetTreeViewItem
    bool isOutsideY = pointerYRelativeToTreeViewItem < 0 || pointerYRelativeToTreeViewItem > targetTreeViewItem.ActualHeight;
    DragAndDropDirection direction;
    if (pointerYRelativeToTreeViewItem < 0)
    {
        direction = DragAndDropDirection.AboveTargetItem;
    }
    else if (pointerYRelativeToTreeViewItem > targetTreeViewItem.ActualHeight)
    {
        direction = DragAndDropDirection.BelowTargetItem;
    }
    else
    {
        direction = DragAndDropDirection.OnTargetItem;
    }

    Logger.Log(LogLevel.Trace, $"Mouse Y Position Relative to NavigationTree: Y={transformedPointerPosition.Y}");
    Logger.Log(LogLevel.Trace, $"direction = {direction.ToString()}" );
    Logger.Log
    return direction;
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