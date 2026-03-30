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

    /// <summary>
    ///     Handles the Delete My Data button click.
    ///     Shows a confirmation dialog, deletes backend + local data
    ///     on confirm, then exits the application.
    /// </summary>
    private async void DeleteMyData_Click(object sender, RoutedEventArgs e)
    {
        ContentDialog confirmDialog = new()
        {
            Title = "Delete My Data?",
            Content = "This will permanently delete:\n" +
                      "\u2022 Your account from our servers\n" +
                      "\u2022 Error reporting preferences\n" +
                      "\u2022 Version tracking history\n" +
                      "\u2022 Newsletter subscription\n\n" +
                      "Your local story files will NOT be deleted.\n\n" +
                      "StoryCAD will close after deletion.",
            PrimaryButtonText = "Delete My Data",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = this.XamlRoot
        };

        var result = await confirmDialog.ShowAsync();
        if (result != ContentDialogResult.Primary)
            return;

        bool success = await PreferencesVm.DeleteMyDataAsync();

        if (success)
        {
            ContentDialog goodbyeDialog = new()
            {
                Title = "Data Deleted",
                Content = "Your data has been deleted. Thank you for using StoryCAD.",
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await goodbyeDialog.ShowAsync();

            // Exit the application
            Application.Current.Exit();
        }
        else
        {
            ContentDialog failDialog = new()
            {
                Title = "Deletion Failed",
                Content = "We could not reach our server to delete your data. " +
                          "Your local data has not been changed.\n\n" +
                          "Please try again later, or contact support@storybuilder.org.",
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await failDialog.ShowAsync();
        }
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
