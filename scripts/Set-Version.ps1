#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Sets the StoryCAD version across Package.appxmanifest, StoryCADLib.csproj,
    and the macOS Info.plist (CFBundleShortVersionString).

.PARAMETER Version
    The version string to apply (e.g. "4.0.2" or "4.0.2.65534").

.PARAMETER RepoRoot
    Root of the repository. Defaults to the parent directory of this script.

.EXAMPLE
    pwsh ./scripts/Set-Version.ps1 -Version 4.0.2
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Version,
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot ".."))
)

$ErrorActionPreference = 'Stop'

$manifestPath  = Join-Path $RepoRoot "StoryCAD/Package.appxmanifest"
$csprojPath    = Join-Path $RepoRoot "StoryCADLib/StoryCADLib.csproj"
$infoPlistPath = Join-Path $RepoRoot "StoryCAD/Platforms/Desktop/Info.plist"

# Package.appxmanifest — update <Identity Version="...">
[xml]$manifest = Get-Content $manifestPath
$ns = New-Object System.Xml.XmlNamespaceManager($manifest.NameTable)
$ns.AddNamespace('ns', $manifest.DocumentElement.NamespaceURI)
$identity = $manifest.SelectSingleNode('/ns:Package/ns:Identity', $ns)
$identity.SetAttribute('Version', $Version)
$manifest.Save($manifestPath)
Write-Host "Updated $manifestPath to $Version"

# StoryCADLib.csproj — update Version / AssemblyVersion / FileVersion if present
[xml]$csproj = Get-Content $csprojPath
foreach ($nodeName in @('Version', 'AssemblyVersion', 'FileVersion')) {
    $node = $csproj.SelectSingleNode("//$nodeName")
    if ($node) { $node.InnerText = $Version }
}
$csproj.Save($csprojPath)
Write-Host "Updated $csprojPath to $Version"

# Info.plist (macOS) — CFBundleShortVersionString must be 1-3 dotted integers.
# Use targeted regex (not [xml]) so the Apple DTD reference isn't fetched.
$shortVersion = ($Version -split '\.')[0..2] -join '.'
$content = [IO.File]::ReadAllText($infoPlistPath)
$pattern = '(<key>CFBundleShortVersionString</key>\s*<string>)[^<]*(</string>)'
$updated = [regex]::Replace($content, $pattern, "`${1}$shortVersion`${2}")
if ($updated -eq $content) {
    throw "Failed to update CFBundleShortVersionString in $infoPlistPath (key not found?)."
}
[IO.File]::WriteAllText($infoPlistPath, $updated)
Write-Host "Updated $infoPlistPath CFBundleShortVersionString to $shortVersion"
