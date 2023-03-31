using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using StoryCAD.Models;
using StoryCAD.ViewModels;

namespace StoryCAD.Controls;

public sealed class CharacterName : ComboBox
{

    public CharacterName()
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