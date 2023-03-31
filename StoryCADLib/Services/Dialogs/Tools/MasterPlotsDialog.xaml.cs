using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using StoryCAD.ViewModels.Tools;

namespace StoryCAD.Services.Dialogs.Tools;

public sealed partial class MasterPlotsDialog : Page
{
    public MasterPlotsViewModel MasterPlotsVm => Ioc.Default.GetService<MasterPlotsViewModel>();
    public MasterPlotsDialog()
    {
        InitializeComponent();
        DataContext = MasterPlotsVm;
    }
}