using System.Diagnostics;

namespace StoryCADLib.Services.Dialogs;

/// <summary>
///     File open menu page, allows user to open and create outlines/samples
/// </summary>
public sealed partial class FileOpenMenuPage : Page
{
    /// <summary>
    ///     Newest backups the Backups tab lists. This constructor runs on the UI thread
    ///     before the dialog shows, and it builds a StackPanel per entry; a backup folder
    ///     on a cloud drive held 5,219 zips on 2026-09-04 and the menu took 5 s to appear.
    ///     Older backups stay on disk; the tab shows the ones a writer would restore.
    /// </summary>
    private const int MaxBackupsListed = 100;

    public FileOpenVM FileOpenVM = Ioc.Default.GetRequiredService<FileOpenVM>();

    public FileOpenMenuPage()
    {
        var stopwatch = Stopwatch.StartNew();
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

        // Get the newest backups from the backup directory. Order comes from the
        // timestamp in each file name, not from a stat per file: on a cloud-synced
        // folder every GetLastWriteTime is a round trip, and the old code made three
        // of them per zip before the dialog could show.
        // On macOS, the sandbox may deny access if the folder was only granted
        // via a previous session's folder picker. Gracefully handle this.
        var backupDir = Ioc.Default.GetRequiredService<PreferenceService>().Model.BackupDirectory;
        FileOpenVM.BackupPaths = Array.Empty<string>();
        var totalBackups = 0;
        if (!string.IsNullOrWhiteSpace(backupDir))
        {
            try
            {
                if (!Directory.Exists(backupDir))
                {
                    Directory.CreateDirectory(backupDir);
                }

                var allBackups = Directory.GetFiles(backupDir);
                totalBackups = allBackups.Length;
                FileOpenVM.BackupPaths = FileOpenVM.OrderBackupsNewestFirst(allBackups)
                    .Take(MaxBackupsListed)
                    .ToArray();
            }
            catch (UnauthorizedAccessException)
            {
                Ioc.Default.GetRequiredService<ILogService>().Log(LogLevel.Warn,
                    $"Sandbox denied access to backup directory '{backupDir}'. Backups tab will be empty.");
            }
            catch (IOException ex)
            {
                Ioc.Default.GetRequiredService<ILogService>().Log(LogLevel.Warn,
                    $"Cannot access backup directory '{backupDir}': {ex.Message}");
            }
        }

        // BackupUI must stay index-aligned with BackupPaths: ConfirmClicked opens
        // BackupPaths[SelectedBackupIndex]. GetFiles returned existing files, so no
        // entry is skipped here, and the list is cleared first because FileOpenVM is a
        // singleton and every earlier open had appended its rows to the last ones.
        FileOpenVM.BackupUI.Clear();
        foreach (var file in FileOpenVM.BackupPaths)
        {
            var stamp = FileOpenVM.ParseBackupTimestamp(file) ?? File.GetLastWriteTime(file);

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
                       + " at " + stamp,
                FontSize = 16,
                TextWrapping = TextWrapping.Wrap,
                TextTrimming = TextTrimming.CharacterEllipsis
            });

            FileOpenVM.BackupUI.Add(item);
        }

        Ioc.Default.GetRequiredService<ILogService>().Log(LogLevel.Info,
            $"File menu: {FileOpenVM.RecentsUI.Count} recents, {FileOpenVM.BackupPaths.Length} of {totalBackups} backups listed in {stopwatch.ElapsedMilliseconds} ms");
    }
}
