# Issue 1681: Remove Trait crash (PR #1685)

## Current Status
- **Phase**: PR #1685 open against `main`, Release 4.3, awaiting review
- **Date**: 2026-11-05
- **Branch**: `issue-1681-remove-trait-crash` (base `main`), worktree `D:\dev\src\StoryCAD-1681-trait`

## Session Log

### 2026-11-05
- `RemoveTrait` now does nothing when the selected trait number is outside the trait list. 3 lines in `CharacterViewModel.cs`.
- Test `RemoveTrait_WithSelectionOutsideTraitList_DoesNotThrow` failed with `ArgumentOutOfRangeException` before the fix and passes after.
- Build clean. Full suite: 1777 tests, 1758 passed, 17 skipped, 2 failed (`CheckDotEnvFile`, `CheckDoppler`; no `.env` file in the worktree).
- Replaces the #1681 part of closed PR #1683.
- Open: the user steps that leave the trait number out of range are not known.
