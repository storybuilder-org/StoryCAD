<#
    uia_header_probe.ps1 - scripted UIA probe of the live StoryCAD WinAppSDK head.

    Written for the issue #1443 header-exposure question and promoted to devdocs/tools
    because its launch + navigation recipe is the reusable part: it is the working
    pattern for anything that must drive the running app before inspecting it -
    per-page axe_scan.ps1 runs (axe_scan's own header calls navigation out of scope)
    and the #1422 FlaUI harness (every step here translates one-for-one to FlaUI).

    WHAT IT DOES
      1. Launches the WinAppSDK head exe from the build output.
      2. Opens the "Danger Calls" sample via the file-open menu (all UIA, no file picker).
      3. On OverviewPage, reads the UIA Name of StoryIdeaRichEdit (RichEditBoxExtended,
         Header="Story Idea") and the DateCreated/LastChanged TextBoxes.
      4. Opens Preferences > Save Locations and reads the Name of both BrowseTextBox
         inner PathTextBox controls.
      5. Kills the app (always, via finally).

    ANSWER IT PRODUCED (2026-07-04, dev @ 3a143319, net10.0-windows10.0.22621, Win11 26200):
      Header text DOES reach the UIA Name on all five probed controls:
        StoryIdeaRichEdit                -> Name='Story Idea'
        DateCreatedTextBox               -> Name='Date Created'
        LastChangedTextBox               -> Name='Last Changed'
        BrowseTextBox PathTextBox (x2)   -> Name='Project directory:' / 'Backup directory:'
      Consequences: no SetName follow-up needed in RichEditBoxExtended's constructor;
      Unit 5 needs no explicit Name on BrowseTextBox; Header-bearing controls are
      covered, controls WITHOUT a visible Header still need explicit names.

    PRECONDITIONS FOR UNATTENDED LAUNCH (all learned the hard way; every one blocks the
    probe when unmet):
      1. Preferences.json must exist in the exe directory (unpackaged RootDirectory)
         and be seeded, or the app opens on PreferencesInitialization instead of Shell:
           "Initialized": true
           "Version":  <StoryCADLib assembly version, e.g. "4.1.0.0">  (suppresses changelog)
           "ShowStartupDialog": false                                   (suppresses help dialog)
           "HideKeyFileWarning": true    (suppresses the key-file dialog; WinUI allows ONE
                                          ContentDialog at a time - any open dialog makes the
                                          file-open menu close instantly)
           "ShowFilePickerOnStartup": true, real OutlineDirectory/BackupDirectory paths
         NOTE: a first run with no Preferences.json registers a new user with the backend
         when .env is present. Seed the file BEFORE the first launch.
      2. .env must NOT be in the exe directory, or the app crashes at Shell_Loaded:
         AppState.DeveloperBuild (AppState.cs) evaluates Package.Current, which throws
         unpackaged; a debugger or missing .env short-circuits before that term.
         Rename .env away for the run and restore it after. (Removing .env also keeps
         the probe from posting anything to the backend.)
      3. StoryCAD is single-instance: a leftover process swallows the next launch.
         The probe refuses to start if one is running.

    USAGE (Windows PowerShell 5.1 - needs the GAC UIAutomationClient assemblies):
      powershell.exe -ExecutionPolicy Bypass -File devdocs\tools\uia_header_probe.ps1
      powershell.exe -ExecutionPolicy Bypass -File devdocs\tools\uia_header_probe.ps1 -Configuration Release

    Exit codes: 0 = all probes reported; 1 = a navigation step threw; 2 = pre-flight failure.
#>
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',

    [string]$ExePath,

    [int]$WindowTimeoutSeconds = 45
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

$AEType = [System.Windows.Automation.AutomationElement]
$TS = [System.Windows.Automation.TreeScope]

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

function Get-ProcessWindows {
    param([int]$ProcId)
    $cond = New-Object System.Windows.Automation.PropertyCondition($AEType::ProcessIdProperty, $ProcId)
    [System.Windows.Automation.AutomationElement]::RootElement.FindAll($TS::Children, $cond)
}

function Find-InProcess {
    # Searches every top-level window of the process (main window + windowed popups/flyouts).
    param([int]$ProcId, [System.Windows.Automation.Condition]$Cond, [int]$TimeoutSec = 10)
    $deadline = (Get-Date).AddSeconds($TimeoutSec)
    do {
        foreach ($w in (Get-ProcessWindows -ProcId $ProcId)) {
            $el = $w.FindFirst($TS::Descendants, $Cond)
            if ($el) { return $el }
        }
        Start-Sleep -Milliseconds 400
    } while ((Get-Date) -lt $deadline)
    return $null
}

function FindAll-InProcess {
    param([int]$ProcId, [System.Windows.Automation.Condition]$Cond)
    $results = @()
    foreach ($w in (Get-ProcessWindows -ProcId $ProcId)) {
        $found = $w.FindAll($TS::Descendants, $Cond)
        foreach ($f in $found) { $results += $f }
    }
    return $results
}

function ById { param([string]$Id) New-Object System.Windows.Automation.PropertyCondition($AEType::AutomationIdProperty, $Id) }
function ByName { param([string]$Name) New-Object System.Windows.Automation.PropertyCondition($AEType::NameProperty, $Name) }

function Invoke-El {
    param($El)
    ($El.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)).Invoke()
}

