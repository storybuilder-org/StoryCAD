# Issue #1339: Beat Sheet Usability — Status Log

## Known Defects

### 1. Button icons clipped horizontally and vertically
- **Severity**: Minor/cosmetic
- **Location**: Structure tab button strip (Column 1)
- **Symptom**: Trash can, save, and right-arrow icons are clipped on the right edge. Save button is cut off at the bottom of the button column.
- **Cause**: Button column too narrow for icon content; vertical space insufficient for all 7 buttons.
- **Screenshot**: `/mnt/c/temp/element_description_works.png`

### 2. "This will clear selected story beats" dialog fires on Problem tab navigation
- **Severity**: Medium — confusing UX, could cause accidental data loss
- **Location**: `UpdateSelectedBeat` ComboBox SelectionChanged handler
- **Symptom**: Dialog appears when navigating to/selecting a problem, even though the user is on the Problem tab, not the Structure tab.
- **Cause**: ComboBox SelectionChanged fires during navigation/data binding, not just from user interaction.
- **Screenshot**: `/mnt/c/temp/this_will_clear_on_select_problem.png`

### 3. Missing beat assignment validation — circular and Story Problem assignment
- **Severity**: Medium — allows invalid data structures
- **Location**: `AssignBeatAsync` in BeatSheetsViewModel, `AssignElementToBeat` in OutlineService
- **Symptom**: Two missing validations:
  1. **Story Problem** (the root problem identified on the Overview Premise tab) can be assigned as a beat on another problem. It should be rejected — the root of the problem tree can never be a beat.
  2. **Mutual assignment** creates a loop: if Main Problem's beat sheet has Sub-Problem A assigned to a beat, then Sub-Problem A's beat sheet can also assign Main Problem to a beat. This creates a circular reference (Ab<B and Bb<A). Should be rejected.
- **Reproduction**: Open an outline. Main Problem has a beat sheet with Sub-Problem A assigned to B-Story. Navigate to Sub-Problem A, give it a custom beat sheet, add a beat, assign Main Problem to it — it succeeds when it should be blocked.
- **Screenshot**: `/mnt/c/temp/main_problem_makes_loop.png`

### 4. Reassigning a problem doesn't clear the old beat assignment
- **Severity**: High — violates the one-assignment rule
- **Location**: `AssignElementToBeat` in OutlineService / `AssignBeatAsync` in BeatSheetsViewModel
- **Symptom**: A problem can only be assigned to one beat, period. When reassigning a problem that is already assigned, the old beat should be cleared. Instead, the problem ends up assigned to both beats.
- **Reproduction**: On Main Problem's Save The Cat beat sheet, assign Sub-Problem A to B Story. Then assign Sub-Problem A to Midpoint. Both B Story and Midpoint now show Sub-Problem A.
- **Rule**: A problem can only be assigned once — to one beat on one problem's beat sheet. Any new assignment must clear the previous one.
- **Screenshot**: `/mnt/c/temp/reassign_problem.png`

### 5. No way to edit beat title inline
- **Severity**: Low — enhancement
- **Location**: Beat ListView item template in `ProblemPage.xaml`
- **Symptom**: Beat titles are displayed in a read-only TextBlock. There is no inline editing mechanism (e.g., double-click to edit, or a TextBox swap) to rename a beat.
- **Note**: Beat descriptions are editable via the Beat Description panel below. Only the title lacks an edit path.

## Completed Work

- Phase 0: Characterization tests
- Phase 1: BeatEditorViewModel extraction, StructureBeat rename
- Phase 2: All beat sheets editable, delete confirmation
- Phase 3: UI improvements (buttons, icons, layout)
- Phase 4: Reports (detail, unassigned, plot diagram)
- Phase 5: User manual rewrite
- Button review: All 7 button methods reviewed
- AssignBeat bug fix: delegation to OutlineService
- CreateBeat guard: status message when no beat sheet selected
- Element Description: TextBox → RichEditBoxExtended for RTF rendering
- Rebased onto issue-1358 SceneModel.Description fix
