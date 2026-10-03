# Handoff: StoryCAD #1421 build work (Claude Opus on Brigid)

- **Issue:** https://github.com/storybuilder-org/StoryCAD/issues/1421 (UI automation DSL and standalone runner over FlaUI; the smoke test is the first script)
- **Branch:** `issue-1421-ui-automation-dsl` (no PR)
- **From:** Shipping Sentinel (the Linux agent working for Terry Cox), 2026-10-03
- **To:** Claude Opus in Claude Code, PowerShell, on Terry's Windows 11 PC **Brigid**
- **Approved by Terry**, 2026-10-03

Read this file first, then the issue body and comments, then `devdocs/issue_1421_dsl_design.md` and `CLAUDE.md` / `AGENTS.md`.

---

## 1. Current state

| | |
|---|---|
| Branch head at handoff | merge commit `6488b458` ("chore: merge dev into issue-1421-ui-automation-dsl (#1421)"), followed by the commit that adds this file |
| Position vs `dev` | **0 behind, 8 ahead** at `6488b458`: the 7 branch commits plus the merge. `dev` was merged in, not rebased. |
| Issue status | Unparked 2026-10-03 (4.3 shipped 2026-10-02). Label `active`. Org project 13 (Release 4.4) Status = **In progress** |
| Code tasks done | 1. scaffold. 2. `StoryCADXamlScan` shared scan. 3. driver layer (`327d162e`). All three reviewed. |
| Code tasks written but with review findings open | 4. parser + `check` lint. 5. interpreter (`1b1f549f`). The review found 1 blocker and 7 majors (section 2). |
| Code tasks not started | 6. runner, 7. presentation profile, 8. smoke script, 9. CI job |

**Merge conflicts, both resolved mechanically:**
- `Directory.Packages.props`: took `dev`'s side, which drops the Tmds.DBus.Protocol pin. Kept the FlaUI.Core / FlaUI.UIA3 5.0.0 pins and their comment.
- `StoryCADTests/StoryCADTests.csproj`: kept both new project references (`CollaboratorLib` from `dev`, `StoryCADXamlScan` from the branch).

**Built and tested on Linux (.NET SDK 10.0.112):**
- `StoryCADXamlScan` (net10.0) builds with 0 warnings.
- `StoryCADAutomation` (net10.0-windows10.0.22621 via `EnableWindowsTargeting`) **compiles** with 0 warnings. That proves compilation only.
- `StoryCADTests` builds for the `net10.0-desktop` target. `AutomationConventionTests` pass **7/7** against the merged XAML (604 AutomationIds).
- No parser or linter unit tests exist yet. A scratch harness outside the repo ran the design's ST-002 sample, which parses and lints clean except the one expected `name:` warning.
- Uno.Sdk 6.5.31 → 6.6.42 keeps the same WinAppSDK (1.7.250909003). The Windows UIA surface should therefore be unchanged, but that is an inference, not a test result.

**Still needs Windows (Brigid):**
- A full solution build for both app versions (the WinAppSDK head and the desktop head).
- The full MSTest run.
- `AutomationConventionTests` on the WinAppSDK target.
- Every live FlaUI run: driver, interpreter, runner, smoke script.

Merge record: https://github.com/storybuilder-org/StoryCAD/issues/1421#issuecomment-5970035142

---

## 2. Work order

### Phase A: fix the review findings on `1b1f549f` (do this before task 6)

Review summary on the issue: https://github.com/storybuilder-org/StoryCAD/issues/1421#issuecomment-5970040518
The full report has 1 blocker, 7 majors, 8 minors and 6 nits. The line refs below are valid at `6488b458`, because the merge did not touch `StoryCADAutomation/` or `StoryCADXamlScan/`. Fix the items in this order, one commit per finding or small group. Put the finding id in each commit message, e.g. `fix: enforce scratch containment at run time (#1421 review B1)`.

1. **B1 (blocker): `{scratch}` containment is enforced only by lint.**
   - `ScriptInterpreter.cs:58-63`: `Execute` refuses parse errors only, though its message says lint also gates execution.
   - `ScriptInterpreter.cs:366-374`: `ResolveScratchPath` never checks that the result stays under `driver.ScratchDirectory`.
   - Fix: after `Path.GetFullPath`, require the path to start with the full scratch path plus a separator (case-insensitive), and otherwise throw `AutomationStepException`. Have `Execute` run, or require, the scratch rule. In `ScriptLinter.cs:133-150`, also reject a second `{scratch}` and any `:` after the root.