function Select-El {
    param($El)
    try {
        ($El.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern)).Select()
    } catch {
        Invoke-El $El   # NavigationViewItem sometimes only exposes Invoke
    }
}

function Report {
    param([string]$Label, $El)
    if ($null -eq $El) { Write-Host ("RESULT | {0,-42} | NOT FOUND" -f $Label); return }
    $c = $El.Current
    $labeledBy = ''
    try {
        $lb = $El.GetCurrentPropertyValue($AEType::LabeledByProperty)
        if ($lb) { $labeledBy = $lb.Current.Name }
    } catch { }
    Write-Host ("RESULT | {0,-42} | Name='{1}' | ControlType={2} | LabeledBy='{3}'" -f $Label, $c.Name, $c.ControlType.ProgrammaticName, $labeledBy)
}

function Dump-Tree {
    param($El, [int]$Depth = 0, [int]$MaxDepth = 3)
    if ($null -eq $El -or $Depth -gt $MaxDepth) { return }
    $c = $El.Current
    Write-Host ("{0}{1} Name='{2}' Id='{3}'" -f ('  ' * $Depth), $c.ControlType.ProgrammaticName, $c.Name, $c.AutomationId)
    if ($Depth -eq $MaxDepth) { return }
    $walker = [System.Windows.Automation.TreeWalker]::ControlViewWalker
    $child = $walker.GetFirstChild($El)
    while ($child) {
        Dump-Tree $child ($Depth + 1) $MaxDepth
        $child = $walker.GetNextSibling($child)
    }
}

function Log-Liveness {
    param($Proc, [string]$Where)
    $Proc.Refresh()
    Write-Host ("STATE | at '{0}': HasExited={1}" -f $Where, $Proc.HasExited)
}

# --- pre-flight ---------------------------------------------------------------
if (-not $ExePath) {
    $repoRoot = Resolve-RepoRoot
    $ExePath = Join-Path $repoRoot "StoryCAD\bin\x64\$Configuration\net10.0-windows10.0.22621\win-x64\StoryCAD.exe"
}
if (-not (Test-Path $ExePath)) { Write-Error "Exe not found: $ExePath - build the WinAppSDK head first."; exit 2 }

$binDir = Split-Path $ExePath -Parent
if (Test-Path (Join-Path $binDir '.env')) {
    Write-Error ".env present in $binDir - the app crashes at Shell_Loaded when launched unpackaged with .env and no debugger (AppState.DeveloperBuild evaluates Package.Current). Rename it away for the run, restore it after."
    exit 2
}
if (-not (Test-Path (Join-Path $binDir 'Preferences.json'))) {
    Write-Error "No Preferences.json in $binDir - seed it first (see PRECONDITIONS in this script's header) or the app opens on PreferencesInitialization and registers a junk backend user."
    exit 2
}

$existing = Get-Process StoryCAD -ErrorAction SilentlyContinue
if ($existing) {
    Write-Error "StoryCAD is already running (PID $($existing.Id -join ', ')). The app is single-instance; close it first so the probe gets its own instance."
    exit 2
}

