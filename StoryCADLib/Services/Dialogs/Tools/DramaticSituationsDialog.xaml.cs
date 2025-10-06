using StoryCAD.ViewModels.Tools;

namespace StoryCAD.Services.Dialogs.Tools;

public sealed partial class DramaticSituationsDialog
{
    public DramaticSituationsDialog()
    {
        InitializeComponent();
    }

    public DramaticSituationsViewModel DramaticSituationsVm => Ioc.Default.GetService<DramaticSituationsViewModel>();
}
