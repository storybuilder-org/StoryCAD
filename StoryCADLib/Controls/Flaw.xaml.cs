using StoryCADLib.ViewModels.Tools;

namespace StoryCADLib.Controls;

public sealed partial class Flaw
{
    public Flaw()
    {
        InitializeComponent();
    }

    public FlawViewModel FlawVm => Ioc.Default.GetService<FlawViewModel>();
}
