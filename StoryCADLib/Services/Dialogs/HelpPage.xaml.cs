using StoryCADLib.DAL;

namespace StoryCADLib.Services.Dialogs;

public sealed partial class HelpPage : Page
{
    public HelpPageViewModel ViewModel { get; } = new();

    public HelpPage()
    {
        InitializeComponent();
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
