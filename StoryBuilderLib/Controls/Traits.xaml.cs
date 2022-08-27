using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using StoryBuilder.ViewModels.Tools;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace StoryBuilder.Controls;

public sealed partial class Traits
{
    public TraitsViewModel TraitVm => Ioc.Default.GetService<TraitsViewModel>();
    public Traits()
    {
        InitializeComponent();
    }
}