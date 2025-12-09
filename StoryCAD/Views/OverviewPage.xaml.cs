using StoryCADLib.Services;

namespace StoryCAD.Views;

public sealed partial class OverviewPage : Page
{
    public OverviewPage()
    {
        InitializeComponent();
        DataContext = OverviewVm;
    }

    public OverviewViewModel OverviewVm => Ioc.Default.GetService<OverviewViewModel>();

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        var appState = Ioc.Default.GetRequiredService<AppState>();
        appState.CurrentSaveable = DataContext as ISaveable;
    }
}
