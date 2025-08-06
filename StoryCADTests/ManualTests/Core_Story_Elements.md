# Core Story Elements Test Plan
**Time**: ~10 minutes  
**Purpose**: Verify creation and editing of all story element types

## Setup
1. Launch StoryCAD
2. Create new story or use existing test file
3. Save as "ElementsTest.stbx" in TestInputs/CoreTests

---

### CE-001: Create Character
**Priority:** Critical  
**Time:** ~2 minutes

**Steps:**
1. Right-click Story Overview  
   **Expected:** Context menu appears

2. Select Add > Character  
   **Expected:** "New Character" node appears

3. Enter "Alice Smith" in Name field  
   **Expected:** Tree updates immediately

4. Select "Major" for Story Role  
   **Expected:** Selection saved

5. Switch to Physical tab and add description  
   **Expected:** Text saves when switching tabs

**Pass/Fail:** ______

---

### CE-002: Create Problem
**Priority:** Critical  
**Time:** ~2 minutes

**Steps:**
1. Click Add button in toolbar  
   **Expected:** Add menu dropdown

2. Select Problem  
   **Expected:** "New Problem" node appears

3. Enter "Main Conflict" as Problem name  
   **Expected:** Tree updates

4. Select "Person vs Person" as Conflict Type  
   **Expected:** Form updates accordingly

5. Enter Story Question: "Will Alice succeed?"  
   **Expected:** Text saves

**Pass/Fail:** ______

---

### CE-003: Create Scene
**Priority:** Critical  
**Time:** ~2 minutes

**Steps:**
1. Right-click on Problem node  
   **Expected:** Context menu

2. Select Add > Scene  
   **Expected:** Scene appears as child of Problem

3. Enter "Opening Scene" as name  
   **Expected:** Tree updates

4. Select "Alice Smith" as Viewpoint Character  
   **Expected:** Dropdown shows all characters

5. Add Scene Sketch text  
   **Expected:** Text area accepts input

**Pass/Fail:** ______

---

### CE-004: Create Setting
**Priority:** High  
**Time:** ~1 minute

**Steps:**
1. Right-click any node, select Add > Setting  
   **Expected:** "New Setting" appears

2. Enter "Coffee Shop" as Setting name  
   **Expected:** Tree updates

3. Add "Downtown Seattle" as Locale  
   **Expected:** Field accepts text

4. Set Time as "Present Day"  
   **Expected:** Field accepts text

**Pass/Fail:** ______

---

### CE-005: Create Notes
**Priority:** Medium  
**Time:** ~1 minute

**Steps:**
1. Right-click and select Add > Notes  
   **Expected:** "New Notes" appears

2. Enter "Research Notes" as name  
   **Expected:** Tree updates

3. Type multi-line text in notes area  
   **Expected:** Accepts formatted text

**Pass/Fail:** ______

---

### CE-006: Delete and Restore
**Priority:** High  
**Time:** ~2 minutes

**Steps:**
1. Select the Notes element  
   **Expected:** Element highlighted

2. Right-click and select Delete  
   **Expected:** Confirmation dialog

3. Confirm deletion  
   **Expected:** Element disappears

4. Right-click parent and select Restore  
   **Expected:** Restore option available

5. Click Restore  
   **Expected:** Element returns

**Pass/Fail:** ______

---

## CORE ELEMENTS RESULT: _____ / 6 Passed

**Critical Issues**: ________________

**Tested by**: _________ **Date**: _________