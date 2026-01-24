# Copy Elements Screenshot Script

**Purpose:** Manual script for capturing screenshots of the Copy Elements dialog for documentation.
**Created:** 2026-01-24

---

## Instructions

1. Open StoryCAD with a story that has multiple elements (characters, settings, problems, etc.)
2. Have a second .stbx file ready as the target (can be a new empty story)
3. Follow each section below to capture the required screenshots
4. Save screenshots to `/docs/media/`

---

## Prerequisites

### Source Story (Current Outline)
Open a story with at least:
- 2-3 Characters
- 1-2 Settings
- 1-2 Problems
- 1 StoryWorld (optional, for demonstrating the singleton constraint)

The "Danger Calls" or "Land of Oz" test stories work well.

### Target Story
Create or have ready a second .stbx file to use as the copy target.

---

## Screenshot 1: Copy-Elements-Dialog.png

**Purpose:** Show the main dialog layout with source elements visible.

### Steps:
1. Open source story in StoryCAD
2. Select **Tools > Copy Elements** from menu
3. Click **Browse** and select target file
4. Set **Filter** to "Character"
5. Ensure source list (left) shows several characters
6. Target list (right) should be empty

### Capture:
- Full dialog visible
- Filter set to "Character"
- Source list populated
- Target list empty
- Status message shows loaded target file name

### Save as: `Copy-Elements-Dialog.png`

---

## Screenshot 2: Copy-Elements-With-Copied.png

**Purpose:** Show the dialog after copying elements of multiple types.

### Steps:
1. Continue from Screenshot 1
2. Select a character in source list, click → to copy
3. Change Filter to "Setting"
4. Select a setting in source list, click → to copy
5. Change Filter to "Problem"
6. Select a problem in source list, click → to copy
7. Change Filter back to "Character" (to show target list persists)

### Capture:
- Full dialog visible
- Filter set to "Character" (showing we changed filters)
- Source list shows characters
- Target list shows ALL copied elements (character, setting, problem) regardless of current filter
- CopiedCount should show 3

### Save as: `Copy-Elements-With-Copied.png`

---

## Screenshot Capture Checklist

After capturing, save to `/docs/media/`:

| Screenshot | Filename | Purpose |
|------------|----------|---------|
| Main dialog | `Copy-Elements-Dialog.png` | Initial dialog state |
| After copying | `Copy-Elements-With-Copied.png` | Shows accumulated copies |

**Capture checklist:**
- [ ] `Copy-Elements-Dialog.png`
- [ ] `Copy-Elements-With-Copied.png`

---

## Notes

- Capture at a reasonable window size so text is readable
- Ensure the status message area is visible
- The key feature to demonstrate is that the target list accumulates ALL copied elements regardless of filter
