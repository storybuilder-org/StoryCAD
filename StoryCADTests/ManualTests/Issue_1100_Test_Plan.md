# Issue 1100 Manual Test Plan
## Move StoryModel to AppState - Focused Testing

This test plan focuses on areas affected by moving StoryModel from OutlineViewModel to AppState.CurrentDocument.

---

## Critical Tests - File Operations & UI Updates

### TC-1100-001: Create New Story - UI Binding Update
**Priority:** Critical  
**Time:** ~2 minutes  
**Focus:** Verify tree view updates when creating new document

**Steps:**
1. Launch StoryCAD  
   **Expected:** Empty tree view displays
   
2. Click File > New Story  
   **Expected:** Tree view immediately shows "Untitled" Story Overview node
   
3. Enter "Test Story 1100" in Story Name field  
   **Expected:** Tree view updates in real-time to show "Test Story 1100"

4. Add a Character element  
   **Expected:** Character appears in tree view immediately

**Pass Criteria:** Tree view updates without requiring save or navigation

---

### TC-1100-002: Open Existing Story - Tree Display
**Priority:** Critical  
**Time:** ~2 minutes  
**Focus:** Verify tree populates correctly when loading file

**Steps:**
1. Click File > Open Sample Outline > Danger Calls  
   **Expected:** Tree view populates with all story elements immediately
   
2. Verify all nodes are visible  
   **Expected:** Can see Overview, Problems, Characters, Scenes in tree

3. Click on different nodes  
   **Expected:** Content pane updates for each selection

**Pass Criteria:** Tree displays all elements immediately after load

---

### TC-1100-003: Save and Auto-Save Operations
**Priority:** Critical  
**Time:** ~3 minutes  
**Focus:** Verify save operations work with new architecture

**Setup:**
- Enable Auto-save in Tools > Preferences (set to 30 seconds)

**Steps:**
1. Create new story with a character "SaveTest"  
   **Expected:** Status shows "Changed"
   
2. Click File > Save, save as "TC1100_Save.stbx"  
   **Expected:** File saves, "Changed" indicator clears
   
3. Edit character name to "SaveTest2"  
   **Expected:** "Changed" indicator appears
   
4. Wait 35 seconds for auto-save  
   **Expected:** "Changed" indicator clears automatically
   
5. Close and reopen file  
   **Expected:** Character name is "SaveTest2" (auto-save worked)

**Cleanup:**
- Disable auto-save after test

---

### TC-1100-004: Save As - Path Update
**Priority:** Critical  
**Time:** ~2 minutes  
**Focus:** Verify file path updates correctly

**Steps:**
1. Open existing file from TC-1100-003  
   **Expected:** Title bar shows "TC1100_Save.stbx"
   
2. Click File > Save As  
   **Expected:** Save dialog appears
   
3. Save as "TC1100_SaveAs.stbx"  
   **Expected:** Title bar updates to show new filename
   
4. Make a change and save (Ctrl+S)  
   **Expected:** Saves to new file, not original

**Pass Criteria:** File path correctly tracked after Save As

---

### TC-1100-005: Edit Flush on Page Navigation
**Priority:** Critical  
**Time:** ~3 minutes  
**Focus:** Verify edits save when switching between elements

**Steps:**
1. Open outline with multiple characters
2. Select first character, edit name to "EditTest1"  
   **DO NOT manually save**
   
3. Click on second character  
   **Expected:** First character's tree node shows "EditTest1"
   
4. Edit second character name to "EditTest2"
5. Click on a Scene element  
   **Expected:** Second character's tree node shows "EditTest2"
   
6. Save file and reopen  
   **Expected:** Both character names persist

**Pass Criteria:** Edits automatically flush when navigating

---

### TC-1100-006: Status Bar Edit Button
**Priority:** High  
**Time:** ~2 minutes  
**Focus:** Verify manual edit flush works

**Steps:**
1. Select a character and edit description field
2. Without clicking away, look at Status Bar  
   **Expected:** Edit button is enabled
   
3. Click Edit button in Status Bar  
   **Expected:** Edit button becomes disabled
   
4. Navigate to different element  
   **Expected:** Previous edits are preserved

**Pass Criteria:** Edit button manually flushes current edits

---

