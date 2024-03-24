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
using Windows.ApplicationModel.DataTransfer;
using Microsoft.UI;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
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

    private StoryNodeItem dragTargetStoryNode;
    private StoryNodeItem dragSourceStoryNode;
    private bool dragSourceIsValid;
    private bool dragTargetIsValid;

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
    /// Initiates a drag operation for an item in a TreeView. It logs the drag start event, validates the drag source,
    /// and sets up for a move operation if valid. If the source is invalid, the drag operation is canceled.
    ///</summary>
    /// <param name="sender">The TreeView that is the source of the drag event.</param>
    /// <param name="args">Event data containing information about the drag items starting operation.</param>
    private void TreeView_DragItemsStarting(TreeView sender, TreeViewDragItemsStartingEventArgs args)
    {
        Logger.Log(LogLevel.Trace, "OnDragItemsStarting enter");
        // Assume the worst
        args.Data.RequestedOperation = DataPackageOperation.None;
        dragTargetStoryNode = null;   // set in case OnDragEnter is not fired 
        
        dragSourceIsValid = ShellVm.ValidateDragSource(args);
        if (dragSourceIsValid) 
        { 
            args.Data.RequestedOperation = DataPackageOperation.Move; 
        }
        else
            args.Cancel = true;
    }

    /// <summary>
    /// Handles the DragEnter event for TreeViewItem, setting the operation based on the sender's validity.
    /// </summary>
    /// <param name="sender">The source of the event, expected to be a TreeViewItem.</param>
    /// <param name="args">Event data containing drag-and-drop information.</param>

     private void TreeView_DragItemsCompleted(TreeView sender, TreeViewDragItemsCompletedEventArgs args)
    {
        Logger.Log(LogLevel.Trace, $"DragItemsCompleted entry");
        Logger.Log(LogLevel.Trace, $"dragSourceIsValid: {dragSourceIsValid.ToString()}");

        //if (dragSourceStoryNode! == null)
        //{
        //    ShellVm.ShowMessage(LogLevel.Warn, "Invalid source node", true);
        //}
        //else if (dragTargetStoryNode! == null)
        //{
        //    ShellVm.ShowMessage(LogLevel.Warn, "Invalid target node", true);
        //}
        //else if (dragSourceStoryNode.Uuid == dragTargetStoryNode.Uuid)
        //{
        //    ShellVm.ShowMessage(LogLevel.Warn, "Drag to self", true);
        //}
        
        //else
        {
            if (dragSourceIsValid && dragTargetIsValid)
            {
                ShellVm.MoveStoryNode();
                ShellVm.ShowMessage(LogLevel.Info, "Drag and drop successful", true);
            }
        }

        NavigationTree.CanDrag = true;
        NavigationTree.AllowDrop = true;
        Logger.Log(LogLevel.Trace, $"OnDragItemsCompleted exit");
    } 

    /// <summary>
    /// Handles the DragEnter event for TreeViewItem, setting the operation based on the sender's validity.
    /// </summary>
    /// <param name="sender">The source of the event, expected to be a TreeViewItem.</param>
    /// <param name="args">Event data containing drag-and-drop information.</param>
    private void TreeViewItem_DragEnter(object sender, DragEventArgs args)
    {
        Logger.Log(LogLevel.Trace, "OnDragEnter enter");
        
        dragTargetIsValid = ShellVm.ValidateDragTarget(sender, args, NavigationTree);

         
        // Use IsInvalidMove to check validity
        // The first two tests are unnecessary?
        if (!ShellVm.ValidateDragAndDrop(dragSourceStoryNode, dragTargetStoryNode))
        {
            args.Handled = true;
            return;
        }
 
        // if the drag location is valid, indicate so. The actual move only 
        // occurs when the user releases the mouse button (the Drop event).
        args.AcceptedOperation = DataPackageOperation.Move;
        //args.Handled = true; // Mark the event as handled.
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
        //if (ShellVm.invalid_dnd_state)
        //{
        //    e.Handled = true;
        //    ShellVm.invalid_dnd_state = false;
        //}
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