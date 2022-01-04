using Microsoft.UI.Xaml.Data;
using System;
using System.Globalization;

namespace StoryBuilder.Converters;

public class StringToGlyphConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value.GetType() != typeof(string))
        {
            return null;
        }

        string glyph = (value as string)?.Substring(3, 4); // for example: &#xe11b; will become e11b 
        char font = (char)int.Parse(glyph, NumberStyles.HexNumber);
        return font;
    }

    // No need to implement converting back on a one-way binding 
    public object ConvertBack(object value, Type targetType,
        object parameter, string language)
    {
        throw new NotImplementedException();
    }
}