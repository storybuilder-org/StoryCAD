using System.ComponentModel;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using StoryCADLib.ViewModels.SubViewModels;
using Path = Microsoft.UI.Xaml.Shapes.Path;

namespace StoryCADLib.Controls;

/// <summary>
///     The contents of the beat tree window (issue #1546). The code here does two things that
///     XAML cannot: it tells the view model which beat column has the focus, and it draws the connectors.
/// </summary>
public sealed partial class BeatTreeView : UserControl
{
    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel), typeof(BeatTreeViewModel), typeof(BeatTreeView),
        new PropertyMetadata(null, (d, e) => ((BeatTreeView)d).OnViewModelChanged(
            (BeatTreeViewModel)e.OldValue, (BeatTreeViewModel)e.NewValue)));

    private readonly List<BeatColumnControl> _columns = new();
    private readonly HashSet<ScrollViewer> _watchedScrollViewers = new();
    private string _drawnKey = string.Empty;

    public BeatTreeView()
    {
        InitializeComponent();
        ColumnsHost.LayoutUpdated += (_, _) => DrawConnectors();
    }

    public BeatTreeViewModel ViewModel
    {
        get => (BeatTreeViewModel)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    private void OnViewModelChanged(BeatTreeViewModel oldValue, BeatTreeViewModel newValue)
    {
        if (oldValue != null)
        {
            oldValue.PropertyChanged -= ViewModel_PropertyChanged;
        }

        if (newValue != null)
        {
            newValue.PropertyChanged += ViewModel_PropertyChanged;
        }
    }

    private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(BeatTreeViewModel.ShowBeatDescriptions))
        {
            foreach (var column in _columns)
            {
                column.ShowBeatDescriptions = ViewModel.ShowBeatDescriptions;
            }
        }
        else if (e.PropertyName == nameof(BeatTreeViewModel.Connectors))
        {
            _drawnKey = string.Empty;
            DrawConnectors();
        }
    }

    private void Column_Loaded(object sender, RoutedEventArgs e)
    {
        var column = (BeatColumnControl)sender;
        if (_columns.Contains(column))
        {
            return;
        }

        _columns.Add(column);
        column.ShowBeatDescriptions = ViewModel?.ShowBeatDescriptions ?? false;
        column.Activated += Column_Activated;
        Watch(column.BeatsList);
        Watch(column.NoBeatList);
    }

    private void Column_Unloaded(object sender, RoutedEventArgs e)
    {
        var column = (BeatColumnControl)sender;
        column.Activated -= Column_Activated;
        _columns.Remove(column);
    }

    private void Column_Activated(object sender, BeatSheetsViewModel column)
    {
        if (ViewModel != null)
        {
            ViewModel.ActiveColumn = column;
        }
    }

    /// <summary>A card moves when its list moves up or down, so the connectors are drawn again.</summary>
    private void Watch(ListView list)
    {
        var scrollViewer = FindDescendant<ScrollViewer>(list);
        if (scrollViewer != null && _watchedScrollViewers.Add(scrollViewer))
        {
            scrollViewer.ViewChanged += (_, _) => DrawConnectors();
        }
    }

    private static T FindDescendant<T>(DependencyObject root) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T match)
            {
                return match;
            }

            if (FindDescendant<T>(child) is { } deeper)
            {
                return deeper;
            }
        }

        return null;
    }

    // ----- The connectors -----

    /// <summary>
    ///     Draws one curve for each connector: from the right side of the card, or of the element on
    ///     no beat, that has the child Problem, to the left side of the header of the beat column of the child.
    /// </summary>
    private void DrawConnectors()
    {
        if (ViewModel == null)
        {
            return;
        }

        var lines = new List<(Point Start, Point End)>();
        foreach (var connector in ViewModel.Connectors)
        {
            var parent = _columns.FirstOrDefault(c => c.ViewModel?.ProblemGuid == connector.ParentGuid);
            var child = _columns.FirstOrDefault(c => c.ViewModel?.ProblemGuid == connector.ChildGuid);
            if (parent == null || child == null)
            {
                continue;
            }

            var list = connector.BeatIndex >= 0 ? parent.BeatsList : parent.NoBeatList;
            var source = connector.BeatIndex >= 0
                ? list.ContainerFromIndex(connector.BeatIndex) as FrameworkElement
                : ContainerOf(list, connector.ChildGuid);
            if (source == null || child.Header.ActualWidth <= 0)
            {
                continue;
            }

            // A card that is out of view in its list keeps its connector at the edge of the list.
            var listTop = Position(list, 0, 0).Y;
            var start = Position(source, source.ActualWidth, source.ActualHeight / 2);
            start.Y = Math.Clamp(start.Y, listTop, listTop + list.ActualHeight);
            var end = Position(child.Header, 0, Math.Min(child.Header.ActualHeight / 2, 12));
            lines.Add((start, end));
        }

        // LayoutUpdated comes often. Draw only when a connector moved.
        var key = string.Join(";", lines.Select(l =>
            $"{l.Start.X:F0},{l.Start.Y:F0},{l.End.X:F0},{l.End.Y:F0}"));
        if (key == _drawnKey)
        {
            return;
        }

        _drawnKey = key;
        ConnectorCanvas.Children.Clear();
        var brush = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"];
        foreach (var (start, end) in lines)
        {
            var middle = (start.X + end.X) / 2;
            var figure = new PathFigure { StartPoint = start, IsClosed = false };
            figure.Segments.Add(new BezierSegment
            {
                Point1 = new Point(middle, start.Y),
                Point2 = new Point(middle, end.Y),
                Point3 = end
            });
            var geometry = new PathGeometry();
            geometry.Figures.Add(figure);
            ConnectorCanvas.Children.Add(new Path { Data = geometry, Stroke = brush, StrokeThickness = 1.5 });

            var dot = new Ellipse { Width = 8, Height = 8, Fill = brush };
            Canvas.SetLeft(dot, start.X - 4);
            Canvas.SetTop(dot, start.Y - 4);
            ConnectorCanvas.Children.Add(dot);
        }
    }

    private Point Position(UIElement element, double x, double y)
    {
        return element.TransformToVisual(ConnectorCanvas).TransformPoint(new Point(x, y));
    }

    private static FrameworkElement ContainerOf(ListView list, Guid element)
    {
        foreach (var item in list.Items)
        {
            if (item is StoryElement storyElement && storyElement.Uuid == element)
            {
                return list.ContainerFromItem(item) as FrameworkElement;
            }
        }

        return null;
    }
}
