# Issue #1323: Right-Click Menu Submenu Fix (v2)

This document supersedes v1. The key change is the discovery of `FlyoutBase.Closing` with a cancellable `Cancel` property, which provides a clean mechanism for handling the diagonal mouse movement problem.

## Problem

On macOS/Skia, right-click context menu submenus disappear when the user moves the cursor from the parent menu item into the submenu. Works fine on WinAppSDK (Windows).

Two distinct sub-problems:

1. **Horizontal gap**: A physical pixel gap between the parent `AppBarButton` and the child `MenuFlyout` popup causes `PointerExited` to fire, dismissing the submenu.
2. **Diagonal movement**: Moving diagonally from the parent button toward a lower submenu item causes the cursor to enter a sibling `AppBarButton`, which triggers UNO's internal logic to close the submenu.

## Root Cause

UNO Platform's Skia backend manages all pointer events in a fully managed layer with no native OS bridging. The parent `AppBarButton` and its child `MenuFlyout` submenu live in separate Popup elements with independent hit-test regions.

On WinAppSDK, the diagonal problem was fixed at the OS level in Windows 11 build 22000.120 (see [microsoft-ui-xaml#5617](https://github.com/microsoft/microsoft-ui-xaml/issues/5617)). UNO's managed reimplementation does not include this fix.

**Related UNO issues:**
- [#22036](https://github.com/unoplatform/uno/issues/22036) — Pointer events broken for menu controls on Skia Desktop (open, Dec 2025)
- [#4795](https://github.com/unoplatform/uno/issues/4795) — Nested MenuFlyoutSubItem light-dismiss problems
- [#19404](https://github.com/unoplatform/uno/issues/19404) — CommandBar flyout positioning wrong on UNO/Skia

---

## Fix Part 1: Negative Margin (Horizontal Gap)

### Approach

A `MenuFlyoutPresenterStyle` with negative left margin on the `MenuFlyout` inside the "Add Elements" `AppBarButton` pulls the submenu popup leftward, overlapping with the parent button and eliminating the horizontal gap.

```xml
<MenuFlyout>
    <MenuFlyout.MenuFlyoutPresenterStyle>
        <Style TargetType="MenuFlyoutPresenter">
            <Setter Property="Margin" Value="-16,0,0,0"/>
        </Style>
    </MenuFlyout.MenuFlyoutPresenterStyle>
    <!-- items -->
</MenuFlyout>
```

### Why This Works

The `MenuFlyoutPresenter` is rendered inside the popup. Negative left margin shifts the content leftward, causing visual overlap with the parent button. The cursor no longer crosses dead space.

### Why Padding on the AppBarButton Does Not Work

Padding makes the button larger, but the `MenuFlyout` positions itself relative to the button's edge. Both sides of the gap move together.

### Test Results

| Configuration | Result |
|---|---|
| Windows 3840x2160 @ 150% | Submenu accessible, reliable horizontal movement |
| Mac Mini external LG monitor | Submenu accessible, no horizontal gap |
| MacBook Retina 2x | Horizontal movement works; **diagonal movement fails** |

### Status

Implemented and tested. The `-16` value is empirical — it worked on all three tested configurations. This value may need adjustment if UNO updates its flyout presenter template. It should be documented as a magic number.

### Scaling Concern

The `-16` margin value is in logical pixels, but UNO Skia's handling of margins in popup elements under different display scaling factors is not fully verified. [UNO #19246](https://github.com/unoplatform/uno/issues/19246) documents incorrect flyout positioning under scaling on Skia targets. If the negative margin does not scale proportionally with the display scale factor, the gap could reappear (or the overlap could become excessive) on untested configurations. The fix worked on all three tested configurations (150% Windows, 1x Mac external, 2x Retina), but this remains a risk.

---

## Fix Part 2: Cancellable Closing Event (Diagonal Movement)

### The Diagonal Problem

The `CommandBarFlyout.SecondaryCommands` stacks multiple `AppBarButton` elements vertically. When the user moves diagonally from "Add Elements" toward a lower submenu item, the cursor passes through sibling buttons (or the `AppBarSeparator` at line 71 of Shell.xaml). Those elements' `PointerEntered` handlers fire, triggering UNO's internal logic to close the open submenu.

No amount of margin or padding on the submenu or the button solves this — the cursor is entering *sibling elements*, not crossing empty space.

### The Key Discovery: FlyoutBase.Closing

`MenuFlyout` inherits from `FlyoutBase`, which has a `Closing` event. The event args (`FlyoutBaseClosingEventArgs`) have a writable `Cancel` property. Setting `args.Cancel = true` prevents the flyout from closing.

**Documentation:** [FlyoutBase.Closing Event](https://learn.microsoft.com/en-us/uwp/api/windows.ui.xaml.controls.primitives.flyoutbase.closing), [FlyoutBaseClosingEventArgs.Cancel](https://learn.microsoft.com/en-us/uwp/api/windows.ui.xaml.controls.primitives.flyoutbaseclosingeventargs.cancel)

This means we do not need to:
- Intercept UNO's internal `PointerExited` handling
- Create custom control subclasses
- Walk the visual tree
- Fight with event routing order

We handle `Closing` on the `MenuFlyout` itself and cancel the close under the right conditions.

### Approach: Timer-Based Cancellation

When the `MenuFlyout` starts to close, cancel the close and start a short timer (350-400ms, matching Windows' `MenuShowDelay` convention). If the pointer enters the submenu within that window, cancel the timer and keep the menu open. If the timer expires, allow the close.

This is the same mechanism Windows uses natively for its Win32 menus (`MenuShowDelay` registry value, default 400ms). It is not as elegant as the macOS safe triangle (see Appendix A), but it is:
- Simple to implement
- Does not require coordinate calculations or geometric detection
- Does not depend on screen geometry, resolution, or scaling
- Proven effective (Windows has used this approach for decades)

### Scope

This fix should only apply on UNO Skia builds. Windows already has the OS-level fix (build 22000.120+). Use `#if HAS_UNO` conditional compilation.

### Integration Points

The `Closing` handler needs to be attached to the `MenuFlyout` inside the "Add Elements" `AppBarButton`. This requires:

1. A reference to the `MenuFlyout` — obtainable from the `CommandBarFlyout` which is already accessed in Shell.xaml.cs (line 55: `_contextFlyout = (CommandBarFlyout)ShellPage.Resources["AddStoryElementFlyout"]`)
2. Navigating from the `CommandBarFlyout` to the specific `AppBarButton` to its `Flyout` property
3. Attaching the `Closing` event handler

The handler also needs to know when the pointer enters the submenu (to cancel the timer). The `MenuFlyout.Opened` event can be used to set up tracking, and `MenuFlyout.Closed` to clean up.

### Interaction with Other Flyouts

The "Empty Trash" `AppBarButton` (Shell.xaml line 113) also has a nested `Flyout` (a confirmation dialog). The timer-based cancellation must only apply to the "Add Elements" `MenuFlyout`, not to other flyouts in the `CommandBarFlyout`.

### Touch Input

The diagonal problem does not affect touch input (no hover/diagonal movement between taps). The `Closing` handler should check `PointerDeviceType` and only apply the timer logic for mouse input.

### Other Affected Menus

The "Plotting Aids" `MenuFlyoutSubItem` in the Tools menu (Shell.xaml line 373) has the same potential diagonal problem. If the timer approach works for "Add Elements", it should be applied there as well.

---

## How Other Platforms Solve This

### macOS: Safe Triangle

Invented by Tognazzini and Batson at Apple in the 1980s. A virtual triangle is maintained between the cursor position and the two far corners of the open submenu. As long as the cursor stays inside this triangle, the submenu stays open. Purely geometric — no timer. See Appendix A.

### Windows: Timer Delay

Win32 uses `MenuShowDelay` (default 400ms, configurable via registry). When the cursor leaves a parent menu item, the system waits before closing the submenu. Simple but makes menus feel slightly sluggish.

WinUI 3's diagonal bug ([#5617](https://github.com/microsoft/microsoft-ui-xaml/issues/5617)) was fixed at the OS level in Windows 11 build 22000.120 by correcting timer cancellation logic in `AppBarButton.OnPointerEntered`.

### Comparison

| Aspect | macOS (Safe Triangle) | Windows (Timer) | Our Approach |
|---|---|---|---|
| Mechanism | Geometric hit-test | Time delay | Time delay via Closing cancellation |
| Responsiveness | Instant | 400ms delay | 350-400ms delay |
| Complexity | Moderate | Simple | Simple |
| Screen geometry dependent | No | No | No |
| Custom controls needed | No (built into NSMenu) | No (built into OS) | No |

---

## Open Questions

1. **Does `FlyoutBase.Closing` with `Cancel = true` work on UNO Skia?** This is documented for WinUI but needs testing on UNO. If it doesn't work, this approach fails.

2. **Can we detect pointer entry into the submenu from the `Closing` handler context?** We need a way to know the pointer has reached the submenu so we can cancel the timer and keep the menu open permanently (until the user makes a selection or moves away).

3. **What is the right timer value?** Windows uses 400ms. Jake's code used 350ms. Too short and diagonal movement still fails; too long and the menu feels sticky.

4. **Does the `AppBarSeparator` (Shell.xaml line 71) between "Add Elements" and "Delete element" affect the diagonal behavior?** It's another hit-test element the cursor crosses. Needs testing.

5. **Is `-16` the right margin value across all configurations?** Tested on three setups. May need adjustment for others. See "Scaling Concern" above and Appendix B. [UNO #19246](https://github.com/unoplatform/uno/issues/19246) suggests flyout positioning may not scale correctly on Skia.

---

## Appendix A: The Safe Triangle

The safe triangle is the gold standard solution for diagonal menu navigation, used by macOS since the 1980s.

### Geometry

Three vertices define the triangle:
1. **Cursor position** — the current pointer location on the parent menu item
2. **Submenu top-far corner** — the top-right corner of the open submenu (for a right-opening submenu)
3. **Submenu bottom-far corner** — the bottom-right corner of the open submenu

```
        cursor ·──────────────────── submenu top corner
                 \                  │
                   \                │
                     \   SAFE ZONE  │  submenu items
                       \            │
                         \          │
                           \────────── submenu bottom corner
```

### Decision Logic

As the cursor moves, the system checks whether the cursor is inside the triangle. If yes, the submenu stays open even if the cursor passes over other parent menu items. If the cursor exits the triangle (e.g., moves straight down the parent menu), the submenu closes immediately.

### Properties
- **No timer** — purely geometric, instant response
- **Dynamic** — the triangle updates as the cursor moves
- **Screen-independent** — defined in logical coordinates relative to the menu elements
- **Trajectory-aware** — distinguishes "moving toward submenu" from "moving away"

### Implementation Status

The safe triangle is the preferred solution — it is responsive, trajectory-aware, and does not add artificial delay to the UI. It requires:
1. Continuous cursor position tracking during the flyout's lifetime
2. Submenu corner coordinates (obtainable via `TransformToVisual` on `MenuFlyoutItem` elements after `MenuFlyout.Opened`, but coordinate space across popup boundaries on UNO Skia is unverified)
3. A mechanism to suppress or cancel the submenu close when the cursor is inside the triangle

Item 3 is now solved (`FlyoutBase.Closing` with `Cancel`). Items 1 and 2 are feasible but need validation on UNO Skia. The timer approach (Part 2 above) is the simpler starting point if coordinate access across popup boundaries proves unreliable. Both approaches use the same `Closing` cancellation mechanism.

### Point-in-Triangle Algorithm (for future reference)

```csharp
private static bool IsPointInTriangle(Point p, Point a, Point b, Point c)
{
    double d1 = CrossProduct(p, a, b);
    double d2 = CrossProduct(p, b, c);
    double d3 = CrossProduct(p, c, a);

    bool hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
    bool hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);

    return !(hasNeg && hasPos);
}

private static double CrossProduct(Point p1, Point p2, Point p3)
{
    return (p2.X - p1.X) * (p3.Y - p1.Y) - (p3.X - p1.X) * (p2.Y - p1.Y);
}
```

---

## Appendix B: Skia Hit Testing and DPI Scaling

On UNO Skia, all pointer events run in a fully managed layer. Key considerations for hit-test accuracy:

- **DPI Scaling**: On high-DPI displays (macOS Retina, Windows 150%+), logical pointer coordinates may not match Skia's pixel coordinates. Input coordinates may need scaling by the display's scale factor.
- **Effective Pixels**: WinAppSDK and UNO use effective pixels (epx) to abstract physical resolution. Formula: `Effective Size = Physical Pixels / (DPI / 96)`.
- **Automatic Scaling**: For standard XAML controls, UNO handles scaling automatically — a `Padding="12"` at 200% scale physically occupies 24px. But for low-level Skia drawing or popup margin hacks (like our `-16` fix), the system may not scale margins consistently across popup boundaries.
- **Minimum Target Sizes**: For reliable hit testing: 44x44 epx (mouse+touch), 32x32 epx (mouse only). If a visual element is smaller, padding should expand the hit-testable area: `Total Target Area = Visual Size + (2 × Padding)`.

Sources: Gemini research on UNO Skia hit testing; Windows Fluent Design target size guidelines; Adrian Roselli on WCAG 2.5.5 target sizing.

---

## References

- [WinUI #5617 — AppBarButton submenu diagonal dismissal](https://github.com/microsoft/microsoft-ui-xaml/issues/5617)
- [FlyoutBase.Closing Event — Microsoft Learn](https://learn.microsoft.com/en-us/uwp/api/windows.ui.xaml.controls.primitives.flyoutbase.closing)
- [FlyoutBaseClosingEventArgs.Cancel — Microsoft Learn](https://learn.microsoft.com/en-us/uwp/api/windows.ui.xaml.controls.primitives.flyoutbaseclosingeventargs.cancel)
- [UNO #22036 — Pointer events broken on Skia Desktop](https://github.com/unoplatform/uno/issues/22036)
- [UNO #4795 — Nested MenuFlyout light-dismiss problems](https://github.com/unoplatform/uno/issues/4795)
- [Better Context Menus With Safe Triangles — Smashing Magazine](https://www.smashingmagazine.com/2023/08/better-context-menus-safe-triangles/)
- [Breaking Down Amazon's Mega Dropdown — Ben Kamens](https://bjk5.com/post/44698559168/breaking-down-amazons-mega-dropdown)
- [Creating a Pointer-Friendly Submenu Experience — React Spectrum (Adobe)](https://react-spectrum.adobe.com/blog/creating-a-pointer-friendly-submenu-experience.html)
- [StoryCAD Issue #1212 — Window sizing on small displays](https://github.com/storybuilder-org/StoryCAD/issues/1212)
- [StoryCAD Issue #1164 — Window centering on macOS external monitor](https://github.com/storybuilder-org/StoryCAD/issues/1164)
- [UNO Platform Feature Flags](https://platform.uno/docs/articles/feature-flags.html) — Global behavior flags for popups, light dismiss, scaling
- [UNO #19246 — Flyout positioning incorrect under scaling on Skia](https://github.com/unoplatform/uno/issues/19246)
