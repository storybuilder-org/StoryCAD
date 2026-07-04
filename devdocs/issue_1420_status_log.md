# Issue #1420 status log

Session-recovery state for the AutomationProperties annotation pass. Newest entry first.

## 2026-07-04 (Unit 5 merge, Unit 6 open) — PR #1453 merged; #1441 audit trigger tripped; Unit 6 branch cut

**PR #1453 merged to `dev` (1dc0355b) on founder instruction.** Merge cleanup done: #1420 body updated (Unit 5 checked off with the #1452 sunset and gallery-identity notes); #1441 notified that the audit trigger tripped — six main views annotated, running total on `dev` 416 AutomationIds across 16 XAML files (18 in the ratchet; HomePage and TrashCanPage hold nothing interactive), with the four inherited findings listed (TreeItem SizeOfSet, Feelings-combo duplicate names, FolderPage id-only Notes RichEdit until #1452, no full assessment pass yet); wiki `log.md` entry appended (page updates postponed to closeout, wiki tree left uncommitted for StoryCADWiki#1, same posture as Units 3-4). One merge straggler found and fixed: the batch5 status-log ruling entry (975b738b) was pushed 14 seconds after the merge and missed PR #1453; cherry-picked onto this branch (c99ec174).

**Unit 6 is open on branch `issue-1420-batch6-dialogs` off `dev` @ 1dc0355b.** Scope: the eight `StoryCADLib/Services/Dialogs` files — AdminMessagePage (est. 1), BackupNow (2), ElementPicker (5), FeedbackDialog (~7), FileOpenMenu (9), HelpPage (7), NewRelationshipPage (4), SaveAsDialog (2), ~37 est. **The x:Load pre-check is done and clean: none of the eight files contains `x:Load`, so the #1452 literal-Name ban does not constrain this unit.** Same cycle: Sonnet implementer (red against the ratchet, then green), Sonnet reviewer, fixes back to the implementer, PR to `dev`. Naming rule for this unit per the plan: dialog-name id prefixes (Notes/Save/Cancel/Name-class collisions).

## 2026-07-04 (Unit 5, ruling) — x:Load literal-Name ban accepted as temporary; no open decision items on PR #1453

**Founder ruled on the x:Load convention proposal: agreed, but only as long as the Uno bug exists.** Commit 3793eac1 records it accordingly: the convention doc gains the rule as an explicitly temporary bullet (Name vs LabeledBy section, *(added Unit 5)*) that names #1452 as the tracker for the upstream report, the FolderPage Notes Name restore, and the bullet's own deletion; the FolderPage comment now cites #1452 for the same sunset. #1452's task list updated to match (the sunset tasks are explicit; the recording task is checked). Doc and comment only; convention tests 7/7. PR #1453 has no open decision items; only founder review/merge remains.

## 2026-07-04 (Unit 5 session) — Unit 5 PR #1453 filed; x:Load Name bug found and scoped (#1452)

**Unit 5 is in review as PR #1453** after the standard cycle: Sonnet implementer (red 34 → green, commit 077778bb), Sonnet reviewer (request changes: two blockers, one nit), fixes back to the same implementer (commit 4feee9ae). Actual count 34 ids / 18 explicit Names against the plan's ~42 estimate (most Conflict/Flaw/Traits/RelationshipView combos already had Headers). Suite 1,138 / 0 failed, run three times (implementer, reviewer, post-fix); both heads build clean.

**The #1445 "HomePage duplicate Browse" finding was reattributed.** HomePage.xaml holds one Image and nothing interactive; the scan's duplicate pair was the first-run wizard's two header-less BrowseTextBox rows on PreferencesInitialization (both default to ButtonText="Browse"). Fixed there: ProjectDirTextBox/BackupDirTextBox ids plus "Project directory"/"Backup directory" Names; that is the header-less exception to the no-BrowseTextBox-Name ruling. HomePage and TrashCanPage enter the ratchet with zero annotations.

