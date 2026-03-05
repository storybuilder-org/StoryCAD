# Window Management Test Plan
**Time**: ~5 minutes
**Purpose**: Verify window resize, minimize/maximize, full-screen, and responsive layout
**Priority**: Tier 3

## Platform Notes

| Feature | Windows | macOS |
|---------|---------|-------|
| Minimize | Minimize button / Win+Down | Yellow traffic light / Cmd+M |
| Maximize | Maximize button / Win+Up | Green traffic light (full-screen) |
| Full-screen | F11 or maximize | Green traffic light (enters macOS full-screen space) |
| Snap layouts | Win+Arrow | Not available natively |
| Window chrome | WinAppSDK title bar | macOS traffic lights |

**macOS-specific concerns**:
- macOS full-screen is a separate Space — verify the app works correctly in this mode.
- Traffic light buttons (close/minimize/full-screen) should be present and functional.
- Window resizing behavior may differ at small sizes.

## Setup
1. Launch StoryCAD
2. Open an outline with several elements (for content to observe during resizing)

---

### WM-001: Window Resize
**Priority:** High
**Time:** ~1 minute

**Steps:**
1. Drag window edge to make the window wider
   **Expected:** Content pane expands; navigation pane remains usable

2. Drag to make the window narrower
   **Expected:** Layout adapts; tab carousel arrows may appear; command bar items may overflow to "More" menu

3. Drag to make the window very narrow (~400px)
   **Expected:** UI remains usable; no overlapping controls or cut-off text

4. Drag vertically to make window shorter
   **Expected:** Content scrolls or adapts; no controls disappear permanently

**Pass/Fail:** ______

---

### WM-002: Minimize and Restore
**Priority:** High
**Time:** ~1 minute

**Steps:**
1. Click the minimize button
   **Expected:** Window minimizes to taskbar (Windows) or dock (macOS)

2. Click the taskbar/dock icon to restore
   **Expected:** Window restores to previous size and position; content intact; no errors

**Pass/Fail:** ______

---

### WM-003: Maximize and Restore
**Priority:** High
**Time:** ~1 minute

**Steps:**
1. Click the maximize button
   **Expected:** Window fills the screen; layout adjusts to full width

2. Click maximize again (or restore button)
   **Expected:** Window returns to previous size and position

**Pass/Fail:** ______

---

### WM-004: Full-Screen Mode (macOS)
**Priority:** High (macOS) / Low (Windows)
**Time:** ~1 minute

**Steps:**
1. Enter full-screen (macOS: green traffic light; Windows: F11 if supported)
   **Expected:** App enters full-screen; menu bar auto-hides on macOS

2. Navigate the outline and edit an element
   **Expected:** All functionality works in full-screen

3. Exit full-screen (macOS: hover top to reveal traffic lights; Windows: F11 or Esc)
   **Expected:** Window returns to normal; no visual artifacts

**Pass/Fail:** ______

---

### WM-005: Command Bar Overflow at Narrow Width
**Priority:** Medium
**Time:** ~1 minute

**Steps:**
1. Resize window to narrow width so toolbar items cannot all fit
   **Expected:** Overflow items move to a "..." (More) menu

2. Click the overflow menu
   **Expected:** Hidden toolbar items appear in the flyout

3. Click an overflow item (e.g., a tool command)
   **Expected:** Command executes normally

4. Widen the window
   **Expected:** Items return from overflow to the main toolbar

**Pass/Fail:** ______

---

## WINDOW MANAGEMENT RESULT: _____ / 5 Passed

**Critical Issues**: ________________

**Tested by**: _________ **Date**: _________ **Platform**: _________
