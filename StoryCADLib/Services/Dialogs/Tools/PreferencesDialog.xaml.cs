using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Windows.Storage;
using Windows.Storage.Pickers;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Octokit;
using StoryCAD.Models;
using StoryCAD.Services.Installation;
using StoryCAD.Services.Logging;
using StoryCAD.ViewModels.Tools;
using WinRT;
using Microsoft.UI.Xaml.Controls;

namespace StoryCAD.Services.Dialogs.Tools;

public sealed partial class PreferencesDialog
{
    public PreferencesViewModel PreferencesVm => Ioc.Default.GetService<PreferencesViewModel>();
    public InstallationService InstallVM => Ioc.Default.GetRequiredService<InstallationService>();
    public LogService Logger => Ioc.Default.GetRequiredService<LogService>();
    public PreferencesDialog()
    {
        InitializeComponent();
        DataContext = PreferencesVm;
        ShowInfo();
    }

    /// <summary>
    /// Sets info text for changelog and dev menu
    /// </summary>
    private async void ShowInfo()
    {
        Version.Text = PreferencesVm.CurrentModel.Version;
        Changelog.Text = await new Changelog().GetChangelogText();

        if (PreferencesVm.CurrentModel.WrapNodeNames == TextWrapping.WrapWholeWords) { TextWrap.IsChecked = true; }
        else { TextWrap.IsChecked = false; }

        SearchEngine.SelectedIndex = (int)PreferencesVm.CurrentModel.PreferredSearchEngine;


        //TODO: Put this in a VM and make this data get logged at start up with some more system info.
        if (Debugger.IsAttached)
        {
            DeviceInfo.Text = $"""
                Device Information Report

                CPU Architecture = { RuntimeInformation.ProcessArchitecture},
                OS Architecture =  {RuntimeInformation.OSArchitecture},
                .NET Runtime Version = {RuntimeInformation.FrameworkDescription},
                Windows Build = {Environment.OSVersion.Version.Build},
                Boot Time = {GlobalData.StartUpTimer.ElapsedMilliseconds} Milliseconds,
                """ ;

            try
            {
                if (Convert.ToInt32(Environment.OSVersion.Version.Build) >= 22000)
                {
                    DeviceInfo.Text += "\nWindows Version: Windows 11";
                }
                else { DeviceInfo.Text += "\nWindows Version: Windows 10"; }
            }
            catch { DeviceInfo.Text += "\nOS Info:Error"; }


            //Detect if 32-bit or 64-bit process (I'm not sure if it's possible to )
            if (IntPtr.Size == 4) { DeviceInfo.Text += "\nApp Architecture - 32 bit"; }
            else if (IntPtr.Size == 8) { DeviceInfo.Text += "\nApp Architecture - 64 bit"; }
            else { DeviceInfo.Text += $"\nApp Architecture - Unknown. (Report that IntPtr is {IntPtr.Size})"; }
        }
        else //Remove this because no debugger is attached.
        {
            PivotView.Items.Remove(Dev);
        }
    }

    /// <summary>
    /// Opens the Log Folder
    /// </summary>
    private void OpenLogFolder(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo { FileName = Path.Combine(GlobalData.RootDirectory, "Logs"), UseShellExecute = true, Verb = "open" });
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
            PreferencesVm.CurrentModel.BackupDirectory = _folder.Path;
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
            PreferencesVm.CurrentModel.ProjectDirectory = folder.Path;
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
    /// StoryCAD is opened the PreferencesInitialization
    /// page will be shown.
    /// </summary>
    private void SetInitToFalse(object sender, RoutedEventArgs e)
    {
        PreferencesVm.CurrentModel.PreferencesInitialized = false;
    }

    /// <summary>
    /// This toggles the status of preferences.TextWrapping
    /// </summary>
    private void ToggleWrapping(object sender, RoutedEventArgs e)
    {
        if ((sender as CheckBox).IsChecked == true)
        {
            PreferencesVm.CurrentModel.WrapNodeNames = TextWrapping.WrapWholeWords;
        }
        else { PreferencesVm.CurrentModel.WrapNodeNames = TextWrapping.NoWrap; }
    }

    /// <summary>
    /// This sets the backup service timer to run every 10 seconds
    /// to speed up testing, this should not be saved.
    /// </summary>
    public void FastBackupService(object sender, RoutedEventArgs e)
    {
        Ioc.Default.GetRequiredService<BackupService>().EnableFastBackupService = true;
    }
}