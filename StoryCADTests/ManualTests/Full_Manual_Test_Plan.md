# StoryCAD Manual Test Plan

## Test Case Template
Use this template when adding new test cases:

```markdown
### TC-XXX: [Test Name]
**Priority:** [Critical/High/Medium/Low]  
**Time:** [~X minutes]

**Setup:**
- [Prerequisites]

**Steps:**
1. [Action]  
   **Expected:** [Result]

**Cleanup:**
- [Any cleanup needed]
```

---

## Session Setup (Run Once Before Testing)
1. Launch StoryCAD
2. Go to Tools > Preferences > General tab
3. Disable "Auto-save every X seconds" 
4. Go to Backup tab
5. Disable "Automatic backups"
6. Click OK to save preferences
7. Create a TestSession folder in TestInputs for this session's test files
8. Note: Sample outlines available in File > Open Sample Outline menu

## Session Cleanup (After All Tests)
1. Delete all test files created in TestInputs/TestSession folder
2. Re-enable auto-save and backups if desired
3. Close StoryCAD

---

## File Operations

### TC-001: Create New Story
**Priority:** Critical  
**Time:** ~2 minutes

**Setup:**
- StoryCAD is running

**Steps:**
1. Click File > New Story  
   **Expected:** New outline opens with "Untitled" Story Overview node
   
2. Enter "Test Story" in the Story Name field  
   **Expected:** Tree view updates to show "Test Story" as root

3. Click File > Save  
   **Expected:** Save dialog appears

4. Navigate to TestInputs/TestSession and save as "TC001_NewStory.stbx"  
   **Expected:** File saves, title bar shows filename

**Cleanup:**
- Keep file for next test

---

### TC-002: Open Existing Story
**Priority:** Critical  
**Time:** ~2 minutes

**Setup:**
- StoryCAD is running
- TC001_NewStory.stbx exists from previous test

**Steps:**
1. Click File > Open Story  
   **Expected:** Open dialog appears
   
2. Navigate to TestInputs/TestSession and select TC001_NewStory.stbx  
   **Expected:** File opens showing "Test Story" outline

3. Verify Story Overview shows correct story name  
   **Expected:** "Test Story" appears as root node

**Cleanup:**
- None

---

### TC-003: Save As
**Priority:** High  
**Time:** ~2 minutes

**Setup:**
- TC001_NewStory.stbx is open

**Steps:**
1. Click File > Save As  
   **Expected:** Save dialog appears
   
2. Enter "TC003_SaveAs.stbx" as filename  
   **Expected:** File saves with new name

3. Verify title bar shows new filename  
   **Expected:** Title bar displays "TC003_SaveAs.stbx"

**Cleanup:**
- Delete TC003_SaveAs.stbx

---

### TC-004: Open Sample Outline
**Priority:** Medium  
**Time:** ~2 minutes

**Setup:**
- StoryCAD is running

**Steps:**
1. Click File > Open Sample Outline  
   **Expected:** Submenu shows available samples (e.g., "Danger Calls", "Hamlet")
   
2. Select "Danger Calls"  
   **Expected:** Sample outline opens in read-only mode

3. Make a change and click File > Save As  
   **Expected:** Save dialog appears (cannot overwrite sample)

4. Save to TestInputs/TestSession as "TC004_Sample.stbx"  
   **Expected:** File saves successfully

**Cleanup:**
- Delete TC004_Sample.stbx

---

### TC-005: Recent Files
**Priority:** Low  
**Time:** ~2 minutes

**Setup:**
- Multiple test files have been opened previously

**Steps:**
1. Click File menu  
   **Expected:** Recent files list appears at bottom
   
2. Click on a recent file  
   **Expected:** File opens successfully

**Cleanup:**
- None

---

## Navigation

### TC-010: Navigate Tree View
**Priority:** High  
**Time:** ~3 minutes

**Setup:**
- Open sample outline "Danger Calls"

**Steps:**
1. Click on different nodes in Navigation Pane  
   **Expected:** Content Pane updates to show selected element

