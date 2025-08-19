# PowerShell script to convert INI files to JSON
Add-Type -TypeDefinition @"
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

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
                var parts = trimmedLine.Split(new[] { '=' }, 2);
                var key = parts[0].Trim();
                var value = parts[1].Trim();
                
                if (!lists.ContainsKey(key))
                    lists[key] = new List<string>();
                
                lists[key].Add(value);
            }
        }
        
        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(lists, options);
        File.WriteAllText(jsonPath, json);
    }
    
    public static void ConvertToolsIni(string iniPath, string jsonPath)
    {
        var lines = File.ReadAllLines(iniPath);
        var result = new Dictionary<string, object>();
        var keyQuestions = new Dictionary<string, List<Dictionary<string, string>>>();
        var stockScenes = new Dictionary<string, List<string>>();
        var topics = new Dictionary<string, object>();
        var masterPlots = new List<Dictionary<string, object>>();
        var beatSheets = new List<Dictionary<string, object>>();
        var dramaticSituations = new Dictionary<string, Dictionary<string, object>>();
        
        var currentSection = "";
        var currentElement = "";
        var currentTopic = "";
        var currentStockCategory = "";
        var currentTopicName = "";
        Dictionary<string, object> currentMasterPlot = null;
        Dictionary<string, object> currentBeatSheet = null;
        Dictionary<string, object> currentDramaticSituation = null;
        Dictionary<string, string> currentPlotPoint = null;
        
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
                
            var parts = trimmedLine.Split(new[] { '=' }, 2);
            var keyword = parts[0].Trim();
            var value = parts[1].Trim();
            
            switch (currentSection)
            {
                case "Key Questions":
                    switch (keyword)
                    {
                        case "Element":
                            currentElement = value;
                            if (!keyQuestions.ContainsKey(value))
                                keyQuestions[value] = new List<Dictionary<string, string>>();
                            break;
                        case "Topic":
                            currentTopic = value;
                            break;
                        default:
                            keyQuestions[currentElement].Add(new Dictionary<string, string>
                            {
                                ["key"] = keyword,
                                ["topic"] = currentTopic,
                                ["question"] = value
                            });
                            break;
                    }
                    break;
                    
                case "Stock Scenes":
                    switch (keyword)
                    {
                        case "Title":
                            currentStockCategory = value;
                            stockScenes[value] = new List<string>();
                            break;
                        case "Scene":
                            stockScenes[currentStockCategory].Add(value);
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
                            topics[currentTopicName] = new Dictionary<string, object>
                            {
                                ["type"] = "notepad",
                                ["filename"] = value
                            };
                            break;
                        case "Subtopic":
                            if (!topics.ContainsKey(currentTopicName))
                            {
                                topics[currentTopicName] = new Dictionary<string, object>
                                {
                                    ["type"] = "inline",
                                    ["subTopics"] = new List<Dictionary<string, string>>()
                                };
                            }
                            var topicDict = (Dictionary<string, object>)topics[currentTopicName];
                            var subTopics = (List<Dictionary<string, string>>)topicDict["subTopics"];
                            subTopics.Add(new Dictionary<string, string>
                            {
                                ["name"] = value,
                                ["notes"] = ""
                            });
                            break;
                        case "Remarks":
                            if (topics.ContainsKey(currentTopicName))
                            {
                                var topic = (Dictionary<string, object>)topics[currentTopicName];
                                if (topic.ContainsKey("subTopics"))
                                {
                                    var subs = (List<Dictionary<string, string>>)topic["subTopics"];
                                    if (subs.Count > 0)
                                    {
                                        var last = subs[subs.Count - 1];
                                        if (!string.IsNullOrEmpty(last["notes"]))
                                            last["notes"] += " ";
                                        last["notes"] += value;
                                    }
                                }
                            }
                            break;
                    }
                    break;
                    
                case "MasterPlots":
                    switch (keyword)
                    {
                        case "MasterPlot":
                            currentMasterPlot = new Dictionary<string, object>
                            {
                                ["name"] = value,
                                ["notes"] = "",
                                ["scenes"] = new List<Dictionary<string, string>>()
                            };
                            masterPlots.Add(currentMasterPlot);
                            break;
                        case "Remarks":
                            if (!string.IsNullOrEmpty((string)currentMasterPlot["notes"]))
                                currentMasterPlot["notes"] += "\n";
                            currentMasterPlot["notes"] += value;
                            break;
                        case "PlotPoint":
                        case "Scene":
                            currentPlotPoint = new Dictionary<string, string>
                            {
                                ["name"] = value,
                                ["notes"] = ""
                            };
                            ((List<Dictionary<string, string>>)currentMasterPlot["scenes"]).Add(currentPlotPoint);
                            break;
                        case "Notes":
                            if (!string.IsNullOrEmpty(currentPlotPoint["notes"]))
                                currentPlotPoint["notes"] += "\n";
                            currentPlotPoint["notes"] += value;
                            break;
                    }
                    break;
                    
                case "BeatSheets":
                    switch (keyword)
                    {
                        case "BeatSheet":
                            currentBeatSheet = new Dictionary<string, object>
                            {
                                ["name"] = value,
                                ["notes"] = "",
                                ["scenes"] = new List<Dictionary<string, string>>()
                            };
                            beatSheets.Add(currentBeatSheet);
                            break;
                        case "Remarks":
                            if (!string.IsNullOrEmpty((string)currentBeatSheet["notes"]))
                                currentBeatSheet["notes"] += "\n";
                            currentBeatSheet["notes"] += value;
                            break;
                        case "Beat":
                            currentPlotPoint = new Dictionary<string, string>
                            {
                                ["name"] = value,
                                ["notes"] = ""
                            };
                            ((List<Dictionary<string, string>>)currentBeatSheet["scenes"]).Add(currentPlotPoint);
                            break;
                        case "Notes":
                            if (!string.IsNullOrEmpty(currentPlotPoint["notes"]))
                                currentPlotPoint["notes"] += "\n";
                            currentPlotPoint["notes"] += value;
                            break;
                    }
                    break;
                    
                case "Dramatic Situations":
                    switch (keyword)
                    {
                        case "Situation":
                            currentDramaticSituation = new Dictionary<string, object>
                            {
                                ["name"] = value,
                                ["roles"] = new List<string>(),
                                ["descriptions"] = new List<string>(),
                                ["examples"] = new List<string>(),
                                ["notes"] = ""
                            };
                            dramaticSituations[value] = currentDramaticSituation;
                            break;
                        case "Role1":
                        case "Role2":
                        case "Role3":
                        case "Role4":
                            ((List<string>)currentDramaticSituation["roles"]).Add(value);
                            break;
                        case "Desc1":
                        case "Desc2":
                        case "Desc3":
                        case "Desc4":
                            ((List<string>)currentDramaticSituation["descriptions"]).Add(value);
                            break;
                        case "Example":
                            ((List<string>)currentDramaticSituation["examples"]).Add(value);
                            break;
                        case "Notes":
                            currentDramaticSituation["notes"] += value;
                            break;
                    }
                    break;
            }
        }
        
        result["keyQuestions"] = keyQuestions;
        result["stockScenes"] = stockScenes;
        result["topics"] = topics;
        result["masterPlots"] = masterPlots;
        result["beatSheets"] = beatSheets;
        result["dramaticSituations"] = dramaticSituations;
        
        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(result, options);
        File.WriteAllText(jsonPath, json);
    }
}
"@ -ReferencedAssemblies System.Text.Json

$basePath = "C:\Users\Jake\Documents\GitHub\StoryCAD\StoryCADLib\Assets\Install"

Write-Host "Converting Lists.ini to Lists.json..."
[IniToJsonConverter]::ConvertListsIni(
    [IO.Path]::Combine($basePath, "Lists.ini"),
    [IO.Path]::Combine($basePath, "Lists.json")
)
Write-Host "Lists.json created successfully!"

Write-Host "Converting Tools.ini to Tools.json..."
[IniToJsonConverter]::ConvertToolsIni(
    [IO.Path]::Combine($basePath, "Tools.ini"),
    [IO.Path]::Combine($basePath, "Tools.json")
)
Write-Host "Tools.json created successfully!"

Write-Host "`nConversion complete!"