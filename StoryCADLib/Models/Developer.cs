using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.WinUI.Helpers;
using StoryCAD.ViewModels;
using Windows.ApplicationModel;
using Windows.Devices.Input;

namespace StoryCAD.Models;

/// <summary>
/// This class holde developer info/tooling.f
/// </summary>
public class Developer
{

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
                     Elmah Status - {GlobalData.ElmahLogging}
                     Windows {WinVer} Build - {Environment.OSVersion.Version.Build}
                     Debugger Attached - {Debugger.IsAttached}
                     Touchscreen - {PointerDevice.GetPointerDevices().Any(p => p.PointerDeviceType == PointerDeviceType.Touch)}
                     ProcessID - {Environment.ProcessId}
                     Core Count - {Environment.ProcessorCount}

                     === User Prefs ===
                     Name - {GlobalData.Preferences.Name}
                     Email - {GlobalData.Preferences.Email}
                     Elmah Consent - {GlobalData.Preferences.ErrorCollectionConsent}
                     Theme - {GlobalData.Preferences.PrimaryColor.Color.ToHex()}
                     Accent Color - {GlobalData.Preferences.AccentColor} 
                     Last Version Prefs logged - {GlobalData.Preferences.Version}
                     Search Engine - {GlobalData.Preferences.PreferredSearchEngine} 
                     AutoSave - {GlobalData.Preferences.AutoSave}
                     AutoSave Interval - {GlobalData.Preferences.AutoSaveInterval} 
                     Backup - {GlobalData.Preferences.TimedBackup}
                     Backup Interval - {GlobalData.Preferences.TimedBackupInterval}
                     Backup on open - {GlobalData.Preferences.BackupOnOpen} 
                     Project Dir - {GlobalData.Preferences.ProjectDirectory}
                     Backup Dir - {GlobalData.Preferences.BackupDirectory} 
                     RecordPreferencesStatus - {GlobalData.Preferences.RecordPreferencesStatus}

                     ===CAD Info===
                     StoryCAD Version - {GlobalData.Version}
                     Developer - {DeveloperBuild}
                     Env Present - {EnvPresent}
                     """;
            }
            catch (Exception e) { return $"Error getting System Info, {e.Message}"; }       
         }
    }
}