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
using StoryBuilder.Services.Messages;
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
    private TreeViewItem dragSourceItem;
    private TreeViewNode dragSourceNode;
    private StoryNodeItem dragSourceStoryNode;
    private LogService Logger;

    public Shell()
    {
        try
        {
            InitializeComponent();
            Logger = Ioc.Default.GetService<LogService>();
            DataContext = ShellVm;
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
        Logger.Log(LogLevel.Trace, $"OnDragEnter event");
     
        // args.OriginalSource is the TreeViewItem you're dragging
        Type type = args.OriginalSource.GetType();
        if (!type.Name.Equals("TreeViewItem"))
        {
            Logger.Log(LogLevel.Warn, $"Invalid dragSource type: {type.Name}");
            args.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.None;
            args.Handled = true;
            base.OnDragEnter(args);
            ShellVm.ShowMessage(LogLevel.Warn, "Invalid drag source", false);
            return;
        }
        dragSourceItem = args.OriginalSource as TreeViewItem;
        dragSourceNode = NavigationTree.NodeFromContainer(dragSourceItem);
        Logger.Log(LogLevel.Trace, $"dragSource Depth: {dragSourceNode.Depth}");    

        var node = dragSourceNode;
        // Insure the source is not a root or above
       if (node.Depth < 1)
        {
            Logger.Log(LogLevel.Warn, $"dragSource is not below root");
            args.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.None;
            args.Handled = true;
            base.OnDragEnter(args);
            ShellVm.ShowMessage(LogLevel.Warn, "Invalid drag source", false);
            return;
        }
       
        dragSourceStoryNode = dragSourceNode.Content as StoryNodeItem;
        Logger.Log(LogLevel.Trace, $"dragSource Name: {dragSourceStoryNode.Name}");
        Logger.Log(LogLevel.Trace, $"dragSource type: {dragSourceStoryNode.Type.ToString()}");

        // Insure that the source is not in the trashcan
        while (node.Depth != 0)
        {
            node = node.Parent;
        }
        var root = node.Content as StoryNodeItem;
        if (root.Type == StoryItemType.TrashCan)
        {
            Logger.Log(LogLevel.Warn, $"dragSource root is TrashCan");
            args.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.None;
            args.Handled = true;
            base.OnDragEnter(args);
            ShellVm.ShowMessage(LogLevel.Warn, "Invalid drag source", false);
            return;
        }

        // sender is the node you're dragging over (the prospective target)
        type = sender.GetType();
        if (!type.Name.Equals("TreeViewItem"))
        {
            Logger.Log(LogLevel.Warn, $"Invalid dragTarget type: {type.Name}");
            args.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.None;
            args.Handled = true;
            base.OnDragEnter(args);
            ShellVm.ShowMessage(LogLevel.Warn, "Invalid drag target", false);
            return;
        }
        dragTargetItem = sender as TreeViewItem;
        dragTargetNode = NavigationTree.NodeFromContainer(dragTargetItem);
        Logger.Log(LogLevel.Trace, $"dragTarget Depth: {dragTargetNode.Depth}");
        
        node = dragTargetNode;
        // Insure the target is not a root or above (yes, there's a -1)
        // (you can't move a root)
        if (node.Depth < 1)
        {
            Logger.Log(LogLevel.Warn, $"dragTarget is not below root");
            args.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.None;
            args.Handled = true;
            base.OnDragEnter(args);
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
        root = node.Content as StoryNodeItem;
        if (root.Type == StoryItemType.TrashCan)
        {
            Logger.Log(LogLevel.Warn, $"dragTarget root is TrashCan");
            args.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.None;
            args.Handled = true;
            base.OnDragEnter(args);
            ShellVm.ShowMessage(LogLevel.Warn, "Invalid drag target", false);
            return;
        }
        ShellVm.ShowMessage(LogLevel.Info, "Drag and drop successful", true);
        base.OnDragEnter(args);
    }
    
}