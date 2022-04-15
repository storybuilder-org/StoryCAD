using CommunityToolkit.Mvvm.DependencyInjection;
using StoryBuilder.ViewModels;

namespace StoryBuilder.Views;

public sealed partial class OverviewPage : BindablePage
{

    public OverviewViewModel OverviewVm => Ioc.Default.GetService<OverviewViewModel>();

    public OverviewPage()
    {
        InitializeComponent();
        DataContext = OverviewVm;
    }
}