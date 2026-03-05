# Services Test Plan
**Time**: ~15 minutes
**Purpose**: Verify AutoSave, Backup, Search, and Logging services
**Priority**: Tier 2

## Platform Notes

| Service | Windows | macOS |
|---------|---------|-------|
| AutoSave | Same behavior | Same behavior |
| Backup | .zip created in backup directory | Same — verify macOS paths |
| Search | Same behavior | Same behavior |
| Logging | `%LOCALAPPDATA%/.../logs/` | `~/Library/.../logs/` or UNO-specific path |

**macOS-specific concerns**:
- Backup directory path uses macOS conventions. Verify the default backup location is accessible.
- Log file location may differ. Check Preferences for the actual path.
- File system permissions on macOS may affect backup directory creation.

## Setup
1. Launch StoryCAD
2. Open or create an outline with several elements and data
3. Note the current Preferences settings for AutoSave and Backup (we will change them during testing)

---

## AutoSave

### SV-001: AutoSave Triggers on Interval
**Priority:** Critical
**Time:** ~3 minutes

**Steps:**
1. Open Tools > Preferences
   **Expected:** Preferences dialog opens

2. Enable AutoSave and set interval to 15 seconds (minimum)
   **Expected:** Settings accepted

3. Save preferences and return to the outline
   **Expected:** Preferences saved

4. Make a change to any element (edit a name or text field)
   **Expected:** Changed indicator appears (edit pencil turns red)

5. Wait 15-20 seconds without manually saving
   **Expected:** Changed indicator clears; status bar briefly shows auto-save message

6. Close and reopen the file
   **Expected:** The change you made is preserved (auto-save wrote it)

**Pass/Fail:** ______

---

### SV-002: AutoSave Does Not Save When Unchanged
**Priority:** High
**Time:** ~1 minute

**Steps:**
1. With AutoSave enabled, open a saved file (no changes)
   **Expected:** No Changed indicator

2. Wait 20+ seconds
   **Expected:** No auto-save activity in status bar (nothing to save)

**Pass/Fail:** ______

---

### SV-003: Disable AutoSave
**Priority:** Medium
**Time:** ~1 minute

**Steps:**
1. Open Preferences and disable AutoSave
   **Expected:** Setting saved

2. Make a change to an element
   **Expected:** Changed indicator appears

3. Wait 30+ seconds
   **Expected:** Changed indicator remains; no auto-save occurs

4. Re-enable AutoSave when done testing
   **Expected:** Setting restored

**Pass/Fail:** ______

---

## Backup

### SV-004: Manual Backup on Open
**Priority:** High
**Time:** ~2 minutes

**Steps:**
1. Open Preferences and enable "Backup on Open"
   **Expected:** Setting accepted

2. Note the configured Backup Directory path
   **Expected:** Path is visible and accessible

3. Close and reopen an outline file
   **Expected:** File opens normally

4. Check the Backup Directory
   **Expected:** A .zip file exists with format "[storyname] as of YYYYMMDD_HHmm.zip"

5. Open the .zip to verify contents
   **Expected:** Contains the .stbx file

**Pass/Fail:** ______

---

### SV-005: Timed Backup
**Priority:** Medium
**Time:** ~3 minutes

**Steps:**
1. Open Preferences; enable "Timed Backup" with minimum interval
   **Expected:** Setting accepted

2. Open an outline and wait for the backup interval to elapse
   **Expected:** Status bar shows backup activity

3. Check the Backup Directory
   **Expected:** New .zip backup file created

4. Disable Timed Backup when done
   **Expected:** Setting restored

**Pass/Fail:** ______

---

## Search

### SV-006: Search Across All Elements
**Priority:** Critical
**Time:** ~2 minutes

**Steps:**
1. Open an outline with multiple elements containing text (use "Danger Calls" sample)
   **Expected:** Outline open

2. Press Ctrl+F / Cmd+F or click Tools > Search
   **Expected:** Search interface appears

3. Type a term that exists in multiple elements (e.g., a character name)
   **Expected:** Tree filters to show matching nodes

4. Click on a search result
   **Expected:** Content pane shows the matched element

5. Clear the search
   **Expected:** Full tree reappears

**Pass/Fail:** ______

---

### SV-007: Search — No Results
**Priority:** Medium
**Time:** ~1 minute

**Steps:**
1. Search for a term that does not exist in the outline (e.g., "xyzzyplugh")
   **Expected:** No results shown or "no results" indication

2. Clear the search
   **Expected:** Full tree reappears

**Pass/Fail:** ______

---

## Logging

### SV-008: Log File Exists
**Priority:** Medium
**Time:** ~1 minute

**Steps:**
1. Launch StoryCAD and use it briefly (open a file, navigate)
   **Expected:** Normal operation

2. Locate the log directory (check Preferences or look in the app's data folder)
   **Expected:** Directory exists

3. Find today's log file (format: `StoryCAD.YYYY-MM-DD.log`)
   **Expected:** Log file exists with today's date

4. Open the log file in a text editor
   **Expected:** Contains timestamped entries; no unexpected errors

**Pass/Fail:** ______

---

### SV-009: Advanced Logging Toggle
**Priority:** Low
**Time:** ~1 minute

**Steps:**
1. Open Preferences and enable "Advanced Logging"
   **Expected:** Setting saved

2. Perform some actions (navigate, edit, save)
   **Expected:** Normal operation

3. Check the log file
   **Expected:** More detailed (Trace-level) entries visible compared to normal logging

4. Disable Advanced Logging when done
   **Expected:** Setting restored

**Pass/Fail:** ______

---

## Collaborator Service (Platform-Specific)

### SV-010: Collaborator Disabled on macOS
**Priority:** High
**Time:** ~1 minute
**Platform:** macOS only

**Steps:**
1. Launch StoryCAD on macOS
   **Expected:** Application starts normally

2. Check whether the Collaborator menu/button is available
   **Expected:** Collaborator is disabled or hidden — the AI plugin requires Windows

3. Verify no error messages related to Collaborator appear in the log
   **Expected:** Log shows "Collaborator interface not supported on this platform" (Info level)

**Pass/Fail:** ______

---

### SV-011: Collaborator COLLAB_DEBUG Override
**Priority:** Low
**Time:** ~2 minutes
**Platform:** Windows

**Steps:**
1. Set environment variable `COLLAB_DEBUG=0`
   **Expected:** Variable set

2. Launch StoryCAD
   **Expected:** Collaborator is disabled regardless of plugin directory

3. Set environment variable `COLLAB_DEBUG=1` (or unset it)
   **Expected:** Collaborator availability depends on plugin directory as normal

**Pass/Fail:** ______

---

## SERVICES RESULT: _____ / 11 Passed

**Critical Issues**: ________________

**Tested by**: _________ **Date**: _________ **Platform**: _________
