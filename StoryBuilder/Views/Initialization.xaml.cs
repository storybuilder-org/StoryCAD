using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Storage;
using Windows.Storage.Pickers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using CommunityToolkit.Mvvm.DependencyInjection;
using StoryBuilder.Models;
using StoryBuilder.ViewModels.Tools;
using WinRT;
using Elmah.Io.Client;

namespace StoryBuilder.Views;

/// <summary>
/// This Page is displayed if Preferences.Initialised is false.
/// </summary>
public sealed partial class PreferencesInitialization : Page
{
    InitVM _initVM = Ioc.Default.GetService<InitVM>();
    public PreferencesInitialization() { InitializeComponent(); }

    /// <summary>
    /// This is called when the browse button next to Project Path
    /// once clicked it opens a folder picker. If canceled, the folder
    /// will be null and nothing will happen.
    /// 
    /// If a folder is selected it will set the VM and UI versions
    /// of the variables to ensure they are in sync.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
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
            ProjPath.Text = folder.Path;
            _initVM.Path = folder.Path;
        }
    }

    /// <summary>
    /// This is called when the browse button next to Project Path
    /// once clicked it opens a folder picker. If canceled4 the folder
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
            BackPath.Text = folder.Path;
            _initVM.BackupPath = folder.Path;
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

    /// <summary>
    /// Opens discord URL in default browser (Via ShellExecute)
    /// </summary>
    public void Discord(object sender, RoutedEventArgs e)
    {
        Process Browser = new() { StartInfo = new() { FileName = "https://discord.gg/wfZxU4bx6n", UseShellExecute = true } };
        Browser.Start();
    }

    /// <summary>
    /// Checks that the Paths, Name and Email aren't blank or null.
    /// </summary>
    public void Check(object sender, RoutedEventArgs e)
    {
        if (String.IsNullOrWhiteSpace(_initVM.Name))
        {
            _initVM.ErrorMessage = "Please enter your name";
            return;
        }
        if (String.IsNullOrWhiteSpace(_initVM.Email))
        {
            _initVM.ErrorMessage = "Please enter your email";
            return;
        }
        if (String.IsNullOrWhiteSpace(_initVM.Path))
        {
            _initVM.ErrorMessage = "Please set a project path";
            return;
        }
        if (String.IsNullOrWhiteSpace(_initVM.BackupPath))
        {
            _initVM.ErrorMessage = "Please set a backup path";
            return;
        }


        _initVM.Save();
        RootFrame.Navigate(typeof(Shell));
    }

}