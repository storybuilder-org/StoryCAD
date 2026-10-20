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
- Image standard (Terry, 2026-10-17): 1036 x 744 frame at 100% scale, no "(DEV BUILD)" in the title; the runner sets `STORYCAD_PLAIN_TITLE=1` (`3d0de6f7`). Rebuild `StoryCAD` before the next live run so the app reads it.
- Paused for a reboot to turn on Hyper-V. After the reboot: create the automation VM (Terry supplies the Windows 11 ISO and license, and clicks through setup), then run `Core-Navigation.scs` there with `--window 1036x744` to check both new options, then start translating Core_File_Operations.md.

## 2026-10-18 (automation VM)

- Hyper-V VM `StoryCAD-Automation` on Brigid, stored in `D:\VMs`: Generation 2, 16 GB fixed memory, 4 processors, 128 GB disk, Default Switch, TPM on. Windows 11 Pro build 26300, not activated.
- Installed unattended from `D:\VMs\ISO\answer\autounattend.xml` (packed as `answer.iso`); local admin `storycad` signs in automatically. Host reaches it with PowerShell Direct (`Invoke-Command -VMName StoryCAD-Automation`).
- VM display fixed at 1920x1080, 100% scale (`Set-VMVideo -ResolutionType Single`); boots from its disk.
- `Core-Navigation.scs` passed in the VM in 26.4 s (54.1 s on Brigid), with `--window 1036x744 --keep-going`. The three screenshots are 1036 x 744 with no "(DEV BUILD)" in the title; they replace the 1536 x 1107 images from PR 1. No step failed, so `--keep-going` has not yet resumed after a failure live.
- VM layout: `C:\Automation\storycad` (the self-contained `win-x64` build, without `.env`), `C:\Automation\repo` (the repo's `.xaml` files and `StoryCAD.sln`, which the runner's XAML scan needs) with the runner, published self-contained, in `repo\runner`. The run goes through a scheduled task in the signed-in session, because PowerShell Direct has no desktop.
- Next: translate Core_File_Operations.md.

## 2026-10-18 (PR 2, Core_File_Operations)

- `StoryCADTests/AutomationScripts/Core-File-Operations.scs`: all six plan steps pass in the VM (35.8 s, `--window 1036x744`). Two runs; the first failure (an off-screen tree row) let `--keep-going` resume at the next step live. No new verbs.
- `Core_File_Operations.md` corrected: no New Story command (Ctrl+N opens the Narrative Editor); a new outline is written when created, so Save shows no dialog; the changed indicator is the red pencil, not "Changed" text; "Save changes?" has Yes and No only; menu paths updated.
- Manual: `Create-New-Outline-Dialog.png` replaced (old image showed the 3.x dialog). Page comparison (agent) found five mismatches, all fixed: Reading_and_Writing_Outlines.md (menu item names, Save As dialog, open saves silently, duplicate samples paragraph), Starting_and_Stopping_StoryCAD.md (single instance is Windows only), Key_Binds.md (Alt+A needs the Narrator view; macOS swaps the two report keys). Label check: no app-label misses.
- "Save changes?" with no Cancel, and opening another outline saving without asking: Terry ruled these defects; filed as #1636 (fix based on `dev`), which lists the #1422 script, plan, page and image updates to make after the fix.
- New `screenshot "Name.png" crop x,y,w,h` (Terry, 2026-10-18: keep close-up images): 100%-scale pixels from the window's top-left corner. Parser and `CropArea` unit tests (8 cases). `File-Menu.png` (was missing Create Backup) and `Status-Bar.png` (was missing the AutoSave and Backup icons) now come from the script. Final run: passed in 37.7 s.
- PR #1637 opened against `dev` (branch rebased onto `dev` after PR 1's merge); CI green; ready for Terry's review.

### Session recovery (2026-10-18, paused for Terry's demo)

