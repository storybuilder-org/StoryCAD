# StoryCAD Automation DSL, Driver, and Runner: Design

Issue #1421. Decision context: issue body "Architecture decision (2026-07-12)" section. Status: draft for design review.

**Build status (2026-10-04).** Milestone 1 built and ran live: the driver, the parser, lint, the interpreter in the test profile, and `run <script>` with console output and exit codes 0-3. The app's data root comes from the `STORYCAD_ROOT_DIR` environment variable, not a copy of the build. `StoryCADTests/ManualTests/Smoke_Test.scs` passes. Milestone 2 (in progress) adds `check`, `--ci`, `--out`, the `screenshot` verb and a CI job. Still deferred: the presentation profile, report files, `--keep-going`, launch retry and folder runs for `run`. Sections below that describe deferred parts are not built.

## Components

One new console project, `StoryCADAutomation`, target `net10.0-windows10.0.22621` (FlaUI wraps UIA3 and is Windows-only; the DSL itself is backend-neutral, see "macOS seam" below). Three layers inside it:

| Layer | Contents | Depends on |
|---|---|---|
| Driver | Launch/teardown, element location, waits, pointer/keyboard primitives, the #1420 runtime facts | FlaUI.Core, FlaUI.UIA3 |
| Interpreter | Script parser, verb dispatch table, one handler per verb calling driver methods | Driver |
| Runner | CLI, execution profiles, environment prep, report writers | Interpreter |

No MSTest reference. The runner is the executable; CI invokes it directly.

**Dependencies:** FlaUI.Core and FlaUI.UIA3, dev/CI-only (never shipped to users), MIT-licensed, slow-moving upstream. Both get pinned in `Directory.Packages.props` with a provenance comment in the house style (the Tmds.DBus.Protocol pin is the template).

**Input containment:** element location is rooted at the launched process's windows (plus its owned native dialogs), never the whole desktop, and every real-pointer action verifies the app window is foreground first, failing the step otherwise. Real input lands on whatever owns the pixels; these two rules keep that from being someone else's window.

## Script language

Line-oriented plain text, UTF-8, extension `.scs` (proposed; trivially changeable at review). One statement per line: a verb followed by arguments. Double quotes delimit strings; `#` starts a comment; blank lines are ignored.

**No variables, no control flow, no includes in v1.** Manual test plans are linear procedures; scripts mirror them one step per line. This is what makes an AI-assisted translation reviewable: a reviewer reads plan step and script line side by side. Data-driven repetition, if ever needed, is runner parameterization, not script logic.

Two structural verbs exist for reporting, not execution:

```
script "Smoke Test"              # once, first non-comment line; names the run
step "ST-002: Create and Save"   # opens a step; every step becomes a report testcase
```

## Element addressing

1. **AutomationId, bare.** `click SaveAsCancelButton`. The 591-id inventory from #1420 (`devdocs/automation_naming_convention.md`) is the namespace. This is the default and covers everything outside tree rows and native dialogs.
2. **Tree path.** `tree "Hamlet/Problems/Hamlet vs. Claudius"` addresses a TreeItem by the node-name path from the outline root. Rationale: the XAML `NavigationTree`/`TrashTree` ids never surface (ItemsRepeater creates no automation peer; rows live under two Tree controls with id `ListControl`), and bare names are ambiguous (the Hamlet sample has two nodes named Hamlet). A path is unambiguous unless siblings share a name; then an index suffix disambiguates: `"Hamlet/Characters/Hamlet[2]"`.
3. **Name fallback.** `name:"Don't Save"` locates by UIA Name. Needed for native file dialogs and system buttons that carry no AutomationId. Discouraged elsewhere; the script linter (below) flags `name:` uses outside dialog scopes.

## Verb set v1

Grounded in what `Smoke_Test.md` and the four tier-1 plans actually do. There are no bare sleeps in test mode; the implicit wait below is the flakiness budget's home.

**Implicit wait = readiness, not existence.** Every locating verb waits (default 5 s) until the target is found AND enabled AND on-screen AND exposes the pattern the verb needs: `click` waits for Invoke availability (or a clickable point when the realization is real pointer), `set` for Value plus focusability, `select` for SelectionItem, `expand`/`collapse` for ExpandCollapse. Existence alone is not readiness: an element can sit in the UIA tree disabled, off-screen, or pattern-less, and the known flake classes here are readiness classes (the 2026-06-12 navigation case; collapsed Expanders hiding RichEdit content). `expect <target> enabled|disabled` differs from the implicit wait: it asserts the state once the element is found instead of waiting for usability.

**Session**

```
launch                       # starts the app per runner config; waits for main window
close                        # File > Exit path with no unsaved changes expected
expect-exit                  # asserts the process ended (used after dialog-driven exits)
```

**Pointer and keyboard**