2. Use arrow keys to navigate up/down  
   **Expected:** Selection moves through tree

3. Press Enter on a node  
   **Expected:** Node expands/collapses

4. Double-click a parent node  
   **Expected:** Node expands/collapses showing/hiding children

**Cleanup:**
- Close without saving

---

### TC-011: Switch Between Tabs
**Priority:** High  
**Time:** ~2 minutes

**Setup:**
- Open any story with a Character element

**Steps:**
1. Select a Character in Navigation Pane  
   **Expected:** Character form appears with multiple tabs

2. Click different tabs (Role, Physical, Social, etc.)  
   **Expected:** Tab content changes appropriately

3. On narrow window, verify tab carousel arrows appear  
   **Expected:** Left/right arrows allow tab scrolling

**Cleanup:**
- None

---

### TC-012: Toggle Navigation Pane
**Priority:** Medium  
**Time:** ~1 minute

**Setup:**
- StoryCAD is running with outline open

**Steps:**
1. Click View > Show/Hide Navigation Pane  
   **Expected:** Navigation Pane toggles visibility

2. Press keyboard shortcut (if available)  
   **Expected:** Navigation Pane toggles back

**Cleanup:**
- None

---

### TC-013: Switch Views (Explorer/Narrator)
**Priority:** High  
**Time:** ~2 minutes

**Setup:**
- Open outline with scenes

**Steps:**
1. Click View Selector in Status Bar  
   **Expected:** Shows "Story Explorer" or "Story Narrator"

2. Click to switch to Story Narrator  
   **Expected:** View changes to show narrative sequence

3. Click to switch back to Story Explorer  
   **Expected:** View returns to hierarchical outline

**Cleanup:**
- None

---

## Story Elements

### TC-020: Create Character
**Priority:** Critical  
**Time:** ~3 minutes

**Setup:**
- New or existing outline open

**Steps:**
1. Right-click Story Overview  
   **Expected:** Context menu appears

2. Select Add > Character  
   **Expected:** New Character node appears

3. Enter "Jane Doe" in Name field  
   **Expected:** Tree updates to show "Jane Doe"

4. Fill in Role tab fields  
   **Expected:** Data saves when switching tabs

**Cleanup:**
- Save for relationship test

---

### TC-021: Create Scene
**Priority:** Critical  
**Time:** ~3 minutes

**Setup:**
- Outline with at least one Problem element

**Steps:**
1. Right-click on Problem node  
   **Expected:** Context menu appears

2. Select Add > Scene  
   **Expected:** New Scene appears as child

3. Enter scene name and sketch  
   **Expected:** Tree updates with scene name

4. Select Viewpoint Character from dropdown  
   **Expected:** Character list shows all characters

**Cleanup:**
- None

---

### TC-022: Create Problem
**Priority:** Critical  
**Time:** ~3 minutes

**Setup:**
- Outline open

**Steps:**
1. Click Menu Bar Add button  
   **Expected:** Add menu appears

2. Select Problem  
   **Expected:** New Problem node appears

3. Select Problem Type (Conflict/Decision/Discovery)  
   **Expected:** Form updates based on type

4. Fill in Story Question  
   **Expected:** Text saves correctly

**Cleanup:**
- None

---

### TC-023: Create Setting
**Priority:** High  
**Time:** ~2 minutes

**Setup:**
- Outline open

**Steps:**
1. Right-click any node and select Add > Setting  
   **Expected:** New Setting node appears

2. Enter Locale and Time Period  
   **Expected:** Fields accept text

3. Switch to Sensations tab  
   **Expected:** Five senses fields available

**Cleanup:**
- None

---

### TC-024: Create Folder
**Priority:** Medium  
**Time:** ~2 minutes

**Setup:**
- Outline with multiple elements

**Steps:**
1. Right-click and select Add > Folder  
   **Expected:** New Folder appears

2. Name it "Act 1"  
   **Expected:** Tree shows folder name

3. Drag scenes into folder  
   **Expected:** Scenes become children of folder

**Cleanup:**
- None

