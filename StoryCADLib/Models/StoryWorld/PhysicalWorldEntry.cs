using System.Text.Json.Serialization;

namespace StoryCADLib.Models.StoryWorld;

/// <summary>
/// Entry for the Physical World list tab.
/// Represents a world, planet, or realm in multi-world stories.
/// </summary>
public class PhysicalWorldEntry
{
    [JsonIgnore] private string _name = string.Empty;
    [JsonIgnore] private string _geography = string.Empty;
    [JsonIgnore] private string _climate = string.Empty;
    [JsonIgnore] private string _naturalResources = string.Empty;
    [JsonIgnore] private string _flora = string.Empty;
    [JsonIgnore] private string _fauna = string.Empty;
    [JsonIgnore] private string _astronomy = string.Empty;

    [JsonInclude] [JsonPropertyName("Name")]
    public string Name { get => _name; set => _name = value; }

    [JsonInclude] [JsonPropertyName("Geography")]
    public string Geography { get => _geography; set => _geography = value; }

    [JsonInclude] [JsonPropertyName("Climate")]
    public string Climate { get => _climate; set => _climate = value; }

    [JsonInclude] [JsonPropertyName("NaturalResources")]
    public string NaturalResources { get => _naturalResources; set => _naturalResources = value; }

    [JsonInclude] [JsonPropertyName("Flora")]
    public string Flora { get => _flora; set => _flora = value; }

    [JsonInclude] [JsonPropertyName("Fauna")]
    public string Fauna { get => _fauna; set => _fauna = value; }

    [JsonInclude] [JsonPropertyName("Astronomy")]
    public string Astronomy { get => _astronomy; set => _astronomy = value; }

    public PhysicalWorldEntry Clone() => new()
    {
        Name = Name, Geography = Geography, Climate = Climate,
        NaturalResources = NaturalResources, Flora = Flora,
        Fauna = Fauna, Astronomy = Astronomy
    };
}
