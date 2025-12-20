using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json.Serialization;
using Microsoft.SemanticKernel;
using StoryCADLib.Models.Tools;
using StoryCADLib.Services.Collaborator.Contracts;
using StoryCADLib.Services.Outline;
using StoryCADLib.ViewModels;
using StoryCADLib.ViewModels.Tools;

namespace StoryCADLib.Services.API;

///     This class is designed for integration with Semantic Kernel, if you are writing
///     something that's not AI, you probably want to use OutlineService directly.
/// 
///     For detailed documentation on using the StoryCAD API, please refer to:
///     https://storybuilder-org.github.io/StoryCAD/docs/For%20Developers/Using_the_API.html
/// 
///     Usage:
///     - State Handling: The API operates on a CurrentModel property which holds the active StoryModel instance.
///     This model must be set before most operations can be performed, either via SetCurrentModel() or by
///     creating a new outline with CreateEmptyOutline().
///     - Calling Standard: All public API methods return OperationResult<T> to ensure safe external consumption.
///         No exceptions are thrown to external callers; all errors are communicated through the OperationResult
///         pattern with IsSuccess flags and descriptive ErrorMessage strings.
public class SemanticKernelApi(OutlineService outlineService, ListData listData, ControlData controlData, ToolsData toolsData) : IStoryCADAPI
{
    public StoryModel CurrentModel { get; set; }

    /// <summary>
    ///     Creates a new empty story outline based on a template.
    ///     Parameters:
    ///     filePath - full path to the file that will back the outline
    ///     name - the name to use for the outline's Overview element
    ///     author - the author name for the overview
    ///     templateIndex - index (as a string) specifying the template to use
    ///     Returns a JSON-serialized OperationResult payload containing a list of the StoryElement Guids.
    /// </summary>
    [KernelFunction]
    [Description("Creates a new empty story outline from a template.")]
    public async Task<OperationResult<List<Guid>>> CreateEmptyOutline(string name, string author, string templateIndex)
    {
        var response = new OperationResult<List<Guid>>();
        if (!int.TryParse(templateIndex, out var idx))
        {
            response.IsSuccess = false;
            response.ErrorMessage = $"'{templateIndex}' is not a valid template index.";
            return response;
        }

        try
        {
            // Create a new StoryModel using the OutlineService.
            var result =
                await OperationResult<StoryModel>.SafeExecuteAsync(outlineService.CreateModel(name, author, idx));
            //var result = 
            if (!result.IsSuccess || result.Payload == null)
            {
                response.IsSuccess = false;
                response.ErrorMessage = result.ErrorMessage;
                return response;
            }

            // Set model.
            CurrentModel = result.Payload;
            var elementGuids = CurrentModel.StoryElements.Select(e => e.Uuid).ToList();

            response.IsSuccess = true;
            response.Payload = elementGuids;
        }
        catch (Exception ex)
        {
            response.IsSuccess = false;
            response.ErrorMessage = $"Error in CreateEmptyOutline: {ex.Message}";
        }

        return response;
    }

    /// <summary>
    ///     Writes an updated story outline to disk.
    ///     Parameters:
    ///     jsonModel - a JSON string representing the StoryModel to be saved
    ///     filePath - full path to the file where the model should be saved
    ///     Returns an OperationResult indicating IsSuccess or error.
    /// </summary>
    [KernelFunction]
    [Description("Writes the story outline to the backing store, outline files must have the .stbx extension.")]
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
            await outlineService.WriteModel(CurrentModel, filePath);

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


    [KernelFunction]
    [Description("Returns basic information about all elements in the story model.")]
    public OperationResult<ObservableCollection<StoryElement>> GetAllElements()
    {
        if (CurrentModel == null)
        {
            return OperationResult<ObservableCollection<StoryElement>>.Failure(
                "No StoryModel available. Create a model first.");
        }

        try
        {
            return OperationResult<ObservableCollection<StoryElement>>.Success(CurrentModel.StoryElements);
        }
        catch (Exception ex)
        {
            return OperationResult<ObservableCollection<StoryElement>>.Failure(
                $"Error retrieving elements: {ex.Message}");
        }
    }

