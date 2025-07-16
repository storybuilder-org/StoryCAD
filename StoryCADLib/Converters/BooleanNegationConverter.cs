// BooleanNegationConverter.cs
using Microsoft.UI.Xaml.Data;

namespace StoryCAD.Converters
{
    /// <summary>Negates a Boolean for XAML bindings.</summary>
    public sealed class BooleanNegationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value is bool b && !b;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value is bool b && !b;
        }
    }
}