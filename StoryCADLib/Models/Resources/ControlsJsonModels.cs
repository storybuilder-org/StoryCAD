using System.Text.Json.Serialization;

namespace StoryCADLib.Models.Resources;

/// <summary>
/// JSON deserialization models for Controls.json resource file.
/// </summary>
internal record ControlsJson(
    [property: JsonPropertyName("conflictTypes")] List<ConflictTypeJson> ConflictTypes,
    [property: JsonPropertyName("relationTypes")] List<string> RelationTypes);

internal record ConflictTypeJson(
    [property: JsonPropertyName("category")] string Category,
    [property: JsonPropertyName("subCategories")] List<SubCategoryJson> SubCategories);

internal record SubCategoryJson(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("examples")] List<string> Examples);
