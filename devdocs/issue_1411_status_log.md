# Issue #1411 — Hamburger / Stacked Mode — Status Log

**Branch:** `issue-1411-hamburger-stacked-mode` (based on `main` / `dev` — they were at the same commit at branch time)
**Issue:** https://github.com/storybuilder-org/StoryCAD/issues/1411
**Last update:** 2026-05-19, end of session.

---

## Commits on this branch (top to bottom = newest to oldest)

1. `14758eed` — Track pane width during narrow-to-narrow resize (#1411)
2. `a1c591fe` — Fix narrow-resize closing the pane (#1411 regression)
3. `af59b025` — Fix stacked-mode hamburger toggle at narrow widths (#1411)

Nothing pushed. Push decision pending — see "Open" below.

## What is done

- **Diagnosis** (delegated to debugger agent): `ShellSplitView.IsPaneOpen` was bound `TwoWay` to `ShellViewModel.IsPaneOpen` (default `true`); the `VisualStateManager`'s `NarrowState` setter `IsPaneOpen=False` was immediately overridden by the binding back-flow. Same root cause on HP-Spectre-style rotation because Windows does not resize a non-maximized window on rotation, so the `MinWindowWidth` trigger never fires.
- **Code change**:
  - `ShellViewModel`: added `internal HandleSizeChanged(double width)`, `internal IsStacked`, public `DisplayMode` (`SplitViewDisplayMode`), public `OpenPaneLength` (`double`). All are notifying properties (`SetProperty`).
  - `Shell.xaml`: removed `IsPaneOpen`/`DisplayMode`/`OpenPaneLength` setters from the VSM; bound `DisplayMode` and `OpenPaneLength` OneWay to the VM; kept the `CommandBar.OverflowButtonVisibility` VSM setter.
  - `Shell.xaml.cs`: `ShellPage_SizeChanged` is now a one-line `ShellVm.HandleSizeChanged(e.NewSize.Width)`. `AdjustSplitViewPane` is gone. `Shell_Loaded` calls the VM method too.
  - `StoryCADLib/Properties/AssemblyInfo.cs`: added `InternalsVisibleTo("StoryCAD")` so the executable project can see the internal VM method.
- **Final shape of `HandleSizeChanged`**:
  - On wide↔narrow crossing: set `OpenPaneLength` (full window in stacked, `Math.Max(200, width*0.3)` in wide), `DisplayMode`, `IsPaneOpen=!nowStacked`, `IsStacked=nowStacked`.
  - Within stacked, with pane open: `OpenPaneLength = width` so the pane never exceeds window width.
  - All other cases: no-op.
- **Tests** (13 new VM tests in `StoryCADTests/ViewModels/ShellViewModelTests.cs`, all green):
  - Truth-table for transitions, DisplayMode, OpenPaneLength
  - `NarrowToNarrow_DoesNotChangeIsPaneOpen`, `NarrowAndUserOpenedPane_DoesNotForceClose`
  - `NarrowToNarrow_DoesNotRaiseDisplayModeChanged`, `WideToWide_DoesNotRaiseDisplayModeChanged`
  - `WhenStackedAndPaneOpen_TracksPaneToNewWidth`
  - `WhenStackedAndPaneClosed_DoesNotChangePaneLength`
  - `WhenWide_DoesNotApplyStackedSizing`
  - Full suite: 1005 passed / 14 skipped / 0 failed (1019 total).
- **Manual test added**: `StoryCADTests/ManualTests/Window_Management.md` WM-006 "Hamburger Button and Stacked Mode at Narrow Width", 10 steps including portrait-rotation note.
- **Issue body**: Design and Implementation sections both have Plan + Approve checked. Integration section has Plan checked with WM-006 citation.

## What is open / blocked

- **WM-006 step 8 manual confirmation**: I committed `14758eed` and stated desk-debug shows the fix works, but Terry has not yet run the build and confirmed step 8 actually passes in the running app. **This is the next blocker.** If it's still broken, the next move is diagnostic (logging — see below), not another guess.
- **WM-001 step 3 expected-outcome update**: at ~400px the window now enters stacked mode; the existing expected outcome doesn't mention that. I proposed an edit; Terry said "No" — wording or approach pending direction.
- **Window_Management.md restructure**: Terry asked for Windows tests grouped, then macOS tests grouped (WM-004 currently sits in the middle), with better macOS coverage. Not started. Scope of the macOS additions to be agreed before doing.
- **Push decision**: this is a fix against production behavior described in the user manual.
  - Option A: hotfix → rebase onto `main`, PR to `main`, ship as 4.1.2 (Windows MSIX + Mac App Store + NuGet.org). The blog post can publish on schedule.
  - Option B: ride `dev` to 4.2 — no new release needed, but the marketing blog post about the hamburger button has to be held until 4.2 ships.
  - Terry is talking to Jake. The branch sits cleanly on either base today because `main` HEAD == `origin/dev` HEAD.

## Logging — gap to address

`HandleSizeChanged` has no logging. The notifying setters for `IsPaneOpen` / `IsStacked` / `DisplayMode` / `OpenPaneLength` have no logging. The model right above is `TogglePane`, which traces (`Logger.Log(LogLevel.Trace, ...)`).

If WM-006 step 8 turns out to still be broken on the manual retest, **the right next step is to add tracing — not to write another hypothesis-driven patch**. Specifically:
- `HandleSizeChanged` should trace its inputs and the branch it takes.
- The `IsPaneOpen` setter should trace its caller (stack frame is enough). That will tell us deterministically which code path is closing the pane, removing the "Option A vs Option B / closes vs clips" guesswork from earlier sessions.

This was flagged by the debugger agent and not acted on. Adding it as standing follow-up regardless of whether step 8 passes — the methods are too central to leave silent.

## Pickup notes for next session

1. **First action**: confirm with Terry whether WM-006 step 8 now passes on a real build. Do not assume.
2. If step 8 still broken: add the logging described above; reproduce; let the trace name the culprit.
3. If step 8 passes: pick up the open items in this order — WM-001 step-3 wording (re-propose, get direction), then Window_Management.md restructure (scope macOS additions first), then push decision once Jake responds.
4. Do not delegate design while pre-listing solution candidates. Memory: there is no formal memory file on this lesson (the proposed one was rejected); the rule is "describe the bug and constraints, let the agent propose its own shape".
