using System.Diagnostics;
using StoryCADLib.ViewModels.Tools;

namespace StoryCAD.Views;

/// <summary>
/// This Page is displayed if Preferences.Initialise is false.
/// </summary>
public sealed partial class PreferencesInitialization : Page
{
    private InitVM _initVM = Ioc.Default.GetService<InitVM>();

    public PreferencesInitialization()
    {
        InitializeComponent();
    }

    /// <summary>
    ///     Called when a project path is selected from the browse control
    /// </summary>
    private void OnProjectPathSelected(object sender, string path)
    {
        _initVM.ProjectDir = path;
    }

    /// <summary>
    ///     Called when a backup path is selected from the browse control
    /// </summary>
    private void OnBackupPathSelected(object sender, string path)
    {
        _initVM.BackupDir = path;
    }

    /// <summary>
    ///     Opens discord URL in default browser (Via ShellExecute)
    /// </summary>
    public void Discord(object sender, RoutedEventArgs e)
    {
        Process browser = new()
            { StartInfo = new ProcessStartInfo { FileName = "http://discord.gg/bpCyAQnWCa", UseShellExecute = true } };
        browser.Start();
    }

    /// <summary>
    ///     Checks that the Paths, Name and Email aren't blank or null.
    /// </summary>
    public async void Check(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_initVM.Preferences.FirstName))
        {
            _initVM.ErrorMessage = "Please enter your first name";
            return;
        }

        if (string.IsNullOrWhiteSpace(_initVM.Preferences.LastName))
        {
            _initVM.ErrorMessage = "Please enter your last name";
            return;
        }

        if (string.IsNullOrWhiteSpace(_initVM.Preferences.Email))
        {
            _initVM.ErrorMessage = "Please enter your Email";
            return;
        }

        if (!_initVM.Preferences.Email.Contains("@") || !_initVM.Preferences.Email.Contains("."))
        {
            _initVM.ErrorMessage = "Please enter a valid email address.";
            return;
        }

        if (string.IsNullOrWhiteSpace(_initVM.ProjectDir))
        {
            _initVM.ErrorMessage = "Please set a Project path";
            return;
        }

        if (string.IsNullOrWhiteSpace(_initVM.BackupDir))
        {
            _initVM.ErrorMessage = "Please set a Backup path";
            return;
        }

        await _initVM.Save();
        RootFrame.Navigate(typeof(Shell));
    }
}
