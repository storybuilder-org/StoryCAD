using Microsoft.UI.Xaml.Navigation;
using StoryCAD.Models;
using StoryCAD.Services;
using CommunityToolkit.Mvvm.DependencyInjection;

namespace StoryCAD.Views;

public sealed partial class OverviewPage : Page
{

    public OverviewViewModel OverviewVm => Ioc.Default.GetService<OverviewViewModel>();
    public ShellViewModel ShellVM => Ioc.Default.GetService<ShellViewModel>();

    public OverviewPage()
    {
        InitializeComponent();
            // Responsive XAML: Pivot wrapped in ScrollViewer; containers stretch; child mins removed.
        DataContext = OverviewVm;
    }
    
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        var appState = Ioc.Default.GetRequiredService<AppState>();
        appState.CurrentSaveable = DataContext as ISaveable;
    }
}