# --- launch -------------------------------------------------------------------
$proc = Start-Process -FilePath $ExePath -PassThru
try {
    $waited = 0
    while ($true) {
        Start-Sleep -Milliseconds 250
        $waited += 250
        $proc.Refresh()
        if ($proc.HasExited) { Write-Error "StoryCAD exited before showing a window (code $($proc.ExitCode))."; exit 2 }
        if ($proc.MainWindowHandle -ne 0) { break }
        if ($waited -ge ($WindowTimeoutSeconds * 1000)) { throw "Timed out waiting for main window." }
    }
    Write-Host "STEP | main window up after $($waited)ms (PID $($proc.Id))"
    Start-Sleep -Seconds 5   # let XAML finish loading and startup dialogs settle

    # --- get the file-open menu on screen ---------------------------------------
    $samplesNav = Find-InProcess $proc.Id (ByName 'Sample outlines') -TimeoutSec 8
    if (-not $samplesNav) {
        Log-Liveness $proc 'samples-nav search failed'
        Write-Host "STEP | file-open menu not auto-shown; driving File > Open/create"
        $fileMenuBtn = Find-InProcess $proc.Id (ById 'FileMenuButton') -TimeoutSec 10
        if (-not $fileMenuBtn) {
            Log-Liveness $proc 'FileMenuButton search failed'
            Write-Host "DUMP | could not find FileMenuButton; window tree follows"
            foreach ($w in (Get-ProcessWindows -ProcId $proc.Id)) { Dump-Tree $w 0 3 }
            exit 2
        }
        Invoke-El $fileMenuBtn
        $openItem = Find-InProcess $proc.Id (ById 'OpenCreateFileMenuItem') -TimeoutSec 5
        if (-not $openItem) { throw "OpenCreateFileMenuItem not found after opening File menu." }
        Invoke-El $openItem
        $samplesNav = Find-InProcess $proc.Id (ByName 'Sample outlines') -TimeoutSec 10
        if (-not $samplesNav) { throw "File-open menu never appeared. A blocking ContentDialog (key-file warning, changelog, help) is the usual cause - see PRECONDITIONS." }
    }
    Write-Host "STEP | file-open menu visible; selecting Sample outlines tab"
    Select-El $samplesNav
    Start-Sleep -Milliseconds 600

    $sample = Find-InProcess $proc.Id (ByName 'Danger Calls') -TimeoutSec 8
    if (-not $sample) { throw "'Danger Calls' sample not found in samples list." }
    Select-El $sample
    Start-Sleep -Milliseconds 400

    $openBtn = Find-InProcess $proc.Id (ByName 'Open sample') -TimeoutSec 5
    if (-not $openBtn) { throw "'Open sample' button not found." }
    Invoke-El $openBtn
    Write-Host "STEP | opened Danger Calls sample; waiting for OverviewPage"

    # --- OverviewPage probes -----------------------------------------------------
    $storyIdea = Find-InProcess $proc.Id (ById 'StoryIdeaRichEdit') -TimeoutSec 12
    if (-not $storyIdea) {
        # Overview may not be the selected node; click the tree root.
        Write-Host "STEP | StoryIdeaRichEdit not visible yet; selecting tree root"
        $tree = Find-InProcess $proc.Id (ById 'NavigationTree') -TimeoutSec 10
        if ($tree) {
            $walker = [System.Windows.Automation.TreeWalker]::ControlViewWalker
            $rootItem = $walker.GetFirstChild($tree)
            if ($rootItem) { Select-El $rootItem }
        }
        $storyIdea = Find-InProcess $proc.Id (ById 'StoryIdeaRichEdit') -TimeoutSec 12
    }

    Report 'StoryIdeaRichEdit (Header="Story Idea")' $storyIdea
    Report 'DateCreatedTextBox (Header="Date Created")' (Find-InProcess $proc.Id (ById 'DateCreatedTextBox') -TimeoutSec 5)
    Report 'LastChangedTextBox (Header="Last Changed")' (Find-InProcess $proc.Id (ById 'LastChangedTextBox') -TimeoutSec 5)

    # --- PreferencesDialog probes --------------------------------------------------
    Write-Host "STEP | opening Preferences"
    $prefsBtn = Find-InProcess $proc.Id (ById 'PreferencesButton') -TimeoutSec 10
    if (-not $prefsBtn) { throw "PreferencesButton not found." }
    Invoke-El $prefsBtn

    $saveLocTab = Find-InProcess $proc.Id (ByName 'Save Locations') -TimeoutSec 10
    if (-not $saveLocTab) { throw "'Save Locations' tab not found in Preferences." }
    Select-El $saveLocTab
    Start-Sleep -Milliseconds 800

    # Both BrowseTextBox instances expose the SAME AutomationId ('PathTextBox', from the
    # x:Name inside the UserControl) - the HomePage duplicate-Browse-button problem again.
    $pathBoxes = FindAll-InProcess $proc.Id (ById 'PathTextBox')
    if ($pathBoxes.Count -eq 0) {
        Write-Host "RESULT | BrowseTextBox PathTextBox | NOT FOUND"
    } else {
        $i = 0
        foreach ($box in $pathBoxes) {
            Report ("BrowseTextBox inner PathTextBox #{0}" -f ++$i) $box
        }
    }

    Write-Host "STEP | probe complete"
    exit 0
}
finally {
    if ($proc -and -not $proc.HasExited) {
        Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
    }
}
