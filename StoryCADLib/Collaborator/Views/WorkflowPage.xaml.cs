using Windows.System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using StoryCADLib.Collaborator.ViewModels;
using Windows.Foundation;

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
    private bool _thumbDragging;
    private double _thumbDragStartY;
    private double _thumbDragStartOffset;

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
        UpdatePendingUpdatesScrollLayout();
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
        // Items arrive after workflow result; re-measure after layout.
        DispatcherQueue.TryEnqueue(() => UpdatePendingUpdatesScrollLayout());
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

        HookPendingUpdatesCollection();
        UpdatePendingUpdatesScrollLayout();
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

    private void PendingUpdatesScrollHost_SizeChanged(object sender, SizeChangedEventArgs e)
        => UpdatePendingUpdatesScrollLayout();

    private void PendingUpdatesScroll_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        => UpdatePendingUpdatesThumb();

    private void PendingUpdatesItems_SizeChanged(object sender, SizeChangedEventArgs e)
        => UpdatePendingUpdatesScrollLayout();

    private void PendingUpdatesTrackCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        => UpdatePendingUpdatesThumb();

    /// <summary>
    /// Give the ScrollViewer a real pixel height. Without this, measure is often infinite
    /// and the content never scrolls (it is only clipped by a parent).
    /// </summary>
    private void UpdatePendingUpdatesScrollLayout()
    {
        if (PendingUpdatesScrollHost == null || PendingUpdatesScroll == null)
            return;

        var hostHeight = PendingUpdatesScrollHost.ActualHeight;
        if (hostHeight > 1)
        {
            PendingUpdatesScroll.Height = hostHeight;
        }

        UpdatePendingUpdatesThumb();
    }

    /// <summary>
    /// Position the permanent thumb from ScrollViewer offset / extent.
    /// </summary>
    private void UpdatePendingUpdatesThumb()
    {
        if (PendingUpdatesScroll == null || PendingUpdatesThumb == null || PendingUpdatesTrackCanvas == null)
            return;

        var trackHeight = PendingUpdatesTrackCanvas.ActualHeight;
        if (trackHeight <= 1)
            return;

        var viewport = PendingUpdatesScroll.ViewportHeight;
        var extent = PendingUpdatesScroll.ExtentHeight;
        if (viewport <= 0)
            viewport = PendingUpdatesScroll.ActualHeight;
        if (extent <= 0)
            extent = PendingUpdatesItems?.ActualHeight ?? 0;

        var scrollable = Math.Max(0, extent - viewport);

        // Thumb size proportional to visible fraction; minimum so it stays grabbable.
        double thumbHeight;
        if (extent <= 0 || scrollable <= 0.5)
        {
            thumbHeight = trackHeight;
            Canvas.SetTop(PendingUpdatesThumb, 0);
            PendingUpdatesThumb.Opacity = 0.35;
            PendingUpdatesThumb.IsHitTestVisible = false;
        }
        else
        {
            thumbHeight = Math.Max(24, trackHeight * (viewport / extent));
            thumbHeight = Math.Min(thumbHeight, trackHeight);
            var maxTop = trackHeight - thumbHeight;
            var top = maxTop * (PendingUpdatesScroll.VerticalOffset / scrollable);
            Canvas.SetTop(PendingUpdatesThumb, top);
            PendingUpdatesThumb.Opacity = 1.0;
            PendingUpdatesThumb.IsHitTestVisible = true;
        }

        PendingUpdatesThumb.Height = thumbHeight;
        PendingUpdatesThumb.Width = Math.Max(8, PendingUpdatesTrackCanvas.ActualWidth - 4);
        Canvas.SetLeft(PendingUpdatesThumb, 2);
    }

    private void PendingUpdatesTrack_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (PendingUpdatesScroll == null || PendingUpdatesTrackCanvas == null)
            return;

        var pos = e.GetCurrentPoint(PendingUpdatesTrackCanvas).Position;
        JumpScrollToTrackY(pos.Y);
        e.Handled = true;
    }

    private void PendingUpdatesThumb_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (PendingUpdatesScroll == null || PendingUpdatesThumb == null)
            return;

        _thumbDragging = true;
        _thumbDragStartY = e.GetCurrentPoint(PendingUpdatesTrackCanvas).Position.Y;
        _thumbDragStartOffset = PendingUpdatesScroll.VerticalOffset;
        PendingUpdatesThumb.CapturePointer(e.Pointer);
        e.Handled = true;
    }

    private void PendingUpdatesThumb_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_thumbDragging || PendingUpdatesScroll == null || PendingUpdatesTrackCanvas == null)
            return;

        var y = e.GetCurrentPoint(PendingUpdatesTrackCanvas).Position.Y;
        var delta = y - _thumbDragStartY;
        var trackHeight = PendingUpdatesTrackCanvas.ActualHeight;
        var thumbHeight = PendingUpdatesThumb.Height;
        var maxTop = Math.Max(1, trackHeight - thumbHeight);
        var scrollable = PendingUpdatesScroll.ScrollableHeight;
        if (scrollable <= 0)
            scrollable = Math.Max(0, PendingUpdatesScroll.ExtentHeight - PendingUpdatesScroll.ViewportHeight);

        var newOffset = _thumbDragStartOffset + (delta / maxTop) * scrollable;
        newOffset = Math.Max(0, Math.Min(scrollable, newOffset));
        PendingUpdatesScroll.ChangeView(null, newOffset, null, true);
        e.Handled = true;
    }

    private void PendingUpdatesThumb_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (!_thumbDragging)
            return;

        _thumbDragging = false;
        PendingUpdatesThumb.ReleasePointerCapture(e.Pointer);
        e.Handled = true;
    }

    private void PendingUpdatesThumb_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
    {
        _thumbDragging = false;
    }

    private void JumpScrollToTrackY(double trackY)
    {
        if (PendingUpdatesScroll == null || PendingUpdatesTrackCanvas == null)
            return;

        var trackHeight = PendingUpdatesTrackCanvas.ActualHeight;
        var thumbHeight = PendingUpdatesThumb?.Height ?? 24;
        var maxTop = Math.Max(1, trackHeight - thumbHeight);
        var scrollable = PendingUpdatesScroll.ScrollableHeight;
        if (scrollable <= 0)
            scrollable = Math.Max(0, PendingUpdatesScroll.ExtentHeight - PendingUpdatesScroll.ViewportHeight);

        var top = Math.Max(0, Math.Min(maxTop, trackY - thumbHeight / 2));
        var offset = (top / maxTop) * scrollable;
        PendingUpdatesScroll.ChangeView(null, offset, null, true);
    }
}
