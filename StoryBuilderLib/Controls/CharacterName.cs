
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using StoryBuilder.Models;

namespace StoryBuilder.Controls
{
    public sealed class CharacterName : ComboBox
    {

        public CharacterName() : base()
        {
            DefaultStyleKey = typeof(ComboBox);
            ItemsSource = GlobalData.StoryModel.StoryElements.Characters;
            
            //TODO: Subscribe to change for StoryModel?
        }
    }
}
