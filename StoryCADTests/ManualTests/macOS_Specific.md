# macOS-Specific Test Plan
**Time**: ~15 minutes
**Purpose**: Verify macOS-specific behaviors, native integration, and platform conventions
**Priority**: Tier 3

## Prerequisites
- macOS 10.15 (Catalina) or later
- StoryCAD installed via .pkg installer or built from source with `dotnet run -f net10.0-desktop`
- Familiarity with macOS conventions (Cmd shortcuts, menu bar, traffic lights)

---

## Installation and Launch

### MAC-001: Application Install
**Priority:** Critical
**Time:** ~3 minutes

**Steps:**
1. Obtain the StoryCAD .pkg installer (or .app bundle)
   **Expected:** Installer file available

2. Double-click to install
   **Expected:** macOS installer opens; may prompt for Gatekeeper approval ("from an identified developer" or "open anyway" in System Settings > Privacy)

3. Complete installation
   **Expected:** StoryCAD appears in Applications folder (or chosen location)

4. Launch StoryCAD from Applications
   **Expected:** App launches without crash; main window appears

**Pass/Fail:** ______

---

### MAC-002: File System Permissions
**Priority:** High
**Time:** ~2 minutes

**Steps:**
1. Create a new story and attempt to save to Documents folder
   **Expected:** macOS may prompt for file access permission; after granting, file saves

2. Open a file from Documents
   **Expected:** File picker navigates to Documents; file opens

3. Try saving to Desktop
   **Expected:** macOS grants permission (may prompt); file saves

4. Try saving to a restricted location (e.g., /usr/local)
   **Expected:** Permission denied or macOS blocks the operation gracefully

**Pass/Fail:** ______

---

## macOS Native Integration

### MAC-003: Menu Bar
**Priority:** Critical
**Time:** ~2 minutes

**Steps:**
1. Verify the macOS menu bar shows "StoryCAD" (or app name) when the app is focused
   **Expected:** App name appears in the menu bar next to the Apple menu

2. Check for standard macOS menus (File, Edit, View, Window, Help)
   **Expected:** Standard menus present (some may be the in-app toolbar menus depending on UNO implementation)

3. Click StoryCAD menu > About StoryCAD (if present)
   **Expected:** About dialog or info shown

4. Click StoryCAD menu > Quit StoryCAD (or Cmd+Q)
   **Expected:** App closes (prompts to save if unsaved changes)

**Pass/Fail:** ______

---

### MAC-004: Cmd Keyboard Shortcuts
**Priority:** Critical
**Time:** ~2 minutes

**Steps:**
1. Press Cmd+N
   **Expected:** New story created (same as File > New)

2. Press Cmd+O
   **Expected:** Open dialog appears

3. Press Cmd+S
   **Expected:** Save executes

4. Press Cmd+Shift+S
   **Expected:** Save As dialog appears

5. Press Cmd+Z
   **Expected:** Undo last action

6. Press Cmd+F
   **Expected:** Search interface appears

7. Press Cmd+Q
   **Expected:** App closes (with save prompt if needed)

**Note:** If any shortcut uses Ctrl instead of Cmd, file it as a bug — macOS convention requires Cmd.

**Pass/Fail:** ______

---

### MAC-005: Native File Dialogs
**Priority:** High
**Time:** ~2 minutes

**Steps:**
1. Click File > Open Story
   **Expected:** macOS native file picker opens (not a custom dialog)

2. Verify .stbx filter is applied
   **Expected:** File picker filters to .stbx files (or shows an option to filter)

3. Navigate to different folders using the sidebar (Favorites, iCloud, etc.)
   **Expected:** Standard macOS navigation works

4. Click File > Save As
   **Expected:** macOS native save dialog opens

5. Verify you can create a new folder from the save dialog
   **Expected:** "New Folder" button works

**Pass/Fail:** ______

---

### MAC-006: Traffic Light Buttons
**Priority:** High
**Time:** ~1 minute

**Steps:**
1. Verify red/yellow/green traffic light buttons appear in the window title bar
   **Expected:** All three buttons present in the top-left corner

2. Click yellow (minimize)
   **Expected:** Window minimizes to Dock

3. Click Dock icon to restore
   **Expected:** Window restores

4. Click green (full-screen)
   **Expected:** App enters macOS full-screen Space

5. Exit full-screen (Esc or hover top and click green button)
   **Expected:** Window returns to normal mode

6. Click red (close)
   **Expected:** Window closes; if unsaved changes, prompts to save

**Pass/Fail:** ______

---

### MAC-007: Drag and Drop on macOS
**Priority:** Medium
**Time:** ~2 minutes

**Steps:**
1. Open an outline with multiple elements
   **Expected:** Tree view populated

2. Click and drag an element in the tree
   **Expected:** Drag cursor appears; drag behavior works

3. Drop on a valid target
   **Expected:** Element moves; same behavior as Windows

4. Try an invalid drop
   **Expected:** Drop rejected

**Note:** macOS drag-and-drop may have slightly different visual feedback than Windows. The key test is that it works functionally.

**Pass/Fail:** ______

---

### MAC-008: Context Menus on macOS
**Priority:** High
**Time:** ~1 minute

**Steps:**
1. Right-click (or Ctrl+click) on a tree node
   **Expected:** Context menu appears with Add, Delete, etc.

2. Right-click on different element types
   **Expected:** Context menu options are appropriate for each type

3. Select a context menu item
   **Expected:** Action executes correctly

**Pass/Fail:** ______

---

### MAC-009: Font Rendering and UI Layout
**Priority:** Medium
**Time:** ~1 minute

**Steps:**
1. Examine text throughout the application
   **Expected:** Text is crisp and readable (macOS uses its own font rendering)

