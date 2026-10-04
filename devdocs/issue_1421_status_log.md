# Issue 1421 status log

Session recovery for #1421 (UI automation DSL and runner). The issue body is the checklist; this file holds build state, test counts and progress only.

## 2026-10-04

- Restarted under the milestone plan in the #1421 milestone 1 scope comment.
- Merged `dev` into the branch (0 behind). Removed `devdocs/issue_1421_handoff.md`.
- Baseline before changes: full solution build, both targets. MSTest WinAppSDK 1745 passed, 15 skipped; desktop 1744 passed, 12 skipped. AutomationConventionTests 7/7.
- Done: review B1, M1, M5; `STORYCAD_ROOT_DIR` replaces the build copy; minimal runner; `StatusMessageText` AutomationId; smoke script.
- Smoke script: three consecutive passes on Brigid (28.3 s, 28.2 s, 27.8 s). Milestone 1 gate met.
- Tests after changes: WinAppSDK 1755 passed, 15 skipped (one earlier full run had 2 Scrivener report failures that passed on rerun); desktop 1746 passed, 12 skipped; AutomationConventionTests 7/7.
- Next: milestone 2 (CI on PRs; one manual screenshot from a script).

How to run locally: build Debug x64, move `StoryCAD/bin/x64/Debug/net10.0-windows10.0.22621/win-x64/.env` aside, run `StoryCADAutomation\bin\x64\Debug\net10.0-windows10.0.22621\StoryCADAutomation.exe run StoryCADTests\ManualTests\Smoke_Test.scs`, then restore `.env`.
