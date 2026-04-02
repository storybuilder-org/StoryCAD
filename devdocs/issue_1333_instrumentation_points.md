# Issue 1333: Telemetry Instrumentation Points

**Date**: 2026-04-02
**Purpose**: Map where usage telemetry hooks would be placed in the StoryCAD codebase, independent of collection approach (OTel vs custom).

---

## Session Lifecycle

| Event | File | Method | Line | What to Capture |
|-------|------|--------|------|-----------------|
| Session start | `App.xaml.cs` | `OnLaunched()` | 134 | Timestamp; already calls `BackendService.StartupRecording()` |
| Session end | `OutlineViewModel.cs` | `ExitApp()` | 565 | Timestamp, compute duration |

`BackendService.StartupRecording()` (`BackendService.cs:64`) already posts user/version data to MySQL on launch. A matching shutdown hook in `ExitApp()` would close the loop.

---

## Outline Lifecycle

| Event | File | Method | Line | What to Capture |
|-------|------|--------|------|-----------------|
| Open file | `FileOpenService.cs` | `OpenFile()` | 52 | Outline GUID, genre, story form, element counts |
| Create file | `FileCreateService.cs` | `CreateFile()` | 47 | Same (new outline) |
| Close file | `OutlineViewModel.cs` | `CloseFile()` | 519 | Duration open, element delta since open |
| Save file | `OutlineViewModel.cs` | `SaveFile()` | 357 | Could snapshot current element counts |

Genre and story form live on the Story Overview element. Element counts can be derived from `StoryModel.StoryElements`.

---

## Element Operations

| Event | File | Method | Line | What to Capture |
|-------|------|--------|------|-----------------|
| Add element | `OutlineViewModel.cs` | `AddStoryElement()` | 1085 | Element type (Character, Scene, Problem, etc.) |
| Delete element | `OutlineViewModel.cs` | `RemoveStoryElement()` | 1126 | Element type, count of children |
| Restore from trash | `OutlineViewModel.cs` | `RestoreStoryElement()` | 1249 | Element type |
| Move element | `ShellViewModel.cs` | `MoveTreeViewItem*()` | 1023-1322 | Could count rearrangements |

---

## Feature/Tool Usage

| Event | File | Method | Line |
|-------|------|--------|------|
| Collaborator (AI) | `ShellViewModel.cs` | `LaunchCollaborator()` | 985 |
| Key Questions | `OutlineViewModel.cs` | `KeyQuestionsTool()` | 788 |
| Topics | `OutlineViewModel.cs` | `TopicsTool()` | 813 |
| Master Plots | `OutlineViewModel.cs` | `MasterPlotTool()` | 838 |
| Dramatic Situations | `OutlineViewModel.cs` | `DramaticSituationsTool()` | 912 |
| Stock Scenes | `OutlineViewModel.cs` | `StockScenesTool()` | 1003 |
| Scrivener Export | `OutlineViewModel.cs` | `GenerateScrivenerReports()` | 735 |
| Print Report | `OutlineViewModel.cs` | `PrintCurrentNodeAsync()` | 771 |
| Search | `SearchService.cs` | `SearchString()` | 33 |

---

## Existing Infrastructure to Build On

| Component | File | Role |
|-----------|------|------|
| `BackendService` | `BackendService.cs` | Startup telemetry (user, preferences, version) via `IMySqlIo` |
| `IMySqlIo` | `IMySqlIo.cs` | MySQL interface — new methods would follow existing stored procedure pattern |
| `PreferencesModel` | Various | Consent flags (`ErrorCollectionConsent`, `Newsletter`) — add `UsageStatsConsent` |
| Doppler | `App.xaml.cs:143` | Already manages MySQL connection string |

---

## Wiring Pattern

Regardless of OTel vs custom, the instrumentation points are the same. The difference is what happens at those points:

- **Custom**: Accumulate counts in memory → flush to MySQL via `IMySqlIo` at session end
- **OTel**: Increment metrics/record values → OTel SDK exports to a backend periodically

A `UsageTrackingService` would be called from the methods above (e.g., `_usageTracking.RecordFeatureUse("MasterPlots")`), gated on consent, and responsible for flushing data via whichever backend is chosen.
