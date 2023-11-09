using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.WinUI.Helpers;
using Microsoft.UI.Xaml;
using MySqlX.XDevAPI;
using StoryCAD.DAL;
using StoryCAD.Models.Tools;
using StoryCAD.Services.Json;
using StoryCAD.Services.Logging;
using StoryCAD.ViewModels;
using Windows.ApplicationModel;
using Windows.Devices.Input;
using Windows.Storage;

namespace StoryCAD.Models;

/// <summary>
/// This class holds developer tools
/// and app data.
/// </summary>
public class AppState
{
    public AppState() { }

    /// <summary>
    /// This is the path where all app files are stored
    /// </summary>
    public string RootDirectory
    {
        get
        {
            if (Assembly.GetEntryAssembly().Location.ToString().Contains("StoryCADTests.dll") || Assembly.GetEntryAssembly().Location.ToString().Contains("testhost.dll"))
            {
                return System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StoryCADTests");
            }
            else 
            {
                return System.IO.Path.Combine(ApplicationData.Current.RoamingFolder.Path, "StoryCAD");
            }
        }
    }

    /// <summary>
    /// User preferences
    /// </summary>
    public PreferencesModel Preferences;

    /// <summary>
    /// This variable will return true if any of following are true:
    ///  - The Build revision is NOT 0.
    ///  - A debugger i.e VS2022 is attached.
    ///  - .ENV is missing.
    ///  
    /// Usually it's all or none of the above.
    /// </summary>
    public bool DeveloperBuild
    {
        get
        {
            if (Debugger.IsAttached || !EnvPresent || Package.Current.Id.Version.Revision != 0)
            {
                return true;
            }
            else { return false; }
        }
    }

    /// <summary>
    /// This is a debug timer that counts the ammount of time from
    /// the app being opened to Shell being properly initalised.
    /// </summary>
    public Stopwatch StartUpTimer = Stopwatch.StartNew();

    /// <summary>
    /// Is .env present?
    /// </summary>
    public bool EnvPresent = false;

    /// <summary>
    /// The current (running) version of StoryCAD
    /// Returns a simple 4 number tuple on release versions i.e 2.12.0.0
    /// Returns a 3 number tuple and build time on 
    /// </summary>
    public string Version
    {
        get
        {
            string _packageVersion = $"{ Package.Current.Id.Version.Major }.{ Package.Current.Id.Version.Minor}.{ Package.Current.Id.Version.Build}";
            if (Package.Current.Id.Version.Revision == 65535)
            {
                string StoryCADManifestVersion = Assembly.GetEntryAssembly()
                    .GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion
                    .Split("build")[1];
                return $"Version: {_packageVersion} Built on: {StoryCADManifestVersion}";
            }
            else
            {
                return $"Version: {_packageVersion}";
            }
        }
    }

    /// <summary>
    /// Returns true if the app has loaded with a version change.
    /// If this is true a changelog will show, install service will
    /// run and the server will update the version.
    /// </summary>    
    public bool LoadedWithVersionChange = false;

    public string SystemInfo
    {
        get {

            try
            {
                string AppArch = IntPtr.Size switch
                {
                    4 => "32 bit",
                    8 => "64 bit",
                    _ => "Unknown"
                };

                string WinVer;
                try
                {
                    WinVer = Environment.OSVersion.Version.Build >= 22000 ? "11" : "10";
                }
                catch { WinVer = "?"; }


                return $"""
                     ===== SYSTEM INFO =====
                     CPU ARCH - {RuntimeInformation.ProcessArchitecture}  
                     OS  ARCH - {RuntimeInformation.OSArchitecture}  
                     App ARCH - {AppArch}
                     .NET Ver - {RuntimeInformation.OSArchitecture}
                     Startup  - {StartUpTimer.ElapsedMilliseconds} ms
                     Elmah Status - {Ioc.Default.GetRequiredService<LogService>().ElmahLogging}
                     Windows {WinVer} Build - {Environment.OSVersion.Version.Build}
                     Debugger Attached - {Debugger.IsAttached}
                     Touchscreen - {PointerDevice.GetPointerDevices().Any(p => p.PointerDeviceType == PointerDeviceType.Touch)}
                     ProcessID - {Environment.ProcessId}
                     Core Count - {Environment.ProcessorCount}

                     === User Prefs ===
                     Name - {Preferences.Name}
                     Email - {Preferences.Email}
                     Elmah Consent - {Preferences.ErrorCollectionConsent}
                     Theme - {Preferences.PrimaryColor.Color.ToHex()}
                     Accent Color - {Ioc.Default.GetRequiredService<Windowing>().AccentColor} 
                     Last Version Prefs logged - {Preferences.Version}
                     Search Engine - {Preferences.PreferredSearchEngine} 
                     AutoSave - {Preferences.AutoSave}
                     AutoSave Interval - {Preferences.AutoSaveInterval} 
                     Backup - {Preferences.TimedBackup}
                     Backup Interval - {Preferences.TimedBackupInterval}
                     Backup on open - {Preferences.BackupOnOpen} 
                     Project Dir - {Preferences.ProjectDirectory}
                     Backup Dir - {Preferences.BackupDirectory} 
                     RecordPreferencesStatus - {Preferences.RecordPreferencesStatus}

                     ===CAD Info===
                     StoryCAD Version - {Version}
                     Developer - {DeveloperBuild}
                     Env Present - {EnvPresent}
                     Doppler Connection - {Doppler.DopplerConnection}
                     Loaded with version change - {LoadedWithVersionChange}
                     Invoked through STBX File - {Ioc.Default.GetRequiredService<ShellViewModel>().FilePathToLaunch != ""}
                     """;
            }
            catch (Exception e) { return $"Error getting System Info, {e.Message}"; }       
        }

    }

    public async Task LoadPreferences() {
        LogService Logger = Ioc.Default.GetService<LogService>();
        try
        {
            Logger.Log(LogLevel.Info, "Loading Preferences");
            PreferencesModel model = new();
            PreferencesIo loader = new(model, RootDirectory);
            await loader.ReadPreferences();

            Preferences = model;
        }
        catch (Exception ex)
        {
            Logger.LogException(LogLevel.Error, ex, "Error loading Preferences");
            Application.Current.Exit();  // Win32; 
        }
    }

}