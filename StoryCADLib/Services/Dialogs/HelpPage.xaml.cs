using StoryCAD.DAL;

namespace StoryCAD.Services.Dialogs;

public sealed partial class HelpPage : Page
{
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
