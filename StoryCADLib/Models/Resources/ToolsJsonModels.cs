using System.Text.Json.Serialization;

namespace StoryCADLib.Models.Resources;

/// <summary>
/// JSON deserialization models for Tools.json resource file.
/// </summary>
internal record ToolsJson(
    [property: JsonPropertyName("keyQuestions")] Dictionary<string, List<KeyQuestionJson>> KeyQuestions,
    [property: JsonPropertyName("stockScenes")] Dictionary<string, List<string>> StockScenes,
    [property: JsonPropertyName("topics")] Dictionary<string, TopicJson> Topics,
    [property: JsonPropertyName("masterPlots")] List<PlotPatternJson> MasterPlots,
    [property: JsonPropertyName("beatSheets")] List<PlotPatternJson> BeatSheets,
    [property: JsonPropertyName("dramaticSituations")] Dictionary<string, DramaticSituationJson> DramaticSituations,
    [property: JsonPropertyName("MaleFirstNames")] List<string> MaleFirstNames,
    [property: JsonPropertyName("FemaleFirstNames")] List<string> FemaleFirstNames,
    [property: JsonPropertyName("LastNames")] List<string> LastNames,
    [property: JsonPropertyName("Relationships")] List<string> Relationships);

internal record KeyQuestionJson(
    [property: JsonPropertyName("key")] string Key,
    [property: JsonPropertyName("topic")] string Topic,
    [property: JsonPropertyName("question")] string Question);

internal record TopicJson(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("filename")] string Filename,
    [property: JsonPropertyName("subTopics")] List<SubTopicJson> SubTopics);

internal record SubTopicJson(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("notes")] string Notes);

internal record PlotPatternJson(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("notes")] string Notes,
    [property: JsonPropertyName("scenes")] List<PlotPointJson> Scenes);

internal record PlotPointJson(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("notes")] string Notes);

internal record DramaticSituationJson(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("roles")] List<string> Roles,
    [property: JsonPropertyName("descriptions")] List<string> Descriptions,
    [property: JsonPropertyName("examples")] List<string> Examples,
    [property: JsonPropertyName("notes")] string Notes);
