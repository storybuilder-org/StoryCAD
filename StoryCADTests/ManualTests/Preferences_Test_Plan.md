# Preferences Test Plan
**Time**: ~10 minutes
**Purpose**: Verify all Preferences dialog tabs and settings persist correctly
**Priority**: Tier 2

## Platform Notes

| Feature | Windows | macOS |
|---------|---------|-------|
| Open Preferences | Preferences button on the command bar (Ctrl+,) | StoryCAD menu > Preferences... (Cmd+,) |
| Theme options | Light / Dark / System | Same (verify macOS dark mode integration) |
| Directory paths | Windows paths (C:\...) | macOS paths (/Users/...) |
| Store review | Links to Microsoft Store | Links to Mac App Store |
| File picker for directories | Windows folder picker | macOS folder picker |
| Collaborator menu item (PF-009 step 4) | Not applicable | Tools menu |

**macOS-specific concerns**:
- Theme switching should respect macOS system appearance. Verify "System" theme follows macOS dark/light mode.
- Directory pickers use macOS native dialogs. Verify they work and return valid paths.
- The "About" tab may show different store links on macOS.

## Setup
1. Launch StoryCAD
2. Note current preferences so you can restore them after testing
3. Have an outline open (some settings only apply when a file is open)

---

### PF-001: Open and Navigate Preferences Dialog
**Priority:** Critical
**Time:** ~1 minute

**Steps:**
1. Click the Preferences button on the command bar (macOS: StoryCAD menu > Preferences...)
   **Expected:** Preferences dialog opens

2. Verify tabs are visible (Account, Save Locations, Backup, Privacy, Other, About, What's new)
   **Expected:** All tabs display and are clickable

3. Click through each tab
   **Expected:** Each tab loads its content without errors

**Pass/Fail:** ______

---

### PF-002: User Information
**Priority:** High
**Time:** ~2 minutes

**Steps:**
1. Go to the Account tab
   **Expected:** Fields for Your first name, Your surname, Your email visible, and a Delete My Data button

2. Enter or update the first name and surname
   **Expected:** Fields accept text

3. Enter an email address
   **Expected:** Field accepts text

4. Go to the Privacy tab and toggle the "Send error logs" check box
   **Expected:** Check box toggles

5. Click Save
   **Expected:** Dialog closes

6. Reopen Preferences
   **Expected:** All entered values persisted

**Pass/Fail:** ______

---

### PF-003: Theme Switching
**Priority:** High
**Time:** ~2 minutes

**Steps:**
1. In Preferences, go to the Other tab and find Preferred Theme
   **Expected:** Options: Use system theme, Light Theme, Dark Theme

2. Select "Dark Theme"
   **Expected:** Selection accepted

3. Save and close Preferences
   **Expected:** App may prompt for restart or apply immediately

4. Verify the UI reflects the dark theme
   **Expected:** Dark background, light text throughout the app

5. Switch to "Light Theme"
   **Expected:** Light background, dark text

6. Switch to "Use system theme"
   **Expected:** Follows OS dark/light setting

**Pass/Fail:** ______

---

### PF-004: AutoSave Settings
**Priority:** High
**Time:** ~1 minute

**Steps:**
1. Go to the Backup tab and find the "Automatically Save every" check box
   **Expected:** Check box and a seconds interval field visible

2. Check and clear "Automatically Save every"
   **Expected:** Check box toggles; the interval field is disabled when it is clear

3. Set interval to 15 seconds (minimum)
   **Expected:** Accepted

4. Try setting interval below 15 or above 60
   **Expected:** Value clamped or validation error shown

5. Save and reopen
   **Expected:** Settings persisted

**Pass/Fail:** ______

---

### PF-005: Backup Settings
**Priority:** Medium
**Time:** ~2 minutes

**Steps:**
1. Go to the Backup tab
   **Expected:** "Make a backup of the story when opened" and "Make timed backups" check boxes and the "How often should backups be made? (Minutes)" field visible

2. Check and clear "Make a backup of the story when opened"
   **Expected:** Check box toggles

3. Check and clear "Make timed backups"
   **Expected:** Check box toggles

4. Go to the Save Locations tab and change the Backup directory using the folder picker
   **Expected:** Native folder picker opens; new path appears after selection

5. Save and reopen
   **Expected:** All backup settings persisted, including directory path

**Pass/Fail:** ______

---

### PF-006: Search Engine Preference
**Priority:** Low
**Time:** ~1 minute

**Steps:**
1. Go to the Other tab and find Preferred Search Engine
   **Expected:** Drop-down with options DuckDuckGo, Google, Bing, Yahoo

2. Change the selection
   **Expected:** New engine selected

3. Save and reopen
   **Expected:** Selection persisted

**Pass/Fail:** ______

---

### PF-007: Project Directory Setting
**Priority:** Medium
**Time:** ~1 minute

**Steps:**
1. Go to the Save Locations tab and find the Project directory field
   **Expected:** Current path displayed with a browse button

2. Click browse and select a new directory
   **Expected:** Native folder picker; path updates

3. Save and reopen
   **Expected:** New path persisted

**Pass/Fail:** ______

---

### PF-008: About Tab
**Priority:** Low
**Time:** ~1 minute

**Steps:**
1. Click the About tab
   **Expected:** Version info, Open Logs folder button, contributors link, store review link, social media buttons visible

2. Verify version number is displayed
   **Expected:** Shows current StoryCAD version

3. Click the store review link (if safe to do so)
   **Expected:** Opens appropriate store (Microsoft Store on Windows, Mac App Store on macOS)

**Pass/Fail:** ______

---

### PF-009: Show Collaborator
**Priority:** Medium
**Time:** ~3 minutes

**Steps:**
1. Open Preferences, select the Other tab, and make sure Show Collaborator is checked
   **Expected:** Show Collaborator is checked

2. Clear Show Collaborator and click Save
   **Expected:** Dialog closes without a restart prompt

3. Look at the command bar
   **Expected:** The Collaborator button and the Buy Credits button are not on the command bar

4. (macOS only) Open the Tools menu
   **Expected:** The Tools menu has no Collaborator item

5. Open Preferences, check Show startup page, click Save, and restart StoryCAD
   **Expected:** The startup help dialog appears

6. Look at the startup help dialog
   **Expected:** The Meet Collaborator (Video) link is still there

7. Look at the command bar after the restart
   **Expected:** The Collaborator button is not on the command bar

8. Open Preferences, check Show Collaborator, and click Save
   **Expected:** Dialog closes

9. Look at the command bar
   **Expected:** The Collaborator button and the Buy Credits button are on the command bar again

10. Open Preferences, clear Show Collaborator, and click Cancel
    **Expected:** The buttons stay on the command bar

**Pass/Fail:** ______

---

## PREFERENCES RESULT: _____ / 9 Passed

**Critical Issues**: ________________

**Tested by**: _________ **Date**: _________ **Platform**: _________
