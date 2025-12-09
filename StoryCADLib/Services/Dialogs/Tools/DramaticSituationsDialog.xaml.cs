using StoryCADLib.ViewModels.Tools;

namespace StoryCADLib.Services.Dialogs.Tools;

public sealed partial class DramaticSituationsDialog : Page
{
    public DramaticSituationsDialog()
    {
        InitializeComponent();
    }

    public DramaticSituationsViewModel DramaticSituationsVm => Ioc.Default.GetService<DramaticSituationsViewModel>();
}
