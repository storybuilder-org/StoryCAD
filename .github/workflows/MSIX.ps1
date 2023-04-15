# MSIX Cleaner by Jake Shaw (C) 2023 StoryCAD Non-Profit

param(
    [Parameter(Mandatory=$true)]
    [string]$TargetDirectory
)

Write-Host "MSIX Cleaner running on: $TargetDirectory"
New-Item -Path "$TargetDirectory\msix" -ItemType Directory -Force | Out-Null

Get-ChildItem -Path $TargetDirectory -Recurse -File | ForEach-Object {
    $file = $_.FullName

    if ($file -like "*.cer" -and -not (Test-Path "$TargetDirectory\StoryCAD.cer")) {
        Copy-Item $file -Destination "$TargetDirectory\StoryCAD.cer"
    }

    if ($file -like "*StoryCAD*.msix" -and $file -notlike "*.msixsym" -and $file -notlike "*Dependencies*") {
        $filename = "StoryCAD "

        if ($file -like "*Debug*") { $filename += "Debug" }
        else { $filename += "Release" }

        if ($file -like "*x64*") { $filename += " x64" }
        elseif ($file -like "*arm*") { $filename += " Arm64" }
        elseif ($file -like "*x86*") { $filename += " x86" }
        else { $filename += " ArchUnknown" }

        $filename += ".msix"
        Write-Host "Moved file: $file to $TargetDirectory\msix\$filename"
        Copy-Item $file -Destination "$TargetDirectory\msix\$filename"
    }
    else {
        Write-Host "Deleted file: $file"
        Remove-Item $file -Force
    }
}

Get-ChildItem -Path $TargetDirectory -Recurse -Directory | Where-Object { $_.FullName -ne "$TargetDirectory\msix" } | ForEach-Object {
    Remove-Item $_.FullName -Recurse -Force
}

Write-Host "MSIX Cleaner finished"
