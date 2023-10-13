using System.IO;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using StoryCAD.Models;
using StoryCAD.ViewModels;

namespace StoryCAD.Services.Dialogs;

public sealed partial class RecentFiles : Page
{
    private AppState State = Ioc.Default.GetService<AppState>();

    public RecentFiles(UnifiedVM vm)
    {
        InitializeComponent();
        UnifiedMenuVM = vm;

        string[] RecentFiles = { State.Preferences.LastFile1,
            State.Preferences.LastFile2,
            State.Preferences.LastFile3,
            State.Preferences.LastFile4,
            State.Preferences.LastFile5 };
        foreach (string File in RecentFiles)
        {
            if (!string.IsNullOrWhiteSpace(File) )
            {
                if (System.IO.File.Exists(File))
                {
                    StackPanel Item = new();
                    ToolTipService.SetToolTip(Item,File);
                    Item.Width = 300;
                    Item.Children.Add(new TextBlock { Text = Path.GetFileName(File).Replace(".stbx", ""), FontSize = 20 });
                    Item.Children.Add(new TextBlock { Text = "Last edited: " + System.IO.File.GetLastWriteTime(File), FontSize = 10, VerticalAlignment = VerticalAlignment.Center });
                    Recents.Items.Add(Item);
                }
            }
        }

        if (Recents.Items.Count == 0)
        {
            Recents.Items.Add(new TextBlock{Text = "No files have been opened recently."});
            Recents.IsEnabled = false;
        }
    }
    public UnifiedVM UnifiedMenuVM;
}