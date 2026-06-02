# Issue #1411 — Hamburger / Stacked Mode — Status Log

**Branch:** `issue-1411-hamburger-stacked-mode` (based on `main` / `dev` — they were at the same commit at branch time)
**Issue:** https://github.com/storybuilder-org/StoryCAD/issues/1411
**Last update:** 2026-06-02, end of session.

---

## Commits on this branch (top to bottom = newest to oldest)

1. `14758eed` — Track pane width during narrow-to-narrow resize (#1411)
2. `a1c591fe` — Fix narrow-resize closing the pane (#1411 regression)
3. `af59b025` — Fix stacked-mode hamburger toggle at narrow widths (#1411)

Nothing new committed. The 2026-06-02 work below is **uncommitted in the working tree**.

## Current status: NOT FIXED

Manual testing on a real build (2026-06-02) shows the narrow-mode hamburger still does
not work both ways: when narrow, the hamburger switches from content to the navigation
pane once, but a second click does not switch back to content.

## 2026-06-02 session

### What was changed (all uncommitted in the working tree)

- `StoryCADLib/ViewModels/ShellViewModel.cs`
  - Added Info-level logging in the `IsPaneOpen`, `IsStacked`, `DisplayMode`, and
    `OpenPaneLength` property setters (logs old -> new on change).
  - `HandleSizeChanged(double width)` — added Info logging of the branch taken
    (wide<->narrow transition / narrow resize / no-op). The no-op log line was
    later removed as noise; transition and narrow-resize logging remain.
  - `TogglePane()` — existing log line raised from Trace to Info.
  - Added `internal bool ShouldCancelPaneClose() => IsStacked && IsPaneOpen;`
- `StoryCAD/Views/Shell.xaml`
  - `ShellSplitView`: `IsPaneOpen` binding changed from TwoWay to OneWay.
  - Added `PaneClosing="ShellSplitView_PaneClosing"`.
- `StoryCAD/Views/Shell.xaml.cs`
  - Added `ShellSplitView_PaneClosing` handler: `if (ShellVm.ShouldCancelPaneClose()) args.Cancel = true;`
- `StoryCADTests/ViewModels/ShellViewModelTests.cs`
  - Added `ShouldCancelPaneClose_WhenStackedAndNavShown_CancelsResizeClose`,
    `ShouldCancelPaneClose_WhenStackedAndContentShown_DoesNotCancel`,
    `ShouldCancelPaneClose_WhenWide_DoesNotCancel`.

### What the changes were trying to do

The earlier symptom was that a resize in narrow mode closed the navigation pane on its
own. The logging confirmed the SplitView closes its own open pane during a resize and
(under the old TwoWay binding) wrote that close back into the view-model. The change made
`IsPaneOpen` OneWay so only the view-model sets it, and used the `PaneClosing` event to
cancel a close while stacked with the pane open.

### Result

- Unit tests pass (full `ShellViewModelTests` class: 98 passed, 7 skipped, 0 failed).
- The app still fails: the second hamburger click in narrow mode does nothing.

### Key evidence not yet explained

In the manual-test log, the first hamburger click logs `TogglePane from False to True`.
The **second** click logs **nothing at all** — no `TogglePane` line. `TogglePane` always
logs when it runs, so on the second click the command is not being invoked. None of the
view-model changes above can affect this, because the code that would run on a click is
never reached on the second click.

An earlier guess that the open navigation pane covers the hamburger button was checked and
is wrong: the hamburger `AppBarButton` is in the `CommandBar` in page Grid row 0
(`Shell.xaml:192`), while the `SplitView` is in row 1, so the pane does not overlap the
button.

Cause of "second click invokes nothing" is still unknown. It needs evidence, not another
guess: confirm the manual-test log is complete through the second click, and determine
which element actually receives the second click (and whether `TogglePaneCommand`'s
CanExecute is false at that point).

## Open items carried over from prior sessions

- WM-001 step 3 expected-outcome wording — proposed edit was rejected; awaiting direction.
- Window_Management.md restructure — group Windows tests, then macOS tests; add macOS
  coverage. Not started.
- Push decision — 4.1.2 hotfix off `main` vs. ride `dev` to 4.2.

## Pickup notes for next session

1. The narrow-mode hamburger is not fixed. Start from the unexplained evidence above:
   the second click invokes no command.
2. Get a complete manual-test log through the second click before theorizing.
3. The 2026-06-02 changes are uncommitted; decide whether to keep the logging, keep/revert
   the OneWay + PaneClosing approach, or revert all of it.

## Codex handoff — 2026-06-02

User clarified the intended behavior:
- Wide mode: Navigation and Content panes are side-by-side; hamburger hides/shows Navigation.
- Narrow/portrait stacked mode: only one pane is visible at a time. Entering narrow should show Content.
  Hamburger switches to Navigation; next hamburger switches back to Content. Narrow-to-narrow resize must
  not change the user's current pane.

Working direction approved by user: keep the fix small and VM-centered. Avoid new code-behind events and
avoid workaround tests for control events. Use `ShellViewModel.HandleSizeChanged` for layout state and keep
`TogglePaneCommand` as the pane switch.

Current content changes made by Codex:
- `StoryCADLib/ViewModels/ShellViewModel.cs`
  - In `HandleSizeChanged`, narrow mode now keeps `DisplayMode = SplitViewDisplayMode.Inline` instead of
    `Overlay`.
  - While `IsStacked`, `OpenPaneLength` tracks the current width regardless of `IsPaneOpen`.
- `StoryCADTests/ViewModels/ShellViewModelTests.cs`
  - Narrow display-mode test now expects `Inline`.
  - Stacked/narrow resize test now expects `OpenPaneLength` to track width even when the pane is closed.
  - Removed misleading test comment about fighting the TwoWay binding.

Important: use the repo's WinAppSDK verification path only. Do not use `dotnet test` or chase Uno test
side paths.
- Build:
  `C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe StoryCAD.sln -t:Build -p:Configuration=Debug -p:Platform=x64`
- Tests:
  `C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe "StoryCADTests\bin\x64\Debug\net10.0-windows10.0.22621\StoryCADTests.dll"`

Verification status:
- Build command failed before compilation with `MSB3491: Access to the path is denied` while writing generated
  files under `obj\x64\Debug\...`.
- User deleted all `bin`/`obj` folders and the exact build still failed immediately on generated `GlobalUsings.g.cs`
  and `FileListAbsolute.txt` writes.
- No compiler errors from the code change were observed.
- Tests were not run because the test DLL was not rebuilt.
- User is rebooting before retrying.

Next pickup:
1. After reboot, rerun the exact MSBuild command above.
2. If build succeeds, run the exact `vstest.console` command above.
3. Then manually smoke narrow stacked hamburger: enter narrow -> content visible; click hamburger -> nav visible;
   click hamburger again -> content visible; narrow resize must not switch panes.
