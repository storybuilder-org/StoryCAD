using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCAD.Controls;
using StoryCAD.ViewModels;

namespace StoryCAD.Views;

public sealed partial class OverviewPage : BindablePage
{

    public OverviewViewModel OverviewVm => Ioc.Default.GetService<OverviewViewModel>();
    public ShellViewModel ShellVM => Ioc.Default.GetService<ShellViewModel>();

    public OverviewPage()
    {
        InitializeComponent();
        DataContext = OverviewVm;
    }
}