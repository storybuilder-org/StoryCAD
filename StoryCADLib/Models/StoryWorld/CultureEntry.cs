using System.Text.Json.Serialization;

namespace StoryCADLib.Models.StoryWorld;

/// <summary>
/// Entry for the Cultures list tab.
/// Represents a culture, milieu, or social environment.
/// For Consensus Reality stories, each entry is a milieu (e.g., Wall Street, police precinct).
/// </summary>
public class CultureEntry
{
    [JsonIgnore] private string _name = string.Empty;
    [JsonIgnore] private string _values = string.Empty;
    [JsonIgnore] private string _customs = string.Empty;
    [JsonIgnore] private string _taboos = string.Empty;
    [JsonIgnore] private string _art = string.Empty;
    [JsonIgnore] private string _dailyLife = string.Empty;
    [JsonIgnore] private string _entertainment = string.Empty;

    [JsonInclude] [JsonPropertyName("Name")]
    public string Name { get => _name; set => _name = value; }

    [JsonInclude] [JsonPropertyName("Values")]
    public string Values { get => _values; set => _values = value; }

    [JsonInclude] [JsonPropertyName("Customs")]
    public string Customs { get => _customs; set => _customs = value; }

    [JsonInclude] [JsonPropertyName("Taboos")]
    public string Taboos { get => _taboos; set => _taboos = value; }

    [JsonInclude] [JsonPropertyName("Art")]
    public string Art { get => _art; set => _art = value; }

    [JsonInclude] [JsonPropertyName("DailyLife")]
    public string DailyLife { get => _dailyLife; set => _dailyLife = value; }

    [JsonInclude] [JsonPropertyName("Entertainment")]
    public string Entertainment { get => _entertainment; set => _entertainment = value; }
}
