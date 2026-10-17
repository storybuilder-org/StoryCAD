# Issue 1422 status log

Session recovery for #1422 (manual test plans as automation scripts, with their manual pages and images). The issue body is the checklist; this file holds build state, test counts and progress only.

## 2026-10-17 (PR 1, Core_Navigation, in progress)

- Branch `issue-1422-core-navigation` from `dev`.
- Runner: menu search removed (menu items open through their XAML owners); lint rejects `menu <id>` for non-toolbar items; menu text paths linted as whole paths; `labels` command; image name standard in `devdocs/automation_naming_convention.md`; new `expect <target> expanded|collapsed`; `tab` and `drag` scroll into view; drag sends real input moves; ComboBox text reads the selected item; `set` reaches an AutoSuggestBox's inner text box; `type` waits 500 ms after typing (#1634); screenshots wait 500 ms.
- App: clearing the search also clears its "Found N matches" message (`ShellViewModel.ClearSearch`), with two unit tests.
- `StoryCADTests/AutomationScripts/Core-Navigation.scs`: all six plan steps pass on Brigid (54.1 s, `--window 1554x1116`). `Core_Navigation.md` step CN-001.3 corrected.
- Images replaced and renamed: `Content-Pane.png`, `Character-Tabs.png`, `Search-Results.png`. `Explorer-and-Navigator-Views.png` stays hand-maintained.
- Tests: Automation 135+ passed; ShellViewModel, OutlineViewModel and Automation together 283 passed, 0 failed.
- Full WinAppSDK suite at `c9c14093`: 1910 tests, 1893 passed, 17 skipped, 0 failed.
- Parked 2026-10-17 for Collaborator work. A read-only agent was comparing the seven manual pages with the code; rerun that comparison on resume if its result is gone.
- Manual page comparison finished (label check: 8 pages, 0 misses). Fixes to apply:
  - Navigation_and_Content_Panes.md: narrow window shows the Navigation Pane full width (not stacked); add StoryWorld to the element list; trash node is "Deleted Story Elements"; "Most forms" have a Notes tab; "Content Panel" typo.
  - Tabs.md: tab names are all one size; Character's tabs overflow at normal width and scroll with the arrows.
  - Status_Bar.md: delete Filter Status; add AutoSave and Backup indicators; name the pencil icon.
  - Search.md: clearing is changing the text or clicking X; search covers every text field, any case.
  - Rearranging_Content.md: narrator root is "Narrative View"; Old Man and the Sea example is wrong (it uses folders); template drop-down is under File > Open/Create file > Create new outline; a dropped node becomes the last child; "parent node", not "parent folder"; Hamlet's acts are Folders, not Sections.
  - Move_Story_Elements.md: Narrative View cannot move either.
  - Clean: Navigating_in_StoryCAD.md, Story_Explorer_and_Story_Narrator_Views.md.
- Manual fixes applied to the six pages; `labels` reports no misses on them.
- Next: open the PR for Core_Navigation.

## 2026-10-17 (PR 2 branch started)

- PR #1635 (Core_Navigation) open, CI green, ready for review.
- Branch `issue-1422-core-file-operations` from PR 1's branch. Added from the PR 1 evaluation: `--keep-going` (a failure skips the rest of its step) and `--window` as the visible frame in 100%-scale pixels. Automation tests 139 passed. Not yet run live: the next live run checks `--window 1036x744`.

How to run locally: build Debug x64, move `StoryCAD/bin/x64/Debug/net10.0-windows10.0.22621/win-x64/.env` aside, run `StoryCADAutomation\bin\x64\Debug\net10.0-windows10.0.22621\StoryCADAutomation.exe run StoryCADTests\AutomationScripts\Core-Navigation.scs --window 1036x744 --out <folder>`, then restore `.env`. Keep hands off the mouse and keyboard during the run.
