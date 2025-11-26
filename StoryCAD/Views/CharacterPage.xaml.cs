using StoryCADLib.Services;

namespace StoryCAD.Views;

public sealed partial class CharacterPage : Page
{
    public CharacterPage()
    {
        InitializeComponent();
        DataContext = CharVm;
    }

    public CharacterViewModel CharVm => Ioc.Default.GetService<CharacterViewModel>();

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        var appState = Ioc.Default.GetRequiredService<AppState>();
        appState.CurrentSaveable = DataContext as ISaveable;
    }
}
