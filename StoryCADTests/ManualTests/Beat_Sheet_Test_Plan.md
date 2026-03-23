# Beat Sheet Test Plan
**Time**: ~60–90 minutes
**Purpose**: Verify all beat sheet usability improvements from Issue #1339 — beat sheet selection, beat assignment/unassignment, beat editing, UI layout, persistence, reports, edge cases, and cross-feature integration.

## Platform Notes

| Action | Windows | macOS |
|--------|---------|-------|
| Context menu | Right-click | Right-click or Ctrl+click |
| Tab switching | Click tab | Click tab |
| Dialog confirmation | Enter or click OK | Enter or click OK |
| File picker | Standard Windows dialog | Standard macOS sheet |

**macOS testers**: All Structure tab behavior is cross-platform. File save/load dialogs look different but function identically. Report generation uses the same dialog on both platforms.

## Setup

1. Launch StoryCAD
2. Create a new story outline: **File > New**
3. Name it "BeatSheetTest" and save as `BeatSheetTest.stbx` in a dedicated test folder
4. Add a Story Problem: right-click the Story Overview node, select **Add > Problem**, name it "Main Conflict"
5. Add a second Problem as a sub-problem: right-click "Main Conflict", select **Add > Problem**, name it "Sub-Problem A"
6. Add at least three Scenes under the Story Overview: name them "Opening Scene", "Midpoint Scene", "Climax Scene"
7. On the Story Overview's Premise tab, set the Story Problem to "Main Conflict"
8. Save the file

---

## Section 1: Beat Sheet Selection

### BS-001: Load a built-in template from the dropdown
**Priority:** Critical
**Time:** ~2 minutes

**Preconditions:** The "Main Conflict" problem is open. The Structure tab is visible. No beat sheet is currently loaded (dropdown shows blank or placeholder).

**Steps:**
1. Click the Structure tab on the "Main Conflict" problem form.
   **Expected:** The beat sheet dropdown is visible and empty. Both lists are empty. Seven buttons are visible between the lists.

2. Open the beat sheet dropdown.
   **Expected:** Dropdown shows built-in templates including at least "Save the Cat", "Hero's Journey", and "Seven Point Story Structure".

3. Select "Save the Cat".
   **Expected:** The beats list populates with the 15 standard Save the Cat beats (Opening Image, Theme Stated, Set-Up, Catalyst, Debate, Break into Two, B Story, Fun and Games, Midpoint, Bad Guys Close In, All is Lost, Dark Night of the Soul, Break into Three, Finale, Final Image). The elements list remains empty until a radio button is selected.

4. Verify no edits are present yet.
   **Expected:** Beats list shows exactly 15 beats. No beats show assignment indicators other than the bullet icon.

**Pass/Fail:** ______

---

### BS-002: Switch to a different template — confirm dialog appears
**Priority:** Critical
**Time:** ~2 minutes

**Preconditions:** "Save the Cat" is loaded on "Main Conflict" from BS-001. At least one beat has been assigned (assign any scene to any beat to create unsaved state; see Section 2 for assign steps, or do a quick assign here).

**Steps:**
1. Assign "Opening Scene" to the first beat ("Opening Image") using the Assign button.
   **Expected:** Beat shows "Opening Scene" with the scene icon.

2. Open the beat sheet dropdown again.
   **Expected:** Dropdown opens.

3. Select "Hero's Journey".
   **Expected:** A confirmation dialog appears. Dialog text warns that switching templates will clear existing beat assignments. Dialog has at minimum a confirm and a cancel button.

4. Note the exact dialog text.
   **Expected:** Dialog mentions that existing assignments will be lost (wording may vary but must communicate data loss risk).

**Pass/Fail:** ______

---

### BS-003: Cancel the template switch — assignments preserved
**Priority:** High
**Time:** ~1 minute

