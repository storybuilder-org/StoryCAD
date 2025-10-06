using StoryCAD.ViewModels.Tools;

namespace StoryCAD.Controls;

public sealed partial class Flaw
{
    public Flaw()
    {
        InitializeComponent();
    }

    public FlawViewModel FlawVm => Ioc.Default.GetService<FlawViewModel>();
}
