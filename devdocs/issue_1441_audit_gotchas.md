# Issue #1441 audit gotchas

Briefing for whoever runs the #1441 post-annotation accessibility audit: scripted UIA probes, axe scans, Inspect checks, and the write-up of findings. Compiled 2026-07-05 from the twelve merged #1420 PRs (#1442, #1443, #1444, #1445, #1447, #1450, #1451, #1453, #1455, #1456, #1457, #1458), `devdocs/issue_1420_status_log.md`, `devdocs/automation_naming_convention.md`, the two probe scripts under `devdocs/tools/`, and `StoryCADWiki/wiki/repos/StoryCAD/topics/ui-automation-accessibility.md`.

Two standing rules before reporting anything:

1. Check every candidate finding against sections 5 and 6. Most "defects" a scan surfaces here are either already filed or deliberate rulings.
2. Name the page from the live UIA tree, not from assumption. The one misattributed finding in the #1420 record (PR #1445 blamed HomePage for a duplicate-Browse-button finding; HomePage contains a single Image and zero interactive controls, and the scan was actually looking at the first-run wizard, corrected in PR #1453) came from assuming which page was open.

## 1. Launch and environment preconditions

- **The #1448 unpackaged-launch crash is fixed.** PR #1459 (merged 2026-07-05, commit 18e61239) wrapped the `Package.Current` revision check in `AppState.DeveloperBuild` in try/catch; unpackaged launches with `.env` present no longer die at `Shell_Loaded`. Text written before that date, including the `uia_header_probe.ps1` header and the #1422 handoff comment, still describes the crash as live.
- **Still rename `.env` away for probe runs.** `uia_header_probe.ps1` pre-flight exits 2 if `.env` sits in the bin directory, and the reason beyond the old crash still holds: with `.env` present an uninitialized run registers a junk user on the production backend (the 2026-07-04 probe run registered userId 3015, all consents false). Restore the file after the run.
- **Seed `Preferences.json` before first launch** or the app opens on the PreferencesInitialization wizard instead of Shell. Required keys per the probe header: `"Initialized": true`, `"Version"` matching the StoryCADLib assembly version (suppresses the changelog dialog), `"ShowStartupDialog": false`, `"HideKeyFileWarning": true`, `"ShowFilePickerOnStartup": true`, plus real OutlineDirectory/BackupDirectory paths.
- **StoryCAD is single-instance.** A leftover process swallows the next launch; the probe refuses to start (exit 2) if one is running. Kill strays first.
- **WinUI allows one ContentDialog at a time.** Any open dialog (key-file warning, changelog, help) makes the file-open menu close instantly. "File-open menu never appeared" usually means a blocking ContentDialog, not a navigation failure.
- **An uninitialized machine scans the wizard, not HomePage.** `axe_scan.ps1` has no click-through logic; on a machine without `Preferences.json` it scans PreferencesInitialization. This is the root of the misattributed PR #1445 finding above.

## 2. Tooling

