using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Windows.AppLifecycle;
using StoryCAD.Services.Logging;
using StoryCAD.ViewModels;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.ViewManagement;
using WinUIEx;

namespace StoryCAD.Models;

/// <summary>
/// This class contains window (mainwindow) related items ect.
/// </summary>
public class Windowing : ObservableRecipient
{
    private void OnPropertyChanged(object sender, PropertyChangedEventArgs e) { }

    /// <summary>
    /// A pointer to the App Window (MainWindow) handle
    /// </summary>
    public IntPtr WindowHandle;

    /// Tools that copy data into StoryElements must access and update the viewmodel currently 
    /// active viewmodel at the time the tool is invoked. The viewmodel type is identified
    /// by the navigation service page key.
    public string PageKey;

    // MainWindow is the main window displayed by the app. It's an instance of
    // WinUIEx's WindowEx, which is an extension of Microsoft.Xaml.UI.Window 
    // and which hosts a Frame holding 
    public WindowEx MainWindow;

    /// <summary>
    // A defect in early WinUI 3 Win32 code is that ContentDialog
    // controls don't have an established XamlRoot. A workaround
    // is to assign the dialog's XamlRoot to the root of a visible
    // Page. The Shell page's XamlRoot is stored here and accessed wherever needed. 
    /// </summary>
    public XamlRoot XamlRoot;

    /// <summary>
    /// A univerisal dispatcher to show messages/change UI from 
    /// a non UI thread. Example: Showing a warning from backup.
    /// </summary>
    public DispatcherQueue GlobalDispatcher = null;

    private ElementTheme _requestedTheme = ElementTheme.Default;
    public ElementTheme RequestedTheme
    {
        get => _requestedTheme;
        set => SetProperty(ref _requestedTheme, value);
    }

    /// <summary>
    /// Returns the users accent color
    /// (Set in Windows Settings)
    /// </summary>
    public Color AccentColor => new UISettings().GetColorValue(UIColorType.Accent);


    // Visual changes
    private SolidColorBrush _primaryBrush;
    /// <summary>
    /// Sets the shell color
    /// </summary>
    public SolidColorBrush PrimaryColor
    {
        get => _primaryBrush;
        set => SetProperty(ref _primaryBrush, value);
    }

    private SolidColorBrush _secondaryBrush;
    /// <summary>
    /// Handles various other colorations
    /// </summary>
    public SolidColorBrush SecondaryColor
    {
        get => _secondaryBrush;
        set => SetProperty(ref _secondaryBrush, value);
    }

    /// <summary>
    /// This is a color that should in most cases
    /// constrast the users accent color
    /// </summary>
    public SolidColorBrush ContrastColor
    {
        get
        {
            Color Contrast = AccentColor;
            Contrast.R = (byte)(Contrast.R * 1.4);
            Contrast.B = (byte)(Contrast.B * 1.4);
            Contrast.G = (byte)(Contrast.G * 1.4);
            return new SolidColorBrush(Contrast);
        }
    }

    /// <summary>
    /// This will dynamically update the title based
    /// on the current conditions of the app.
    /// </summary>
    public void UpdateWindowTitle()
    {
        string BaseTitle = "StoryCAD ";

        //Devloper/Unoffical Build title warning
        if (Ioc.Default.GetRequiredService<AppState>().DeveloperBuild)
        {
            BaseTitle += "(DEV BUILD) ";
        }

        //Open file check
        ShellViewModel ShellVM = Ioc.Default.GetRequiredService<ShellViewModel>();
        if (ShellVM.StoryModel != null && ShellVM.DataSource != null && ShellVM.DataSource.Count > 0)
        {
            BaseTitle += $"- Currently editing {ShellVM.StoryModel.ProjectFilename.Replace(".stbx", "")} ";
        }

        //Set window Title.
        MainWindow.Title = BaseTitle;
    }

    /// <summary>
    /// This will update the elements of the UI to 
    /// match the theme set in RequestedTheme.
    /// </summary>
    public void UpdateUIToTheme()
    {
        (MainWindow.Content as FrameworkElement).RequestedTheme = RequestedTheme;
    }

    /// <summary>
    /// This takes a ContentDialog and shows it to the user
    /// It will handle themeing, XAMLRoot and showing the dialog.
    /// </summary>
    /// <returns>A ContentDialogResult enum value.</returns>
    public async Task<ContentDialogResult> ShowContentDialog(ContentDialog Dialog)
    {
        //Set XAML root and correct theme.
        Dialog.XamlRoot = XamlRoot;
        Dialog.RequestedTheme = RequestedTheme;

        return await Dialog.ShowAsync();
    }


    /// <summary>
    /// When a second instance is opened, this code will be ran on the main (first) instance
    /// It will bring up the main window.
    /// </summary>
    public void ActivateMainInstance(object sender, AppActivationArguments e)
    {
        Windowing wnd = Ioc.Default.GetRequiredService<Windowing>();
        wnd.MainWindow.Restore(); //Resize window and unminimize window
        wnd.MainWindow.BringToFront(); //Bring window to front

        try
        {
            wnd.GlobalDispatcher.TryEnqueue(() =>
            {
                Ioc.Default.GetRequiredService<ShellViewModel>().ShowMessage(LogLevel.Warn, "You can only have one file open at once", false);
            });
        }
        finally { }
    }


    /// <summary>
    /// Shows an error message to the user that there's an issue
    /// with the app and it needs to be reinstalled
    /// As of the RemoveInstallService merge, this is theoretically 
    /// impossible to occur but it should stay incase something
    /// goes wrong with resource loading.
    /// </summary>
    /// <exception cref="MissingManifestResourceException"></exception>
    public async void ShowResourceErrorMessage()
    {
        await new ContentDialog()
        {
            XamlRoot = Ioc.Default.GetRequiredService<Windowing>().XamlRoot,
            Title = "Error loading resources",
            Content = "An error has occurred, please reinstall or update StoryCAD to continue.",
            CloseButtonText = "Close",
            RequestedTheme = RequestedTheme
        }.ShowAsync();
        throw new MissingManifestResourceException();
    }
}