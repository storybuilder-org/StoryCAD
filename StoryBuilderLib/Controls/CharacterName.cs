using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using StoryBuilder.Models;
using StoryBuilder.ViewModels;

namespace StoryBuilder.Controls;

public sealed class CharacterName : ComboBox
{

    public CharacterName() : base()
    {
        DefaultStyleKey = typeof(ComboBox);
        Loaded += CharacterName_Loaded; 
    }

    private void CharacterName_Loaded(object o, RoutedEventArgs routedEventArgs)
    {
        StoryModel model = ShellViewModel.GetModel();
        ItemsSource = model.StoryElements.Characters;
    }
}