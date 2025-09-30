using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using StoryCAD.Exceptions;
using StoryCAD.Services.Logging;
using StoryCAD.ViewModels.SubViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI;
using Windows.UI.ViewManagement;
using LogLevel = StoryCAD.Services.Logging.LogLevel;
#if WINDOWS
using Microsoft.UI.Windowing;
using Windows.Graphics;
#endif

namespace StoryCAD.Models;

/// <summary>
/// This class contains window (MainWindow) related items etc.
/// </summary>
public partial class Windowing : ObservableRecipient
{
    private readonly AppState _appState;
    private readonly ILogService _logService;

    public Windowing(AppState appState, ILogService logService)
    {
        _appState = appState;
        _logService = logService;
    }

    // Constructor for backward compatibility - will be removed later
    public Windowing() : this(
        Ioc.Default.GetRequiredService<AppState>(),
        Ioc.Default.GetRequiredService<ILogService>())
    {
    }

    private void OnPropertyChanged(object sender, PropertyChangedEventArgs e) { }

    /// <summary>
    /// A pointer to the App Window (MainWindow) handle
    /// </summary>
    public IntPtr WindowHandle;

    /// Tools that copy data into StoryElements must access and update the viewmodel currently 
    /// active viewmodel at the time the tool is invoked. The viewmodel type is identified
    /// by the navigation service page key.
    public string PageKey;

    // MainWindow is the main window displayed by the app. 
    // Resize events can be tracked via ShellPage.xaml.cs ShellPage_SizeChanged
    public Window MainWindow;

    /// <summary>
    // A defect in early WinUI 3 Win32 code is that ContentDialog
    // controls don't have an established XamlRoot. A workaround
    // is to assign the dialog's XamlRoot to the root of a visible
    // Page. The Shell page's XamlRoot is stored here and accessed wherever needed. 
    /// </summary>
    public XamlRoot XamlRoot;

    /// <summary>
    /// A universal dispatcher to show messages/change UI from 
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
        if (_appState.Headless) { return; }

        string BaseTitle = "StoryCAD ";

        //Developer Build title warning
        if (_appState.DeveloperBuild)
        {
            BaseTitle += "(DEV BUILD) ";
        }

