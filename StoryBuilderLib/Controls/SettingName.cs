using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using StoryBuilder.Models;
using StoryBuilder.ViewModels;

namespace StoryBuilder.Controls;

public sealed class SettingName : ComboBox
{

    public SettingName()
    {
        DefaultStyleKey = typeof(ComboBox);
        Loaded += SettingName_Loaded;
    }

    private void SettingName_Loaded(object sender, RoutedEventArgs e)
    {
        StoryModel model = ShellViewModel.GetModel();
        ItemsSource = model.StoryElements.Settings;
    }
}