using System;
using Windows.Storage;
using Windows.Storage.Pickers;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using StoryBuilder.Models;
using StoryBuilder.ViewModels;
using WinRT.Interop;

namespace StoryBuilder.Services.Dialogs;

public sealed partial class SaveAsDialog
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
        FolderPicker _FolderPicker = new();
        InitializeWithWindow.Initialize(_FolderPicker, GlobalData.WindowHandle);
        
        _FolderPicker.CommitButtonText = "Select folder";
        _FolderPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        _FolderPicker.FileTypeFilter.Add("*");

        ParentFolder = await _FolderPicker.PickSingleFolderAsync();
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