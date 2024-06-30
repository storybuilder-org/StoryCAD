using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Windows.AppLifecycle;
using StoryCAD.Exceptions;
using StoryCAD.ViewModels;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using System.ComponentModel;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.ViewManagement;
using NLog;
using StoryCAD.Services.Logging;
using WinUIEx;
using LogLevel = StoryCAD.Services.Logging.LogLevel;

namespace StoryCAD.Models;

/// <summary>
/// This class contains window (MainWindow) related items etc.
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

    /// <summary>
    /// This is used to track if a ContentDialog is already open
    /// within ShowContentDialog() as spwaning two at once will 
    /// cause a crash.
    /// </summary>
    private bool _IsContentDialogOpen = false;
    
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
    public async void UpdateUIToTheme()
    {
        (MainWindow.Content as FrameworkElement).RequestedTheme = RequestedTheme;

        ShellViewModel ShellVM = Ioc.Default.GetRequiredService<ShellViewModel>();
        //Save file, close current node since it won't be the right theme.
        if (ShellVM.StoryModel != null && ShellVM.DataSource != null && ShellVM.DataSource.Count > 0)
        {
            await Ioc.Default.GetRequiredService<ShellViewModel>().SaveFile();
            Ioc.Default.GetRequiredService<ShellViewModel>().ShowHomePage();
        }
	}

	/// <summary>
	/// Reference to currently open dialog.
	/// </summary>
    private ContentDialog OpenDialog;

    /// <summary>
    /// This takes a ContentDialog and shows it to the user
    /// It will handle theming, XAMLRoot and showing the dialog.
    /// </summary>
    /// <param name="Dialog">Content dialog to show</param>
    /// <param name="force">Force show content dialog, will close currently open dialog if one
    /// is already open
    /// </param>
    /// <returns>A ContentDialogResult enum value.</returns>
    public async Task<ContentDialogResult> ShowContentDialog(ContentDialog Dialog, bool force=false)
    {
		//force close any other dialog if one is open
		//(if force is enabled)
	    if (force && _IsContentDialogOpen && OpenDialog != null)
	    {
		    OpenDialog.Hide();

			//This will be unset eventually but because we have called
			//Hide() already we can unset this early.
			_IsContentDialogOpen = false;
	    }

	    //Checks a content dialog isn't already open
        if (!_IsContentDialogOpen) 
        {
	        OpenDialog = Dialog;
			//Set XAML root and correct theme.
			OpenDialog.XamlRoot = XamlRoot;
			OpenDialog.RequestedTheme = RequestedTheme;

            _IsContentDialogOpen = true;
            ContentDialogResult Result = await OpenDialog.ShowAsync();
            _IsContentDialogOpen = false;
            OpenDialog = null;
            return Result;
        }
        return ContentDialogResult.None; 
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
    /// Shows a file picker.
    /// </summary>
    /// <returns>A StorageFile object, of the file picked.</returns>
    public async Task<StorageFile> ShowFilePicker(string ButtonText = "Open", string Filter = "*")
    {
	    LogService logger = Ioc.Default.GetRequiredService<LogService>();

		try
		{
			logger.Log(LogLevel.Info, $"Trying to open a file picker with filter:{Filter}");


			FileOpenPicker _filePicker = new()
		    {
			    CommitButtonText = ButtonText,
			    SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
			    FileTypeFilter = { Filter }
		    };

			//Init and spawn file picker
		    WinRT.Interop.InitializeWithWindow.Initialize(_filePicker, MainWindow.GetWindowHandle());
			StorageFile file = await _filePicker.PickSingleFileAsync();

			logger.Log(LogLevel.Info, $"Picked folder {file.Path} attributes:{file.Attributes}");
			return file;
		}
	    catch (Exception e)
	    {
			//See below for possible exception cause.
		    logger.Log(LogLevel.Error, $"File picker error!, ex:{e.Message} {e.StackTrace}");
		    throw;
	    }

    }

	/// <summary>
	/// Spawn a folder picker for the user to select a folder.
	/// </summary>
	/// <param name="ButtonText">Text shown on the confirmation button</param>
	/// <param name="Filter">Filter filetype?</param>
	/// <returns></returns>
    public async Task<StorageFolder> ShowFolderPicker(string ButtonText = "Select folder",
	    string Filter = "*")
    {
		LogService logger = Ioc.Default.GetRequiredService<LogService>();

		try
	    {
		    logger.Log(LogLevel.Info, $"Trying to open a folder picker with filter:{Filter}");
		    // Find a home for the new project
		    FolderPicker folderPicker = new()
		    {
			    CommitButtonText = ButtonText,
			    SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
				FileTypeFilter = { Filter }
		    };

			//Initialize and show picker 
		    WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, 
			    MainWindow.GetWindowHandle());
			StorageFolder folder = await folderPicker.PickSingleFolderAsync();

			//Log it was successful
			logger.Log(LogLevel.Info, $"Picked folder {folder.Path} attributes:{folder.Attributes}");

			return folder;
	    }
	    catch (Exception e)
	    {
			//I'm not really sure how we can get an error with the folder picker
			//But I'm aware WinUI will throw an exception if the app is running
			//as administrator. (See microsoft/WindowsAppSDK #2504)
			logger.Log(LogLevel.Error, $"Folder picker error!, ex:{e.Message} {e.StackTrace}");
		    throw;
	    }

    }

    #region Com stuff for File/Folder pickers
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("EECDBF0E-BAE9-4CB6-A68E-9598E1CB57BB")]
    internal interface IWindowNative
    {
        IntPtr WindowHandle { get; }
    }

    [ComImport]
    [Guid("3E68D4BD-7135-4D10-8018-9FB6D9F33FA1")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IInitializeWithWindow
    {
        void Initialize(IntPtr hwnd);
    }

    [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto, PreserveSig = true, SetLastError = false)]
    public static extern IntPtr GetActiveWindow();
    #endregion

    /// <summary>
    /// Shows an error message to the user that there's an issue
    /// with the app and it needs to be reinstalled
    /// As of the RemoveInstallService merge, this is theoretically 
    /// impossible to occur but it should stay incase something
    /// goes wrong with resource loading.
    /// </summary>
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
        throw new ResourceLoadingException();
    }
}