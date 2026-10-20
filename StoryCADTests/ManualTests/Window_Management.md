# Window Management Test Plan
**Time**: ~5 minutes
**Purpose**: Verify window resize, minimize/maximize, full-screen, and responsive layout
**Priority**: Tier 3

## Platform Notes

| Feature | Windows | macOS |
|---------|---------|-------|
| Minimize | Minimize button / Win+Down | Yellow traffic light / Cmd+M |
| Maximize | Maximize button / Win+Up | Green traffic light (full-screen) |
| Full-screen | None (use Maximize) | Green traffic light (enters macOS full-screen space) |
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

2. Drag to make the window narrower, still wider than 800px
   **Expected:** Layout adapts; tab carousel arrows appear when the tabs do not fit; both panes stay side by side; no "..." (More) button yet

3. Drag to make the window as narrow as it goes (the minimum width is 520px)
   **Expected:** One pane at a time (stacked mode, see WM-006); UI remains usable; no overlapping controls or cut-off text

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
**Priority:** High (macOS) / Not applicable (Windows: StoryCAD has no full-screen command; F11 does nothing)
**Time:** ~1 minute

**Steps:**
1. Enter full-screen (macOS: green traffic light)
   **Expected:** App enters full-screen; menu bar auto-hides on macOS

2. Navigate the outline and edit an element
   **Expected:** All functionality works in full-screen

3. Exit full-screen (macOS: hover top to reveal traffic lights)
   **Expected:** Window returns to normal; no visual artifacts

**Pass/Fail:** ______

---

### WM-005: Command Bar Overflow at Narrow Width
**Priority:** Medium
**Time:** ~1 minute

**Steps:**
1. Resize the window narrower than 800px
   **Expected:** Overflow items move to a "..." (More) menu

2. Click the overflow menu
   **Expected:** The buttons that did not fit (Tools, Reports, Preferences, Report Feedback, Help) appear in the flyout; the bar shows each button's label

3. Click an overflow item (e.g., Tools > Key Questions)
   **Expected:** Command executes normally

4. Widen the window
   **Expected:** Items return from overflow to the main toolbar

**Pass/Fail:** ______

---

### WM-006: Hamburger Button and Stacked Mode at Narrow Width
**Priority:** High
**Time:** ~3 minutes

**Steps:**
1. Start with window wider than 800px
   **Expected:** Navigation Pane and Content Pane visible side-by-side

2. Click the hamburger button (three-line icon, left of command bar)
   **Expected:** Navigation Pane hides; Content Pane widens

3. Click the hamburger button again
   **Expected:** Navigation Pane reappears; side-by-side restored

4. Drag window narrower than 800px
   **Expected:** Layout switches to stacked mode; only Content Pane visible (Navigation Pane hidden, not squeezed side-by-side)

5. Click the hamburger button
   **Expected:** Navigation Pane appears full-width; Content Pane hidden

6. Click any node in the Navigation Pane
   **Expected:** The node opens in the Content Pane; the Navigation Pane stays in front until the next step

7. Click the hamburger button again
   **Expected:** Returns to Content-only view (not side-by-side)

8. While still narrow, open the Navigation Pane via the hamburger, then drag the window slightly narrower (still < 800px)
   **Expected:** Navigation Pane stays open across the resize

9. Drag the window back to wider than 800px
   **Expected:** Side-by-side restored; pane reopens

10. On a rotatable device (e.g. HP Spectre), rotate the display to portrait
    **Expected:** If window auto-resizes below 800px, stacked mode engages; if window retains landscape width, stacked mode does not engage (known follow-up to #1411)

**Pass/Fail:** ______

---

## WINDOW MANAGEMENT RESULT: _____ / 6 Passed

**Critical Issues**: ________________

**Tested by**: _________ **Date**: _________ **Platform**: _________
