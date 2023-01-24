using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.Storage.Pickers;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Octokit;
using StoryBuilder.Models;
using StoryBuilder.Services.Installation;
using StoryBuilder.Services.Logging;
using StoryBuilder.ViewModels.Tools;
using WinRT;
using Page = Microsoft.UI.Xaml.Controls.Page;

namespace StoryBuilder.Services.Dialogs.Tools;

public sealed partial class PreferencesDialog : Page
{
    public PreferencesViewModel PreferencesVm => Ioc.Default.GetService<PreferencesViewModel>();
    public InstallationService InstallVM => Ioc.Default.GetRequiredService<InstallationService>();
    public LogService Logger => Ioc.Default.GetRequiredService<LogService>();
    public PreferencesDialog()
    {
        InitializeComponent();
        DataContext = PreferencesVm;
        Version.Text = GlobalData.Version;
        SetChangelog();

        //TODO: Put this in a VM and make this data get logged at start up with some more system info.
        if (Debugger.IsAttached)
        {
            //Get Device Info such as architecture and .NET Version
            CPUArchitecture.Text = "CPU ARCH: " + RuntimeInformation.ProcessArchitecture;
            OSArchitecture.Text = "OS ARCH: " + RuntimeInformation.OSArchitecture;
            NetVer.Text = ".NET Version: " + RuntimeInformation.FrameworkDescription;

            try
            {
                //Get Windows Build and Version
                OSInfo.Text = "Windows Build: " + Environment.OSVersion.Version.Build;
                if (Convert.ToInt32(Environment.OSVersion.Version.Build) >= 22000)
                {
                    OSInfo.Text += " (Windows 11)";
                }
                else { OSInfo.Text += " (Windows 10)"; }
            }
            catch { OSInfo.Text = "OS Info:Error";  }


            //Detect if 32-bit or 64-bit process (I'm not sure if it's possible to )
            if (IntPtr.Size == 4) { AppArchitecture.Text = "We are running as a 32 bit process."; }
            else if (IntPtr.Size == 8) { AppArchitecture.Text = "We are running as a 64 bit process."; }
            else { AppArchitecture.Text = $"UNKNOWN ARCHITECTURE!\nIntPtr was {IntPtr.Size}, expected 4 or 8."; }
        }
        else //Remove this because no debugger is attached.
        {
            PivotView.Items.Remove(Dev);
        }
    }

    private async void SetChangelog()
    {
        try
        {
            GitHubClient client = new(new ProductHeaderValue("STB"));
            Changelog.Text = (await client.Repository.Release.Get("storybuilder-org", "StoryBuilder-2", GlobalData.Version.Replace("Version: ", ""))).Body;
        }
        catch
        {
            //TODO: Use .NET7 Raw String Literal?
            Changelog.Text = "Failed to get changelog for this version, this because either:" +
                             "\n - You are running an auto-build version" +
                             "\n- There is an issue connecting to Github";
        }

    }

    /// <summary>
    /// Opens the Log Folder
    /// </summary>
    private void OpenLogFolder(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo { FileName = Path.Combine(GlobalData.RootDirectory, "Logs"), UseShellExecute = true, Verb = "open" });
    }

    /// <summary>
    /// This opens the user's browser to join the StoryBuilder Discord Server
    /// </summary>
    private void OpenDiscordUrl(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo { FileName = @"https://discord.gg/wfZxU4bx6n", UseShellExecute = true, Verb = "open" });
    }

    private async void SetBackupPath(object sender, RoutedEventArgs e)
    {
        FolderPicker _folderPicker = new();
        if (Window.Current == null)
        {
            //TODO: Can this be put into a helper class or removed at some point with WinAppSDK updates?
            IntPtr hwnd = GlobalData.WindowHandle;
            IInitializeWithWindow initializeWithWindow = _folderPicker.As<IInitializeWithWindow>();
            initializeWithWindow.Initialize(hwnd);
        }

        _folderPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        _folderPicker.FileTypeFilter.Add("*");
        StorageFolder _folder = await _folderPicker.PickSingleFolderAsync();
        if (_folder != null)
        {
            Ioc.Default.GetRequiredService<PreferencesViewModel>().BackupDir = _folder.Path;
        }
    }
    private async void SetProjectPath(object sender, RoutedEventArgs e)
    {
        FolderPicker _folderPicker = new();
        if (Window.Current == null)
        {
            //IntPtr hwnd = GetActiveWindow();
            IntPtr _hwnd = GlobalData.WindowHandle;
            IInitializeWithWindow _initializeWithWindow = _folderPicker.As<IInitializeWithWindow>();
            _initializeWithWindow.Initialize(_hwnd);
        }

        _folderPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        _folderPicker.FileTypeFilter.Add("*");
        StorageFolder folder = await _folderPicker.PickSingleFolderAsync();
        if (folder != null)
        {
            Ioc.Default.GetRequiredService<PreferencesViewModel>().ProjectDir = folder.Path;
            ProjDirBox.Text = folder.Path; //Updates the box visually (fixes visual glitch.)
        }
    }

    [ComImport]
    [Guid("3E68D4BD-7135-4D10-8018-9FB6D9F33FA1")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IInitializeWithWindow { void Initialize(IntPtr hwnd); }

    /// <summary>
    /// This function throws an error as it is used to test errors.
    /// </summary>
    private void ThrowException(object sender, RoutedEventArgs e)
    {
        throw new NotImplementedException("This is a test exception thrown by the developer Menu and should be ignored.");
    }

    /// <summary>
    /// This sets init to false, meaning the next time
    /// StoryBuilder is opened the PreferencesInitialization
    /// page will be shown.
    /// </summary>
    private void SetInitToFalse(object sender, RoutedEventArgs e)
    {
        PreferencesVm.Init = false;
    }
}