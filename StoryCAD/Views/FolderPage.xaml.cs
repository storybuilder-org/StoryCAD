using StoryCAD.Services;

namespace StoryCAD.Views;

public sealed partial class FolderPage : Page
{
    public FolderPage()
    {
        InitializeComponent();
        DataContext = FolderVm;
    }

    public FolderViewModel FolderVm => Ioc.Default.GetService<FolderViewModel>();

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        var appState = Ioc.Default.GetRequiredService<AppState>();
        appState.CurrentSaveable = DataContext as ISaveable;
    }
}