**New Uno bug, isolated and filed as #1452:** a literal `AutomationProperties.Name` on ANY element inside an `x:Load`-deferred subtree fails the net10.0-desktop build: the generator emits a phantom `_{value}Subject` reference (CS1061); WinAppSDK head unaffected; AutomationId unaffected. Evidence chain: "Notes" → `_NotesSubject`, "Zebra" → `_ZebraSubject`, same on a TabViewItem, clean outside the subtree; StoryWorldPage's 43 literal-Name RichEdits build clean because it has no x:Load. The implementer's first root-cause comment (blamed x:Name-less RichEditBoxExtended) was wrong; the reviewer caught the contradiction and the re-isolation produced the narrow rule. Consequence: FolderPage's tabbed Notes RichEdit is id-only, a documented standing exception to the Unit 4 sibling-header ruling until the generator is fixed. **Units 6-9 must check dialog files for x:Load before placing literal Names.** Convention-doc proposal recorded on PR #1453; doc growth stays postponed to closeout.

**Reviewer blockers fixed in 4feee9ae:** ImageGalleryControl's four in-template Names were static (every tile announced identically); tile root Grid and image Button now bind `{x:Bind Caption, Mode=OneWay}`; RelationshipView's `{x:Bind Partner.Name}` gained Mode=OneWay (x:Bind defaults to OneTime). Known adjacent issue left for Unit 7: BrowseTextBox's internal BrowseButton/PathTextBox ids duplicate across all six app-wide instances (comment at BrowseTextBox.xaml:6-12).

**Next: founder reviews/merges PR #1453. On merge, the #1441 audit trigger trips (six main views annotated); notify #1441. Then Unit 6 (StoryCADLib/Services/Dialogs: AdminMessagePage, BackupNow, ElementPicker, FeedbackDialog, FileOpenMenu, HelpPage, NewRelationshipPage, SaveAsDialog, ~37 est.), branch `issue-1420-batch6-dialogs` off `dev`, fresh session, same cycle, x:Load check first.**

## 2026-07-04 (end of session) — Unit 4 merged; five batches remain

**PR #1451 merged to `dev` (5446c8d5) on founder instruction.** #1420 body updated (Unit 4 checked off with its three rulings noted); wiki `log.md` entry appended (page updates postponed to closeout, same posture as Unit 3; wiki tree left uncommitted for the StoryCADWiki#1 workstream). **Founder dropped the per-unit FastPass/Narrator step entirely from Unit 4 on — no scripted axe scan either.** Verification of judgment-risk controls happened in-session by UIA probe instead (composite buttons, Expander naming, literal RichEdit Names, all recorded on the PR).

