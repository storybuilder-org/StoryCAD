using StoryCADLib.Services;

namespace StoryCAD.Views;

public sealed partial class OverviewPage : Page
{
    public OverviewPage()
    {
        InitializeComponent();
        // Responsive XAML: Pivot wrapped in ScrollViewer; containers stretch; child mins removed.
        DataContext = OverviewVm;
    }

    public OverviewViewModel OverviewVm => Ioc.Default.GetService<OverviewViewModel>();
    public ShellViewModel ShellVM => Ioc.Default.GetService<ShellViewModel>();

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        var appState = Ioc.Default.GetRequiredService<AppState>();
        appState.CurrentSaveable = DataContext as ISaveable;
    }
}
