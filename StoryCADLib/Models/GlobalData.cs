using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Windows.Storage;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using StoryCAD.Models.Tools;
using WinUIEx;

namespace StoryCAD.Models;

/// <summary>
/// GlobalData provides access to the application data provided by the
/// DAL loader classes, ControlLoader
/// 
/// It also provides access the Preferences instance and other global items.
/// </summary>
public static class GlobalData
{
    /// A pointer to the App Window (MainWindow) handle
    public static IntPtr WindowHandle;

    /// The current (running) version of StoryCAD
    public static string Version;

    /// Tools that copy data into StoryElements must access and update the viewmodel currently 
    /// active viewmodel at the time the tool is invoked. The viewmodel type is identified
    /// by the navigation service page key.
    public static string PageKey;
    /// <summary>
    /// Some controls and all tools have their own specific data model. The following 
    /// data types hold data for user controls and tool forms.
    /// </summary>


    // Connection status
    public static bool DopplerConnection;
    public static bool ElmahLogging;

    // Preferences data
    public static PreferencesModel Preferences;

    //Path to root directory where data is stored
    public static string RootDirectory = Path.Combine(ApplicationData.Current.RoamingFolder.Path, "StoryCAD");

    // MainWindow is the main window displayed by the app. It's an instance of
    // WinUIEx's WindowEx, which is an extension of Microsoft.Xaml.UI.Window 
    // and which hosts a Frame holding 
    public static WindowEx MainWindow;

    // A defect in early WinUI 3 Win32 code is that ContentDialog controls don't have an
    // established XamlRoot. A workaround is to assign the dialog's XamlRoot to 
    // the root of a visible Page.
    // The Shell page's XamlRoot is stored here and accessed wherever needed. 
    public static XamlRoot XamlRoot;

    // Set to true if the app has loaded with a version change. (Changelog)
    public static bool LoadedWithVersionChange = false;

    //If this is not "" then the app was invoked via a .STBX file and once initalised, should load it.
    public static string FilePathToLaunch;

    public static DispatcherQueue GlobalDispatcher = null;

    //This counts the amount of time it takes from the app being started to the Shell being opened.
    public static Stopwatch StartUpTimer;

    //This will be set to true if any of the following are met
    //The revision number in build is not 0
    //A debugger is attached
    //.ENV is missing.
    public static bool DeveloperBuild;
}