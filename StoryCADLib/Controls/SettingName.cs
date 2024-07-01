using Microsoft.UI.Xaml;

namespace StoryCAD.Controls;

public sealed class SettingName : ComboBox
{

    public SettingName()
    {
        DefaultStyleKey = typeof(ComboBox);
        CornerRadius = new(4);
        Loaded += SettingName_Loaded;
    }

    private void SettingName_Loaded(object sender, RoutedEventArgs e)
    {
        StoryModel model = ShellViewModel.GetModel();
        ItemsSource = model.StoryElements.Settings;
    }
}