using StoryCADLib.ViewModels.Tools;

namespace StoryCADLib.Controls;

public sealed partial class Traits : UserControl
{
    public Traits()
    {
        InitializeComponent();
    }

    public TraitsViewModel TraitVm => Ioc.Default.GetService<TraitsViewModel>();
}
