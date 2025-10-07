using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using FileAttributes = System.IO.FileAttributes;

#if WINDOWS
using Windows.Storage;
#endif

namespace StoryCADLib.DAL;

/// <summary>
///     Handles Reading and Writing of Story Data
/// </summary>
public class StoryIO
{
    private readonly AppState _appState;
    private readonly ILogService _logService;

    public StoryIO(ILogService logService, AppState appState)
    {
        _logService = logService;
        _appState = appState;
    }

    /// <summary>
    ///     Writes the current Story to the disk
    /// </summary>
    public async Task WriteStory(string output_path, StoryModel model)
    {
        var parent = Path.GetDirectoryName(output_path);
        Directory.CreateDirectory(parent!);

        var folder = await StorageFolder.GetFolderFromPathAsync(parent);
        var output =
            await folder.CreateFileAsync(Path.GetFileName(output_path), CreationCollisionOption.ReplaceExisting);
        _logService.Log(LogLevel.Info, $"Saving Model to disk as {output_path}  " +
                                       $"Elements: {model.StoryElements.StoryElementGuids.Count}");

        //Save version data
        model.LastVersion = _appState.Version;
        _logService.Log(LogLevel.Info, $"Saving version as {model.LastVersion}");

        var json = model.Serialize();
        _logService.Log(LogLevel.Trace, $"Serialised as {json}");

        //Save file to disk
        await FileIO.WriteTextAsync(output, json);
    }


    public async Task<StoryModel> ReadStory(StorageFile StoryFile)
    {
        try
        {
            //Read file
            _logService.Log(LogLevel.Info, $"Reading Model from disk ({StoryFile.Path})");
            var JSON = await FileIO.ReadTextAsync(StoryFile);

            //Check if file is legacy
            if (JSON.Split("\n")[0].Contains("<?xml version=\"1.0\" encoding=\"utf-8\"?>"))
            {
                _logService.Log(LogLevel.Info, "File is legacy XML format");

                // Show dialog informing user about legacy format
                var result = await Ioc.Default.GetRequiredService<Windowing>().ShowContentDialog(new ContentDialog
                {
                    Title = "Legacy Outline format detected!",
                    Content = """
                              This outline is in an older format that’s no longer supported.
                              To update it, please use the Legacy STBX tool to update your outlines to the current format.
                              """,
                    PrimaryButtonText = "Download Conversion Tool",
                    SecondaryButtonText = "Close"
                }, true);

                // Open the conversion tool repository if user clicks primary button
                if (result == ContentDialogResult.Primary)
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName =
                            "https://github.com/storybuilder-org/StoryCAD-Legacy-STBX-Conversion-Tool/releases/tag/1.0.0",
                        UseShellExecute = true
                    });
                }

