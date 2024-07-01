using Microsoft.UI.Xaml;

namespace StoryCAD.Services.Dialogs;

public sealed partial class RecentFiles : Page
{
    private PreferenceService preferences = Ioc.Default.GetService<PreferenceService>();

    public RecentFiles(UnifiedVM vm)
    {
        InitializeComponent();
        UnifiedMenuVM = vm;

        string[] RecentFiles =
        [
	        preferences.Model.LastFile1,
	        preferences.Model.LastFile2,
	        preferences.Model.LastFile3,
	        preferences.Model.LastFile4,
	        preferences.Model.LastFile5
        ];
        foreach (string File in RecentFiles)
        {
            if (!string.IsNullOrWhiteSpace(File) )
            {
                if (System.IO.File.Exists(File))
                {
                    StackPanel Item = new();
                    ToolTipService.SetToolTip(Item,File);
                    Item.Width = 300;
                    Item.Children.Add(new TextBlock { Text = Path.GetFileNameWithoutExtension(File), FontSize = 20 });
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