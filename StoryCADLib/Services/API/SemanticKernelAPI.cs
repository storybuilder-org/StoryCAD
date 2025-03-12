using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using Windows.Storage;
using StoryCAD.Services.Outline;
using Microsoft.SemanticKernel;
using StoryCAD.DAL;

namespace StoryCAD.Services.API;

/// <summary>
/// 
/// </summary>
public class SemanticKernelApi
{
    private readonly OutlineService _outlineService = Ioc.Default.GetRequiredService<OutlineService>();
    private StoryModel? CurrentModel;

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
    public async Task<OperationResult<List<Guid>>> CreateEmptyOutline(string name, string author, string templateIndex)
    {
        var response = new OperationResult<List<Guid>>();
        if (!int.TryParse(templateIndex, out int idx))
        {
            response.IsSuccess = false;
            response.ErrorMessage = $"'{templateIndex}' is not a valid template index.";
            return response;
        }

        try
        {
            // Create a new StoryModel using the OutlineService.
            var result = await OperationResult<StoryModel>.SafeExecuteAsync(_outlineService.CreateModel(name, author, idx));
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
    [KernelFunction, Description("Writes the story outline to the backing store, outline files must have the .stbx extension.")]
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


    [KernelFunction, Description("Returns basic information about all elements in the story model.")]
    public ObservableCollection<StoryElement> GetAllElements()
    {
        if (CurrentModel == null)
        {
            throw new InvalidOperationException("No StoryModel available. Create a model first.");
        }

        return CurrentModel.StoryElements;
    }

    /// <summary>
    /// Deletes a story element from the current StoryModel.
    /// </summary>
    /// <remarks>Element is just moved to trashcan</remarks>
    /// <param name="uuid"></param>
    public void DeleteStoryElement(string uuid)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Updates a story model element.
    /// </summary>
    /// <param name="NewElement">Element source</param>
    /// <param name="uuid">UUID of element that will be updated.</param>
    [KernelFunction, Description("""
                                 Updates a given story element within the outline, call get element first
                                 Guidelines:
                                  - Dont modify the fields, just thier content.
                                  - The type you are replacing must be the same type.
                                  - You cannot modify the trashcan with this element.
                                  - You cannot modify the parent or children of this element using this method.
                                  - You cannot move the element position in the tree using this method.
                                  - You must provide the GUID in payload and separately as a parameter and should be the same value.
                                  - You cannot create elements using this method.
                                  - You must return all fields previously given to you for that element previously
                                 """)]
    public void UpdateStoryElement(object NewElement, Guid guid)
    {
        if (CurrentModel == null)
        {
            throw new InvalidOperationException("No StoryModel available. Create a model first.");
        }

        if (guid == null)
        {
            throw new ArgumentNullException("GUID is null");
        }

        if (!CurrentModel.StoryElements.StoryElementGuids.ContainsKey(guid))
        {
            throw new ArgumentNullException("StoryElement does not exist");
        }

        //Deserialise and update.
        StoryElement updated = StoryElement.Deserialize(NewElement.ToString());
        //TODO: force set uuid somehow.
        updated.Uuid = guid;
        CurrentModel.StoryElements.StoryElementGuids[guid] = updated;
    }

    [KernelFunction, Description("Returns a single element and all its fields.")]
    public object GetElement(Guid guid)
    {
        if (CurrentModel == null)
        {
            throw new InvalidOperationException("No StoryModel available. Create a model first.");
        }

        if (guid == null)
        {
            throw new ArgumentNullException("GUID is null");
        }

        return CurrentModel.StoryElements.StoryElementGuids[guid].Serialize();
    }


    [KernelFunction, Description("""
                                 Adds a new StoryElement to the current StoryModel. 
                                 This function returns a object representing the story element that was added. 
                                 Some included fields are GUIDs. These are the GUIDs of other StoryElements, and are how one story element links to another.
                                 Examples include 'Protagonist' and 'Antagonist' in a ProblemModel, which link to CharacterModels, and 'Setting' and 'Cast' in a Scene,
                                 which link to a SettingModel and CharacterModel elements, respectively.
                                 
                                 Dont ADD NEW FIELDS TO THE PAYLOAD.
                                 Dont add GUIDs that are not within the current storymodel.
                                 The following Types are supported: Problem, Character, Setting, Scene, Folder, Section, Web, Notes.
                                 Dont add Folders to the the Narrator View or children of it, add Sections instead or vice versa.
                                 Dont add any elements to the TrashCan or any type beside Section to the narrator view.
                                 Dont add sections to the narrator view unless explictly asked to, always add to the Overview element 
                                 unless told otherwise.
                                 """)]
    public OperationResult<object> AddElement(StoryItemType typeToAdd, string parentGUID)
    {
        if (CurrentModel == null)
        {
            return OperationResult<object>.Failure("No StoryModel available. Create a model first.");
        }

        if (!Guid.TryParse(parentGUID, out var parentGuid))
        {
            return OperationResult<object>.Failure($"Invalid parent GUID: {parentGUID}");
        }

        // Attempt to locate the parent element using the provided GUID.
        var parent = CurrentModel.StoryElements.FirstOrDefault(e => e.Uuid == parentGuid);
        if (parent == null)
        {
            return OperationResult<object>.Failure("Parent not found.");
        }

        try
        {
            // Create the new element using the OutlineService.
            var newElement = _outlineService.AddStoryElement(CurrentModel, typeToAdd, parent.Node);
            return OperationResult<object>.Success(newElement);
        }
        catch (Exception ex)
        {
            return OperationResult<object>.Failure($"Error in AddElement: {ex.Message}");
        }
    }


}