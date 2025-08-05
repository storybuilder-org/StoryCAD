using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json.Serialization;
using StoryCAD.Services.Outline;
using Microsoft.SemanticKernel;
using System.Reflection;

namespace StoryCAD.Services.API;

/// <summary>
/// 
/// </summary>
public class SemanticKernelApi
{
    private readonly OutlineService _outlineService = Ioc.Default.GetRequiredService<OutlineService>();
    public StoryModel CurrentModel;

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
            if (!result.IsSuccess || result.Payload == null)
            {
                response.IsSuccess = false;
                response.ErrorMessage = result.ErrorMessage;
                return response;
            }

            // Set model.
            CurrentModel = result.Payload;
            List<Guid> elementGuids = CurrentModel.StoryElements.Select(e => e.Uuid).ToList();

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
    /// <param name="newElement">Element source</param>
    /// <param name="guid">UUID of element that will be updated.</param>
    [KernelFunction, Description(
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
    public void UpdateStoryElement(object newElement, Guid guid)
    {
        if (CurrentModel == null)
        {
            throw new InvalidOperationException("No StoryModel available. Create a model first.");
        }

        if (guid == Guid.Empty)
        {
            throw new ArgumentNullException("GUID is null");
        }

        // Use OutlineService to get the element
        try
        {
            var existingElement = _outlineService.GetStoryElementByGuid(CurrentModel, guid);
        }
        catch (InvalidOperationException)
        {
            throw new ArgumentNullException("StoryElement does not exist");
        }

        //Deserialize and update.
        StoryElement updated = StoryElement.Deserialize(newElement.ToString());
        //TODO: force set uuid somehow.
        updated.Uuid = guid;
        _outlineService.UpdateStoryElement(CurrentModel, updated);
    }

    /// <summary>
    /// Updates specified properties for a story element identified by its GUID.
    /// A dictionary is used to specify the properties to update, where keys are
    /// property names and values are the new values.
    /// Iterates over each property in the dictionary and calls UpdateElementProperty.
    /// </summary>
    /// <param name="elementGuid">The GUID of the story element to update.</param>
    /// <param name="properties">A Dictionary where keys are property names and values are the new values.</param>
    /// <returns>void if all properties are updated successfully, exception otherwise.</returns>
    [KernelFunction, Description(
         """
         Updates multiple properties for a given story element within the outline.
         This function is a wrapper for UpdateElementProperty and iterates over each 
         property in a dictionary of properties to update.
         Guidelines:
         - You must provide the StoryElement GUID as a parameter. 
         - All properties will be updated for the same StoryElement.
         - UpdateElementProperty edits each properties of the StoryElement.
         """)]
    public void UpdateElementProperties(Guid elementGuid, Dictionary<string, object> properties)
    {
        foreach (var kvp in properties)
        {
            var result = UpdateElementProperty(elementGuid, kvp.Key, kvp.Value);
        }
    }

    [KernelFunction, Description("Returns a single element and all its fields.")]
    public object GetElement(Guid guid)
    {
        if (CurrentModel == null)
        {
            throw new InvalidOperationException("No StoryModel available. Create a model first.");
        }

        if (guid == Guid.Empty)
        {
            throw new ArgumentNullException("GUID is null");
        }

        var element = _outlineService.GetStoryElementByGuid(CurrentModel, guid);
        return element.Serialize();
    }

    [KernelFunction, Description("""
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
    public OperationResult<Guid> AddElement(StoryItemType typeToAdd, string parentGUID, string name, string GUIDOverride = "")
    {
        if (CurrentModel == null)
        {
            return OperationResult<Guid>.Failure("No StoryModel available. Create a model first.");
        }

        if (!Guid.TryParse(parentGUID, out var parentGuid))
        {
            return OperationResult<Guid>.Failure($"Invalid parent GUID: {parentGUID}");
        }

        Guid DesiredGuid = Guid.Empty;
        if (!String.IsNullOrEmpty(GUIDOverride))
        {
            if (!Guid.TryParse(GUIDOverride, out DesiredGuid))
            {
                return OperationResult<Guid>.Failure($"Invalid guid GUID: {parentGUID}");
            }

            try
            {
                _outlineService.GetStoryElementByGuid(CurrentModel, DesiredGuid);
                return OperationResult<Guid>.Failure($"GUID Override already exists.");
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
            var newElement = _outlineService.AddStoryElement(CurrentModel, typeToAdd, parent.Node);
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

    
    [KernelFunction, Description("""
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
    public OperationResult<Guid> AddElement(StoryItemType typeToAdd, string parentGUID, string name, Dictionary<string, object> properties, string GUIDOverride = "")
    {
        if (CurrentModel == null)
        {
            return OperationResult<Guid>.Failure("No StoryModel available. Create a model first.");
        }

        if (!Guid.TryParse(parentGUID, out var parentGuid))
        {
            return OperationResult<Guid>.Failure($"Invalid parent GUID: {parentGUID}");
        }

        Guid DesiredGuid = Guid.Empty;
        if (!String.IsNullOrEmpty(GUIDOverride))
        {
            if (!Guid.TryParse(GUIDOverride, out DesiredGuid))
            {
                return OperationResult<Guid>.Failure($"Invalid guid GUID: {parentGUID}");
            }

            try
            {
                _outlineService.GetStoryElementByGuid(CurrentModel, DesiredGuid);
                return OperationResult<Guid>.Failure($"GUID Override already exists.");
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
            var newElement = _outlineService.AddStoryElement(CurrentModel, typeToAdd, parent.Node);

            if (DesiredGuid != Guid.Empty)
            {
                newElement.UpdateGuid(CurrentModel, DesiredGuid);
            }

            newElement.Name = name;

            newElement.Name = name;
            UpdateElementProperties(newElement.Uuid,  properties);

            return OperationResult<Guid>.Success(newElement.Uuid);
        }
        catch (Exception ex)
        {
            return OperationResult<Guid>.Failure($"Error in AddElement: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates the specified property of a StoryElement identified by its UUID.
    /// Only properties decorated with [JsonInclude] are allowed to be updated.
    /// The function uses reflection to locate the property, verify its [JsonInclude] attribute,
    /// and update its value (performing a type conversion if necessary).
    /// If the property is not found, is read-only, or is missing the attribute, an error is returned.
    /// </summary>
    [KernelFunction, Description(
         """
         Updates the specified property on a StoryElement (identified by its UUID).
         Only properties decorated with [JsonInclude] are updatable. If the property is missing the attribute
         or if any error occurs (such as a type conversion issue), the operation will fails.
         You should use AddCastMember and AddRelationship for updating those fields when updating those fields.
         """)]
    public OperationResult<StoryElement> UpdateElementProperty(Guid elementUuid, string propertyName, object value)
    {
        // Ensure we have a current StoryModel.
        if (CurrentModel == null)
            return OperationResult<StoryElement>.Failure("No StoryModel available. Create a model first.");

        // Find the StoryElement in the current model.
        StoryElement element = CurrentModel.StoryElements.FirstOrDefault(e => e.Uuid == elementUuid);
        if (element == null)
            return OperationResult<StoryElement>.Failure("StoryElement not found.");

        try
        {
            // Get the property info by name.
            PropertyInfo property = element.GetType().GetProperty(propertyName);
            
            if (property == null)
                throw new ArgumentException($"Property '{propertyName}' not found on type {element.GetType().FullName}.");

            // Ensure the property has the [JsonInclude] attribute.
            if (!Attribute.IsDefined(property, typeof(JsonIncludeAttribute)))
                throw new InvalidOperationException($"Property '{propertyName}' does not have the JsonInclude attribute.");

            // Ensure the property is writable.
            if (!property.CanWrite)
                throw new InvalidOperationException($"Property '{propertyName}' is read-only.");


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
    /// Opens new outline from file.
    /// </summary>
    /// <param name="path">Outline to open</param>
    [KernelFunction, Description("Opens an outline from a file.")]
    public async Task<OperationResult<bool>> OpenOutline(string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path) | !Path.Exists(path))
            {
                throw new ArgumentException("Invalid path");
            }

            CurrentModel = await _outlineService.OpenFile(path);
            return OperationResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return OperationResult<bool>.Failure($"Error in OpenOutline: {ex.Message}");
        }

    }

    [KernelFunction, Description("""
                                 Move Element to the trashcan, this is a destructive action.
                                 Do not call this unless the user wants you to delete the element.
                                 You cannot delete the overview, narrator view, or trashcan elements.
                                 Elements that are deleted will appear under the trashcan element.
                                 Type is a enum that specifies if you are deleting from explorer or narrator
                                 view.
                                 """)]
    public Task<OperationResult<bool>> DeleteElement(Guid elementToDelete, StoryViewType Type)
    {
        // Ensure we have a current StoryModel.
        if (CurrentModel == null)
            return Task.FromResult(OperationResult<bool>.Failure("No outline is opened"));

        //Remove reference to element
        StoryElement element = _outlineService.GetStoryElementByGuid(CurrentModel, elementToDelete);
        _outlineService.RemoveReferenceToElement(elementToDelete, CurrentModel);

        // Remove the element from the model.
        if (Type == StoryViewType.ExplorerView)
        {
            element.Node.Delete(Type);
        }
        else
        {
            element.Node.Delete(Type);
        }

        return Task.FromResult(OperationResult<bool>.Success(true));
    }

    [KernelFunction, Description("""
                                 Adds a new cast member to a scene.
                                 Scene MUST be a GUID of element that is a scene
                                 Character MUST be a GUID of an element that is a character.
                                 """)]
    public OperationResult<bool> AddCastMember(Guid scene, Guid character)
    {
        try
        {
            if (CurrentModel == null)
                return OperationResult<bool>.Failure("No outline is opened");

            StoryElement element = _outlineService.GetStoryElementByGuid(CurrentModel, scene);
            _outlineService.AddCastMember(CurrentModel, element, character);

            return OperationResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return OperationResult<bool>.Failure($"Error in AddCastMember: {ex.Message}");
        }

    }


    [KernelFunction, Description("""
                                 Adds a relationship between characters.
                                 Both Source and Recipient must be GUIDs of elements that are characters.
                                 Description is the relationship between the two characters.
                                 mirror is a boolean that specifies if the relationship
                                 should be created on both characters.
                                 """)]
    public bool AddRelationship(Guid source, Guid recipient, string desc, bool mirror = false)
    {
        if (source == Guid.Empty)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (recipient == Guid.Empty)
        {
            throw new ArgumentNullException(nameof(recipient));
        }

        _outlineService.AddRelationship(CurrentModel, source, recipient, desc, mirror);
        return true;
    }
}