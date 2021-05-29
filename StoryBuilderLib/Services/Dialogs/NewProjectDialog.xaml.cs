using Microsoft.UI.Xaml;
using System;
using System.IO;
using System.Runtime.InteropServices;
using Windows.Storage;
using Windows.Storage.Pickers;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using StoryBuilder.ViewModels;
using WinRT;
using Windows.Gaming.Input.ForceFeedback;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace StoryBuilder.Services.Dialogs
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class NewProjectDialog
    {
        public NewProjectViewModel NewProjectVm
        {
            get
            {
                return Ioc.Default.GetService<NewProjectViewModel>();
            }
        }
        public NewProjectDialog()
        {
            InitializeComponent();
        }
        public bool BrowseButtonClicked { get; set; }
        public bool ProjectFolderExists { get; set; }
        public StorageFolder ParentFolder { get; set; }
        public string ParentFolderPath { get; set; }
        public string ProjectFolderPath { get; set; }

        private async void Browse_Click(object sender, RoutedEventArgs e) 
        {
            // Find a home for the new project
            var folderPicker = new FolderPicker();
            if (Window.Current == null)
            {
                IntPtr hwnd = GetActiveWindow();
                var initializeWithWindow = folderPicker.As<IInitializeWithWindow>();
                initializeWithWindow.Initialize(hwnd);
            }
            //BUG: 
            folderPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            folderPicker.FileTypeFilter.Add("*");
            StorageFolder folder = await folderPicker.PickSingleFolderAsync();
            ParentFolderPath = folder.Path;
            NewProjectVm.ParentPathName = folder.Path;
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