2. **M7: settle the execution-profile interface before task 6.**
   - `IExecutionProfile.cs:13-31` covers only `Click`, `ScalePause` and `Narrate`.
   - `ScriptInterpreter.cs:178-232` calls the driver directly for `double-click`, `right-click`, `drag`, `open-node`, `expand`/`collapse`, `menu`, `context-menu`, `tab`, `select` and `set`.
   - The design's presentation column says "real pointer, eased, always", so route every input verb through the profile, or give the driver a realization mode that the profile sets.
   - If the right shape is unclear, stop and raise decision `D-PRESENTATION` (section 6) instead of guessing.
3. **M6: `--keep-going` resumes at the next statement, not the next step.**
   - `ScriptInterpreter.cs:94-98` and `:119-122`.
   - Design line 153 says "remaining steps". On failure, skip to the next `Step` statement.
4. **M4: name lookups take the first match anywhere in the app.**
   - `ElementLocator.cs:238-240` is the first-match search.
   - `ScriptInterpreter.cs:248-254`: the `dialog` verb throws away the dialog it waited for, then clicks a match from the whole app. `tab "text"` at `:229-232` has the same problem.
   - Fix: have `WaitForWindowTitle` return a handle and search for the target within it. Scope tab text to the TabView and the TabItem control type. Keep the id-existence lint for dialog targets that are ids (`ScriptLinter.cs:85-92`).
5. **M2: menu-leaf probing can overrun its deadline and then allow 1 ms for readiness.**
   - `ElementLocator.cs:651-699`: the deadline is checked only after a full pass (`:684`). The budget is 500 ms per flyout (`:640`). `Remaining()` returns 1 ms (`:695-699`).
   - Fix: check the deadline inside the probe loops, and give a found leaf a minimum readiness budget (e.g. at least 1 s).
6. **M3: the menu/tab text-path lint can pass paths that fail at run time.**
   - `XamlUiFacts.cs:53-65` and `AutomationXamlScan.cs:193-196` build one flat label set. `ScriptLinter.cs:103-116` checks segments without order.
   - Example: `menu "File/Exit"` lints clean, but the Windows UIA Name is "Quit" (`Shell.xaml:266-268`).
   - Fix: per element, compute the Windows runtime name (explicit `AutomationProperties.Name`, else the normalized `win:Text`/`Text`/`Label`), and lint the path as a parent→child chain.
7. **M5: `expect … text` is a single exact read.**
   - `ScriptInterpreter.cs:279-289`. `Snapshot` reads only the Value and Text patterns (`ElementLocator.cs:292-313`).
   - Fix: poll until the text matches or the timeout passes, normalize trailing whitespace and CR, and fall back to UIA Name. This matters for the ST-002 status-text assertion (section 3, step 5).
8. **M1: `open-file-dialog` uses the Save dialog's filename id.**
   - `StoryCADDriver.cs:596-621` uses `"1001"` for both dialogs. The Open dialog's filename box is normally `1148`.
   - Fix: find the dialog window first, then search inside it for `1001` or `1148`. **Confirm both ids live on Brigid.** ST-004 depends on this.

The 8 minors and 6 nits are in the full report. Fix them when convenient. Two of them feed planning:
- m3: statements before the first `step` have no test case.
- m7: the parser and linter sit in a Windows-only (net10.0-windows) project, which limits where unit tests can live (input for the Test-section plan).

Shipping Sentinel can paste the full report into a comment if you need it.

### Phase B: tasks 6-9 (text from the issue body)

