# StoryCAD Smoke Test
**Time**: ~5 minutes  
**Purpose**: Verify build is stable enough for further testing

## Prerequisites
- Fresh StoryCAD installation or update
- Windows 10/11 system

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
1. Click File > New Story  
   **Expected:** New outline with "Untitled" appears
   
2. Type "Smoke Test" in Story Name  
   **Expected:** Name updates in tree

3. Press Ctrl+S  
   **Expected:** Save dialog appears

4. Save as "SmokeTest.stbx" in Documents  
   **Expected:** File saves, no errors

**Pass/Fail:** ______

---

### ST-003: Add Basic Elements
**Priority:** Critical  
**Time:** ~1 minute

**Steps:**
1. Right-click Story Overview  
   **Expected:** Context menu appears

2. Select Add > Character  
   **Expected:** New Character node appears

3. Type "Test Character" in Name field  
   **Expected:** Tree updates with name

4. Right-click and Add > Scene  
   **Expected:** New Scene appears

**Pass/Fail:** ______

---

### ST-004: Open Existing File
**Priority:** Critical  
**Time:** ~1 minute

**Steps:**
1. Click File > Open Story  
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

3. Click "Don't Save"  
   **Expected:** Application closes cleanly

**Pass/Fail:** ______

---

## SMOKE TEST RESULT: PASS / FAIL

**Notes**:
_____________________

**Tested by**: _________ **Date**: _________ **Build**: _________