    /// <summary>
    ///     Updates a story model element.
    /// </summary>
    /// <param name="newElement">Element source</param>
    /// <param name="guid">UUID of element that will be updated.</param>
    [KernelFunction]
    [Description(
        """
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
    public OperationResult<bool> UpdateStoryElement(object newElement, Guid guid)
    {
        if (CurrentModel == null)
        {
            return OperationResult<bool>.Failure("No StoryModel available. Create a model first.");
        }

        if (guid == Guid.Empty)
        {
            return OperationResult<bool>.Failure("GUID cannot be empty");
        }

        try
        {
            //Deserialize and update.
            var updated = StoryElement.Deserialize(newElement.ToString());
            updated.Uuid = guid;
            outlineService.UpdateStoryElement(CurrentModel, updated);

            return OperationResult<bool>.Success(true);
        }
        catch (InvalidOperationException)
        {
            return OperationResult<bool>.Failure("StoryElement does not exist");
        }
        catch (Exception ex)
        {
            return OperationResult<bool>.Failure($"Error updating element: {ex.Message}");
        }
    }

    /// <summary>
    ///     Updates specified properties for a story element identified by its GUID.
    ///     A dictionary is used to specify the properties to update, where keys are
    ///     property names and values are the new values.
    ///     Iterates over each property in the dictionary and calls UpdateElementProperty.
    /// </summary>
    /// <param name="elementGuid">The GUID of the story element to update.</param>
    /// <param name="properties">A Dictionary where keys are property names and values are the new values.</param>
    /// <returns>void if all properties are updated successfully, exception otherwise.</returns>
    [KernelFunction]
    [Description(
        """
        Updates multiple properties for a given story element within the outline.
        This function is a wrapper for UpdateElementProperty and iterates over each 
        property in a dictionary of properties to update.
        Guidelines:
        - You must provide the StoryElement GUID as a parameter. 
        - All properties will be updated for the same StoryElement.
        - UpdateElementProperty edits each properties of the StoryElement.
        """)]
    public OperationResult<bool> UpdateElementProperties(Guid elementGuid, Dictionary<string, object> properties)
    {
        if (CurrentModel == null)
        {
            return OperationResult<bool>.Failure("No StoryModel available. Create a model first.");
        }

        if (elementGuid == Guid.Empty)
        {
            return OperationResult<bool>.Failure("GUID cannot be empty");
        }

        if (properties == null || properties.Count == 0)
        {
            return OperationResult<bool>.Failure("No properties to update");
        }

        try
        {
            foreach (var kvp in properties)
            {
                var result = UpdateElementProperty(elementGuid, kvp.Key, kvp.Value);
                if (!result.IsSuccess)
                {
                    return OperationResult<bool>.Failure(
                        $"Failed to update property '{kvp.Key}': {result.ErrorMessage}");
                }
            }

            return OperationResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return OperationResult<bool>.Failure($"Error updating properties: {ex.Message}");
        }
    }

    /// <summary>
    ///     Implementation of IStoryCADAPI.GetStoryElement.
    ///     This is an alias for GetStoryElementByGuid.
    /// </summary>
    public OperationResult<StoryElement> GetStoryElement(Guid guid)
    {
        if (CurrentModel == null)
        {
            return OperationResult<StoryElement>.Failure("No StoryModel available. Create a model first.");
        }

        if (guid == Guid.Empty)
        {
            return OperationResult<StoryElement>.Failure("GUID cannot be empty");
        }

        try
        {
            var element = outlineService.GetStoryElementByGuid(CurrentModel, guid);
            return OperationResult<StoryElement>.Success(element);
        }
        catch (InvalidOperationException)
        {
            return OperationResult<StoryElement>.Failure("Element not found");
        }
        catch (Exception ex)
        {
            return OperationResult<StoryElement>.Failure($"Error retrieving element: {ex.Message}");
        }
    }

    /// <summary>
    ///     Updates the specified property of a StoryElement identified by its UUID.
    ///     Only properties decorated with [JsonInclude] are allowed to be updated.
    ///     The function uses reflection to locate the property, verify its [JsonInclude] attribute,
    ///     and update its value (performing a type conversion if necessary).
    ///     If the property is not found, is read-only, or is missing the attribute, an error is returned.
    /// </summary>
    [KernelFunction]
    [Description(
        """
        Updates the specified property on a StoryElement (identified by its UUID).
        Only properties decorated with [JsonInclude] are updatable. If the property is missing the attribute
        or if any error occurs (such as a type conversion issue), the operation will fails.
        You should use AddCastMember and AddRelationship for updating those fields when updating those fields.
        """)]
    /// <summary>
    /// Implementation of IStoryCADAPI.UpdateElementProperty with compatible return type
    /// </summary>
    OperationResult<object> IStoryCADAPI.UpdateElementProperty(Guid elementUuid, string propertyName, object value)
    {
        var result = UpdateElementProperty(elementUuid, propertyName, value);
        return new OperationResult<object>
        {
            IsSuccess = result.IsSuccess,
            Payload = result.Payload,
            ErrorMessage = result.ErrorMessage
        };
    }

    /// <summary>
    ///     Sets the current StoryModel to work with (for Collaborator integration)
    ///     An open outline in StoryCAD is represented by a StoryModel and ShellViewModel
    ///     contains the active StoryModel as CurrentModel, which is passed to Collaborator when opening
    ///     a session. This method allows Collaborator to set the API's current StoryModel.
    /// </summary>
    /// <param name="model">The active StoryModel from ShellViewModel</param>
    public void SetCurrentModel(StoryModel model)
    {
        CurrentModel = model;
    }

    /// <summary>
    ///     Deletes a story element from the current StoryModel.
    /// </summary>
    /// <remarks>Element is just moved to trashcan</remarks>
    /// <param name="uuid"></param>
    public OperationResult<bool> DeleteStoryElement(string uuid)
    {
        if (CurrentModel == null)
        {
            return OperationResult<bool>.Failure("No StoryModel available. Create a model first.");
        }

        if (!Guid.TryParse(uuid, out var guid))
        {
            return OperationResult<bool>.Failure($"Invalid UUID: {uuid}");
        }

        try
        {
            var element = outlineService.GetStoryElementByGuid(CurrentModel, guid);
            outlineService.MoveToTrash(element, CurrentModel);
            return OperationResult<bool>.Success(true);
        }
        catch (InvalidOperationException ex)
        {
            return OperationResult<bool>.Failure($"Element not found: {ex.Message}");
        }
        catch (Exception ex)
        {
            return OperationResult<bool>.Failure($"Error deleting element: {ex.Message}");
        }
    }

    [KernelFunction]
    [Description("Returns a single element and all its fields.")]
    public OperationResult<object> GetElement(Guid guid)
    {
        if (CurrentModel == null)
        {
            return OperationResult<object>.Failure("No StoryModel available. Create a model first.");
        }

        if (guid == Guid.Empty)
        {
            return OperationResult<object>.Failure("GUID cannot be empty");
        }

        try
        {
            var element = outlineService.GetStoryElementByGuid(CurrentModel, guid);
            return OperationResult<object>.Success(element.Serialize());
        }
        catch (InvalidOperationException)
        {
            return OperationResult<object>.Failure("Element not found");
        }
        catch (Exception ex)
        {
            return OperationResult<object>.Failure($"Error retrieving element: {ex.Message}");
        }
    }

    [KernelFunction]
    [Description("""
                 Adds a new StoryElement to the current StoryModel. 
                 This function returns the Guid of the story element that was added. 
                 Some included fields are GUIDs. These are the GUIDs of other StoryElements, and are how one story element links to another.
                 Examples include 'Protagonist' and 'Antagonist' in a ProblemModel, which link to CharacterModels, and 'Setting' and 'Cast' in a Scene,
                 which link to a SettingModel and CharacterModel elements, respectively.
                 You may specify the GUID of the element should you wish, but it must be unique.

                 Dont ADD NEW FIELDS TO THE PAYLOAD.
                 Dont add GUIDs that are not within the current storymodel.
                 The following Types are supported: Problem, Character, Setting, Scene, Folder, Section, Web, Notes.
                 Dont add Folders to the the Narrator View or children of it, add Sections instead or vice versa.
                 Dont add any elements to the TrashCan or any type beside Section to the narrator view.
                 Dont add sections to the narrator view unless explictly asked to, always add to the Overview element 
                 unless told otherwise.
                 """)]
    public OperationResult<Guid> AddElement(StoryItemType typeToAdd, string parentGUID, string name,
        string GUIDOverride = "")
    {
        if (CurrentModel == null)
        {
            return OperationResult<Guid>.Failure("No StoryModel available. Create a model first.");
        }

        if (!Guid.TryParse(parentGUID, out var parentGuid))
        {
            return OperationResult<Guid>.Failure($"Invalid parent GUID: {parentGUID}");
        }

        var DesiredGuid = Guid.Empty;
        if (!string.IsNullOrEmpty(GUIDOverride))
        {
            if (!Guid.TryParse(GUIDOverride, out DesiredGuid))
            {
                return OperationResult<Guid>.Failure($"Invalid guid GUID: {parentGUID}");
            }

            try
            {
                outlineService.GetStoryElementByGuid(CurrentModel, DesiredGuid);
                return OperationResult<Guid>.Failure("GUID Override already exists.");
            }
            catch (InvalidOperationException)
            {
                // GUID doesn't exist, which is what we want
            }
        }

        // Attempt to locate the parent element using the provided GUID.
        var parent = CurrentModel.StoryElements.FirstOrDefault(e => e.Uuid == parentGuid);
        if (parent == null)
        {
            return OperationResult<Guid>.Failure("Parent not found.");
        }

        try
        {
            // Create the new element using the OutlineService.
            var newElement = outlineService.AddStoryElement(CurrentModel, typeToAdd, parent.Node);
            if (DesiredGuid != Guid.Empty)
            {
                newElement.UpdateGuid(CurrentModel, DesiredGuid);
            }

            newElement.Name = name;

            return OperationResult<Guid>.Success(newElement.Uuid);
        }
        catch (Exception ex)
        {
            return OperationResult<Guid>.Failure($"Error in AddElement: {ex.Message}");
        }
    }


    [KernelFunction]
    [Description("""
                 Adds a new StoryElement to the current StoryModel and sets some properties.
                 This function returns a objectthe Guid of the element that was added.
                 Some parameters in the properties Dictionary may be GUIDs. These are the GUIDs of other StoryElements, 
                 and are how one story element links to another.
                 Examples include 'Protagonist' and 'Antagonist' in a ProblemModel, which link to CharacterModels, and 'Setting' and 'Cast' in a Scene,
                 which link to a SettingModel and CharacterModel elements, respectively.
                 You may specify the GUID of the element should you wish, but it must be unique.

                 Dont ADD NEW FIELDS TO THE PAYLOAD. 
                 Dont add GUIDs that are not within the current storymodel.
                 The following Types are supported: Problem, Character, Setting, Scene, Folder, Section, Web, Notes.
                 Dont add Folders to the the Narrator View or children of it, add Sections instead or vice versa.
                 Dont add any elements to the TrashCan or any type beside Section to the narrator view.
                 Dont add sections to the narrator view unless explictly asked to, always add to the Overview element 
                 unless told otherwise.
                 """)]
    public OperationResult<Guid> AddElement(StoryItemType typeToAdd, string parentGUID, string name,
        Dictionary<string, object> properties, string GUIDOverride = "")
    {
        if (CurrentModel == null)
        {
            return OperationResult<Guid>.Failure("No StoryModel available. Create a model first.");
        }

        if (!Guid.TryParse(parentGUID, out var parentGuid))
        {
            return OperationResult<Guid>.Failure($"Invalid parent GUID: {parentGUID}");
        }

        var DesiredGuid = Guid.Empty;
        if (!string.IsNullOrEmpty(GUIDOverride))
        {
            if (!Guid.TryParse(GUIDOverride, out DesiredGuid))
            {
                return OperationResult<Guid>.Failure($"Invalid guid GUID: {parentGUID}");
            }

            try
            {
                outlineService.GetStoryElementByGuid(CurrentModel, DesiredGuid);
                return OperationResult<Guid>.Failure("GUID Override already exists.");
            }
            catch (InvalidOperationException)
            {
                // GUID doesn't exist, which is what we want
            }
        }

        // Attempt to locate the parent element using the provided GUID.
        var parent = CurrentModel.StoryElements.FirstOrDefault(e => e.Uuid == parentGuid);
        if (parent == null)
        {
            return OperationResult<Guid>.Failure("Parent not found.");
        }

        try
        {
            // Create the new element using the OutlineService.
            var newElement = outlineService.AddStoryElement(CurrentModel, typeToAdd, parent.Node);

            if (DesiredGuid != Guid.Empty)
            {
                newElement.UpdateGuid(CurrentModel, DesiredGuid);
            }

            newElement.Name = name;

            newElement.Name = name;
            UpdateElementProperties(newElement.Uuid, properties);

            return OperationResult<Guid>.Success(newElement.Uuid);
        }
        catch (Exception ex)
        {
            return OperationResult<Guid>.Failure($"Error in AddElement: {ex.Message}");
        }
    }

    public OperationResult<StoryElement> UpdateElementProperty(Guid elementUuid, string propertyName, object value)
    {
        // Ensure we have a current StoryModel.
        if (CurrentModel == null)
        {
            return OperationResult<StoryElement>.Failure("No StoryModel available. Create a model first.");
        }

        // Find the StoryElement in the current model.
        var element = CurrentModel.StoryElements.FirstOrDefault(e => e.Uuid == elementUuid);
        if (element == null)
        {
            return OperationResult<StoryElement>.Failure("StoryElement not found.");
        }

        try
        {
            // Get the property info by name.
            var property = element.GetType().GetProperty(propertyName);

            if (property == null)
            {
                throw new ArgumentException(
                    $"Property '{propertyName}' not found on type {element.GetType().FullName}.");
            }

            // Ensure the property has the [JsonInclude] attribute.
            if (!Attribute.IsDefined(property, typeof(JsonIncludeAttribute)))
            {
                throw new InvalidOperationException(
                    $"Property '{propertyName}' does not have the JsonInclude attribute.");
            }

            // Ensure the property is writable.
            if (!property.CanWrite)
            {
                throw new InvalidOperationException($"Property '{propertyName}' is read-only.");
            }


            if (property.PropertyType == typeof(Guid) && typeof(string) == value.GetType())
            {
                value = Guid.Parse(value.ToString());
            }
            else
            {
                // Convert the value to the property's type if needed.
                if (value != null && !property.PropertyType.IsAssignableFrom(value.GetType()))
                {
                    value = Convert.ChangeType(value, property.PropertyType);
                }
            }

            // Update the property value.
            property.SetValue(element, value);

            return OperationResult<StoryElement>.Success(element);
        }
        catch (Exception ex)
        {
            return OperationResult<StoryElement>.Failure($"Error updating property: {ex.Message}");
        }
    }

    /// <summary>
    ///     Opens new outline from file.
    /// </summary>
    /// <param name="path">Outline to open</param>
    [KernelFunction]
    [Description("Opens an outline from a file.")]
    public async Task<OperationResult<bool>> OpenOutline(string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path) | !Path.Exists(path))
            {
                throw new ArgumentException("Invalid path");
            }

            CurrentModel = await outlineService.OpenFile(path);
            return OperationResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return OperationResult<bool>.Failure($"Error in OpenOutline: {ex.Message}");
        }
    }

    [KernelFunction]
    [Description("""
                 Move Element to the trashcan, this is a destructive action.
                 Do not call this unless the user wants you to delete the element.
                 You cannot delete the overview, narrator view, or trashcan elements.
                 Elements that are deleted will appear under the trashcan element.
                 Type is a enum that specifies if you are deleting from explorer or narrator
                 view.
                 """)]
    public Task<OperationResult<bool>> DeleteElement(Guid elementToDelete)
    {
        try
        {
            // Ensure we have a current StoryModel.
            if (CurrentModel == null)
            {
                return Task.FromResult(OperationResult<bool>.Failure("No outline is opened"));
            }

            // Get the element and move it to trash using OutlineService
            var element = outlineService.GetStoryElementByGuid(CurrentModel, elementToDelete);
            outlineService.MoveToTrash(element, CurrentModel);

            return Task.FromResult(OperationResult<bool>.Success(true));
        }
        catch (Exception ex)
        {
            return Task.FromResult(OperationResult<bool>.Failure($"Error in DeleteElement: {ex.Message}"));
        }
    }

    [KernelFunction]
    [Description("""
                 Adds a new cast member to a scene.
                 Scene MUST be a GUID of element that is a scene
                 Character MUST be a GUID of an element that is a character.
                 """)]
    public OperationResult<bool> AddCastMember(Guid scene, Guid character)
    {
        try
        {
            if (CurrentModel == null)
            {
                return OperationResult<bool>.Failure("No outline is opened");
            }

            var element = outlineService.GetStoryElementByGuid(CurrentModel, scene);
            outlineService.AddCastMember(CurrentModel, element, character);

            return OperationResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return OperationResult<bool>.Failure($"Error in AddCastMember: {ex.Message}");
        }
    }


    [KernelFunction]
    [Description("""
                 Adds a relationship between characters.
                 Both Source and Recipient must be GUIDs of elements that are characters.
                 Description is the relationship between the two characters.
                 mirror is a boolean that specifies if the relationship
                 should be created on both characters.
                 """)]
    public OperationResult<bool> AddRelationship(Guid source, Guid recipient, string desc, bool mirror = false)
    {
        if (CurrentModel == null)
        {
            return OperationResult<bool>.Failure("No StoryModel available. Create a model first.");
        }

        if (source == Guid.Empty)
        {
            return OperationResult<bool>.Failure("Source GUID cannot be empty");
        }

        if (recipient == Guid.Empty)
        {
            return OperationResult<bool>.Failure("Recipient GUID cannot be empty");
        }

        try
        {
            outlineService.AddRelationship(CurrentModel, source, recipient, desc, mirror);
            return OperationResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return OperationResult<bool>.Failure($"Error adding relationship: {ex.Message}");
        }
    }

    /// <summary>
    ///     Searches for story elements containing the specified text.
    /// </summary>
    /// <param name="searchText">The text to search for (case-insensitive)</param>
    /// <returns>A list of element GUIDs and names that contain the search text</returns>
    [KernelFunction]
    [Description("""
                 Searches for story elements containing the specified text.
                 The search is case-insensitive and searches through all text fields
                 of story elements including names, descriptions, and other properties.
                 Returns a list of matching elements with their GUIDs and names.
                 """)]
    public OperationResult<List<Dictionary<string, object>>> SearchForText(string searchText)
    {
        try
        {
            if (CurrentModel == null)
            {
                return OperationResult<List<Dictionary<string, object>>>.Failure("No outline is opened");
            }

            if (string.IsNullOrWhiteSpace(searchText))
            {
                return OperationResult<List<Dictionary<string, object>>>.Failure("Search text cannot be empty");
            }

            var results = outlineService.SearchForText(CurrentModel, searchText);

            var formattedResults = results.Select(element => new Dictionary<string, object>
            {
                { "Guid", element.Uuid },
                { "Name", element.Name },
                { "Type", element.ElementType.ToString() }
            }).ToList();

            return OperationResult<List<Dictionary<string, object>>>.Success(formattedResults);
        }
        catch (Exception ex)
        {
            return OperationResult<List<Dictionary<string, object>>>.Failure($"Error in SearchForText: {ex.Message}");
        }
    }

    /// <summary>
    ///     Searches for story elements that reference a specific UUID.
    /// </summary>
    /// <param name="targetUuid">The UUID to search for references to</param>
    /// <returns>A list of elements that reference the specified UUID</returns>
    [KernelFunction]
    [Description("""
                 Searches for story elements that reference a specific UUID.
                 This is useful for finding all elements that link to a particular
                 character, setting, or other story element.
                 Returns a list of elements that contain references to the UUID.
                 """)]
    public OperationResult<List<Dictionary<string, object>>> SearchForReferences(Guid targetUuid)
    {
        try
        {
            if (CurrentModel == null)
            {
                return OperationResult<List<Dictionary<string, object>>>.Failure("No outline is opened");
            }

            if (targetUuid == Guid.Empty)
            {
                return OperationResult<List<Dictionary<string, object>>>.Failure("Target UUID cannot be empty");
            }

            var results = outlineService.SearchForUuidReferences(CurrentModel, targetUuid);

            var formattedResults = results.Select(element => new Dictionary<string, object>
            {
                { "Guid", element.Uuid },
                { "Name", element.Name },
                { "Type", element.ElementType.ToString() }
            }).ToList();

            return OperationResult<List<Dictionary<string, object>>>.Success(formattedResults);
        }
        catch (Exception ex)
        {
            return OperationResult<List<Dictionary<string, object>>>.Failure(
                $"Error in SearchForReferences: {ex.Message}");
        }
    }

    /// <summary>
    ///     Removes all references to a specified UUID from the story model.
    /// </summary>
    /// <param name="targetUuid">The UUID to remove references to</param>
    /// <returns>The number of elements that had references removed</returns>
    [KernelFunction]
    [Description("""
                 Removes all references to a specified UUID from the story model.
                 This is useful when deleting an element and cleaning up all references
                 to it from other elements. Use with caution as this modifies multiple
                 elements in the story model.
                 Returns the count of elements that were modified.
                 """)]
    public OperationResult<int> RemoveReferences(Guid targetUuid)
    {
        try
        {
            if (CurrentModel == null)
            {
                return OperationResult<int>.Failure("No outline is opened");
            }

            if (targetUuid == Guid.Empty)
            {
                return OperationResult<int>.Failure("Target UUID cannot be empty");
            }

            var affectedCount = outlineService.RemoveUuidReferences(CurrentModel, targetUuid);

            return OperationResult<int>.Success(affectedCount);
        }
        catch (Exception ex)
        {
            return OperationResult<int>.Failure($"Error in RemoveReferences: {ex.Message}");
        }
    }

    /// <summary>
    ///     Searches for story elements within a specific subtree.
    /// </summary>
    /// <param name="rootNodeGuid">The GUID of the root node to search from</param>
    /// <param name="searchText">The text to search for (case-insensitive)</param>
    /// <returns>A list of elements in the subtree that contain the search text</returns>
    [KernelFunction]
    [Description("""
                 Searches for story elements within a specific subtree.
                 This allows searching within a specific folder or section
                 rather than the entire story model.
                 Provide the GUID of the root node and the search text.
                 Returns matching elements within that subtree only.
                 """)]
    public OperationResult<List<Dictionary<string, object>>> SearchInSubtree(Guid rootNodeGuid, string searchText)
    {
        try
        {
            if (CurrentModel == null)
            {
                return OperationResult<List<Dictionary<string, object>>>.Failure("No outline is opened");
            }

            if (rootNodeGuid == Guid.Empty)
            {
                return OperationResult<List<Dictionary<string, object>>>.Failure("Root node GUID cannot be empty");
            }

            if (string.IsNullOrWhiteSpace(searchText))
            {
                return OperationResult<List<Dictionary<string, object>>>.Failure("Search text cannot be empty");
            }

            // Find the root node
            var rootElement = outlineService.GetStoryElementByGuid(CurrentModel, rootNodeGuid);
            if (rootElement == null)
            {
                return OperationResult<List<Dictionary<string, object>>>.Failure("Root node not found");
            }

            var results = outlineService.SearchInSubtree(CurrentModel, rootElement.Node, searchText);

            var formattedResults = results.Select(element => new Dictionary<string, object>
            {
                { "Guid", element.Uuid },
                { "Name", element.Name },
                { "Type", element.ElementType.ToString() }
            }).ToList();

            return OperationResult<List<Dictionary<string, object>>>.Success(formattedResults);
        }
        catch (Exception ex)
        {
            return OperationResult<List<Dictionary<string, object>>>.Failure($"Error in SearchInSubtree: {ex.Message}");
        }
    }

    /// <summary>
    ///     Restores a story element from the trash back to its original location.
    /// </summary>
    /// <param name="elementToRestore">The GUID of the element to restore from trash</param>
    /// <returns>An OperationResult indicating success or failure</returns>
    [KernelFunction]
    [Description("""
                 Restores an element from the trashcan back to the explorer view.
                 The element must be a direct child of the trashcan (not nested).
                 All children of the element will be restored with it.
                 """)]
    public Task<OperationResult<bool>> RestoreFromTrash(Guid elementToRestore)
    {
        try
        {
            // Ensure we have a current StoryModel.
            if (CurrentModel == null)
            {
                return Task.FromResult(OperationResult<bool>.Failure("No outline is opened"));
            }

            // Get the trash node
            var trashNode = CurrentModel.TrashView.FirstOrDefault(n => n.Type == StoryItemType.TrashCan);
            if (trashNode == null)
            {
                return Task.FromResult(OperationResult<bool>.Failure("TrashCan node not found"));
            }

            // Find the node to restore
            var nodeToRestore = trashNode.Children.FirstOrDefault(n => n.Uuid == elementToRestore);
            if (nodeToRestore == null)
            {
                return Task.FromResult(OperationResult<bool>.Failure("Element not found in trash"));
            }

            // Use OutlineService to restore the element
            outlineService.RestoreFromTrash(nodeToRestore, CurrentModel);

            return Task.FromResult(OperationResult<bool>.Success(true));
        }
        catch (Exception ex)
        {
            return Task.FromResult(OperationResult<bool>.Failure($"Error in RestoreFromTrash: {ex.Message}"));
        }
    }

    /// <summary>
    ///     Empties the trash, permanently removing all items.
    /// </summary>
    /// <returns>An OperationResult indicating success or failure</returns>
    [KernelFunction]
    [Description("""
                 Empties the trashcan, permanently deleting all items within it.
                 This is a destructive action that cannot be undone.
                 All elements and their children will be permanently removed.
                 """)]
    public Task<OperationResult<bool>> EmptyTrash()
    {
        try
        {
            // Ensure we have a current StoryModel.
            if (CurrentModel == null)
            {
                return Task.FromResult(OperationResult<bool>.Failure("No outline is opened"));
            }

            // Use OutlineService to empty the trash
            outlineService.EmptyTrash(CurrentModel);

            return Task.FromResult(OperationResult<bool>.Success(true));
        }
        catch (Exception ex)
        {
            return Task.FromResult(OperationResult<bool>.Failure($"Error in EmptyTrash: {ex.Message}"));
        }
    }

    #region Resource API (Issue #1223)

    /// <summary>
    /// Gets examples for a property from Lists.json
    /// </summary>
    /// <param name="propertyName">Property name matching a list key</param>
    /// <returns>Result containing the list values, or error if key not found</returns>
    [KernelFunction]
    [Description("Gets valid example values for a story element property from Lists.json")]
    public OperationResult<IEnumerable<string>> GetExamples(string propertyName)
    {
        if (listData.ListControlSource.TryGetValue(propertyName, out var list))
            return OperationResult<IEnumerable<string>>.Success(list);

        return OperationResult<IEnumerable<string>>.Failure($"No list found for property '{propertyName}'");
    }

    /// <summary>
    /// Gets all conflict categories from Controls.json
    /// </summary>
    /// <returns>Result containing the list of conflict categories</returns>
    [KernelFunction]
    [Description("Gets all available conflict categories for character development")]
    public OperationResult<IEnumerable<string>> GetConflictCategories()
    {
        return OperationResult<IEnumerable<string>>.Success(controlData.ConflictTypes.Keys);
    }

    /// <summary>
    /// Gets subcategories for a conflict category
    /// </summary>
    /// <param name="category">The conflict category name</param>
    /// <returns>Result containing the list of subcategories, or error if category not found</returns>
    [KernelFunction]
    [Description("Gets subcategories for a specific conflict category")]
    public OperationResult<IEnumerable<string>> GetConflictSubcategories(string category)
    {
        if (controlData.ConflictTypes.TryGetValue(category, out var model))
            return OperationResult<IEnumerable<string>>.Success(model.SubCategories);

        return OperationResult<IEnumerable<string>>.Failure($"No conflict category '{category}' found");
    }

    /// <summary>
    /// Gets examples for a conflict category and subcategory
    /// </summary>
    /// <param name="category">The conflict category name</param>
    /// <param name="subcategory">The subcategory name within the category</param>
    /// <returns>Result containing the list of examples, or error if not found</returns>
    [KernelFunction]
    [Description("Gets example conflicts for a specific category and subcategory")]
    public OperationResult<IEnumerable<string>> GetConflictExamples(string category, string subcategory)
    {
        if (!controlData.ConflictTypes.TryGetValue(category, out var model))
            return OperationResult<IEnumerable<string>>.Failure($"No conflict category '{category}' found");

        if (!model.Examples.TryGetValue(subcategory, out var examples))
            return OperationResult<IEnumerable<string>>.Failure($"No subcategory '{subcategory}' in category '{category}'");

        return OperationResult<IEnumerable<string>>.Success(examples);
    }

    /// <summary>
    /// Applies a conflict description to a Problem's protagonist conflict field
    /// </summary>
    /// <param name="problemGuid">The GUID of the Problem element</param>
    /// <param name="conflictText">The conflict description text</param>
    /// <returns>Result indicating success or failure</returns>
    [KernelFunction]
    [Description("Applies a conflict description to a Problem's protagonist conflict. Use after selecting from GetConflictExamples or with custom text.")]
    public OperationResult<bool> ApplyConflictToProtagonist(Guid problemGuid, string conflictText)
    {
        var elementResult = GetStoryElement(problemGuid);
        if (!elementResult.IsSuccess)
            return OperationResult<bool>.Failure(elementResult.ErrorMessage);

        if (elementResult.Payload is not ProblemModel problem)
            return OperationResult<bool>.Failure($"Element {problemGuid} is not a Problem");

        problem.ProtConflict = conflictText;
        return OperationResult<bool>.Success(true);
    }

    /// <summary>
    /// Applies a conflict description to a Problem's antagonist conflict field
    /// </summary>
    /// <param name="problemGuid">The GUID of the Problem element</param>
    /// <param name="conflictText">The conflict description text</param>
    /// <returns>Result indicating success or failure</returns>
    [KernelFunction]
    [Description("Applies a conflict description to a Problem's antagonist conflict. Use after selecting from GetConflictExamples or with custom text.")]
    public OperationResult<bool> ApplyConflictToAntagonist(Guid problemGuid, string conflictText)
    {
        var elementResult = GetStoryElement(problemGuid);
        if (!elementResult.IsSuccess)
            return OperationResult<bool>.Failure(elementResult.ErrorMessage);

        if (elementResult.Payload is not ProblemModel problem)
            return OperationResult<bool>.Failure($"Element {problemGuid} is not a Problem");

        problem.AntagConflict = conflictText;
        return OperationResult<bool>.Success(true);
    }

    /// <summary>
    /// Gets all element types that have key questions available
    /// </summary>
    /// <returns>Result containing the list of element types</returns>
    [KernelFunction]
    [Description("Gets all element types that have key questions available (e.g., Character, Problem, Scene)")]
    public OperationResult<IEnumerable<string>> GetKeyQuestionElements()
    {
        return OperationResult<IEnumerable<string>>.Success(toolsData.KeyQuestionsSource.Keys);
    }

    /// <summary>
    /// Gets key questions for an element type
    /// </summary>
    /// <param name="elementType">The element type (e.g., Character, Problem, Scene)</param>
    /// <returns>Result containing tuples of (Topic, Question), or error if element type not found</returns>
    [KernelFunction]
    [Description("Gets key questions for an element type. Returns tuples where Item1 is the topic (aspect being addressed) and Item2 is the question text.")]
    public OperationResult<IEnumerable<(string Topic, string Question)>> GetKeyQuestions(string elementType)
    {
        if (!toolsData.KeyQuestionsSource.TryGetValue(elementType, out var questions))
            return OperationResult<IEnumerable<(string Topic, string Question)>>.Failure($"No key questions for element type '{elementType}'");

        return OperationResult<IEnumerable<(string Topic, string Question)>>.Success(
            questions.Select(q => (q.Topic, q.Question)));
    }

    /// <summary>
    /// Gets all master plot names
    /// </summary>
    /// <returns>Result containing the list of master plot names</returns>
    [KernelFunction]
    [Description("Gets all available master plot names (e.g., Quest, Revenge, Pursuit)")]
    public OperationResult<IEnumerable<string>> GetMasterPlotNames()
    {
        return OperationResult<IEnumerable<string>>.Success(
            toolsData.MasterPlotsSource.Select(p => p.PlotPatternName));
    }

    /// <summary>
    /// Gets notes for a master plot
    /// </summary>
    /// <param name="plotName">The master plot name</param>
    /// <returns>Result containing the plot notes, or error if plot not found</returns>
    [KernelFunction]
    [Description("Gets the descriptive notes for a specific master plot")]
    public OperationResult<string> GetMasterPlotNotes(string plotName)
    {
        var plot = toolsData.MasterPlotsSource.FirstOrDefault(p => p.PlotPatternName == plotName);
        if (plot == null)
            return OperationResult<string>.Failure($"No master plot '{plotName}' found");

        return OperationResult<string>.Success(plot.PlotPatternNotes);
    }

    /// <summary>
    /// Gets the scene breakdown for a master plot
    /// </summary>
    /// <param name="plotName">The master plot name</param>
    /// <returns>Result containing tuples of (SceneTitle, Notes), or error if plot not found</returns>
    [KernelFunction]
    [Description("Gets the scene breakdown for a master plot. Returns tuples where Item1 is the scene title and Item2 is the scene notes.")]
    public OperationResult<IEnumerable<(string SceneTitle, string Notes)>> GetMasterPlotScenes(string plotName)
    {
        var plot = toolsData.MasterPlotsSource.FirstOrDefault(p => p.PlotPatternName == plotName);
        if (plot == null)
            return OperationResult<IEnumerable<(string SceneTitle, string Notes)>>.Failure($"No master plot '{plotName}' found");

        return OperationResult<IEnumerable<(string SceneTitle, string Notes)>>.Success(
            plot.PlotPatternScenes.Select(s => (s.SceneTitle, s.Notes)));
    }

    /// <summary>
    /// Gets all stock scene categories
    /// </summary>
    /// <returns>Result containing the list of category names</returns>
    [KernelFunction]
    [Description("Gets all available stock scene categories")]
    public OperationResult<IEnumerable<string>> GetStockSceneCategories()
    {
        return OperationResult<IEnumerable<string>>.Success(toolsData.StockScenesSource.Keys);
    }

    /// <summary>
    /// Gets stock scenes for a category
    /// </summary>
    /// <param name="category">The stock scene category name</param>
    /// <returns>Result containing the list of scenes, or error if category not found</returns>
    [KernelFunction]
    [Description("Gets stock scenes for a specific category")]
    public OperationResult<IEnumerable<string>> GetStockScenes(string category)
    {
        if (toolsData.StockScenesSource.TryGetValue(category, out var scenes))
            return OperationResult<IEnumerable<string>>.Success(scenes);

        return OperationResult<IEnumerable<string>>.Failure($"No stock scene category '{category}' found");
    }

    // ===== Beat Sheets API =====
    // Note: To find Scenes/Problems for beat assignment, use GetAllElements() or SearchForText()
    // and filter by ElementType (Scene or Problem). A convenience method may be added in the future.

    /// <summary>
    /// Gets all beat sheet template names
    /// </summary>
    [KernelFunction]
    [Description("Gets all available beat sheet template names")]
    public OperationResult<IEnumerable<string>> GetBeatSheetNames()
    {
        return OperationResult<IEnumerable<string>>.Success(
            toolsData.BeatSheetSource.Select(b => b.PlotPatternName));
    }

    /// <summary>
    /// Gets a beat sheet template by name
    /// </summary>
    [KernelFunction]
    [Description("Gets a beat sheet template by name. Returns the description and all beats.")]
    public OperationResult<(string Description, IEnumerable<(string BeatName, string BeatNotes)> Beats)> GetBeatSheet(string beatSheetName)
    {
        var beatSheet = toolsData.BeatSheetSource.FirstOrDefault(b => b.PlotPatternName == beatSheetName);
        if (beatSheet == null)
            return OperationResult<(string, IEnumerable<(string, string)>)>.Failure($"No beat sheet '{beatSheetName}' found");

        var beats = beatSheet.PlotPatternScenes.Select(s => (s.SceneTitle, s.Notes));
        return OperationResult<(string, IEnumerable<(string, string)>)>.Success((beatSheet.PlotPatternNotes, beats));
    }

    /// <summary>
    /// Applies a beat sheet template to a Problem's structure
    /// </summary>
    [KernelFunction]
    [Description("Applies a beat sheet template to a Problem element. This sets up the structure with beats from the template.")]
    public OperationResult<bool> ApplyBeatSheetToProblem(Guid problemGuid, string beatSheetName)
    {
        if (CurrentModel == null)
            return OperationResult<bool>.Failure("No StoryModel available");

        var element = CurrentModel.StoryElements.FirstOrDefault(e => e.Uuid == problemGuid);
        if (element == null || element.ElementType != StoryItemType.Problem)
            return OperationResult<bool>.Failure($"Problem with GUID '{problemGuid}' not found");

        var beatSheet = toolsData.BeatSheetSource.FirstOrDefault(b => b.PlotPatternName == beatSheetName);
        if (beatSheet == null)
            return OperationResult<bool>.Failure($"No beat sheet '{beatSheetName}' found");

        var problem = (ProblemModel)element;
        problem.StructureTitle = beatSheetName;
        problem.StructureDescription = beatSheet.PlotPatternNotes;
        problem.StructureBeats.Clear();

        foreach (var scene in beatSheet.PlotPatternScenes)
        {
            problem.StructureBeats.Add(new StructureBeatViewModel(scene.SceneTitle, scene.Notes));
        }

        return OperationResult<bool>.Success(true);
    }

    /// <summary>
    /// Gets the current structure of a Problem
    /// </summary>
    [KernelFunction]
    [Description("Gets the current beat sheet structure of a Problem, including title, description, and all beats with their assignments.")]
    public OperationResult<(string Title, string Description, IEnumerable<(string BeatTitle, string BeatDescription, Guid? LinkedElement)> Beats)> GetProblemStructure(Guid problemGuid)
    {
        if (CurrentModel == null)
            return OperationResult<(string, string, IEnumerable<(string, string, Guid?)>)>.Failure("No StoryModel available");

        var element = CurrentModel.StoryElements.FirstOrDefault(e => e.Uuid == problemGuid);
        if (element == null || element.ElementType != StoryItemType.Problem)
            return OperationResult<(string, string, IEnumerable<(string, string, Guid?)>)>.Failure($"Problem with GUID '{problemGuid}' not found");

        var problem = (ProblemModel)element;
        var beats = problem.StructureBeats.Select(b => (
            b.Title,
            b.Description,
            b.Guid == Guid.Empty ? (Guid?)null : b.Guid
        ));

        return OperationResult<(string, string, IEnumerable<(string, string, Guid?)>)>.Success(
            (problem.StructureTitle ?? "", problem.StructureDescription ?? "", beats));
    }

    /// <summary>
    /// Assigns a Scene or Problem element to a beat
    /// </summary>
    [KernelFunction]
    [Description("Assigns a Scene or Problem element to a specific beat in the Problem's structure.")]
    public OperationResult<bool> AssignElementToBeat(Guid problemGuid, int beatIndex, Guid elementGuid)
    {
        if (CurrentModel == null)
            return OperationResult<bool>.Failure("No StoryModel available");

        var element = CurrentModel.StoryElements.FirstOrDefault(e => e.Uuid == problemGuid);
        if (element == null || element.ElementType != StoryItemType.Problem)
            return OperationResult<bool>.Failure($"Problem with GUID '{problemGuid}' not found");

        var problem = (ProblemModel)element;
        if (beatIndex < 0 || beatIndex >= problem.StructureBeats.Count)
            return OperationResult<bool>.Failure($"Beat index {beatIndex} is out of range");

        // Verify the element to assign exists and is a Scene or Problem
        var targetElement = CurrentModel.StoryElements.FirstOrDefault(e => e.Uuid == elementGuid);
        if (targetElement == null)
            return OperationResult<bool>.Failure($"Element with GUID '{elementGuid}' not found");
        if (targetElement.ElementType != StoryItemType.Scene && targetElement.ElementType != StoryItemType.Problem)
            return OperationResult<bool>.Failure("Only Scene or Problem elements can be assigned to beats");

        problem.StructureBeats[beatIndex].Guid = elementGuid;
        return OperationResult<bool>.Success(true);
    }

    /// <summary>
    /// Clears the element assignment from a beat
    /// </summary>
    [KernelFunction]
    [Description("Clears the element assignment from a specific beat in the Problem's structure.")]
    public OperationResult<bool> ClearBeatAssignment(Guid problemGuid, int beatIndex)
    {
        if (CurrentModel == null)
            return OperationResult<bool>.Failure("No StoryModel available");

        var element = CurrentModel.StoryElements.FirstOrDefault(e => e.Uuid == problemGuid);
        if (element == null || element.ElementType != StoryItemType.Problem)
            return OperationResult<bool>.Failure($"Problem with GUID '{problemGuid}' not found");

        var problem = (ProblemModel)element;
        if (beatIndex < 0 || beatIndex >= problem.StructureBeats.Count)
            return OperationResult<bool>.Failure($"Beat index {beatIndex} is out of range");

        problem.StructureBeats[beatIndex].Guid = Guid.Empty;
        return OperationResult<bool>.Success(true);
    }

    /// <summary>
    /// Creates a new beat in a Problem's structure
    /// </summary>
    [KernelFunction]
    [Description("Creates a new beat at the end of a Problem's structure.")]
    public OperationResult<bool> CreateBeat(Guid problemGuid, string title, string description)
    {
        if (CurrentModel == null)
            return OperationResult<bool>.Failure("No StoryModel available");

        var element = CurrentModel.StoryElements.FirstOrDefault(e => e.Uuid == problemGuid);
        if (element == null || element.ElementType != StoryItemType.Problem)
            return OperationResult<bool>.Failure($"Problem with GUID '{problemGuid}' not found");

        var problem = (ProblemModel)element;
        problem.StructureBeats.Add(new StructureBeatViewModel(title, description));
        return OperationResult<bool>.Success(true);
    }

    /// <summary>
    /// Updates a beat's title and description
    /// </summary>
    [KernelFunction]
    [Description("Updates a beat's title and description in the Problem's structure.")]
    public OperationResult<bool> UpdateBeat(Guid problemGuid, int beatIndex, string title, string description)
    {
        if (CurrentModel == null)
            return OperationResult<bool>.Failure("No StoryModel available");

        var element = CurrentModel.StoryElements.FirstOrDefault(e => e.Uuid == problemGuid);
        if (element == null || element.ElementType != StoryItemType.Problem)
            return OperationResult<bool>.Failure($"Problem with GUID '{problemGuid}' not found");

        var problem = (ProblemModel)element;
        if (beatIndex < 0 || beatIndex >= problem.StructureBeats.Count)
            return OperationResult<bool>.Failure($"Beat index {beatIndex} is out of range");

        problem.StructureBeats[beatIndex].Title = title;
        problem.StructureBeats[beatIndex].Description = description;
        return OperationResult<bool>.Success(true);
    }

    /// <summary>
    /// Deletes a beat from a Problem's structure
    /// </summary>
    [KernelFunction]
    [Description("Deletes a beat from the Problem's structure.")]
    public OperationResult<bool> DeleteBeat(Guid problemGuid, int beatIndex)
    {
        if (CurrentModel == null)
            return OperationResult<bool>.Failure("No StoryModel available");

        var element = CurrentModel.StoryElements.FirstOrDefault(e => e.Uuid == problemGuid);
        if (element == null || element.ElementType != StoryItemType.Problem)
            return OperationResult<bool>.Failure($"Problem with GUID '{problemGuid}' not found");

        var problem = (ProblemModel)element;
        if (beatIndex < 0 || beatIndex >= problem.StructureBeats.Count)
            return OperationResult<bool>.Failure($"Beat index {beatIndex} is out of range");

        problem.StructureBeats.RemoveAt(beatIndex);
        return OperationResult<bool>.Success(true);
    }

    /// <summary>
    /// Moves a beat from one position to another
    /// </summary>
    [KernelFunction]
    [Description("Moves a beat from one position to another in the Problem's structure.")]
    public OperationResult<bool> MoveBeat(Guid problemGuid, int fromIndex, int toIndex)
    {
        if (CurrentModel == null)
            return OperationResult<bool>.Failure("No StoryModel available");

        var element = CurrentModel.StoryElements.FirstOrDefault(e => e.Uuid == problemGuid);
        if (element == null || element.ElementType != StoryItemType.Problem)
            return OperationResult<bool>.Failure($"Problem with GUID '{problemGuid}' not found");

        var problem = (ProblemModel)element;
        if (fromIndex < 0 || fromIndex >= problem.StructureBeats.Count)
            return OperationResult<bool>.Failure($"From index {fromIndex} is out of range");
        if (toIndex < 0 || toIndex >= problem.StructureBeats.Count)
            return OperationResult<bool>.Failure($"To index {toIndex} is out of range");

        problem.StructureBeats.Move(fromIndex, toIndex);
        return OperationResult<bool>.Success(true);
    }

    #endregion
}
