using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using StoryBuilder.Models;

namespace StoryBuilder.Controls
{
    public sealed class ProblemName : ComboBox
    {

        public ProblemName() : base()
        {
            DefaultStyleKey = typeof(ComboBox);
            ItemsSource = GlobalData.StoryModel.StoryElements.Problems;
            //TODO: Subscribe to change for StoryModel?
        }
    }
}
