# Collaborator #197 status log

Session recovery for Collaborator #197 (AutomationIds on C#-built Collaborator controls). The issue body is the checklist; this file holds build state, test counts and progress only.

## 2026-10-19

StoryCAD branch `collab-197-automation-ids`. `CollaboratorAutomationIds` (StoryCADLib/Collaborator) names the C#-built controls: workflow rows `Workflow{Label}NavItem`, stars `Workflow{Label}StarButton`, groups `WorkflowGroup{Title}NavItem`, `WorkflowOutlineGapsNavItem`, Customize checks `Workflow{Label}Check`, six `WorkflowSettings*` fields. A source test fails when a C# control has no id. The runner lint accepts these id shapes; a test ties the two together.

Terry, 2026-10-19: the runner's `.env` refusal is gone (the seeded preferences already block registration and telemetry). The runner copies the machine's Collaborator sign-in from the installed StoryCAD's preferences into each run. Full suite 1941 tests, 1924 passed, 17 skipped.

Live check on Brigid passed: Hamlet, Collaborator open, every new id found, Premise run through its row, proposals ready within 10 s. Two earlier attempts failed on script mistakes (no `--window`, so Explorer kept focus; a File menu step while the startup file dialog was open). Narrator not checked.

Next: PR into StoryCAD `dev`, then wiki (E1).