**Next: Unit 5, branch `issue-1420-batch5-...` off `dev`, fresh session: FolderPage (~5), WebPage (5), PreferencesInitialization (11), HomePage (0 + duplicate Browse-button fix from #1445), TrashCanPage (0), StoryCADLib/Controls: BrowseTextBox (2), Conflict (3), Flaw (2), RelationshipView (6, includes its 1 Expander now that the element list covers Expander), Traits (2), ImageGalleryControl (6). No BrowseTextBox Name needed (Header reaches UIA Name). After Unit 5 merges, the #1441 audit trigger trips (six main views annotated).**

## 2026-07-04 (Unit 4, close) — literal-Name ruling recorded; all PR #1451 decision items closed

**Founder ruled: literal Name stands for the Expander-labeled RichEdit fields; no LabeledBy conversion.** Root cause established first by history: all 43 fields carried `Header` attributes until the #782 Expander layout (af2a93de, 2026-01-21) moved each label into its Expander header, whose FontWeight doubles as a bold-when-filled content indicator; the move silently dropped the framework-derived accessible name, unnoticed because the codebase then had zero AutomationProperties. Commit 4d3317d8 records the rule in the convention doc Name section (*(added Unit 4)* bullet) and cites the ruling in the StoryWorldPage comment block; doc and comment only, convention tests 7/7. PR #1451 now has four commits and zero open decision items; only the founder FastPass/Narrator pass remains before merge.

## 2026-07-04 (Unit 4, later) — Expander ruling implemented; button-name check answered by live probe

**Founder accepted the Expander proposal on PR #1451.** Commit 273b0fc0 (same implementer agent, resumed): `Expander` added to the convention suffix table, the test's InteractiveElementNames, and SuffixByElementName; 44 Expanders annotated (43 StoryWorldPage + BeatSheetExpander on ProblemPage, which is outside its DataTemplate and became coverage-visible when the element list grew). No Names on Expanders; the probe confirmed the header text is framework-derived (GeographyExpander announces "Geography", ControlType Group).

**The composite-content Button theory is disproven, live.** A scratchpad probe (uia_header_probe.ps1 recipe pointed at Land of Oz, Invoke on the "Story World" row, Physical Worlds tab) showed AddWorldButton/RemoveWorldButton with Name='' — WinUI 3 does not aggregate a TextBlock inside StackPanel Content into a Button's UIA Name. Fix in commit 8f8e3f12: explicit Names on the ten Add/Remove buttons, five one-line constraint comments, and a new convention-doc rule (panel-content Buttons get explicit Names). Re-probe against the rebuilt binary: "Add World"/"Remove World" announce correctly; Previous/Next and RichEdit literal Names all surface.

**FlaUI harness facts (#1422):** the new Expander ids work as live ExpandCollapse handles (the probe expanded GeographyExpander by id), and collapsed Expanders keep their RichEdit content out of the UIA tree until expanded — the harness must expand before it types.

**Branch state:** three commits pushed (328148fe, 273b0fc0, 8f8e3f12); StoryWorldPage 123 ids / 63 Names, ProblemPage +1 id; suite 1,138 / 0 failed after each commit; all recorded in PR #1451 comment. Still open for merge: founder FastPass/Narrator on StoryWorldPage, and the RichEdit literal-Name-vs-LabeledBy convention proposal (unruled; shipped pattern verified working by the probe).

## 2026-07-04 (Unit 4 session) — Unit 4 PR #1451 filed; Expander gap needs a founder ruling

**Unit 4 (StoryWorldPage) is in review as PR #1451** after the standard cycle: Sonnet implementer (red 80 → green, commit 328148fe), Sonnet reviewer (request changes: two decision items, code otherwise clean), full suite 1,138 total / 0 failed verified by both implementer and orchestrating session. Actual count is 80 controls against the plan's 123 (stale survey delta). 53 explicit Names: 43 on RichEditBoxExtended fields (none use Header on this page; each is labeled by a sibling Expander.Header, so the implementer mirrored the label text as a literal Name — convention doc is silent on this pattern, proposal recorded on the PR) plus 10 Previous/Next icon-only buttons named from tooltip text.

**Reviewer blocker, needs founder ruling on PR #1451:** Expander is in neither the convention suffix table nor the test's interactive-element list, so the coverage test is structurally blind to it. This page has 43 (scope-wide 45: one more each on ProblemPage and RelationshipView). Options on the PR: add `Expander` to the table/test and annotate in a follow-up commit (ProblemPage's instance comes along since that file is already ratcheted), or document an exemption (the Expander peer already announces header text plus expand/collapse state).

**Second pending check:** the 10 Add/Remove buttons carry AutomationId but no Name on the theory that the visible TextBlock inside their StackPanel content reaches the accessible name via the default peer fallback — documented WinUI behavior, unverified in this app. The StoryWorldPage FastPass settles it; Land of Oz contains a StoryWorld element, so the page is reachable without creating one. If they announce blank, the fix is ten Name attributes.

**No scripted scan this session:** Unit 3's per-page scan driver was scratchpad-local to that session; rebuilding it counts as side tooling under the cost directive, so the per-unit bar rests on the founder FastPass. Founder FastPass/Narrator checks for Units 1-3 also remain outstanding.

**Next: founder reviews/merges PR #1451 with the Expander ruling; then Unit 5 (FolderPage, WebPage, PreferencesInitialization, HomePage, TrashCanPage + StoryCADLib/Controls incl. BrowseTextBox and the HomePage duplicate-Browse fix), branch `issue-1420-batch5-...` off `dev`, fresh session, same cycle.**

## 2026-07-04 (end of session) — Unit 3 merged; six batches remain

**PR #1450 merged to `dev` (efab79e0) on founder instruction.** #1420 body updated (Unit 3 checked off, header-exposure check recorded as done); wiki `log.md` entry appended. The founder FastPass and Narrator checks on ScenePage/SettingPage were not run before merge and remain outstanding, along with the beat-row announcement check (ProblemPage Structure tab). Open spin-offs: #1448 (launch crash), #1449 (dead Cast-tab ListView).

**Next: Unit 4, StoryWorldPage alone (~123 controls), branch `issue-1420-batch4-storyworld` off `dev`, same implementer/reviewer cycle.**

**Founder cost directive (binding for Units 4-9):** spend goes to the planned batches only — no side tooling without asking first. Run each batch in a fresh session (context length drives main-session cost), put the reviewer agent on Sonnet, timebox diagnostics. Estimated cost to finish all six batches at Unit 3's lean batch shape: $60-150 total (token counts × list prices, estimated 2026-07-04).

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
