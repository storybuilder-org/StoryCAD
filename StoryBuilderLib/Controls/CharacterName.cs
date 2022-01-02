
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using StoryBuilder.ViewModels;

namespace StoryBuilder.Controls
{
    public sealed class CharacterName : ComboBox
    {

        public CharacterName() : base()
        {
            DefaultStyleKey = typeof(ComboBox);
            ShellViewModel shell = Ioc.Default.GetService<ShellViewModel>();
            ItemsSource = shell.StoryModel.StoryElements.Characters;
            
            //TODO: Subscribe to change for StoryModel?
        }
    }
}
