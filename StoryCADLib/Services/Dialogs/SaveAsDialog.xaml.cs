﻿using Windows.Storage;
using Microsoft.UI.Xaml;

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
        SaveAsVm.ParentFolder = ParentFolder.Path;

        if (ParentFolder != null)
        {
            ProjectFolderPath = ParentFolder.Path;
            ProjectPathName.Text = ProjectFolderPath;
            SaveAsVm.ProjectPathName = ProjectFolderPath;
            ProjectPathName.IsReadOnly = true;
        }
    }
}