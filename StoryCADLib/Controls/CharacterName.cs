using Microsoft.UI.Xaml;

namespace StoryCAD.Controls;

public sealed class CharacterName : ComboBox
{
    //TODO: Get rid of this control one all uses are converted to ComboBox
    public CharacterName()
    {
        DefaultStyleKey = typeof(ComboBox);
        CornerRadius = new(4);
        Loaded += CharacterName_Loaded; 
    }

    private void CharacterName_Loaded(object o, RoutedEventArgs routedEventArgs)
    {
        StoryModel model = ShellViewModel.GetModel();
        ItemsSource = model.StoryElements.Characters;
    }
}