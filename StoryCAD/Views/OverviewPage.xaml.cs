using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCAD.ViewModels;

namespace StoryCAD.Views;

public sealed partial class OverviewPage : BindablePage
{

    public OverviewViewModel OverviewVm => Ioc.Default.GetService<OverviewViewModel>();

    public OverviewPage()
    {
        InitializeComponent();
        DataContext = OverviewVm;
    }
}