### TC-1100-007: Backup Service Integration
**Priority:** High  
**Time:** ~3 minutes  
**Focus:** Verify backups work with new document structure

**Setup:**
- Enable automatic backups in Tools > Preferences > Backup

**Steps:**
1. Create new story "BackupTest"
2. Save as "TC1100_Backup.stbx"
3. Make several changes over 2 minutes  
   **Expected:** Backup created in backup folder
   
4. Check backup folder  
   **Expected:** Backup file exists with timestamp
   
5. Open backup file  
   **Expected:** Contains story data

**Cleanup:**
- Disable automatic backups
- Delete backup files

---

### TC-1100-008: Unsaved Changes on Exit
**Priority:** Critical  
**Time:** ~2 minutes  
**Focus:** Verify unsaved changes detected correctly

**Steps:**
1. Open any story file
2. Make a change (edit any text field)  
   **Expected:** "Changed" indicator appears
   
3. Try to close StoryCAD (File > Exit)  
   **Expected:** "Save changes?" dialog appears
   
4. Click Cancel  
   **Expected:** Returns to application
   
5. Save file  
   **Expected:** "Changed" indicator clears
   
6. Try to exit again  
   **Expected:** Closes without prompt

**Pass Criteria:** Properly detects unsaved changes

---

### TC-1100-009: Multiple View Updates
**Priority:** High  
**Time:** ~3 minutes  
**Focus:** Verify Explorer/Narrator views update correctly

**Steps:**
1. Open outline with scenes
2. In Explorer view, add new Scene "TestScene"  
   **Expected:** Scene appears in tree
   
3. Switch to Narrator view (Status Bar selector)  
   **Expected:** Can drag TestScene to narrative
   
4. Drag scene to narrative order  
   **Expected:** Scene appears in Narrator view
   
5. Switch back to Explorer  
   **Expected:** Scene still visible in Explorer

**Pass Criteria:** Both views maintain correct state

---

### TC-1100-010: Trash Operations
**Priority:** Medium  
**Time:** ~2 minutes  
**Focus:** Verify trash view updates correctly

**Steps:**
1. Create element "TrashTest"
2. Right-click and Delete  
   **Expected:** Element moves to trash (if visible)
   
3. Open trash/recycle bin view  
   **Expected:** "TrashTest" appears in trash
   
4. Right-click in trash and Restore  
   **Expected:** Element returns to main tree

**Pass Criteria:** Trash operations work correctly

---

## Regression Tests Summary

Run these existing tests from the full plan to ensure no regressions:

- **TC-001**: Create New Story (basic file creation)
- **TC-002**: Open Existing Story (basic file loading)  
- **TC-003**: Save As (file path handling)
- **TC-020**: Create Character (element creation)
- **TC-040**: Copy and Paste Elements (edit operations)
- **TC-041**: Undo and Redo (edit history)
- **TC-083**: Changed Indicator (dirty flag tracking)
- **TC-090**: Unsaved Changes Warning (exit handling)

---

## Test Execution Notes

1. **Setup Before Testing:**
   - Create a fresh TestSession folder
   - Note initial preference settings
   - Have sample files ready

2. **Critical Path (30 minutes):**
   - Run all TC-1100-xxx tests in order
   - These specifically test the refactored code

3. **Extended Testing (1 hour):**
   - Run Critical Path plus Regression Tests
   - Focuses on core functionality

4. **Issues to Watch For:**
   - Tree view not updating after file operations
   - "Changed" indicator not working correctly
   - Auto-save not triggering
   - Edits lost when switching elements
   - Save As not updating file path
   - Crashes or hangs during save operations

5. **If Issues Found:**
   - Note exact steps to reproduce
   - Check if issue exists in main branch
   - Screenshot any error messages
   - Check logs in AppData/Roaming/StoryCAD/Logs

---

## Sign-off Checklist

- [ ] All Critical tests pass
- [ ] Tree view updates correctly for all operations
- [ ] Save/SaveAs/AutoSave work correctly
- [ ] Edit flushing works on navigation
- [ ] No data loss when switching elements
- [ ] Backup service creates backups
- [ ] Changed indicator accurate
- [ ] No crashes or hangs
- [ ] No regression in core features

**Tester:** ________________  
**Date:** ________________  
**Build:** Issue-1100 branch  
**Result:** Pass / Fail (circle one)