using System.Reflection;
using System.Resources;
using System.Text.Json;
using LogLevel = StoryCADLib.Services.Logging.LogLevel;

namespace StoryCADLib.DAL;

/// <summary>
/// Updated resource loader for embedded resources now they are all JSON Files and don't need wierd XML handling.
/// </summary>
public class JSONResourceLoader(ILogService logger)
{
    /// <summary>
    /// This is the manifest path where the embedded resources are located.
    /// </summary>
    private const string ResourcePath = "StoryCADLib.Assets.Install.";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<T> LoadResource<T>(string resourceName) where T : class
    {
        logger.Log(LogLevel.Info, $"Loading resource of type {typeof(T).Name} from {resourceName}");
        
        var fullPath = ResourcePath + resourceName;
        await using var stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream(fullPath);
        
        if (stream is null)
        {
            logger.Log(LogLevel.Error, $"{resourceName} resource not found!");
            throw new MissingManifestResourceException(fullPath);
        }

        var result = await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions)
                     ?? throw new JsonException($"Failed to deserialize {resourceName} to {typeof(T).Name}");
        
        logger.Log(LogLevel.Info, $"Deserialized {resourceName} successfully.");
        return result;
    }
    
}
