using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace StoryCADLib.Controls;

/// <summary>
///     Draws the value of an editable ComboBox in the normal text color.
///     The editable drop-downs show a saved value through PlaceholderText, because
///     WinUI does not draw a bound Text when the page loads (commit 46779289). WinUI
///     draws placeholder text, and the edit box of an editable ComboBox, in the grey
///     placeholder color, so a value the user typed (not in the list) looked unset.
///     Every placeholder that uses this property holds the value itself, never a hint.
///     Do not set it on a ComboBox whose PlaceholderText is a hint.
/// </summary>
public static class ComboBoxValueText
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached("IsEnabled", typeof(bool), typeof(ComboBoxValueText),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ComboBox comboBox)
        {
            return;
        }

        comboBox.Loaded -= ComboBox_Loaded;
        if ((bool)e.NewValue)
        {
            // The edit box of an editable ComboBox takes its Foreground from
            // ComboBox.PlaceholderForeground (WinUI ComboBox template, "EditableText").
            comboBox.SetBinding(ComboBox.PlaceholderForegroundProperty, ForegroundOf(comboBox));
            comboBox.Loaded += ComboBox_Loaded;
        }
    }

    private static void ComboBox_Loaded(object sender, RoutedEventArgs e)
    {
        var comboBox = (ComboBox)sender;
        var editBox = FindTextBox(comboBox);
        // The edit box draws PlaceholderText with its own PlaceholderForeground.
        editBox?.SetBinding(TextBox.PlaceholderForegroundProperty, ForegroundOf(comboBox));
    }

    // Binding to the ComboBox's own Foreground follows a theme change.
    private static Binding ForegroundOf(ComboBox comboBox) =>
        new() { Source = comboBox, Path = new PropertyPath(nameof(Control.Foreground)) };

    private static TextBox FindTextBox(DependencyObject parent)
    {
        var count = VisualTreeHelper.GetChildrenCount(parent);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is TextBox textBox)
            {
                return textBox;
            }

            var found = FindTextBox(child);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
