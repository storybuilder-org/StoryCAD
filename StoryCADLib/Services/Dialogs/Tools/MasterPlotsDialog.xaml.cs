using StoryCAD.ViewModels.Tools;

namespace StoryCAD.Services.Dialogs.Tools;

public sealed partial class MasterPlotsDialog : Page
{
    public MasterPlotsDialog()
    {
        InitializeComponent();
        DataContext = MasterPlotsVm;
    }

    public MasterPlotsViewModel MasterPlotsVm => Ioc.Default.GetService<MasterPlotsViewModel>();
}
