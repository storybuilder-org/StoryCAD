# Issue #1626 status log

## 2026-10-20 (evening): session recovery

**Branch:** `issue-1626-youtube-upload` in worktree `D:\dev\src\StoryCAD-1626`. Local only, not pushed.

**Done:** from an unelevated Claude Code session, `run Intro-Video.scs --assets C:\temp\intro-assets` passed (55 s). `video` recorded `C:\temp\intro-run-2\Intro-Video.mp4` (3:45, 1920x1080, 30 fps), using the 18 existing voice clips (none made). The title card shows the music credit; Santiago's Backstory tab has text.

**Terry's notes on that render, fixed and re-recorded** (3:49): a card followed by a card in the next step stays opaque until the next has faded in, so the app no longer flashes between them; the story elements card has an upper-case heading and a blank line; the music credit is 28 px (was 36). A meta.json for upload holds the store links in the description; it sits with the assets, outside this repo. Video tests 73 passed.

**Shared title-card files** (Terry, 2026-10-20): `video` takes `--shared <dir>` for the logo, intro music and credit that every video uses; a copy in the assets folder wins. Not built or run yet (Terry: no rebuild for this); the next build runs the two new tests.

**Next:** Terry watches the new render. Remaining review items: cursor glide, zoom on the active area, music fade, burned-in captions, end-card silence.

## 2026-10-20 (later): session recovery

**Branch:** `issue-1626-youtube-upload` in worktree `D:\dev\src\StoryCAD-1626`. Local only, not pushed.

**Resolved:** the Open from file failure below is bug #1652 (StoryCAD run as administrator; low priority, Terry). The session's terminal ran as administrator, so the runner and StoryCAD did too. Started without administrator rights, the script opens the outline (lines 24 to 26 pass). `Windowing.ShowFileOpenPicker` now logs the exception type and HResult (this commit).

**Open problem (next session starts here):** with the runner unelevated and the terminal elevated, the run stops at line 54 (`menu OpenCreateFileMenuItem`): the foreground window is the terminal, and Windows blocks an unelevated program from taking the foreground from an elevated one. Terry is restarting Claude Code without administrator rights.

Next steps:
1. In the unelevated session, run `run StoryCADTests\AutomationScripts\Intro-Video.scs --assets C:\temp\intro-assets` with nobody typing in the terminal.
2. When it passes, record with `video`.
3. Then Terry's 2026-10-20 overview notes: blank tabs (Backstory for Santiago and Manolin).

`Get-VM` works without administrator rights (Hyper-V Administrators group); `Run-InVM.ps1`'s `New-PSSession -VMName` is not yet tried unelevated.

## 2026-10-20: session recovery

**Branch:** `issue-1626-youtube-upload` in worktree `D:\dev\src\StoryCAD-1626` (never switch branches in `D:\dev\src\StoryCAD`; other sessions use it). Local only, not pushed.

**Commits on the branch:** `09866ee9`, `ca243f51`, `08c7b395` (youtube auth and upload commands), `addfd266` (privacy policy), `0530b9c8` (first-card cover, dissolves, card fades), `c2ef56d7` (music credit line), `f613349f` (fast typing in the presentation profile), `1adcd181` (outlines from the assets folder; `\n` in `show text`), and this commit (`run --assets`).

**Tests:** full suite 2023 tests, 2004 passed, 17 skipped, 2 failed (`CheckDotEnvFile`, `CheckDoppler`: this worktree has no `.env`).

**Open problem (next session starts here):** `Intro-Video.scs` step "The outline file" clicks Open from file in the start dialog shown at launch. Under the automation runner (Debug build from this worktree), StoryCAD shows "Open Story command failed" and no file picker opens; a 3 s wait and a placed, foreground window do not change it. Opening via Ctrl+O, then Open from file, works under the runner (Core-File-Operations.scs). Terry reports Open from file at launch works in his normal StoryCAD. Not yet known: whether the cause is in StoryCAD or in the runner's environment. Goal: a working product, so find the cause; do not route around it.

Next steps:
1. Reproduce with the same Debug build started by hand (no runner) to separate StoryCAD from the runner environment.
2. Read StoryCAD's log for the failing run (the runner deletes its scratch folder at teardown; keep it).
3. Fix the cause; re-run `run Intro-Video.scs --assets C:\temp\intro-assets`, then re-record with `video`.
