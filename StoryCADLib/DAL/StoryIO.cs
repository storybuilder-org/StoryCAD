using System.Text.Json;
using Windows.Storage;

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
	/// <returns></returns>
	public async Task WriteStory(StorageFile output, StoryModel model)
	{
		_logService.Log(LogLevel.Info, $"Saving Model to disk as {output.Path}  " +
		                               $"Elements: {model.StoryElements.StoryElementGuids.Count}");

		//Save version data
		model.LastVersion = Ioc.Default.GetRequiredService<AppState>().Version;
		_logService.Log(LogLevel.Info, $"Saving version as {model.LastVersion}");

		//Serialise
		string JSON = JsonSerializer.Serialize(model, new JsonSerializerOptions { WriteIndented = true });
		_logService.Log(LogLevel.Info, $"Serialised as {JSON}");

		//Save file to disk
		await FileIO.WriteTextAsync(output, JSON);
	}
}