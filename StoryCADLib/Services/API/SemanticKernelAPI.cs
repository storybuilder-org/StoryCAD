﻿using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json.Serialization;
using StoryCAD.Services.Outline;
using StoryCAD.Services.Collaborator.Contracts;
using Microsoft.SemanticKernel;
using System.Reflection;

namespace StoryCAD.Services.API;

/// <summary>
/// The StoryCAD API—a powerful interface that combines human and AI interactions for generating and managing comprehensive story outlines.
/// 
/// Two types of interaction are supported:
/// - Human Interaction: Allows users to directly create, modify, and manage story elements through method calls.
/// - AI-Driven Automation: Integrates with Semantic Kernel to enable AI-powered story generation and element creation.
/// 
/// For a complete description of the API and its capabilities, see:
/// https://storybuilder-org.github.io/StoryCAD/docs/For%20Developers/Using_the_API.html
/// 
/// Usage:
/// - State Handling: The API operates on a CurrentModel property which holds the active StoryModel instance.
///   This model must be set before most operations can be performed, either via SetCurrentModel() or by
///   creating a new outline with CreateEmptyOutline().
/// - Calling Standard: All public API methods return OperationResult<T> to ensure safe external consumption.
///   No exceptions are thrown to external callers; all errors are communicated through the OperationResult
///   pattern with IsSuccess flags and descriptive ErrorMessage strings.
/// </summary>
public class SemanticKernelApi : IStoryCADAPI
{
    private readonly OutlineService _outlineService = Ioc.Default.GetRequiredService<OutlineService>();
    public StoryModel CurrentModel { get; set; }

