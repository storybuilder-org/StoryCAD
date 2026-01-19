using System.Text.Json.Serialization;

namespace StoryCADLib.Models.StoryWorld;

/// <summary>
/// Entry for the Governments list tab.
/// Represents a government, faction, or power structure.
/// </summary>
public class GovernmentEntry
{
    [JsonIgnore] private string _name = string.Empty;
    [JsonIgnore] private string _type = string.Empty;
    [JsonIgnore] private string _powerStructures = string.Empty;
    [JsonIgnore] private string _laws = string.Empty;
    [JsonIgnore] private string _classStructure = string.Empty;
    [JsonIgnore] private string _foreignRelations = string.Empty;

    [JsonInclude] [JsonPropertyName("Name")]
    public string Name { get => _name; set => _name = value; }

    [JsonInclude] [JsonPropertyName("Type")]
    public string Type { get => _type; set => _type = value; }

    [JsonInclude] [JsonPropertyName("PowerStructures")]
    public string PowerStructures { get => _powerStructures; set => _powerStructures = value; }

    [JsonInclude] [JsonPropertyName("Laws")]
    public string Laws { get => _laws; set => _laws = value; }

    [JsonInclude] [JsonPropertyName("ClassStructure")]
    public string ClassStructure { get => _classStructure; set => _classStructure = value; }

    [JsonInclude] [JsonPropertyName("ForeignRelations")]
    public string ForeignRelations { get => _foreignRelations; set => _foreignRelations = value; }
}
