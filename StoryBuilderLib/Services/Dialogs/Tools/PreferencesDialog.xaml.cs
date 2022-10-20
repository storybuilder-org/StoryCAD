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

namespace StoryBuilder.Services.Dialogs.Tools;

public sealed partial class PreferencesDialog
{
    public PreferencesViewModel PreferencesVm => Ioc.Default.GetService<PreferencesViewModel>();
    public PreferencesDialog()
    {
        InitializeComponent();
        DataContext = PreferencesVm;
        Version.Text = "StoryBuilder Version: " + Package.Current.Id.Version.Major + "." + Package.Current.Id.Version.Minor + "." + Package.Current.Id.Version.Build + "." + Package.Current.Id.Version.Revision;
        SetChangelog();

        //TODO: Put this in a VM and make this data get logged at start up with some more system info.
        if (Debugger.IsAttached)
        {
            CPUArchitecture.Text = "CPU ARCH: " + RuntimeInformation.ProcessArchitecture;
            OSArchitecture.Text = "OS ARCH: " + RuntimeInformation.OSArchitecture;
            OSInfo.Text = "OS Info: Windows Build " + Environment.OSVersion.VersionString.Replace("Microsoft Windows NT 10.0.","").Replace(".0","");
            if (IntPtr.Size == 4) { AppArchitecture.Text = "We are running as a 32 bit process."; }
            else if (IntPtr.Size == 8) { AppArchitecture.Text = "We are running as a 64 bit process."; }
            else { AppArchitecture.Text = $"UNKNOWN ARCHITECTURE!\nIntPtr was {IntPtr.Size}, expected 4 or 8."; }
        }
        else
        {
            Dev.Header = "";
            Dev.Content = null;
            Dev.IsEnabled = false;
            Dev.Opacity = 0;
            PivotView.Items.Remove(Dev);
        }
    }

    private async void SetChangelog()
    {
        try
        {
            GitHubClient _Client = new(new ProductHeaderValue("Stb2ChangelogGrabber"));
            Changelog.Text = (await _Client.Repository.Release.Get("storybuilder-org", "StoryBuilder-2", GlobalData.Version.Replace("Version: ", ""))).Body;
        }
        catch
        {
            Changelog.Text = "Failed to get changelog for this version, this because either:\n " +
                             "- You are running an automated build version" +
                             "\n- There is an issue connecting to Github\n\n" +
                             $" For reference your version is {GlobalData.Version}";
        }

    }

    private void OpenPath(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = Path.Combine(GlobalData.RootDirectory, "Logs"),
            UseShellExecute = true,
            Verb = "open"
        });
    }

    private void OpenDiscordUrl(object sender, RoutedEventArgs e)
    {
        Process _Browser = new();
        _Browser.StartInfo.FileName = @"https://discord.gg/wfZxU4bx6n";
        _Browser.StartInfo.UseShellExecute = true;
        _Browser.Start();
    }

    private async void SetBackupPath(object sender, RoutedEventArgs e)
    {
        FolderPicker _FolderPicker = new();
        if (Window.Current == null)
        {
            //IntPtr hwnd = GetActiveWindow();
            IntPtr _Hwnd = GlobalData.WindowHandle;
            IInitializeWithWindow _InitializeWithWindow = _FolderPicker.As<IInitializeWithWindow>();
            _InitializeWithWindow.Initialize(_Hwnd);
        }

        _FolderPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        _FolderPicker.FileTypeFilter.Add("*");
        StorageFolder _Folder = await _FolderPicker.PickSingleFolderAsync();
        if (_Folder != null)
        {
            Ioc.Default.GetRequiredService<PreferencesViewModel>().BackupDir = _Folder.Path;
        }
    }
    private async void SetProjectPath(object sender, RoutedEventArgs e)
    {
        FolderPicker _FolderPicker = new();
        if (Window.Current == null)
        {
            //IntPtr hwnd = GetActiveWindow();
            IntPtr _Hwnd = GlobalData.WindowHandle;
            IInitializeWithWindow _InitializeWithWindow = _FolderPicker.As<IInitializeWithWindow>();
            _InitializeWithWindow.Initialize(_Hwnd);
        }

        _FolderPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        _FolderPicker.FileTypeFilter.Add("*");
        StorageFolder _Folder = await _FolderPicker.PickSingleFolderAsync();
        if (_Folder != null)
        {
            Ioc.Default.GetRequiredService<PreferencesViewModel>().ProjectDir = _Folder.Path;
            ProjDirBox.Text = _Folder.Path; //Updates the box visually (fixes visual glitch.)
        }
    }

    [ComImport]
    [Guid("3E68D4BD-7135-4D10-8018-9FB6D9F33FA1")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IInitializeWithWindow
    {
        void Initialize(IntPtr hwnd);
    }

    private void ThrowException(object sender, RoutedEventArgs e)
    {
        throw new NotImplementedException();
    }

    private void SetInitToFalse(object sender, RoutedEventArgs e)
    {
        PreferencesVm.init = false;
    }

    private async void AttachElmah(object sender, RoutedEventArgs e)
    {
        await Ioc.Default.GetRequiredService<LogService>().AddElmahTarget();
    }

    private async void Reinstall(object sender, RoutedEventArgs e)
    {
        await Ioc.Default.GetRequiredService<InstallationService>().InstallFiles();
    }
}