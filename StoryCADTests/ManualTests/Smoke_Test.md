# StoryCAD Smoke Test
**Time**: ~5 minutes  
**Purpose**: Verify build is stable enough for further testing

## Platform Notes

| Action | Windows | macOS |
|--------|---------|-------|
| Launch | Double-click icon or Start menu | Double-click in Applications or Dock |
| Save shortcut | Ctrl+S | Cmd+S |
| File dialogs | Windows native | macOS native |
| Exit | File > Exit or Alt+F4 | File > Exit, Cmd+Q, or red traffic light |

**macOS testers**: Substitute Cmd for Ctrl in all keyboard shortcuts below. File dialogs will be macOS-native.

## Prerequisites
- Fresh StoryCAD installation or update
- Windows 10/11 or macOS 10.15+

---

### ST-001: Application Launch
**Priority:** Critical  
**Time:** ~1 minute

**Steps:**
1. Double-click StoryCAD icon  
   **Expected:** Application launches without error
   
2. Verify main window appears  
   **Expected:** Navigation and Content panes visible

3. Check for error dialogs  
   **Expected:** No error messages

**Pass/Fail:** ______

---

### ST-002: Create and Save New Story
**Priority:** Critical  
**Time:** ~1 minute

**Steps:**
1. In the file menu (it opens at startup, or File > Open/Create file), click **Create new outline**, type `SmokeTest` as the project name, and click **Create outline**  
   **Expected:** The new outline opens with `SmokeTest` at the top of the tree
   
2. Type `Smoke Test` in **Title**  
   **Expected:** The top of the tree changes to `Smoke Test`

3. Press Ctrl+S  
   **Expected:** No dialog; the status bar says the save completed

**Pass/Fail:** ______

---

### ST-003: Add Basic Elements
**Priority:** Critical  
**Time:** ~1 minute

**Steps:**
1. Right-click Story Overview  
   **Expected:** Context menu appears

2. Select **Add Elements > Add Character**  
   **Expected:** New Character node appears

3. **Click the new Character node**, then type `Test Character` in Name  
   **Expected:** Tree updates with name

4. Right-click the top node and select **Add Elements > Add Scene**  
   **Expected:** New Scene appears

**Pass/Fail:** ______

---

### ST-004: Open Existing File
**Priority:** Critical  
**Time:** ~1 minute

**Steps:**
1. Click **File > Open/Create file**, then **Open from file**  
   **Expected:** Open dialog appears

2. Select SmokeTest.stbx  
   **Expected:** File opens successfully

3. Verify elements are present  
   **Expected:** Character and Scene visible

**Pass/Fail:** ______

---

### ST-005: Clean Exit
**Priority:** Critical  
**Time:** ~1 minute

**Steps:**
1. Make a small change  
   **Expected:** Edit pencil icon button in status bar turns red (indicates unsaved changes)

2. Click File > Exit  
   **Expected:** Save changes dialog appears

3. Click **No**  
   **Expected:** Application closes cleanly

**Pass/Fail:** ______

---

## SMOKE TEST RESULT: PASS / FAIL

**Notes**:
_____________________

**Tested by**: _________ **Date**: _________ **Build**: _________