# Copy Elements to Another Outline Test Plan
**Time**: ~10 minutes
**Purpose**: Verify the Copy Elements tool correctly copies story elements between outlines
**Priority**: Tier 2

## Platform Notes

| Action | Windows | macOS |
|--------|---------|-------|
| Open dialog | Tools > Copy Elements to Another Outline | Same |
| File picker | Windows native | macOS native |
| Keyboard | N/A (no shortcut assigned) | N/A |

**macOS-specific concerns**: File picker for selecting target .stbx file uses platform-native dialog. Verify the dialog opens correctly and filters to .stbx files.

## Setup
1. Launch StoryCAD
2. Create and save a **source** outline "CopySource.stbx" with:
   - 2 Characters (e.g., "Alice", "Bob")
   - 1 Problem
   - 1 Setting
   - 1 StoryWorld (with at least one Physical World entry)
   - 1 Notes element
3. Create and save a **target** outline "CopyTarget.stbx" with just a Story Overview
4. Reopen "CopySource.stbx" as the active outline

---

### CP-001: Open Dialog and Browse for Target
**Priority:** Critical
**Time:** ~2 minutes

**Steps:**
1. Click Tools > Copy Elements to Another Outline
   **Expected:** Dialog opens with empty source/target lists; status says "Select a filter type and browse for a target outline."

2. Click "Browse..." button
   **Expected:** File picker opens filtered to .stbx files

3. Select "CopyTarget.stbx"
   **Expected:** Path appears in the target field; status shows "Target loaded: [story name]"

4. Try selecting "CopySource.stbx" (the currently open file) as target
   **Expected:** Rejected with status "Cannot select current file as target."

**Pass/Fail:** ______

---

### CP-002: Filter and Copy a Character
**Priority:** Critical
**Time:** ~2 minutes

**Steps:**
1. With dialog open and target loaded, select "Character" from the Filter dropdown
   **Expected:** Source list (left) shows "Alice" and "Bob"

2. Select "Alice" in the source list
   **Expected:** Row highlighted

3. Click the right arrow button (Copy to target)
   **Expected:** "Alice" appears in the target list (right); status shows "Copied: Alice"

4. Select "Bob" and copy
   **Expected:** "Bob" also appears in the target list

**Pass/Fail:** ______

---

### CP-003: Filter by Different Types
**Priority:** High
**Time:** ~2 minutes

**Steps:**
1. Change the Filter dropdown to "Setting"
   **Expected:** Source list updates to show settings; target list retains previously copied characters (it accumulates)

2. Copy the Setting to the target
   **Expected:** Setting appears in target list alongside characters

3. Change filter to "Problem" and copy
   **Expected:** Problem copies successfully

4. Change filter to "Notes" and copy
   **Expected:** Notes element copies

**Pass/Fail:** ______

---

### CP-004: Copy StoryWorld Element
**Priority:** High
**Time:** ~1 minute

**Steps:**
1. Change filter to "StoryWorld"
   **Expected:** Source list shows the StoryWorld element

2. Copy the StoryWorld to the target
   **Expected:** StoryWorld copies; status confirms

3. Try copying the StoryWorld again
   **Expected:** Blocked with message "Target already has a StoryWorld. Only one allowed per story."

**Pass/Fail:** ______

---

### CP-005: Remove a Copied Element
**Priority:** Medium
**Time:** ~1 minute

**Steps:**
1. Select a previously copied element in the target list (right side)
   **Expected:** Element highlighted

2. Click the left arrow button (Remove from target)
   **Expected:** Element removed from target list

**Pass/Fail:** ______

---

### CP-006: Save and Verify Target File
**Priority:** Critical
**Time:** ~2 minutes

**Steps:**
1. Click "Save" (primary button) on the dialog
   **Expected:** Status shows "Saved: CopyTarget.stbx"; dialog closes

2. Open "CopyTarget.stbx" via File > Open
   **Expected:** File opens

3. Verify all copied elements are present in the tree
   **Expected:** Characters, Setting, Problem, Notes, StoryWorld all present

4. Check that copied elements have their data (names, text fields, dropdown selections)
   **Expected:** Data matches the source outline

5. Check StoryWorld has its Physical World entries
   **Expected:** World entries preserved with content

**Pass/Fail:** ______

---

### CP-007: Cancel Without Saving
**Priority:** Medium
**Time:** ~1 minute

**Steps:**
1. Open the Copy Elements dialog again with CopySource.stbx active
   **Expected:** Dialog opens with fresh state

2. Browse to a target, copy an element
   **Expected:** Element copied to target list

3. Click "Cancel"
   **Expected:** Dialog closes; no changes written to target file

4. Open the target file and verify the element was NOT added
   **Expected:** Target file unchanged from last save

**Pass/Fail:** ______

---

## COPY ELEMENTS RESULT: _____ / 7 Passed

**Critical Issues**: ________________

**Tested by**: _________ **Date**: _________ **Platform**: _________
