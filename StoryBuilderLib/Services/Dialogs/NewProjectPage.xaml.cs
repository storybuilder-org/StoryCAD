using System;
using System.IO;
using System.Runtime.InteropServices;
using Windows.Storage;
using Windows.Storage.Pickers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using StoryBuilder.ViewModels;
using WinRT;

namespace StoryBuilder.Services.Dialogs;

public sealed partial class NewProjectPage
{
    public NewProjectPage(UnifiedVM vm)
    {
        InitializeComponent();
        UnifiedVM = vm;
    }

    public UnifiedVM UnifiedVM;

    public bool BrowseButtonClicked { get; set; }
    public bool ProjectFolderExists { get; set; }
    public StorageFolder ParentFolder { get; set; }
    public string ParentFolderPath { get; set; }
    public string ProjectFolderPath { get; set; }

    private async void Browse_Click(object sender, RoutedEventArgs e)
    {
        // Find a home for the new project
        FolderPicker _FolderPicker = new();
        if (Window.Current == null)
        {
            IntPtr _Hwnd = GetActiveWindow();
            //IntPtr hwnd = GlobalData.WindowHandle;
            IInitializeWithWindow _InitializeWithWindow = _FolderPicker.As<IInitializeWithWindow>();
            _InitializeWithWindow.Initialize(_Hwnd);
        }
        _FolderPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        _FolderPicker.FileTypeFilter.Add("*");
        StorageFolder _Folder = await _FolderPicker.PickSingleFolderAsync();
        if (_Folder != null)
        {
            ParentFolderPath = _Folder.Path;
            UnifiedVM.ProjectPath = _Folder.Path;
        }

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

    private void CheckValidity(object sender, RoutedEventArgs e)
    {
        //Checks file name validity
        try
        {
            File.Create(Path.Combine(Path.GetTempPath(), ProjectName.Text));
        }
        catch (Exception _Ex)
        {
            if (_Ex.Message.Contains("The filename, directory name, or volume label syntax is incorrect. "))
            {
                ProjectName.Text = "";
                ProjectName.PlaceholderText = "You can't call your file that!";
                return;
            }
        }

        //Checks file path validity
        try { Directory.CreateDirectory(ProjectPathName.Text); }
        catch
        {
            ProjectPathName.Text = "";
            ProjectPathName.PlaceholderText = "You can't put files here!";
            return;
        }
        UnifiedVM.MakeProject();
    }
}