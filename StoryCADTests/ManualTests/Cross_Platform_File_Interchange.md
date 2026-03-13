# Cross-Platform File Interchange Test Plan
**Time**: ~10 minutes
**Purpose**: Verify .stbx files round-trip between macOS and Windows without data loss
**Priority**: Tier 1 (blocks release)

## Platform Notes

This test requires access to **both** a macOS machine and a Windows machine, plus a shared file location (Google Drive, OneDrive, USB drive, or network share).

**Known differences to watch for:**
- Path separators: `\` (Windows) vs `/` (macOS) — should not appear in .stbx content
- Line endings: CR+LF (Windows) vs LF (macOS) — JSON serialization should be consistent
- File picker behavior differs by platform

## Setup
1. Identify a shared location accessible from both platforms (e.g., Google Drive folder)
2. Have StoryCAD installed on both macOS and Windows
3. Prepare a moderately complex test outline with: Characters, Problems, Scenes, Settings, Notes, a StoryWorld with multiple entries, and a Folder

---

### XP-001: Create on macOS, Open on Windows
**Priority:** Critical
**Time:** ~3 minutes

**Steps (macOS):**
1. Launch StoryCAD on macOS
   **Expected:** App launches

2. Create a new story "CrossPlatTest"
   **Expected:** Outline created

3. Add at least: 1 Character, 1 Problem, 1 Scene, 1 Setting, 1 StoryWorld (with a Physical World entry)
   **Expected:** All elements created

4. Enter data in each element (names, text fields, dropdown selections)
   **Expected:** Data entered

5. Save as "CrossPlatTest.stbx" to the shared location
   **Expected:** File saved

**Steps (Windows):**
6. Launch StoryCAD on Windows
   **Expected:** App launches

7. Open "CrossPlatTest.stbx" from the shared location
   **Expected:** File opens without errors

8. Verify all elements present in the tree
   **Expected:** Same structure as created on macOS

9. Check each element's data (names, text, dropdowns)
   **Expected:** All data matches what was entered on macOS

10. Check StoryWorld entries and navigation
    **Expected:** Physical World entry present with correct data

**Pass/Fail:** ______

---

### XP-002: Edit on Windows, Verify on macOS
**Priority:** Critical
**Time:** ~3 minutes

**Steps (Windows):**
1. With the file from XP-001 open on Windows, add a new Character "Windows-Added"
   **Expected:** Character created

2. Edit an existing Scene's text
   **Expected:** Text updated

3. Save the file back to the shared location
   **Expected:** File saved

**Steps (macOS):**
4. Open the updated file on macOS
   **Expected:** File opens without errors

5. Verify "Windows-Added" character exists
   **Expected:** Character present with correct name

6. Verify Scene text change persists
   **Expected:** Edited text matches what was entered on Windows

7. Verify all original data still intact
   **Expected:** No data loss from the round-trip

**Pass/Fail:** ______

---

### XP-003: Create on Windows, Open on macOS
**Priority:** High
**Time:** ~3 minutes

**Steps (Windows):**
1. Create a new story "WinOrigin" on Windows
   **Expected:** Outline created

2. Add several elements with data, including a StoryWorld with multiple list entries (Physical Worlds, Species)
   **Expected:** Complex outline created

3. Save to shared location
   **Expected:** File saved

**Steps (macOS):**
4. Open "WinOrigin.stbx" on macOS
   **Expected:** File opens without errors

5. Verify all elements and data
   **Expected:** Everything matches

6. Edit a field and save
   **Expected:** Save succeeds

7. Reopen and verify edit persisted
   **Expected:** Edit preserved

**Pass/Fail:** ______

---

### XP-004: Sample Outline Cross-Platform
**Priority:** Medium
**Time:** ~2 minutes

**Steps:**
1. On one platform, open sample outline "Danger Calls"
   **Expected:** Sample opens

2. Save As to shared location as "DangerCalls_Copy.stbx"
   **Expected:** File saved

3. On the other platform, open "DangerCalls_Copy.stbx"
   **Expected:** Opens without errors; all elements and data intact

**Pass/Fail:** ______

---

## Version Migration Tests

These tests verify that outlines created in StoryCAD 3.4 (Windows-only, WinUI 3) work in 4.0 (UNO Platform) and vice versa.

**Setup:**
- Have StoryCAD 3.4 installed on a Windows machine (or available via the Microsoft Store)
- Have StoryCAD 4.0 (dev branch build) available on macOS and/or Windows
- Locate or create a moderately complex 3.4 outline with: Characters, Problems, Scenes, Settings, Notes, Folders

---

### XP-005: Forward Migration — 3.4 Outline in 4.0
**Priority:** Critical
**Time:** ~3 minutes

**Steps:**
1. Open a .stbx file created and saved in StoryCAD 3.4
   **Expected:** File opens without errors or crashes

2. Verify all elements are present in the tree
   **Expected:** Same structure as in 3.4 — Characters, Problems, Scenes, Settings, Notes, Folders all present

3. Check element data (names, text fields, dropdown selections, relationships)
   **Expected:** All data intact; no blank or corrupted fields

4. Check that new 4.0 features are available (e.g., can Add StoryWorld to this outline)
   **Expected:** StoryWorld can be added; new features work on migrated file

5. Save the file in 4.0
   **Expected:** Save succeeds without errors

6. Close and reopen in 4.0
   **Expected:** All data still intact after re-save

**Pass/Fail:** ______

---

### XP-006: Backward Compatibility — 4.0 Outline (No StoryWorld) in 3.4
**Priority:** High
**Time:** ~2 minutes

**Steps:**
1. In StoryCAD 4.0, create an outline with Characters, Problems, Scenes, Settings, Notes — but NO StoryWorld element
   **Expected:** Standard outline created

2. Save the file
   **Expected:** File saved

3. Open the file in StoryCAD 3.4 on Windows
   **Expected:** File opens without errors or crashes

4. Verify all elements and data are present
   **Expected:** Everything intact — 3.4 can read the 4.0 file when it contains only known element types

5. Edit something in 3.4 and save
   **Expected:** Save succeeds

6. Reopen in 4.0
   **Expected:** File opens; 3.4 edits preserved; no data loss from the round-trip

**Pass/Fail:** ______

---

### XP-007: Backward Compatibility — 4.0 Outline (With StoryWorld) in 3.4
**Priority:** Critical
**Time:** ~3 minutes

This is the high-risk case: StoryWorld is a new element type that 3.4 does not recognize.

**Steps:**
1. In StoryCAD 4.0, create an outline with standard elements PLUS a StoryWorld with populated data (World Type, Physical Worlds entries, Cultures, etc.)
   **Expected:** Outline created with StoryWorld

2. Save the file
   **Expected:** File saved

3. Open the file in StoryCAD 3.4 on Windows
   **Expected:** One of:
   - (Best) File opens; StoryWorld element ignored or shown as unknown node; other elements intact
   - (Acceptable) File opens with a warning about unrecognized element; other elements intact
   - (Bad) File fails to open or crashes — **file a bug**
   - (Worst) File opens but other elements are corrupted — **file a critical bug**

4. If the file opened, verify all non-StoryWorld elements are intact
   **Expected:** Characters, Problems, Scenes, Settings, Notes all present and correct

5. If the file opened, save it in 3.4
   **Expected:** Save succeeds (or warns about unknown elements)

6. Reopen the 3.4-saved file in 4.0
   **Expected:** File opens; check whether the StoryWorld data survived the 3.4 round-trip or was stripped out

**Document the actual behavior** — this test establishes what happens and whether users need to be warned about backward compatibility.

**Pass/Fail:** ______

---

### XP-008: Preferences Migration — 3.4 to 4.0
**Priority:** Medium
**Time:** ~2 minutes

**Steps:**
1. In StoryCAD 3.4, configure Preferences: set a theme, AutoSave interval, Backup directory, user name/email
   **Expected:** Preferences saved

2. Note the Preferences.json file location (in the app's RoamingState or data directory)
   **Expected:** File exists

3. Install/launch StoryCAD 4.0 on the same machine (or copy Preferences.json to 4.0's data directory)
   **Expected:** 4.0 launches

4. Open Preferences in 4.0
   **Expected:** One of:
   - (Best) All 3.4 settings carried over (theme, intervals, directories, user info)
   - (Acceptable) Some settings carried over; new 4.0 settings use defaults
   - (Bad) Preferences reset to defaults with no migration — **note as issue**

5. Verify no crash from unrecognized or missing preference keys
   **Expected:** App handles old/new preference format gracefully

**Pass/Fail:** ______

---

## CROSS-PLATFORM RESULT: _____ / 8 Passed

**Critical Issues**: ________________

**Tested by**: _________ **Date**: _________ **Platforms**: _________
