using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using Windows.UI.ViewManagement;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using StoryCADLib.Helpers;
using StoryCADLib.Models.Tools;
using StoryCADLib.Services;
using StoryCADLib.Services.Backup;
using StoryCADLib.Services.Collaborator;
using StoryCADLib.Services.Dialogs;
using StoryCADLib.Services.Locking;
using StoryCADLib.Services.Logging;
using StoryCADLib.Services.Ratings;
using StoryCADLib.ViewModels.SubViewModels;
using StoryCADLib.ViewModels.Tools;

namespace StoryCAD.Views;

public sealed partial class Shell : Page
{
    public LogService Logger;
    public PreferencesModel Preferences = Ioc.Default.GetRequiredService<PreferenceService>().Model;
    private CommandBarFlyout _contextFlyout;

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
            AppState.CurrentDocumentChanged += (_, __) => UpdateDocumentBindings();
            // Marshal to UI thread to prevent cross-thread deadlock when AutoSave fires this event
            SerializationLock.CanExecuteStateChanged += (_, __) =>
            {
                Windowing.GlobalDispatcher.TryEnqueue(() => ShellVm.RefreshAllCommands());
            };
        }
        catch (Exception ex)
        {
            // A shell initialization error is fatal
            Logger!.LogException(LogLevel.Error, ex, ex.Message);
            Logger.Flush();
            Application.Current.Exit(); // Win32
        }

        ShellVm.SplitViewFrame = SplitViewFrame;
        _contextFlyout = (CommandBarFlyout)ShellPage.Resources["AddStoryElementFlyout"];
    }

    public ShellViewModel ShellVm => Ioc.Default.GetService<ShellViewModel>();
    public Windowing Windowing => Ioc.Default.GetService<Windowing>();
    public OutlineViewModel OutlineVM => Ioc.Default.GetService<OutlineViewModel>();
    public AppState AppState => Ioc.Default.GetService<AppState>();

    private async void Shell_Loaded(object sender, RoutedEventArgs e)
    {
        Windowing.XamlRoot = XamlRoot;
        Ioc.Default.GetService<AppState>()!.StartUpTimer.Stop();

        if (await Ioc.Default.GetService<CollaboratorService>()!.CollaboratorEnabled())
        {
            Ioc.Default.GetService<CollaboratorService>()!.ConnectCollaborator();
            ShellVm.CollaboratorVisibility = Visibility.Visible;
        }
        else
        {
            ShellVm.CollaboratorVisibility = Visibility.Collapsed;
        }

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
            using (var serializationLock = new SerializationLock(logService))
            {
                await Ioc.Default.GetRequiredService<WebViewModel>().ShowWebViewDialog();
            }
        }

        //Shows changelog if the app has been updated since the last launch.
        if (Ioc.Default.GetRequiredService<AppState>().LoadedWithVersionChange)
        {
            Ioc.Default.GetService<PreferenceService>()!.Model.HideRatingPrompt =
                false; //rating prompt re-enabled on updates.
            var logger = Ioc.Default.GetService<ILogService>();
            var appState = Ioc.Default.GetService<AppState>();
            await new Changelog(logger, appState).ShowChangeLog();
        }

        if (Preferences.ShowStartupDialog)
        {
            ContentDialog cd = new()
            {
                Title = "Need help getting started?",
                Content = new HelpPage(),
                PrimaryButtonText = "Close"
            };
            await Ioc.Default.GetRequiredService<Windowing>().ShowContentDialog(cd);
        }

        AdjustSplitViewPane(ShellPage.ActualWidth);

        //If StoryCAD was loaded from a .STBX File then instead of showing the file open menu
        //We will instead load the file instead.
        Logger.Log(LogLevel.Info, $"Filepath to launch {ShellVm.FilePathToLaunch}");
        if (ShellVm.FilePathToLaunch == null)
        {
            // Only show the file open menu if the preference is enabled
            if (Preferences.ShowFilePickerOnStartup)
            {
                await ShellVm.OutlineManager.OpenFileOpenMenu();
            }
        }
        else
        {
            await ShellVm.OutlineManager.OpenFile(ShellVm.FilePathToLaunch);
        }

        //Ask user for review if appropriate.
        var rateService = Ioc.Default.GetService<RatingService>();
        if (rateService!.AskForRatings())
        {
            rateService.OpenRatingPrompt();
        }

        // Track when the application is shutting down

        // Hook up the Closing event for cleanup before window destruction
        if (Windowing.MainWindow.AppWindow != null)
        {
            Windowing.MainWindow.AppWindow.Closing += OnMainWindowClosing;
        }

    }

    /// <summary>
    ///     Handles the main window closing event. Calls ShellViewModel to perform cleanup.
    /// </summary>
    private async void OnMainWindowClosing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        try
        {
            await ShellVm.OnApplicationClosing();
        }
        catch (Exception ex)
        {
            Logger.LogException(LogLevel.Error, ex, "Error during application closing");
            // Don't re-throw - let the window close even if cleanup fails
        }
    }

    /// <summary>
    ///     Makes the TreeView lose its selection when there is no corresponding main menu item.
    /// </summary>
    /// <remarks>But I don't know why...</remarks>
    private void SplitViewFrame_OnNavigated(object sender, NavigationEventArgs e)
    {
        (SplitViewFrame.Content as FrameworkElement)!.RequestedTheme = Windowing.RequestedTheme;
        SplitViewFrame.Background = Windowing.RequestedTheme == ElementTheme.Light
            ? new SolidColorBrush(Colors.White)
            : new SolidColorBrush(Colors.Black);
        if (!((FrameworkElement)SplitViewFrame.Content).BaseUri.ToString().Contains("HomePage"))
        {
            ((Page)SplitViewFrame.Content).Margin = new Thickness(0, 0, 0, 5);
        }
    }

    /// <summary>
    ///     Navigates to the specified source page type.
    /// </summary>
    public bool Navigate(Type sourcePageType, object parameter = null)
    {
        return SplitViewFrame.Navigate(sourcePageType, parameter);
    }

    private void TreeViewItem_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        e.Handled = true; // Stop event propagation to prevent recursion

        if (ShellVm.LastClickedTreeviewItem != null)
        {
            ShellVm.LastClickedTreeviewItem.Background = null;
            ShellVm.LastClickedTreeviewItem.IsSelected = false;
            ShellVm.LastClickedTreeviewItem.BorderBrush = null;
        }

        //Remove old right-clicked node's background
        var item = (TreeViewItem)sender;
        item.Background = new SolidColorBrush(new UISettings().GetColorValue(UIColorType.Accent));

        ShellVm.RightTappedNode = (StoryNodeItem)item.DataContext;
        ShellVm.LastClickedTreeviewItem = item; //We can't set the background through RightTappedNode so
        //we set a reference to the node itself to reset the background later
        ShellVm.ShowFlyoutButtons();

        // Programmatically show the flyout on the clicked item
        _contextFlyout?.ShowAt(item, new FlyoutShowOptions
        {
            Position = e.GetPosition(item)
        });
    }

    /// <summary>
    ///     Updates the bindings when the document changes.
    ///     Called when AppState.CurrentDocument is set to refresh x:Bind bindings
    ///     for the tree views (CurrentView and TrashView) in the Shell UI.
    /// </summary>
    public void UpdateDocumentBindings()
    {
        Bindings.Update();
    }

    /// <summary>
    ///     Treat a treeview item as if it were a button
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    private void TreeViewItem_Invoked(TreeView sender, TreeViewItemInvokedEventArgs args)
    {
        ShellVm.TreeViewNodeClicked(args.InvokedItem);
        args.Handled = true;
    }

    private void Search(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        ShellVm.OutlineManager.SearchNodes();
    }

    private void ClearNodes(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        var storyModel = Ioc.Default.GetService<AppState>()!.CurrentDocument.Model;
        if (storyModel?.CurrentView == null || storyModel.CurrentView.Count == 0)
        {
            return;
        }

        foreach (var node in storyModel.CurrentView[0])
        {
            node.Background = null;
        }
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
        var result = await Ioc.Default.GetRequiredService<Windowing>().ShowContentDialog(new ContentDialog
        {
            Content = new FeedbackDialog(),
            PrimaryButtonText = "Submit Feedback",
            SecondaryButtonText = "Discard",
            Title = "Submit"
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
            var pane = Math.Max(200, width * 0.3);
            ShellSplitView.OpenPaneLength = pane;
        }
    }

    /* Drag-and-drop overview
       - Root nodes are rendered by an ItemsRepeater as standalone TreeViewItems.
         They only handle tap or right-tap via RootClick and perform no pointer tracking.

       - Each root hosts a nested TreeView bound to its Children. The TreeView has
         CanReorderItems enabled so the control handles dragging internally.

       - NavigationTree_DragItemsCompleted runs after a drop to update the moved
         item's parent in the story model.
    */

    /// <summary>
    ///     Ran when root nodes are clicked.
    ///     This is because you can't attach the TreViewItem_Invoked event
    ///     to the root nodes as they are not within a tree view,
    ///     so this just forwards the click so it can run normally.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void RootClick(object s, RoutedEventArgs e) =>
        ShellVm.TreeViewNodeClicked((s as FrameworkElement).DataContext);

    /// <summary>
    ///     This updates the parent of a node in a drag and drop to correctly update backing store (story model)
    ///     when the parent of the item being moved is supposed to be the root of the tree view.
    /// </summary>
    private void NavigationTree_DragItemsCompleted(TreeView sender, TreeViewDragItemsCompletedEventArgs args)
    {
        try
        {
            //Block all other operations
            if (args.DropResult != DataPackageOperation.Move)
            {
                return;
            }

            //Update parent field of item in storymodel so it's correct
            var movedItem = (StoryNodeItem)args.Items[0];
            var parent = args.NewParentItem as StoryNodeItem;

            // If parent is null, use the CurrentView view's root node
            if (parent == null)
            {
                var storyModel = Ioc.Default.GetService<AppState>()!.CurrentDocument!.Model;
                if (storyModel?.CurrentView?.Count > 0)
                {
                    //This gets the parent grid containing the tree's data context
                    //this will be the correct root in the cases where there are
                    //multiple roots in view (i.e. explorer view has the overview and trash)
                    var root = (sender.Parent as FrameworkElement).DataContext;
                    movedItem.Parent = (StoryNodeItem)root;
                }
            }
            else
            {
                movedItem.Parent = parent;
            }
        }
        catch (Exception ex)
        {
            Logger.LogException(LogLevel.Error, ex, "Error during drag and drop operation.");
        }
    }

    /// <summary>
    /// Centralized keyboard shortcut handler using KeyboardHelper for cross-platform support.
    /// This replaces individual KeyboardAccelerator elements in XAML.
    /// </summary>
    private void ShellPage_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        try
        {
            // Don't process keyboard shortcuts if the event has already been handled
            // by a child element (like a text box or context menu)
            if (e.Handled)
            {
                return;
            }

            // Don't process if the original source is a TextBox or other input control
            // to avoid interfering with text entry
            if (e.OriginalSource is TextBox || e.OriginalSource is RichEditBox ||
                e.OriginalSource is AutoSuggestBox || e.OriginalSource is PasswordBox)
            {
                return;
            }

            var ctrl = KeyboardHelper.IsControlPressed();
            var shift = KeyboardHelper.IsShiftPressed();
            var alt = KeyboardHelper.IsAltPressed();

            // File Menu shortcuts
            if (ctrl && !shift && !alt && e.Key == VirtualKey.O)
            {
                ShellVm.OpenFileOpenMenuCommand.Execute(null);
                e.Handled = true;
                return;
            }

            if (ctrl && !shift && !alt && e.Key == VirtualKey.S)
            {
                ShellVm.SaveFileCommand.Execute(null);
                e.Handled = true;
                return;
            }

            if (ctrl && shift && !alt && e.Key == VirtualKey.S)
            {
                ShellVm.SaveAsCommand.Execute(null);
                e.Handled = true;
                return;
            }

            if (ctrl && !shift && !alt && e.Key == VirtualKey.B)
            {
                ShellVm.CreateBackupCommand.Execute(null);
                e.Handled = true;
                return;
            }

            // Add Menu shortcuts (Alt+Letter)
            if (!ctrl && !shift && alt && e.Key == VirtualKey.F && ShellVm.ExplorerVisibility == Visibility.Visible)
            {
                ShellVm.AddFolderCommand.Execute(null);
                e.Handled = true;
                return;
            }

            if (!ctrl && !shift && alt && e.Key == VirtualKey.A && ShellVm.NarratorVisibility == Visibility.Visible)
            {
                ShellVm.AddSectionCommand.Execute(null);
                e.Handled = true;
                return;
            }

            if (!ctrl && !shift && alt && e.Key == VirtualKey.P && ShellVm.ExplorerVisibility == Visibility.Visible)
            {
                ShellVm.AddProblemCommand.Execute(null);
                e.Handled = true;
                return;
            }

            if (!ctrl && !shift && alt && e.Key == VirtualKey.C && ShellVm.ExplorerVisibility == Visibility.Visible)
            {
                ShellVm.AddCharacterCommand.Execute(null);
                e.Handled = true;
                return;
            }

            if (!ctrl && !shift && alt && e.Key == VirtualKey.L && ShellVm.ExplorerVisibility == Visibility.Visible)
            {
                ShellVm.AddSettingCommand.Execute(null);
                e.Handled = true;
                return;
            }

            if (!ctrl && !shift && alt && e.Key == VirtualKey.S && ShellVm.ExplorerVisibility == Visibility.Visible)
            {
                ShellVm.AddSceneCommand.Execute(null);
                e.Handled = true;
                return;
            }

            if (!ctrl && !shift && alt && e.Key == VirtualKey.W && ShellVm.ExplorerVisibility == Visibility.Visible)
            {
                ShellVm.AddWebCommand.Execute(null);
                e.Handled = true;
                return;
            }

            if (!ctrl && !shift && alt && e.Key == VirtualKey.N && ShellVm.ExplorerVisibility == Visibility.Visible)
            {
                ShellVm.AddNotesCommand.Execute(null);
                e.Handled = true;
                return;
            }

            // Delete key (no modifiers)
            if (!ctrl && !shift && !alt && e.Key == VirtualKey.Delete && ShellVm.ExplorerVisibility == Visibility.Visible)
            {
                ShellVm.RemoveStoryElementCommand.Execute(null);
                e.Handled = true;
                return;
            }

            // Exit shortcut (Cmd+Q on macOS, Ctrl+Q on Windows)
            if (ctrl && !shift && !alt && e.Key == VirtualKey.Q)
            {
                ShellVm.ExitCommand.Execute(null);
                e.Handled = true;
                return;
            }

            // Tools Menu shortcuts
            if (ctrl && !shift && !alt && e.Key == VirtualKey.N)
            {
                ShellVm.NarrativeToolCommand.Execute(null);
                e.Handled = true;
                return;
            }

            if (ctrl && !shift && !alt && e.Key == VirtualKey.M)
            {
                ShellVm.MasterPlotsCommand.Execute(null);
                e.Handled = true;
                return;
            }

            if (ctrl && !shift && !alt && e.Key == VirtualKey.D)
            {
                ShellVm.DramaticSituationsCommand.Execute(null);
                e.Handled = true;
                return;
            }

            if (ctrl && !shift && !alt && e.Key == VirtualKey.L)
            {
                ShellVm.StockScenesCommand.Execute(null);
                e.Handled = true;
                return;
            }

            // Reports Menu shortcuts
            // Note: Ctrl+P is platform-specific - Print on Windows, Preferences elsewhere
#if HAS_UNO_WINUI
            if (ctrl && !shift && !alt && e.Key == VirtualKey.P)
            {
                ShellVm.PrintReportsCommand.Execute(null);
                e.Handled = true;
                return;
            }
#else
            if (ctrl && !shift && !alt && e.Key == VirtualKey.P)
            {
                ShellVm.PreferencesCommand.Execute(null);
                e.Handled = true;
                return;
            }
#endif

            if (ctrl && shift && !alt && e.Key == VirtualKey.P)
            {
                ShellVm.ExportReportsToPdfCommand.Execute(null);
                e.Handled = true;
                return;
            }

            if (ctrl && !shift && !alt && e.Key == VirtualKey.R)
            {
                ShellVm.ScrivenerReportsCommand.Execute(null);
                e.Handled = true;
                return;
            }

            // Help shortcut
            if (!ctrl && !shift && !alt && e.Key == VirtualKey.F1)
            {
                ShellVm.HelpCommand.Execute(null);
                e.Handled = true;
                return;
            }
        }
        catch (Exception ex)
        {
            Logger?.LogException(LogLevel.Error, ex, "Error handling keyboard shortcut");
        }
    }
}
