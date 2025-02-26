using System.ComponentModel;
using System.Text.Json;
using Windows.Storage;
using StoryCAD.Services.Outline;
using Microsoft.SemanticKernel;

namespace StoryCAD.Services.API;

/// <summary>
/// 
/// </summary>
public class SemanticKernelApi
{
    private readonly OutlineService _outlineService = Ioc.Default.GetRequiredService<OutlineService>();
    private StoryModel? CurrentModel;
    public SemanticKernelApi()
    {
        //_outlineService = outlineService;
    }

    /// <summary>
    /// Creates a new empty story outline based on a template.
    /// Parameters:
    ///   filePath - full path to the file that will back the outline
    ///   name - the name to use for the outline's Overview element
    ///   author - the author name for the overview
    ///   templateIndex - index (as a string) specifying the template to use
    /// Returns a JSON-serialized OperationResult payload containing a list of the StoryElement Guids.
    /// </summary>
    [KernelFunction, Description("Creates a new empty story outline from a template.")]
    public async Task<OperationResult<List<Guid>>> CreateEmptyOutline(string filePath, string name, string author, string templateIndex)
    {
        Console.WriteLine($"creating empty outline at {filePath}");
        var response = new OperationResult<List<Guid>>();
        if (!int.TryParse(templateIndex, out int idx))
        {
            response.IsSuccess = false;
            response.ErrorMessage = $"'{templateIndex}' is not a valid template index.";
            return response;
        }

        try
        {
            // Get the StorageFile from the provided file path.
            string path = Path.GetDirectoryName(filePath);
            StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(path);
            string filename = Path.GetFileName(filePath);
            StorageFile file = await folder.CreateFileAsync(filename, CreationCollisionOption.FailIfExists);

            // Create a new StoryModel using the OutlineService.
            var result = await OperationResult<StoryModel>.SafeExecuteAsync(_outlineService.CreateModel(file, name, author, idx));
            //var result = 
            if (!result.IsSuccess)
            {
                response.IsSuccess = false;
                response.ErrorMessage = result.ErrorMessage;
                return response;
            }

            StoryModel model = result.Payload;

            // Option 1: Return the entire StoryModel as JSON:
            // response.Payload = JsonSerializer.Serialize(model);

            // Option 2: Return just a list of the StoryElement Guids.
            List<Guid> elementGuids = model.StoryElements.Select(e => e.Uuid).ToList();

            response.IsSuccess = true;
            response.Payload = elementGuids;
            CurrentModel = model;
        }
        catch (Exception ex)
        {
            response.IsSuccess = false;
            response.ErrorMessage = $"Error in CreateEmptyOutline: {ex.Message}";
        }

        return response;
    }

    /// <summary>
    /// Writes an updated story outline to disk.
    /// Parameters:
    ///   jsonModel - a JSON string representing the StoryModel to be saved
    ///   filePath - full path to the file where the model should be saved
    /// Returns an OperationResult indicating IsSuccess or error.
    /// </summary>
    [KernelFunction, Description("Writes the story outline to the backing store.")]
    public async Task<OperationResult<string>> WriteOutline(string filePath)
    {
        var response = new OperationResult<string>();

        try
        {
            // Deserialize the JSON into a StoryModel object.
            if (CurrentModel == null)
            {
                response.IsSuccess = false;
                response.ErrorMessage = "Deserialized StoryModel is null.";
                return response;
            }

            // Write the model to disk using the OutlineService.
            await _outlineService.WriteModel(CurrentModel, filePath);

            response.IsSuccess = true;
            response.Payload = "Outline written IsSuccessfully.";
        }
        catch (Exception ex)
        {
            response.IsSuccess = false;
            response.ErrorMessage = $"Error in WriteOutline: {ex.Message}";
        }

        return response;
    }
}