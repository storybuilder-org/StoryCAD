using System;
using System.Runtime.InteropServices;
using Windows.Storage;
using Windows.Storage.Pickers;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using StoryCAD.Models;
using StoryCAD.ViewModels;
using WinRT.Interop;

namespace StoryCAD.Services.Dialogs;

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
        ParentFolder = await Ioc.Default.GetService<Windowing>().ShowFolderPicker();
        SaveAsVm.ParentFolder = ParentFolder;

        if (ParentFolder != null)
        {
            ProjectFolderPath = ParentFolder.Path;
            ProjectPathName.Text = ProjectFolderPath;
            SaveAsVm.ProjectPathName = ProjectFolderPath;
            ProjectPathName.IsReadOnly = true;
        }
    }
}