```
click <target>               # profile decides realization; see Execution profiles
double-click <target>
right-click <target>         # always real pointer (context menus need it)
drag <target> to <target>    # real pointer press-move-release with easing
press "Primary+S"            # logical chord; Primary = Ctrl on this backend, Cmd on a
                             # future macOS one. Literal "Ctrl+..." is a lint warning:
                             # the manual plans carry per-platform shortcut columns and
                             # ~20 translated scripts must not hardcode one platform
type "Smoke Test"            # text to the focused element
focus <target>
```

**Content**

```
set <target> "value"         # writes a text control; expands enclosing Expander first
                             # (collapsed Expanders keep RichEdit content out of the UIA tree)
select <target> "item"       # ComboBox/ListView selection by item name
toggle <target> on|off
```

**Navigation**

```
open-node tree "path"        # Invoke on the tree row; Invoke navigates, selection does not
expand tree "path"           # ExpandCollapse pattern
collapse tree "path"
menu SaveStoryMenuItem       # AutomationId of the leaf item; the driver opens parent menus
menu "File/New Story"        # text-path fallback where a leaf id is missing; lint checks
                             # segments against XAML Text/Header values
context-menu tree "path" "Add/Character"   # right-click + flyout path
tab StoryIdeaTab             # id preferred; text fallback linted as with menu
```

**Dialogs** (composite verbs; the driver owns the native-dialog choreography)

```
save-file-dialog "{scratch}/name.stbx"   # drives the Win32 save picker end to end
open-file-dialog "{scratch}/name.stbx"
dialog "Save changes" click name:"Don't Save"
```

Dialog-verb paths must be `{scratch}`-rooted; the linter rejects anything else as an error, not a warning. The runner is an auto-clicker with the operator's privileges; this rule makes "scripts never touch real user files" mechanical instead of a review convention.

**Waits and pacing**

```
wait <target>                # explicit wait beyond the implicit one
wait-window "title"
pause 1.5                    # seconds × profile pacing factor; ×0 in test mode
```

**Assertions** (diagnostics capture applies to any step failure, timeouts included; see Failure policy)

```
expect <target> exists
expect <target> text "value"
expect <target> enabled | disabled
expect tree contains "path"
expect window "title"
expect-no <target>           # asserts absence, e.g. error dialogs. Settle-then-check:
                             # polls a bounded window (default 1 s) and passes only if
                             # the target never appears. Does not inherit the implicit
                             # wait, which would invert semantics into wait-for-the-error
```

**Presentation**

```
narrate "Adding our first character."   # caption overlay in presentation; log line in test
```

