using Microsoft.UI.Xaml.Controls;
using StoryBuilder.Models;
using StoryBuilder.ViewModels;

namespace StoryBuilder.Controls
{
    public sealed class CharacterName : ComboBox
    {

        public CharacterName() : base()
        {
            DefaultStyleKey = typeof(ComboBox);
            StoryModel model = ShellViewModel.GetModel();
            ItemsSource = model.StoryElements.Characters;
        }
    }
}