- **AxeWindowsCLI is not a dotnet tool and not on NuGet.** It ships as an MSI (admin, installs under `C:\Program Files (x86)\AxeWindowsCLI\<version>\`) or a self-contained zip from the microsoft/axe-windows GitHub releases; v2.4.2 verified both ways. Unit 3 used a zip install at `%LOCALAPPDATA%\AxeWindowsCLI`. `axe_scan.ps1` searches: `-AxeWindowsCliPath` param, `$env:AXE_WINDOWS_CLI_PATH`, PATH, then the MSI default.
- **Exit codes are the whole verdict.** 0 = scan completed, zero findings. 1 = completed with findings. 2 = no verdict reached (couldn't attach, no UIA tree, exception, or any `axe_scan.ps1` pre-flight failure). 3 = third-party-notices flag. 255 = bad CLI parameters. No output parsing needed. The `.a11ytest` file (a zip the Accessibility Insights GUI opens) is only written when findings exist unless `-AlwaysSaveTestFile` is passed.
- **`axe_scan.ps1` scans the whole live process tree of whatever page is open.** Per-page scans require driving navigation first. Unit 3 combined the probe and the scan into a driver that walked Overview, Problem, Character, Scene, and Setting in one launch, but that driver lived in a session scratchpad and was not committed; only `uia_header_probe.ps1` and `axe_scan.ps1` are in the repo.
- **`uia_header_probe.ps1` requires Windows PowerShell 5.1** (GAC UIAutomationClient assemblies): `powershell.exe -ExecutionPolicy Bypass -File devdocs\tools\uia_header_probe.ps1`. Exit codes: 0 all probes reported, 1 a navigation step threw, 2 pre-flight failure.
- **PowerShell 5.1 encoding traps.** 5.1 misreads BOM-less non-ASCII files as CP1252 and treats the resulting curly quotes as string terminators; the tool scripts are kept pure ASCII for this reason. Piping `gh` output through `Set-Content -NoNewline` concatenates lines without separators (this once mangled the #1420 issue body; it was restored from a backup). MSYS `/tmp` paths inside heredoc Python strings do not resolve for Windows Python.

## 3. Platform behaviors that change what UIA reports

Misread any of these and the finding is wrong.

- **A WinUI 3 Button with panel Content announces a blank Name.** WinUI does not aggregate a TextBlock inside StackPanel Content into the Button's UIA Name. Proven live on StoryWorldPage: `AddWorldButton`/`RemoveWorldButton` announced `Name=''` despite visible text. Fixed in commit 8f8e3f12 with explicit Names on all ten Add/Remove buttons; the convention now requires explicit Names on panel-content Buttons.
- **`Setter.Value` never evaluates bindings.** An `AutomationProperties.*` binding in an `ItemContainerStyle` Setter is legal XAML that silently does nothing (no error, no warning). All template Name bindings are inline on the template element; the `SetterSafety` convention test fails any in-scope violation.
- **ItemsRepeater creates no automation peer.** The XAML ids `NavigationTree`/`TrashTree` never surface at runtime; the nav "tree" renders as TreeItems inside two Tree controls whose AutomationId is `ListControl`. A probe that searches for `NavigationTree` finds nothing and that is not a regression.
- **On nav-tree rows, Invoke navigates; selection does not.** The root outline row exposes only ExpandCollapse (no Invoke, no SelectionItem); child rows expose all three. After opening a file the app lands on OverviewPage.
- **Collapsed Expanders keep their content out of the UIA tree.** Expand before probing or typing inside. Expander AutomationIds work as live ExpandCollapse handles (e.g. `GeographyExpander`); the Expander itself announces its framework-derived header text ("Geography", ControlType Group) with no explicit Name.
- **Header text reaches the UIA Name for the TextBox family only; ListView.Header is unprobed.** Verified live 2026-07-04 (dev @ 3a143319, WinAppSDK head): `StoryIdeaRichEdit` announces "Story Idea", `DateCreatedTextBox` "Date Created", `LastChangedTextBox` "Last Changed", both BrowseTextBox inner PathTextBoxes their Header text. Whether `ListView.Header` derives a Name was never tested; StockScenesDialog's list got a pinned `Name="Scenes"` because of that gap. If Header derivation does turn out to work there, watch for a doubled name.
- **`x:Bind` defaults to OneTime.** RelationshipView's `{x:Bind Partner.Name}` needed an explicit `Mode=OneWay` (fixed in 4feee9ae). A name that never updates may be a missing Mode, not a missing annotation.
- **Some NavigationViewItems expose Invoke but not SelectionItem.** The probe's `Select-El` falls back to Invoke on exception for exactly this.
- **Literal `AutomationProperties.Name` inside an `x:Load` subtree breaks the desktop-head build (#1452).** Uno's XAML generator emits a phantom `_{value}Subject` field reference (CS1061; "Notes" produced `_NotesSubject`). AutomationId is unaffected; the WinAppSDK head is unaffected; bound Names in deferred subtrees are untested. Consequence: FolderPage's tabbed Notes RichEdit ships id-only as a standing exception until #1452 closes.

## 4. Runtime-only names and ids (the live-check list)

None of these are visible to static XAML tests; each needs a live Inspect or probe. This is the core #1441 verification work handed off from #1420.

- **Six BrowseTextBox derived id pairs.** A `Loaded` handler in `BrowseTextBox.xaml.cs` derives `{outerId}PathTextBox` / `{outerId}BrowseButton` from each instance's outer AutomationId. Expect e.g. `PreferencesProjectDirTextBoxPathTextBox`, not bare `PathTextBox`, on the six instances (BackupNow, SaveAsDialog, PreferencesDialog x2, PreferencesInitialization x2). The XAML keeps the literal fallback ids `PathTextBox`/`BrowseButton` because the Literalness test requires them; seeing those in the file is not a bug.
- **WorkflowPage's two templated Name bindings need the Collaborator sidecar repo** (`../Collaborator/`) to launch, and have never been probed live. Property Updates rows bind `{Binding Key, Mode=OneWay}`; chat rows bind `{Binding Text, Mode=OneWay}` (deliberately not Sender, which has only two values app-wide).
- **`PreferencesAppStoreReviewButton` has no explicit Name** on the theory that its OneTime-bound Content resolves to a plain string before the AutomationPeer reads it. Unverified; one live Inspect settles it.
- **PrintReportsDialog's five ListViews have no ItemTemplate at all.** Items render via default framework conversion; whether realized items announce their display text is an open live check.
- **AdminMessagePage's `MessageLink`** gets its Content from code-behind; a static scan sees no name source, but the runtime Content is plain text.
- **FeedbackDialog's two bound field Headers are Mode=TwoWay** and update the UIA Name live when `ChangeUIText` runs.

## 5. Known findings: do not re-report

- **Nav-pane TreeItems fail Section 508 SizeOfSet/PositionInSet on every page.** The single finding every Unit 3 AxeWindowsCLI run produced (Overview+Shell, Problem, Character, Scene, Setting). Shell-level, pre-existing, inherited by #1441. Not a per-page regression.
- **ScenePage's two Conflict-tab Feelings combos announce identically.** `ProtagonistFeelingsCombo` and `AntagonistFeelingsCombo` (ScenePage.xaml:255 and :284) both carry the framework-derived name "Feelings". The convention forbids overriding a visible Header, so the fix is a UI-text change, not an annotation. Note: at least one working document misplaced these on ProblemPage; they are on ScenePage, per PR #1450.
- **FolderPage's tabbed Notes RichEdit is id-only until #1452** (the x:Load bug in section 3). Documented standing exception, not a missed annotation.
- **No full Accessibility Insights Assessment pass has ever run**, and the founder FastPass/Narrator checks for Units 1-3 were never performed (the per-unit step was dropped from Unit 4 on under the cost directive). That debt is the audit itself, not a finding the audit reports.
- **Open spin-offs:** #1446 (the "Adventureousness" trait misspelling; the derived `AdventureousnessCombo` id follows the label verbatim and renames with the fix, so a probe hardcoding the correct spelling misses), #1449 (dead nested ListView in ScenePage's Cast tab item template), #1452 (Uno x:Load bug), #1454 (FeedbackDialog title header stays "Issue Title" in Feature Request mode while `ChangeUIText` switches the other two headers). #1448 is closed via PR #1459.
- **DramaticSituations ComboBox announces singular "Dramatic Situation"** against the dialog's plural title; ruled not-a-defect (the control selects one situation).

## 6. Deliberate rulings that look like defects

Convention law from `devdocs/automation_naming_convention.md` and founder rulings recorded in the PR threads.

- **The Exit menu item's accessible name is "Quit"** (PR #1444). Windows Narrator announcing "Quit" under the visible "Exit" label is the ruling working as intended; ADR-001 bans a conditional per-platform Name and the id stays `ExitMenuItem`.
- **The WorkflowPage chat input is `Name="Message"`** (PR #1458): the label-less-input ruling gives inputs with no Header, no PlaceholderText, and no sibling label a function-derived literal Name.
- **ListView Name ruling** (PRs #1457/#1458): a Header-bearing list a user must locate pins the header text (`StockScenesList` → "Scenes"); a header-less pickable list pins a function name (both ElementPicker ListViews → "Elements"); id-only remains correct where the container announcement carries no information (FileOpenMenu lists, PrintReportsDialog's five report lists).
- **LabeledBy is retired app-wide** (PR #1457). Zero `AutomationProperties.LabeledBy` attributes exist; sibling-TextBlock-labeled controls carry mirrored literal Names with punctuation stripped ("Target:" → `Target`). Known cost: a reworded label with a stale mirrored Name is invisible to static tests. StoryWorldPage's 43 RichEdit Names mirror their Expander header text under the same ruling; check for drift, not for the pattern itself.
- **Id-only by design:** the four InfoBars (`FeedbackTipInfoBar`, `FileOpenWarningInfoBar`, `HelpVideoNoteInfoBar`, `PrintReportsSynopsisWarningInfoBar`; the peer announces Severity + Title + Message natively) and the FileOpenMenu footer `FileOpenShowOnStartupNavItem` (the hosted CheckBox carries the name; a host Name would double-announce).
- **Never duplicate a framework-derived name with an explicit Name.** Do-not-annotate list: decorative icons in labeled parents, separators, layout containers, content TextBlocks.
- **Some AutomationIds deliberately diverge from x:Name:** `AssignBeatButton`/`UnassignBeatButton` vs x:Names `AssignButton`/`UnassignButton`; `WorldTabs` vs `x:Name="WorldTabView"`. A probe matching on x:Name misses these.
- **Cross-page prefix map:** ProblemPage prefixes its collision-prone combos (`ProblemProtagonistCombo` etc.) so ScenePage's Conflict tab holds the plain ids; OverviewPage prefixes (`OverviewNotesRichEdit`, `OverviewViewpointCharacterCombo`, ...) so ScenePage's `ViewpointCharacterCombo` is bare; Shell owns `MoveUpButton`/`MoveDownButton` so tool dialogs use `CopyElementsMoveUpButton`/`NarrativeMoveUpButton`; Collaborator uses `Workflow`/`CollabPicker` prefixes against Shell's `HelpButton`/`SaveStatusButton`/`ExitMenuItem` and the near-clone `ElementPicker` in `Services/Dialogs/`.

## 7. Static-test blind spots

What the seven `AutomationConventionTests` (in `StoryCADTests/Xaml/AutomationConventionTests.cs`; XDocument-based, no UI, run in CI) cannot see.

- **They cannot verify a single spoken name.** Framework-derived Header/Content names and runtime-resolved bindings are exactly the audit's territory.
- **The Literalness test bans bound or derived ids** (no `{`, ASCII letters and digits only), which is why BrowseTextBox's derived runtime ids are paired with literal XAML fallbacks. Derived ids are structurally invisible to the suite.
- **Elements inside any DataTemplate are excluded from the coverage test.** Templated TreeViewItems (Shell x3, NarrativeTool x2) carry no AutomationId at all and bind their Name; that is per ruling, not a gap.
- **Control types must be enrolled to be checked.** Expander (Unit 4), NavigationView/NavigationViewItem (Unit 6), InfoBar (Unit 7), and TreeViewItem (Unit 8) were each invisible to the coverage test until ruled into the convention's suffix table. A control type absent from that table may be legitimately unannotated pending a ruling; it may also be a real gap. Check the table before judging.
- **Since PR #1458 the coverage test scans all 39 scope files** (the ratchet's `AnnotatedFiles` list is deleted); any new interactive control fails the suite until annotated. Scope is 39 files, not the 40 in early plan text, and per-unit plan estimates of id counts were wrong every time; validate against the XAML, never against plan numbers. Total shipped: 591 ids across 36 files.
- **HomePage, TrashCanPage, and CollaboratorHostRoot contain nothing interactive.** Zero findings on them is correct behavior, not a scan failure.

## 8. Probe recipes

From `uia_header_probe.ps1` and the PR threads; all verified live except where noted.

- **Open a sample:** find `Sample outlines` by Name (or drive `FileMenuButton` → `OpenCreateFileMenuItem` by id), select `Danger Calls` (or `Land of Oz`), invoke `Open sample`. Caveat: the probe's fallback that selects the tree root via `NavigationTree` predates the ItemsRepeater finding in section 3; at runtime the tree ids surface as `ListControl`.
- **Reach StoryWorldPage:** open the Land of Oz sample (it contains the one StoryWorld element), Invoke the "Story World" tree row, select the Physical Worlds tab.
- **Reach Preferences:** `PreferencesButton` by id, then the `Save Locations` tab by Name.
- **Type into RichEdits:** expand the enclosing Expander first (section 3); collapsed content is absent from the UIA tree.
