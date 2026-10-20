# Issue #1626 status log

## 2026-10-20: session recovery

**Branch:** `issue-1626-youtube-upload` in worktree `D:\dev\src\StoryCAD-1626` (never switch branches in `D:\dev\src\StoryCAD`; other sessions use it). Local only, not pushed.

**Commits on the branch:** `09866ee9`, `ca243f51`, `08c7b395` (youtube auth and upload commands), `addfd266` (privacy policy), `0530b9c8` (first-card cover, dissolves, card fades), `c2ef56d7` (music credit line), `f613349f` (fast typing in the presentation profile), `1adcd181` (outlines from the assets folder; `\n` in `show text`), and this commit (`run --assets`).

**Tests:** full suite 2023 tests, 2004 passed, 17 skipped, 2 failed (`CheckDotEnvFile`, `CheckDoppler`: this worktree has no `.env`).

**Open problem (next session starts here):** `Intro-Video.scs` step "The outline file" clicks Open from file in the start dialog shown at launch. Under the automation runner (Debug build from this worktree), StoryCAD shows "Open Story command failed" and no file picker opens; a 3 s wait and a placed, foreground window do not change it. Opening via Ctrl+O, then Open from file, works under the runner (Core-File-Operations.scs). Terry reports Open from file at launch works in his normal StoryCAD. Not yet known: whether the cause is in StoryCAD or in the runner's environment. Goal: a working product, so find the cause; do not route around it.

Next steps:
1. Reproduce with the same Debug build started by hand (no runner) to separate StoryCAD from the runner environment.
2. Read StoryCAD's log for the failing run (the runner deletes its scratch folder at teardown; keep it).
3. Fix the cause; re-run `run Intro-Video.scs --assets C:\temp\intro-assets`, then re-record with `video`.
