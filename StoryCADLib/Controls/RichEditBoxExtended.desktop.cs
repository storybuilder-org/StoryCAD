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
        TextWrapping = TextWrapping.Wrap;
        AcceptsReturn = true;  // Enable multi-line text entry for proper text wrapping


        // Only set properties that prevent layout issues
        // Let XAML control TextWrapping, AcceptsReturn, and VerticalScrollBarVisibility
        ScrollViewer.SetHorizontalScrollBarVisibility(this, ScrollBarVisibility.Disabled);
        ScrollViewer.SetHorizontalScrollMode(this, ScrollMode.Disabled);
        ScrollViewer.SetZoomMode(this, ZoomMode.Disabled);

        Loaded += RichEditBoxExtended_Loaded;
        TextChanged += RichEditBoxExtended_TextChanged;
        SizeChanged += RichEditBoxExtended_SizeChanged;
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

    private void RichEditBoxExtended_Loaded(object sender, RoutedEventArgs e)
    {
        // Force re-measure when window size changes to trigger text wrapping  
        InvalidateMeasure();
        InvalidateArrange();
        UpdateLayout();
    }

    private void RichEditBoxExtended_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        // Force re-measure when window size changes to trigger text wrapping  
        InvalidateMeasure();
        InvalidateArrange();
        UpdateLayout();
    }
}
