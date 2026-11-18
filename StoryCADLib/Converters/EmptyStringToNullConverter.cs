using Microsoft.UI.Xaml.Data;

namespace StoryCADLib.Converters;

/// <summary>
/// For the SelectedItem or SelectedValue of a non-editable ComboBox bound to a string.
/// A ComboBox ignores a SelectedItem that is not in its list, so an empty string
/// leaves the previous element's choice on screen (#1422: a new Character showed the
/// last Character's Story Role). Null clears the selection. ConvertBack turns null
/// back into an empty string so the view model never holds null.
/// </summary>
public class EmptyStringToNullConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is string text && text.Length == 0 ? null : value;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => value ?? string.Empty;
}
