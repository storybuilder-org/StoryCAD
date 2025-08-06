# Core Navigation Test Plan
**Time**: ~10 minutes  
**Purpose**: Verify navigation between elements and views

## Setup
1. Launch StoryCAD
2. Open sample outline "Danger Calls" or similar with multiple elements
3. Ensure both Explorer and Narrator views are available

---

### CN-001: Tree Navigation with Mouse
**Priority:** Critical  
**Time:** ~2 minutes

**Steps:**
1. Click on Story Overview node  
   **Expected:** Content pane shows Story Overview form

2. Click on a Character node  
   **Expected:** Content pane updates to Character form

3. Double-click parent node with children  
   **Expected:** Node expands/collapses

4. Click expand/collapse arrow  
   **Expected:** Children show/hide

**Pass/Fail:** ______

---

### CN-002: Tree Navigation with Keyboard
**Priority:** High  
**Time:** ~2 minutes

**Steps:**
1. Click any node to focus tree  
   **Expected:** Node highlighted

2. Press Up/Down arrows  
   **Expected:** Selection moves vertically

3. Press Right arrow on parent  
   **Expected:** Node expands

4. Press Left arrow on expanded parent  
   **Expected:** Node collapses

5. Press Enter on node  
   **Expected:** Node expands/collapses

**Pass/Fail:** ______

---

### CN-003: Tab Navigation
**Priority:** Critical  
**Time:** ~2 minutes

**Steps:**
1. Select Character with multiple tabs  
   **Expected:** Tab bar visible

2. Click each tab (Role, Physical, Social, etc.)  
   **Expected:** Content changes for each tab

3. Enter data and switch tabs  
   **Expected:** Data persists when returning

4. Resize window to narrow width  
   **Expected:** Tab carousel arrows appear if needed

**Pass/Fail:** ______

---

### CN-004: View Switching
**Priority:** High  
**Time:** ~1 minute

**Steps:**
1. Check status bar for current view  
   **Expected:** Shows "Story Explorer"

2. Click view selector  
   **Expected:** Switches to "Story Narrator"

3. Verify scene order in Narrator  
   **Expected:** Shows narrative sequence

4. Switch back to Explorer  
   **Expected:** Returns to hierarchical view

**Pass/Fail:** ______

---

### CN-005: Search Navigation
**Priority:** High  
**Time:** ~2 minutes

**Steps:**
1. Press Ctrl+F  
   **Expected:** Search box appears

2. Type character name  
   **Expected:** Tree filters to matching nodes

3. Click on filtered result  
   **Expected:** Content pane updates

4. Clear search (Esc or X)  
   **Expected:** Full tree returns

**Pass/Fail:** ______

---

### CN-006: Drag and Drop Movement
**Priority:** Medium  
**Time:** ~1 minute

**Steps:**
1. Click and hold a Scene node  
   **Expected:** Drag cursor appears

2. Drag to different Problem parent  
   **Expected:** Drop indicator shows

3. Release to drop  
   **Expected:** Scene moves to new parent

4. Try invalid drop (e.g., Story Overview)  
   **Expected:** Drop rejected/cancelled

**Pass/Fail:** ______

---

## CORE NAVIGATION RESULT: _____ / 6 Passed

**Critical Issues**: ________________

**Tested by**: _________ **Date**: _________