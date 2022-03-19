using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using StoryBuilder.Models;
using StoryBuilder.ViewModels;
using System.IO;
using System.Linq;
using System.Net.Mime;
using Elmah.Io.Client;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace StoryBuilder.Services.Dialogs;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class RecentFiles : Page
{
    public RecentFiles(UnifiedVM vm)
    {
        InitializeComponent();
        UnifiedMenuVM = vm;

        string[] RecentFiles = new []{ GlobalData.Preferences.LastFile1 , GlobalData.Preferences.LastFile2, GlobalData.Preferences.LastFile3, GlobalData.Preferences.LastFile4, GlobalData.Preferences.LastFile5};
        foreach (var File in RecentFiles)
        {
            if (!string.IsNullOrWhiteSpace(File))
            {/*
                Grid Item = new();
                Item.RowDefinitions.Add(new(){Height = new(50)});
                Item.RowDefinitions.Add(new() { Height = new(50) });
                Item.ColumnDefinitions.Add(new() { Width = new(50) });
                Item.ColumnDefinitions.Add(new() { Width = new() });

                Item.Children.Add(new TextBlock{ Text = Path.GetFileName(File).Replace(".stbx", ""), FontSize = 20});
                StackPanel SubItem = new();
                SubItem.Orientation=Orientation.Horizontal;
                SubItem.SetValue(Grid.RowProperty, 1);
                Item.Children.Add(new TextBlock(){ Text = "DATE PLACEHOLDER", FontSize = 10});
                Item.Children[1].SetValue(Grid.RowProperty, 1);
                SubItem.Children.Add(new TextBlock{ Text = File, FontSize = 10});
                Item.Children.Add(SubItem);
                Pannel.Children.Add(Item);*/

                StackPanel Item = new();
                Item.Width = 300;
                Item.Children.Add(new TextBlock { Text = Path.GetFileName(File).Replace(".stbx", ""), FontSize = 20});
                Item.Children.Add(new TextBlock { Text = "Last edited: " + System.IO.File.GetLastWriteTime(File), FontSize = 10, VerticalAlignment = VerticalAlignment.Center});
                Recents.Items.Add(Item);

            }
        }
    }
    public UnifiedVM UnifiedMenuVM;
}