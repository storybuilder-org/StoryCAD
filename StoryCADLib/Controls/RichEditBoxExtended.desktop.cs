using StoryCADLib.Services.Reports;
using Windows.Foundation;

namespace StoryCADLib.Controls;

public partial class RichEditBoxExtended : TextBox
{
    public static readonly DependencyProperty RtfTextProperty =
        DependencyProperty.Register(
            nameof(RtfText), typeof(string), typeof(RichEditBoxExtended),
            new PropertyMetadata(default(string), RtfTextPropertyChanged));

    private bool _lockChangeExecution;

    public RichEditBoxExtended()
    {
        // Debug: Log which version is running
        //System.Diagnostics.Debug.WriteLine("=== RichEditBoxExtended: UNO PLATFORM VERSION (TextBox) ===");

        // Only set properties that prevent layout issues
        // Let XAML control TextWrapping, AcceptsReturn, and VerticalScrollBarVisibility
        ScrollViewer.SetHorizontalScrollBarVisibility(this, ScrollBarVisibility.Disabled);
        ScrollViewer.SetHorizontalScrollMode(this, ScrollMode.Disabled);
        ScrollViewer.SetZoomMode(this, ZoomMode.Disabled);

        TextChanged += RichEditBoxExtended_TextChanged;
        //SizeChanged += RichEditBoxExtended_SizeChanged;
    }

    public string RtfText
    {
        get => (string)GetValue(RtfTextProperty);
        set => SetValue(RtfTextProperty, value);
    }

    private void RichEditBoxExtended_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_lockChangeExecution) return;
        _lockChangeExecution = true;
        RtfText = Text;
        _lockChangeExecution = false;
    }

    private static void RtfTextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var tb = d as RichEditBoxExtended;
        if (tb == null || tb._lockChangeExecution) return;

        tb._lockChangeExecution = true;
        tb.Text = new ReportFormatter(Ioc.Default.GetRequiredService<AppState>())
            .GetText(e.NewValue?.ToString() ?? "");
        tb._lockChangeExecution = false;
    }

    public void UpdateTheme(object sender, RoutedEventArgs e)
    {
        // No-op for TextBox version
    }

    //protected override Size MeasureOverride(Size availableSize)
    //{
        
    //    System.Diagnostics.Debug.WriteLine($"=== MeasureOverride: availableSize.Width={availableSize.Width}, availableSize.Height={availableSize.Height}");

    //    if (double.IsInfinity(availableSize.Width))
    //    {
    //        // Return 0 width to let HorizontalAlignment="Stretch" handle sizing
    //        var result = base.MeasureOverride(new Size(0, availableSize.Height));
    //        System.Diagnostics.Debug.WriteLine($"=== MeasureOverride (forced 0): result.Width={result.Width}, result.Height={result.Height}");
    //        return result;
    //    }

    //    var normalResult = base.MeasureOverride(availableSize);
    //    System.Diagnostics.Debug.WriteLine($"=== MeasureOverride: result.Width={normalResult.Width}, result.Height={normalResult.Height}");
    //    return normalResult;
    //}

    private void RichEditBoxExtended_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"=== SizeChanged: PreviousSize={e.PreviousSize}, NewSize={e.NewSize}");
        this.MeasureOverride(e.NewSize);
    }
}