- [ ] **Task 6 — Runner:**
  - CLI: `run` / `check`, with options per the design doc and exit codes 0-3.
  - Environment prep: scratch-copy launch (Terry's decision 1, 2026-09-30), scrubbed environment, refuse to run if a `.env` exists or StoryCAD is already running.
  - Failure policy: abort on first failure, diagnostics on any step failure, retry only exit-3 launch failures, and only once.
  - Reports: JUnit XML, human log, `timeline.json`, per-failure diagnostics, collected NLog files.
  - **`run` must run the lint, or at least the B1 scratch rule, before launching.**
  - Also amend the design doc's Runner section for scratch-copy launch. That was pickup item 3 in the parking comment.
- [ ] **Task 7 — Presentation profile:** real pointer with eased movement, pacing factor, `narrate` caption overlay, fixed window size and position. Timing depends on decision `D-PRESENTATION`.
- [ ] **Task 8 — Smoke script:**
  - Translate `StoryCADTests/ManualTests/Smoke_Test.md` to the first `.scs` script.
  - Pin the File-menu new-outline steps against the live app.
  - Replace the ST-002 `expect-no name:"Error"` check with the status-text assertion (section 3, step 5).
  - **Gate:** 3 consecutive clean local runs in the test profile.
- [ ] **Task 9 — CI:**
  - A per-PR job on `windows-latest` that runs `check` over all scripts and runs the smoke script in the test profile.
  - Assert resolution and DPI at launch.
  - Document the flakiness budget in the workflow file.
  - The workflow file rides in the eventual PR and is never run by hand. Required vs advisory is decision `D-CI-CHECK`.

---

## 3. Windows verification steps (Brigid, PowerShell)

Run them from the repo root on the branch. Record the commands and results in your "Claude status" comments.

1. **Full solution build, both versions of the app:**
   ```powershell
   dotnet restore StoryCAD.sln
   dotnet build StoryCAD.sln -c Debug -p:Platform=x64
   ```
   If `dotnet build` hits the known Uno / WinAppSDK task issues, use VS 2026 MSBuild as listed in CLAUDE.md:
   `& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" StoryCAD.sln -t:Build -p:Configuration=Debug -p:Platform=x64`
   Both the `net10.0-windows10.0.22621` and `net10.0-desktop` targets must build. `StoryCADAutomation` and `StoryCADXamlScan` must build with 0 warnings.
2. **AutomationConventionTests on the WinAppSDK target**, then the full suite:
   ```powershell
   .\StoryCADTests\bin\x64\Debug\net10.0-windows10.0.22621\StoryCADTests.exe --filter "FullyQualifiedName~AutomationConventionTests"
   ```
   Expect 7/7. Then run the full MSTest suite on both targets (`dotnet test ...` per AGENTS.md, or vstest per CLAUDE.md), and report the pass/fail counts plus any failures that are not related to this branch.
3. **Runner stub sanity check, until task 6 replaces it:**
   `.\StoryCADAutomation\bin\x64\Debug\net10.0-windows10.0.22621\StoryCADAutomation.exe check x` must exit 64.
4. **Live smoke-script run** (after tasks 6 and 8):
   - The desktop must be unlocked and interactive, and nobody else may be using Brigid. Display at 100% DPI.
   - Close any running StoryCAD first, and make sure no `.env` sits in the app bin folder.
   - Run `StoryCADAutomation run <smoke>.scs --profile test` against the unpackaged Debug build.
   - Gate: **3 consecutive clean runs**. Report each run's exit code, duration, and the scratch-copy time; Terry asked for that time to be measured on the first run.
   - Attach or summarize the JUnit XML and diagnostics for any failure.
5. **Status-text AutomationId in `StoryCAD/Views/Shell.xaml`** (about line 674, the `TextBlock` bound to `ShellVm.StatusMessage`; Terry's decision 3):
   - Add an AutomationId so ST-002 can assert the save result. The suggested id is `StatusMessageText`.
   - The naming convention (`devdocs/automation_naming_convention.md`) has no TextBlock suffix row, and it says not to annotate content TextBlocks. Treat this as the approved exception: propose the `Text` suffix in the PR description and add a convention-doc note.
   - Keep `AutomationConventionTests` green.
   - Verify live that the TextBlock exposes its text through UIA. With the M5 fix, `expect` falls back to Name.
   - Background: a failed save shows "Save File As failed" (`OutlineViewModel.cs:505`). The assertion must tell success from that message.

---

## 4. Rules

- Work **only** on `issue-1421-ui-automation-dsl`. Pull before starting each session: `git pull --ff-only`.
- **Never** merge into `dev` or `main`. Terry merges.
- **No force-push and no rebase.** Commits on this branch are already pushed. If you need `dev` again, **merge** `origin/dev` into the branch, and only after asking on the issue.
- **No manual workflow runs** (`gh workflow run` or re-running jobs). CI runs only when Terry opens or approves a PR.
- **Do not open a ready-for-review PR.** A draft PR is fine only if Terry asks for one.
- Follow `CLAUDE.md` and `AGENTS.md`:
  - conventional, imperative commit subjects that reference #1421;
  - `dotnet format` before pushing;
  - test naming `MethodName_Scenario_ExpectedResult`;
  - `.editorconfig` (CRLF, 4-space indentation);
  - no secrets, no `.env` commits.
- Commit identity: Terry Cox, `5607778+terrycox@users.noreply.github.com`, plus a `Co-Authored-By` trailer for yourself, matching the branch history.
- Implementer and reviewer stay separate. Shipping Sentinel reviews your pushed commits; don't mark your own work reviewed.
- Don't change the shipped app beyond what a task needs. The status-text AutomationId is the one approved product XAML change so far.
- Never point a file dialog at anything outside `{scratch}`, and never run the automation against Terry's real outlines.

---

## 5. Coordination with Shipping Sentinel

After each task, or at the end of each session if it ends sooner:
1. **Push** your commits to `issue-1421-ui-automation-dsl` (normal push).
2. **Post an issue comment** on #1421 whose first line is `## Claude status` followed by the date. Include:
   - **Changed:** commits (SHA + subject) and the finding or task ids they address.
   - **Windows test results:** exact commands, pass/fail counts, live-run exit codes and durations.
   - **Next:** what you will do next.
   - **Blockers:** anything that stops you, or "none".
3. **Update the checklist** in `devdocs/issue_1421_handoff.md` (section 7) in the same push.

When you are blocked or need a decision, **stop** and post a comment that starts with `## Decision needed: <label>`. Use the labels in section 6, or a new `D-<SHORT-NAME>` label for a new decision. State the options and your recommendation, then wait. Don't pick an option yourself on anything listed in section 6.

Shipping Sentinel reviews each pushed commit and replies on the issue, with findings ranked blocker / major / minor / nit and file:line refs. Address blockers and majors before moving to the next task.

---

## 6. Open decisions for Terry

| Label | Decision | Notes |
|---|---|---|
| `D-PRESENTATION` | Presentation profile (task 7) in the first PR, or in a later one? | A later PR is smaller and reaches the smoke gate sooner. Either way, M7 must settle the interface before task 6. |
| `D-CI-CHECK` | Should the task-9 CI check be **required** or **advisory** on PRs to `dev`/`main`? | Advisory until hosted-runner flake is measured avoids blocking 4.4 PRs. The flake budget and a self-hosted fallback come back to the issue. |
| `D-TEST-EVAL-PLAN` | Approval of the Test-section and Evaluate-section plans, both still unplanned in the issue body | Covers parser/linter unit tests and where they live (minor m7), coverage targets, the wiki/log, and the flake-observation window. |
| `D-SCREEN-LOCK` | Screen lock on Brigid during unattended live runs (Terry's travel: about Oct 7-14 and Oct 26-Nov 9) | UIA input needs an unlocked interactive desktop. Without a decision, live runs (task 8 gate, step 4) happen only while Terry is at Brigid. |

---

## 7. Checklist (Claude updates this on each push)

**Phase A: review fixes**
- [ ] B1 scratch containment enforced at run time
- [ ] M7 execution profile covers all input verbs (or `D-PRESENTATION` raised)
- [ ] M6 keep-going skips to the next step
- [ ] M4 dialog/tab name lookups scoped
- [ ] M2 menu probe deadline and readiness budget
- [ ] M3 text-path lint uses runtime names and parent→child order
- [ ] M5 `expect text` polls, normalizes, falls back to Name
- [ ] M1 Open-dialog filename id (confirmed live)

**Windows verification**
- [ ] Full solution build, both targets
- [ ] AutomationConventionTests 7/7 on WinAppSDK; full MSTest counts recorded
- [ ] Runner stub check (exit 64) before task 6

**Phase B: tasks**
- [ ] Task 6 runner (plus design doc Runner amendment)
- [ ] Status-text AutomationId in Shell.xaml (convention tests green)
- [ ] Task 8 smoke script; 3 consecutive clean runs; scratch-copy time measured
- [ ] Task 7 presentation profile (per `D-PRESENTATION`)
- [ ] Task 9 CI job (per `D-CI-CHECK`; not run by hand)

**Decisions**
- [ ] `D-PRESENTATION` [ ] `D-CI-CHECK` [ ] `D-TEST-EVAL-PLAN` [ ] `D-SCREEN-LOCK`
