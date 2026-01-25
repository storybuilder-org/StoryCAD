# Issue #25 - Responsive Layout for Small Screens Log

**Issue:** [StoryCAD #25 - Alternate use of Hamburger button for small screens and tablets](https://github.com/storybuilder-org/StoryCAD/issues/25)
**Log Started:** 2026-01-25
**Status:** Research
**Branch:** None yet

---

## Context

Improve responsive design for small screens, tablets, and portrait orientation. The shell currently uses a side-by-side Navigation Pane (TreeView) and Content Pane layout that works well in landscape but not in portrait orientation.

## Proposed Behavior

| Orientation | Layout | Hamburger Behavior |
|-------------|--------|-------------------|
| **Landscape** | Side-by-side (current) | Toggle nav pane on/off |
| **Portrait** | Stacked vertically | Switch between nav and content views |

## Technical Considerations

- Window size/orientation detection
- SplitView display modes (Inline vs Overlay)
- Visual State Manager for layout triggers
- Cross-platform support (Windows App SDK vs UNO Platform)

---

## Log Entries

### 2026-01-25 - Session 1: Initial Research

**Participants:** User (Terry), Claude Code

**Context:** During issue cleanup session, updated #25 with PIE format and began technical research.

#### Actions Taken

1. Updated issue #25 with PIE checklist format
2. Added "UNO Platform" label
3. Reviewed current Shell.xaml implementation
4. Researched Windows App SDK window management APIs
5. Created this devdocs folder with log and research document

#### Current Implementation Review

**Shell.xaml key elements:**
- `SplitView` with `DisplayMode="Inline"` and `IsPaneOpen` binding
- `ShellPage_SizeChanged` event adjusts pane width (30% of window, min 200px)
- Hamburger button calls `TogglePaneCommand` to toggle `IsPaneOpen`

**ShellViewModel:**
- `IsPaneOpen` property (default true) bound two-way to SplitView
- `TogglePane()` method simply inverts the boolean

#### Research Summary

**Windows App SDK APIs (from Microsoft docs):**
- `AppWindow.Changed` event - detect size changes
- `DisplayArea.WorkArea` - get available screen space
- `OverlappedPresenter` constraints - set min/max sizes

**Open question:** UNO Platform compatibility for these APIs on macOS/mobile.

#### Next Steps

- Research UNO Platform window management and responsive patterns
- Investigate Visual State Manager for orientation-based layout changes
- Prototype orientation detection logic

---

### 2026-01-25 - Session 1 (continued): UNO Platform Research

**Participants:** User (Terry), Claude Code

**Context:** Continued research on responsive layout, focusing on UNO Platform cross-platform support.

#### Research Completed

1. **AdaptiveTrigger in UNO Platform**
   - Fully supported cross-platform (Windows, macOS, iOS, Android, WebAssembly)
   - Key requirement: Order triggers largest→smallest `MinWindowWidth`
   - Source: [UNO Platform AdaptiveTrigger docs](https://github.com/unoplatform/uno/blob/master/doc/articles/features/AdaptiveTrigger.md)

2. **Mobile Orientation Detection**
   - `SimpleOrientationSensor` for iOS/Android device rotation
   - Event handler runs on background thread - must dispatch to UI
   - Source: [UNO Platform Orientation Sensor](https://github.com/unoplatform/uno/blob/master/doc/articles/features/orientation-sensor.md)

3. **iOS Orientation Control**
   - Requires `UIRequiresFullScreen=true` in info.plist
   - Opts out of iPad Multitasking/Split View
   - Source: [UNO Platform Controlling Orientation](https://github.com/unoplatform/uno/blob/master/doc/articles/features/orientation.md)

4. **AppWindow Support**
   - UNO supports `AppWindow.SetPresenter()` for full-screen mode
   - `ApplicationView.GetForWindowId()` for visible bounds (not `GetForCurrentView()`)
   - Source: [UNO Platform Window Features](https://github.com/unoplatform/uno/blob/master/doc/articles/features/windows-ui-xaml-window.md)

5. **NavigationView Alternative**
   - `PaneDisplayMode="Auto"` adapts automatically based on width
   - Could be alternative to SplitView for future consideration
   - Source: [UNO Platform Silverlight Migration Guide](https://github.com/unoplatform/uno/blob/master/doc/articles/guides/silverlight-migration/03-review-app-startup.md)

#### Updated Issue #25

- Added Research section to GitHub issue body
- Added devdocs folder reference
- Added open questions from research

#### Recommendation

**Option A: Visual State Manager Only** is recommended for MVP:
- Declarative, simple implementation
- Works cross-platform without code changes
- Can add mobile-specific orientation detection later (Option C hybrid)

#### Next Steps

- Design phase: Define breakpoints (800px suggested for wide/narrow)
- Prototype: Add VisualStateManager to Shell.xaml
- Test on Windows and macOS with different window sizes
- Future: Add mobile orientation detection when targeting iOS/Android

---
