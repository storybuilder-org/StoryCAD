# StoryWorld / Worldbuilding Test Plan
**Time**: ~15 minutes
**Purpose**: Verify the StoryWorld feature — all 9 tabs, list entry management, and data persistence
**Priority**: Tier 1 (blocks release)

## Platform Notes

| Action | Windows | macOS |
|--------|---------|-------|
| Keyboard shortcuts | Ctrl+S to save | Cmd+S to save |
| Rich text fields | RichEditBox (WinUI) | RichEditBox (UNO desktop) |
| File dialogs | Windows native | macOS native |

**macOS-specific concerns**: Rich text editor behavior may differ on UNO desktop head. Pay attention to text formatting, cursor placement, and copy/paste within Expander fields.

## Setup
1. Launch StoryCAD
2. Create a new story: File > New Story
3. Save as "WorldTest.stbx"
4. Right-click Story Overview and select Add > StoryWorld
5. Verify "New StoryWorld" node appears in the tree

---

### SW-001: Structure Tab — World Type Selection
**Priority:** Critical
**Time:** ~2 minutes

**Steps:**
1. Select the StoryWorld node in the tree
   **Expected:** StoryWorld page opens on Structure tab (tab 0)

2. Open the World Type dropdown
   **Expected:** 8 options listed: Consensus Reality, Enchanted Reality, Hidden World, Divergent World, Constructed World, Mythic World, Estranged World, Broken World

3. Select "Enchanted Reality"
   **Expected:** Description panel updates with Enchanted Reality description and examples

4. Change to "Constructed World"
   **Expected:** Description and examples update to match Constructed World

5. Save (Ctrl+S / Cmd+S), close and reopen the file
   **Expected:** World Type is still "Constructed World" with correct description

**Pass/Fail:** ______

---

### SW-002: Physical Worlds Tab — List Entry Management
**Priority:** Critical
**Time:** ~3 minutes

**Steps:**
1. Click the "Physical Worlds" tab (tab 2)
   **Expected:** Empty state or default entry; Add/Remove buttons visible

2. Click "+ Add World"
   **Expected:** New entry appears with Name field; position shows "1 of 1"

3. Enter "Eldoria" in the Name field
   **Expected:** Name field updates

4. Expand the "Geography" expander and enter text
   **Expected:** Expander opens; text accepted in rich text field; header becomes **bold** (SemiBold)

5. Expand "Climate" and enter text
   **Expected:** Same behavior; Geography text still present when collapsing/expanding

6. Click "+ Add World" again
   **Expected:** Position shows "2 of 2"; new entry with blank fields

7. Enter "Shadow Realm" as name; add some Flora text
   **Expected:** Fields accept input

8. Click Back arrow to navigate to entry 1
   **Expected:** Position shows "1 of 2"; "Eldoria" with Geography and Climate text preserved

9. Click Forward arrow to return to entry 2
   **Expected:** "Shadow Realm" with Flora text preserved

10. Save, close, and reopen
    **Expected:** Both worlds preserved with all field content

**Pass/Fail:** ______

---

### SW-003: Physical Worlds Tab — Remove Entry
**Priority:** High
**Time:** ~1 minute

**Steps:**
1. Navigate to Physical Worlds tab with 2+ entries
   **Expected:** Position shows current entry

2. Navigate to the entry you want to remove
   **Expected:** Entry displayed

3. Click "Remove World"
   **Expected:** Entry removed; position updates (e.g., "1 of 1")

4. Verify remaining entry is correct
   **Expected:** Correct entry still present with all data

**Pass/Fail:** ______

---

### SW-004: People/Species Tab
**Priority:** High
**Time:** ~2 minutes

**Steps:**
1. Click "People/Species" tab (tab 3)
   **Expected:** Tab displays with Add/Remove Species buttons

2. Click "+ Add Species"
   **Expected:** New entry; position "1 of 1"

3. Enter "Elves" as Name
   **Expected:** Name field updates

