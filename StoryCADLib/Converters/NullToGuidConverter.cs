using Microsoft.UI.Xaml.Data;

namespace StoryCAD.Converters;
public class NullToGuidConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value ?? Guid.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return value is Guid guid ? guid : Guid.Empty;
    }
}
