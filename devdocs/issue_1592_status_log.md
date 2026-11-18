# Issue #1592 status log

## 2026-11-11

**Branch:** `issue-1592-collaborator-manual-link`, from `dev` at `235d13ce`.

**Done:** `HelpPageViewModel.CollaboratorUri` (`docs/StoryCAD%20Collaborator/` on the manual base address, beta or production); `HelpPage.xaml` entry "StoryCAD Collaborator" after "User manual", AutomationId `HelpCollaboratorButton`.

**Tests:** `HelpPageViewModelTests`, two tests (production and beta manual), each seen failing first. Full StoryCADTests: 2314 tests, 2297 passed, 17 skipped, 0 failed. Both heads build.

**Next:** manual check on Windows: the startup dialog shows the entry after "User manual" and it opens the Collaborator section.
