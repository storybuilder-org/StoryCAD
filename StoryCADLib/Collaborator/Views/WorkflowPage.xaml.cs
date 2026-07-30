using Windows.System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using StoryCADLib.Collaborator.ViewModels;

namespace StoryCADLib.Collaborator.Views;

/// <summary>
/// Page for displaying and executing a specific workflow.
///
/// BINDING PATTERN:
/// - Public ViewModel property exposes DataContext as WorkflowViewModel
/// - XAML uses {x:Bind ViewModel.Property, Mode=OneWay} for compile-time binding
/// - DataContext is set by Uno Navigation automatically, or via OnNavigatedTo fallback
/// 
/// NOTE: x:DataType at Page level is NOT supported on Skia/Desktop targets.
/// Instead, expose a public ViewModel property and bind to ViewModel.Property.
/// </summary>
public sealed partial class WorkflowPage : Page
{
    private bool _syncingPendingScroll;

    public WorkflowPage()
    {
        InitializeComponent();
        DataContext = new WorkflowViewModel();
        Loaded += OnPageLoaded;
        DataContextChanged += (_, _) => HookPendingUpdatesCollection();
    }

    private void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        HookPendingUpdatesCollection();
        SyncPendingUpdatesScrollBar();
    }

    private void HookPendingUpdatesCollection()
    {
        if (ViewModel?.PendingUpdateItems == null)
            return;

        ViewModel.PendingUpdateItems.CollectionChanged -= PendingUpdateItems_CollectionChanged;
        ViewModel.PendingUpdateItems.CollectionChanged += PendingUpdateItems_CollectionChanged;
    }

    private void PendingUpdateItems_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        // Layout updates after items arrive (workflow result).
        DispatcherQueue.TryEnqueue(() => SyncPendingUpdatesScrollBar());
    }

    /// <summary>
    /// Get the ViewModel from DataContext.
    /// Must be public for x:Bind to access it from XAML.
    /// </summary>
    public WorkflowViewModel ViewModel => DataContext as WorkflowViewModel;

    /// <summary>
    /// Called when navigating to this page.
    /// Uno Navigation sets DataContext (ViewModel) automatically.
    /// Extract WorkflowModel from navigation data and initialize ViewModel.
    /// </summary>
    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (ViewModel != null)
        {
            await ViewModel.InitializeAsync(e.Parameter);
        }

        SyncPendingUpdatesScrollBar();
    }

    private void InputTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter && !string.IsNullOrWhiteSpace(InputTextBox.Text))
        {
            SendButton_Click(this, new RoutedEventArgs());
        }
    }

    private async void SendButton_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel != null)
        {
            await ViewModel.SendButtonClicked();
        }
    }

    private void PendingUpdatesScroll_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        => SyncPendingUpdatesScrollBar();

    private void PendingUpdatesScroll_SizeChanged(object sender, SizeChangedEventArgs e)
        => SyncPendingUpdatesScrollBar();

    private void PendingUpdatesItems_SizeChanged(object sender, SizeChangedEventArgs e)
        => SyncPendingUpdatesScrollBar();

    private void PendingUpdatesScrollBar_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (_syncingPendingScroll || PendingUpdatesScroll == null)
            return;

        PendingUpdatesScroll.ChangeView(null, e.NewValue, null, true);
    }

    /// <summary>
    /// Keep the explicit vertical ScrollBar in sync. Overlay scrollbars fade; this one does not.
    /// </summary>
    private void SyncPendingUpdatesScrollBar()
    {
        if (PendingUpdatesScroll == null || PendingUpdatesScrollBar == null)
            return;

        _syncingPendingScroll = true;
        try
        {
            var viewport = PendingUpdatesScroll.ViewportHeight;
            var extent = PendingUpdatesScroll.ExtentHeight;
            var scrollable = PendingUpdatesScroll.ScrollableHeight;

            if (viewport <= 0)
            {
                // Not laid out yet.
                PendingUpdatesScrollBar.Minimum = 0;
                PendingUpdatesScrollBar.Maximum = 1;
                PendingUpdatesScrollBar.ViewportSize = 1;
                PendingUpdatesScrollBar.Value = 0;
                PendingUpdatesScrollBar.IsEnabled = false;
                return;
            }

            if (scrollable <= 0.5)
            {
                // Content fits: still show a full track so the affordance is obvious.
                PendingUpdatesScrollBar.Minimum = 0;
                PendingUpdatesScrollBar.Maximum = 1;
                PendingUpdatesScrollBar.ViewportSize = 1;
                PendingUpdatesScrollBar.Value = 0;
                PendingUpdatesScrollBar.IsEnabled = false;
            }
            else
            {
                PendingUpdatesScrollBar.Minimum = 0;
                PendingUpdatesScrollBar.Maximum = scrollable;
                PendingUpdatesScrollBar.ViewportSize = viewport;
                PendingUpdatesScrollBar.Value = PendingUpdatesScroll.VerticalOffset;
                PendingUpdatesScrollBar.IsEnabled = true;
            }
        }
        finally
        {
            _syncingPendingScroll = false;
        }
    }
}
