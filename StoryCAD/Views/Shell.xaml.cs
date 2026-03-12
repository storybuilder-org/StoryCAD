using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
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
#if HAS_UNO
    private MenuFlyout _addElementsFlyout;
    private AppBarButton _addElementsButton;
    private Point _lastPointerPosition;
    private bool _allowNextClose;
    private bool _emptyTrashFlyoutIsOpen;
#endif

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
#if HAS_UNO
        // Issue #1323 Part 2: Safe triangle for diagonal mouse movement on UNO Skia.
        //
        // Problem: UNO's Skia backend fires PointerEntered on sibling AppBarButtons when
        // the cursor moves diagonally from "Add Elements" toward a lower submenu item.
        // This causes UNO's internal logic to close the submenu prematurely.
        //
        // Solution: When the submenu's Closing event fires, check whether the cursor is
        // inside a "safe triangle" — the region between the Add Elements button and the
        // submenu popup. If the cursor is heading toward the submenu (inside the triangle),
        // cancel the close. If not, allow it.
        //
        // The triangle is computed from the Add Elements button's geometry plus fixed
        // offset estimates for the submenu position and size. This avoids calling
        // TransformToVisual across popup boundaries, which is unreliable on UNO Skia
        // (the submenu lives in a separate Popup with its own coordinate space).
        //
        // See also: macOS "safe triangle" (Tognazzini & Batson, 1980s), WinUI #5617.
        if (_contextFlyout.SecondaryCommands[0] is AppBarButton addBtn
            && addBtn.Flyout is MenuFlyout mf)
        {
            _addElementsButton = addBtn;
            _addElementsFlyout = mf;
            _addElementsFlyout.Closing += AddElementsFlyout_Closing;
            _addElementsFlyout.Closed += (_, _) =>
            {
                _allowNextClose = false;
            };

            // Track pointer position on the Add Elements button so we have a current
            // cursor location when the Closing event fires. We track on the button
            // (a UIElement) because CommandBarFlyout inherits from FlyoutBase, not
            // UIElement, and does not support pointer events directly.
            _addElementsButton.PointerMoved += (s, e) =>
            {
                _lastPointerPosition = e.GetCurrentPoint((UIElement)s).Position;
            };

        }

        // Attach Closing handler to Empty Trash flyout to prevent dismissal during pointer movement
        EmptyTrashFlyout.Closing += EmptyTrashFlyout_Closing;
        EmptyTrashFlyout.Opened += (_, _) => { _emptyTrashFlyoutIsOpen = true; };
        EmptyTrashFlyout.Closed += (_, _) => { _emptyTrashFlyoutIsOpen = false; };
