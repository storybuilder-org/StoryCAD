using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using StoryBuilder.ViewModels;

namespace StoryBuilder.Controls
{
    public sealed class ProblemName : ComboBox
    {

        public ProblemName() : base()
        {
            DefaultStyleKey = typeof(ComboBox);
            ShellViewModel shell = Ioc.Default.GetService<ShellViewModel>();
            ItemsSource = shell.StoryModel.StoryElements.Problems;
            //TODO: Subscribe to change for StoryModel?
        }
    }
}
