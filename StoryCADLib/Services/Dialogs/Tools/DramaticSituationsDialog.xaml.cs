using StoryCAD.ViewModels.Tools;

namespace StoryCAD.Services.Dialogs.Tools;

public sealed partial class DramaticSituationsDialog
{
    public DramaticSituationsViewModel DramaticSituationsVm => Ioc.Default.GetService<DramaticSituationsViewModel>();
    public DramaticSituationsDialog()
    {
        InitializeComponent();
    }
}