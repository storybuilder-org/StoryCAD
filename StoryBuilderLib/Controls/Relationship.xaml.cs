using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using StoryBuilder.ViewModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace StoryBuilder.Controls
{
    public sealed partial class Relationship : UserControl
    {
        public CharacterViewModel CharVm => Ioc.Default.GetService<CharacterViewModel>();
        public Relationship()
        {
            this.InitializeComponent();
        }
    }
}
