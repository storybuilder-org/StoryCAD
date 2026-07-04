# Issue #1420 status log

Session-recovery state for the AutomationProperties annotation pass. Newest entry first.

## 2026-07-04 (later) — Unit 3 PR #1450 filed; scripted per-page scans running

**Unit 3 (ScenePage + SettingPage) is in review as PR #1450** after the standard cycle: Sonnet implementer (red 54 → green, commit 98e233e1), independent reviewer (approve, no blockers, two nits), full suite 1,138/0 failed verified by both implementer and orchestrating session. Founder steps outstanding: PR review/merge, manual FastPass, Narrator spot-check. Flagged in the PR: the two Conflict-tab Feelings combos announce identically (framework names; convention forbids overriding a Header, so any fix is UI text).

**Per-page scripted scans work now.** A scratchpad driver (uia_header_probe navigation + AxeWindowsCLI 2.4.2, zip-installed to `%LOCALAPPDATA%\AxeWindowsCLI`) scanned Overview, Problem, Character, Scene, Setting in one launch. Result on every page including Units 1-2's: exactly one finding, the same pre-existing Shell-level element — nav-pane TreeItems fail the Section 508 SizeOfSet/PositionInSet rule. Zero missing-name findings on annotated controls anywhere, so the per-unit pass bar holds, and PR #1450's table doubles as the scan counts owed on #1443/#1447. The #1441 audit inherits the TreeItem finding.

**Runtime facts the #1422 FlaUI harness will need** (learned by UIA dump): the nav "tree" renders as TreeItems inside two Tree controls whose AutomationId is `ListControl`; the XAML `NavigationTree`/`TrashTree` ids never surface because ItemsRepeater creates no automation peer (convention-doc implication: container ids on ItemsRepeater are runtime-inert — founder may want a convention note); the root outline row exposes only ExpandCollapse (no Invoke/SelectionItem); child rows expose Invoke/ExpandCollapse/SelectionItem, and **selection does not navigate — Invoke does**; the app lands on OverviewPage after opening a file.

**Filed/recorded today:** #1448 (DeveloperBuild unpackaged-launch crash), #1449 (ScenePage Cast tab dead nested ListView, reviewer find), header-exposure answer on PR #1443, #1420 body checkboxes updated (Unit 3 in review, header check done).

