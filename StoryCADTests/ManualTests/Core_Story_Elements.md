# Core Story Elements Test Plan
**Time**: ~10 minutes  
**Purpose**: Verify creation and editing of all story element types

## Platform Notes

| Action | Windows | macOS |
|--------|---------|-------|
| Context menu | Right-click | Right-click or Ctrl+click |
| Tab switching | Click tab | Click tab |
| Text entry | Standard | Standard |

**macOS testers**: Right-click works on macOS with a two-button mouse or trackpad. Alternatively, Ctrl+click opens context menus. All element types should behave identically on both platforms.

**Missing element types**: This plan covers Character, Problem, Scene, Setting, and Notes. WebPage, Section, and StoryWorld are tested in their own plans (see StoryWorld_Test_Plan.md).

## Setup
1. Launch StoryCAD
2. Create a new outline named "ElementsTest" (File > Open/Create file > Create new outline). StoryCAD writes ElementsTest.stbx when it creates the outline.

---

### CE-001: Create Character
**Priority:** Critical  
**Time:** ~2 minutes

**Steps:**
1. Right-click the outline's root node  
   **Expected:** Context menu appears

2. Select Add Elements > Add Character  
   **Expected:** "New Character" node appears

3. Enter "Alice Smith" in Name field  
   **Expected:** Tree updates immediately

4. Select "Major Role" for Story Role  
   **Expected:** Selection saved

5. Switch to Physical tab and fill in a field (e.g., Age, Build, or Appearance)
   **Expected:** Data saves when switching tabs

**Pass/Fail:** ______

---

### CE-002: Create Problem
**Priority:** Critical  
**Time:** ~2 minutes

**Steps:**
1. Select the root node, then click the Add (+) button in the toolbar  
   **Expected:** Add menu dropdown

2. Select Add Problem  
   **Expected:** "New Problem" node appears

3. Enter "Main Conflict" as Problem name  
   **Expected:** Tree updates

4. Select "Person vs. Person" as Conflict Type  
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

2. Select Add Elements > Add Scene  
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
1. Right-click any node, select Add Elements > Add Setting  
   **Expected:** "New Setting" appears

2. Enter "Coffee Shop" as Setting name  
   **Expected:** Tree updates

3. Add "Downtown Seattle" as Locale  
   **Expected:** Field accepts text

4. Set Period as "Present Day"
   **Expected:** Field accepts text

**Pass/Fail:** ______

---

### CE-005: Create Notes
**Priority:** Medium  
**Time:** ~1 minute

**Steps:**
1. Right-click and select Add Elements > Add Notes  
   **Expected:** "New Note" appears

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

2. Right-click it and select Delete element  
   **Expected:** Element moves under Deleted Story Elements. No dialog appears, because no other element refers to it (a referenced element asks "Are you sure you want to delete this node?" first).

3. Right-click the element under Deleted Story Elements  
   **Expected:** Restore Element is available

4. Click Restore Element  
   **Expected:** Element returns to the outline; the status bar says "Restored node Research Notes and all its contents"

**Pass/Fail:** ______

---

## CORE ELEMENTS RESULT: _____ / 6 Passed

**Critical Issues**: ________________

**Tested by**: _________ **Date**: _________