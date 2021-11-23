﻿using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using StoryBuilder.ViewModels;
using System;
using System.IO;
using System.Runtime.InteropServices;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace StoryBuilder.Services.Dialogs
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SaveAsDialog
    {
        public SaveAsViewModel SaveAsVm
        {
            get
            {
                return Ioc.Default.GetService<SaveAsViewModel>();
            }
        }
        public SaveAsDialog()
        {
            InitializeComponent();
        }
        public bool BrowseButtonClicked { get; set; }
        public bool ProjectFolderExists { get; set; }
        public StorageFolder ParentFolder { get; set; }
        public string ParentFolderPath { get; set; }
        public string ProjectFolderPath { get; set; }

        private async void OnClick(object sender, RoutedEventArgs e)
        {
            // may throw error if invalid folder location
            var folderPicker = new FolderPicker();

            //Make FolderPicker work in Win32
            if (Window.Current == null)
            {
                IntPtr hwnd = GetActiveWindow();
                var initializeWithWindow = folderPicker.As<IInitializeWithWindow>();
                initializeWithWindow.Initialize(hwnd);
            }
            folderPicker.CommitButtonText = "Project Parent Folder:";
            folderPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            folderPicker.FileTypeFilter.Add(("*"));

            ParentFolder = await folderPicker.PickSingleFolderAsync();
            //TODO: Test for cancelled FolderPicker via 'if Parentfolder =! null {} else {}
            ProjectFolderExists = await ParentFolder.TryGetItemAsync(ProjectName.Text) != null;
            ProjectFolderPath = Path.Combine(ParentFolder.Path, ProjectName.Text);
            ProjectPathName.Text = ProjectFolderPath;
            BrowseButtonClicked = true;
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