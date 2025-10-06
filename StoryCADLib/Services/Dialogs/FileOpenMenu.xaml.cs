namespace StoryCAD.Services.Dialogs;

/// <summary>
///     File open menu page, allows user to open and create outlines/samples
/// </summary>
public sealed partial class FileOpenMenuPage
{
    public FileOpenVM FileOpenVM = Ioc.Default.GetRequiredService<FileOpenVM>();

    public FileOpenMenuPage()
    {
        InitializeComponent();
        FileOpenVM.RecentsTabContentVisibility = Visibility.Collapsed;
        FileOpenVM.SamplesTabContentVisibility = Visibility.Collapsed;
        FileOpenVM.BackupTabContentVisibility = Visibility.Collapsed;
        FileOpenVM.NewTabContentVisibility = Visibility.Collapsed;
        FileOpenVM.OutlineName = "";
        FileOpenVM.CurrentTab = new NavigationViewItem { Tag = "Recent" };

        //Set recent files.
        var preferences = Ioc.Default.GetRequiredService<PreferenceService>();
        FileOpenVM.RecentsUI.Clear();
        foreach (var file in preferences.Model.RecentFiles)
        {
            //Skip entries that don't exist or are empty
            if (string.IsNullOrWhiteSpace(file) || !File.Exists(file))
            {
                continue;
            }

            //Create
            StackPanel item = new()
            {
                Orientation = Orientation.Vertical,
                MaxWidth = Math.Max(320, (XamlRoot?.Size.Width ?? 1000) - 64)
            };
            ToolTipService.SetToolTip(item, file);
            item.Children.Add(new TextBlock
            {
                Text = Path.GetFileNameWithoutExtension(file), FontSize = 20, TextWrapping = TextWrapping.Wrap,
                TextTrimming = TextTrimming.CharacterEllipsis
            });
            item.Children.Add(new TextBlock
            {
                Text = "Last edited: " + File.GetLastWriteTime(file),
                FontSize = 12,
                TextWrapping = TextWrapping.NoWrap,
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center
            });
            FileOpenVM.RecentsUI.Add(item);
        }

        // Get files from the backup directory in order of creation.
        // Ensure the directory exists to avoid exceptions on first run.
        var backupDir = Ioc.Default.GetRequiredService<PreferenceService>().Model.BackupDirectory;
        FileOpenVM.BackupPaths = Array.Empty<string>();
        if (!string.IsNullOrWhiteSpace(backupDir))
        {
            if (!Directory.Exists(backupDir))
            {
                Directory.CreateDirectory(backupDir);
            }

            FileOpenVM.BackupPaths = Directory
                .GetFiles(backupDir)
                .OrderByDescending(File.GetLastWriteTime)
                .ToArray();
        }

        foreach (var file in FileOpenVM.BackupPaths)
        {
            //Skip entries that don't exist or are empty
            if (string.IsNullOrWhiteSpace(file) || !File.Exists(file))
            {
                continue;
            }

            //Create
            StackPanel item = new()
            {
                Orientation = Orientation.Vertical,
                MaxWidth = Math.Max(320, (XamlRoot?.Size.Width ?? 1000) - 64)
            };
            ToolTipService.SetToolTip(item, file);
            item.Children.Add(new TextBlock
            {
                //Shows as OutlineName At DATETIME
                Text = Path.GetFileNameWithoutExtension(file.Split(" as of ")[0])
                       + " at " + File.GetLastWriteTime(file),
                FontSize = 16,
                TextWrapping = TextWrapping.Wrap,
                TextTrimming = TextTrimming.CharacterEllipsis
            });

            FileOpenVM.BackupUI.Add(item);
        }
    }
}
