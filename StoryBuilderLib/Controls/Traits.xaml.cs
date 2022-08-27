using CommunityToolkit.Mvvm.DependencyInjection;
using StoryBuilder.ViewModels.Tools;

namespace StoryBuilder.Controls;

public sealed partial class Traits
{
    public TraitsViewModel TraitVm => Ioc.Default.GetService<TraitsViewModel>();
    public Traits() { InitializeComponent(); }
}