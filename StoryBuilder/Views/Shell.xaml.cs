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
using StoryBuilder.ViewModels;

namespace StoryBuilder.Views;

public sealed partial class Shell
{
    public ShellViewModel ShellVm => Ioc.Default.GetService<ShellViewModel>();
    public UnifiedVM UnifiedVm => Ioc.Default.GetService<UnifiedVM>();
    public PreferencesModel Preferences = GlobalData.Preferences;

    private TreeViewItem dragSource = null;
    public Shell()
    {
        try
        {
            InitializeComponent();
            DataContext = ShellVm;
            Loaded += Shell_Loaded;
        }                         
        catch (Exception ex)
        {
            // A shell initialization error is fatal
            LogService log = Ioc.Default.GetService<LogService>();
            log.LogException(LogLevel.Error, ex, ex.Message);
            log.Flush();
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
    private void TreeViewItem_DragEnter(object sender, DragEventArgs args)
    {
        //// disallow starting a drag from the trashcan
        //// make all trashcan entries invalid targets
        //base.OnDragEnter(args);
        //// sender is the object being dragged
        //// args.OriginalSource appears to be the target you're hovering over
        //dragSource = args.OriginalSource as TreeViewItem;  
       
    }

    private void TreeViewItem_OnDrop(object sender, DragEventArgs args)
    {
        //string x = args.ToString();
    }

    //private void TreeViewItem_DropCompleted(UIElement sender, DropCompletedEventArgs args)
    //{
    //    string x = args.ToString();
    //}

    private void TreeViewItem_OnDragOver(object sender, DragEventArgs args)
    {
        //// sender is the item you are currently hovering over 
        //Type type = sender.GetType();
        //if (!type.Name.Equals("TreeViewItem"))
        //{
        //    System.Diagnostics.Debug.WriteLine($" Invalid target type {type.Name}");
        //    args.Handled = true;
        //    args.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.None;
        //    return;
        //}
        //TreeViewItem item = (TreeViewItem)sender;
        //item.AllowDrop = false;
        ////Console.WriteLine($"Entered for {item.Content}");
        //base.OnDragOver(args);
        //var node = NavigationTree.NodeFromContainer(item);
        
        //System.Diagnostics.Debug.WriteLine($"node  = {node.Content}");
        //while (node.Depth != 0) 
        //{ 
        //    node = node.Parent; 
        //}
        //StoryNodeItem storyNode = (StoryNodeItem) node.Content;
        //System.Diagnostics.Debug.WriteLine($"StoryNode Root = {storyNode.Name}");
        
        //if (storyNode.Type == StoryItemType.TrashCan)
        //{
        //    System.Diagnostics.Debug.WriteLine($"Root = Trash can");
        //    item.AllowDrop = false;
        //    args.Handled = true;
        //    args.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.None;      
        //    return;
        //}
        //System.Diagnostics.Debug.WriteLine($"Allowing drop for {storyNode.Name}");
        //item.AllowDrop = true;
        ////args.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Move;
    }

    private void OnDragItemsStarting(TreeView sender, TreeViewDragItemsStartingEventArgs args)
    {
        TreeViewNode trashNode = sender.RootNodes[1];
        var trashItem = NavigationTree.ItemFromContainer(trashNode);
    }
}