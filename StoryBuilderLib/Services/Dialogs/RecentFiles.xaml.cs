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
            if (!string.IsNullOrWhiteSpace(File) )
            {
                if (System.IO.File.Exists(File))
                {
                    StackPanel Item = new();
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