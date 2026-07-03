# Issue #1420 implementation plan: AutomationProperties annotation pass

Written 2026-07-03 for founder review. Executes the design approved 2026-07-03 (issue #1420 design comment plus amendments comment). Written to be implementable by a Sonnet 5 agent working one unit at a time with this document, `devdocs/automation_naming_convention.md`, and the repository as its only required context.

## Ground rules for every unit

- Base branch is `dev`. Every unit branches from current `dev` as `issue-1420-batch{N}-{slug}` and PRs back to `dev`.
- Diffs are XAML attributes plus test code only. No behavior, logic, layout, or ViewModel changes. If a unit surfaces a code problem (for example an MVVM gap in a dialog), file a new issue; do not fix it in the batch PR.
- The convention document is law. Where it is silent, propose the resolution in the PR description rather than inventing silently.
- Every unit ends with: solution builds, full test suite green, Accessibility Insights FastPass on the touched views recorded in the PR description.

### Build and test commands (Windows, PowerShell)

```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" StoryCAD.sln -t:Build -p:Configuration=Debug -p:Platform=x64
& "C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe" "StoryCADTests\bin\x64\Debug\net10.0-windows10.0.22621\StoryCADTests.dll"
```

Run a single test class with `/Tests:AutomationConventionTests`. Accessibility Insights for Windows v1.1.2924.01 is installed at `C:\Users\tcox\AppData\Local\Programs\AccessibilityInsights\1.1\AccessibilityInsights.exe` (per-user).

## TDD structure

The test instrument is a set of static XAML-scanning tests: `StoryCADTests/Xaml/AutomationConventionTests.cs`. They parse the XAML source files as XML (`XDocument`); no UI, no `[UITestMethod]`, runs everywhere including CI. The batch cycle is genuinely red-green:

1. **Red**: add the batch's files to the `AnnotatedFiles` list in the test source. The coverage test now fails, reporting every unannotated interactive control by file and line.
2. **Green**: annotate until the coverage test passes. The other convention tests (template safety, suffix, uniqueness, literalness) must stay green throughout.
3. **Verify**: build, run the app, FastPass the touched views.

What these tests cannot see, and what covers it instead: whether the accessible name a screen reader speaks is correct (framework-derived `Header`/`Content` names, templated `Name` bindings resolving at runtime). That is covered by the per-batch FastPass, the PR 1 Inspect checks, and reviewer-agent scrutiny of naming quality.

### Test specifications

All tests live in `AutomationConventionTests`. Follow the house test naming pattern (`MethodName_Scenario_ExpectedResult` adapted to `Rule_Scope_Expectation`).

**Locating the XAML source**: at test run time, walk up from `AppContext.BaseDirectory` until a directory containing `StoryCAD.sln` is found; scan the 39 convention-scope files relative to that root. Fail with a clear message if the root is not found.

**Interactive element list** (local names, namespace prefixes ignored): `Button`, `AppBarButton`, `HyperlinkButton`, `MenuFlyoutItem`, `MenuFlyoutSubItem`, `ComboBox`, `TextBox`, `CheckBox`, `RadioButton`, `ToggleSwitch`, `NumberBox`, `AutoSuggestBox`, `TabView`, `TabViewItem`, `TreeView`, `ListView`, `GridView`, `Flyout`, `RichEditBoxExtended`, `BrowseTextBox`. Elements inside a `DataTemplate` (any ancestor) are excluded from the coverage requirement.

| Test | Scope | Assertion |
|---|---|---|
| `Coverage_AnnotatedFiles_EveryInteractiveControlHasAutomationId` | files in `AnnotatedFiles` | every interactive element outside a DataTemplate has a non-empty `AutomationProperties.AutomationId`; failure message lists file, line, element |
| `TemplateSafety_AllFiles_NoAutomationIdInsideDataTemplate` | all 39 files, from day one | no element with a DataTemplate ancestor carries AutomationId |
| `Suffix_AllFiles_AutomationIdEndsWithRoleSuffix` | any AutomationId present anywhere | value ends with the suffix mapped to its element type in the convention table |
| `Uniqueness_AllFiles_AutomationIdsGloballyUnique` | any AutomationId present anywhere | no value appears twice across all scanned files |
| `Literalness_AllFiles_AutomationIdIsLiteral` | any AutomationId present anywhere | value contains no `{` (no bindings), only ASCII letters and digits |

`AnnotatedFiles` starts as `{ Shell.xaml, OverviewPage.xaml }` in Unit 1 and grows by one batch per unit. By Unit 9 it holds all 39 files and the list can be replaced by "all scope files"; the coverage test then becomes the permanent fitness function the approved design requires.

## Units of work

Counts are dev-adjusted estimates from the 2026-07-03 survey delta; each unit re-checks its own files at annotation time and reports actuals in the PR description.

### Unit 0: this plan (already on branch `issue-1420-implementation-plan`)

`devdocs/issue_1420_implementation_plan.md` and `devdocs/automation_naming_convention.md`. Founder review of this PR is the Design "Human final approval" checkbox. Deviation from the approved design, flagged: the convention doc rides in this PR instead of PR 1; content is unchanged from what was approved, plus the three *(plan-stage addition)* items marked inside it.

### Unit 1 (PR 1): test scaffolding + Shell + OverviewPage (~97 controls)

Files: `StoryCAD/Views/Shell.xaml` (72), `StoryCAD/Views/OverviewPage.xaml` (25).

1. Write `AutomationConventionTests` per the specifications above, `AnnotatedFiles = { Shell.xaml, OverviewPage.xaml }`. Confirm coverage red, other four green (they pass vacuously or on existing state).
2. Annotate Shell: menu bar buttons and flyout items (Name with clean command text on every padded-shortcut MenuFlyoutItem), `NavigationTree`/`TrashTree` (container ids; `{x:Bind}` Name binding on template roots), status bar icon-only buttons (AutomationId plus Name), Move flyout arrow buttons (AutomationId plus Name), `ShellSearchBox` (AutomationId plus Name).
3. Annotate OverviewPage: `OverviewTabs`, per-tab `Tab` ids, field controls; page-prefix generic functions (`OverviewNotesRichEdit`).
4. Reuse existing `x:Name` strings as ids where they fit (33 exist codebase-wide).
5. Build; full suite green.
6. **Header-exposure check** (decides convention placement): run the app, open Accessibility Insights Inspect, examine a `RichEditBoxExtended` instance (Overview Story Idea field) and a `BrowseTextBox` instance; record whether `Header` reaches the Name property. If not: `BrowseTextBox` Name goes in its own XAML (Unit 5 does it); `RichEditBoxExtended` Name via `AutomationProperties.SetName` in its constructor, a two-line C# change, done in this PR and called out in the PR description as the one non-XAML edit.
7. **Narrator spot-check**: navigation tree announces node names; the three status-bar buttons announce their function.
8. FastPass Shell and OverviewPage; record finding counts. Pass bar: zero missing-name findings on annotated controls.

### Units 2 through 9: the batches

Same cycle every time: branch from `dev`; extend `AnnotatedFiles` (red); annotate per convention (green); build; full suite; FastPass touched views; PR with actual counts, FastPass results, and any proposed convention additions.

| Unit | Files | Est. controls | Notes |
|---|---|---|---|
| 2 | `ProblemPage.xaml` (43), `CharacterPage.xaml` (~62) | ~105 | CharacterPage gained an Images tab on dev. FlaUI harness issue (#1422 chain) unblocks after this merges |
| 3 | `ScenePage.xaml` (~38), `SettingPage.xaml` (~19) | ~57 | both gained an Images tab on dev |
| 4 | `StoryWorldPage.xaml` | 123 | largest single file; one file keeps review mechanical |
| 5 | `FolderPage.xaml` (~5), `WebPage.xaml` (5), `PreferencesInitialization.xaml` (11), `HomePage.xaml` (0), `TrashCanPage.xaml` (0); `StoryCADLib/Controls`: `BrowseTextBox` (2), `Conflict` (3), `Flaw` (2), `RelationshipView` (6), `Traits` (2), `ImageGalleryControl` (6) | ~42 | FolderPage gained a TabView on dev; ImageGalleryControl is new on dev (4 Buttons, GridView container id, caption TextBox); BrowseTextBox gets its Name here if Unit 1's check says so |
| 6 | `StoryCADLib/Services/Dialogs`: `AdminMessagePage` (1), `BackupNow` (2), `ElementPicker` (5), `FeedbackDialog` (~7), `FileOpenMenu` (9), `HelpPage` (7), `NewRelationshipPage` (4), `SaveAsDialog` (2) | ~37 | dialog-name id prefixes; FeedbackDialog grew on dev (#1427) |
| 7 | `PreferencesDialog.xaml` (48), `PrintReportsDialog.xaml` (26) | 74 | |
| 8 | `StoryCADLib/Services/Dialogs/Tools`: `CopyElementsDialog` (9), `DramaticSituationsDialog` (1), `KeyQuestionsDialog` (3), `MasterPlotsDialog` (1), `NarrativeTool` (10), `StockScenesDialog` (2), `TopicsDialog` (5) | 31 | |
| 9 | `StoryCADLib/Collaborator/Views`: `WorkflowShell` (5), `WorkflowPage` (10), `ElementPicker` (5), `CollaboratorHostRoot` (0) | 20 | WorkflowPage's data-bound workflow content takes templated-Name treatment; convert `AnnotatedFiles` to all-scope-files, making the coverage test the permanent fitness function |

### Optional spike (time-boxed, alongside Unit 2): scripted scans

Accessibility Insights' scan engine ships separately as Axe.Windows with a CLI (`AxeWindowsCLI`, NuGet). A script that launches StoryCAD, scans the process, and emits results would automate the recurring per-batch bar; manual FastPass stays authoritative per the approved design, and the audit under umbrella #1441 stays manual regardless. If the spike works in under a session, add `devdocs/tools/axe_scan.ps1` and use it from Unit 3 on; if not, drop it without ceremony.

## Naming decision procedure (for the implementing agent)

1. Identify the element type; get the suffix from the convention table.
2. Function name: from the control's `Header`/`Content`/`Label`/command text, PascalCased, punctuation and shortcut text stripped (`"Save Story  Ctrl+S"` → `SaveStory` → `SaveStoryMenuItem`). Icon-only controls: name the function (`SaveStatusButton`), not the glyph.
3. Collision check: if the bare function could exist on another page or dialog (Notes, Save, Cancel, Name, Description), prefix with the page or dialog name. When in doubt, prefix; the uniqueness test is the backstop.
4. Existing fitting `x:Name`: reuse the string verbatim.
5. Name attribute: only per the convention's Name rules. Never duplicate a framework-derived name.
6. Nothing on the do-not-annotate list gets attributes.

## Delegation and review protocol (for the orchestrating session)

- One implementation agent per unit (Sonnet 5), given: this plan, the convention doc, the unit's file list, and the ground rules. No annotation work in the main session.
- After each unit: a separate reviewer agent (or `/code-review`) checks the diff for convention conformance and naming quality, and specifically for AI-typical failures: ids derived from position rather than function, duplicated framework names, annotations sneaking into DataTemplates, non-attribute diff hunks.
- FastPass and Narrator checks are GUI steps: run in the main session or by the founder, never claimed by an agent that cannot run them.
- Founder merges each PR. After the final merge: update the wiki pages this touched and append a `log.md` entry per house rules; close out #1420's remaining checkboxes; notify umbrella #1441 that the audit trigger (six main views annotated) tripped at Unit 5.

## Definition of done

- All 39 scope files in the coverage test; suite green; the five convention tests permanent.
- ~586 controls annotated (actual count reported in the closing comment on #1420).
- FastPass on every view and dialog: zero missing-name findings on annotated controls.
- Narrator completes the Unit 1 spot-checks; the #1441 post-annotation audit is unblocked and its trigger noted there.
- No behavior, layout, or logic diffs anywhere in the nine batch PRs (the Unit 1 `RichEditBoxExtended` constructor exception, if taken, is the sole and documented exception).
