using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using Windows.Storage;
using StoryCAD.Services;
using System.Diagnostics;
using StoryCAD.ViewModels.SubViewModels;

namespace StoryCAD.DAL;

/// <summary>
/// Handles Reading and Writing of Story Data
/// </summary>
public class StoryIO
{
	private LogService _logService = Ioc.Default.GetRequiredService<LogService>();
    private OutlineViewModel OultineVM = Ioc.Default.GetRequiredService<OutlineViewModel>();

    /// <summary>
    /// Writes the current Story to the disk
    /// </summary>
    public async Task WriteStory(string output_path, StoryModel model)
    {
		StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(Path.GetDirectoryName(output_path));
        var output = await folder.CreateFileAsync(Path.GetFileName(output_path), CreationCollisionOption.OpenIfExists);
		_logService.Log(LogLevel.Info, $"Saving Model to disk as {output_path}  " + 
				$"Elements: {model.StoryElements.StoryElementGuids.Count}");

		//Save version data
		model.LastVersion = Ioc.Default.GetRequiredService<AppState>().Version;
		_logService.Log(LogLevel.Info, $"Saving version as {model.LastVersion}");

        var json = model.Serialize();
        _logService.Log(LogLevel.Info, $"Serialised as {json}");

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
				return await MigrateModel(StoryFile);
			}

			_logService.Log(LogLevel.Info, $"Read file (Length: {JSON.Length})");

			//Deserialize into real story model
			_logService.Log(LogLevel.Debug, $"File read as {JSON}");
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

			//Log info about story
			_logService.Log(LogLevel.Info, $"Model deserialized as {_model.ExplorerView[0].Name}");
			_logService.Log(LogLevel.Info, $"Model contains as {_model.StoryElements.Count}" +
			                               $" (Explorer: {_model.ExplorerView.Count}/Narrator{_model.NarratorView.Count})");
			_logService.Log(LogLevel.Info, $"Version created with {_model.FirstVersion ?? "Pre-JSON"}");
			_logService.Log(LogLevel.Info, $"Version last saved with {_model.LastVersion ?? "Error"}");

			//Update file information
            OultineVM.StoryModelFile = StoryFile.Path;
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

		// First create all nodes (empty parent/children)
		foreach (var n in flatNodes)
		{
			StoryElement element = storyElements.StoryElementGuids[n.Uuid];
			StoryNodeItem nodeItem = new(logger, element, parent: null);
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
	/// Migrates a given StoryModel from XML to JSON
	/// </summary>
	/// <param name="file">StorageFile Object</param>
	/// <returns>StoryModel</returns>
	public async Task<StoryModel>  MigrateModel(StorageFile file)
	{
		StoryModel old = new();
		try
		{
			//Check file exists first.
            if (!await CheckFileAvailability(file.Path))
            {
				_logService.Log(LogLevel.Warn,"File is unavailable or doesn't exist.");
                return new();
            }

            //Read Legacy file
            _logService.Log(LogLevel.Info, $"Migrating Old STBX File from {file.Path}");
			LegacyXMLReader reader = new(_logService);
			old = await reader.ReadFile(file);

			//Copy legacy file
			_logService.Log(LogLevel.Info, "Read legacy file");
			StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(
				Ioc.Default.GetRequiredService<PreferenceService>().Model.BackupDirectory);

            _logService.Log(LogLevel.Info, $"Got folder object at path {folder.Path}");

            string name = file.Name + $" as of {DateTime.Now.ToString().Replace('/', ' ')
                .Replace(':', ' ').Replace(".stbx", "")}.old";
            _logService.Log(LogLevel.Info, $"Got backup name as {name}");

            await file.CopyAsync(folder, name);
            _logService.Log(LogLevel.Info, $"Copied legacy file to backup folder ({folder.Path})");
            
			//File is now backed up, now migrate to new format
			await WriteStory(file.Path, old);
			_logService.Log(LogLevel.Info, "Updated legacy file to JSON File");
			return old;
		}
		catch (Exception ex)
		{
			_logService.LogException(LogLevel.Error, ex, $"Failed to migrate file {file.Path}");
		}

		return await Task.FromResult(old);
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

}