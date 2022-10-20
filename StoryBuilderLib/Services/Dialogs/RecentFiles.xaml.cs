using System.IO;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using StoryBuilder.Models;
using StoryBuilder.ViewModels;

namespace StoryBuilder.Services.Dialogs;

public sealed partial class RecentFiles
{
    public RecentFiles(UnifiedVM vm)
    {
        InitializeComponent();
        UnifiedMenuVM = vm;

        string[] _RecentFiles = { GlobalData.Preferences.LastFile1 , GlobalData.Preferences.LastFile2, GlobalData.Preferences.LastFile3, GlobalData.Preferences.LastFile4, GlobalData.Preferences.LastFile5};
        foreach (string _File in _RecentFiles)
        {
            if (!string.IsNullOrWhiteSpace(_File))
            {
                if (File.Exists(_File))
                {
                    StackPanel _Item = new();
                    ToolTipService.SetToolTip(_Item,_File);
                    _Item.Width = 300;
                    _Item.Children.Add(new TextBlock { Text = Path.GetFileName(_File).Replace(".stbx", ""), FontSize = 20 });
                    _Item.Children.Add(new TextBlock { Text = "Last edited: " + File.GetLastWriteTime(_File), FontSize = 10, VerticalAlignment = VerticalAlignment.Center });
                    Recents.Items.Add(_Item);
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