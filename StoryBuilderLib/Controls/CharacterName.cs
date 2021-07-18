using Microsoft.UI.Xaml.Controls;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using StoryBuilder.ViewModels;

namespace StoryBuilder.Controls
{
    public sealed class CharacterName : ComboBox
    {
        
        public CharacterName() : base()
        {
            DefaultStyleKey = typeof(ComboBox);
            ShellViewModel shell = Ioc.Default.GetService<ShellViewModel>();
            this.ItemsSource = shell.StoryModel.StoryElements.Characters;
            //this.DisplayMemberPath = "Name";
            //TODO: Subscribe to change for StoryModel?
        }
    }
}
