using System;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Controls;
namespace StoryBuilder.Converters
{
    public class StringToComboBoxItem : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value.GetType().Equals(typeof(ComboBoxItem)))
            {
                return value;
            }
            return new ComboBoxItem() { Content = value };
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value as string;
        }
    }
}