# Issue 1682: Empty outline files (PR #1686)

## Current Status
- **Phase**: PR #1686 open against `main`, Release 4.3, awaiting review
- **Date**: 2026-11-05
- **Branch**: `issue-1682-empty-outline` (base `main`), worktree `D:\dev\src\StoryCAD-1682`

## Session Log

### 2026-11-05
- A new outline is written to its file when it is created. A blank name is refused; names that start with a dot, such as ".45 Magnum", still work. `FileCreateService.cs`, 20 lines.
- Reading an empty outline file logs the Error "Outline file is empty" with the path, instead of a JSON parser failure. `StoryIO.cs`, 7 lines.
- Four new tests (8 cases). 6 cases failed before the fix; the 2 dot-name cases passed before and after.
- Build clean. Full suite: 1784 tests, 1765 passed, 17 skipped, 2 failed (`CheckDotEnvFile`, `CheckDoppler`; no `.env` file in the worktree).
- Replaces the #1682 part of closed PR #1683.
- Open: what the writer should see when opening an empty file. It is still "Unable to open file (No Story Elements found)".