---

### TC-025: Delete Story Element
**Priority:** High  
**Time:** ~2 minutes

**Setup:**
- Outline with expendable element

**Steps:**
1. Select element to delete  
   **Expected:** Element highlighted

2. Right-click and select Delete  
   **Expected:** Confirmation dialog appears

3. Confirm deletion  
   **Expected:** Element removed from tree

4. Right-click and select Restore  
   **Expected:** Element restored (if available)

**Cleanup:**
- None

---

## Tools Menu

### TC-030: Open Preferences
**Priority:** High  
**Time:** ~3 minutes

**Setup:**
- StoryCAD running

**Steps:**
1. Click Tools > Preferences  
   **Expected:** Preferences dialog opens

2. Navigate through tabs (General, Directories, Backup, etc.)  
   **Expected:** Each tab displays correctly

3. Change a setting and click OK  
   **Expected:** Setting saved and applied

4. Reopen to verify saved  
   **Expected:** Changed setting persists

**Cleanup:**
- Restore original settings

---

### TC-031: Search Function
**Priority:** High  
**Time:** ~3 minutes

**Setup:**
- Outline with multiple elements containing text

**Steps:**
1. Click Tools > Search or press Ctrl+F  
   **Expected:** Search box appears

2. Enter search term  
   **Expected:** Results filter in real-time

3. Clear search  
   **Expected:** All elements reappear

**Cleanup:**
- None

---

### TC-032: Master Plots Tool
**Priority:** Medium  
**Time:** ~3 minutes

**Setup:**
- Outline open

**Steps:**
1. Click Tools > Master Plots  
   **Expected:** Master Plots dialog opens

2. Select a plot template  
   **Expected:** Description appears

3. Click Copy to Outline  
   **Expected:** Template elements added to outline

**Cleanup:**
- None

---

### TC-033: Dramatic Situations Tool
**Priority:** Medium  
**Time:** ~2 minutes

**Setup:**
- Scene selected

**Steps:**
1. Click Tools > Dramatic Situations  
   **Expected:** Dialog with 36 situations opens

2. Select a situation  
   **Expected:** Description and examples shown

3. Click Apply/Copy  
   **Expected:** Situation added to scene notes

**Cleanup:**
- None

---

### TC-034: Stock Scenes Tool
**Priority:** Medium  
**Time:** ~2 minutes

**Setup:**
- Outline open

**Steps:**
1. Click Tools > Stock Scenes  
   **Expected:** Stock Scenes dialog opens

2. Browse categories  
   **Expected:** Scene types listed

3. Select and copy scene  
   **Expected:** Scene added to outline

**Cleanup:**
- None

---

### TC-035: Key Questions Tool
**Priority:** Medium  
**Time:** ~3 minutes

**Setup:**
- Any story element selected

**Steps:**
1. Click Tools > Key Questions  
   **Expected:** Questions relevant to element type appear

2. Review questions for current element  
   **Expected:** Questions help identify gaps

3. Close dialog  
   **Expected:** Returns to main window

**Cleanup:**
- None

---

### TC-036: Conflict Builder Tool
**Priority:** Low  
**Time:** ~2 minutes

**Setup:**
- Problem or Scene element selected

**Steps:**
1. Open Conflict Builder from Tools  
   **Expected:** Conflict categories appear

2. Select conflict type  
   **Expected:** Examples and details shown

3. Apply to current element  
   **Expected:** Conflict added to element

**Cleanup:**
- None

---

### TC-037: Trait Builder Tools
**Priority:** Low  
**Time:** ~3 minutes

**Setup:**
- Character element selected

**Steps:**
1. Click Tools > Inner Traits  
   **Expected:** Psychological traits dialog opens

2. Select traits  
   **Expected:** Traits added to character

3. Click Tools > Outer Traits  
   **Expected:** Physical/behavioral traits dialog opens

4. Select traits  
   **Expected:** Traits added to character

**Cleanup:**
- None

---

## Edit Operations

### TC-040: Copy and Paste Elements
**Priority:** High  
**Time:** ~3 minutes

