using StoryCAD.ViewModels.Tools;

namespace StoryCAD.Controls;

public sealed partial class Traits
{
    public TraitsViewModel TraitVm => Ioc.Default.GetService<TraitsViewModel>();
    public Traits() { InitializeComponent(); }
}