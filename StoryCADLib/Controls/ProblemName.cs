using Microsoft.UI.Xaml;

namespace StoryCAD.Controls;

public sealed class ProblemName : ComboBox
{

    public ProblemName()
    {
        DefaultStyleKey = typeof(ComboBox);
        CornerRadius = new(4);
        Loaded += ProblemName_Loaded;
    }

    private void ProblemName_Loaded(object sender, RoutedEventArgs e)
    {
        StoryModel model = ShellViewModel.GetModel();
        ItemsSource = model.StoryElements.Problems;
    }
}