    /// <summary>
    /// Sets the current StoryModel to work with (for Collaborator integration)
    /// An open outline in StoryCAD is represented by a StoryModel and ShellViewModel
    /// contains the active StoryModel as CurrentModel, which is passed to Collaborator in CollaboratorArgs
    /// for further operations. This method allows Collaborator to set the API's current StoryModel.
    /// </summary>
    /// <param name="model">The active StoryModel from ShellViewModel</param>
    public void SetCurrentModel(StoryModel model)
    {
        CurrentModel = model;
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
    public OperationResult<ObservableCollection<StoryElement>> GetAllElements()
    {
        if (CurrentModel == null)
        {
            return OperationResult<ObservableCollection<StoryElement>>.Failure("No StoryModel available. Create a model first.");
        }

        try
        {
            return OperationResult<ObservableCollection<StoryElement>>.Success(CurrentModel.StoryElements);
        }
        catch (Exception ex)
        {
            return OperationResult<ObservableCollection<StoryElement>>.Failure($"Error retrieving elements: {ex.Message}");
        }
    }

    /// <summary>
    /// Deletes a story element from the current StoryModel.
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
            var element = _outlineService.GetStoryElementByGuid(CurrentModel, guid);
            _outlineService.MoveToTrash(element, CurrentModel);
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
            // Use OutlineService to get the element
            var existingElement = _outlineService.GetStoryElementByGuid(CurrentModel, guid);
            
            //Deserialize and update.
            StoryElement updated = StoryElement.Deserialize(newElement.ToString());
            //TODO: force set uuid somehow.
            updated.Uuid = guid;
            _outlineService.UpdateStoryElement(CurrentModel, updated);
            
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
                    return OperationResult<bool>.Failure($"Failed to update property '{kvp.Key}': {result.ErrorMessage}");
                }
            }
            return OperationResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return OperationResult<bool>.Failure($"Error updating properties: {ex.Message}");
        }
    }

    [KernelFunction, Description("Returns a single element and all its fields.")]
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
            var element = _outlineService.GetStoryElementByGuid(CurrentModel, guid);
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

    /// <summary>
    /// Implementation of IStoryCADAPI.GetStoryElement.
    /// This is an alias for GetStoryElementByGuid.
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
            var element = _outlineService.GetStoryElementByGuid(CurrentModel, guid);
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
        try
        {
            // Ensure we have a current StoryModel.
            if (CurrentModel == null)
                return Task.FromResult(OperationResult<bool>.Failure("No outline is opened"));

            // Get the element and move it to trash using OutlineService
            StoryElement element = _outlineService.GetStoryElementByGuid(CurrentModel, elementToDelete);
            _outlineService.MoveToTrash(element, CurrentModel);

            return Task.FromResult(OperationResult<bool>.Success(true));
        }
        catch (Exception ex)
        {
            return Task.FromResult(OperationResult<bool>.Failure($"Error in DeleteElement: {ex.Message}"));
        }
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
            _outlineService.AddRelationship(CurrentModel, source, recipient, desc, mirror);
            return OperationResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return OperationResult<bool>.Failure($"Error adding relationship: {ex.Message}");
        }
    }

    /// <summary>
    /// Searches for story elements containing the specified text.
    /// </summary>
    /// <param name="searchText">The text to search for (case-insensitive)</param>
    /// <returns>A list of element GUIDs and names that contain the search text</returns>
    [KernelFunction, Description("""
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
                return OperationResult<List<Dictionary<string, object>>>.Failure("No outline is opened");

            if (string.IsNullOrWhiteSpace(searchText))
                return OperationResult<List<Dictionary<string, object>>>.Failure("Search text cannot be empty");

            var results = _outlineService.SearchForText(CurrentModel, searchText);
            
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
    /// Searches for story elements that reference a specific UUID.
    /// </summary>
    /// <param name="targetUuid">The UUID to search for references to</param>
    /// <returns>A list of elements that reference the specified UUID</returns>
    [KernelFunction, Description("""
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
                return OperationResult<List<Dictionary<string, object>>>.Failure("No outline is opened");

            if (targetUuid == Guid.Empty)
                return OperationResult<List<Dictionary<string, object>>>.Failure("Target UUID cannot be empty");

            var results = _outlineService.SearchForUuidReferences(CurrentModel, targetUuid);
            
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
            return OperationResult<List<Dictionary<string, object>>>.Failure($"Error in SearchForReferences: {ex.Message}");
        }
    }

    /// <summary>
    /// Removes all references to a specified UUID from the story model.
    /// </summary>
    /// <param name="targetUuid">The UUID to remove references to</param>
    /// <returns>The number of elements that had references removed</returns>
    [KernelFunction, Description("""
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
                return OperationResult<int>.Failure("No outline is opened");

            if (targetUuid == Guid.Empty)
                return OperationResult<int>.Failure("Target UUID cannot be empty");

            int affectedCount = _outlineService.RemoveUuidReferences(CurrentModel, targetUuid);
            
            return OperationResult<int>.Success(affectedCount);
        }
        catch (Exception ex)
        {
            return OperationResult<int>.Failure($"Error in RemoveReferences: {ex.Message}");
        }
    }

    /// <summary>
    /// Searches for story elements within a specific subtree.
    /// </summary>
    /// <param name="rootNodeGuid">The GUID of the root node to search from</param>
    /// <param name="searchText">The text to search for (case-insensitive)</param>
    /// <returns>A list of elements in the subtree that contain the search text</returns>
    [KernelFunction, Description("""
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
                return OperationResult<List<Dictionary<string, object>>>.Failure("No outline is opened");

            if (rootNodeGuid == Guid.Empty)
                return OperationResult<List<Dictionary<string, object>>>.Failure("Root node GUID cannot be empty");

            if (string.IsNullOrWhiteSpace(searchText))
                return OperationResult<List<Dictionary<string, object>>>.Failure("Search text cannot be empty");

            // Find the root node
            var rootElement = _outlineService.GetStoryElementByGuid(CurrentModel, rootNodeGuid);
            if (rootElement == null)
                return OperationResult<List<Dictionary<string, object>>>.Failure("Root node not found");

            var results = _outlineService.SearchInSubtree(CurrentModel, rootElement.Node, searchText);
            
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
    /// Restores a story element from the trash back to its original location.
    /// </summary>
    /// <param name="elementToRestore">The GUID of the element to restore from trash</param>
    /// <returns>An OperationResult indicating success or failure</returns>
    [KernelFunction, Description("""
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
                return Task.FromResult(OperationResult<bool>.Failure("No outline is opened"));

            // Get the trash node
            var trashNode = CurrentModel.TrashView.FirstOrDefault(n => n.Type == StoryItemType.TrashCan);
            if (trashNode == null)
                return Task.FromResult(OperationResult<bool>.Failure("TrashCan node not found"));

            // Find the node to restore
            var nodeToRestore = trashNode.Children.FirstOrDefault(n => n.Uuid == elementToRestore);
            if (nodeToRestore == null)
                return Task.FromResult(OperationResult<bool>.Failure("Element not found in trash"));

            // Use OutlineService to restore the element
            _outlineService.RestoreFromTrash(nodeToRestore, CurrentModel);

            return Task.FromResult(OperationResult<bool>.Success(true));
        }
        catch (Exception ex)
        {
            return Task.FromResult(OperationResult<bool>.Failure($"Error in RestoreFromTrash: {ex.Message}"));
        }
    }

    /// <summary>
    /// Empties the trash, permanently removing all items.
    /// </summary>
    /// <returns>An OperationResult indicating success or failure</returns>
    [KernelFunction, Description("""
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
                return Task.FromResult(OperationResult<bool>.Failure("No outline is opened"));

            // Use OutlineService to empty the trash
            _outlineService.EmptyTrash(CurrentModel);

            return Task.FromResult(OperationResult<bool>.Success(true));
        }
        catch (Exception ex)
        {
            return Task.FromResult(OperationResult<bool>.Failure($"Error in EmptyTrash: {ex.Message}"));
        }
    }
}