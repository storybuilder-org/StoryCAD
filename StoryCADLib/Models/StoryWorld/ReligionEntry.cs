using System.Text.Json.Serialization;

namespace StoryCADLib.Models.StoryWorld;

/// <summary>
/// Entry for the Religions list tab.
/// Represents a religion or belief system.
/// </summary>
public class ReligionEntry
{
    [JsonIgnore] private string _name = string.Empty;
    [JsonIgnore] private string _deities = string.Empty;
    [JsonIgnore] private string _beliefs = string.Empty;
    [JsonIgnore] private string _practices = string.Empty;
    [JsonIgnore] private string _organizations = string.Empty;
    [JsonIgnore] private string _creationMyths = string.Empty;

    [JsonInclude] [JsonPropertyName("Name")]
    public string Name { get => _name; set => _name = value; }

    [JsonInclude] [JsonPropertyName("Deities")]
    public string Deities { get => _deities; set => _deities = value; }

    [JsonInclude] [JsonPropertyName("Beliefs")]
    public string Beliefs { get => _beliefs; set => _beliefs = value; }

    [JsonInclude] [JsonPropertyName("Practices")]
    public string Practices { get => _practices; set => _practices = value; }

    [JsonInclude] [JsonPropertyName("Organizations")]
    public string Organizations { get => _organizations; set => _organizations = value; }

    [JsonInclude] [JsonPropertyName("CreationMyths")]
    public string CreationMyths { get => _creationMyths; set => _creationMyths = value; }

    public ReligionEntry Clone() => new()
    {
        Name = Name, Deities = Deities, Beliefs = Beliefs,
        Practices = Practices, Organizations = Organizations,
        CreationMyths = CreationMyths
    };
}