**Setup:**
- Outline with scenes

**Steps:**
1. Select a scene and press Ctrl+C  
   **Expected:** Scene copied to clipboard

2. Select different location and press Ctrl+V  
   **Expected:** Scene pasted as child

3. Verify pasted scene is independent copy  
   **Expected:** Changes to copy don't affect original

**Cleanup:**
- Delete copied scene

---

### TC-041: Undo and Redo
**Priority:** Critical  
**Time:** ~2 minutes

**Setup:**
- Outline open

**Steps:**
1. Make a change (add element or edit text)  
   **Expected:** Change applied

2. Press Ctrl+Z  
   **Expected:** Change undone

3. Press Ctrl+Y  
   **Expected:** Change redone

**Cleanup:**
- None

---

### TC-042: Cut and Paste
**Priority:** High  
**Time:** ~2 minutes

**Setup:**
- Outline with moveable element

**Steps:**
1. Select element and press Ctrl+X  
   **Expected:** Element removed from tree

2. Select new parent and press Ctrl+V  
   **Expected:** Element appears in new location

**Cleanup:**
- None

---

## Drag and Drop

### TC-050: Reorder Scenes
**Priority:** High  
**Time:** ~3 minutes

**Setup:**
- Outline with multiple scenes

**Steps:**
1. Click and hold on a scene  
   **Expected:** Drag cursor appears

2. Drag scene to new position between siblings  
   **Expected:** Insertion line appears

3. Release mouse  
   **Expected:** Scene moves to new position

**Cleanup:**
- None

---

### TC-051: Move Elements Between Parents
**Priority:** High  
**Time:** ~3 minutes

**Setup:**
- Outline with folders or sections

**Steps:**
1. Drag scene from one parent  
   **Expected:** Drag initiated

2. Hover over different parent  
   **Expected:** Parent highlights

3. Drop on new parent  
   **Expected:** Element becomes child of new parent

**Cleanup:**
- None

---

### TC-052: Invalid Drop Locations
**Priority:** Medium  
**Time:** ~2 minutes

**Setup:**
- Outline open

**Steps:**
1. Try to drag Story Overview  
   **Expected:** Drag not allowed or cancelled

2. Try to drop character onto scene  
   **Expected:** Drop rejected (invalid parent)

**Cleanup:**
- None

---

## Reports

### TC-060: Generate Print Report
**Priority:** Medium  
**Time:** ~3 minutes

**Setup:**
- Complete outline open

**Steps:**
1. Click Reports > Print Reports  
   **Expected:** Print dialog opens

2. Select report options  
   **Expected:** Preview updates

3. Click Print  
   **Expected:** Report sends to printer/PDF

**Cleanup:**
- Cancel print if testing

---

### TC-061: Generate Scrivener Report
**Priority:** Low  
**Time:** ~2 minutes

**Setup:**
- Outline open

**Steps:**
1. Click Reports > Scrivener Reports  
   **Expected:** File picker opens

2. Select location and generate  
   **Expected:** Scrivener-compatible file created

**Cleanup:**
- Delete generated file

---

## Keyboard Shortcuts

### TC-070: File Operation Shortcuts
**Priority:** Medium  
**Time:** ~3 minutes

**Setup:**
- StoryCAD running

**Steps:**
1. Press Ctrl+N  
   **Expected:** New story created

2. Press Ctrl+O  
   **Expected:** Open dialog appears

3. Press Ctrl+S  
   **Expected:** Save executes

4. Press Ctrl+Shift+S  
   **Expected:** Save As dialog appears

**Cleanup:**
- None

---

### TC-071: Edit Shortcuts
**Priority:** Medium  
**Time:** ~2 minutes

**Setup:**
- Text field selected

**Steps:**
1. Type text and press Ctrl+A  
   **Expected:** All text selected

2. Press Ctrl+C  
   **Expected:** Text copied

3. Press Ctrl+V  
   **Expected:** Text pasted

**Cleanup:**
- None

---

