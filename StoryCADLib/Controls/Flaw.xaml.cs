using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCAD.ViewModels.Tools;

namespace StoryCAD.Controls;

public sealed partial class Flaw
{
    public FlawViewModel FlawVm => Ioc.Default.GetService<FlawViewModel>();
    public Flaw()
    {
        InitializeComponent();
    }
}