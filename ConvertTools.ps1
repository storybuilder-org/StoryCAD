# PowerShell script to convert Tools.ini to Tools.json
$iniPath = "C:\Users\Jake\Documents\GitHub\StoryCAD\StoryCADLib\Assets\Install\Tools.ini"
$jsonPath = "C:\Users\Jake\Documents\GitHub\StoryCAD\StoryCADLib\Assets\Install\Tools.json"

$lines = Get-Content $iniPath
$result = @{
    keyQuestions = @{}
    stockScenes = @{}
    topics = @{}
    masterPlots = @()
    beatSheets = @()
    dramaticSituations = @{}
}

$currentSection = ""
$currentElement = ""
$currentTopic = ""
$currentStockCategory = ""
$currentTopicName = ""
$currentMasterPlot = $null
$currentBeatSheet = $null
$currentDramaticSituation = $null
$currentPlotPoint = $null

foreach ($line in $lines) {
    $trimmedLine = $line.Trim()
    
    # Skip empty lines and comments
    if ([string]::IsNullOrEmpty($trimmedLine) -or $trimmedLine.StartsWith(";")) {
        continue
    }
    
    # Handle sections
    if ($trimmedLine.StartsWith("[") -and $trimmedLine.EndsWith("]")) {
        $currentSection = $trimmedLine.Substring(1, $trimmedLine.Length - 2)
        continue
    }
    
    # Skip lines without =
    if (-not $trimmedLine.Contains("=")) {
        continue
    }
    
    # Parse key=value
    $parts = $trimmedLine.Split("=", 2)
    $keyword = $parts[0].Trim()
    $value = $parts[1].Trim()
    
    switch ($currentSection) {
        "Key Questions" {
            switch ($keyword) {
                "Element" {
                    $currentElement = $value
                    if (-not $result.keyQuestions.ContainsKey($value)) {
                        $result.keyQuestions[$value] = @()
                    }
                }
                "Topic" {
                    $currentTopic = $value
                }
                default {
                    $result.keyQuestions[$currentElement] += @{
                        key = $keyword
                        topic = $currentTopic
                        question = $value
                    }
                }
            }
        }
        
        "Stock Scenes" {
            switch ($keyword) {
                "Title" {
                    $currentStockCategory = $value
                    $result.stockScenes[$value] = @()
                }
                "Scene" {
                    $result.stockScenes[$currentStockCategory] += $value
                }
            }
        }
        
        "Topic Information" {
            switch ($keyword) {
                "Topic" {
                    $currentTopicName = $value
                }
                "Notepad" {
                    $result.topics[$currentTopicName] = @{
                        type = "notepad"
                        filename = $value
                    }
                }
                "Subtopic" {
                    if (-not $result.topics.ContainsKey($currentTopicName)) {
                        $result.topics[$currentTopicName] = @{
                            type = "inline"
                            subTopics = @()
                        }
                    }
                    $result.topics[$currentTopicName].subTopics += @{
                        name = $value
                        notes = ""
                    }
                }
                "Remarks" {
                    if ($result.topics.ContainsKey($currentTopicName) -and 
                        $result.topics[$currentTopicName].subTopics -and
                        $result.topics[$currentTopicName].subTopics.Count -gt 0) {
                        $lastSubTopic = $result.topics[$currentTopicName].subTopics[-1]
                        if ($lastSubTopic.notes) {
                            $lastSubTopic.notes += " "
                        }
                        $lastSubTopic.notes += $value
                    }
                }
            }
        }
        
        "MasterPlots" {
            switch ($keyword) {
                "MasterPlot" {
                    $currentMasterPlot = @{
                        name = $value
                        notes = ""
                        scenes = @()
                    }
                    $result.masterPlots += $currentMasterPlot
                }
                "Remarks" {
                    if ($currentMasterPlot.notes) {
                        $currentMasterPlot.notes += "`n"
                    }
                    $currentMasterPlot.notes += $value
                }
                { $_ -eq "PlotPoint" -or $_ -eq "Scene" } {
                    $currentPlotPoint = @{
                        name = $value
                        notes = ""
                    }
                    $currentMasterPlot.scenes += $currentPlotPoint
                }
                "Notes" {
                    if ($currentPlotPoint.notes) {
                        $currentPlotPoint.notes += "`n"
                    }
                    $currentPlotPoint.notes += $value
                }
            }
        }
        
        "BeatSheets" {
            switch ($keyword) {
                "BeatSheet" {
                    $currentBeatSheet = @{
                        name = $value
                        notes = ""
                        scenes = @()
                    }
                    $result.beatSheets += $currentBeatSheet
                }
                "Remarks" {
                    if ($currentBeatSheet.notes) {
                        $currentBeatSheet.notes += "`n"
                    }
                    $currentBeatSheet.notes += $value
                }
                "Beat" {
                    $currentPlotPoint = @{
                        name = $value
                        notes = ""
                    }
                    $currentBeatSheet.scenes += $currentPlotPoint
                }
                "Notes" {
                    if ($currentPlotPoint.notes) {
                        $currentPlotPoint.notes += "`n"
                    }
                    $currentPlotPoint.notes += $value
                }
            }
        }
        
        "Dramatic Situations" {
            switch ($keyword) {
                "Situation" {
                    $currentDramaticSituation = @{
                        name = $value
                        roles = @()
                        descriptions = @()
                        examples = @()
                        notes = ""
                    }
                    $result.dramaticSituations[$value] = $currentDramaticSituation
                }
                { $_ -match "Role\d" } {
                    $currentDramaticSituation.roles += $value
                }
                { $_ -match "Desc\d" } {
                    $currentDramaticSituation.descriptions += $value
                }
                "Example" {
                    $currentDramaticSituation.examples += $value
                }
                "Notes" {
                    $currentDramaticSituation.notes += $value
                }
            }
        }
    }
}

# Convert to JSON
$json = $result | ConvertTo-Json -Depth 10
$json | Out-File -FilePath $jsonPath -Encoding UTF8

Write-Host "Tools.json created successfully!"
Write-Host "Created file with:"
Write-Host "  - $($result.keyQuestions.Count) key question categories"
Write-Host "  - $($result.stockScenes.Count) stock scene categories"
Write-Host "  - $($result.topics.Count) topics"
Write-Host "  - $($result.masterPlots.Count) master plots"
Write-Host "  - $($result.beatSheets.Count) beat sheets"
Write-Host "  - $($result.dramaticSituations.Count) dramatic situations"