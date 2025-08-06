# Core File Operations Test Plan
**Time**: ~10 minutes  
**Purpose**: Verify essential file operations work correctly

## Setup
1. Launch StoryCAD
2. Note: Sample outlines in File > Open Sample Outline
3. Create test folder: TestInputs/CoreTests

---

### CF-001: New Story Creation
**Priority:** Critical  
**Time:** ~2 minutes

**Steps:**
1. Click File > New Story (or Ctrl+N)  
   **Expected:** New outline with "Untitled" Story Overview

2. Enter "Core Test Story" in Story Name field  
   **Expected:** Tree updates to show name

3. Add Story Idea text  
   **Expected:** Text saves in field

4. Verify Changed indicator  
   **Expected:** Status bar shows "Changed"

**Pass/Fail:** ______

---

### CF-002: Save New Story
**Priority:** Critical  
**Time:** ~2 minutes

**Steps:**
1. With new story from CF-001, press Ctrl+S  
   **Expected:** Save dialog appears

2. Navigate to TestInputs/CoreTests  
   **Expected:** Folder is accessible

3. Enter "CF002_TestSave.stbx" and click Save  
   **Expected:** File saves successfully

4. Verify Changed indicator cleared  
   **Expected:** "Changed" no longer shown

5. Check title bar  
   **Expected:** Shows "CF002_TestSave.stbx"

**Pass/Fail:** ______

---

### CF-003: Open Existing Story
**Priority:** Critical  
**Time:** ~2 minutes

**Steps:**
1. Click File > New Story  
   **Expected:** New blank story

2. Click File > Open Story (or Ctrl+O)  
   **Expected:** Open dialog appears

3. Select CF002_TestSave.stbx  
   **Expected:** File opens

4. Verify content preserved  
   **Expected:** "Core Test Story" and idea text present

**Pass/Fail:** ______

---

### CF-004: Save As Function
**Priority:** High  
**Time:** ~1 minute

**Steps:**
1. With file open, press Ctrl+Shift+S  
   **Expected:** Save As dialog appears

2. Change name to "CF004_SaveAs.stbx"  
   **Expected:** File saves with new name

3. Verify title bar updated  
   **Expected:** Shows new filename

4. Verify original file still exists  
   **Expected:** Both files in folder

**Pass/Fail:** ______

---

### CF-005: Open Sample Outline
**Priority:** Medium  
**Time:** ~1 minute

**Steps:**
1. Click File > Open Sample Outline  
   **Expected:** Submenu with samples

2. Select "Hamlet"  
   **Expected:** Sample opens

3. Verify read-only behavior  
   **Expected:** Can edit but prompts Save As

4. Close without saving  
   **Expected:** No errors

**Pass/Fail:** ______

---

### CF-006: Unsaved Changes Dialog
**Priority:** Critical  
**Time:** ~2 minutes

**Steps:**
1. Open any story and make a change  
   **Expected:** "Changed" appears

2. Click File > Exit  
   **Expected:** "Save changes?" dialog

3. Click Cancel  
   **Expected:** Returns to app

4. Try again, click Don't Save  
   **Expected:** Exits without saving

5. Reopen file  
   **Expected:** Changes not present

**Pass/Fail:** ______

---

## CORE FILE OPS RESULT: _____ / 6 Passed

**Critical Issues**: ________________

**Tested by**: _________ **Date**: _________