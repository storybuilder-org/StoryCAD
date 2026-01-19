using StoryCADLib.Services;

namespace StoryCAD.Views;

public sealed partial class StoryWorldPage : Page
{
    public StoryWorldPage()
    {
        InitializeComponent();
        DataContext = StoryWorldVm;
    }

    public StoryWorldViewModel StoryWorldVm => Ioc.Default.GetService<StoryWorldViewModel>();

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        var appState = Ioc.Default.GetRequiredService<AppState>();
        appState.CurrentSaveable = DataContext as ISaveable;
    }
}
