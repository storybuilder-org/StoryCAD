using System.Text.Json.Serialization;

namespace StoryCAD.Models.Tools;

public class PlotPatternModel
{
    [JsonInclude, JsonPropertyName("PlotPatternName")]
    public string PlotPatternName;

    [JsonInclude, JsonPropertyName("PlotPatternNotes")]
    public string PlotPatternNotes;

    [JsonInclude, JsonPropertyName("PlotPatternScenes")]
    public List<PlotPatternScene> PlotPatternScenes;


    public PlotPatternModel(string name)
    {
        PlotPatternName = name;
        PlotPatternNotes = string.Empty;
        PlotPatternScenes = new List<PlotPatternScene>();
    }
}