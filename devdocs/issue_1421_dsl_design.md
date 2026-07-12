# StoryCAD Automation DSL, Driver, and Runner: Design

Issue #1421. Decision context: issue body "Architecture decision (2026-07-12)" section. Status: draft for design review.

## Components

One new console project, `StoryCADAutomation`, target `net10.0-windows10.0.22621` (FlaUI wraps UIA3 and is Windows-only; the DSL itself is backend-neutral, see "macOS seam" below). Three layers inside it:

| Layer | Contents | Depends on |
|---|---|---|
| Driver | Launch/teardown, element location, waits, pointer/keyboard primitives, the #1420 runtime facts | FlaUI.Core, FlaUI.UIA3 |
| Interpreter | Script parser, verb dispatch table, one handler per verb calling driver methods | Driver |
| Runner | CLI, execution profiles, environment prep, report writers | Interpreter |

No MSTest reference. The runner is the executable; CI invokes it directly.

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

Grounded in what `Smoke_Test.md` and the four tier-1 plans actually do. Every locating verb carries an implicit wait-until-found with a default 5 s timeout; there are no bare sleeps in test mode. That policy is the flakiness budget's home.

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
press "Ctrl+S"               # key chord to the focused element
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
menu "File/New Story"        # menu bar path
context-menu tree "path" "Add/Character"   # right-click + flyout path
tab "Physical"               # Pivot/tab item within the current page
```

**Dialogs** (composite verbs; the driver owns the native-dialog choreography)

```
save-file-dialog "path"      # drives the Win32 save picker end to end
open-file-dialog "path"
dialog "Save changes" click name:"Don't Save"
```

**Waits and pacing**

```
wait <target>                # explicit wait beyond the implicit one
wait-window "title"
pause 1.5                    # seconds × profile pacing factor; ×0 in test mode
```

**Assertions** (each failure captures screenshot, UIA subtree dump, script line)

```
expect <target> exists
expect <target> text "value"
expect <target> enabled | disabled
expect tree contains "path"
expect window "title"
expect-no <target>           # asserts absence, e.g. error dialogs at launch
```

**Presentation**

```
narrate "Adding our first character."   # caption overlay in presentation; log line in test
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

StoryCADAutomation check <script.scs | directory>   # lint only, no launch
```

**Exit codes:** 0 all steps passed; 1 one or more step failures; 2 script parse/lint error; 3 environment or launch failure.

**Environment prep** (the `uia_header_probe.ps1` preconditions, made code): seeded `Preferences.json`, refuse to run if a `.env` sits in the app bin, refuse if a StoryCAD instance is already running, per-run scratch directory for outlines the script saves (`SmokeTest.stbx` lands there, not in the user's Documents), teardown kills a hung process and sweeps scratch.

**Reports per run:** JUnit XML (one testsuite per script, one testcase per `step`) for CI publishing, a human-readable log, failure screenshots, and `timeline.json`.

**Failure policy:** default abort-on-first-failure with remaining steps reported skipped. No step-level retries in v1; the implicit waits are the anti-flake mechanism, and retrying steps hides real regressions. One whole-script retry is permitted for exit-code-3 launch failures only, and CI counts it in the published run.

## Script lint as a fitness function

`check` mode parses every script and verifies each bare-id target exists as an `AutomationProperties.AutomationId` in the XAML, using the same static scan `AutomationConventionTests` already does (no UI, runs anywhere). CI runs `check` over all scripts on every PR. A renamed or deleted id fails the build at parse time instead of at 2 a.m. in a UI run. `name:` targets outside `dialog` scopes are lint warnings.

## macOS seam

Verbs name intent (`click`, `open-node`), never UIA mechanics. A future macOS backend (the accessibility-spike issue) would implement the same driver interface; scripts and interpreter don't change. No macOS work in v1 beyond keeping UIA types out of the interpreter layer.

## Out of scope for v1

Control flow, variables, includes; a recording tool that emits `.scs`; screen capture; step-level retry; macOS backend; translating any plan beyond `Smoke_Test.md` (#1422 owns the portfolio).

## First script

`Smoke_Test.md`'s five sections compile to roughly 35 lines. ST-002 as the shape proof:

```
step "ST-002: Create and Save New Story"
menu "File/New Story"
expect tree contains "Untitled"
set OverviewName "Smoke Test"
expect tree contains "Smoke Test"
press "Ctrl+S"
save-file-dialog "{scratch}/SmokeTest.stbx"
expect-no name:"Error"
```

(`{scratch}` is the one built-in substitution: the runner's per-run scratch directory. It exists so scripts never write into real user folders; it is not a variable system.)
