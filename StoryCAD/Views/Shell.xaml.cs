using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using StoryCAD.Models.Tools;
using StoryCAD.Services.Logging;
using Windows.Foundation;
using Windows.UI.ViewManagement;
using Windows.ApplicationModel.DataTransfer;
using Microsoft.UI;
using StoryCAD.Services.Collaborator;
using StoryCAD.Exceptions;
using StoryCAD.Services;
using StoryCAD.Services.Dialogs;
using StoryCAD.Services.Ratings;
using StoryCAD.ViewModels.SubViewModels;
using StoryCAD.ViewModels.Tools;
using StoryCAD.Services.Backup;
using StoryCAD.Services.Locking;

namespace StoryCAD.Views;

public sealed partial class Shell
{
    public ShellViewModel ShellVm => Ioc.Default.GetService<ShellViewModel>();
    public Windowing Windowing => Ioc.Default.GetService<Windowing>();
    public OutlineViewModel OutlineVM => Ioc.Default.GetService<OutlineViewModel>();
    public LogService Logger;
    public PreferencesModel Preferences = Ioc.Default.GetRequiredService<PreferenceService>().Model;

    private Point lastPointerPosition;
    private bool dragSourceIsValid;
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
            Ioc.Default.GetRequiredService<Windowing>().GlobalDispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
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

        if (await Ioc.Default.GetService<CollaboratorService>()!.CollaboratorEnabled())
        {
            Ioc.Default.GetService<CollaboratorService>()!.ConnectCollaborator();
            ShellVm.CollaboratorVisibility = Visibility.Visible;
        }
        else
            ShellVm.CollaboratorVisibility = Visibility.Collapsed;

        ShellVm.ShowHomePage();
        ShellVm.ShowConnectionStatus();
        Windowing.UpdateWindowTitle();
        if (!Ioc.Default.GetService<AppState>()!.EnvPresent &&
            !Preferences.HideKeyFileWarning)
        {
            await ShellVm.ShowDotEnvWarningAsync();
        }

        if (!await Ioc.Default.GetRequiredService<WebViewModel>().CheckWebViewState())
        {
            var autoSaveService = Ioc.Default.GetRequiredService<AutoSaveService>();
            var backupService = Ioc.Default.GetRequiredService<BackupService>();
            var logService = Ioc.Default.GetRequiredService<LogService>();
            using (var serializationLock = new SerializationLock(autoSaveService, backupService, logService))
            {
                await Ioc.Default.GetRequiredService<WebViewModel>().ShowWebViewDialog();
            }

        }
        //Shows changelog if the app has been updated since the last launch.
        if (Ioc.Default.GetRequiredService<AppState>().LoadedWithVersionChange)
        {
			Ioc.Default.GetService<PreferenceService>()!.Model.HideRatingPrompt = false;  //rating prompt re-enabled on updates.
			await new Changelog().ShowChangeLog();
        }

        if (Preferences.ShowStartupDialog)
        {
                ContentDialog cd = new()
                {
                        Title = "Need help getting started?",
                        Content = new HelpPage(),
                        PrimaryButtonText = "Close",
                };
                await Ioc.Default.GetRequiredService<Windowing>().ShowContentDialog(cd);
                }

        AdjustSplitViewPane(ShellPage.ActualWidth);

        //If StoryCAD was loaded from a .STBX File then instead of showing the file open menu
        //We will instead load the file instead.
        Logger.Log(LogLevel.Info, $"Filepath to launch {ShellVm.FilePathToLaunch}");
        if (ShellVm.FilePathToLaunch == null) { await ShellVm.OutlineManager.OpenFileOpenMenu(); }
        else { await ShellVm.OutlineManager.OpenFile(ShellVm.FilePathToLaunch); }

		//Ask user for review if appropriate.
		RatingService rateService = Ioc.Default.GetService<RatingService>();
		if (rateService!.AskForRatings())
		{
			rateService.OpenRatingPrompt();
		}

        // Track when the application is shutting down
        
        Windowing.MainWindow.Closed += ((_, _) => ShellVm.IsClosing = true);
    }

    /// <summary>
    /// Makes the TreeView lose its selection when there is no corresponding main menu item.
    /// </summary>
    /// <remarks>But I don't know why...</remarks>
    private void SplitViewFrame_OnNavigated(object sender, NavigationEventArgs e)
    {
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
        ShellVm.OutlineManager.SearchNodes();
    }

    private void ClearNodes(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (ShellVm.DataSource == null || ShellVm.DataSource.Count == 0) { return; }
        foreach (StoryNodeItem node in ShellVm.DataSource[0]) { node.Background = null; }
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

    private async void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
	    var result = await Ioc.Default.GetRequiredService<Windowing>().ShowContentDialog(new()
	    {
		    Content = new FeedbackDialog(),
		    PrimaryButtonText = "Submit Feedback",
			SecondaryButtonText = "Discard",
		    Title = "Submit",
	    });

	    if (result == ContentDialogResult.Primary)
	    {
		    Ioc.Default.GetRequiredService<FeedbackViewModel>().CreateFeedback();
	    }
    }
    private void ShellPage_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        AdjustSplitViewPane(e.NewSize.Width);
    }

    private void AdjustSplitViewPane(double width)
    {
        if (ShellSplitView != null && ShellSplitView.IsPaneOpen)
        {
            double pane = Math.Max(200, width * 0.3);
            ShellSplitView.OpenPaneLength = pane;
        }
    }

    /// <summary>
    /// Alternative enterance for root nodes as iteminvoked isn't available.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void RootClick(object s, RoutedEventArgs e) => ShellVm.TreeViewNodeClicked((s as FrameworkElement).DataContext);

    /// <summary>
    /// This updates the parent of a node when DND occurs to correctly update backing store.
    /// </summary>
    private void NavigationTree_DragItemsCompleted(TreeView sender, TreeViewDragItemsCompletedEventArgs args)
    {
        try
        {
            //Block all other opperations
            if (args.DropResult != DataPackageOperation.Move) return;

            //Update parent
            var movedItem = (StoryNodeItem)args.Items[0];
            var parent = args.NewParentItem as StoryNodeItem;
            movedItem.Parent = parent;
        }
        catch (Exception ex)
        {
            Logger.LogException(LogLevel.Error, ex, "Error during drag and drop operation.");
        }
    }

}