**Gotcha log additions:** Windows PowerShell 5.1 misreads BOM-less non-ASCII as CP1252 and treats the resulting curly quotes as string terminators (tool scripts are now pure ASCII); piping `gh` output through `Set-Content -NoNewline` concatenates lines without separators (mangled the #1420 body once; restored from a /tmp backup); MSYS `/tmp` paths inside heredoc python strings don't resolve for Windows python.

## 2026-07-04 — header-exposure check answered by scripted UIA probe

**The #1443 founder-pending header question is answered: Header text reaches the UIA Name.** Verified against the live app (dev @ 3a143319, WinAppSDK head) with a UIA probe script, on all five controls of interest: StoryIdeaRichEdit → "Story Idea", DateCreated/LastChanged TextBoxes → their headers, and both PreferencesDialog BrowseTextBox inner text boxes → "Project directory:"/"Backup directory:". Decisions this settles: no `RichEditBoxExtended` constructor `SetName` follow-up PR; Unit 5 adds no explicit Name to `BrowseTextBox`; deferred RichEdit Names stay Header-driven where a Header exists, and only header-less controls need explicit names. Answer not yet recorded on #1443 itself.

**New tool:** `devdocs/tools/uia_header_probe.ps1` — launches the exe, opens the Danger Calls sample, navigates to OverviewPage and the Preferences dialog, reads UIA properties. It is the navigation driver `axe_scan.ps1` declared out of scope, and a working proof for the #1422 FlaUI harness (the Unit 1/2 AutomationIds work as live automation handles). Unattended-launch preconditions are documented in its header: seed `Preferences.json` (Initialized, matching Version, ShowStartupDialog false, HideKeyFileWarning true), remove `.env` from the bin, no second instance.

**Findings along the way:**
- **Standalone launch crash:** `AppState.DeveloperBuild` (AppState.cs:78) evaluates `Package.Current`, which throws when the exe runs unpackaged; only a debugger or a missing `.env` short-circuits ahead of it. Any unattended launch with `.env` present dies at Shell_Loaded — this will hit `axe_scan.ps1` runs too. Expression unchanged since fbefa036 (2024-03-16). Needs an issue.
- **Duplicate AutomationId:** both BrowseTextBox instances in PreferencesDialog expose id `PathTextBox` (the `x:Name` inside the UserControl) — same duplicate-fallback-id class as HomePage's Browse buttons already slated for Unit 5.
- The probe's uninitialized first run registered a junk backend user (userId 3015, all consents false).

## 2026-07-03 — design approved, Units 0-2 and axe spike merged

**Where things stand:** `dev` contains Units 0, 1, 2 and the scan script. 204 controls annotated (Shell 74, OverviewPage 25, ProblemPage 43, CharacterPage 62), 55 accessible names, 7 static convention tests ratcheting over 4 of 39 scope files. Full suite 1,136 tests, 0 failed.

**Merged today (all to `dev`):**
- PR #1442 — Unit 0: `devdocs/issue_1420_implementation_plan.md` + `devdocs/automation_naming_convention.md`. Founder design final approval given against these.
- PR #1443 — Unit 1: `AutomationConventionTests` (now 7 tests) + Shell + OverviewPage.
- PR #1444 — founder ruling: Exit menu item's accessible name is "Quit" (id stays `ExitMenuItem`).
- PR #1445 — axe spike: `devdocs/tools/axe_scan.ps1` scripts the accessibility scan (AxeWindowsCLI, exit 0/1/2 pass bar). AxeWindowsCLI is MSI/zip from microsoft/axe-windows releases, NOT a dotnet tool.
- PR #1447 — Unit 2: ProblemPage + CharacterPage, after a request-changes review round.

**Process (works, keep):** per unit — Sonnet 5 implementation agent (red/green against the ratchet test) → independent reviewer agent → fixes back to the implementer → PR. Unit 1 review caught a real test gap (namespaced-attribute blind spot); Unit 2 review caught a silent runtime bug static tests can't see (WinUI 3 ignores bindings in Style Setters — now commented at all four template-binding sites, ruled in the convention doc, and enforced by the `SetterSafety` test).

**Founder-pending items:**
- Header-exposure check (from #1443's checklist, still unrecorded): Inspect a `RichEditBoxExtended` (Overview Story Idea) and a `BrowseTextBox` — does `Header` reach the UIA Name? Decides: `RichEditBoxExtended` two-line constructor `SetName` (small follow-up PR) and `BrowseTextBox` Name placement in Unit 5. All RichEdit Names on Overview/Problem/Character pages stay deferred until this is answered.
- FastPass finding counts for merged views were not recorded on #1443/#1447 (merged with checklists unchecked). `devdocs/tools/axe_scan.ps1` can now supply scripted counts.
- Beat-row announcement check (ProblemPage Structure tab) — the specific failure the Setter bug would have caused.

**Next session:**
1. Unit 3: ScenePage (~38) + SettingPage (~19), branch off `dev`, same agent cycle. Run `axe_scan.ps1` as part of verification (first unit to use it).
2. #1422 (FlaUI harness) is now unblocked per the design (Shell + two element pages annotated and merged).
3. Units 4-9 per the plan table; Unit 5 also fixes HomePage's duplicate "Browse" buttons (x:Name in a DataTemplate duplicates the UIA fallback id — found by the spike's live scan, noted in PR #1445).

**Spin-offs today:** #1441 (accessibility umbrella; audit triggers after Unit 5), #1446 ("Adventureousness" trait misspelling across model/VM/reports; `AdventureousnessCombo` renames with it).

**Gotcha log:** WinUI 3 Setter.Value never evaluates bindings (silent); AxeWindowsCLI not on NuGet; scope is 39 files (the 40 in early docs was derived arithmetic); GitHub auto-deletes merged head branches — a push to one resurrects it (PR #1444 exists because of this).
