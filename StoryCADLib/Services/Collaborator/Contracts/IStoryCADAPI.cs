using System.Collections.ObjectModel;
using StoryCAD.Services.API;

namespace StoryCAD.Services.Collaborator.Contracts;

/// <summary>
///     Interface for existing SemanticKernelAPI methods that Collaborator uses
/// </summary>
public interface IStoryCADAPI
{
    /// <summary>
    ///     The current story model being worked on
    /// </summary>
    StoryModel CurrentModel { get; set; }

    /// <summary>
    ///     Creates a new empty outline
    /// </summary>
    /// <param name="name">Name of the story</param>
    /// <param name="author">Author of the story</param>
    /// <param name="templateIndex">Template index to use</param>
    /// <returns>Result with list of created element GUIDs</returns>
    Task<OperationResult<List<Guid>>> CreateEmptyOutline(string name, string author, string templateIndex);

    /// <summary>
    ///     Writes the outline to a file
    /// </summary>
    /// <param name="filePath">Path to write the file to</param>
    /// <returns>Result with the file path</returns>
    Task<OperationResult<string>> WriteOutline(string filePath);

    /// <summary>
    ///     Gets all story elements in the current model
    /// </summary>
    /// <returns>OperationResult containing the collection of all story elements</returns>
    OperationResult<ObservableCollection<StoryElement>> GetAllElements();

    /// <summary>
    ///     Updates a story element
    /// </summary>
    /// <param name="newElement">The updated element</param>
    /// <param name="guid">The GUID of the element to update</param>
    /// <returns>Result of the update operation</returns>
    OperationResult<bool> UpdateStoryElement(object newElement, Guid guid);

    /// <summary>
    ///     Updates multiple properties of an element
    /// </summary>
    /// <param name="elementGuid">The GUID of the element to update</param>
    /// <param name="properties">Dictionary of property names and values</param>
    /// <returns>Result of the update operation</returns>
    OperationResult<bool> UpdateElementProperties(Guid elementGuid, Dictionary<string, object> properties);

    /// <summary>
    ///     Updates a single property of an element
    /// </summary>
    /// <param name="elementGuid">The GUID of the element to update</param>
    /// <param name="propertyName">Name of the property to update</param>
    /// <param name="newValue">New value for the property</param>
    /// <returns>Result of the operation</returns>
    OperationResult<object> UpdateElementProperty(Guid elementGuid, string propertyName, object newValue);

    /// <summary>
    ///     Gets a story element by its GUID
    /// </summary>
    /// <param name="guid">The GUID of the element</param>
    /// <returns>OperationResult containing the story element if found, or error message if not</returns>
    OperationResult<StoryElement> GetStoryElement(Guid guid);
}
