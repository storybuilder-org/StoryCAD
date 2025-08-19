# PowerShell script to convert Lists.ini to Lists.json
$iniPath = "C:\Users\Jake\Documents\GitHub\StoryCAD\StoryCADLib\Assets\Install\Lists.ini"
$jsonPath = "C:\Users\Jake\Documents\GitHub\StoryCAD\StoryCADLib\Assets\Install\Lists.json"

$lines = Get-Content $iniPath
$lists = @{}

foreach ($line in $lines) {
    $trimmedLine = $line.Trim()
    
    # Skip empty lines, sections, and comments
    if ([string]::IsNullOrEmpty($trimmedLine) -or 
        $trimmedLine.StartsWith("[") -or 
        $trimmedLine.StartsWith(";")) {
        continue
    }
    
    # Process key=value pairs
    if ($trimmedLine.Contains("=")) {
        $parts = $trimmedLine.Split("=", 2)
        $key = $parts[0].Trim()
        $value = $parts[1].Trim()
        
        if (-not $lists.ContainsKey($key)) {
            $lists[$key] = @()
        }
        
        $lists[$key] += $value
    }
}

# Convert to JSON
$json = $lists | ConvertTo-Json -Depth 10
$json | Out-File -FilePath $jsonPath -Encoding UTF8

Write-Host "Lists.json created successfully!"
Write-Host "Created file with $($lists.Count) lists"