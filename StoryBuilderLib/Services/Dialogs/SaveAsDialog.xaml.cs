using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using StoryBuilder.Models;
using StoryBuilder.ViewModels;
using System;
using System.IO;
using System.Runtime.InteropServices;
using Windows.Storage;
using Windows.Storage.Pickers;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace StoryBuilder.Services.Dialogs
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SaveAsDialog : Page
    {
        public SaveAsDialog()
        {
            InitializeComponent();
            ProjectPathName.IsReadOnly = true;
        }

        public SaveAsViewModel SaveAsVm => Ioc.Default.GetService<SaveAsViewModel>();

        public StorageFolder ParentFolder { get; set; }
        public string ParentFolderPath { get; set; }
        public string ProjectFolderPath { get; set; }

        private async void OnBrowse(object sender, RoutedEventArgs e)
        {
            ProjectPathName.IsReadOnly = false;
            // may throw error if invalid folder location
            FolderPicker folderPicker = new();
            WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, GlobalData.WindowHandle);

            //Make FolderPicker work in Win32
            //if (Window.Current == null)
            //{
            //    IntPtr hwnd = GetActiveWindow();
            //    var initializeWithWindow = folderPicker.As<IInitializeWithWindow>();
            //    initializeWithWindow.Initialize(hwnd);
            //}
            folderPicker.CommitButtonText = "Project Parent Folder:";
            folderPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            folderPicker.FileTypeFilter.Add("*");

            ParentFolder = await folderPicker.PickSingleFolderAsync();
            SaveAsVm.ParentFolder = ParentFolder;

            //TODO: Test for cancelled FolderPicker via 'if Parentfolder =! null {} else {}
            
            ProjectFolderPath = Path.Combine(ParentFolder.Path, ProjectName.Text);
            ProjectPathName.Text = ProjectFolderPath;
            SaveAsVm.ProjectPathName = ProjectFolderPath;
            ProjectPathName.IsReadOnly = true;
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

    }
}
