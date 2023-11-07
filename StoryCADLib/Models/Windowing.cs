using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.AppLifecycle;
using StoryCAD.Services.Logging;
using StoryCAD.ViewModels;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinUIEx;

namespace StoryCAD.Models;

/// <summary>
/// This class contains window (mainwindow) related items ect.
/// </summary>
public class Windowing
{
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
        FileOpenPicker _filePicker = new()
        {
            CommitButtonText = ButtonText,
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
        };
        
        WinRT.Interop.InitializeWithWindow.Initialize(_filePicker, MainWindow.GetWindowHandle());
        //TODO: Use preferences project folder instead of DocumentsLibrary except you can't. Thanks, UWP.
        _filePicker.FileTypeFilter.Add(Filter);

        return await _filePicker.PickSingleFileAsync();
    }

    public async Task<StorageFolder> ShowFolderPicker(string ButtonText = "Select folder", string Filter = "*")
    {
        // Find a home for the new project
        FolderPicker folderPicker = new() { 
            CommitButtonText = ButtonText,
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
        };

        WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, MainWindow.GetWindowHandle());
        folderPicker.FileTypeFilter.Add(Filter);
        return await folderPicker.PickSingleFolderAsync();
    }

    #region Various Com Imports for File/Folder Pickers
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
}