**Capture** (added 2026-10-04, #1421 Milestone 2)

```
screenshot "Overview-Page.png"   # captures the StoryCAD main window to the run's output
                                 # folder (--out). Lint error: anything but a plain .png file
                                 # name (no folder, no ".."), so scripts cannot write elsewhere.
```

Verbs missing during #1422 translation go into this table by PR against `StoryCADAutomation`, never as per-script workarounds (per #1422 scope).

## Execution profiles

Same script, two realizations. Profile is a runner flag, never a script statement.

| Concern | `test` | `presentation` |
|---|---|---|
| `click` realization | UIA pattern (Invoke/SelectionItem) where sufficient; real pointer where patterns are known-insufficient (driver keeps that per-control strategy; the 2026-06-12 navigation case is the founding example) | Real pointer, eased cursor movement, always |
| `pause` factor | 0 | 1.0 (scalable via `--pacing`) |
| Assertions | Hard fail; script aborts (unless `--keep-going`) | Log and continue, except session-fatal errors |
| Window | Whatever launch yields | Fixed size and position for capture (`--window 1920x1080`) |
| `narrate` | Log line | On-screen caption overlay |
| Screenshots | On failure only | Off (external capture owns the pixels) |

Screen recording stays outside the runner: OBS or ffmpeg wraps the process. The runner writes a timestamped step timeline (`timeline.json`) so captions and cuts can be synced to a recording afterward. Building capture into v1 adds an encoder dependency for something the OS already does.

## Runner

```
StoryCADAutomation run <script.scs | directory> [options]
  --profile test|presentation     default test
  --app <path>                    StoryCAD exe; default: sibling Debug build output
  --report <dir>                  default ./automation-reports/<timestamp>
  --keep-going                    report failures but run remaining steps
  --pacing <factor>               presentation pacing multiplier
  --timeout <seconds>             implicit-wait default override
  --window <WxH>                  presentation window size
  --out <dir>                     screenshot output folder; default ./automation-output
  --ci                            assert the CI display (1920x1080 or larger, 100% scale)
                                  at launch; local runs skip it

StoryCADAutomation check <script.scs | directory>   # lint only, no launch
```

**Exit codes:** 0 all steps passed; 1 one or more step failures; 2 script parse/lint error; 3 environment or launch failure.

**Environment prep** (the `uia_header_probe.ps1` preconditions, made code). The seeded `Preferences.json` is load-bearing and its contents are part of this design, not an implementation detail:

- `OutlineDirectory` and `BackupDirectory` point INSIDE the per-run scratch directory, so AutoSaveService and BackupService write there and nowhere else. Containment fails if these stay at user defaults.
- AutoSave and timed backup OFF in the test profile. ST-005 asserts the dirty indicator and the Save-changes dialog; with autosave on, that dialog may never appear and the smoke script flakes on an unpinned preference.
- First-run/onboarding suppression per the probe header's remaining keys.

The child process launches with a scrubbed environment: `COLLAB_DEBUG` and `COLLAB_DEV_ENABLED` cleared at minimum (a Debug build with `COLLAB_DEBUG=1` throws a `Debugger.Launch()` JIT prompt that hangs an unattended run; issue #1461). The runner refuses to start if a `.env` sits in the app bin. The reason is backend isolation: no user registration, no telemetry posts during automated runs. The Shell_Loaded crash that rule originally worked around was fixed by PR #1459 and is not the justification. It also refuses if a StoryCAD instance is already running.

**Teardown:** Ctrl+C (console cancel) runs teardown and still writes reports. The app launches inside a kill-on-close job object so a crashed child and its Windows Error Reporting dialog cannot outlive the run. Scratch is swept on exit.

**Reports per run:** JUnit XML (one testsuite per script, one testcase per `step`) for CI publishing, a human-readable log, per-failure diagnostics, `timeline.json`, and a copy of the app's own NLog log files. The last one matters most at 2 a.m.: a screenshot of an error dialog without the app's stack trace diagnoses nothing, and elmah.io is deliberately unreachable during runs (no `.env`, no keys).

**Failure policy:** default abort-on-first-failure with remaining steps reported skipped. ANY step failure, timeout or assertion alike, captures a screenshot, the relevant UIA subtree, and the script line. App process death mid-script is a step failure (exit 1) with full diagnostics, never a retryable environment error. No step-level retries in v1; the readiness waits are the anti-flake mechanism, and retrying steps hides real regressions. One whole-script retry is permitted for exit-code-3 launch failures only, and CI counts it in the published run.

## Script lint as a fitness function

`check` mode parses every script, then verifies:

- **Bare-id targets** exist as `AutomationProperties.AutomationId` in the XAML. The scan logic is extracted from `AutomationConventionTests` into a shared library referenced by both StoryCADTests and StoryCADAutomation; a second copy would drift.
- **Runtime-inert ids** are rejected via a deny-list (ItemsRepeater container ids like `NavigationTree`/`TrashTree` exist statically but never surface a peer; a pure existence scan would pass them).
- **Menu/tab text-path segments** match XAML `Text`/`Header` values where the text form is used.
- **Dialog-verb paths** are `{scratch}`-rooted (error).
- **`name:` targets** outside `dialog` scopes and **literal platform chords** (`Ctrl+...`) are warnings.
- **`screenshot` names** are plain `.png` file names (error).

CI runs `check` over all scripts on every PR, so a renamed id or menu label fails at lint time instead of in a UI run. What `check` cannot verify, stated plainly: tree node names (runtime outline data), native-dialog element names, and whether a statically valid id actually surfaces a peer at runtime outside the deny-list. Those fail only live.

## CI hosting

The smoke script runs per PR on hosted `windows-latest`, test profile only. Recorded assumptions that pointer math and `--window` depend on: hosted runners provide an interactive desktop at a fixed resolution with 100% DPI; the driver asserts the actual resolution/DPI at launch and fails with exit 3 on mismatch rather than clicking blind. The app is single-instance, so at most one automation job per runner at a time. Presentation profile is not a CI workload; it runs on a local interactive machine where an operator owns the desktop. If hosted-runner flake exceeds the budget in practice, the fallback decision (self-hosted interactive runner) comes back to the issue rather than being made silently.

## macOS seam

Verbs name intent (`click`, `open-node`), never UIA mechanics. A future macOS backend (the accessibility-spike issue) would implement the same driver interface; scripts and interpreter don't change. No macOS work in v1 beyond keeping UIA types out of the interpreter layer.

## Out of scope for v1

Control flow, variables, includes; a recording tool that emits `.scs`; video capture (stills arrive with `screenshot`, Milestone 2); step-level retry; macOS backend; translating any plan beyond `Smoke_Test.md` (#1422 owns the portfolio).

## First script

`Smoke_Test.md`'s five sections compile to roughly 35 lines. ST-002 as the shape proof:

```
step "ST-002: Create and Save New Story"
menu OpenCreateFileMenuItem      # 4.x File menu unifies New/Open behind one item; the exact
                                 # new-outline choreography gets pinned during implementation
expect tree contains "Untitled"
set TitleTextBox "Smoke Test"    # OverviewPage.xaml:15
expect tree contains "Smoke Test"
press "Primary+S"
save-file-dialog "{scratch}/SmokeTest.stbx"
expect-no name:"Error"
```

(`{scratch}` is the one built-in substitution: the runner's per-run scratch directory. It exists so scripts never write into real user folders; it is not a variable system.)
