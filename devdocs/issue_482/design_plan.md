# Design Plan: Issue #482 - Copy Elements Dialog

**Status:** Design Complete - Awaiting Approval
**Date:** 2026-01-23

---

## 1. Overview

This design implements a dialog for copying story elements (Character, Setting, StoryWorld, Problem, Notes, Web) from the current outline to another outline. The primary use case is copying worldbuilding elements to other stories in a series.

---

## 2. UI Layout Design

### 2.1 Dialog Structure

Title includes current outline name for clarity.

```
+---------------------------------------------------------------+
| Copy Elements from "Story Title" to Another Outline       [X] |
+---------------------------------------------------------------+
| Target: [Browse...] [path/to/target.stbx                    ] |
+---------------------------------------------------------------+
| Filter: [Character v]                                         |
+---------------------------------------------------------------+
|                                                               |
| SOURCE (Current Outline) |     | TARGET (Opened File)         |
| ----------------------   | [→] | ----------------------        |
| [ListView]               | [←] | [ListView]                    |
|   - John Smith           | [↑] |   - Mary Jones                |
|   - Jane Doe             | [↓] |   - Bob Wilson                |
|   - ...                  |     |   - ...                       |
|                          |     |                               |
+---------------------------------------------------------------+
| Status: 2 elements copied this session      [Cancel] [Save]   |
+---------------------------------------------------------------+
```

### 2.2 Control Specifications

**Dialog Title:**
- Dynamic: `Copy Elements from "{StoryName}" to Another Outline`
- StoryName from Overview element or filename if unnamed

**Header Area:**
- Target file picker: Browse button + text field showing selected path
- Filter ComboBox with element types: Character, Setting, StoryWorld, Problem, Notes, Web

**Left Pane (Source - Current Outline):**
- ListView with single selection
- Items filtered by selected type
- DisplayMemberPath="Name"
- Shows elements from `AppState.CurrentDocument.Model`
- Read-only (cannot modify current outline from this dialog)

**Button Bar (Vertical, Center):**

| Button | Action | Notes |
|--------|--------|-------|
| → | Copy selected source element to target | Main action (left to right) |
| ← | Remove selected target element | ONLY elements copied this session |
| ↑ | Navigate up in selected list | |
| ↓ | Navigate down in selected list | |

**NOT included:** Copy all, Add section/folder, Trash/Delete

**Right Pane (Target - Opened File):**
- ListView with single selection
- Items filtered by selected type
- DisplayMemberPath="Name"
- Shows elements from loaded target StoryModel

**Footer:**
- Status message area
- Count of elements copied this session
- [Cancel] button - close without saving target file
- [Save] button - save target file and close

---

## 3. ViewModel Design

### 3.1 CopyElementsDialogVM

Location: `StoryCADLib/ViewModels/Tools/CopyElementsDialogVM.cs`

**Dependencies (injected):**
- AppState
- OutlineService
- Windowing
- ILogService

**Key Properties:**
- `TargetModel` - StoryModel loaded from target file
- `TargetFilePath` - Path display for target file
- `SelectedFilterType` - StoryItemType for filtering
- `SourceElements` - ObservableCollection for left list (current outline)
- `TargetElements` - ObservableCollection for right list (target file)
- `SelectedSourceElement` - Current selection (left, from current outline)
- `SelectedTargetElement` - Current selection (right, from target file)
- `StatusMessage` - Status text
- `CopiedCount` - Session copy count

**Session Tracking:**
- `_copiedElementIds` - HashSet<Guid> of elements copied this session
- Used to enable/disable ← button (remove only session-copied)

**Commands:**
- `BrowseTargetCommand` - Open file picker for target
- `CopyElementCommand` - → button (copy source to target)
- `RemoveElementCommand` - ← button (remove from target)
- `MoveUpCommand` - ↑ button
- `MoveDownCommand` - ↓ button
- `SaveCommand` - Save target file
- `CancelCommand` - Close without saving

**Filter Types:**
```csharp
public List<StoryItemType> CopyableTypes { get; } = new()
{
    StoryItemType.Character,
    StoryItemType.Setting,
    StoryItemType.StoryWorld,
    StoryItemType.Problem,
    StoryItemType.Notes,
    StoryItemType.Web
};
```

---

## 4. Copy Logic Design

### 4.1 Element Deep Copy Strategy

- Create new elements with NEW UUIDs
- Copy all properties
- Cross-references (like Problem.Protagonist) are CLEARED (set to Guid.Empty)
- Relationships are NOT copied (partner may not exist)

### 4.2 Type-Specific Handling

