using CommunityToolkit.Mvvm.DependencyInjection;
using StoryBuilder.ViewModels.Tools;

namespace StoryBuilder.Controls;

public sealed partial class Flaw
{
    public FlawViewModel FlawVm => Ioc.Default.GetService<FlawViewModel>();
    public Flaw()
    {
        InitializeComponent();
    }
}