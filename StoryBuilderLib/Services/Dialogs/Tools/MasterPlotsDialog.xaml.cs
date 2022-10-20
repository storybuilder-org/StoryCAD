using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using StoryBuilder.ViewModels.Tools;

namespace StoryBuilder.Services.Dialogs.Tools;

public sealed partial class MasterPlotsDialog : Page
{
    public MasterPlotsViewModel MasterPlotsVm => Ioc.Default.GetService<MasterPlotsViewModel>();
    public MasterPlotsDialog()
    {
        InitializeComponent();
        DataContext = MasterPlotsVm;
    }
}