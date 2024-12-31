﻿using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using Windows.Storage;
using Org.BouncyCastle.X509;

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
		model.FlattenedNarratorView = FlattenTree(model.ExplorerView);

		//Serialise
		string JSON = JsonSerializer.Serialize(model, new JsonSerializerOptions
		{
			WriteIndented = true,
			ReferenceHandler = ReferenceHandler.Preserve,
			Converters =
			{
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
		//Read file
		_logService.Log(LogLevel.Info, $"Reading Model from disk ({StoryFile.Path})");
		string JSON = await FileIO.ReadTextAsync(StoryFile);
		_logService.Log(LogLevel.Info, $"Read file (Length: {JSON.Length})");

		//Deserialize into real story model
		_logService.Log(LogLevel.Info, $"File read as {JSON}");
		StoryModel _model = JsonSerializer.Deserialize<StoryModel>(JSON, new JsonSerializerOptions
		{
			ReferenceHandler = ReferenceHandler.Preserve,
			PropertyNameCaseInsensitive = true
		});

		//Log info about story
		_logService.Log(LogLevel.Info, $"Model deserialized as {_model.ExplorerView[0].Name}");
		_logService.Log(LogLevel.Info, $"Model contains as {_model.StoryElements.Count}" +
				$" (Explorer: {_model.ExplorerView.Count}/Narrator{_model.NarratorView.Count})");
		_logService.Log(LogLevel.Info, $"Version created with {_model.FirstVersion ?? "Pre-JSON"}");
		_logService.Log(LogLevel.Info, $"Version last saved with {_model.LastVersion ?? "Error"}");

		//Rebuild tree.
		_model.ExplorerView = RebuildTree(_model.FlattenedExplorerView, _model.StoryElements, _logService);
		_model.NarratorView = RebuildTree(_model.FlattenedNarratorView, _model.StoryElements, _logService);

		//Update file information
		_model.ProjectFile = StoryFile;
		_model.ProjectFilename = StoryFile.Name;
		_model.ProjectFolder = await StoryFile.GetParentAsync();
		_model.ProjectPath = _model.ProjectFolder.Path;
		_model.ProjectFilename = Path.GetFileName(StoryFile.Path);

		return _model;
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

	public static ObservableCollection<StoryNodeItem> RebuildTree(
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
			else
			{
				// No parent => it's a root node
				rootCollection.Add(nodeItem);
			}
		}

		return rootCollection;
	}


}