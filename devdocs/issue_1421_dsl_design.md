# StoryCAD Automation DSL, Driver, and Runner: Design

Issue #1421. Decision context: issue body "Architecture decision (2026-07-12)" section. Status: draft for design review.

**Build status (2026-10-05).** Milestones 1 and 2 are built: the driver, the parser, lint, the interpreter, `run` with console output and exit codes 0-3, `check`, `--ci`, `--out`, the `screenshot` verb and the CI job. The app's data root comes from the `STORYCAD_ROOT_DIR` environment variable. Milestone 4 adds the presentation profile in a minimum form (see Execution profiles) with `--profile`, `--pacing` and `--window`. Dropped on 2026-10-05: JUnit XML, NLog collection, `--keep-going`, launch retry and `timeline.json`. `--keep-going` returned on 2026-10-17 (#1422 evaluation): a failure skips the rest of its step and the run goes on with the next step. Folder runs for `run` moved to #1422. Sections below that describe dropped parts are kept for history and are not built.

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

Strings have no escape sequences, with one exception. In the string of `show text`, the two characters `\n` start a new line on the card (#1626, Terry, 2026-10-20). Each line is centered. Other strings keep `\n` as two characters, because a Windows path can contain them (`C:\new`).

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
wait <target> condense       # a long wait, up to 5 minutes, for a Collaborator run to finish;
                             # test just waits, and `video` cuts the wait down to its two ends
                             # (video design section 9.2; #1626, Terry, 2026-10-24)
wait-window "title"
pause 1.5                    # seconds × profile pacing factor; ×0 in test mode
```

**Assertions** (diagnostics capture applies to any step failure, timeouts included; see Failure policy)

```
expect <target> exists
expect <target> text "value"
expect <target> enabled | disabled
expect <target> expanded | collapsed   # the ExpandCollapse state of the target itself (#1422);
                                       # a child-row check cannot work, because resolving a tree
                                       # path expands every parent on the way
expect <target> offscreen     # found, but out of view: a stacked SplitView pushes the page past
                              # the window edge instead of removing it (#1422 Window_Management)
expect <target> gone          # waits (implicit wait) until the target is absent: for a control an
                              # action removes, such as a pane the hamburger button hides. UI
                              # Automation keeps it for a moment after the app hides it, so
                              # expect-no failed there in 2 of 5 VM runs (#1422). Passes once
                              # the target stays absent for the 1 s settle window; a UIA read
                              # error restarts it. expect <target> offscreen also waits, up to
                              # the implicit wait
expect tree contains "path"
expect window "title"
expect-no <target>           # asserts absence, e.g. error dialogs. Settle-then-check:
                             # polls a bounded window (default 1 s) and passes only if
                             # the target never appears. Does not inherit the implicit
                             # wait, which would invert semantics into wait-for-the-error
```

**Window** (added 2026-10-20, #1422 Window_Management)

```
window size 700x744          # visible frame in 100%-scale pixels at the display's top-left (the
                             # units of --window); the app's minimum width is 520
window minimize
window maximize
window restore               # fails unless the frame is back where it was before the first
                             # minimize or maximize; gives the app the foreground again
```

The driver acts through Windows (ShowWindow), not the title bar buttons, which UI Automation does not reach in the WinAppSDK title bar.

`MoreButton`, the CommandBar "..." button, is a WinUI template part, so the XAML scan never sees it; the linter accepts it by name.

**Presentation**

```
narrate "Adding our first character."   # caption overlay in presentation; log line in test
narrate "The extension dot S-T-B-X." caption "The extension .stbx."
                                        # caption "...": the written form for the video's captions
                                        # when the spoken form reads badly (#1626, Terry, 2026-10-23).
                                        # Without it, the captions use the narrate text.
```

**Capture** (added 2026-10-04, #1421 Milestone 2)

```
screenshot "Overview-Page.png"   # captures the StoryCAD main window to the run's output
                                 # folder (--out). Lint error: anything but a plain .png file
                                 # name (no folder, no ".."), so scripts cannot write elsewhere.
screenshot dialog "File-Open-Dialog.png"   # captures only the open ContentDialog, for manual
                                           # images of a dialog; same name rule (Terry approved
                                           # this second Milestone 2 design change, 2026-10-04)
screenshot "File-Menu.png" crop 380,30,340,276   # captures this part of the main window only:
                                                 # x,y,width,height in 100%-scale pixels from the
                                                 # window's top-left corner (the units of --window).
                                                 # For close-up manual images (Terry, 2026-10-18,
                                                 # #1422). A crop outside the window fails the step.
screenshot "File-Menu.png" of FileMenuButton ExitMenuItem margin 12   # captures the union of the
                                                 # targets' on-screen rectangles (one or more targets,
                                                 # same grammar as other verbs), grown by margin N
                                                 # 100%-scale pixels and clipped to the window. Follows
                                                 # layout changes; prefer it to crop. crop and of are
                                                 # exclusive. A target with no area in the window fails.
```

Verbs missing during #1422 translation go into this table by PR against `StoryCADAutomation`, never as per-script workarounds (per #1422 scope).

## Execution profiles

Same script, two realizations. Profile is a runner flag, never a script statement.

| Concern | `test` | `presentation` |
|---|---|---|
| Targeted verbs | Act directly | The cursor glides to the target first (one hook for every targeted verb and for `dialog`; settles review M7) |
| `click` realization | UIA pattern (Invoke/SelectionItem) where sufficient; real pointer where patterns are known-insufficient | Glide, then the same realization as test |
| `type`, `set` | `type` all at once; `set` through the Value pattern | One character at a time, with a 65 ms pause times `--pacing` after each one on top of input processing; `set` clears the field through the Value pattern (which opens a collapsed Expander), then types |
| `pause` factor | 0 | 1.0 times `--pacing` |
| `narrate` | Log line | Log line; in a step, the clip plays while the step's actions run, and the step waits for it (video design, section 7) |
| Assertions | Hard fail; script aborts | Same as test: a failed demo is recorded again |
| Window | Whatever launch yields | `--window WxH` places the main window at the top-left of the primary display with a visible frame of W x H pixels at 100% scale; the runner multiplies by the window's display scale, so a script gives the same layout on any display (#1422). The `video` command sizes the window itself |

Recording is the `video` command (`devdocs/issue_1421_video_design.md`, Milestone 5). It replaced the OBS recording and the `.srt` subtitle file of Milestone 4 (Terry, 2026-10-07). YouTube's captions from the voice track misspelled names and terms, so `video` again writes a `.srt` file next to the `.mp4`, from the `narrate` text (or its `caption` clause) and the clip times that assembly uses (#1626, Terry, 2026-10-23; video design section 9.1).

Not glided in presentation: `press`, the flyout steps inside `context-menu`, and the text-path form of `menu`. `menu` by id clicks, as a user would, the toolbar button and any sub-menu that the XAML puts the item under (`XamlUiFacts.MenuOpeners`), then the item. It never searches the menus: the lint rejects `menu` for an id that no toolbar button owns, and a context-menu item is reached with `context-menu` (#1422). If the item has not appeared 1.5 s after the menus are opened, the menus are clicked once more (a click that lands while another flyout closes only dismisses it). If the item is still showing 1.5 s after its click, the Invoke pattern runs. `menu` needs StoryCAD in the foreground in both profiles. A UIA Invoke left the flyout holding the keyboard, so later `type` and `press` steps were lost (found in Milestone 4).

## Runner

```
StoryCADAutomation run <script.scs> [options]
  --profile test|presentation     default test
  --app <path>                    StoryCAD exe; default: sibling Debug build output
  --pacing <factor>               presentation pause and typing multiplier; default 1.0
  --window <WxH>                  visible frame size in 100%-scale pixels, e.g. 1036x744
  --keep-going                    after a failure, skip the rest of that step and run the next step
  --out <dir>                     output folder (screenshots, voice clips); default ./automation-output
  --ci                            assert the CI display (1920x1080 or larger, 100% scale)
                                  at launch; local runs skip it

StoryCADAutomation check <script.scs | directory>   # lint only, no launch
StoryCADAutomation voice <script.scs> --out <dir>   # voice clips (video design, 8.1)
StoryCADAutomation video <script.scs> --out <dir> --assets <dir>   # the .mp4 (video design, 8.2)
```

**Exit codes:** 0 all steps passed; 1 one or more step failures; 2 script parse/lint error; 3 environment or launch failure.

**Environment prep** (the `uia_header_probe.ps1` preconditions, made code). The seeded `Preferences.json` is load-bearing and its contents are part of this design, not an implementation detail:

- `OutlineDirectory` and `BackupDirectory` point INSIDE the per-run scratch directory, so AutoSaveService and BackupService write there and nowhere else. Containment fails if these stay at user defaults.
- AutoSave and timed backup OFF in the test profile. ST-005 asserts the dirty indicator and the Save-changes dialog; with autosave on, that dialog may never appear and the smoke script flakes on an unpinned preference.
- First-run/onboarding suppression per the probe header's remaining keys.

The child process launches with a scrubbed environment: `COLLAB_DEBUG` and `COLLAB_DEV_ENABLED` cleared at minimum (a Debug build with `COLLAB_DEBUG=1` throws a `Debugger.Launch()` JIT prompt that hangs an unattended run; issue #1461). A `.env` in the app bin is allowed (Collaborator #197, Terry 2026-10-19): the seeded preferences already prevent user registration and telemetry, and Collaborator needs the backend. When the machine's installed StoryCAD holds a Collaborator sign-in, the runner copies it into the seeded preferences. The runner refuses if a StoryCAD instance is already running.

**Teardown:** Ctrl+C (console cancel) runs teardown and still writes reports. The app launches inside a kill-on-close job object so a crashed child and its Windows Error Reporting dialog cannot outlive the run. Scratch is swept on exit.

**Reports per run:** JUnit XML (one testsuite per script, one testcase per `step`) for CI publishing, a human-readable log, per-failure diagnostics, `timeline.json`, and a copy of the app's own NLog log files. The last one matters most at 2 a.m.: a screenshot of an error dialog without the app's stack trace diagnoses nothing, and elmah.io receives nothing during runs (error-reporting consent is off in the seeded preferences).

**Failure policy:** default abort-on-first-failure with remaining steps reported skipped. ANY step failure, timeout or assertion alike, captures a screenshot, the relevant UIA subtree, and the script line. App process death mid-script is a step failure (exit 1) with full diagnostics, never a retryable environment error. No step-level retries in v1; the readiness waits are the anti-flake mechanism, and retrying steps hides real regressions. One whole-script retry is permitted for exit-code-3 launch failures only, and CI counts it in the published run.

## Script lint as a fitness function

`check` mode parses every script, then verifies:

- **Bare-id targets** exist as `AutomationProperties.AutomationId` in the XAML. The scan logic is extracted from `AutomationConventionTests` into a shared library referenced by both StoryCADTests and StoryCADAutomation; a second copy would drift.
- **Runtime-inert ids** are rejected via a deny-list (ItemsRepeater container ids like `NavigationTree`/`TrashTree` exist statically but never surface a peer; a pure existence scan would pass them).
- **Menu text paths** match a whole menu path in the XAML: the owning button label, then the sub-menu labels, then the leaf, each as the Windows UIA Name (explicit `AutomationProperties.Name`, else the Windows `Text`/`Label`/`Header`). Changed for #1422 (review M3): a check of each segment alone passed `menu "File/Exit"`, but the Windows Name is `Quit`. **Tab text** matches a TabViewItem `Header`.
- **Dialog-verb paths** are `{scratch}`-rooted (error).
- **`name:` targets** outside `dialog` scopes and **literal platform chords** (`Ctrl+...`) are warnings.
- **`screenshot` names** are plain `.png` file names (error). A name outside the manual image standard (`devdocs/automation_naming_convention.md`, Manual image names) is a warning.
- **`narrate` captions**: two `narrate` lines with the same text and different caption text are an error. A line without `caption` has the narrate text as its caption text, so a line with `caption` and a line without it differ unless the caption equals the narrate text. The clip is keyed by the narrate text, so one text has one caption (#1626).

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
