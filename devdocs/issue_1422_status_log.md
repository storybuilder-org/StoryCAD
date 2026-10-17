# Issue 1422 status log

Session recovery for #1422 (manual test plans as automation scripts, with their manual pages and images). The issue body is the checklist; this file holds build state, test counts and progress only.

## 2026-10-17 (PR 1, Core_Navigation, in progress)

- Branch `issue-1422-core-navigation` from `dev`.
- Runner: menu search removed (menu items open through their XAML owners); lint rejects `menu <id>` for non-toolbar items; menu text paths linted as whole paths; `labels` command; image name standard in `devdocs/automation_naming_convention.md`; new `expect <target> expanded|collapsed`; `tab` and `drag` scroll into view; drag sends real input moves; ComboBox text reads the selected item; `set` reaches an AutoSuggestBox's inner text box; `type` waits 500 ms after typing (#1634); screenshots wait 500 ms.
- App: clearing the search also clears its "Found N matches" message (`ShellViewModel.ClearSearch`), with two unit tests.
- `StoryCADTests/AutomationScripts/Core-Navigation.scs`: all six plan steps pass on Brigid (54.1 s, `--window 1554x1116`). `Core_Navigation.md` step CN-001.3 corrected.
- Images replaced and renamed: `Content-Pane.png`, `Character-Tabs.png`, `Search-Results.png`. `Explorer-and-Navigator-Views.png` stays hand-maintained.
- Tests: Automation 135+ passed; ShellViewModel, OutlineViewModel and Automation together 283 passed, 0 failed.
- Next: apply the manual page comparison (Status_Bar.md Filter Status entry, Search.md, Rearranging_Content.md drop position and narrator root name), full test run, commit, PR.

How to run locally: build Debug x64, move `StoryCAD/bin/x64/Debug/net10.0-windows10.0.22621/win-x64/.env` aside, run `StoryCADAutomation\bin\x64\Debug\net10.0-windows10.0.22621\StoryCADAutomation.exe run StoryCADTests\AutomationScripts\Core-Navigation.scs --window 1554x1116 --out <folder>`, then restore `.env`. Keep hands off the mouse and keyboard during the run.
