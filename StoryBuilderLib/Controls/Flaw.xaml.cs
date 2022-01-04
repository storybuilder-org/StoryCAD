using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using StoryBuilder.ViewModels.Tools;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace StoryBuilder.Controls;

public sealed partial class Flaw : UserControl
{
    public FlawViewModel FlawVm => Ioc.Default.GetService<FlawViewModel>();
    public Flaw()
    {
        InitializeComponent();
    }
}