- Waiting on Terry: review of PR #1637.
- After #1637 merges: merge-cleanup (tick C2 on #1422, `log.md` merge entry, wiki page for the crop option), then C3, Core_Story_Elements, on a new branch from `dev`.
- Separate: #1636 (Save changes dialog), fix based on `dev`; its body lists the #1422 follow-ups.
- VM runs: stage the build to `D:\VMs\stage` (StoryCAD `win-x64` without `.env`; runner published self-contained into the VM's `C:\Automation\repo\runner`), set the script name in `C:\Automation\run.cmd`, start it as a scheduled task in the signed-in session, copy `C:\Automation\out` back.

How to run locally: build Debug x64, move `StoryCAD/bin/x64/Debug/net10.0-windows10.0.22621/win-x64/.env` aside, run `StoryCADAutomation\bin\x64\Debug\net10.0-windows10.0.22621\StoryCADAutomation.exe run StoryCADTests\AutomationScripts\Core-Navigation.scs --window 1036x744 --out <folder>`, then restore `.env`. Keep hands off the mouse and keyboard during the run.

## 2026-10-18 (PR 3 branch started, Core_Story_Elements)

- Branch `issue-1422-core-story-elements` from `dev` at `8abc5c54` (after PR #1637 merged).
- New `screenshot "Name.png" of <target> [<target> ...] [margin N]`: the image covers the named elements plus a margin in 100%-scale pixels, clipped to the window (Terry, 2026-10-18: close-ups by element name instead of pixel numbers). `Core-File-Operations.scs` uses it for `Status-Bar.png` and `File-Menu.png`. Automation tests 158 passed; lint clean.
- VM runs are one command: `D:\VMs\Run-InVM.ps1 -Repo <worktree> -Script <name>.scs` (elevated PowerShell) stages the Debug build, runner and XAML, runs in the VM, and copies results to `D:\VMs\out\<script>`. The script lives on Brigid, not in the repo. First run: Core_File_Operations passed, 42.1 s in the VM, 97 s end to end.
- Open: the new `Status-Bar.png` takes a thin strip of the page above the status bar (margin 8 reaches past its top edge). Fix the margin, retake both images into `docs/media`, then start the Core_Story_Elements script.

### Session recovery (2026-10-19)

- Merged since this branch started: #1639 (PR #1643), #1641 (PR #1642), Collaborator #197 (PR #1646, `d0b427cf`). Rebase this branch on `dev` before more work.
- From #197: the runner no longer refuses a `.env` in the app folder, so local runs no longer need `.env` moved aside. Use `--window 1036x744` on every local run (it gives StoryCAD the focus).
- Next on this branch: fix the `Status-Bar.png` margin (a strip of the page above shows), retake `Status-Bar.png` and `File-Menu.png` into `docs/media`, then script Core_Story_Elements (C3).

## 2026-10-19 (PR 3, Core_Story_Elements)

- Branch already on `dev` at `d0b427cf`; no rebase needed. The worktree needed a restore for Uno.Sdk 6.7.30 before it built.
- `Status-Bar.png`: margin 0 (the status bar buttons fill the bar's height, so margin 8 reached into the page). `Status-Bar.png` and `File-Menu.png` retaken in the VM (`d4deefea`).
- `D:\VMs\stage` held `runner` and `runner-build` folders from the 2026-10-18 hand runs; they broke `Run-InVM.ps1` and were deleted.
- `StoryCADTests/AutomationScripts/Core-Story-Elements.scs`: all six plan steps pass in the VM, three runs in a row (44.3 to 44.5 s). No new verbs.
- Script facts: Locale is an editable drop-down that refuses a direct value write, so the script focuses it and types; Notes use `FolderNotesTabRichEdit`; the multi-line Notes text has no text check because a script string cannot hold a line break; the right-click flyout stays open (an empty box) after an item runs through UI Automation and catches the next pointer input; it failed CI `ui-smoke` on PR #1650. Runner fix: `context-menu` ends with `CloseContextFlyout`, which presses Escape unless the item opened a dialog. Smoke_Test, Core-Story-Elements and Core-Navigation each passed three VM runs with it; automation unit tests 166 passed. The second CI run failed `ui-smoke` at ST-005 for another reason: the open-file flow ends by selecting the root node (`FileOpenVM.NavigateToOverview`), and the CI app log shows it landing after the script had opened Test Character. Smoke_Test and Core-Story-Elements now wait for the Overview title box after each open; six VM runs passed.
- `Core_Story_Elements.md` corrected: setup creates the outline by name; menu paths are Add Elements > Add ...; "Major Role", "Person vs. Person", "New Note"; deleting an unreferenced element shows no dialog; restore is Restore Element on the node under Deleted Story Elements.
- Images: `CharRoleTab.png` and `CharPhysTab.png` renamed to `Character-Role-Tab.png` and `Character-Physical-Tab.png` (links fixed); `Problem-Problem-Tab.png` and `Scene-Scene-Tab.png` retaken. `Setting-Setting-Tab.png` retaken. Its Locale box is blank: an editable drop-down shows a saved value only when the value is in its list (Hamlet stores "Castle"). Terry, 2026-10-19: not an app defect.
- Label check on the five pages: 0 misses. Page comparison (agent) found mismatches on all five, all fixed: Role_Tab.md (six story roles, Ally and Higher Self archetypes), Physical_Tab.md (heading, field list, Appearance is its own tab), Problem_Tab.md ("Discover", "Person vs. Nature" with its own text, "Person vs. Self", Problem Category added, "Problem Source"), Scene_Tab.md (Date and Time are two fields, Viewpoint Character, SceneType added, Cast is its own tab), Setting_Tab.md (Setting Summary added, broken Props sentence finished).

### Session recovery (2026-10-20, before a restart)

- PR #1650 merged into `dev` as `9f80a06b`; merge-cleanup done (C3 ticked, wiki `log.md` entry, sync-state at `9f80a06b`).
- Next: C4, Window_Management (`StoryCADTests/ManualTests/Window_Management.md`), on branch `issue-1422-window-management` from `9f80a06b`, in the worktree `D:\dev\src\StoryCAD-1422`. Nothing done on it yet.
- Per plan: translate to a `.scs` script with `--window 1036x744`, run in the VM, retake the manual images it covers under the image name standard, run `labels` on the pages, have an agent compare the pages with the code, fix plan and pages, one PR.
- VM runs need an elevated session: `D:\VMs\Run-InVM.ps1 -Repo D:\dev\src\StoryCAD-1422 -Script <name>.scs`; results in `D:\VMs\out\<script>`. Build Debug x64 in the worktree first; after a `dev` package change, build with `-t:Restore,Build`.
- Script facts: wait for `TitleTextBox` after opening a file before opening a node; right-click menus close themselves now (`CloseContextFlyout`); a script string cannot hold a line break.
- The worktree has no `.env`, so `CheckDotEnvFile` and `CheckDoppler` fail in a local full-suite run there.
