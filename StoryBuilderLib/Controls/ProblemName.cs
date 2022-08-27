using Microsoft.UI.Xaml.Controls;
using StoryBuilder.Models;
using StoryBuilder.ViewModels;

namespace StoryBuilder.Controls;

public sealed class ProblemName : ComboBox
{

    public ProblemName()
    {
        DefaultStyleKey = typeof(ComboBox);
        Loaded += ProblemName_Loaded;
    }

    private void ProblemName_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        StoryModel model = ShellViewModel.GetModel();
        ItemsSource = model.StoryElements.Problems;
    }
}