# Preferences Test Plan
**Time**: ~10 minutes
**Purpose**: Verify all Preferences dialog tabs and settings persist correctly
**Priority**: Tier 2

## Platform Notes

| Feature | Windows | macOS |
|---------|---------|-------|
| Open Preferences | Tools > Preferences | Same |
| Theme options | Light / Dark / System | Same (verify macOS dark mode integration) |
| Directory paths | Windows paths (C:\...) | macOS paths (/Users/...) |
| Store review | Links to Microsoft Store | Links to Mac App Store |
| File picker for directories | Windows folder picker | macOS folder picker |

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
1. Click Tools > Preferences
   **Expected:** Preferences dialog opens

2. Verify tabs are visible (User Info, Backup/AutoSave, Search, About)
   **Expected:** All tabs display and are clickable

3. Click through each tab
   **Expected:** Each tab loads its content without errors

**Pass/Fail:** ______

---

### PF-002: User Information
**Priority:** High
**Time:** ~2 minutes

**Steps:**
1. Go to the User Info tab
   **Expected:** Fields for First Name, Last Name, Email visible

2. Enter or update First Name and Last Name
   **Expected:** Fields accept text

3. Enter an email address
   **Expected:** Field accepts text

4. Toggle "Error Collection Consent" checkbox
   **Expected:** Checkbox toggles

5. Click OK to save
   **Expected:** Dialog closes

6. Reopen Preferences
   **Expected:** All entered values persisted

**Pass/Fail:** ______

---

### PF-003: Theme Switching
**Priority:** High
**Time:** ~2 minutes

**Steps:**
1. In Preferences, find the Theme setting
   **Expected:** Options: Light, Dark, System (or Default)

2. Select "Dark" theme
   **Expected:** Selection accepted

3. Save and close Preferences
   **Expected:** App may prompt for restart or apply immediately

4. Verify the UI reflects the dark theme
   **Expected:** Dark background, light text throughout the app

5. Switch to "Light" theme
   **Expected:** Light background, dark text

6. Switch to "System" / "Default"
   **Expected:** Follows OS dark/light setting

**Pass/Fail:** ______

---

### PF-004: AutoSave Settings
**Priority:** High
**Time:** ~1 minute

**Steps:**
1. Find the AutoSave settings (Backup/AutoSave tab)
   **Expected:** "Enable AutoSave" toggle and interval field visible

2. Toggle AutoSave on/off
   **Expected:** Toggle works

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
1. Find the Backup settings
   **Expected:** "Backup on Open" toggle, "Timed Backup" toggle, interval field, Backup Directory visible

2. Toggle "Backup on Open" on/off
   **Expected:** Toggle works

3. Toggle "Timed Backup" on/off
   **Expected:** Toggle works

4. Change the Backup Directory using the folder picker
   **Expected:** Native folder picker opens; new path appears after selection

5. Save and reopen
   **Expected:** All backup settings persisted, including directory path

**Pass/Fail:** ______

---

### PF-006: Search Engine Preference
**Priority:** Low
**Time:** ~1 minute

**Steps:**
1. Find the Search Engine preference
   **Expected:** Dropdown with options (DuckDuckGo, Google, Bing, etc.)

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
1. Find the Project Directory setting
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
   **Expected:** Version info, social media links, store review button visible

2. Verify version number is displayed
   **Expected:** Shows current StoryCAD version

3. Click the store review button (if safe to do so)
   **Expected:** Opens appropriate store (Microsoft Store on Windows, Mac App Store on macOS)

**Pass/Fail:** ______

---

## PREFERENCES RESULT: _____ / 8 Passed

**Critical Issues**: ________________

**Tested by**: _________ **Date**: _________ **Platform**: _________
