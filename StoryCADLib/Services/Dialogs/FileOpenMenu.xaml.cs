﻿using Microsoft.UI.Xaml;
namespace StoryCAD.Services.Dialogs;

/// <summary>
/// File open menu page, allows user to open and create outlines/samples
/// </summary>
public sealed partial class FileOpenMenuPage
{
    public FileOpenVM FileOpenVM = Ioc.Default.GetRequiredService<FileOpenVM>();

    public FileOpenMenuPage()
    {
        InitializeComponent();
        FileOpenVM.RecentsTabContentVisibility = Visibility.Collapsed;
        FileOpenVM.SamplesTabContentVisibility = Visibility.Collapsed;
        FileOpenVM.NewTabContentVisibility = Visibility.Collapsed;
        FileOpenVM.OutlineName = "";
        FileOpenVM.CurrentTab = new NavigationViewItem() { Tag = "Recent" };

        //Set recent files.
        var preferences = Ioc.Default.GetRequiredService<PreferenceService>();
        FileOpenVM.RecentsUI.Clear();
        foreach (var file in preferences.Model.RecentFiles)
        {
            //Skip entries that don't exist or are empty
            if (string.IsNullOrWhiteSpace(file) || !File.Exists(file)) continue;

            //Create
            StackPanel item = new() { Width = 300 };
            ToolTipService.SetToolTip(item, file);
            item.Children.Add(new TextBlock { Text = Path.GetFileNameWithoutExtension(file), FontSize = 20 });
            item.Children.Add(new TextBlock
            {
                Text = "Last edited: " + File.GetLastWriteTime(file),
                FontSize = 10,
                VerticalAlignment = VerticalAlignment.Center
            });
            FileOpenVM.RecentsUI.Add(item);
        }
    }
}