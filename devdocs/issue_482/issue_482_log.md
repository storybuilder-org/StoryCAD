# Issue #482 - Copy Characters and Settings Log

**Issue:** [StoryCAD #482 - Copy Characters and Settings](https://github.com/storybuilder-org/StoryCAD/issues/482)
**Log Started:** 2026-01-23
**Status:** Planning
**Branch:** `issue-482-copy-elements`

---

## Context

This issue is a dependency for Issue #782 (StoryWorld). The StoryWorld user documentation references copying worldbuilding elements between series outlines, which requires this feature to exist.

## Copyable Element Types (Decided)

| Type | Copyable | Notes |
|------|----------|-------|
| Character | ✅ Yes | Series recurring characters |
| Setting | ✅ Yes | Series recurring locations |
| StoryWorld | ✅ Yes | Primary use case for series/shared worlds |
| Problem | ✅ Yes | Series often inherit problems - document considerations |
| Notes | ✅ Yes | Research and notes transfer |
| Web | ✅ Yes | Webpage references |
| StoryOverview | ❌ No | Singleton, story-specific |
| Scene | ❌ No | Story-specific plot content |
| Folder | ❌ No | Organizational only |
| Section | ❌ No | Organizational (Narrator view) |
| TrashCan | ❌ No | System |
| Unknown | ❌ No | System |

## Key Technical Challenges

1. **Two Open Outlines**: StoryCAD currently has a 'current StoryModel' design - needs to support reading a second outline
2. **Deep Copy**: Elements may have references to other elements (e.g., Character relationships) - need to handle or document
3. **Singleton Handling**: StoryWorld is singleton - what happens if destination already has one?

---

## Log Entries

### 2026-01-23 - Session 1: Setup

**Participants:** User (Terry), Claude Code

**Context:** Pausing #782 review work to address #482 dependency.

#### Actions Taken

1. Created branch `issue-482-copy-elements`
2. Created `devdocs/issue_482/` folder
3. Moved session status file to `devdocs/issue_482/session_status.md`
4. Updated GitHub issue with PIE checklist format
5. Created this log file

#### Next Steps

- Begin Design phase planning
- Research existing copy/paste or import/export patterns in codebase
- Design UI approach (ElementPicker-style dialog)

---

### 2026-01-23 - Session 1 (continued): Design Research

**Participants:** User (Terry), Claude Code

**Context:** Researching codebase patterns for the copy elements feature.

#### Research Completed (via Explore agents)

1. **StoryModel Management**
   - `AppState.CurrentDocument` holds exactly ONE story (single-file design)
   - `OutlineService.OpenFile(path)` returns a StoryModel with NO side effects on AppState
   - This means we CAN load a second file for copying without affecting the current document

2. **TreeView Patterns**
   - `NarrativeTool.xaml` has dual TreeView layout we can adapt
   - `StoryNodeItem.CopyToNarratorView()` is the copy pattern to adapt
   - Selection tracked via Tag property

3. **Dialog Patterns**
   - `Windowing.ShowContentDialog()` is standard approach
   - `ElementPicker` shows filtered elements by type (flattened list)
   - Page-based content inside ContentDialog

4. **SemanticKernelAPI**
   - Has `CurrentModel` property separate from `AppState.CurrentDocument`
   - Can use `outlineService.OpenFile(path)` directly to load source file

#### UI Design Discussion

**Layout (user-directed):**
```
┌─────────────────────┬───┬─────────────────────┐
│  DESTINATION (TO)   │   │   SOURCE (FROM)     │
│  Current Document   │ ← │   Opened File       │
│                     │ → │                     │
│  [View]             │ ↑ │   [View]            │
│                     │ ↓ │                     │
└─────────────────────┴───┴─────────────────────┘
```

- Left side: Current document (destination) - left-to-right reading order
- Right side: Source file to copy from
- Vertical button bar between:
  - ← Copy selected element from source to destination
  - → Remove element from destination (ONLY elements copied this session - prevents accidental deletion)
  - ↑ Navigate up in selected tree
  - ↓ Navigate down in selected tree

**View Options Discussed:**

| Approach | Pros | Cons |
|----------|------|------|
| Flattened List | Simple, easy filtering, like ElementPicker | Can't see hierarchy, harder to choose placement |
| Full TreeView | See structure, better placement control | Complex population, filters complicate things |

**Hybrid Idea (needs discussion):**
Flattened list but show parent path as context, e.g., "John Smith (Characters > Protagonists)"

**Key Implementation Notes:**
- Track copied elements in session list (for → button safety)
- User can bail (close without saving) at any time
- Filter to copyable types only: Character, Setting, StoryWorld, Problem, Notes, Web

#### Session Resumed - Design Decisions with Jake

**View Type Decision:** Flattened lists filtered by element type (like ElementPicker)

**Button Bar Simplified:**
- ← Copy selected from source to destination
- → Remove (only session-copied elements)
- ↑ Navigate up
- ↓ Navigate down

**NOT included:** Copy all, Add section/folder, Trash/Delete

**Template:** Clone NarrativeTool for layout, use ListViews instead of TreeViews

#### Design Plan Created

Full design documented in `devdocs/issue_482/design_plan.md`

Key decisions:
- Dual ListView layout with vertical button bar
- ComboBox filter for element type
- Session tracking via HashSet<Guid> for safe removal
- Cross-references cleared on copy (Guid.Empty)
- StoryWorld singleton enforced (block if exists)

---

### 2026-01-23 - Session 2: Code Phase 1 Implementation

**Participants:** User (Terry), Claude Code

**Context:** Implementing Phase 1 (Infrastructure) following TDD approach.

#### TDD Approach

1. **Red Phase:** Created 23 tests in `CopyElementsDialogVMTests.cs`
   - Constructor/DI tests
   - CopyableTypes tests
   - Initial state tests
   - Filter selection tests
   - Command existence tests
   - DialogTitle tests

2. **Green Phase:** Implemented `CopyElementsDialogVM.cs` to pass all tests
   - DI constructor with AppState, OutlineService, Windowing, ILogService
   - CopyableTypes list (Character, Setting, StoryWorld, Problem, Notes, Web)
   - ObservableCollections for SourceElements and TargetElements
   - RefreshSourceElements/RefreshTargetElements methods
   - All commands (Browse, Copy, Remove, MoveUp, MoveDown, Save, Cancel)
   - DialogTitle property with story name

3. **XAML:** Created `CopyElementsDialog.xaml` and minimal code-behind
   - Target file picker row (Browse button + TextBox)
   - Filter ComboBox bound to CopyableTypes
   - Dual ListView layout (Source left, Target right)
   - Vertical button bar (→, ←, ↑, ↓)
   - Status message display

4. **Shell Integration:**
   - Added menu item in Tools menu
   - Added CopyElementsCommand to ShellViewModel

#### Files Created
- `StoryCADTests/ViewModels/Tools/CopyElementsDialogVMTests.cs`
- `StoryCADLib/ViewModels/Tools/CopyElementsDialogVM.cs`
- `StoryCADLib/Services/Dialogs/Tools/CopyElementsDialog.xaml`
- `StoryCADLib/Services/Dialogs/Tools/CopyElementsDialog.xaml.cs`

#### Files Modified
- `StoryCADLib/Services/IoC/ServiceLocator.cs` - DI registration
- `StoryCAD/Views/Shell.xaml` - Menu item
- `StoryCADLib/ViewModels/ShellViewModel.cs` - Command

#### Test Results
- 725 tests pass (702 existing + 23 new)
- 14 skipped
- Build clean

#### Next Steps
- Phase 2: File Loading (BrowseTargetAsync implementation)
- UI Review checkpoint per design plan

---

### 2026-01-23 - Session 2 (continued): Phase 2 File Loading

**TDD Tests Added:**
- `LoadTargetFile_WithValidPath_SetsTargetModel`
- `LoadTargetFile_WithValidPath_UpdatesStatusMessage`
- `LoadTargetFile_WithInvalidPath_SetsErrorStatus`
- `LoadTargetFile_WithCurrentFile_BlocksAndShowsError`
- `RefreshTargetElements_AfterLoadingFile_PopulatesTargetList`

**Implementation:**
- `LoadTargetFileAsync(path)` - loads target via OutlineService.OpenFile()
- `BrowseTargetAsync` - file picker integration
- Validates: file exists, not current file

**Test Results:** 28 tests pass

---

### 2026-01-23 - Session 3: Phase 3 Copy Logic

**TDD Tests Added:**
- `CopyElement_WithSelectedCharacter_AddsToTargetModel`
- `CopyElement_CreatesNewUuid_NotSameAsSource`
- `CopyElement_IncrementsCopieedCount`
- `CopyElement_WithNoSelection_DoesNothing`
- `CopyElement_WithNoTargetLoaded_ShowsError`
- `CopyElement_StoryWorld_WhenTargetHasOne_BlocksAndShowsError`
- `CopyElement_TracksSessionCopiedIds`

**Implementation:**
- Added `SemanticKernelApi` dependency
- `CopyElement()` - copies via API's `AddElement`
- `IsSessionCopied(Guid)` - session tracking
- StoryWorld singleton enforcement

**Files Modified:**
- `ServiceLocator.cs` - registered SemanticKernelApi in DI

**Test Results:** 35 tests pass

---

### 2026-01-23 - Session 3 (continued): Phase 4 Remove and Navigation

**TDD Tests Added:**
- `RemoveElement_WithSessionCopiedElement_RemovesFromTarget`
- `RemoveElement_WithNonSessionElement_ShowsErrorAndDoesNotRemove`
- `RemoveElement_WithNoSelection_DoesNothing`
- `RemoveElement_DecrementsCopiedCount`
- `MoveUp_InSourceList_SelectsPreviousElement`
- `MoveDown_InSourceList_SelectsNextElement`
- `MoveUp_AtFirstElement_StaysAtFirst`
- `MoveDown_AtLastElement_StaysAtLast`

**Implementation:**
- `RemoveElement()` - removes session-copied elements only
- `MoveUp()` / `MoveDown()` - navigation in source list

**Test Results:** 43 tests pass

---

### 2026-01-23 - Session 3 (continued): Phase 5 Save and Edge Cases

**TDD Tests Added:**
- `SaveAsync_WithTargetLoaded_WritesTargetFile`
- `SaveAsync_WithNoTargetLoaded_DoesNotThrow`
- `SaveAsync_UpdatesStatusMessage`
- `Cancel_ResetsVMState`

**Implementation:**
- `SaveAsync()` - saves target via OutlineService.WriteModel()
- `Cancel()` - resets VM state

**Test Results:** 47 tests pass, 749 total tests pass

---

### Implementation Complete

All 5 code phases complete:
- Phase 1: Infrastructure ✅
- Phase 2: File Loading ✅
- Phase 3: Copy Logic ✅
- Phase 4: Remove and Navigation ✅
- Phase 5: Save and Edge Cases ✅

**Next:** Manual UI testing

---

### 2026-01-23 - Session 4: Manual Testing and Critical Bug Discovery

**Participants:** User (Terry), Claude Code

**Context:** Manual UI testing revealed critical bugs in the copy/save functionality.

#### Manual Test Performed

1. Opened source file (Land of Oz.stbx) in StoryCAD
2. Opened Copy Elements dialog from Tools menu
3. Browsed and selected target file (Danger Calls.stbx)
4. Selected StoryWorld filter, selected the Land of Oz StoryWorld
5. Clicked Copy - element appeared in target list
6. Clicked Save - dialog closed

**Result:** Target file (Danger Calls.stbx) could NOT be opened in StoryCAD afterwards.

#### Problem 1: File Corruption - "No Story Elements found"

When attempting to open the saved target file in StoryCAD:
- Error message: "Unable to open file (No Story Elements found)"
- The file IS being written (verified by opening .stbx in VS2026 as text)
- The StoryWorld element IS in the JSON
- But the file structure is corrupted - StoryCAD can't deserialize it

**Evidence:** Screenshot showed StoryWorld JSON was present in the file, but app couldn't load it.

#### Problem 2: Incomplete Property Copying

Opening the .stbx file as text in VS2026 showed:
- StoryWorld element WAS written to file
- GUID was preserved correctly
- **Structure tab properties were copied** (WorldType, Ontology, WorldRelation, etc.)
- **Other tab properties were EMPTY** (all the text fields, lists, etc.)

The source file (Land of Oz) had content across multiple tabs, but only simple string dropdown values from the Structure tab were copied.

#### Root Cause Analysis

**Critical User Guidance:** "Use ONLY the API for all target file operations"

The original implementation was flawed:
1. Initially used `OutlineService.OpenFile()` to load target - WRONG
2. Was swapping `_api.CurrentModel` back and forth - WRONG
3. Should use `_api.OpenOutline(path)` which sets API state for all subsequent calls

**Correct Pattern (user-directed):**
```
1. _api.OpenOutline(path) - sets _api.CurrentModel to target
2. _api.AddElement(...) - operates on CurrentModel (target)
3. _api.UpdateElementProperties(...) - operates on CurrentModel
4. _api.WriteOutline(path) - writes CurrentModel to disk
```

#### Fixes Applied

1. **LoadTargetFileAsync** - Changed to use `_api.OpenOutline(path)` instead of `_outlineService.OpenFile(path)`

2. **CopyElement** - Removed CurrentModel swapping, now just calls `_api.AddElement()` directly

3. **SaveAsync** - Removed CurrentModel swapping, now just calls `_api.WriteOutline(TargetFilePath)` directly

4. **Property Copying** - Added code to serialize source element and copy properties via `_api.UpdateElementProperties()`:
   ```csharp
   // Serialize source to get properties
   var sourceJson = SelectedSourceElement.Serialize();
   var properties = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(sourceJson);

   // Filter out system properties
   var skipProperties = new HashSet<string> { "Uuid", "Type", "Name", "Node", "Children", "Parent", "ElementType", "IsSelected", "IsExpanded" };

   // Convert and copy
   var propertiesToCopy = new Dictionary<string, object>();
   foreach (var kvp in properties) {
       if (skipProperties.Contains(kvp.Key)) continue;
       // Convert JsonElement to object based on ValueKind
       ...
   }
   _api.UpdateElementProperties(copiedUuid, propertiesToCopy);
   ```

#### Current Test Status

- 46 of 47 CopyElementsDialogVM tests pass
- 1 test fails: `SaveAsync_WithTargetLoaded_WritesTargetFile`
  - Element IS in api.CurrentModel (verified by diagnostic assertions)
  - File IS being written (modification time changes)
  - But reloaded file has 0 characters

#### Remaining Issues to Investigate

**Issue 1: File Structure Corruption**
- The saved file can't be loaded by StoryCAD
- Error: "No Story Elements found"
- The JSON contains the element, but the tree structure (ExplorerView, NarratorView, TrashView) may be broken
- When AddElement creates an element, it creates a node in the tree. Something about this isn't working correctly for the target model.

**Issue 2: Incomplete Property Copying**
- Only simple string properties (like dropdown values) are being copied
- RichEditBoxExtended content (RTF text fields) are NOT being copied
- Array properties are passed as raw JSON strings which may not work with UpdateElementProperty
- Need to investigate how RichEditBox/RTF properties are stored and how to properly pass them to the API

**User Note:** "What you copied is no different than what History or any other tab that allows just a single occurrence would be. What you're looking for is RichEditBoxExt properties."

#### Key Files Modified This Session

- `StoryCADLib/ViewModels/Tools/CopyElementsDialogVM.cs` - Main fixes
- `StoryCADTests/ViewModels/Tools/CopyElementsDialogVMTests.cs` - Added diagnostics

#### VS Test Explorer Issue (Unrelated)

Visual Studio Test Explorer shows COM exception: "Could not determine target device configuration"
- This is a known issue with MSTest 4.0.2 and WinAppSDK
- Tests run fine from command line: `vstest.console.exe`
- Not blocking - use command line for testing

#### Next Steps for New Session

1. **Debug file corruption issue first** - This is the critical blocker
   - The element is in the JSON but file won't load
   - Investigate if node tree structure is being set up correctly
   - May need to examine what AddElement does to the tree vs what serialization expects

2. **Then fix property copying**
   - Investigate how RichEditBoxExtended content is stored (RTF format?)
   - Check how UpdateElementProperty handles different property types
   - May need different handling for array/list properties vs simple strings

3. **Key constraint from user:** "Use ONLY the API" - don't dive into OutlineService, DAL, Models internals unless absolutely necessary

---

### 2026-01-24 - Session 5: Bug Fixes Complete

**Participants:** User (Terry), Claude Code

**Context:** Resolving the two bugs discovered in manual testing.

#### Bug 2 Fix: List/Array Properties Not Copying

**Root Cause:** In `CopyElement`, array properties were being passed to `UpdateElementProperties` as raw JSON strings (`GetRawText()`) instead of properly typed objects. `Convert.ChangeType` in `UpdateElementProperty` can't convert JSON strings to `List<PhysicalWorldEntry>` etc.

**Fix Applied:** In `CopyElementsDialogVM.CopyElement()`, for Array and Object properties, get the property type from the target element and deserialize to that type:

```csharp
case JsonValueKind.Array:
case JsonValueKind.Object:
    var propInfo = targetElement.GetType().GetProperty(kvp.Key);
    if (propInfo != null)
    {
        var jsonText = kvp.Value.GetRawText();
        value = JsonSerializer.Deserialize(jsonText, propInfo.PropertyType);
    }
    break;
```

**Verified:** Screenshots showed PhysicalWorlds, Species arrays with RTF content copied correctly.

#### Bug 1 Fix: File Corruption ("No Story Elements found")

**Root Cause:** When `AddElement` uses `GUIDOverride`, it calls `UpdateGuid()` to change the element's UUID. However, `UpdateGuid` only updated:
- The element's `Uuid` property
- The `StoryElementGuids` dictionary

**It did NOT update `Node.Uuid`!**

This caused a mismatch:
- `FlattenedExplorerView` contained the node's UUID (original)
- `Elements` contained the element's UUID (overridden)

On reload, `RebuildTree` couldn't match them, causing "No Story Elements found".

**Fix Applied:** In `StoryElement.UpdateGuid()`:

```csharp
internal void UpdateGuid(StoryModel model, Guid newGuid)
{
    model.StoryElements.StoryElementGuids.Remove(Uuid);
    Uuid = newGuid;
    model.StoryElements.StoryElementGuids.Add(newGuid, this);

    // Also update the node's UUID to stay in sync
    if (Node != null)
    {
        Node.Uuid = newGuid;
    }
}
```

#### Files Modified

- `StoryCADLib/Models/StoryElement.cs` - Added `Node.Uuid = newGuid` to `UpdateGuid()`
- `StoryCADLib/ViewModels/Tools/CopyElementsDialogVM.cs` - Fixed array deserialization, added Overview.Node null check

#### Test Results

- All 47 CopyElementsDialogVM tests pass
- All 749 tests pass (14 skipped)
- Manual testing confirmed:
  - File opens correctly after copy
  - StoryWorld appears in tree
  - All list tabs (Physical Worlds, Species, etc.) with RTF content copied correctly

#### Implementation Complete

Issue #482 Copy Elements feature is now fully functional:
- ✅ Phase 1: Infrastructure
- ✅ Phase 2: File Loading
- ✅ Phase 3: Copy Logic
- ✅ Phase 4: Remove and Navigation
- ✅ Phase 5: Save and Edge Cases
- ✅ Bug fixes for list properties and tree structure

---

*Log maintained by Claude Code sessions*
