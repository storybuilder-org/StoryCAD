using StoryCADLib.ViewModels.Tools;

namespace StoryCADLib.Controls;

public sealed partial class Traits
{
    public Traits()
    {
        InitializeComponent();
    }

    public TraitsViewModel TraitVm => Ioc.Default.GetService<TraitsViewModel>();
}
