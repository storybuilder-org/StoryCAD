# StoryCAD Tools Menu Test Plan
**Time**: ~15 minutes  
**Purpose**: Verify all Tools menu functions work correctly

## Setup
1. Launch StoryCAD
2. Open sample outline "Danger Calls" (File > Open Sample Outline)
3. Disable auto-save in Preferences if not already done

---

### TT-001: Master Plots Tool
**Priority:** High  
**Time:** ~2 minutes

**Steps:**
1. Select Story Overview node  
   **Expected:** Node highlighted

2. Click Tools > Master Plots  
   **Expected:** Master Plots dialog opens with list

3. Select "The Quest" plot  
   **Expected:** Description appears on right

4. Click "Copy to Outline"  
   **Expected:** Plot structure added as new nodes

5. Verify nodes in tree  
   **Expected:** New Problem/Scene nodes visible

**Pass/Fail:** ______

---

### TT-002: Dramatic Situations Tool  
**Priority:** Medium  
**Time:** ~2 minutes

**Steps:**
1. Select any Scene node  
   **Expected:** Scene form displays

2. Click Tools > Dramatic Situations  
   **Expected:** Dialog with 36 situations opens

3. Select "Deliverance" (#1)  
   **Expected:** Description and examples shown

4. Click Copy/Apply  
   **Expected:** Situation text added to scene

**Pass/Fail:** ______

---

### TT-003: Stock Scenes Tool
**Priority:** Medium  
**Time:** ~2 minutes

**Steps:**
1. Click Tools > Stock Scenes  
   **Expected:** Stock Scenes dialog opens

2. Expand "Action" category  
   **Expected:** List of action scenes appears

3. Select "Chase Scene"  
   **Expected:** Description displayed

4. Click Copy to Outline  
   **Expected:** Scene added to current location

**Pass/Fail:** ______

---

### TT-004: Key Questions Tool
**Priority:** High  
**Time:** ~2 minutes

**Steps:**
1. Select a Character node  
   **Expected:** Character form displays

2. Click Tools > Key Questions  
   **Expected:** Character-specific questions appear

3. Review questions  
   **Expected:** Questions relevant to character development

4. Select different node type (Problem)  
   **Expected:** Questions update for Problem type

**Pass/Fail:** ______

---

### TT-005: Search Function
**Priority:** High  
**Time:** ~2 minutes

**Steps:**
1. Click Tools > Search (or Ctrl+F)  
   **Expected:** Search box appears

2. Type "the"  
   **Expected:** Tree filters to show matching nodes

3. Clear search box  
   **Expected:** All nodes reappear

4. Search for text not in outline  
   **Expected:** "No results" or empty tree

**Pass/Fail:** ______

---

### TT-006: Inner/Outer Traits Tools
**Priority:** Low  
**Time:** ~2 minutes

**Steps:**
1. Select a Character node  
   **Expected:** Character form displays

2. Click Tools > Inner Traits  
   **Expected:** Psychological traits dialog opens

3. Select 2-3 traits  
   **Expected:** Traits added to character

4. Click Tools > Outer Traits  
   **Expected:** Physical/behavioral traits dialog

5. Select 2-3 traits  
   **Expected:** Traits added to character  

**Pass/Fail:** ______

---

### TT-007: Conflict Builder Tool
**Priority:** Low  
**Time:** ~1 minute

**Steps:**
1. Select Problem or Scene node  
   **Expected:** Form displays

2. Click Tools > Conflict Builder  
   **Expected:** Conflict types dialog opens

3. Select "Person vs Nature"  
   **Expected:** Examples and details shown

4. Apply to element  
   **Expected:** Conflict type set in form

**Pass/Fail:** ______

---

### TT-008: Narrative Editor Tool
**Priority:** Medium  
**Time:** ~2 minutes

**Steps:**
1. Ensure Story Explorer view active  
   **Expected:** Hierarchical tree visible

2. Click Tools > Narrative Editor  
   **Expected:** Editor dialog opens

3. Move 2 scenes to narrative  
   **Expected:** Scenes appear in right panel

4. Click OK/Apply  
   **Expected:** Changes reflected in Story Narrator view

**Pass/Fail:** ______

---

## TOOLS TEST RESULT: _____ / 8 Passed

**Issues Found**:
_____________________

**Tested by**: _________ **Date**: _________ **Build**: _________