4. Fill in Physical Traits and Lifespan expanders
   **Expected:** Text accepted; expander headers become bold when content present

5. Add a second species "Dwarves" with Origins text
   **Expected:** Position "2 of 2"; navigate back confirms "Elves" data preserved

**Pass/Fail:** ______

---

### SW-005: Cultures Tab
**Priority:** High
**Time:** ~2 minutes

**Steps:**
1. Click "Cultures" tab (tab 4)
   **Expected:** Tab displays with Add/Remove Culture buttons

2. Add a culture "Nomadic Tribes"
   **Expected:** Entry created

3. Fill in Values, Customs, and Taboos expanders
   **Expected:** All three accept text; headers bold when populated

4. Add a second culture "City Dwellers" with Daily Life text
   **Expected:** Both cultures preserved when navigating between them

**Pass/Fail:** ______

---

### SW-006: Governments Tab
**Priority:** Medium
**Time:** ~1 minute

**Steps:**
1. Click "Governments" tab (tab 5)
   **Expected:** Tab displays with Add/Remove Government buttons

2. Add "The Council" government
   **Expected:** Entry created

3. Fill in Type ("Democracy") and Power Structures
   **Expected:** Expanders accept text

4. Navigate away and back
   **Expected:** Data preserved

**Pass/Fail:** ______

---

### SW-007: Religions Tab
**Priority:** Medium
**Time:** ~1 minute

**Steps:**
1. Click "Religions" tab (tab 6)
   **Expected:** Tab displays with Add/Remove Religion buttons

2. Add "The Old Faith" religion
   **Expected:** Entry created

3. Fill in Deities and Beliefs expanders
   **Expected:** Text accepted

4. Navigate away and back
   **Expected:** Data preserved

**Pass/Fail:** ______

---

### SW-008: History Tab (Single Entry)
**Priority:** Medium
**Time:** ~1 minute

**Steps:**
1. Click "History" tab (tab 7)
   **Expected:** Tab displays; NO Add/Remove buttons (single entry per story)

2. Fill in Founding Events and Major Conflicts
   **Expected:** Expanders accept text; headers bold when populated

3. Navigate to another tab, then return
   **Expected:** History text preserved

**Pass/Fail:** ______

---

### SW-009: Economy Tab (Single Entry)
**Priority:** Medium
**Time:** ~1 minute

**Steps:**
1. Click "Economy" tab (tab 8)
   **Expected:** Tab displays; no Add/Remove buttons

2. Fill in Economic System and Currency
   **Expected:** Text accepted

3. Save, close, reopen
   **Expected:** Economy data preserved

**Pass/Fail:** ______

---

### SW-010: Magic/Tech Tab (Single Entry)
**Priority:** Medium
**Time:** ~1 minute

**Steps:**
1. Click "Magic/Tech" tab (tab 9)
   **Expected:** Tab displays with System Type dropdown; no Add/Remove buttons

2. Select a System Type from the dropdown
   **Expected:** Selection accepted

3. Fill in Source and Rules expanders
   **Expected:** Text accepted

4. Save, close, reopen
   **Expected:** System Type and all text preserved

**Pass/Fail:** ______

---

### SW-011: Full Round-Trip Save/Load
**Priority:** Critical
**Time:** ~2 minutes

**Steps:**
1. Populate data across all 9 tabs (at least one field per tab, multiple list entries on Physical Worlds and Species)
   **Expected:** All data entered without errors

2. Save the file (Ctrl+S / Cmd+S)
   **Expected:** Save completes

3. Close and reopen the file
   **Expected:** All 9 tabs retain their data; list entries have correct count and content

4. Verify list navigation works on reopened file
   **Expected:** Back/Forward arrows navigate correctly; position displays correct "N of M"

**Pass/Fail:** ______

---

## STORYWORLD TEST RESULT: _____ / 11 Passed

**Critical Issues**: ________________

**Tested by**: _________ **Date**: _________ **Platform**: _________
