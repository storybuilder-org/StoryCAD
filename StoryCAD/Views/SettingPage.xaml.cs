using StoryCAD.Services;

namespace StoryCAD.Views;

public sealed partial class SettingPage : Page
{
    public SettingPage()
    {
        InitializeComponent();
        DataContext = SettingVm;
    }

    public SettingViewModel SettingVm => Ioc.Default.GetService<SettingViewModel>();

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        var appState = Ioc.Default.GetRequiredService<AppState>();
        appState.CurrentSaveable = DataContext as ISaveable;
    }
}
