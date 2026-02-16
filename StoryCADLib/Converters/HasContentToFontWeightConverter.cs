using Microsoft.UI.Xaml.Data;

namespace StoryCADLib.Converters;

/// <summary>
/// Converts a string value to FontWeight: Bold (700) if non-empty, Normal (400) if empty/null.
/// Used by Expander headers to indicate when RichEditBox fields have content.
/// </summary>
public class HasContentToFontWeightConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        bool hasContent = !string.IsNullOrEmpty(value as string);
        return new Windows.UI.Text.FontWeight { Weight = (ushort)(hasContent ? 700 : 400) };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}
