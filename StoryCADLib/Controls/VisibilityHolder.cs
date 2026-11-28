using Microsoft.UI.Xaml;

namespace StoryCADLib.Controls;

/// <summary>
///     Holds one Visibility value as a resource, so that a DataTemplate can bind to a
///     property of the control that owns it.
/// </summary>
public sealed class VisibilityHolder : DependencyObject
{
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(Visibility), typeof(VisibilityHolder), new PropertyMetadata(Visibility.Collapsed));

    public Visibility Value
    {
        get => (Visibility)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }
}
