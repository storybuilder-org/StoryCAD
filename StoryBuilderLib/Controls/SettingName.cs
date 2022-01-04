using Microsoft.UI.Xaml.Controls;
using StoryBuilder.Models;
using StoryBuilder.ViewModels;

namespace StoryBuilder.Controls;

public sealed class SettingName : ComboBox
{

    public SettingName() : base()
    {
        DefaultStyleKey = typeof(ComboBox);
        StoryModel model = ShellViewModel.GetModel();
        ItemsSource = model.StoryElements.Settings;
    }
}