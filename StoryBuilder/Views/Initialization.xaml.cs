using System;
using System.Runtime.InteropServices;
using Windows.Storage;
using Windows.Storage.Pickers;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using StoryBuilder.ViewModels.Tools;
using WinRT;

namespace StoryBuilder.Views;

public sealed partial class Initialization : Page
{
    InitVM _initVM = Ioc.Default.GetService<InitVM>();
    public Initialization()
    {
        InitializeComponent();
    }

    /// <summary>
    /// This is called when the browse button next to Project Path
    /// once clicked it opens a folder picker. If cancled the folder
    /// will be null and nothing will happen.
    /// 
    /// If a folder is selected it will set the VM and UI versions of
    /// the variables to make sure they are in sync.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void SetProjectPath(object sender, RoutedEventArgs e)
    {
        FolderPicker folderPicker = new();
        if (Window.Current == null)
        {
            IntPtr hwnd = GetActiveWindow();
            //IntPtr hwnd = GlobalData.WindowHandle;
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
    /// once clicked it opens a folder picker. If cancled the folder
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
            IntPtr hwnd = GetActiveWindow();
            //IntPtr hwnd = GlobalData.WindowHandle;
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


    public void Check(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(_initVM.Path) && !string.IsNullOrWhiteSpace(_initVM.BackupPath) && _initVM.Name != "")
        {
            _initVM.Save();
            RootFrame.Navigate(typeof(Shell));
        }
    }

}