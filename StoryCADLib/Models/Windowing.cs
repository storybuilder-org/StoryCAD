using System.Resources;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Dispatching;
using System.Runtime.InteropServices;
using StoryCADLib.Services.Messages;
using Windows.Storage.Pickers;
using Windows.UI;
using Windows.UI.ViewManagement;
using LogLevel = StoryCADLib.Services.Logging.LogLevel;
#if WINDOWS
using Microsoft.UI.Windowing;
using Windows.Graphics;
#endif

namespace StoryCADLib.Models;

/// <summary>
/// This class contains window (MainWindow) related items etc.
/// </summary>
public class Windowing : ObservableRecipient
{
    private readonly AppState _appState;
    private readonly ILogService _logService;

    public Windowing(AppState appState, ILogService logService)
    {
        _appState = appState;
        _logService = logService;
    }

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
    /// A defect in early WinUI 3 Win32 code is that ContentDialog
    /// controls don't have an established XamlRoot. A workaround
    /// is to assign the dialog's XamlRoot to the root of a visible
    /// Page. The Shell page's XamlRoot is stored here and accessed wherever needed.
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
    private bool _isContentDialogOpen;

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
    ///     This is a color that should in most cases
    ///     contrast the users accent color
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

        if (!string.IsNullOrEmpty(_appState.CurrentDocument?.FilePath))
        {
            BaseTitle += $"- Currently editing {Path.GetFileNameWithoutExtension(_appState.CurrentDocument.FilePath)}";
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

        // Notify subscribers (ShellViewModel) to save and navigate to safe state
        if (!string.IsNullOrEmpty(_appState.CurrentDocument?.FilePath))
        {
            WeakReferenceMessenger.Default.Send(new ThemeChangedMessage());
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
		if (force && _isContentDialogOpen && OpenDialog != null)
	    {
		    logger.Log(LogLevel.Info, $"Force closing open content dialog: ({OpenDialog.Title})");
			OpenDialog.Hide();

			//This will be unset eventually but because we have called
			//Hide() already we can unset this early.
			_isContentDialogOpen = false;
			logger.Log(LogLevel.Info, $"Closed content dialog: ({OpenDialog.Title})");
		}

		//Checks a content dialog isn't already open
		if (!_isContentDialogOpen)
        {
			logger.Log(LogLevel.Trace, $"Showing dialog {Dialog.Title}");
			OpenDialog = Dialog;

			//Set XAML root and correct theme.
			OpenDialog.XamlRoot = XamlRoot;
			OpenDialog.RequestedTheme = RequestedTheme;

            OpenDialog.Resources["ContentDialogMaxWidth"] = 1080;
            OpenDialog.Resources["ContentDialogMaxHeight"] = 1080;

            _isContentDialogOpen = true;

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
			_isContentDialogOpen = false;
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
        if (_isContentDialogOpen)
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
        MainWindow.Activate();

        GlobalDispatcher.TryEnqueue(() =>
        {
            WeakReferenceMessenger.Default.Send(new ActivateInstanceMessage());
        });
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

			logger.Log(LogLevel.Info, $"Picked folder {file.Path}");
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

    /// <summary>
    /// Sets the window size in physical pixels (consistent across all platforms regardless of DPI)
    /// </summary>
    /// <param name="window">The Window to resize</param>
    /// <param name="desiredWidthPx">Desired width in physical pixels</param>
    /// <param name="desiredHeightPx">Desired height in physical pixels</param>
    public void SetWindowSize(Window window, double desiredWidthPx, double desiredHeightPx)
    {
        ILogService logger = _logService;

        if (window == null)
        {
            logger.Log(LogLevel.Warn, "SetWindowSize called with null window");
            return;
        }

        // Parameters are physical pixels - pass directly to AppWindow.Resize
        // This ensures consistent window size across all platforms regardless of DPI scaling
        int targetWidth = (int)desiredWidthPx;
        int targetHeight = (int)desiredHeightPx;

        // Clamp to available screen space to prevent window from exceeding display bounds
        var (maxWidth, maxHeight) = GetMaxWindowSize();
        if (maxWidth > 0 && maxHeight > 0)
        {
            const int margin = 50; // Leave room for window chrome/shadows
            int clampedWidth = Math.Min(targetWidth, maxWidth - margin);
            int clampedHeight = Math.Min(targetHeight, maxHeight - margin);

            if (clampedWidth != targetWidth || clampedHeight != targetHeight)
            {
                logger.Log(LogLevel.Warn,
                    $"Requested size {targetWidth}x{targetHeight} exceeds screen {maxWidth}x{maxHeight}, " +
                    $"clamping to {clampedWidth}x{clampedHeight}");
                targetWidth = clampedWidth;
                targetHeight = clampedHeight;
            }
        }

        logger.Log(LogLevel.Info,
            $"Setting window size: {targetWidth}x{targetHeight} physical pixels");

        window.AppWindow.Resize(new Windows.Graphics.SizeInt32
        {
            Width = targetWidth,
            Height = targetHeight
        });
    }

    /// <summary>
    /// Gets the maximum available window size based on the current display's work area.
    /// Returns (0,0) if unable to determine screen dimensions.
    /// </summary>
    /// <returns>Tuple of (maxWidth, maxHeight) in physical pixels</returns>
    private (int width, int height) GetMaxWindowSize()
    {
        ILogService logger = _logService;

#if WINDOWS && !HAS_UNO
        // Use SystemParametersInfo to get work area (excludes taskbar)
        if (SystemParametersInfo(SPI_GETWORKAREA, 0, out RECT work, 0))
        {
            int w = work.Right - work.Left;
            int h = work.Bottom - work.Top;
            logger.Log(LogLevel.Trace, $"GetMaxWindowSize: work area = {w}x{h}");
            return (w, h);
        }
        // Fallback to full screen metrics
        int screenW = GetSystemMetrics(SM_CXSCREEN);
        int screenH = GetSystemMetrics(SM_CYSCREEN);
        logger.Log(LogLevel.Trace, $"GetMaxWindowSize: screen metrics fallback = {screenW}x{screenH}");
        return (screenW, screenH);
#elif HAS_UNO
        try
        {
            var di = Windows.Graphics.Display.DisplayInformation.GetForCurrentView();
            int w = (int)di.ScreenWidthInRawPixels;
            int h = (int)di.ScreenHeightInRawPixels;
            logger.Log(LogLevel.Trace, $"GetMaxWindowSize (Uno): screen = {w}x{h}");
            return (w, h);
        }
        catch (Exception ex)
        {
            logger.Log(LogLevel.Warn, $"GetMaxWindowSize: failed to get display info - {ex.Message}");
            return (0, 0); // No clamping if we can't get screen size
        }
#else
        return (0, 0);
#endif
    }

    private double TryGetScaleFactor(Window window)
    {
        ILogService logger = _logService;

#if WINDOWS && !HAS_UNO
        // WinAppSDK (net10.0-windows10.0.22621) - use P/Invoke
        var dpi = GetDpiForWindow(new IntPtr((long)window.AppWindow.Id.Value));
        double scaleFactor = dpi / 96.0;
        logger.Log(LogLevel.Trace, $"Scale factor from WinAppSDK P/Invoke: {scaleFactor:F2} (DPI: {dpi})");
        return scaleFactor;
#elif HAS_UNO
        // Uno Skia (net10.0-desktop on Windows/macOS) - try DisplayInformation
        try
        {
            var displayInfo = Windows.Graphics.Display.DisplayInformation.GetForCurrentView();
            double scaleFactor = displayInfo.RawPixelsPerViewPixel;
            logger.Log(LogLevel.Trace, $"Scale factor from DisplayInformation: {scaleFactor:F2}");
            return scaleFactor;
        }
        catch (Exception ex)
        {
            logger.Log(LogLevel.Warn, $"Failed to get DisplayInformation: {ex.Message}");

            // Fallback to checking environment variable
            var envScale = Environment.GetEnvironmentVariable("UNO_DISPLAY_SCALE_OVERRIDE");
            if (double.TryParse(envScale, out double scale))
            {
                logger.Log(LogLevel.Info, $"Using UNO_DISPLAY_SCALE_OVERRIDE: {scale:F2}");
                return scale;
            }

            // Final fallback
            logger.Log(LogLevel.Info, "Using default scale factor: 1.0");
            return 1.0;
        }
#endif
    }

#if WINDOWS && !HAS_UNO
    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hwnd);

    // P/Invoke declarations for CenterOnScreen
    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [DllImport("user32.dll")]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, out RECT pvParam, uint fWinIni);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    // Structures for Win32 API
    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    // Constants for Win32 API
    private const uint MONITOR_DEFAULTTONEAREST = 2;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_SHOWWINDOW = 0x0040;
    private const uint SPI_GETWORKAREA = 0x0030;
    private const int SM_CXSCREEN = 0;
    private const int SM_CYSCREEN = 1;
#endif

    // Helper methods for CenterOnScreen
    private static bool TryWmhCall(Window window, string methodName)
    {
#if HAS_UNO
        try
        {
            // Try to use Uno's WindowManagerHelper if available
            var wmhType = Type.GetType("Uno.UI.Xaml.WindowManagerHelper, Uno.UI");
            if (wmhType != null)
            {
                var instanceProp = wmhType.GetProperty("Instance");
                if (instanceProp != null)
                {
                    var helper = instanceProp.GetValue(null);
                    if (helper != null)
                    {
                        var method = helper.GetType().GetMethod(methodName);
                        if (method != null)
                        {
                            method.Invoke(helper, new object[] { window });
                            return true;
                        }
                    }
                }
            }
        }
        catch { }
#endif
        return false;
    }

    private static bool IsValidWin32Handle(IntPtr handle)
    {
        return handle != IntPtr.Zero && handle != new IntPtr(-1);
    }

    private static Microsoft.UI.Windowing.AppWindow GetAppWindow(Window window)
    {
        return window?.AppWindow;
    }

    public void CenterOnScreen(Window window)
    {
        ILogService logger = _logService;

        if (window == null)
        {
            logger.Log(LogLevel.Warn, "CenterOnScreen called with null window");
            return;
        }

        logger.Log(LogLevel.Info, "Attempting to center window on screen");

        // 1) Uno helper (cross-platform) if present
        if (TryWmhCall(window, "Center"))
        {
            logger.Log(LogLevel.Info, "Window centered using Uno WindowManagerHelper");
            return;
        }

#if WINDOWS && !HAS_UNO
        // 2) WinAppSDK: center using monitor work area via Win32 P/Invoke
        if (IsValidWin32Handle(WindowHandle))
        {
            try
            {
                var hMon = MonitorFromWindow(WindowHandle, MONITOR_DEFAULTTONEAREST);
                MONITORINFO mi = new() { cbSize = Marshal.SizeOf<MONITORINFO>() };
                if (hMon != IntPtr.Zero && GetMonitorInfo(hMon, ref mi) && GetWindowRect(WindowHandle, out var rect))
                {
                    int winW = rect.Right - rect.Left;
                    int winH = rect.Bottom - rect.Top;
                    int workW = mi.rcWork.Right - mi.rcWork.Left;
                    int workH = mi.rcWork.Bottom - mi.rcWork.Top;

                    int x = mi.rcWork.Left + (workW - winW) / 2;
                    int y = mi.rcWork.Top + (workH - winH) / 2;

                    logger.Log(LogLevel.Trace, $"Centering window: size={winW}x{winH}, work area={workW}x{workH}, position={x},{y}");

                    SetWindowPos(WindowHandle, IntPtr.Zero, x, y, 0, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_SHOWWINDOW);
                    logger.Log(LogLevel.Info, "Window centered using Win32 API");
                    return;
                }
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Warn, $"Win32 centering failed: {ex.Message}");
            }
        }

        // 3) WinAppSDK fallback: compute work area via SPI, move via AppWindow
        try
        {
            RECT work;
            if (!SystemParametersInfo(SPI_GETWORKAREA, 0, out work, 0))
            {
                work = new RECT
                {
                    Left = 0,
                    Top = 0,
                    Right = GetSystemMetrics(SM_CXSCREEN),
                    Bottom = GetSystemMetrics(SM_CYSCREEN)
                };
                logger.Log(LogLevel.Trace, "Using screen dimensions as fallback work area");
            }

            var appWin = GetAppWindow(window);
            if (appWin != null)
            {
                var size = appWin.Size;  // Physical pixels (SizeInt32)

                // All values are in physical pixels - no conversion needed
                int winWidth = size.Width;   // Physical pixels
                int winHeight = size.Height; // Physical pixels

                int workW = work.Right - work.Left;  // Physical pixels
                int workH = work.Bottom - work.Top;  // Physical pixels

                int newX = work.Left + (workW - winWidth) / 2;
                int newY = work.Top + (workH - winHeight) / 2;

                // AppWindow.Move expects physical pixels
                appWin.Move(new Windows.Graphics.PointInt32 { X = newX, Y = newY });
                logger.Log(LogLevel.Info,
                    $"Window centered using AppWindow: size={winWidth}x{winHeight}px work={workW}x{workH}px position={newX},{newY}px");
                return;
            }
        }
        catch (Exception ex)
        {
            logger.Log(LogLevel.Warn, $"AppWindow centering failed: {ex.Message}");
        }
#endif

#if HAS_UNO
        try
        {
            var appWin = GetAppWindow(window);
            if (appWin != null)
            {
                // Get screen size in physical pixels (no DisplayArea API on UNO)
                var di = Windows.Graphics.Display.DisplayInformation.GetForCurrentView();
                var scale = di.RawPixelsPerViewPixel; // e.g., 1.5 for 150%
                int screenWpx = (int)di.ScreenWidthInRawPixels;   // Physical pixels
                int screenHpx = (int)di.ScreenHeightInRawPixels;  // Physical pixels

                // AppWindow.Size is in physical pixels on Uno/Skia (same as WinAppSDK)
                int w = appWin.Size.Width;   // Physical pixels
                int h = appWin.Size.Height;  // Physical pixels

                // Center in physical pixels relative to (0,0)
                int x = Math.Max(0, (screenWpx - w) / 2);
                int y = Math.Max(0, (screenHpx - h) / 2);

                appWin.Move(new Windows.Graphics.PointInt32 { X = x, Y = y });

                logger.Log(LogLevel.Info,
                    $"Window centered (Uno): pos={x},{y}px size={w}×{h}px screen={screenWpx}×{screenHpx}px (scale={scale:F2})");
                return;
            }
        }
        catch (Exception ex)
        {
            logger.Log(LogLevel.Warn, $"Uno centering fallback failed: {ex.Message}");
        }
#endif



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
    /// with the app, and it needs to be reinstalled.
    /// </summary>
    public async void ShowResourceErrorMessage()
    {
        await new ContentDialog
        {
            XamlRoot = this.XamlRoot,
            Title = "Error loading resources",
            Content = "An error has occurred, please reinstall StoryCAD, your outlines will not be affected.",
            CloseButtonText = "Close",
            RequestedTheme = RequestedTheme
        }.ShowAsync();
        throw new MissingManifestResourceException();
    }
}