                return new StoryModel(); // Return empty model
            }

            _logService.Log(LogLevel.Info, $"Read file (Length: {JSON.Length})");

            //Deserialize into real story model
            _logService.Log(LogLevel.Trace, $"File read as {JSON}");
            var _model = JsonSerializer.Deserialize<StoryModel>(JSON, new JsonSerializerOptions
            {
                Converters =
                {
                    new EmptyGuidConverter(),
                    new StoryElementConverter(),
                    new JsonStringEnumConverter()
                }
            });

            //Rebuild tree.
            _model.ExplorerView = RebuildTree(_model.FlattenedExplorerView, _model.StoryElements, _logService);
            _model.NarratorView = RebuildTree(_model.FlattenedNarratorView, _model.StoryElements, _logService);

            // Rebuild TrashView if it exists
            if (_model.FlattenedTrashView != null && _model.FlattenedTrashView.Any())
            {
                _model.TrashView = RebuildTree(_model.FlattenedTrashView, _model.StoryElements, _logService);
            }
            else
            {
                // Create empty TrashView with TrashCan root
                _model.TrashView = new ObservableCollection<StoryNodeItem>();
                var trashCan = new TrashCanModel(_model, null);
                _model.TrashView.Add(trashCan.Node);
            }

            // Check for legacy dual-root structure and migrate if needed
            if (_model.ExplorerView.Count > 1 && _model.ExplorerView.Any(n => n.Type == StoryItemType.TrashCan))
            {
                _logService.Log(LogLevel.Info, "Detected legacy dual-root structure, migrating...");

                // Find and remove TrashCan from ExplorerView
                var trashCanNode = _model.ExplorerView.FirstOrDefault(n => n.Type == StoryItemType.TrashCan);
                if (trashCanNode != null)
                {
                    _model.ExplorerView.Remove(trashCanNode);

                    // Move TrashCan and its children to TrashView
                    _model.TrashView.Clear();
                    _model.TrashView.Add(trashCanNode);
                }
            }

            // Re-attach collection change handlers (they're not serialized)
            _model.ReattachCollectionHandlers();

            // Set CurrentView to ExplorerView by default
            _model.CurrentView = _model.ExplorerView;
            _model.CurrentViewType = StoryViewType.ExplorerView;

            _logService.Log(LogLevel.Info, $"CurrentView set with {_model.CurrentView?.Count ?? 0} items");

            //Log info about story
            _logService.Log(LogLevel.Info, $"Model deserialized as {_model.ExplorerView[0].Name}");
            _logService.Log(LogLevel.Info, $"Model contains as {_model.StoryElements.Count}" +
                                           $" (Explorer: {_model.ExplorerView.Count}/Narrator{_model.NarratorView.Count})");
            _logService.Log(LogLevel.Info, $"Version created with {_model.FirstVersion ?? "Pre-JSON"}");
            _logService.Log(LogLevel.Info, $"Version last saved with {_model.LastVersion ?? "Error"}");

            //Update file information
            if (_appState.CurrentDocument != null)
            {
                _appState.CurrentDocument.FilePath = StoryFile.Path;
            }

            return _model;
        }
        catch (Exception ex)
        {
            _logService.LogException(LogLevel.Error, ex,
                $"Failed to read file ({StoryFile.Path}) " +
                $"{ex.Message}\n{ex.StackTrace}");
        }

        return new StoryModel();
    }


    private static ObservableCollection<StoryNodeItem> RebuildTree(
        List<PersistableNode> flatNodes, StoryElementCollection storyElements,
        ILogService logger)
    {
        var lookup = new Dictionary<Guid, StoryNodeItem>();

        // Remove duplicate entries (same UUID with different parent relationships)
        var uniqueNodes = new Dictionary<Guid, PersistableNode>();
        foreach (var n in flatNodes)
        {
            // Keep the first occurrence of each UUID
            if (!uniqueNodes.ContainsKey(n.Uuid))
            {
                uniqueNodes[n.Uuid] = n;
            }
        }

        flatNodes = uniqueNodes.Values.ToList();

        // First create all nodes (empty parent/children)
        foreach (var n in flatNodes)
        {
            var element = storyElements.StoryElementGuids[n.Uuid];
            StoryNodeItem nodeItem = new(element, null);
            element.Node = nodeItem;
            lookup[n.Uuid] = nodeItem;
        }

        // Now link each node to its parent
        ObservableCollection<StoryNodeItem> rootCollection = new();
        foreach (var n in flatNodes)
        {
            var nodeItem = lookup[n.Uuid];
            if (n.ParentUuid.HasValue && lookup.TryGetValue(n.ParentUuid.Value, out var parentItem))
            {
                nodeItem.Parent = parentItem;
                parentItem.Children.Add(nodeItem);
                nodeItem.IsRoot = false;
            }
            else // No parent => it's a root node
            {
                nodeItem.IsRoot = true;
                nodeItem.IsExpanded = true;
                rootCollection.Add(nodeItem);
            }
        }

        return rootCollection;
    }


    /// <summary>
    /// Checks if a file is available for reading across platforms.
    /// </summary>
    /// <param name="filePath">Full path to the file to check</param>
    /// <param name="probeBytes">Number of bytes to attempt reading (default 1024)</param>
    /// <param name="timeoutMs">Timeout in milliseconds for the availability check (default 1500)</param>
    /// <returns>True if file exists and is readable, false otherwise</returns>
    private async Task<bool> IsAvailableAsync(string filePath, int probeBytes = 1024, int timeoutMs = 1500)
    {
        if (string.IsNullOrWhiteSpace(filePath)) return false;

        // Fast fail if the path doesn't exist on this platform
        if (!File.Exists(filePath)) return false;

#if WINDOWS
        // 1) Heuristics that often catch dehydrated cloud placeholders
        var attrs = File.GetAttributes(filePath);
        if ((attrs & FileAttributes.Offline) != 0) return false;

        // 2) Windows StorageFile view (can still hydrate on demand, so not definitive)
        try
        {
            var sf = await StorageFile.GetFileFromPathAsync(filePath).AsTask().ConfigureAwait(false);
            if (sf is null || !sf.IsAvailable) return false;
        }
        catch
        {
            return false;
        }
#endif

        // 3) Final arbiter everywhere: a tiny, timed async read off the UI thread
        return await TryTinyReadAsync(filePath, probeBytes, timeoutMs).ConfigureAwait(false);
    }


    /// <summary>
    /// Attempts to read a small portion of a file to verify it's actually accessible.
    /// </summary>
    /// <param name="path">Full path to the file</param>
    /// <param name="probeBytes">Number of bytes to attempt reading</param>
    /// <param name="timeoutMs">Timeout in milliseconds</param>
    /// <returns>True if the read succeeds within the timeout, false otherwise</returns>
    private async Task<bool> TryTinyReadAsync(string path, int probeBytes, int timeoutMs)
    {
        if (probeBytes <= 0) probeBytes = 1;
        if (timeoutMs <= 0) timeoutMs = 1500;

        using var cts = new CancellationTokenSource(timeoutMs);
        try
        {
            // Use small buffer + async stream. Avoid locking the file for writes: Read/ShareRead
            var buffer = new byte[probeBytes];

#if WINDOWS
            // Windows: async FileStream honors CancellationToken on ReadAsync
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
            var read = await fs.ReadAsync(buffer.AsMemory(0, buffer.Length), cts.Token).ConfigureAwait(false);
            return read > 0 || fs.Length >= 0; // opened and readable
#else
            // Other Uno targets (Android, iOS, macOS, WebAssembly, Linux):
            // System.IO async works; still use timeout to guard cloud-provider stalls
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
            var readTask = fs.ReadAsync(buffer, 0, buffer.Length, cts.Token);
            var completed = await Task.WhenAny(readTask, Task.Delay(Timeout.Infinite, cts.Token)).ConfigureAwait(false);
            if (completed != readTask) return false; // timed out
            var n = await readTask.ConfigureAwait(false);
            return n > 0 || fs.Length >= 0;
#endif
        }
        catch (OperationCanceledException)
        {
            return false; // timed out
        }
        catch (IOException)
        {
            return false; // dehydrated/unavailable/networked file not ready
        }
        catch (UnauthorizedAccessException)
        {
            // Permission issues = not available for our purposes
            return false;
        }
    }


    /// <summary>
    ///     Checks if a file exists and is genuinely available.
    /// </summary>
    /// <remarks>
    ///     Cloud storage can report a file as available here even if it's not.
    /// </remarks>
    /// <returns></returns>
    public async Task<bool> CheckFileAvailability(string filePath)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return false;
        }

        // First check if file exists at all
        if (!File.Exists(filePath))
        {
            return false;
        }

        // Check if file is available using cross-platform helper
        var isAvailable = await IsAvailableAsync(filePath);

        // If file exists but is not available, show error dialog (cloud storage scenario)
        if (!isAvailable)
        {
            _logService.Log(LogLevel.Error, $"File {filePath} is unavailable.");

            // Show warning so user knows their file isn't lost and is just on cloud storage
            var result = await Ioc.Default.GetRequiredService<Windowing>().ShowContentDialog(new ContentDialog
            {
                Title = "File unavailable.",
                Content = """
                          The story outline you are trying to open is stored on a cloud service, and isn't available currently.
                          Try going online and syncing the file, then try again.

                          Click show help article for more information.
                          """,
                PrimaryButtonText = "Show help article",
                SecondaryButtonText = "Close"
            }, true);

            // Open help article in default browser
            if (result == ContentDialogResult.Primary)
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName =
                        "https://storybuilder-org.github.io/StoryCAD/docs/Miscellaneous" +
                        "/Troubleshooting_Cloud_Storage_Providers.html",
                    UseShellExecute = true
                });
            }
        }

        return isAvailable;
    }


    /// <summary>
    ///     Takes a path and checks if it's valid
    /// </summary>
    /// <param name="path">File path</param>
    /// <returns>True if path is valid, false otherwise</returns>
    public static bool IsValidPath(string path)
    {
        //Checks file name validity
        try
        {
            var dir = Path.GetDirectoryName(path);
            var file = Path.GetFileName(path);
            // Try creating directory (will throw if invalid)
            Directory.CreateDirectory(dir ?? "");

            //Check filename for invalid chars.
            var invalidChars = Path.GetInvalidFileNameChars();
            foreach (var c in file)
            {
                if (Array.Exists(invalidChars, invalidChar => invalidChar == c))
                {
                    //Checks file name validity
                    return false;
                }
            }
        }
        catch (UnauthorizedAccessException) //Invalid access
        {
            return false;
        }
        catch //Different IO error
        {
            return false;
        }

        //File is valid
        return true;
    }
}
