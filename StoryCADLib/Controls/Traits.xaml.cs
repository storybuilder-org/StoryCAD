using StoryCAD.ViewModels.Tools;

namespace StoryCAD.Controls;

public sealed partial class Traits
{
    public Traits()
    {
        InitializeComponent();
    }

    public TraitsViewModel TraitVm => Ioc.Default.GetService<TraitsViewModel>();
}
