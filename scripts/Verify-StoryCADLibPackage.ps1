<#
.SYNOPSIS
    Fails if the StoryCADLib nupkg is missing a TFM or the CollaboratorLib.dll it carries.
.DESCRIPTION
    Protects the release contract for consumers like the API-Samples Outliner and
    StoryCADMcp. Used by build-release.yml and nuget-prerelease.yml.
.PARAMETER PackageDir
    The folder that holds StoryCADLib.*.nupkg.
.EXAMPLE
    pwsh ./scripts/Verify-StoryCADLibPackage.ps1 -PackageDir ./nupkg
#>
param(
    [Parameter(Mandatory)][string]$PackageDir
)

$ErrorActionPreference = 'Stop'

$nupkg = Get-ChildItem (Join-Path $PackageDir 'StoryCADLib.*.nupkg') | Select-Object -First 1
if (-not $nupkg) { throw "No StoryCADLib nupkg produced." }

Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [System.IO.Compression.ZipFile]::OpenRead($nupkg.FullName)
try {
    $entries = $zip.Entries | ForEach-Object { $_.FullName }
} finally { $zip.Dispose() }

$libs = $entries | Where-Object { $_ -like 'lib/*' } |
    ForEach-Object { ($_ -split '/')[1] } | Sort-Object -Unique
Write-Host "Package: $($nupkg.Name)"
Write-Host "lib/ TFMs: $($libs -join ', ')"

# Prefix-match: NuGet normalizes 'net10.0-desktop' to 'net10.0-desktop1.0'
# and could change Windows TPV in future. Match the family, not the exact folder.
foreach ($family in 'net10.0-desktop', 'net10.0-windows') {
    $tfm = $libs | Where-Object { $_ -like "$family*" } | Select-Object -First 1
    if (-not $tfm) { throw "nupkg is missing the $family TFM." }

    # Collaborator#291 / #1648: CollaboratorLib ships inside this package.
    foreach ($dll in 'StoryCADLib.dll', 'CollaboratorLib.dll') {
        if ($entries -notcontains "lib/$tfm/$dll") { throw "nupkg is missing lib/$tfm/$dll." }
    }
}
Write-Host "StoryCADLib.dll and CollaboratorLib.dll present in both TFMs."