2. Check form labels, tree view text, and rich text editors
   **Expected:** No truncated labels, no overlapping text, no missing characters

3. If on a Retina display, verify HiDPI rendering
   **Expected:** No blurry elements; icons and text are sharp

**Pass/Fail:** ______

---

### MAC-010: Menu Bar / Toolbar Item Walkthrough
**Priority:** High
**Time:** ~5 minutes

**Purpose:** Systematically verify every menu/toolbar item is clickable and triggers the correct action on macOS.

**Setup:** Open an outline with several elements (use "Danger Calls" sample). Be in Story Explorer view.

#### Toolbar Top-Level
| Item | Action | Expected | Pass/Fail |
|------|--------|----------|-----------|
| Show | Click toggle | Navigation pane shows/hides | ______ |
| Search box | Type a term, press Enter | Tree filters to matches | ______ |
| Collaborator | Click (if visible) | Collaborator dialog opens (requires API key) | ______ |
| Preferences | Click (or Cmd+,) | Preferences dialog opens | ______ |
| Report Feedback | Click | Feedback UI opens | ______ |
| Help | Click (or F1) | Help flyout appears | ______ |

#### File Menu
| Item | Action | Expected | Pass/Fail |
|------|--------|----------|-----------|
| Open/Create file | Click (or Cmd+O) | File open/create dialog appears | ______ |
| Save Story | Click (or Cmd+S) | File saves (or Save As if new) | ______ |
| Save Story As | Click (or Cmd+Shift+S) | Save As dialog appears | ______ |
| Create Backup | Click (or Cmd+B) | Backup created in backup directory | ______ |
| Close Story | Click | Story closes (prompts if unsaved) | ______ |
| Exit / Quit | Click (or Cmd+Q) | App exits (prompts if unsaved) | ______ |

#### Add Menu (Story Explorer view)
| Item | Action | Expected | Pass/Fail |
|------|--------|----------|-----------|
| Add folder | Click (or Opt+F) | New Folder node appears | ______ |
| Add problem | Click (or Opt+P) | New Problem node appears | ______ |
| Add character | Click (or Opt+C) | New Character node appears | ______ |
| Add setting | Click (or Opt+L) | New Setting node appears | ______ |
| Add scene | Click (or Opt+S) | New Scene node appears | ______ |
| Add Web node | Click (or Opt+W) | New Web node appears | ______ |
| Add Notes node | Click (or Opt+N) | New Notes node appears | ______ |
| Add StoryWorld | Click (or Opt+B) | New StoryWorld node appears | ______ |
| Delete story element | Select element, click Delete | Confirmation dialog, element deleted | ______ |
| Restore Story element | Select Trash, click Restore | Element restored from trash | ______ |
| Add To Narrative | Select scene, click | Scene added to Narrator view | ______ |
| Convert to Scene | Select Notes, click | Element converted to Scene type | ______ |
| Convert to Problem | Select Notes, click | Element converted to Problem type | ______ |

#### Add Menu (Story Narrator view)
| Item | Action | Expected | Pass/Fail |
|------|--------|----------|-----------|
| Add section | Switch to Narrator, click (or Opt+A) | New Section node appears | ______ |
| Remove from Narrative | Select scene, click | Scene removed from Narrator | ______ |

#### Move Menu
| Item | Action | Expected | Pass/Fail |
|------|--------|----------|-----------|
| Move Left | Select child node, click | Node moves up one level | ______ |
| Move Right | Select node, click | Node becomes child of sibling above | ______ |
| Move Up | Select node, click | Node moves up among siblings | ______ |
| Move Down | Select node, click | Node moves down among siblings | ______ |

#### Tools Menu
| Item | Action | Expected | Pass/Fail |
|------|--------|----------|-----------|
| Open Narrative Editor | Click (or Cmd+N) | Narrative Editor dialog opens | ______ |
| Key Questions | Click (or Cmd+K) | Key Questions dialog opens | ______ |
| Topic Information | Click | Topic Information dialog opens | ______ |
| Copy Elements to Another Outline | Click | Copy Elements dialog opens | ______ |
| Plotting Aids > Master Plots | Click (or Cmd+M) | Master Plots dialog opens | ______ |
| Plotting Aids > Dramatic Situations | Click (or Cmd+D) | Dramatic Situations dialog opens | ______ |
| Plotting Aids > Stock Scenes | Click (or Cmd+L) | Stock Scenes dialog opens | ______ |

#### Reports Menu
| Item | Action | Expected | Pass/Fail |
|------|--------|----------|-----------|
| Print Reports | Click (may be Windows-only) | Print dialog or hidden on macOS | ______ |
| PDF Reports | Click (or Cmd+P) | PDF export dialog opens | ______ |
| Scrivener Reports | Click (or Cmd+R) | Scrivener file picker opens | ______ |

#### Right-Click Context Menu
| Item | Action | Expected | Pass/Fail |
|------|--------|----------|-----------|
| Right-click tree node | Right-click (or Ctrl+click) | Context menu appears | ______ |
| Add Elements sub-menu | Hover/click Add Elements | Sub-flyout with all element types | ______ |
| Delete element | Click | Confirmation, then delete | ______ |
| Empty Trash | Right-click Trash node | Confirmation, then empties trash | ______ |

#### Status Bar
| Item | Action | Expected | Pass/Fail |
|------|--------|----------|-----------|
| View selector | Change selection | Switches Explorer/Narrator view | ______ |
| Edit pencil (unsaved) | Make a change, then click pencil | Saves the file | ______ |
| Backup icon | Click | Creates a backup | ______ |

**Total items:** 48

**Pass/Fail:** ______ / 48

---

## macOS SPECIFIC RESULT: _____ / 10 Passed

**Critical Issues**: ________________

**Tested by**: _________ **Date**: _________ **Platform**: macOS _______ (version)
