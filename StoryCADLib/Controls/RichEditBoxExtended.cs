using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace StoryCAD.Controls;

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
		//Color fix for dark mode
		TextChanged += RichEditBoxExtended_TextChanged;
        TextAlignment = TextAlignment.Left;
        CornerRadius = new(5);
        HandleColoringOverrides();
	}

	/// <summary>
	/// WinUI3's implementation of RichTextBox is sorta bugged.
	/// Since rich text can set text color, WinUI doesn't color it
	/// to handle theming and since all text is implicitly black unless
	/// otherwise set this makes it impossible to read on dark theme.
	///
	/// Yet also conversely does somewhat account for this wierd edge case
	/// by making black=white and white=black on theming on everything
	///
	/// Since StoryCAD has no option to color text, the only winning
	/// move is not to play and just color the text constantly.
	/// </summary>
	public void HandleColoringOverrides()
    {
	    if (Ioc.Default.GetRequiredService<Windowing>().RequestedTheme == ElementTheme.Dark)
	    {
			/*
		    RichEditTextDocument document = this.Document;

		    // Select the desired text range (e.g., entire document)
		    ITextRange textRange = document.GetRange(0, TextConstants.MaxUnitCount);

		    textRange.CharacterFormat.ForegroundColor = Colors.LightGray; // Set the text color*/
		}
	    else
	    {
		    Foreground = new SolidColorBrush(Colors.Black);
		}
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

			//Workaround for crash if readonly is true
			bool isReadOnly = rtb.IsReadOnly;
			rtb.IsReadOnly = false;

            rtb.Document.SetText(options, rtb.RtfText);
            rtb.IsReadOnly = isReadOnly;
            // get rid of new EOP (cr/lf) somehow
            rtb._lockChangeExecution = false;
        }
    }
}