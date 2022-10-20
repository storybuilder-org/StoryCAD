using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace StoryBuilder.Controls;

/// <summary>
/// RichEditBoxExtended inherits from the UI.Xaml.Controls RichExitBox
/// control and adds a DependencyProperty "RtfText", which allows binding
/// (including TwoWay binding) to  RTF text in a ViewModel using the
/// ITextDocument interface.
///
/// Use:
/// Go to your XAML and enter the following:
///     <local:RichTextBoxExtended RtfText="{Binding MyRichText, Mode=TwoWay}"/>
///
/// References:
/// https://stackoverflow.com/questions/26549156/winrt-binding-a-rtf-string-to-a-richeditbox/26549205#26549205
/// https://stackoverflow.com/questions/28909808/richeditbox-two-way-binbing-does-not-work-windows-store-app/28981762#28981762
/// (esp. note Rob Caplan's comment at the end, which is not handled in the provided code.)
/// https://social.msdn.microsoft.com/Forums/en-US/f9a83d4e-26e9-476b-8818-7ccdf91a2341/richeditbox-mvvm-pattern?forum=winappswithcsharp
/// </summary>
public class RichEditBoxExtended : RichEditBox
{
    public static readonly DependencyProperty RtfTextProperty =
        DependencyProperty.Register(
            "RtfText", typeof(string), typeof(RichEditBoxExtended),
            new PropertyMetadata(default(string), RtfTextPropertyChanged));

    private bool _lockChangeExecution;

    public RichEditBoxExtended()
    {
        TextChanged += RichEditBoxExtended_TextChanged;
        TextAlignment = TextAlignment.Left;
        CornerRadius = new(5);
        if (Application.Current.RequestedTheme == ApplicationTheme.Dark) { Foreground = new SolidColorBrush(Colors.LightGray); } //Color fix for dark mode
    }


    public string RtfText
    {
        get => (string) GetValue(RtfTextProperty);
        set => SetValue(RtfTextProperty, value);
    }

    private void RichEditBoxExtended_TextChanged(object sender, RoutedEventArgs e)
    {
        if (!_lockChangeExecution)
        {
            _lockChangeExecution = true;
            Document.GetText(TextGetOptions.None, out string text);
            if (string.IsNullOrWhiteSpace(text))  
            {
                RtfText = "";
            }
            else
            {
                Document.GetText(TextGetOptions.FormatRtf, out text);
                RtfText = text.TrimEnd('\0'); // remove end of string marker
            }
            _lockChangeExecution = false;
        }
    }

    private static void RtfTextPropertyChanged(DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
    {
        TextSetOptions options = TextSetOptions.FormatRtf | TextSetOptions.ApplyRtfDocumentDefaults;
        RichEditBoxExtended rtb = dependencyObject as RichEditBoxExtended;
        if (rtb == null) return;
        if (!rtb._lockChangeExecution)
        {
            rtb._lockChangeExecution = true;
                
            rtb.Document.SetText(options, rtb.RtfText);
            // get rid of new EOP (cr/lf) somehow
            rtb._lockChangeExecution = false;
        }
    }
}