using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StoryCAD.Utilities
{
    public class IniToJsonConverter
    {
        public static void ConvertListsIni(string iniPath, string jsonPath)
        {
            var lines = File.ReadAllLines(iniPath);
            var lists = new Dictionary<string, List<string>>();
            
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("[") || trimmedLine.StartsWith(";"))
                    continue;
                    
                if (trimmedLine.Contains('='))
                {
                    var parts = trimmedLine.Split('=', 2);
                    var key = parts[0].Trim();
                    var value = parts[1].Trim();
                    
                    if (!lists.ContainsKey(key))
                        lists[key] = new List<string>();
                    
                    lists[key].Add(value);
                }
            }
            
            var json = JsonSerializer.Serialize(lists, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
            File.WriteAllText(jsonPath, json);
        }
        
        public static void ConvertToolsIni(string iniPath, string jsonPath)
        {
            var lines = File.ReadAllLines(iniPath);
            var toolsData = new ToolsJsonData();
            
            var currentSection = "";
            var currentKeyQuestionElement = "";
            var currentKeyQuestionTopic = "";
            var currentStockSceneCategory = "";
            var currentTopicName = "";
            var currentMasterPlot = (PlotPatternData)null;
            var currentBeatSheet = (PlotPatternData)null;
            var currentDramaticSituation = (DramaticSituationData)null;
            var currentPlotPoint = (PlotPointData)null;
            
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith(";"))
                    continue;
                    
                if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                {
                    currentSection = trimmedLine.Substring(1, trimmedLine.Length - 2);
                    continue;
                }
                
                if (!trimmedLine.Contains('='))
                    continue;
                    
                var parts = trimmedLine.Split('=', 2);
                var keyword = parts[0].Trim();
                var value = parts[1].Trim();
                
                switch (currentSection)
                {
                    case "Key Questions":
                        switch (keyword)
                        {
                            case "Element":
                                currentKeyQuestionElement = value;
                                if (!toolsData.KeyQuestions.ContainsKey(value))
                                    toolsData.KeyQuestions[value] = new List<KeyQuestionData>();
                                break;
                            case "Topic":
                                currentKeyQuestionTopic = value;
                                break;
                            default:
                                toolsData.KeyQuestions[currentKeyQuestionElement].Add(new KeyQuestionData
                                {
                                    Key = keyword,
                                    Topic = currentKeyQuestionTopic,
                                    Question = value
                                });
                                break;
                        }
                        break;
                        
                    case "Stock Scenes":
                        switch (keyword)
                        {
                            case "Title":
                                currentStockSceneCategory = value;
                                toolsData.StockScenes[value] = new List<string>();
                                break;
                            case "Scene":
                                toolsData.StockScenes[currentStockSceneCategory].Add(value);
                                break;
                        }
                        break;
                        
                    case "Topic Information":
                        switch (keyword)
                        {
                            case "Topic":
                                currentTopicName = value;
                                break;
                            case "Notepad":
                                toolsData.Topics[currentTopicName] = new TopicData
                                {
                                    Name = currentTopicName,
                                    Type = "notepad",
                                    Filename = value
                                };
                                break;
                            case "Subtopic":
                                if (!toolsData.Topics.ContainsKey(currentTopicName))
                                {
                                    toolsData.Topics[currentTopicName] = new TopicData
                                    {
                                        Name = currentTopicName,
                                        Type = "inline",
                                        SubTopics = new List<SubTopicData>()
                                    };
                                }
                                toolsData.Topics[currentTopicName].SubTopics.Add(new SubTopicData
                                {
                                    Name = value,
                                    Notes = ""
                                });
                                break;
                            case "Remarks":
                                if (toolsData.Topics[currentTopicName].SubTopics?.Count > 0)
                                {
                                    var lastSubTopic = toolsData.Topics[currentTopicName].SubTopics.Last();
                                    if (!string.IsNullOrEmpty(lastSubTopic.Notes))
                                        lastSubTopic.Notes += " ";
                                    lastSubTopic.Notes += value;
                                }
                                break;
                        }
                        break;
                        
                    case "MasterPlots":
                        switch (keyword)
                        {
                            case "MasterPlot":
                                currentMasterPlot = new PlotPatternData
                                {
                                    Name = value,
                                    Notes = "",
                                    Scenes = new List<PlotPointData>()
                                };
                                toolsData.MasterPlots.Add(currentMasterPlot);
                                break;
                            case "Remarks":
                                if (!string.IsNullOrEmpty(currentMasterPlot.Notes))
                                    currentMasterPlot.Notes += "\n";
                                currentMasterPlot.Notes += value;
                                break;
                            case "PlotPoint":
                            case "Scene":
                                currentPlotPoint = new PlotPointData
                                {
                                    Name = value,
                                    Notes = ""
                                };
                                currentMasterPlot.Scenes.Add(currentPlotPoint);
                                break;
                            case "Notes":
                                if (!string.IsNullOrEmpty(currentPlotPoint.Notes))
                                    currentPlotPoint.Notes += "\n";
                                currentPlotPoint.Notes += value;
                                break;
                        }
                        break;
                        
                    case "BeatSheets":
                        switch (keyword)
                        {
                            case "BeatSheet":
                                currentBeatSheet = new PlotPatternData
                                {
                                    Name = value,
                                    Notes = "",
                                    Scenes = new List<PlotPointData>()
                                };
                                toolsData.BeatSheets.Add(currentBeatSheet);
                                break;
                            case "Remarks":
                                if (!string.IsNullOrEmpty(currentBeatSheet.Notes))
                                    currentBeatSheet.Notes += "\n";
                                currentBeatSheet.Notes += value;
                                break;
                            case "Beat":
                                currentPlotPoint = new PlotPointData
                                {
                                    Name = value,
                                    Notes = ""
                                };
                                currentBeatSheet.Scenes.Add(currentPlotPoint);
                                break;
                            case "Notes":
                                if (!string.IsNullOrEmpty(currentPlotPoint.Notes))
                                    currentPlotPoint.Notes += "\n";
                                currentPlotPoint.Notes += value;
                                break;
                        }
                        break;
                        
                    case "Dramatic Situations":
                        switch (keyword)
                        {
                            case "Situation":
                                currentDramaticSituation = new DramaticSituationData
                                {
                                    Name = value,
                                    Roles = new List<string>(),
                                    Descriptions = new List<string>(),
                                    Examples = new List<string>(),
                                    Notes = ""
                                };
                                toolsData.DramaticSituations[value] = currentDramaticSituation;
                                break;
                            case "Role1":
                            case "Role2":
                            case "Role3":
                            case "Role4":
                                currentDramaticSituation.Roles.Add(value);
                                break;
                            case "Desc1":
                            case "Desc2":
                            case "Desc3":
                            case "Desc4":
                                currentDramaticSituation.Descriptions.Add(value);
                                break;
                            case "Example":
                                currentDramaticSituation.Examples.Add(value);
                                break;
                            case "Notes":
                                currentDramaticSituation.Notes += value;
                                break;
                        }
                        break;
                        
                    default:
                        // Handle simple key-value lists (like Male Names, etc.)
                        if (!string.IsNullOrEmpty(currentSection))
                        {
                            if (!toolsData.SimpleLists.ContainsKey(currentSection))
                                toolsData.SimpleLists[currentSection] = new Dictionary<string, List<string>>();
                            
                            if (!toolsData.SimpleLists[currentSection].ContainsKey(keyword))
                                toolsData.SimpleLists[currentSection][keyword] = new List<string>();
                                
                            toolsData.SimpleLists[currentSection][keyword].Add(value);
                        }
                        break;
                }
            }
            
            var json = JsonSerializer.Serialize(toolsData, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
            File.WriteAllText(jsonPath, json);
        }
    }
    
    // Data classes for JSON serialization
    public class ToolsJsonData
    {
        [JsonPropertyName("keyQuestions")]
        public Dictionary<string, List<KeyQuestionData>> KeyQuestions { get; set; } = new();
        
        [JsonPropertyName("stockScenes")]
        public Dictionary<string, List<string>> StockScenes { get; set; } = new();
        
        [JsonPropertyName("topics")]
        public Dictionary<string, TopicData> Topics { get; set; } = new();
        
        [JsonPropertyName("masterPlots")]
        public List<PlotPatternData> MasterPlots { get; set; } = new();
        
        [JsonPropertyName("beatSheets")]
        public List<PlotPatternData> BeatSheets { get; set; } = new();
        
        [JsonPropertyName("dramaticSituations")]
        public Dictionary<string, DramaticSituationData> DramaticSituations { get; set; } = new();
        
        [JsonPropertyName("lists")]
        public Dictionary<string, Dictionary<string, List<string>>> SimpleLists { get; set; } = new();
    }
    
    public class KeyQuestionData
    {
        [JsonPropertyName("key")]
        public string Key { get; set; }
        
        [JsonPropertyName("topic")]
        public string Topic { get; set; }
        
        [JsonPropertyName("question")]
        public string Question { get; set; }
    }
    
    public class TopicData
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("type")]
        public string Type { get; set; }
        
        [JsonPropertyName("filename")]
        public string Filename { get; set; }
        
        [JsonPropertyName("subTopics")]
        public List<SubTopicData> SubTopics { get; set; }
    }
    
    public class SubTopicData
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("notes")]
        public string Notes { get; set; }
    }
    
    public class PlotPatternData
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("notes")]
        public string Notes { get; set; }
        
        [JsonPropertyName("scenes")]
        public List<PlotPointData> Scenes { get; set; }
    }
    
    public class PlotPointData
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("notes")]
        public string Notes { get; set; }
    }
    
    public class DramaticSituationData
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("roles")]
        public List<string> Roles { get; set; }
        
        [JsonPropertyName("descriptions")]
        public List<string> Descriptions { get; set; }
        
        [JsonPropertyName("examples")]
        public List<string> Examples { get; set; }
        
        [JsonPropertyName("notes")]
        public string Notes { get; set; }
    }
}