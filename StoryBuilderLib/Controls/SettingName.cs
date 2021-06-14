using Microsoft.UI.Xaml.Controls;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using StoryBuilder.Models;
using StoryBuilder.Controllers;
using StoryBuilder.ViewModels;

namespace StoryBuilder.Controls
{
    public sealed class SettingName : ComboBox
    {

        public SettingName() : base()
        {
            DefaultStyleKey = typeof(ComboBox);
            ShellViewModel shell = Ioc.Default.GetService<ShellViewModel>();
            this.ItemsSource = shell.StoryModel.StoryElements.Settings;
            //TODO: Subscribe to change for StoryModel?
        }
    }
}
