using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using Windows.Storage;
using StoryCAD.Services;
using System.Diagnostics;
using System.Linq;

namespace StoryCAD.DAL;

/// <summary>
/// Handles Reading and Writing of Story Data
/// </summary>
public class StoryIO
{
	private readonly ILogService _logService;
	private readonly AppState _appState;

	public StoryIO(ILogService logService, AppState appState)
	{
		_logService = logService;
		_appState = appState;
	}

    /// <summary>
    /// Writes the current Story to the disk
    /// </summary>
    public async Task WriteStory(string output_path, StoryModel model)
    {
        string parent = Path.GetDirectoryName(output_path);
        Directory.CreateDirectory(parent);

        StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(parent);
        var output = await folder.CreateFileAsync(Path.GetFileName(output_path), CreationCollisionOption.ReplaceExisting);
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
			string JSON = await FileIO.ReadTextAsync(StoryFile);

			//Check if file is legacy
			if (JSON.Split("\n")[0].Contains("<?xml version=\"1.0\" encoding=\"utf-8\"?>"))
			{
				_logService.Log(LogLevel.Info, "File is legacy XML format");

                // Show dialog informing user about legacy format
                // TODO: Circular dependency - StoryIO ↔ OutlineViewModel prevents injecting Windowing
                var result = await Ioc.Default.GetRequiredService<Windowing>().ShowContentDialog(new()
				{
					Title = "Legacy Outline format detected!",
					Content ="""
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
						FileName = "https://github.com/storybuilder-org/StoryCAD-Legacy-STBX-Conversion-Tool/releases/tag/1.0.0",
						UseShellExecute = true
					});
				}
				
				return new StoryModel(); // Return empty model
			}

			_logService.Log(LogLevel.Info, $"Read file (Length: {JSON.Length})");

			//Deserialize into real story model
			_logService.Log(LogLevel.Trace, $"File read as {JSON}");
			StoryModel _model = JsonSerializer.Deserialize<StoryModel>(JSON, new JsonSerializerOptions
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

		return new();
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
			StoryElement element = storyElements.StoryElementGuids[n.Uuid];
			StoryNodeItem nodeItem = new(element, parent: null);
            element.Node = nodeItem;
			lookup[n.Uuid] = nodeItem;
		}

		// Now link each node to its parent
		ObservableCollection<StoryNodeItem> rootCollection = new();
		foreach (var n in flatNodes)
		{
			StoryNodeItem nodeItem = lookup[n.Uuid];
			if (n.ParentUuid.HasValue && lookup.TryGetValue(n.ParentUuid.Value, out var parentItem))
			{
				nodeItem.Parent = parentItem;
				parentItem.Children.Add(nodeItem);
                nodeItem.IsRoot = false;
            }
            else  // No parent => it's a root node
			{
				nodeItem.IsRoot = true;
				nodeItem.IsExpanded = true;
				rootCollection.Add(nodeItem);
			}
		}

		return rootCollection;
	}


	/// <summary>
	/// Checks if a file exists and is genuinely available.
	/// </summary>
	/// <remarks>
	///	Cloud storage can report a file as available here even if it's not.
	/// </remarks>
	/// <returns></returns>
    public async Task<bool> CheckFileAvailability(string filePath)
    {
        //TODO: investigate alternatives on other platforms.
        #if HAS_UNO
        _logService.Log(LogLevel.Warn, $"Checking file availability is not supported on non-windows platforms");
        return true;
        #endif
        
        if (!File.Exists(filePath))
        {
            return false;
        }

        var fileAttributes = File.GetAttributes(filePath);
        StorageFile file = await StorageFile.GetFileFromPathAsync(filePath);
        bool showOfflineError = false;
        if (file.IsAvailable) // Microsoft thinks the file is accessible
        {
            try
            {
                //Test read file before continuing.
                //If the file is saved on a cloud provider and not locally this should force
                //the file to be pulled locally if possible. A file could be unavailable for
                //many reasons, such as the user being offline, server outage, etc.
                //Windows might pause StoryCAD while it syncs the file.
                File.ReadAllText(file.Path);
            }
            catch (IOException) { showOfflineError = true; }
        }
        else { showOfflineError = true; }

        //Failed to access network file
        if (showOfflineError && (fileAttributes & System.IO.FileAttributes.Offline) == 0)
        {
            //The file is actually inaccessible and microsoft is wrong.
            _logService.Log(LogLevel.Error, $"File {file.Path} is unavailable.");

            //Show warning so user knows their file isn't lost and is just on onedrive.
            // TODO: Circular dependency - StoryIO ↔ OutlineViewModel prevents injecting Windowing
            var result = await Ioc.Default.GetRequiredService<Windowing>().ShowContentDialog(new()
            {
                Title = "File unavailable.",
                Content = """
                          The story outline you are trying to open is stored on a cloud service, and isn't available currently.
                          Try going online and syncing the file, then try again. 

                          Click show help article for more information.
                          """,
                PrimaryButtonText = "Show help article",
                SecondaryButtonText = "Close",
            }, true);

            //Open help article in default browser
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

        return !showOfflineError;
    }


    /// <summary>
    /// Takes a path and checks if it's valid
    /// </summary>
    /// <param name="path">File path</param>
    /// <returns>True if path is valid, false otherwise</returns>
    public static bool IsValidPath(string path)
    {
        // TODO: Static method requires Ioc.Default until refactored to instance method or removed logging
        ILogService logger = Ioc.Default.GetRequiredService<ILogService>();
        //Checks file name validity
        try
        {
            string dir = Path.GetDirectoryName(path);
            string file = Path.GetFileName(path);
            // Try creating directory (will throw if invalid)
            Directory.CreateDirectory(dir ?? "");

            //Check filename for invalid chars.
            char[] invalidChars = Path.GetInvalidFileNameChars();
            foreach (char c in file)
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
            logger.Log(LogLevel.Warn, "User can't access this path");
            return false;
        }
        catch //Different IO error
        {
            logger.Log(LogLevel.Warn, "Invalid file name");
            return false;
        }

        //File is valid
        return true;
    }
}
