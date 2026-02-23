using StoryCADLib.DAL;

namespace StoryCADLib.Services.Dialogs;

public sealed partial class HelpPage : Page
{
    public HelpPage()
    {
        InitializeComponent();

        var appState = Ioc.Default.GetRequiredService<AppState>();
        TutorialLink.NavigateUri = new Uri(appState.ManualBaseUrl + "docs/Tutorial%20Creating%20a%20Story/Tutorial_Creating_a_Story.html");
        ManualLink.NavigateUri = new Uri(appState.ManualBaseUrl);
    }

    /// <summary>
    ///     Update preferences to hide/show menu
    /// </summary>
    private async void Clicked(object sender, RoutedEventArgs e)
    {
        var Pref = Ioc.Default.GetService<PreferenceService>();
        Pref.Model.ShowStartupDialog = !(bool)(sender as CheckBox).IsChecked;

        PreferencesIo _prfIo = new();
        await _prfIo.WritePreferences(Pref.Model);
    }
}