        // TODO: ARCHITECTURAL ISSUE - Windowing (UI layer) should not depend on OutlineViewModel (business logic)
        // This creates a circular dependency: OutlineViewModel -> Windowing -> OutlineViewModel
        // Proper fix (SRP): Move UpdateWindowTitle logic to OutlineViewModel (which knows about file changes)
        // and have it set the title on Windowing. OutlineViewModel already has Windowing reference.
        // Current workaround: Use service locator to break the circular dependency
        AppState appState = Ioc.Default.GetRequiredService<AppState>();
        if (!string.IsNullOrEmpty(appState.CurrentDocument?.FilePath))
        {
            BaseTitle += $"- Currently editing {Path.GetFileNameWithoutExtension(appState.CurrentDocument.FilePath)}";
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

        // TODO: ARCHITECTURAL ISSUE - Same as UpdateWindowTitle above
        // Proper fix (SRP): Move UpdateUIToTheme logic to ShellViewModel (which manages navigation/UI state)
        // ShellViewModel can coordinate the save and navigation when theme changes
        // Current workaround: Use service locator to break the circular dependency
        AppState appState = Ioc.Default.GetRequiredService<AppState>();
        //Save file, close current node since it won't be the right theme.
        if (!string.IsNullOrEmpty(appState.CurrentDocument?.FilePath))
        {
            await Ioc.Default.GetRequiredService<OutlineViewModel>().SaveFile();
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
    /// <remarks>This will return primary if running under headless mode</remarks>
    /// <returns>A ContentDialogResult value</returns>
    public async Task<ContentDialogResult> ShowContentDialog(ContentDialog Dialog, bool force=false)
    {
	    ILogService logger = _logService;

        // Don't show dialog if headless
        AppState state = _appState;
        if (state.Headless) { return ContentDialogResult.Primary; }

        logger.Log(LogLevel.Info, $"Requested to show dialog {Dialog.Title}");

		//force close any other dialog if one is open
		//(if force is enabled)
		if (force && _IsContentDialogOpen && OpenDialog != null)
	    {
		    logger.Log(LogLevel.Info, $"Force closing open content dialog: ({OpenDialog.Title})");
			OpenDialog.Hide();

			//This will be unset eventually but because we have called
			//Hide() already we can unset this early.
			_IsContentDialogOpen = false;
			logger.Log(LogLevel.Info, $"Closed content dialog: ({OpenDialog.Title})");
		}

		//Checks a content dialog isn't already open
		if (!_IsContentDialogOpen) 
        {
			logger.Log(LogLevel.Trace, $"Showing dialog {Dialog.Title}");
			OpenDialog = Dialog;

			//Set XAML root and correct theme.
			OpenDialog.XamlRoot = XamlRoot;
			OpenDialog.RequestedTheme = RequestedTheme;

            OpenDialog.Resources["ContentDialogMaxWidth"] = 1080;
            OpenDialog.Resources["ContentDialogMaxHeight"] = 1080;

            _IsContentDialogOpen = true;
            
			//Show and log result.
            ContentDialogResult Result = await OpenDialog.ShowAsync();
            switch (Result)
            {
	            case ContentDialogResult.None:
		            logger.Log(LogLevel.Info, "User selected no option.");
					break;
	            case ContentDialogResult.Primary:
		            logger.Log(LogLevel.Info, $"User selected primary option {Dialog.PrimaryButtonText}");
					break;
	            case ContentDialogResult.Secondary:
		            logger.Log(LogLevel.Info, $"User selected secondary option {Dialog.SecondaryButtonText}");
					break;
            }
			_IsContentDialogOpen = false;
            OpenDialog = null;
            return Result;
        }
        return ContentDialogResult.None; 
    }

    /// <summary>
    /// Dismisses the current content dialog
    /// </summary>
    public void CloseContentDialog()
    {
        if (_IsContentDialogOpen)
        {
            OpenDialog.Hide();
        }
    }


    /// <summary>
    /// When a second instance is opened, this code will be ran on the main (first) instance
    /// It will bring up the main window.
    /// </summary>
    public void ActivateMainInstance()
    {
        Windowing wnd = Ioc.Default.GetRequiredService<Windowing>();
        wnd.MainWindow.Activate();

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
    public async Task<StorageFile> ShowFilePicker(string buttonText = "Open", string filter = "*")
    {
	    ILogService logger = _logService;

		try
		{
			logger.Log(LogLevel.Info, $"Trying to open a file picker with filter: {filter}");

			FileOpenPicker filePicker = new()
		    {
			    CommitButtonText = buttonText,
			    SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
			    FileTypeFilter = { filter }
		    };

			//Init and spawn file picker
		    WinRT.Interop.InitializeWithWindow.Initialize(filePicker, WindowHandle);
			StorageFile file = await filePicker.PickSingleFileAsync();

			//Null check
			if (file == null)
			{
				logger.Log(LogLevel.Warn, "File picker returned null, this is because the user probably clicked cancel.");
				return null;
			}

			logger.Log(LogLevel.Info, $"Picked folder {file.Path}");

            #if !HAS_UNO
                logger.Log(LogLevel.Info, $"Picked file attributes {file.Attributes}");
            #endif
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
    /// Shows a file save as picker.
    /// </summary>
    /// <returns>A StorageFile object, of the file picked.</returns>
    public async Task<StorageFile> ShowFileSavePicker(string buttonText, string extension)
    {
	    ILogService logger = _logService;

		try
		{
			logger.Log(LogLevel.Info, $"Trying to save a file picker with extension: {extension}");

			FileSavePicker filePicker = new()
		    {
			    CommitButtonText = buttonText,
			    SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                DefaultFileExtension = extension,
            };

            filePicker.FileTypeChoices.Add("File", new List<string>() {extension });

            //Init and spawn file picker
            WinRT.Interop.InitializeWithWindow.Initialize(filePicker, WindowHandle);
			StorageFile file = await filePicker.PickSaveFileAsync();

			//Null check
			if (file == null)
			{
				logger.Log(LogLevel.Warn, "File picker returned null, this is because the user probably clicked cancel.");
				return null;
			}
            
            //Ensure that the file is created (
            //Appears to not happen automatically on MacOS, returning an invalid file object)
            File.Create(file.Path).Close();
            #if HAS_UNO
			logger.Log(LogLevel.Info, $"Picked folder {file.Path}");
            #else
			logger.Log(LogLevel.Info, $"Picked folder {file.Path} attributes:{file.Attributes}"); //Log 
            #endif
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
	/// <param name="buttonText">Text shown on the confirmation button</param>
	/// <param name="filter">Filter filetype?</param>
	/// <returns></returns>
    public async Task<StorageFolder> ShowFolderPicker(string buttonText = "Select folder", string filter = "*")
    {
		ILogService logger = _logService;

		try
	    {
		    logger.Log(LogLevel.Info, $"Trying to open a folder picker with filter:{filter}");
		    // Find a home for the new project
		    FolderPicker folderPicker = new()
		    {
			    CommitButtonText = buttonText,
			    SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
				FileTypeFilter = { filter }
		    };

			//Initialize and show picker 
		    WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, WindowHandle);
			StorageFolder folder = await folderPicker.PickSingleFolderAsync();

			//Null check
			if (folder == null)
			{
				logger.Log(LogLevel.Warn, "File picker returned null, this is because the user probably clicked cancel.");
				return null;
			}

			//Log it was successful
			logger.Log(LogLevel.Info, $"Picked folder {folder.Path}");
            #if !HAS_UNO
                logger.Log(LogLevel.Info, $"Picked folder attributes {folder.Attributes}");
            #endif
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
    /// with the app, and it needs to be reinstalled
    /// As of the RemoveInstallService merge, this is theoretically 
    /// impossible to occur, but it should stay incase something
    /// goes wrong with resource loading.
    /// </summary>
    public async void ShowResourceErrorMessage()
    {
        await new ContentDialog()
        {
            XamlRoot = this.XamlRoot,
            Title = "Error loading resources",
            Content = "An error has occurred, please reinstall or update StoryCAD to continue.",
            CloseButtonText = "Close",
            RequestedTheme = RequestedTheme
        }.ShowAsync();
        throw new ResourceLoadingException();
    }
}
