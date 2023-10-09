using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using StoryCAD.ViewModels;
using System;
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
        if (Ioc.Default.GetRequiredService<Developer>().DeveloperBuild)
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
}
