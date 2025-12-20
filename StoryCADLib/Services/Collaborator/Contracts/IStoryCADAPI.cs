using System.Collections.ObjectModel;
using StoryCADLib.Services.API;

namespace StoryCADLib.Services.Collaborator.Contracts;

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

    #region Resource API (Issue #1223)

    /// <summary>
    /// Gets examples for a property from Lists.json
    /// </summary>
    /// <param name="propertyName">Property name matching a list key</param>
    /// <returns>Result containing the list values, or error if key not found</returns>
    OperationResult<IEnumerable<string>> GetExamples(string propertyName);

    /// <summary>
    /// Gets all conflict categories from Controls.json
    /// </summary>
    /// <returns>Result containing the list of conflict categories</returns>
    OperationResult<IEnumerable<string>> GetConflictCategories();

    /// <summary>
    /// Gets subcategories for a conflict category
    /// </summary>
    /// <param name="category">The conflict category name</param>
    /// <returns>Result containing the list of subcategories, or error if category not found</returns>
    OperationResult<IEnumerable<string>> GetConflictSubcategories(string category);

    /// <summary>
    /// Gets examples for a conflict category and subcategory
    /// </summary>
    /// <param name="category">The conflict category name</param>
    /// <param name="subcategory">The subcategory name within the category</param>
    /// <returns>Result containing the list of examples, or error if not found</returns>
    OperationResult<IEnumerable<string>> GetConflictExamples(string category, string subcategory);

    /// <summary>
    /// Gets all element types that have key questions available
    /// </summary>
    /// <returns>Result containing the list of element types</returns>
    OperationResult<IEnumerable<string>> GetKeyQuestionElements();

    /// <summary>
    /// Gets key questions for an element type
    /// </summary>
    /// <param name="elementType">The element type (e.g., Character, Problem, Scene)</param>
    /// <returns>Result containing tuples of (Topic, Question), or error if element type not found</returns>
    OperationResult<IEnumerable<(string Topic, string Question)>> GetKeyQuestions(string elementType);

    /// <summary>
    /// Gets all master plot names
    /// </summary>
    /// <returns>Result containing the list of master plot names</returns>
    OperationResult<IEnumerable<string>> GetMasterPlotNames();

    /// <summary>
    /// Gets notes for a master plot
    /// </summary>
    /// <param name="plotName">The master plot name</param>
    /// <returns>Result containing the plot notes, or error if plot not found</returns>
    OperationResult<string> GetMasterPlotNotes(string plotName);

    /// <summary>
    /// Gets all stock scene categories
    /// </summary>
    /// <returns>Result containing the list of category names</returns>
    OperationResult<IEnumerable<string>> GetStockSceneCategories();

    /// <summary>
    /// Gets stock scenes for a category
    /// </summary>
    /// <param name="category">The stock scene category name</param>
    /// <returns>Result containing the list of scenes, or error if category not found</returns>
    OperationResult<IEnumerable<string>> GetStockScenes(string category);

    // ===== Beat Sheets API =====

    /// <summary>
    /// Gets all beat sheet template names
    /// </summary>
    OperationResult<IEnumerable<string>> GetBeatSheetNames();

    /// <summary>
    /// Gets a beat sheet template by name
    /// </summary>
    OperationResult<(string Description, IEnumerable<(string BeatName, string BeatNotes)> Beats)> GetBeatSheet(string beatSheetName);

    /// <summary>
    /// Applies a beat sheet template to a Problem's structure
    /// </summary>
    OperationResult<bool> ApplyBeatSheetToProblem(Guid problemGuid, string beatSheetName);

    /// <summary>
    /// Gets the current structure of a Problem
    /// </summary>
    OperationResult<(string Title, string Description, IEnumerable<(string BeatTitle, string BeatDescription, Guid? LinkedElement)> Beats)> GetProblemStructure(Guid problemGuid);

    /// <summary>
    /// Assigns a Scene or Problem element to a beat
    /// </summary>
    OperationResult<bool> AssignElementToBeat(Guid problemGuid, int beatIndex, Guid elementGuid);

    /// <summary>
    /// Clears the element assignment from a beat
    /// </summary>
    OperationResult<bool> ClearBeatAssignment(Guid problemGuid, int beatIndex);

    /// <summary>
    /// Creates a new beat in a Problem's structure
    /// </summary>
    OperationResult<bool> CreateBeat(Guid problemGuid, string title, string description);

    /// <summary>
    /// Updates a beat's title and description
    /// </summary>
    OperationResult<bool> UpdateBeat(Guid problemGuid, int beatIndex, string title, string description);

    /// <summary>
    /// Deletes a beat from a Problem's structure
    /// </summary>
    OperationResult<bool> DeleteBeat(Guid problemGuid, int beatIndex);

    /// <summary>
    /// Moves a beat from one position to another
    /// </summary>
    OperationResult<bool> MoveBeat(Guid problemGuid, int fromIndex, int toIndex);

    #endregion
}
