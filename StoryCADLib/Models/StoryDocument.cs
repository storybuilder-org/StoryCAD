#pragma warning disable CS8632 // Nullable annotations used without nullable context
namespace StoryCADLib.Models;

/// <summary>
///     Encapsulates a story document, combining the model and its file path.
///     This ensures the model and path are always kept together as a unit.
/// </summary>
public sealed class StoryDocument
{
    /// <summary>
    ///     Creates a new StoryDocument with the specified model and optional file path.
    /// </summary>
    /// <param name="model">The story model (required)</param>
    /// <param name="filePath">The file path (optional, null for new documents)</param>
    public StoryDocument(StoryModel model, string? filePath = null)
        => (Model, FilePath) = (model, filePath);

    /// <summary>
    ///     The story model containing all story data.
    ///     Readonly - to change models, create a new StoryDocument instance.
    /// </summary>
    public StoryModel Model { get; }

    /// <summary>
    ///     The file path where this document is saved.
    ///     Null for new unsaved documents ("Untitled").
    ///     Mutable to support SaveAs operations.
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    ///     Indicates whether the document has unsaved changes.
    ///     Delegates to the Model's Changed property.
    /// </summary>
    public bool IsDirty => Model.Changed;
}
