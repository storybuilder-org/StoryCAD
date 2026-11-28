using Microsoft.UI.Xaml.Data;
using StoryCADLib.Services.Reports;

namespace StoryCADLib.Converters;

/// <summary>
///     Converts the rich text of a field, such as the description of a Scene, to plain text
///     for a TextBlock (issue #1546).
/// </summary>
public class RtfToPlainTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return ToPlainText(value as string);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotSupportedException();
    }

    /// <summary>The text of a rich-text value, with no markup. Plain text comes back as it is.</summary>
    public static string ToPlainText(string rtf)
    {
        if (string.IsNullOrWhiteSpace(rtf))
        {
            return string.Empty;
        }

        try
        {
            return new RichTextStripper().StripRichTextFormat(rtf).Trim();
        }
        catch (Exception)
        {
            return rtf.Trim();
        }
    }
}
