using System.Text.Json.Serialization;

namespace StoryCADLib.Models.StoryWorld;

/// <summary>
/// Entry for the People/Species list tab.
/// Represents a species, race, or people group.
/// </summary>
public class SpeciesEntry
{
    [JsonIgnore] private string _name = string.Empty;
    [JsonIgnore] private string _physicalTraits = string.Empty;
    [JsonIgnore] private string _lifespan = string.Empty;
    [JsonIgnore] private string _origins = string.Empty;
    [JsonIgnore] private string _socialStructure = string.Empty;
    [JsonIgnore] private string _diversity = string.Empty;

    [JsonInclude] [JsonPropertyName("Name")]
    public string Name { get => _name; set => _name = value; }

    [JsonInclude] [JsonPropertyName("PhysicalTraits")]
    public string PhysicalTraits { get => _physicalTraits; set => _physicalTraits = value; }

    [JsonInclude] [JsonPropertyName("Lifespan")]
    public string Lifespan { get => _lifespan; set => _lifespan = value; }

    [JsonInclude] [JsonPropertyName("Origins")]
    public string Origins { get => _origins; set => _origins = value; }

    [JsonInclude] [JsonPropertyName("SocialStructure")]
    public string SocialStructure { get => _socialStructure; set => _socialStructure = value; }

    [JsonInclude] [JsonPropertyName("Diversity")]
    public string Diversity { get => _diversity; set => _diversity = value; }
}
