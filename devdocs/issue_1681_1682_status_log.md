# Issues #1681 and #1682 status log

Branch `issue-1681-1682-production-fixes`, based on `main`, for the next 4.3.x point release.

## 2026-11-04

- #1681 (Remove Trait crash): fixed. The trait selection starts at -1 (nothing selected), resets when a character loads, and `RemoveTrait` ignores an index outside the list. 5 new tests in `CharacterViewModelTests`.
- #1682 (empty outline file): fixed.
  - New outlines are written to disk at creation, and blank names are rejected.
  - Every open path reports an empty file as empty, logs a warning, and keeps the open outline.
  - The API and Copy Elements fail cleanly on unreadable files.
  - The New Outline dialog stays open with a warning when the name or folder is bad.
  - The Python binding starts headless by default.
- Build: clean. Full suite: 1818 tests, 1799 passed, 17 skipped, 2 failed (`CheckDotEnvFile`, `CheckDoppler`; the worktree has no `.env` file, and both fail without these changes).
- Not tested: saving on macOS and into a OneDrive folder; the Python binding test suite (pytest not installed here).
