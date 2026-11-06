# Issue #1692 status log

Branch `issue-1692-hide-collaborator`, worktree `../StoryCAD-1692`, based on `dev` `a5437f33`.

## 2026-11-06

- Design drafted: `devdocs/issue_1692_design.md`. Waiting for approval.
- Code, tests, manual: not started.

## 2026-11-06, implementation

- Design approved by Terry.
- Code and 8 unit tests written by TDD. Build: 0 errors, no new warnings.
- Full `StoryCADTests` run: 2281 passed, 2 failed, 17 skipped. The 2 failures are `DopplerTests.CheckDotEnvFile` and `DopplerTests.CheckDoppler`, which need a `.env` file the worktree does not have. `AutomationConventionTests` 7 of 7 passed.
- PF-009 added to `Preferences_Test_Plan.md`; PF-001 to PF-008 corrected to match the app.
- Manual: `Preferences.md` tabs now match the app; `Usage_Statistics.md` names the Privacy tab; `Opening_Collaborator.md` describes Show Collaborator.
- Open: Preferences tab images to retake; separate review; PR.
