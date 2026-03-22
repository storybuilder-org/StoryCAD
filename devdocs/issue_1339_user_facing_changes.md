# Issue #1339: Beat Sheet Usability — User-Facing Changes

This document captures what changed from the writer's perspective, for use by documentation and blog post authors.

## What is the Structure Tab?

The Structure tab on a Problem element lets writers organize their story using "beat sheets" — structured templates (like Save the Cat, Hero's Journey, etc.) that break a story into key moments called "beats." Writers assign scenes and sub-problems to each beat, building a structural outline of their story.

## Changes by Phase

### Phase 1: Internal Refactoring (no visible changes)

No user-facing changes. Beat editing logic was reorganized internally for maintainability.

### Phase 2: All Beat Sheets Are Now Editable

**Before:** Beat sheets came in two flavors:
- **Template sheets** (Save the Cat, Hero's Journey, etc.) were read-only. You could assign scenes/problems to beats but couldn't add, delete, rename, or reorder beats. The edit buttons (add, delete, move up/down, save) were hidden.
- **Custom Beat Sheet** was fully editable — you could do everything.

**After:** All beat sheets are editable starting points. When you load "Save the Cat," you get its 15 standard beats, but you can:
- **Add** new beats to fill gaps in the template
- **Delete** beats that don't apply to your story
- **Rename** beats by editing their title and description
- **Reorder** beats to match your story's flow
- **Save** your modified beat sheet to a file for reuse

The original template is never modified — reloading "Save the Cat" from the dropdown always restores the original 15 beats.

**Delete confirmation:** Deleting a beat now shows a confirmation dialog ("Delete beat 'All is Lost'? This will also remove its scene/problem assignment.") to prevent accidental loss. This is important because reloading a template clears all your existing beat assignments.

### Phase 3: UI Improvements

**Buttons:** The seven command buttons between the beats list and elements list have been redesigned:
- **Size:** 32x32 → 48x48 (meets minimum touch target guidelines)
- **Labels:** Cryptic single characters (`<`, `>`, `+`, `-`, `∧`, `∨`, `S`) replaced with standard icons (chevrons for assign/unassign, plus/trash for add/delete, up/down arrows for reorder, floppy disk for save)
- **Tooltips:** Every button now shows a tooltip on hover explaining what it does (e.g., "Assign element to beat", "Delete selected beat")

**Beat status indicators:** Unassigned beats previously showed an X icon (`Symbol.Cancel`) which looked like a delete button. Now they show a neutral bullet icon. Assigned beats still show their element type icon (globe for scenes, question mark for problems).

**"Unassigned" label:** Beats without an assigned element previously showed "No element Selected" — now they show "Unassigned" in secondary text color.

**Layout proportions:** The lists area and descriptions area now use a 3:2 ratio (was 2:1), giving the description panels more vertical space for reading beat and element descriptions. The beats list column is slightly wider than the elements list (5:4 ratio) since beat items show two lines (title + assignment).

**Card-style lists:** Both the beats list and elements list now have a subtle card border with rounded corners, providing clearer visual boundaries without the heavy vertical separator lines that were removed.

**Separator cleanup:** The vertical line separators between the lists and buttons, and between the two description panels, have been removed. The card borders and column spacing provide sufficient visual separation.
