using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using StoryBuilder.Models;

namespace StoryBuilder.Controls
{
    public sealed class SettingName : ComboBox
    {

        public SettingName() : base()
        {
            DefaultStyleKey = typeof(ComboBox);
            ItemsSource = GlobalData.StoryModel.StoryElements.Settings;
            //TODO: Subscribe to change for StoryModel?
        }
    }
}
