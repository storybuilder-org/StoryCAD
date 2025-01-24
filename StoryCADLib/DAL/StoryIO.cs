using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using Windows.Storage;
using StoryCAD.Services;

namespace StoryCAD.DAL;

/// <summary>
/// Handles Reading and Writing of Story Data
/// </summary>
public class StoryIO
{
	private LogService _logService = Ioc.Default.GetRequiredService<LogService>();

	/// <summary>
	/// Writes the current Story to the disk
	/// </summary>
	public async Task WriteStory(StorageFile output, StoryModel model)
	{
		_logService.Log(LogLevel.Info, $"Saving Model to disk as {output.Path}  " + 
				$"Elements: {model.StoryElements.StoryElementGuids.Count}");

		//Save version data
		model.LastVersion = Ioc.Default.GetRequiredService<AppState>().Version;
		_logService.Log(LogLevel.Info, $"Saving version as {model.LastVersion}");

		//Flatten trees (solves issues when deserialization)
		model.FlattenedExplorerView = FlattenTree(model.ExplorerView);
		model.FlattenedNarratorView = FlattenTree(model.NarratorView);

		//Serialise
		string JSON = JsonSerializer.Serialize(model, new JsonSerializerOptions
		{
			WriteIndented = true,
			Converters =
			{
				new EmptyGuidConverter(),
				new StoryElementConverter(),
				new JsonStringEnumConverter()
			}
		});
		_logService.Log(LogLevel.Info, $"Serialised as {JSON}");

		//Save file to disk
		await FileIO.WriteTextAsync(output, JSON);
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
			_model.ProjectFile = StoryFile;
			_model.ProjectFilename = StoryFile.Name;
			_model.ProjectFolder = await StoryFile.GetParentAsync();
			_model.ProjectPath = _model.ProjectFolder.Path;
			_model.ProjectFilename = Path.GetFileName(StoryFile.Path);

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


	/// <summary>
	/// Used to prepare tree for serialisation
	/// </summary>
	/// <param name="rootNodes"></param>
	/// <returns></returns>
	private static List<PersistableNode> FlattenTree(ObservableCollection<StoryNodeItem> rootNodes)
	{
		var list = new List<PersistableNode>();
		foreach (var root in rootNodes)
		{
			AddNodeRecursively(root, list);
		}
		return list;
	}

	private static void AddNodeRecursively(StoryNodeItem node, List<PersistableNode> list)
	{
		list.Add(new PersistableNode
		{
			Uuid = node.Uuid,
			ParentUuid = node.Parent?.Uuid
		});

		foreach (var child in node.Children)
		{
			AddNodeRecursively(child, list);
		}
	}

	private static ObservableCollection<StoryNodeItem> RebuildTree(
		List<PersistableNode> flatNodes,
		StoryElementCollection storyElements,
		ILogService logger)
	{
		var lookup = new Dictionary<Guid, StoryNodeItem>();

		// First create all nodes (empty parent/children)
		foreach (var n in flatNodes)
		{
			var element = storyElements.StoryElementGuids[n.Uuid];
			var nodeItem = new StoryNodeItem(logger, element, parent: null);
			lookup[n.Uuid] = nodeItem;
		}

		// Now link each node to its parent
		var rootCollection = new ObservableCollection<StoryNodeItem>();
		foreach (var n in flatNodes)
		{
			var nodeItem = lookup[n.Uuid];
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
	/// <param name="File">StorageFile Object</param>
	/// <returns>StoryModel</returns>
	public async Task<StoryModel> MigrateModel(StorageFile File)
	{
		StoryModel Old = new();
		try
		{
			//Read Legacy file
			_logService.Log(LogLevel.Info, $"Migrating Old STBX File from {File.Path}");
			LegacyXMLReader reader = new(_logService);
			Old = await reader.ReadFile(File);

			//Copy legacy file
			_logService.Log(LogLevel.Info, "Read legacy file");
			StorageFolder Folder = await StorageFolder.GetFolderFromPathAsync(
				Ioc.Default.GetRequiredService<PreferenceService>().Model.BackupDirectory);
			await File.CopyAsync(Folder, File.Name + $" as of {DateTime.Now.ToString().Replace('/', ' ')
				.Replace(':', ' ').Replace(".stbx", "")}.old");
			_logService.Log(LogLevel.Info, $"Copied legacy file to backup folder ({Folder.Path})");

			//File is now backed up, now migrate to new format
			await WriteStory(File, Old);
			_logService.Log(LogLevel.Info, "Updated legacy file to JSON File");
			return Old;
		}
		catch (Exception ex)
		{
			_logService.LogException(LogLevel.Error, ex, $"Failed to migrate file {File.Path}");
		}

		return await Task.FromResult(Old);
	}
}