### TC-072: Navigation Shortcuts
**Priority:** Low  
**Time:** ~2 minutes

**Setup:**
- Outline open

**Steps:**
1. Press F6  
   **Expected:** Focus moves between panes

2. Press Tab  
   **Expected:** Focus moves to next control

3. Press Shift+Tab  
   **Expected:** Focus moves to previous control

**Cleanup:**
- None

---

## Dialogs and Special Features

### TC-080: Character Relationships
**Priority:** Medium  
**Time:** ~3 minutes

**Setup:**
- Outline with multiple characters

**Steps:**
1. Select character and go to Relationships tab  
   **Expected:** Relationships section appears

2. Click Add Relationship  
   **Expected:** Dialog shows other characters

3. Select character and define relationship  
   **Expected:** Relationship added to list

**Cleanup:**
- None

---

### TC-081: Narrative Editor
**Priority:** Low  
**Time:** ~4 minutes

**Setup:**
- Outline with scenes in Explorer view

**Steps:**
1. Open Tools > Narrative Editor  
   **Expected:** Editor dialog opens

2. Move scenes to narrative order  
   **Expected:** Scenes reorder in Narrator view

3. Create sections  
   **Expected:** Sections organize narrative

**Cleanup:**
- None

---

### TC-082: Auto-complete Lists
**Priority:** Low  
**Time:** ~2 minutes

**Setup:**
- Character form open

**Steps:**
1. Start typing in a field with auto-complete  
   **Expected:** Suggestions appear

2. Select from suggestions  
   **Expected:** Field populated

**Cleanup:**
- None

---

### TC-083: Changed Indicator
**Priority:** High  
**Time:** ~2 minutes

**Setup:**
- Saved outline open

**Steps:**
1. Make any change  
   **Expected:** Status bar shows "Changed"

2. Save file  
   **Expected:** "Changed" indicator clears

**Cleanup:**
- None

---

## Error Handling

### TC-090: Unsaved Changes Warning
**Priority:** Critical  
**Time:** ~2 minutes

**Setup:**
- Outline with unsaved changes

**Steps:**
1. Attempt to close StoryCAD  
   **Expected:** Warning dialog appears

2. Choose Save  
   **Expected:** File saves and closes

3. Repeat and choose Don't Save  
   **Expected:** Closes without saving

4. Repeat and choose Cancel  
   **Expected:** Returns to application

**Cleanup:**
- None

---

### TC-091: Invalid File Format
**Priority:** Medium  
**Time:** ~2 minutes

**Setup:**
- Non-STBX file available

**Steps:**
1. Try to open non-STBX file  
   **Expected:** Error message appears

2. Dismiss error  
   **Expected:** Returns to application

**Cleanup:**
- None

---

### TC-092: Read-Only File Handling
**Priority:** Low  
**Time:** ~2 minutes

**Setup:**
- Sample outline open

**Steps:**
1. Try to save sample outline  
   **Expected:** Save As dialog appears (cannot overwrite)

2. Cancel save  
   **Expected:** Outline remains open read-only

**Cleanup:**
- None

---

## Performance Tests

### TC-100: Large Outline Performance
**Priority:** Medium  
**Time:** ~5 minutes

**Setup:**
- Large sample outline (100+ elements)

**Steps:**
1. Open large outline  
   **Expected:** Opens within 5 seconds

2. Navigate between elements  
   **Expected:** No noticeable lag

3. Search for text  
   **Expected:** Results appear quickly

4. Save file  
   **Expected:** Saves within 3 seconds

**Cleanup:**
- None

---

## Notes

- **Critical Priority**: Core functionality that must work
- **High Priority**: Important features used frequently  
- **Medium Priority**: Standard features
- **Low Priority**: Advanced or rarely-used features

- Tests can be run selectively based on:
  - Available time (run Critical only for quick check)
  - Feature area being tested
  - After specific code changes

- Consider creating test data files for consistent testing:
  - Small outline (5-10 elements)
  - Medium outline (30-50 elements)  
  - Large outline (100+ elements)
  - Outline with all element types