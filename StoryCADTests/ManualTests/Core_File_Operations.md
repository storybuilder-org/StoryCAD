# Core File Operations Test Plan
**Time**: ~10 minutes  
**Purpose**: Verify essential file operations work correctly

## Platform Notes

| Action | Windows | macOS |
|--------|---------|-------|
| Save | Ctrl+S | Cmd+S |
| Open/Create file | Ctrl+O | Cmd+O |
| Save As | Ctrl+Shift+S | Cmd+Shift+S |
| File dialogs | Windows native | macOS native |
| Test folder location | Documents\TestInputs | ~/Documents/TestInputs |

**macOS testers**: Substitute Cmd for Ctrl. File pickers are macOS-native — use the sidebar to navigate. The test folder path will use forward slashes.

## Setup
1. Launch StoryCAD
2. Note: Sample outlines are in File > Open/Create file > Sample outlines
3. Create test folder: TestInputs/CoreTests (in Documents or a convenient location)

---

### CF-001: New Story Creation
**Priority:** Critical  
**Time:** ~2 minutes

**Steps:**
1. Click File > Open/Create file > Create new outline  
   **Expected:** The New outline page opens; Project path is the folder from Preferences

2. Set Project path to TestInputs/CoreTests, enter "CF002_TestSave" as Project Name, and click Create outline  
   **Expected:** The outline file is created at once; title bar shows "Currently editing CF002_TestSave"

3. Enter "Core Test Story" in the Name field  
   **Expected:** Tree updates to show name

4. Add Story Idea text  
   **Expected:** Text saves in field

5. Verify the changed indicator  
   **Expected:** The pencil icon in the status bar is red

**Pass/Fail:** ______

---

### CF-002: Save New Story
**Priority:** Critical  
**Time:** ~1 minute

**Steps:**
1. With new story from CF-001, press Ctrl+S (Cmd+S on macOS)  
   **Expected:** File saves silently; the outline got its name and folder when it was created, so no Save dialog appears

2. Verify the changed indicator cleared  
   **Expected:** The pencil icon is green

3. Check title bar  
   **Expected:** Shows "Currently editing CF002_TestSave"

**Pass/Fail:** ______

---

### CF-003: Open Existing Story
**Priority:** Critical  
**Time:** ~2 minutes

**Steps:**
1. Click File > Close Story, then File > Open/Create file (or Ctrl+O / Cmd+O) > Open from file  
   **Expected:** Open dialog appears

2. Select CF002_TestSave.stbx from the CoreTests folder  
   **Expected:** File opens

3. Verify content preserved  
   **Expected:** "Core Test Story" and idea text present

**Pass/Fail:** ______

---

### CF-004: Save As Function
**Priority:** High  
**Time:** ~1 minute

**Steps:**
1. With file open, press Ctrl+Shift+S  
   **Expected:** Save as dialog appears

2. Change New project Name to "CF004_SaveAs.stbx" and click Save  
   **Expected:** File saves with new name

3. Verify title bar updated  
   **Expected:** Shows "Currently editing CF004_SaveAs"

4. Verify original file still exists  
   **Expected:** Both files in folder; File > Open/Create file > Open from file opens CF002_TestSave

**Pass/Fail:** ______

---

### CF-005: Open Sample Outline
**Priority:** Medium  
**Time:** ~1 minute

**Steps:**
1. Click File > Open/Create file > Sample outlines  
   **Expected:** List of samples

2. Select "Hamlet" and click Open sample  
   **Expected:** Sample opens

3. Browse the outline content  
   **Expected:** Can navigate and view all elements

4. Click File > Close Story without changing anything  
   **Expected:** No save prompt

**Pass/Fail:** ______

---

### CF-006: Unsaved Changes Dialog
**Priority:** Critical  
**Time:** ~2 minutes

**Steps:**
1. Open any story and make a change  
   **Expected:** The pencil icon turns red

2. Click File > Exit  
   **Expected:** "Save changes?" dialog with Yes and No

3. Click No  
   **Expected:** Exits without saving

4. Start StoryCAD and reopen the file  
   **Expected:** Changes not present

**Pass/Fail:** ______

---

## CORE FILE OPS RESULT: _____ / 6 Passed

**Critical Issues**: ________________

**Tested by**: _________ **Date**: _________
