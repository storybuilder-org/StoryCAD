using Microsoft.UI;
using Microsoft.UI.Text;
using StoryCADLib.Services.Reports;

namespace StoryCADLib.Controls;

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
        CornerRadius = new CornerRadius(5);
        PointerEntered += (s, e) => UpdateTheme(null, null);
        // Expander content is often created after the bound RtfText was set; re-apply on Loaded
        // so plain-text Collaborator Values etc. appear when the expander opens.
        Loaded += OnLoadedApplyRtfText;
        UpdateTheme(null, null);
    }

    private void OnLoadedApplyRtfText(object sender, RoutedEventArgs e)
    {
        UpdateTheme(sender, e);
        if (_lockChangeExecution)
            return;
        var text = RtfText ?? "";
        if (string.IsNullOrEmpty(text))
            return;
        _lockChangeExecution = true;
        var wasReadOnly = IsReadOnly;
        IsReadOnly = false;
        var isRtf = text.TrimStart().StartsWith(@"{\rtf", StringComparison.Ordinal);
        Document.SetText(
            isRtf
                ? TextSetOptions.FormatRtf | TextSetOptions.ApplyRtfDocumentDefaults
                : TextSetOptions.None,
            text);
        IsReadOnly = wasReadOnly;
        _lockChangeExecution = false;
    }

    public string RtfText
    {
        get => (string)GetValue(RtfTextProperty);
        set => SetValue(RtfTextProperty, value);
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

    private void RichEditBoxExtended_TextChanged(object sender, RoutedEventArgs e)
    {
        if (_lockChangeExecution)
        {
            return;
        }

        // Collaborator #237 items 6 and 7. RichEditBox raises TextChanged after a programmatic
        // SetText returns, so the lock above does not cover it. Without a focus check the
        // handler then writes the document back over the bound property: as RTF when the box
        // rendered the plain text Collaborator wrote (the Scorecard S01 file held Concept as RTF
        // and Premise as plain text, one box realized and one not), and as "" when the box had
        // not rendered it yet (an empty page over accepted text, and SaveModel then wrote the
        // empties to the outline). Only the writer changes text, and the writer has focus.
        if (FocusState == FocusState.Unfocused)
        {
            return;
        }

        _lockChangeExecution = true;
        Document.GetText(TextGetOptions.None, out var plain);
        if (string.IsNullOrWhiteSpace(plain))
        {
            RtfText = "";
        }
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
        if (rtb == null || rtb._lockChangeExecution)
        {
            return;
        }

        rtb._lockChangeExecution = true;
        var wasReadOnly = rtb.IsReadOnly;
        rtb.IsReadOnly = false;
        // Collaborator Accept writes plain text. FormatRtf on non-RTF leaves the box blank
        // and TextChanged can clear the bound property. Use plain set when not RTF.
        var text = rtb.RtfText ?? "";
        var isRtf = text.TrimStart().StartsWith(@"{\rtf", StringComparison.Ordinal);
        rtb.Document.SetText(
            isRtf
                ? TextSetOptions.FormatRtf | TextSetOptions.ApplyRtfDocumentDefaults
                : TextSetOptions.None,
            text);
        rtb.IsReadOnly = wasReadOnly;
        rtb._lockChangeExecution = false;
    }
}