using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using StoryCAD.Services.Reports;

namespace StoryCAD.Controls
{
#if !HAS_UNO
    public partial class RichEditBoxExtended : RichEditBox
    {
        public static readonly DependencyProperty RtfTextProperty =
            DependencyProperty.Register(
                nameof(RtfText), typeof(string), typeof(RichEditBoxExtended),
                new PropertyMetadata(default(string), RtfTextPropertyChanged));

        private bool _lockChangeExecution;

        public RichEditBoxExtended()
        {
            TextChanged += RichEditBoxExtended_TextChanged;
            TextAlignment = TextAlignment.Left;
            CornerRadius = new(5);

            PointerEntered += ((s, e) => UpdateTheme(null, null));
            Loaded += UpdateTheme;
            UpdateTheme(null, null);
        }

        public void UpdateTheme(object sender, RoutedEventArgs e)
        {
            var theme = ActualTheme;
            var format = Document.GetDefaultCharacterFormat();
            format.ForegroundColor = theme == ElementTheme.Dark
                ? Colors.White
                : Colors.Black;
            Document.SetDefaultCharacterFormat(format);
        }

        public string RtfText
        {
            get => (string)GetValue(RtfTextProperty);
            set => SetValue(RtfTextProperty, value);
        }

        private void RichEditBoxExtended_TextChanged(object sender, RoutedEventArgs e)
        {
            if (_lockChangeExecution) return;
            _lockChangeExecution = true;

            Document.GetText(TextGetOptions.None, out var plain);
            if (string.IsNullOrWhiteSpace(plain))
                RtfText = "";
            else
            {
                Document.GetText(TextGetOptions.FormatRtf, out var rtf);
                RtfText = rtf.TrimEnd('\0');
            }

            _lockChangeExecution = false;
        }

        private static void RtfTextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var rtb = d as RichEditBoxExtended;
            if (rtb == null || rtb._lockChangeExecution) return;

            rtb._lockChangeExecution = true;
            var wasReadOnly = rtb.IsReadOnly;
            rtb.IsReadOnly = false;

            rtb.Document.SetText(
                TextSetOptions.FormatRtf | TextSetOptions.ApplyRtfDocumentDefaults,
                rtb.RtfText ?? "");

            rtb.IsReadOnly = wasReadOnly;
            rtb._lockChangeExecution = false;
        }
    }
#else
    public partial class RichEditBoxExtended : TextBox
    {
        public static readonly DependencyProperty RtfTextProperty =
            DependencyProperty.Register(
                nameof(RtfText), typeof(string), typeof(RichEditBoxExtended),
                new PropertyMetadata(default(string), RtfTextPropertyChanged));

        public RichEditBoxExtended()
        {
            TextWrapping = TextWrapping.Wrap;
        }

        public string RtfText
        {
            get => Text;
            set => Text = value ?? "";
        }

        private static void RtfTextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var tb = d as RichEditBoxExtended;
            if (tb == null) return;
            
            tb.Text =new ReportFormatter(Ioc.Default.GetRequiredService<AppState>())
                .GetText(e.NewValue.ToString()?? "");
        }
        public void UpdateTheme(object sender, RoutedEventArgs e)
        {
        }
    }
#endif
}