**Preconditions:** Confirmation dialog from BS-002 is visible (or re-trigger it: load Save the Cat with one assignment, then try to switch to Hero's Journey).

**Steps:**
1. Click Cancel (or the equivalent dismiss button) on the confirmation dialog.
   **Expected:** Dialog closes. Beat sheet dropdown still shows "Save the Cat". The beat list still shows the 15 Save the Cat beats. The previously assigned beat ("Opening Image") still shows "Opening Scene".

**Pass/Fail:** ______

---

### BS-004: Confirm the template switch — assignments cleared
**Priority:** Critical
**Time:** ~1 minute

**Preconditions:** "Save the Cat" is loaded with at least one assignment. Confirmation dialog is visible (trigger from BS-002 step 3).

**Steps:**
1. Click Confirm (OK or equivalent) on the confirmation dialog.
   **Expected:** Beat sheet switches to "Hero's Journey". The beats list now shows Hero's Journey beats. The previously assigned beat is gone. No assignments carry over from Save the Cat.

**Pass/Fail:** ______

---

### BS-005: Load a beat sheet from a file (.stbeat)
**Priority:** High
**Time:** ~3 minutes

**Preconditions:** A valid `.stbeat` file exists on disk. (If none exists, create one by running BS-016 first to save a sheet, then use it here.) The Structure tab is open on any problem.

**Steps:**
1. Look for a Load or Open button or dropdown option that opens a file picker for `.stbeat` files. (This may be a dropdown item, a button, or available through the Save button area — check the UI for the correct trigger.)
   **Expected:** A file picker dialog opens.

2. Navigate to the `.stbeat` file and select it.
   **Expected:** Dialog closes. The beat sheet dropdown reflects the loaded file name or "Custom". The beats list populates with the beats from the file.

3. Verify beat count and titles match what was saved.
   **Expected:** Beats are identical to what was in the file.

**Pass/Fail:** ______

---

### BS-006: Behavior when no beat sheet is selected
**Priority:** Medium
**Time:** ~1 minute

**Preconditions:** Open a problem that has never had a beat sheet loaded. Structure tab is visible.

**Steps:**
1. Navigate to the Structure tab without selecting a beat sheet from the dropdown.
   **Expected:** Both the beats list and elements list are empty (or show placeholder text). The seven command buttons are present but the Assign, Unassign, Delete, Move Up, Move Down buttons are disabled (grayed out) since there are no beats to act on.

2. Attempt to click the Add Beat button without a beat sheet loaded.
   **Expected:** Either a new empty beat is added and the beat sheet is implicitly created as a custom sheet, or the button is disabled. Document which behavior occurs.

**Pass/Fail:** ______

---

## Section 2: Beat Assignment

### BA-001: Assign a scene to a beat
**Priority:** Critical
**Time:** ~2 minutes

**Preconditions:** "Save the Cat" is loaded on "Main Conflict". "Opening Scene", "Midpoint Scene", and "Climax Scene" exist in the outline.

**Steps:**
1. Click the Scene radio button above the elements list.
   **Expected:** The elements list populates with all scenes from the outline: "Opening Scene", "Midpoint Scene", "Climax Scene".

2. Select the first beat in the beats list ("Opening Image").
   **Expected:** Beat is highlighted. The bottom-left description panel shows the beat's description.

3. Select "Opening Scene" in the elements list.
   **Expected:** Element is highlighted. The bottom-right description panel shows the scene's description (Sketch or other text, if any).

4. Click the Assign button (left chevron).
   **Expected:** The "Opening Image" beat now displays "Opening Scene" and shows the scene icon (globe). The bullet icon is replaced by the scene icon. The beat's assignment line shows the scene name.

**Pass/Fail:** ______

---

### BA-002: Assign a problem to a beat
**Priority:** Critical
**Time:** ~2 minutes

**Preconditions:** "Save the Cat" is loaded on "Main Conflict". "Sub-Problem A" exists in the outline.

**Steps:**
1. Click the Problem radio button above the elements list.
   **Expected:** The elements list populates with available problems. "Sub-Problem A" appears. "Main Conflict" itself should NOT appear (a problem cannot be assigned to itself — see BA-009).

2. Select the "B Story" beat in the beats list.
   **Expected:** Beat is highlighted.

3. Select "Sub-Problem A" in the elements list.
   **Expected:** Element is highlighted.

4. Click the Assign button.
   **Expected:** The "B Story" beat shows "Sub-Problem A" and displays the problem icon (question mark). The description panels update accordingly.

**Pass/Fail:** ______

---

### BA-003: Icon changes after assignment
**Priority:** High
**Time:** ~1 minute

**Preconditions:** At least one beat has a scene assigned (from BA-001) and at least one beat has a problem assigned (from BA-002). At least one beat is unassigned.

**Steps:**
1. Observe the beat icon column for the assigned scene beat ("Opening Image").
   **Expected:** Beat shows the scene icon (globe).

2. Observe the beat icon column for the assigned problem beat ("B Story").
   **Expected:** Beat shows the problem icon (question mark).

3. Observe the beat icon column for an unassigned beat (e.g., "Theme Stated").
   **Expected:** Beat shows the neutral bullet icon — NOT an X or Cancel icon.

**Pass/Fail:** ______

---

### BA-004: Description panels update on selection
**Priority:** High
**Time:** ~2 minutes

**Preconditions:** "Save the Cat" is loaded. At least one scene is assigned to a beat.

**Steps:**
1. Click an unassigned beat.
   **Expected:** Left description panel updates to show that beat's description. Right description panel is empty or shows placeholder text since no element is selected.

2. Click a different beat in the elements list side.
   **Expected:** Right description panel updates to show the selected element's description.

3. Click an assigned beat.
   **Expected:** Left description panel shows the beat's description. Right description panel shows the assigned element's description (even though the element is implicitly selected by the assignment).

4. Click between several beats and elements rapidly.
   **Expected:** Both panels update correctly each time without stale content.

**Pass/Fail:** ______

---

### BA-005: Assign the same scene to multiple beats
**Priority:** High
**Time:** ~2 minutes

**Preconditions:** "Save the Cat" is loaded. "Opening Scene" is already assigned to "Opening Image".

**Steps:**
1. Select the "Catalyst" beat.
   **Expected:** Beat is highlighted.

2. Ensure Scene radio is active. Select "Opening Scene" in the elements list.
   **Expected:** Element is highlighted.

3. Click Assign.
   **Expected:** "Catalyst" beat now also shows "Opening Scene". No error or warning dialog appears — scenes are explicitly allowed to appear in multiple beats. "Opening Image" still shows "Opening Scene" as well.

4. Verify both beats show "Opening Scene".
   **Expected:** "Opening Image" and "Catalyst" both display "Opening Scene" with the scene icon.

**Pass/Fail:** ______

---

### BA-006: Attempt to assign a problem that is already assigned — dialog appears
**Priority:** High
**Time:** ~2 minutes

**Preconditions:** "Sub-Problem A" is already assigned to the "B Story" beat (from BA-002).

**Steps:**
1. Select a different beat, e.g., "Midpoint".
   **Expected:** Beat is highlighted.

2. Click the Problem radio button. Select "Sub-Problem A" in the elements list.
   **Expected:** Element is highlighted.

3. Click Assign.
   **Expected:** A dialog appears indicating that "Sub-Problem A" is already assigned to another beat ("B Story") and asking whether to reassign. The dialog should offer options to reassign (move the assignment) or cancel.

**Pass/Fail:** ______

---

### BA-007: Reassign a problem from one beat to another
**Priority:** High
**Time:** ~1 minute

**Preconditions:** BA-006 dialog is visible (or re-trigger: Sub-Problem A is on B Story, attempt to assign it to Midpoint).

**Steps:**
1. Click the Reassign (or Yes/Confirm) option in the dialog.
   **Expected:** Dialog closes. "Sub-Problem A" now appears on "Midpoint" and is REMOVED from "B Story". "B Story" returns to the bullet icon (unassigned). Only one beat holds "Sub-Problem A" at any time.

**Pass/Fail:** ______

---

### BA-008: Cancel reassignment — original assignment preserved
**Priority:** Medium
**Time:** ~1 minute

**Preconditions:** "Sub-Problem A" is assigned to "Midpoint" (from BA-007). Attempt to assign it to another beat to trigger the dialog again.

**Steps:**
1. Select a third beat (e.g., "Dark Night of the Soul"). Select "Sub-Problem A" in the elements list. Click Assign.
   **Expected:** The reassignment dialog appears again.

2. Click Cancel on the dialog.
   **Expected:** Dialog closes. "Sub-Problem A" remains on "Midpoint". "Dark Night of the Soul" remains unassigned.

**Pass/Fail:** ______

---

### BA-009: Attempt to assign a problem to its own beat sheet
**Priority:** Medium
**Time:** ~1 minute

**Preconditions:** "Main Conflict" has the Structure tab open with Save the Cat loaded.

**Steps:**
1. Click the Problem radio button.
   **Expected:** The elements list shows available problems.

2. Verify that "Main Conflict" does NOT appear in the elements list.
   **Expected:** "Main Conflict" is absent from the list. A problem cannot be assigned to a beat on its own beat sheet.

**Pass/Fail:** ______

---

## Section 3: Beat Unassignment

### BU-001: Unassign a scene from a beat
**Priority:** Critical
**Time:** ~1 minute

**Preconditions:** "Opening Image" has "Opening Scene" assigned.

**Steps:**
1. Select the "Opening Image" beat in the beats list.
   **Expected:** Beat is highlighted. The beat shows "Opening Scene" and the scene icon.

2. Click the Unassign button (right chevron).
   **Expected:** "Opening Image" returns to showing the beat title only with the neutral bullet icon. "Unassigned" appears in the description area (or the description area shows the beat's own description only). No confirmation dialog is required for unassign.

**Pass/Fail:** ______

---

### BU-002: Unassign a problem — BoundStructure reference cleared
**Priority:** Critical
**Time:** ~1 minute

**Preconditions:** "Midpoint" beat has "Sub-Problem A" assigned.

**Steps:**
1. Select the "Midpoint" beat.
   **Expected:** Beat is highlighted showing "Sub-Problem A".

2. Click the Unassign button.
   **Expected:** "Midpoint" returns to bullet icon (unassigned). "Sub-Problem A" becomes available again in the Problem elements list for reassignment to any beat.

3. Switch to the Problem radio and verify "Sub-Problem A" is available (not grayed out or hidden) as if it were never assigned.
   **Expected:** "Sub-Problem A" appears normally in the elements list.

**Pass/Fail:** ______

---

## Section 4: Beat Editing

### BE-001: Add a new beat to a loaded beat sheet
**Priority:** Critical
**Time:** ~2 minutes

**Preconditions:** "Save the Cat" is loaded on "Main Conflict" with 15 beats.

**Steps:**
1. Select the "Finale" beat (second to last beat, or whichever is appropriate as an insertion point).
   **Expected:** Beat is highlighted.

2. Click the Add button (plus sign).
   **Expected:** A new beat is added after the selected beat. The new beat has a default placeholder name (e.g., "New Beat" or similar). The beats list now shows 16 beats total.

3. The new beat is selected automatically.
   **Expected:** The new beat is highlighted and its title field is editable (or a title/description area at the bottom is ready for input).

4. Type a title for the new beat: "Epilogue Moment".
   **Expected:** The beat title updates in the list as you type (or after committing the edit).

**Pass/Fail:** ______

---

### BE-002: Delete a beat — confirmation dialog appears
**Priority:** Critical
**Time:** ~1 minute

**Preconditions:** An unassigned beat exists. Save the Cat is loaded.

**Steps:**
1. Select an unassigned beat (e.g., "Theme Stated").
   **Expected:** Beat is highlighted.

2. Click the Delete button (trash can icon).
   **Expected:** A confirmation dialog appears. The dialog names the beat being deleted (e.g., "Delete beat 'Theme Stated'?"). The dialog has at minimum a confirm and a cancel button.

**Pass/Fail:** ______

---

### BE-003: Confirm beat delete — beat removed from list
**Priority:** Critical
**Time:** ~30 seconds

**Preconditions:** Confirmation dialog from BE-002 is visible.

**Steps:**
1. Click Confirm (OK or equivalent).
   **Expected:** Dialog closes. "Theme Stated" is removed from the beats list. The total beat count decreases by one. Adjacent beats remain and their order is preserved.

**Pass/Fail:** ______

---

### BE-004: Cancel beat delete — beat preserved
**Priority:** High
**Time:** ~30 seconds

**Preconditions:** Re-trigger the delete dialog: select "Set-Up" beat, click Delete.

**Steps:**
1. Click Cancel on the confirmation dialog.
   **Expected:** Dialog closes. "Set-Up" remains in the beats list. No beats were removed.

**Pass/Fail:** ______

---

### BE-005: Delete an assigned beat — assignment also removed
**Priority:** High
**Time:** ~2 minutes

**Preconditions:** "Opening Image" has "Opening Scene" assigned.

**Steps:**
1. Select "Opening Image" (showing "Opening Scene" assignment).
   **Expected:** Beat is highlighted showing the assignment.

2. Click Delete. Review the confirmation dialog text.
   **Expected:** Dialog text mentions that the beat's assignment will also be removed (e.g., "Delete beat 'Opening Image'? This will also remove its scene/problem assignment.").

3. Click Confirm.
   **Expected:** "Opening Image" is removed. "Opening Scene" is no longer assigned to any beat on this problem. The scene is available for reassignment.

**Pass/Fail:** ______

---

### BE-006: Move a beat up
**Priority:** High
**Time:** ~1 minute

**Preconditions:** Save the Cat is loaded with at least 3 beats. Note the order of beats 2 and 3.

**Steps:**
1. Select the third beat in the list.
   **Expected:** Beat is highlighted.

2. Click the Move Up button (up arrow).
   **Expected:** The selected beat moves one position up in the list. It now occupies the second position, and the beat that was second now occupies the third position. The beat remains selected after the move.

**Pass/Fail:** ______

---

### BE-007: Move a beat down
**Priority:** High
**Time:** ~1 minute

**Preconditions:** Save the Cat is loaded with at least 3 beats.

**Steps:**
1. Select the first beat in the list.
   **Expected:** Beat is highlighted.

2. Click the Move Down button (down arrow).
   **Expected:** The selected beat moves one position down. It now occupies the second position. The original second beat now occupies the first position. The beat remains selected.

**Pass/Fail:** ______

---

### BE-008: Move Up disabled at the top of the list
**Priority:** Medium
**Time:** ~30 seconds

**Preconditions:** Save the Cat is loaded.

**Steps:**
1. Select the very first beat in the list.
   **Expected:** Beat is highlighted.

2. Observe the Move Up button.
   **Expected:** Move Up button is disabled (grayed out or non-interactive). There is nowhere to move up to.

**Pass/Fail:** ______

---

### BE-009: Move Down disabled at the bottom of the list
**Priority:** Medium
**Time:** ~30 seconds

**Preconditions:** Save the Cat is loaded.

**Steps:**
1. Select the very last beat in the list.
   **Expected:** Beat is highlighted.

2. Observe the Move Down button.
   **Expected:** Move Down button is disabled. There is nowhere to move down to.

**Pass/Fail:** ______

---

### BE-010: Edit a beat's title inline
**Priority:** High
**Time:** ~2 minutes

**Preconditions:** Save the Cat is loaded. A beat is visible with its original title.

**Steps:**
1. Select the "Catalyst" beat.
   **Expected:** Beat is highlighted.

2. Find the title editing mechanism (this may be an inline text field in the list item, or a field in the description area at the bottom — identify which applies to this build).
   **Expected:** A text field for the beat title is accessible.

3. Change the title to "Inciting Incident".
   **Expected:** The beats list updates to show "Inciting Incident" for that beat.

4. Click another beat and then click back.
   **Expected:** The renamed beat still shows "Inciting Incident". The change persisted.

**Pass/Fail:** ______

---

### BE-011: Edit a beat's description
**Priority:** Medium
**Time:** ~2 minutes

**Preconditions:** Save the Cat is loaded. A beat is selected and its description is visible in the bottom-left panel.

**Steps:**
1. Select a beat and observe its description in the bottom-left panel.
   **Expected:** The beat's original description is shown.

2. Click into the description panel (if it is editable) and modify the text.
   **Expected:** The text field accepts input.

3. Click away to another beat, then return.
   **Expected:** The modified description is retained for that beat.

**Pass/Fail:** ______

---

### BE-012: Save a modified beat sheet to a .stbeat file
**Priority:** High
**Time:** ~2 minutes

**Preconditions:** Save the Cat is loaded and has been modified: at least one beat renamed or one beat added (from BE-001 or BE-010).

**Steps:**
1. Click the Save button (floppy disk icon).
   **Expected:** A file save dialog opens. The default file extension is `.stbeat`. A default filename is suggested (e.g., based on the original template name or "Custom").

2. Save the file as `MyCustomSheet.stbeat` to the test folder.
   **Expected:** Dialog closes. No error. The file appears in the test folder.

3. Inspect the file with a text editor (outside StoryCAD).
   **Expected:** The file contains JSON. The beats are represented with their titles and descriptions. The format is human-readable.

**Pass/Fail:** ______

---

### BE-013: Load a .stbeat file and verify content
**Priority:** High
**Time:** ~2 minutes

**Preconditions:** `MyCustomSheet.stbeat` was saved in BE-012. Navigate to a different problem (e.g., "Sub-Problem A") that has no beat sheet loaded.

**Steps:**
1. Open the Structure tab on "Sub-Problem A".
   **Expected:** No beat sheet loaded.

2. Use the load-from-file mechanism to open `MyCustomSheet.stbeat`.
   **Expected:** The beats list populates with the beats from the saved file, including the renamed or added beat from the saved custom sheet.

3. Verify the beat titles and count match what was saved.
   **Expected:** Exact match.

**Pass/Fail:** ______

---

## Section 5: Element Source Toggle

### ET-001: Switch from Scene list to Problem list
**Priority:** Critical
**Time:** ~1 minute

**Preconditions:** Save the Cat is loaded. Both scenes and problems exist in the outline. Scene radio button is currently selected and the elements list shows scenes.

**Steps:**
1. Note the current contents of the elements list (scenes: "Opening Scene", "Midpoint Scene", "Climax Scene").
   **Expected:** Three scenes visible.

2. Click the Problem radio button.
   **Expected:** The elements list immediately replaces scenes with problems ("Sub-Problem A" and any other problems except "Main Conflict" itself).

3. Click the Scene radio button.
   **Expected:** The elements list immediately returns to showing scenes.

**Pass/Fail:** ______

---

### ET-002: Correct list populates based on toggle state
**Priority:** High
**Time:** ~1 minute

**Preconditions:** Both scenes and problems exist. Scene radio is active.

**Steps:**
1. Add a new scene during this session: right-click a node and add a Scene named "New Test Scene".
   **Expected:** New scene added to the outline.

2. Return to the Structure tab on "Main Conflict". Ensure Scene radio is selected.
   **Expected:** "New Test Scene" appears in the elements list alongside the existing scenes.

3. Switch to Problem radio.
   **Expected:** "New Test Scene" does NOT appear. Only problems appear.

4. Switch back to Scene radio.
   **Expected:** "New Test Scene" is present in the list.

**Pass/Fail:** ______

---

## Section 6: UI and Layout

### UI-001: All seven buttons are visible and have icons
**Priority:** Critical
**Time:** ~1 minute

**Preconditions:** Save the Cat is loaded on "Main Conflict". Structure tab is fully visible.

**Steps:**
1. Examine the column of buttons between the beats list and elements list.
   **Expected:** Exactly seven buttons are visible. Each button shows a recognizable icon (not a text label or single character).

2. Verify the icons are: left chevron (Assign), right chevron (Unassign), plus sign (Add), trash can (Delete), up arrow (Move Up), down arrow (Move Down), floppy disk (Save).
   **Expected:** All seven icons are present and match the descriptions above.

3. Verify button size appears large enough to be easy to click (target size: approximately 48x48 pixels or visually substantial).
   **Expected:** Buttons are larger and more prominent than typical toolbar icon buttons.

**Pass/Fail:** ______

---

### UI-002: Tooltips appear on all seven buttons
**Priority:** High
**Time:** ~2 minutes

**Preconditions:** Structure tab is open. Mouse is available (tooltips require hover).

**Steps:**
1. Hover the mouse over the Assign button (left chevron) and wait 1–2 seconds.
   **Expected:** A tooltip appears with text explaining the action (e.g., "Assign element to beat").

2. Hover over the Unassign button.
   **Expected:** Tooltip: "Remove assignment from beat" or equivalent.

3. Hover over the Add button.
   **Expected:** Tooltip: "Add new beat" or equivalent.

4. Hover over the Delete button.
   **Expected:** Tooltip: "Delete selected beat" or equivalent.

5. Hover over the Move Up button.
   **Expected:** Tooltip: "Move beat up" or equivalent.

6. Hover over the Move Down button.
   **Expected:** Tooltip: "Move beat down" or equivalent.

7. Hover over the Save button.
   **Expected:** Tooltip: "Save beat sheet to file" or equivalent.

**Pass/Fail:** ______

---

### UI-003: Unassigned beats show bullet icon (not X/Cancel icon)
**Priority:** High
**Time:** ~30 seconds

**Preconditions:** Save the Cat is loaded. At least one beat is unassigned.

**Steps:**
1. Observe the icon next to an unassigned beat in the beats list.
   **Expected:** The icon is a neutral bullet (dot or similar), NOT an X or a Cancel symbol. The icon does not suggest deletion.

2. Compare visually with an assigned beat's icon.
   **Expected:** Assigned beats show globe (scene) or question mark (problem) icons. Unassigned beats show a clearly distinct, neutral bullet icon.

**Pass/Fail:** ______

---

### UI-004: "Unassigned" label shown for beats with no assignment
**Priority:** High
**Time:** ~30 seconds

**Preconditions:** Save the Cat is loaded. An unassigned beat is selected.

**Steps:**
1. Select an unassigned beat.
   **Expected:** The beat row in the list or the description area for the assignment shows the text "Unassigned" in a secondary (lighter) text color, NOT "No element Selected" or blank.

**Pass/Fail:** ______

---

### UI-005: Card-style borders on both lists
**Priority:** Medium
**Time:** ~30 seconds

**Preconditions:** Save the Cat is loaded. Both lists contain items.

**Steps:**
1. Examine the beats list container.
   **Expected:** The beats list has a subtle rounded-corner border (card style). The border provides a clear visual boundary.

2. Examine the elements list container.
   **Expected:** The elements list also has a matching card-style border with rounded corners.

3. Verify there are no heavy vertical separator lines between the lists and the buttons column.
   **Expected:** The layout uses card borders and column spacing for separation — no thick vertical divider lines in the middle section.

**Pass/Fail:** ******

---

### UI-006: Description panel separator removed
**Priority:** Low
**Time:** ~30 seconds

**Preconditions:** A beat and an element are both selected so both description panels have content.

**Steps:**
1. Examine the bottom portion of the Structure tab where the two description panels appear side by side.
   **Expected:** No heavy vertical separator line exists between the left (beat description) and right (element description) panels. The panels are separated by whitespace or a minimal gap only.

**Pass/Fail:** ______

---

### UI-007: Layout proportions — description panels have adequate space
**Priority:** Medium
**Time:** ~1 minute

**Preconditions:** Save the Cat is loaded. A beat with a long description is selected (e.g., "Set-Up" which typically has a multi-sentence description).

**Steps:**
1. Observe the overall layout proportions of the Structure tab.
   **Expected:** The lists + buttons area and the description panels area use roughly a 3:2 ratio (lists area is taller than in previous versions; descriptions area is taller than in previous versions). Description text is readable without scrolling for typical beat descriptions.

2. Observe the column widths of the two lists.
   **Expected:** The beats list column is slightly wider than the elements list column (approximately 5:4 ratio), since beat items show two lines (title + assignment text).

**Pass/Fail:** ______

---

### UI-008: Layout survives window resize
**Priority:** Medium
**Time:** ~2 minutes

**Preconditions:** Save the Cat is loaded. The application window is at its default size.

**Steps:**
1. Resize the application window to approximately half its normal width.
   **Expected:** The Structure tab reflows without content being cut off or overlapping. Scroll bars appear if needed. Buttons remain visible and accessible.

2. Resize to a very tall but narrow window.
   **Expected:** Lists and description panels stack or reflow acceptably. No buttons disappear.

3. Restore the window to full size.
   **Expected:** Layout returns to normal.

**Pass/Fail:** ______

---

### UI-009: Collapse and expand navigation tree does not affect Structure tab content
**Priority:** Low
**Time:** ~1 minute

**Preconditions:** Save the Cat is loaded with several assignments visible.

**Steps:**
1. Note the current state of the beats list assignments.
   **Expected:** Assignments are visible.

2. Collapse the navigation tree panel (if there is a collapse button or splitter).
   **Expected:** The Structure tab content area expands to use the available space. Beat sheet content remains intact.

3. Expand the navigation tree again.
   **Expected:** Layout returns to normal. All beats and assignments are still present and correct.

**Pass/Fail:** ______

---

## Section 7: Persistence

### PE-001: Navigate away from Structure tab and return — state preserved
**Priority:** Critical
**Time:** ~2 minutes

**Preconditions:** Save the Cat is loaded on "Main Conflict" with at least two beats assigned (one scene, one problem).

**Steps:**
1. Note the current assignment state (which beats have which elements).
   **Expected:** Assignments visible.

2. Click to a different tab on the "Main Conflict" form (e.g., the Overview tab or any other tab).
   **Expected:** The other tab content is shown.

3. Click back to the Structure tab.
   **Expected:** The beat sheet is still "Save the Cat". All assignments are exactly as they were before navigating away. No assignments were lost.

**Pass/Fail:** ______

---

### PE-002: Navigate to a different story element and back — state preserved
**Priority:** Critical
**Time:** ~2 minutes

**Preconditions:** Same as PE-001 — assignments exist on "Main Conflict".

**Steps:**
1. Click on "Opening Scene" in the navigation tree to open the Scene form.
   **Expected:** Scene form opens.

2. Click back on "Main Conflict" in the navigation tree.
   **Expected:** "Main Conflict" form opens on the last-viewed tab (or Structure tab if it was active).

3. Navigate to the Structure tab.
   **Expected:** All beat assignments are exactly as they were. Beat sheet is still "Save the Cat".

**Pass/Fail:** ______

---

### PE-003: Save and reopen the file — beat assignments persist
**Priority:** Critical
**Time:** ~3 minutes

**Preconditions:** "Main Conflict" has Save the Cat loaded with at least two beats assigned. The file has been saved at least once as BeatSheetTest.stbx.

**Steps:**
1. Note the exact assignments (beat name → element name) for later verification.
   **Expected:** At least two assignments are present.

2. Save the file: **File > Save** (or Ctrl+S).
   **Expected:** File saves without error.

3. Close the file: **File > Close** or close and reopen the application.
   **Expected:** The file closes.

4. Reopen `BeatSheetTest.stbx`: **File > Open** and navigate to the file.
   **Expected:** File opens. The outline tree is restored.

5. Navigate to "Main Conflict" > Structure tab.
   **Expected:** "Save the Cat" is still selected in the dropdown. All previously assigned beats still show their assigned elements. No assignments were lost during save/load.

**Pass/Fail:** ______

---

### PE-004: Modified template beats persist after save/reload
**Priority:** High
**Time:** ~3 minutes

**Preconditions:** Save the Cat is loaded on "Main Conflict". Modify it: rename one beat and add one new beat (e.g., from BE-001 and BE-010). Save the file.

**Steps:**
1. Note the renamed beat and the added beat titles.
   **Expected:** Modifications are visible in the beats list.

2. Save the file and reopen it (as in PE-003 steps 2–4).
   **Expected:** File reopens successfully.

3. Navigate to "Main Conflict" > Structure tab.
   **Expected:** The beats list shows the modified Save the Cat template — the renamed beat has its new name, and the added beat "Epilogue Moment" (or whatever was added) is present. The modifications to the template are preserved.

**Pass/Fail:** ______

---

### PE-005: Verifying JSON format of .stbeat file
**Priority:** Low
**Time:** ~2 minutes

**Preconditions:** `MyCustomSheet.stbeat` was saved in BE-012.

**Steps:**
1. Open `MyCustomSheet.stbeat` in a text editor outside StoryCAD (e.g., Notepad, VS Code).
   **Expected:** The file is valid JSON. It is not binary or XML.

2. Verify the structure includes beat titles and descriptions.
   **Expected:** Each beat is represented as a JSON object with at minimum a title field and a description field.

3. Verify the file does not contain sensitive data or full outline content — only the beat sheet definition.
   **Expected:** No character names, scene summaries, or other story content from the outline is embedded in the file.

**Pass/Fail:** ______

---

## Section 8: Reports

### RP-001: Story Problem Structure checkbox generates three sub-reports
**Priority:** Critical
**Time:** ~3 minutes

**Preconditions:** The Story Problem is set to "Main Conflict" on the Story Overview's Premise tab. "Main Conflict" has Save the Cat loaded with at least one beat assigned to "Sub-Problem A". "Sub-Problem A" has any beat sheet loaded (even empty).

**Steps:**
1. Open the Generate Reports dialog: **File > Generate Reports** or equivalent menu location.
   **Expected:** The Generate Reports dialog opens with a list of available reports.

2. Locate the "Story Problem Structure" checkbox and check it.
   **Expected:** The checkbox is checked.

3. Generate the reports.
   **Expected:** Three reports are produced (either as separate pages/sections in one document, or clearly labeled sections): "Recursive Structure Report" (or similar title), "Plot Structure Diagram", and "Unassigned Elements".

4. Verify all three sections are present in the output.
   **Expected:** All three report sections exist and have content (not blank).

**Pass/Fail:** ______

---

### RP-002: Recursive Structure Report — hierarchical beat sheet display
**Priority:** High
**Time:** ~3 minutes

**Preconditions:** Same as RP-001 with reports generated.

**Steps:**
1. Open or scroll to the Recursive Structure Report section.
   **Expected:** The report is present.

2. Verify the report starts from "Main Conflict" (the Story Problem).
   **Expected:** "Main Conflict" appears at the top level.

3. Verify the report shows the beats of "Main Conflict"'s beat sheet.
   **Expected:** Save the Cat beats are listed in order under "Main Conflict".

4. Verify beats are numbered (1, 2, 3...).
   **Expected:** Each beat has a number prefix.

5. Verify that "Sub-Problem A" assigned to a beat is shown, and that the report then recurses into "Sub-Problem A"'s beat sheet.
   **Expected:** After the beat that references "Sub-Problem A", the report indents and shows "Sub-Problem A"'s beats as a nested section.

**Pass/Fail:** ______

---

### RP-003: Per-problem numbered beats with custom beat marker
**Priority:** High
**Time:** ~2 minutes

**Preconditions:** A beat sheet with at least one custom (added) beat is loaded on "Main Conflict" (add a beat if needed using BE-001).

**Steps:**
1. Regenerate reports with Story Problem Structure checked (or use reports from RP-001 if the custom beat was present).
   **Expected:** Reports generated.

2. Find the Recursive Structure section for "Main Conflict".
   **Expected:** Beats are numbered.

3. Locate the custom beat that was added (not from the original Save the Cat template).
   **Expected:** That beat is marked with an asterisk `*` to indicate it is a custom addition not present in the original template.

4. Verify the original template beats do NOT have the `*` marker.
   **Expected:** Only added beats carry the `*`.

**Pass/Fail:** ______

---

### RP-004: "Unassigned" shown for beats with no assignment in report
**Priority:** High
**Time:** ~2 minutes

**Preconditions:** Reports generated with Story Problem Structure checked. Several beats on "Main Conflict" are unassigned.

**Steps:**
1. Find the Recursive Structure section for "Main Conflict".
   **Expected:** Beats are listed.

2. Locate a beat that has no assignment.
   **Expected:** That beat shows "Unassigned" as its assignment text, not blank and not a dash or other placeholder. The word "Unassigned" is explicitly printed.

**Pass/Fail:** ______

---

### RP-005: Legend appears in report for custom beat marker
**Priority:** Medium
**Time:** ~1 minute

**Preconditions:** Same as RP-003 — at least one custom beat exists and is marked `*` in the report.

**Steps:**
1. Scroll to the bottom of the Recursive Structure Report (or the bottom of the Story Problem Structure section).
   **Expected:** A legend is present explaining that `*` marks beats added by the writer that are not part of the original template.

**Pass/Fail:** ______

---

### RP-006: Plot Structure Diagram — problems only, no scenes
**Priority:** High
**Time:** ~2 minutes

**Preconditions:** Reports generated. "Main Conflict" has at least one beat with "Opening Scene" assigned AND at least one beat with "Sub-Problem A" assigned.

**Steps:**
1. Find the Plot Structure Diagram section of the report.
   **Expected:** The section is present.

2. Verify "Main Conflict" appears at the root.
   **Expected:** Story Problem is at the top.

3. Verify "Sub-Problem A" appears as a linked sub-item (connected through the beat that references it).
   **Expected:** "Sub-Problem A" appears indented or linked below "Main Conflict".

4. Verify "Opening Scene" does NOT appear anywhere in the Plot Structure Diagram.
   **Expected:** Scenes are excluded from this diagram. Only problems appear.

**Pass/Fail:** ______

---

### RP-007: Unassigned Elements report — lists orphan scenes and problems
**Priority:** High
**Time:** ~2 minutes

**Preconditions:** Reports generated. "New Test Scene" (added in ET-002) has not been assigned to any beat. Verify at least one other scene or problem is also unassigned.

**Steps:**
1. Find the Unassigned Elements section of the report.
   **Expected:** The section is present and has two sub-sections: "Unassigned Problems" and "Unassigned Scenes".

2. Verify "New Test Scene" appears in the "Unassigned Scenes" section.
   **Expected:** The scene is listed.

3. Verify that scenes assigned to beats do NOT appear in the Unassigned Scenes section.
   **Expected:** "Opening Scene" (if assigned) is absent from Unassigned Scenes.

4. Verify that "Sub-Problem A" does NOT appear in Unassigned Problems if it is currently assigned to a beat.
   **Expected:** Assigned problems are excluded from Unassigned Problems.

**Pass/Fail:** ______

---

### RP-008: No Story Problem set — reports are blank
**Priority:** High
**Time:** ~2 minutes

**Preconditions:** Create a new, separate test outline with problems and scenes but NO Story Problem set on the Story Overview's Premise tab.

**Steps:**
1. Open the Generate Reports dialog on the new test outline.
   **Expected:** Dialog opens.

2. Check "Story Problem Structure" and generate.
   **Expected:** The three Story Problem Structure sub-reports are blank (no content), or a message states that no Story Problem has been set. No error or crash.

**Pass/Fail:** ______

---

## Section 9: Edge Cases

### EC-001: Empty outline — Structure tab loads without error
**Priority:** High
**Time:** ~1 minute

**Preconditions:** Create a brand new outline with only the Story Overview node and one Problem (no scenes, no sub-problems).

**Steps:**
1. Open the Structure tab on the single Problem.
   **Expected:** The tab loads without error.

2. Load "Save the Cat" from the dropdown.
   **Expected:** The beats list populates. The elements list is empty (no scenes or problems to show). No error.

3. Click the Scene radio button, then the Problem radio button.
   **Expected:** Both lists show empty content gracefully. No crash or error message.

**Pass/Fail:** ______

---

### EC-002: No beat sheet selected — buttons behave correctly
**Priority:** Medium
**Time:** ~1 minute

**Preconditions:** The Structure tab is open on a problem. No beat sheet is loaded (dropdown is blank).

**Steps:**
1. Attempt to click the Assign button.
   **Expected:** Button is disabled or produces no effect.

2. Attempt to click the Unassign button.
   **Expected:** Button is disabled or produces no effect.

3. Attempt to click Delete.
   **Expected:** Button is disabled or produces no effect.

4. Attempt to click Move Up or Move Down.
   **Expected:** Buttons are disabled.

5. Click the Add button.
   **Expected:** Either a new beat is created (and the problem now has a custom beat sheet), or the button is disabled and nothing happens. No crash.

**Pass/Fail:** ______

---

### EC-003: Long beat titles and descriptions
**Priority:** Medium
**Time:** ~2 minutes

**Preconditions:** Save the Cat is loaded.

**Steps:**
1. Select the "Catalyst" beat and edit its title to a very long string: "This is a very long beat title that exceeds normal display limits for testing purposes only".
   **Expected:** The title is accepted without truncation at the data level.

2. Observe the title display in the beats list.
   **Expected:** The list item either truncates with ellipsis or wraps the text. The UI does not overflow or break the layout.

3. Enter a long description (5+ sentences) for the same beat.
   **Expected:** The description panel handles the long text with a scroll bar if needed.

**Pass/Fail:** ______

---

### EC-004: Beat sheet with many beats (15+)
**Priority:** Medium
**Time:** ~2 minutes

**Preconditions:** Save the Cat is loaded (already has 15 beats). Add 5 more beats using the Add button.

**Steps:**
1. Add 5 additional beats to reach 20 total.
   **Expected:** All 20 beats appear in the beats list. A scroll bar appears if the list is taller than the visible area.

2. Scroll through the list.
   **Expected:** Scrolling is smooth. All beats are accessible.

3. Select beats near the top and bottom.
   **Expected:** Selection works correctly at any list position.

**Pass/Fail:** ______

---

### EC-005: Rapid clicking — no duplicate assignments or crashes
**Priority:** Medium
**Time:** ~2 minutes

**Preconditions:** Save the Cat is loaded. A scene and a beat are both selected.

**Steps:**
1. Click the Assign button rapidly 5 times in quick succession.
   **Expected:** The scene is assigned to the beat exactly once. No duplicate entries appear. No crash.

2. Click the Unassign button rapidly 5 times in quick succession on an assigned beat.
   **Expected:** The assignment is removed exactly once. No error on subsequent clicks (beat is already unassigned).

**Pass/Fail:** ______

---

### EC-006: Switch Story Problem midway — Structure tab updates
**Priority:** High
**Time:** ~3 minutes

**Preconditions:** "Main Conflict" is the Story Problem and has Save the Cat loaded with assignments. A second problem "Sub-Problem A" exists with a different beat sheet loaded (or none).

**Steps:**
1. Go to the Story Overview's Premise tab and change the Story Problem from "Main Conflict" to "Sub-Problem A".
   **Expected:** The Story Problem is updated.

2. Run the Story Problem Structure reports (RP-001).
   **Expected:** The reports now reflect "Sub-Problem A" as the root, not "Main Conflict".

3. Return to Story Overview and change the Story Problem back to "Main Conflict".
   **Expected:** Reports (if regenerated) reflect "Main Conflict" again.

**Pass/Fail:** ______

---

## Section 10: Cross-Feature Integration

### CF-001: Delete an assigned scene — beat assignment cleared
**Priority:** Critical
**Time:** ~2 minutes

**Preconditions:** "Opening Scene" is assigned to the "Opening Image" beat on "Main Conflict".

**Steps:**
1. In the navigation tree, right-click "Opening Scene" and select Delete.
   **Expected:** A confirmation dialog appears.

2. Confirm the deletion.
   **Expected:** "Opening Scene" is removed from the navigation tree.

3. Navigate to "Main Conflict" > Structure tab.
   **Expected:** The "Opening Image" beat no longer shows "Opening Scene". The beat reverts to the bullet icon (unassigned). The text "Unassigned" is shown. No stale reference or error.

**Pass/Fail:** ______

---

### CF-002: Delete an assigned problem — beat assignment cleared
**Priority:** Critical
**Time:** ~2 minutes

**Preconditions:** "Sub-Problem A" is assigned to a beat on "Main Conflict".

**Steps:**
1. In the navigation tree, right-click "Sub-Problem A" and delete it.
   **Expected:** Confirmation dialog appears.

2. Confirm deletion.
   **Expected:** "Sub-Problem A" is removed from the tree.

3. Navigate to "Main Conflict" > Structure tab.
   **Expected:** The beat that previously held "Sub-Problem A" now shows as unassigned (bullet icon, "Unassigned" text). No orphaned reference or crash.

**Pass/Fail:** ______

---

### CF-003: Rename a scene — beat assignment display updates
**Priority:** High
**Time:** ~2 minutes

**Preconditions:** "Midpoint Scene" is assigned to the "Midpoint" beat on "Main Conflict".

**Steps:**
1. Click on "Midpoint Scene" in the navigation tree to open it.
   **Expected:** Scene form opens.

2. Change the scene name to "Confrontation Scene".
   **Expected:** The navigation tree updates to show "Confrontation Scene".

3. Navigate to "Main Conflict" > Structure tab.
   **Expected:** The "Midpoint" beat now shows "Confrontation Scene" (the updated name), not the old name "Midpoint Scene". The assignment link is preserved; only the display name changed.

**Pass/Fail:** ______

---

### CF-004: Story Problem from Overview used by reports
**Priority:** High
**Time:** ~2 minutes

**Preconditions:** The Story Problem is set to "Main Conflict" on the Overview's Premise tab. "Main Conflict" has a beat sheet loaded. A second problem "Sub-Problem B" exists but is NOT the Story Problem and has its own beat sheet.

**Steps:**
1. Generate Story Problem Structure reports (RP-001 steps).
   **Expected:** Reports generated.

2. Verify the Recursive Structure Report starts from "Main Conflict", not "Sub-Problem B".
   **Expected:** "Sub-Problem B"'s beat sheet does not appear as the root. It only appears if "Main Conflict"'s beat sheet references it via a beat assignment.

3. Now change the Story Problem to "Sub-Problem B" on the Overview Premise tab and regenerate.
   **Expected:** The Recursive Structure Report now starts from "Sub-Problem B".

**Pass/Fail:** ______

---

## Test Results Summary

| Section | Tests | Passed | Failed | Skipped |
|---------|-------|--------|--------|---------|
| 1. Beat Sheet Selection | BS-001 to BS-006 | | | |
| 2. Beat Assignment | BA-001 to BA-009 | | | |
| 3. Beat Unassignment | BU-001 to BU-004 | | | |
| 4. Beat Editing | BE-001 to BE-013 | | | |
| 5. Element Source Toggle | ET-001 to ET-002 | | | |
| 6. UI and Layout | UI-001 to UI-009 | | | |
| 7. Persistence | PE-001 to PE-005 | | | |
| 8. Reports | RP-001 to RP-008 | | | |
| 9. Edge Cases | EC-001 to EC-006 | | | |
| 10. Cross-Feature Integration | CF-001 to CF-004 | | | |
| **TOTAL** | **62** | | | |

---

**Critical Issues**: ________________

**Blocking Issues** (must fix before release): ________________

**Non-blocking Issues** (can ship with known issues): ________________

**Tested by**: _________ **Date**: _________ **Platform**: _________
