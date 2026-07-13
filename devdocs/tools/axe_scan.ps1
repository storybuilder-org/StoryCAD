<#
    axe_scan.ps1 — automated Axe.Windows accessibility scan of the StoryCAD WinAppSDK head.

    Spike deliverable for issue #1420 (see devdocs/issue_1420_implementation_plan.md, "Optional
    spike (time-boxed, alongside Unit 2)"). Automates the per-batch scan that the approved design
    otherwise assigns to manual Accessibility Insights FastPass. Manual FastPass stays authoritative
    per that design; this script is a supplementary, scriptable fitness-function-style gate.

    WHAT IT DOES
      1. Launches StoryCAD's WinAppSDK head exe from the build output.
      2. Waits for its main window (polls Process.MainWindowHandle).
      3. Runs AxeWindowsCLI.exe against the process, capturing verbose console output to a log
         file in -OutputDirectory. AxeWindowsCLI also writes an .a11ytest file (a zip archive
         readable by the Accessibility Insights for Windows GUI) when errors are found.
      4. Kills the StoryCAD process (always, via try/finally, even on scan failure).
      5. Exits with AxeWindowsCLI's own exit code, which already IS the pass/fail bar:
             0   = scan completed, zero findings                          (PASS)
             1   = scan completed, one or more findings                   (FAIL)
             2   = scan failed to complete (couldn't attach, no UIA tree, exception) (INCONCLUSIVE)
             3   = --showthirdpartynotices was passed (not used by this script)
             255 = bad input parameters to AxeWindowsCLI itself
      This script's own pre-flight failures (exe not found, window never appeared, CLI not found)
      also exit 2, so "2" always means "no verdict was reached" as distinct from "0/clean" or
      "1/found problems".

    PREREQUISITE: install AxeWindowsCLI
      AxeWindowsCLI is NOT published as a `dotnet tool` — there is no NuGet package by that name.
      It ships as a self-contained build from the axe-windows GitHub releases:
        https://github.com/microsoft/axe-windows/releases
      Two options:
        - MSI (requires admin): AxeWindowsCLI-X.Y.Z.msi -> installs to
          C:\Program Files (x86)\AxeWindowsCLI\<version>\AxeWindowsCLI.exe
        - Zip (no admin, no install): AxeWindowsCLI-X.Y.Z.zip -> extract anywhere and pass
          -AxeWindowsCliPath, or set the AXE_WINDOWS_CLI_PATH environment variable, or put the
          extracted folder on PATH. This script searches, in order: -AxeWindowsCliPath param,
          $env:AXE_WINDOWS_CLI_PATH, PATH (via Get-Command), then the default MSI install
          location. Verified against v2.4.2 of both distributions.

    PREREQUISITE: build the WinAppSDK head first
      & "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" `
          StoryCAD.sln -t:Restore -p:Configuration=Debug -p:Platform=x64
      & "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" `
          StoryCAD.sln -t:Build -p:Configuration=Debug -p:Platform=x64
      This script does not build for you; it locates
      StoryCAD\bin\x64\<Configuration>\net10.0-windows10.0.22621\win-x64\StoryCAD.exe relative to
      the repo root (found by walking up from this script's location until StoryCAD.sln is found).

    USAGE
      .\axe_scan.ps1
      .\axe_scan.ps1 -Configuration Release
      .\axe_scan.ps1 -AxeWindowsCliPath "D:\tools\AxeWindowsCLI\AxeWindowsCLI.exe"
      .\axe_scan.ps1 -OutputDirectory "D:\scratch\axe-out" -DelayInSeconds 2

      Then check the exit code: `$LASTEXITCODE` after the script runs (or in CI, the process
      exit code). Console output and a plain-text log (console.log) always land in
      -OutputDirectory; the binary .a11ytest file only appears when findings exist (or if you add
      -AlwaysSaveTestFile).

    NOTE ON SCOPE
      This scans whatever page StoryCAD opens to (HomePage, by default, unless first-run
      preferences aren't initialized on the machine running the scan). It scans the live UIA tree
      of the whole process, not a single page/dialog in isolation — if you need per-page scans,
      drive navigation before invoking the scan (out of scope for this spike).
#>

[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',

    [string]$AxeWindowsCliPath,

    [string]$OutputDirectory = (Join-Path $env:TEMP "StoryCAD-AxeScan\$(Get-Date -Format 'yyyy-MM-dd_HH-mm-ss')"),

    [ValidateSet('Quiet', 'Default', 'Verbose')]
    [string]$Verbosity = 'Verbose',

    [int]$WindowTimeoutSeconds = 30,

    [int]$DelayInSeconds = 0,

    [switch]$AlwaysSaveTestFile
)

$ErrorActionPreference = 'Stop'

# Exit codes mirror AxeWindows.CLI's own ReturnValueChooser so callers get one consistent scheme.
$EXIT_NO_FINDINGS = 0
$EXIT_FINDINGS = 1
$EXIT_SCAN_FAILED = 2

function Resolve-RepoRoot {
    $dir = $PSScriptRoot
    while ($dir) {
        if (Test-Path (Join-Path $dir 'StoryCAD.sln')) { return $dir }
        $parent = Split-Path $dir -Parent
        if ($parent -eq $dir) { break }
        $dir = $parent
    }
    throw "Could not find StoryCAD.sln by walking up from $PSScriptRoot"
}

function Resolve-AxeWindowsCli {
    param([string]$ExplicitPath)

    if ($ExplicitPath) {
        if (Test-Path $ExplicitPath) { return (Resolve-Path $ExplicitPath).Path }
        throw "AxeWindowsCliPath '$ExplicitPath' does not exist."
    }

    if ($env:AXE_WINDOWS_CLI_PATH -and (Test-Path $env:AXE_WINDOWS_CLI_PATH)) {
        return (Resolve-Path $env:AXE_WINDOWS_CLI_PATH).Path
    }

    $onPath = Get-Command AxeWindowsCLI.exe -ErrorAction SilentlyContinue
    if ($onPath) { return $onPath.Source }

    $msiDefault = Get-ChildItem "C:\Program Files (x86)\AxeWindowsCLI" -Filter AxeWindowsCLI.exe -Recurse -ErrorAction SilentlyContinue |
        Sort-Object FullName -Descending | Select-Object -First 1
    if ($msiDefault) { return $msiDefault.FullName }

    throw @"
AxeWindowsCLI.exe not found. Install it (see the prerequisite comment at the top of this
script) and either put it on PATH, set AXE_WINDOWS_CLI_PATH, or pass -AxeWindowsCliPath.
Releases: https://github.com/microsoft/axe-windows/releases
"@
}

$repoRoot = Resolve-RepoRoot
$storyCadExe = Join-Path $repoRoot "StoryCAD\bin\x64\$Configuration\net10.0-windows10.0.22621\win-x64\StoryCAD.exe"

if (-not (Test-Path $storyCadExe)) {
    Write-Error @"
StoryCAD.exe not found at:
  $storyCadExe
Build the WinAppSDK head first (see the prerequisite comment at the top of this script).
"@
    exit $EXIT_SCAN_FAILED
}

$axeExe = Resolve-AxeWindowsCli -ExplicitPath $AxeWindowsCliPath

New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
Write-Host "Repo root:       $repoRoot"
Write-Host "StoryCAD.exe:    $storyCadExe"
Write-Host "AxeWindowsCLI:   $axeExe"
Write-Host "Output dir:      $OutputDirectory"

$proc = $null
$exitCode = $EXIT_SCAN_FAILED

try {
    $proc = Start-Process -FilePath $storyCadExe -PassThru

    $waited = 0
    $pollIntervalMs = 250
    while ($true) {
        Start-Sleep -Milliseconds $pollIntervalMs
        $waited += $pollIntervalMs
        $proc.Refresh()

        if ($proc.HasExited) {
            Write-Error "StoryCAD.exe exited before showing a main window (exit code $($proc.ExitCode))."
            exit $EXIT_SCAN_FAILED
        }
        if ($proc.MainWindowHandle -ne 0) { break }
        if ($waited -ge ($WindowTimeoutSeconds * 1000)) {
            throw "Timed out after $WindowTimeoutSeconds s waiting for StoryCAD's main window."
        }
    }
    Write-Host "Main window appeared after $([math]::Round($waited/1000, 1))s (PID $($proc.Id))."

    $axeArgs = @(
        '--processid', $proc.Id,
        '--outputdirectory', $OutputDirectory,
        '--verbosity', $Verbosity,
        '--delayinseconds', $DelayInSeconds
    )
    if ($AlwaysSaveTestFile) { $axeArgs += '--alwayssavetestfile' }

    $consoleLog = Join-Path $OutputDirectory 'console.log'
    & $axeExe @axeArgs 2>&1 | Tee-Object -FilePath $consoleLog
    $exitCode = $LASTEXITCODE
}
finally {
    if ($proc -and -not $proc.HasExited) {
        Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
    }
}

switch ($exitCode) {
    $EXIT_NO_FINDINGS { Write-Host "RESULT: PASS - no accessibility findings." -ForegroundColor Green }
    $EXIT_FINDINGS    { Write-Host "RESULT: FAIL - accessibility findings reported above / in $OutputDirectory\console.log" -ForegroundColor Red }
    default           { Write-Host "RESULT: INCONCLUSIVE - scan did not complete (exit code $exitCode)." -ForegroundColor Yellow }
}

exit $exitCode