#endif
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

        AppState.RightTappedNode = (StoryNodeItem)item.DataContext;
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
        // Always set OpenPaneLength regardless of pane state, so when the pane
        // opens (e.g., via VisualStateManager), it has the correct width
        if (ShellSplitView != null)
        {
            // In narrow mode (<800px), pane should fill entire width for full-screen toggle
            // In wide mode (>=800px), pane should be 30% of width (min 200px)
            var pane = width < 800 ? width : Math.Max(200, width * 0.3);
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
            // Note: On macOS, Option+Letter normally produces special characters (e.g., ñ, ß).
            // UNO Platform's Skia backend may intercept these before the OS does.
            // If Alt+Letter shortcuts don't work on macOS, consider Ctrl+Shift+Letter alternatives.
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

            if (!ctrl && !shift && alt && e.Key == VirtualKey.B && ShellVm.ExplorerVisibility == Visibility.Visible)
            {
                ShellVm.AddStoryWorldCommand.Execute(null);
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

            if (ctrl && !shift && !alt && e.Key == VirtualKey.K)
            {
                ShellVm.KeyQuestionsCommand.Execute(null);
                e.Handled = true;
                return;
            }

            // Reports Menu shortcuts
            // Ctrl+P: Print Reports on Windows, PDF Export on macOS (Print is unsupported on macOS)
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
                ShellVm.ExportReportsToPdfCommand.Execute(null);
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

            // Preferences shortcut (Ctrl+, / ⌘,)
            if (ctrl && !shift && !alt && e.Key == (VirtualKey)188)
            {
                ShellVm.PreferencesCommand.Execute(null);
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

#if HAS_UNO
    /// <summary>
    /// Intercepts submenu dismissal during diagonal mouse movement on UNO Skia.
    ///
    /// Uses a "safe triangle" to decide whether to cancel the close. The triangle
    /// is defined by three points:
    ///   A = center-right edge of the "Add Elements" button
    ///   B = estimated top-right corner of the submenu popup
    ///   C = estimated bottom-right corner of the submenu popup
    ///
    /// If the cursor (_lastPointerPosition) is inside this triangle, the user is
    /// likely moving diagonally toward the submenu, so we cancel the close.
    /// If outside, the user has moved away, so we allow the close.
    ///
    /// The submenu corner positions are approximated using fixed offsets from the
    /// button's position rather than calling TransformToVisual on the MenuFlyoutItem
    /// elements. This is intentional: the submenu lives in a separate UNO Popup with
    /// its own coordinate space, and TransformToVisual across popup boundaries is
    /// unreliable on UNO Skia. The fixed offsets are conservative estimates that
    /// create a triangle larger than the actual submenu, which is safe — a larger
    /// triangle only means we're more permissive about keeping the submenu open.
    ///
    /// Constants:
    ///   SubmenuWidthEstimate (220px) — widest item is "Add StoryWorld  ⌥B" (~200px)
    ///     plus padding. Overestimate is harmless.
    ///   SubmenuHeightEstimate (300px) — 9 items × ~32px + padding. Overestimate is
    ///     harmless.
    ///   HorizontalGap (-16px applied via negative margin in XAML, so effective gap ≈ 0)
    ///
    /// Normal dismissal (click outside, Escape, selecting an item) still works because
    /// those paths either don't trigger Closing or the cursor is outside the triangle.
    ///
    /// See issue #1323, macOS safe triangle (Tognazzini & Batson, 1980s), WinUI #5617.
    /// </summary>
    private void AddElementsFlyout_Closing(object sender, FlyoutBaseClosingEventArgs args)
    {
        // _allowNextClose is set when we programmatically call Hide() — let it through.
        if (_allowNextClose)
        {
            _allowNextClose = false;
            return;
        }

        // Compute the safe triangle from the Add Elements button's geometry.
        // Coordinates are relative to the button itself (origin 0,0) since
        // _lastPointerPosition is captured via PointerMoved on the same button.
        var buttonOrigin = new Point(0, 0);
        double buttonWidth = _addElementsButton.ActualWidth;
        double buttonHeight = _addElementsButton.ActualHeight;

        const double SubmenuWidthEstimate = 220;
        const double SubmenuHeightEstimate = 300;

        // Vertex A: center-right edge of the button (where the user starts moving from)
        Point a = new(buttonOrigin.X + buttonWidth, buttonOrigin.Y + buttonHeight / 2);
        // Vertex B: estimated top-right corner of the submenu
        Point b = new(buttonOrigin.X + buttonWidth + SubmenuWidthEstimate, buttonOrigin.Y);
        // Vertex C: estimated bottom-right corner of the submenu
        Point c = new(buttonOrigin.X + buttonWidth + SubmenuWidthEstimate, buttonOrigin.Y + SubmenuHeightEstimate);

        if (IsPointInTriangle(_lastPointerPosition, a, b, c))
        {
            // Cursor is heading toward the submenu — keep it open.
            args.Cancel = true;
        }
        // Otherwise: cursor is not heading toward submenu — allow close.
    }

    /// <summary>
    /// Determines whether point <paramref name="p"/> lies inside the triangle
    /// defined by vertices <paramref name="a"/>, <paramref name="b"/>, and
    /// <paramref name="c"/> using the cross-product sign method.
    ///
    /// Returns true if the point is inside or on the edge of the triangle.
    /// </summary>
    private static bool IsPointInTriangle(Point p, Point a, Point b, Point c)
    {
        double d1 = CrossProduct(p, a, b);
        double d2 = CrossProduct(p, b, c);
        double d3 = CrossProduct(p, c, a);

        bool hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
        bool hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);

        return !(hasNeg && hasPos);
    }

    /// <summary>
    /// Computes the 2D cross product of vectors (p1→p2) and (p1→p3).
    /// Positive if p3 is counter-clockwise from p2 relative to p1.
    /// </summary>
    private static double CrossProduct(Point p1, Point p2, Point p3)
    {
        return (p2.X - p1.X) * (p3.Y - p1.Y) - (p3.X - p1.X) * (p2.Y - p1.Y);
    }

    /// <summary>
    /// Prevents the Empty Trash confirmation flyout from dismissing when the pointer
    /// moves from the button to the flyout popup. Cancels the first close attempt after
    /// the flyout opens, allowing the pointer to reach the content.
    /// </summary>
    private void EmptyTrashFlyout_Closing(object sender, FlyoutBaseClosingEventArgs args)
    {
        // If the flag is set, this is the first close attempt (pointer exiting button)
        // Cancel it to allow pointer to reach the flyout content
        if (_emptyTrashFlyoutIsOpen)
        {
            args.Cancel = true;
            _emptyTrashFlyoutIsOpen = false; // Next close attempt will proceed normally
        }
        // Otherwise, allow the close (user clicked outside, pressed Escape, or clicked button)
    }
#endif
}
