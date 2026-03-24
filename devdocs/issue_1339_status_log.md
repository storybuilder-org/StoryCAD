# Issue #1339: Beat Sheet Usability — Status Log

## Known Defects

### 1. ~~Button icons clipped horizontally and vertically~~ — FIXED

### 2. "This will clear selected story beats" dialog fires on Problem tab navigation — ACTIVE
- **Severity**: Medium — confusing UX, could cause accidental data loss
- **Location**: `UpdateSelectedBeat` ComboBox SelectionChanged handler
- **Symptom**: Dialog appears when navigating to/selecting a problem, even though the user is on the Problem tab, not the Structure tab.
- **Cause**: ComboBox SelectionChanged fires during navigation/data binding, not just from user interaction.
- **Screenshot**: `/mnt/c/temp/this_will_clear_on_select_problem.png`

### 3. ~~Missing beat assignment validation — circular and Story Problem assignment~~ — FIXED

### 4. ~~Reassigning a problem doesn't clear the old beat assignment~~ — FIXED

### 5. No way to edit beat title inline — DEFERRED

## Completed Work

- Phase 0: Characterization tests
- Phase 1: BeatEditorViewModel extraction, StructureBeat rename
- Phase 2: All beat sheets editable, delete confirmation
- Phase 3: UI improvements (buttons, icons, layout)
- Phase 4: Reports (detail, unassigned, plot diagram)
- Phase 5: User manual rewrite
- Button review: All 7 button methods reviewed
- AssignBeat bug fix: delegation to OutlineService
- CreateBeat guard: status message when no beat sheet selected
- Element Description: TextBox → RichEditBoxExtended for RTF rendering
- Rebased onto issue-1358 SceneModel.Description fix
