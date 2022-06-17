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
        if (ShellVm.DataSource == null || ShellVm.DataSource.Count == 0) { return; }
        foreach (StoryNodeItem node in ShellVm.DataSource[0]) { node.Background = null; }
    }

    // Drag and Drop related

    private void TreeViewItem_OnDragEnter(object sender, DragEventArgs args)
    {
        base.OnDragEnter(args);
        try
        {   
            Logger.Log(LogLevel.Trace, $"OnDragEnter event");
            var root = NavigationTree.RootNodes[0]; // The StoryExplorer (or StoryNarrator) root
            var trash = NavigationTree.RootNodes[1]; // The Trash root

            // args.OriginalSource is the TreeViewItem you're dragging.
            // There is some weirdness with the second root; if you're dragging from the root 
            // over the Trashcan, args.OriginalSource will be the trashcan TreeViewItem and 
            // sender will be ?
            Type type = args.OriginalSource.GetType();
            if (!type.Name.Equals("TreeViewItem"))
            {
                Logger.Log(LogLevel.Warn, $"Invalid dragSource type: {type.Name}");
                var x = args.GetPosition(dragTargetItem);
                args.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.None;
                args.Handled = true;
                ShellVm.ShowMessage(LogLevel.Warn, "Invalid drag source", false);
                return;
            }
            var item = args.OriginalSource as TreeViewItem;
            var node = NavigationTree.NodeFromContainer(item);

            // Insure that the source is below the TreeView root
            if (node.Depth < 1)
            {
                Logger.Log(LogLevel.Warn, $"dragSource is not below root");
                args.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.None;
                args.Handled = true;
                ShellVm.ShowMessage(LogLevel.Warn, "Invalid drag source", false);
                return;
            }
            while (node.Depth != 0)
            {
                node = node.Parent;
            }
            if (node != root)
            {
                Logger.Log(LogLevel.Warn, $"drag source is not below root");
                args.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.None;
                args.Handled = true;
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
                ShellVm.ShowMessage(LogLevel.Warn, "Invalid drag target", false);
                return;
            }
            item = sender as TreeViewItem;
            node = NavigationTree.NodeFromContainer(item);
            // Insure that the target is the treeview root or below
              if (node.Depth < 0)
            {
                Logger.Log(LogLevel.Warn, $"dragTarget is not below root");
                args.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.None;
                args.Handled = true;
                ShellVm.ShowMessage(LogLevel.Warn, "Invalid drag target", false);
                return;
            }
            while (node.Depth != 0)
            {
                node = node.Parent;
            }
            if (node != root)
            {
                Logger.Log(LogLevel.Warn, $"drag target is not below root");
                args.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.None;
                args.Handled = true;
                ShellVm.ShowMessage(LogLevel.Warn, "Invalid drag target", false);
                return;
            }

            ShellVm.ShowMessage(LogLevel.Info, "Drag and drop successful", true);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }
}