| Element Type | Cross-References | Handling |
|--------------|------------------|----------|
| Character | RelationshipList.PartnerUuid | Clear list |
| Problem | Protagonist, Antagonist | Set to Guid.Empty |
| Problem | StructureBeats[].BeatScene | Set to Guid.Empty |
| Setting | (none) | N/A |
| Notes | (none) | N/A |
| Web | (none) | N/A |
| StoryWorld | (none) | N/A |

### 4.3 StoryWorld Singleton Handling

- Only one StoryWorld allowed per story
- If destination already has StoryWorld, show warning and cancel copy
- User must manually delete existing StoryWorld first if they want to replace

---

## 5. Session Tracking

### 5.1 Purpose

Track which elements were copied during this dialog session:
1. **Safe removal**: Only session-copied elements can be removed via ← button
2. **Prevents accidents**: Cannot delete existing target elements
3. **Scope**: Closing dialog without saving discards all copies

### 5.2 Implementation

```csharp
private readonly HashSet<Guid> _copiedElementIds = new();

// On copy (→ button):
_copiedElementIds.Add(newElement.Uuid);

// On remove check (← button):
if (!_copiedElementIds.Contains(selectedElement.Uuid))
{
    StatusMessage = "Can only remove elements copied this session.";
    return;
}
```

---

## 6. File Handling

### 6.1 Loading Target File

Use existing `OutlineService.OpenFile(path)`:
- Returns StoryModel without affecting AppState.CurrentDocument
- Target file loaded into `TargetModel` property
- No side effects on current document

### 6.2 Saving Target File

On [Save] button:
- Use `OutlineService.WriteModel(TargetModel, TargetFilePath)`
- Save the target file with copied elements
- Current document is NOT modified

### 6.3 Validation

- Cannot select current file as target (would cause conflicts)
- Handle file not found
- Handle corrupt/invalid files
- Warn if target has unsaved changes before closing

---

## 7. Edge Cases

| Case | Handling |
|------|----------|
| Duplicate name | Append "(Copy)" or show warning |
| StoryWorld exists | Block copy, show message |
| Current file selected | Block, show message |
| Empty source | Show "No elements of this type" |
| Cross-references | Clear all (Guid.Empty) |

---

## 8. Files to Create/Modify

### 8.1 New Files

| File | Purpose |
|------|---------|
| `StoryCADLib/Services/Dialogs/Tools/CopyElementsDialog.xaml` | XAML layout |
| `StoryCADLib/Services/Dialogs/Tools/CopyElementsDialog.xaml.cs` | Code-behind |
| `StoryCADLib/ViewModels/Tools/CopyElementsDialogVM.cs` | ViewModel |

### 8.2 Files to Modify

| File | Change |
|------|--------|
| `StoryCADLib/Services/BootStrapper.cs` | Register CopyElementsDialogVM |
| `StoryCAD/Views/Shell.xaml` | Add menu item |
| `StoryCADLib/ViewModels/ShellViewModel.cs` | Add command |

---

## 9. Implementation Phases

### Phase 1: Infrastructure
- Create ViewModel with basic structure
- Create XAML with layout
- Register in DI
- Add shell menu item

### Phase 2: File Loading
- Implement BrowseSourceAsync
- Implement RefreshLists
- Test file picker

### Phase 3: Copy Logic
- Implement DeepCopyElement per type
- Implement CopyElementCommand
- Session tracking

### Phase 4: Remove and Navigation
- Implement RemoveElementCommand
- Implement MoveUp/MoveDown
- Test session constraint

### Phase 5: Edge Cases
- Duplicate detection
- StoryWorld singleton
- Status messages

---

## 10. Testing

### Unit Tests
- DeepCopyElement per type
- Cross-reference clearing
- Session tracking
- Duplicate detection

### Integration Tests
- File loading
- Element addition
- Singleton enforcement

### Manual Tests
- Copy each element type
- Remove constraint
- StoryWorld warning

---

## 11. Key Reference Files

| File | Pattern |
|------|---------|
| `StoryCADLib/Services/Dialogs/ElementPicker.xaml` | **Primary template** - ListView with ComboBox filter |
| `StoryCADLib/ViewModels/ElementPickerVM.cs` | ViewModel pattern for filtered lists |
| `NarrativeTool.xaml` | Dual-pane layout reference |
| `OutlineService.cs` | OpenFile() method |
| `CharacterModel.cs` | Property copying reference |

## 12. UI Review Checkpoint

**IMPORTANT:** After Phase 1 (Infrastructure) is complete, conduct a live UI review before proceeding:

1. Build and run with basic dialog shell
2. Verify layout looks correct
3. Test target file picker flow
4. Review with user before implementing copy logic

This prevents wasted effort if layout needs adjustment.
