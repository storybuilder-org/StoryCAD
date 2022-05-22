using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Windows.Storage;
using Windows.Storage.Pickers;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using StoryBuilder.Models;
using StoryBuilder.Services.Logging;
using StoryBuilder.ViewModels.Tools;
using WinRT;

namespace StoryBuilder.Services.Dialogs.Tools;

public sealed partial class PreferencesDialog : Page
{
    public PreferencesViewModel PreferencesVm => Ioc.Default.GetService<PreferencesViewModel>();
    public PreferencesDialog()
    {
        InitializeComponent();
        DataContext = PreferencesVm;
        Version.Text = "StoryBuilder Version: " + Windows.ApplicationModel.Package.Current.Id.Version.Major + "." + Windows.ApplicationModel.Package.Current.Id.Version.Minor + "." + Windows.ApplicationModel.Package.Current.Id.Version.Build + "." + Windows.ApplicationModel.Package.Current.Id.Version.Revision;

        if (Debugger.IsAttached || GlobalData.Preferences.Name == "ShowMeTheDevTab")
        {
            Dev.IsEnabled = true;
            Dev.Opacity = 1;
            Dev.Header = "Dev";
            cpuarch.Text = "CPU ARCH: " + RuntimeInformation.ProcessArchitecture;
            osarch.Text = "OS ARCH: " + RuntimeInformation.OSArchitecture;
            osinfo.Text = "OS INFO: Windows Build " + Environment.OSVersion.VersionString.Replace("Microsoft Windows NT 10.0.","").Replace(".0","");
            if (IntPtr.Size == 4) { apparch.Text = "Looks like we are running as a 32 bit process."; }
            else if (IntPtr.Size == 8) { apparch.Text = "Looks like we are running as a 64 bit process."; }
            else { apparch.Text = $"We don't know what architecture we are running on,\nMight want to call for help.\nIntPtr was {IntPtr.Size}, expected 4 or 8."; }
        }
        else { PivotView.Items.Remove(Dev); }
    }

    private void OpenPath(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo()
        {
            FileName = System.IO.Path.Combine(GlobalData.RootDirectory, "Logs"),
            UseShellExecute = true,
            Verb = "open"
        });
    }

    private void OpenDiscordURL(object sender, RoutedEventArgs e)
    {
        Process Browser = new();
        Browser.StartInfo.FileName = @"https://discord.gg/wfZxU4bx6n";
        Browser.StartInfo.UseShellExecute = true;
        Browser.Start();
    }

    private async void SetBackupPath(object sender, RoutedEventArgs e)
    {
        FolderPicker folderPicker = new();
        if (Window.Current == null)
        {
            //IntPtr hwnd = GetActiveWindow();
            IntPtr hwnd = GlobalData.WindowHandle;
            IInitializeWithWindow initializeWithWindow = folderPicker.As<IInitializeWithWindow>();
            initializeWithWindow.Initialize(hwnd);
        }

        folderPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        folderPicker.FileTypeFilter.Add("*");
        StorageFolder folder = await folderPicker.PickSingleFolderAsync();
        if (folder != null)
        {
            Ioc.Default.GetRequiredService<PreferencesViewModel>().BackupDir = folder.Path;
        }
    }
    private async void SetProjectPath(object sender, RoutedEventArgs e)
    {
        FolderPicker folderPicker = new();
        if (Window.Current == null)
        {
            //IntPtr hwnd = GetActiveWindow();
            IntPtr hwnd = GlobalData.WindowHandle;
            IInitializeWithWindow initializeWithWindow = folderPicker.As<IInitializeWithWindow>();
            initializeWithWindow.Initialize(hwnd);
        }

        folderPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        folderPicker.FileTypeFilter.Add("*");
        StorageFolder folder = await folderPicker.PickSingleFolderAsync();
        if (folder != null)
        {
            Ioc.Default.GetRequiredService<PreferencesViewModel>().ProjectDir = folder.Path;
            ProjDirBox.Text = folder.Path; //Updates the box visually (fixes visual glitch.)
        }
    }

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
}