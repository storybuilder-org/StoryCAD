using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using StoryBuilder.ViewModels;

namespace StoryBuilder.Controls;

public sealed class CharacterName : ComboBox
{

    public CharacterName()
    {
        DefaultStyleKey = typeof(ComboBox);
        Loaded += CharacterName_Loaded; 
    }

    private void CharacterName_Loaded(object o, RoutedEventArgs routedEventArgs)
    {
        ItemsSource = ShellViewModel.GetModel().StoryElements.Characters;
    }
}