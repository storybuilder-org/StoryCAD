using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Storage;
using Windows.Storage.Pickers;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using StoryCAD.Models;
using StoryCAD.ViewModels.Tools;
using WinRT;

namespace StoryCAD.Views;

/// <summary>
/// This Page is displayed if Preferences.Initialise is false.
/// </summary>
public sealed partial class PreferencesInitialization
{
    private InitVM _initVM = Ioc.Default.GetService<InitVM>();
    public PreferencesInitialization() { InitializeComponent(); }

    /// <summary>
    /// This is called when the browse button next to Project Path
    /// once clicked it opens a folder picker. If canceled, the folder
    /// will be null and nothing will happen.
    /// 
    /// If a folder is selected it will set the VM and UI versions
    /// of the variables to ensure they are in sync.
    /// </summary>
    private async void SetProjectPath(object sender, RoutedEventArgs e)
    {
        FolderPicker folderPicker = new();
        if (Window.Current == null)
        {
            IntPtr hwnd = GlobalData.WindowHandle;
            IInitializeWithWindow initializeWithWindow = folderPicker.As<IInitializeWithWindow>();
            initializeWithWindow.Initialize(hwnd);
        }

        folderPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        folderPicker.FileTypeFilter.Add("*");
        StorageFolder folder = await folderPicker.PickSingleFolderAsync();
        if (folder != null)
        {
            _initVM.Preferences.ProjectDirectory = folder.Path;
        }
    }

    /// <summary>
    /// This is called when the browse button next to Project Path
    /// once clicked it opens a folder picker. If canceled the folder
    /// will be null and nothing will happen.
    /// 
    /// If a folder is selected it will set the VM and UI versions of
    /// the variables to make sure they are in sync.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void SetBackupPath(object sender, RoutedEventArgs e)
    {
        FolderPicker folderPicker = new();
        if (Window.Current == null)
        {
            IntPtr hwnd = GlobalData.WindowHandle;
            IInitializeWithWindow initializeWithWindow = folderPicker.As<IInitializeWithWindow>();
            initializeWithWindow.Initialize(hwnd);
        }

        folderPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        folderPicker.FileTypeFilter.Add("*");
        StorageFolder folder = await folderPicker.PickSingleFolderAsync();
        if (folder != null)
        {
            _initVM.Preferences.BackupDirectory = folder.Path;
        }
    }

    [ComImport]
    [Guid("3E68D4BD-7135-4D10-8018-9FB6D9F33FA1")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IInitializeWithWindow
    {
        void Initialize(IntPtr hwnd);
    }

    /// <summary>
    /// Opens discord URL in default browser (Via ShellExecute)
    /// </summary>
    public void Discord(object sender, RoutedEventArgs e)
    {
        Process browser = new() { StartInfo = new() { FileName = "https://discord.gg/wfZxU4bx6n", UseShellExecute = true } };
        browser.Start();
    }

    /// <summary>
    /// Checks that the Paths, Name and Email aren't blank or null.
    /// </summary>
    public void Check(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_initVM.Preferences.Name))
        {
            _initVM.ErrorMessage = "Please enter your Name";
            return;
        }
        if (string.IsNullOrWhiteSpace(_initVM.Preferences.Email))
        {
            _initVM.ErrorMessage = "Please enter your Email";
            return;
        }

        if (!_initVM.Preferences.Email.Contains("@") || !_initVM.Preferences.Email.Contains("."))
        {
            _initVM.ErrorMessage = "Please enter a valid email address.";
            return;
        }
        if (string.IsNullOrWhiteSpace(_initVM.Preferences.ProjectDirectory))
        {
            _initVM.ErrorMessage = "Please set a Project path";
            return;
        }
        if (string.IsNullOrWhiteSpace(_initVM.Preferences.BackupDirectory))
        {
            _initVM.ErrorMessage = "Please set a Backup path";
            return;
        }

        _initVM.Save();
        RootFrame.Navigate(typeof(Shell));
    }

}