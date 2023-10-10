using System.IO;
using Windows.Storage;
using StoryCAD.Models.Tools;

namespace StoryCAD.Models;

/// <summary>
/// GlobalData provides access to the application data provided by the
/// DAL loader classes, ControlLoader
/// 
/// It also provides access the Preferences instance and other global items.
/// </summary>
public static class GlobalData
{
    // Preferences data
    public static PreferencesModel Preferences;

    //Path to root directory where data is stored
    public static string RootDirectory = Path.Combine(ApplicationData.Current.RoamingFolder.Path, "StoryCAD");
}