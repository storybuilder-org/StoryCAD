using StoryCAD.Services;

namespace StoryCAD.Views;

// Note: XAML updated for responsive layout; code-behind unchanged.
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
