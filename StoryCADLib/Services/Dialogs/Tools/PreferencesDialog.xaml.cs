using System.Diagnostics;
using StoryCADLib.ViewModels.Tools;

namespace StoryCADLib.Services.Dialogs.Tools;

public sealed partial class PreferencesDialog : Page
{
    public PreferencesDialog()
    {
        InitializeComponent();
        DataContext = PreferencesVm;
        ShowInfo();
    }

    public PreferencesViewModel PreferencesVm => Ioc.Default.GetService<PreferencesViewModel>();
    public LogService Logger => Ioc.Default.GetRequiredService<LogService>();
    public AppState State => Ioc.Default.GetRequiredService<AppState>();
    public Windowing window => Ioc.Default.GetRequiredService<Windowing>();

    /// <summary>
    ///     Sets info text for changelog and dev menu
    /// </summary>
    private async void ShowInfo()
    {
        DevInfo.Text = Logger.SystemInfo();

        var logger = Ioc.Default.GetService<ILogService>();
        var appState = Ioc.Default.GetService<AppState>();
        Changelog.Text = await new Changelog(logger, appState).GetChangelogText();

        if (PreferencesVm.WrapNodeNames == TextWrapping.Wrap)
        {
            TextWrap.IsChecked = true;
        }
        else
        {
            TextWrap.IsChecked = false;
        }

        SearchEngine.SelectedIndex = (int)PreferencesVm.PreferredSearchEngine;

        //Dev Menu is only shown on unofficial builds
        if (!State.DeveloperBuild)
        {
            PivotView.TabItems.Remove(Dev);
        }
    }

    /// <summary>
    ///     Opens the Log Folder
    /// </summary>
    private void OpenLogFolder(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = Path.Combine(Ioc.Default.GetRequiredService<AppState>().RootDirectory, "Logs"),
            UseShellExecute = true, Verb = "open"
        });
    }

    private void OnBackupPathSelected(object sender, string path)
    {
        PreferencesVm.BackupDirectory = path;
    }

    private void OnProjectPathSelected(object sender, string path)
    {
        PreferencesVm.ProjectDirectory = path;
    }

    /// <summary>
    ///     This function throws an error as it is used to test errors.
    /// </summary>
    private void ThrowException(object sender, RoutedEventArgs e)
    {
        throw new NotImplementedException(
            "This is a test exception thrown by the developer Menu and should be ignored.");
    }

    /// <summary>
    ///     This sets init to false, meaning the next time
    ///     StoryCAD is opened the PreferencesInitialization
    ///     page will be shown.
    /// </summary>
    private void SetInitToFalse(object sender, RoutedEventArgs e)
    {
        PreferencesVm.PreferencesInitialized = false;
    }

    /// <summary>
    ///     Refreshes developer stats such as sys info.
    /// </summary>
    public void RefreshDevStats(object sender, RoutedEventArgs e)
    {
        DevInfo.Text = Logger.SystemInfo();
    }

    /// <summary>
    ///     This toggles the status of preferences.TextWrapping
    /// </summary>
    private void ToggleWrapping(object sender, RoutedEventArgs e)
    {
        if ((sender as CheckBox).IsChecked == true)
        {
            PreferencesVm.WrapNodeNames = TextWrapping.Wrap;
        }
        else
        {
            PreferencesVm.WrapNodeNames = TextWrapping.NoWrap;
        }
    }


    /// <summary>
    ///     This is used to open social media links when
    ///     they clicked in the about page.
    /// </summary>
    private void OpenURL(object sender, RoutedEventArgs e)
    {
        //Shell execute will just open the app in the default for
        //That protocol i.e firefox for https:// and spark for mailto://
        ProcessStartInfo URL = new() { UseShellExecute = true };

        //Select URL based on tag of button that was clicked.
        switch ((sender as Button).Tag)
        {
            case "Mail":
                URL.FileName = "mailto://support@storybuilder.org";
                break;
            case "Discord":
                URL.FileName = "http://discord.gg/bpCyAQnWCa";
                break;
            case "Github":
                URL.FileName = "https://github.com/storybuilder-org/StoryCAD";
                break;
            case "FaceBook":
                URL.FileName = "https://www.facebook.com/StoryCAD";
                break;
            case "Twitter":
                URL.FileName = "https://twitter.com/StoryCAD";
                break;
            case "Youtube":
                URL.FileName = "https://www.youtube.com/channel/UCoRI7Q8r-_T5O1jKrEJsvDg";
                break;
            case "Website":
                URL.FileName = "https://storybuilder.org/";
                break;
        }

        //Launch relevant app (browser/mail client)
        Process.Start(URL);
    }
}
