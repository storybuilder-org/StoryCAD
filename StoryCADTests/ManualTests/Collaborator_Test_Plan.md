# Collaborator Test Plan
**Time**: ~45 minutes by hand (script run: not yet measured)
**Purpose**: Test the Collaborator feature inside StoryCAD, follow the user manual section "StoryCAD Collaborator" (`docs/StoryCAD Collaborator/`), and retake its images.

**Scope:** Open/close, document gate, autosave/backup pause, run each workflow the manual shows without hang/timeout, accept/skip, Review Each, chat, interview save, credits dialog entry. Requires a document open and Collaborator already able to open for this machine (see Prerequisites).

**Out of scope:** Prompt quality, workflow content debugging, Try Again behavior, element-picker memory across runs. Store purchase / paid activation paths. Administrative enrollment of a tester identity (private setup plan). Model error messages (they need a failing model).

**Related (private repo):** system setup, environment configuration, identity enrollment, and the proxy-unreachable case live in Collaborator `CollaboratorTests/ManualTests/Collaborator_System_Setup_Test_Plan.md`. Do not copy those steps into this public plan.

**Cost:** each workflow run, chat turn and interview turn is charged. One full pass makes 12 workflow runs, 2 chat turns and 2 interview turns.

---

## Prerequisites

Complete **before** this suite. This plan does not document how.

1. Developer or tester activation for this machine is configured so Collaborator opens without the Subscribe dialog.
2. The AI proxy this build uses is reachable from your network.
3. If Collaborator still offers Subscribe instead of the feature UI, stop here and finish the private system setup plan. Do not treat that as a TC-110 failure.

## Setup

By hand: start StoryCAD the way you will for the whole session. Tools > Preferences > Backup: AutoSave every **30** seconds, timed backups every **1** minute; note the **Backup directory** under Save Locations (TC-113 uses it). Save preferences. Restore them after the suite.

Script: `StoryCADTests/AutomationScripts/Collaborator.scs`. The header has the run line; four `--pref` values set the same AutoSave and backup intervals, so the script never opens the Preferences dialog. Run it on a desktop with Collaborator sign-in, hands off; the automation VM has none.

Each step lists the manual page it covers. The script follows the pages in reading order, except where one workflow run serves several pages. Steps run in table order: later steps rely on the outline state that earlier steps leave.

Not scripted: `Collaborator-Join-Dialog.png` (needs a machine without activation); TC-139 (added for #1645; a script step comes with the #1422 work). TC-112 and TC-113 timing and file checks stay manual: `pause` is zero in a test run.

---

| Step | Manual page | Actions | Expected | Images |
|------|-------------|---------|----------|--------|
| TC-121 | Getting Started; Outline Gaps | Create a new outline "My Story". Click Collaborator. Click Outline gaps. Exit | Toolbar shows Collaborator. Outline gaps page lists the Overview's empty fields and a Guess sentence | Collaborator-Toolbar-Button, Collaborator-Outline-Gaps |
| TC-111 | | File > Close. Click Collaborator. Then File > Open/Create > Sample outlines > Danger Calls | Collaborator does **not** open. Status bar: "No story is open. Open an outline before using Collaborator." No crash. The main window opens the sample | |
| TC-122 | An Example Session | Select Danger Calls at the top of the tree; Story Idea tab | Overview shows the Story Idea about teenagers selling drugs | Collaborator-Session-Overview |
| TC-110 | What Collaborator Is; Opening Collaborator; A Path to Try | Click Collaborator. Then select Leonard Kraskin in the main window | Story Collaborator window opens alongside the main window. Left panel: Outline gaps row, Starred band (Ideation, Story Problem, Problem Builder, Character Builder, Setting Builder, Scene Builder), closed groups. The main window responds while Collaborator is open | Collaborator-With-StoryCAD, Collaborator-Workflow-Pane |
| TC-123 | Workflows and Writing Craft; Workflow Reference | Open the Overview, Problem, Character and Story World groups | Every workflow is listed, Problem Beats included. No Setting or Scene group | Collaborator-Workflow-List |
| TC-124 | Opening Collaborator | Customize Workflows, then Cancel. Star Problem Beats, then unstar it. Settings, then Cancel. Help, then Escape | Dialog lists every workflow with a checkbox. The star moves the row to Starred and back. Settings shows Response Terseness and the cost checkbox. Help shows Open the full manual | Collaborator-Customize-Workflows |
| TC-112 | | Collaborator open. Type in the Overview Notes tab. Wait 90 s. Exit Collaborator. Wait 60 s | Changed indicator turns red. With Collaborator open, the outline file time does **not** change. After Exit, AutoSave updates the file time | |
| TC-113 | | Open Collaborator. Wait 90 s. Exit. Wait 90 s | No **new** `Danger Calls as of ….zip` in the backup directory while open. A new zip appears after Exit | |
| TC-119 | | Open Collaborator again (TC-113 opened and closed it) | Fresh window; workflow list available. Step 4, a short run, is TC-114 | |
| TC-114 | Running a Workflow; Reviewing Suggestions; Overview Workflows; An Example Session | Overview selected. Click Ideation | The click runs it; no Run button. Response in under 3 minutes, no hang. Property Updates lists Story Idea, Concept and Premise, each Has your text; Accept All at the foot; Review Each and Try Again enabled. Nothing applied yet | Collaborator-Window-Overview, Collaborator-Session-Premise, Collaborator-Workflow-Premise, Collaborator-Property-Updates |
| TC-125 | Opening Collaborator | Click the three-line control twice | The list hides and the work area runs the full width; the list comes back | Collaborator-Pane-Collapsed |
| TC-126 | An Example Session; Reviewing Suggestions | Review Each; Skip three times | Card shows Yours and Proposed and a count. Skip keeps the sample text | |
| TC-115 | Overview Workflows; Running a Workflow | Story Problem: pick Catching the drug dealer Lacas, Leonard Kraskin, Charlie Lacas. Accept All; Cancel at "Replace existing content?" | Three pickers in turn. Accept All writes the New rows and asks before Has your text rows; Cancel keeps them. Checked in TC-138 | Collaborator-Workflow-Story-Problem |
| TC-117 | Overview Workflows | Open the four groups. Story Form. Accept nothing | Proposes Type and Genre. Checked in TC-138: Genre and Type unchanged | Collaborator-Workflow-Story-Form |
| TC-139 | Overview Workflows | Collaborator is open. In the main window, select the Overview. On the Story Overview page, change Genre to Western. Do not select a different element. In the Collaborator window, select Story Form. Select Review Each. Select Skip for each card. In the main window, change Genre to Suspense | The Genre card shows Western in Yours. Collaborator reads a change that the writer makes in the main window while Collaborator is open (#1645). TC-138 finds Genre Suspense | |
| TC-127 | Problem Workflows | Inner and Outer Problems: outer Catching the drug dealer Lacas, inner Leonard's impulsiveness lets Lacas capture him | No protagonist picker (taken from the outer Problem). Rows for the inner Problem and a Flaw | Collaborator-Workflow-Inner-Outer |
| TC-128 | Running a Workflow; An Example Session; Problem Workflows | In StoryCAD, set the tech problem's Problem Category Complication, Protagonist Leonard Kraskin, Antagonist Charlie Lacas. Problem Builder; pick the tech problem. Review Each, Skip one, Accept Free Remaining | Picker "Select Problem" lists the problems with Create a new element. Rows for the Problem fields only: no beat sheet rows (Collaborator #302) | Collaborator-Element-Picker, Collaborator-Updates-After-Run, Collaborator-Session-Problem-Builder |
| TC-129 | Problem Workflows; A Path to Try | Problem Beats on the same problem. Accept All | One row per beat; no Replace question | Collaborator-Workflow-Problem-Beats |
| TC-130 | Character Workflows | Character Builder on Leonard Kraskin. Accept All; Cancel | Long list of sheet fields and the Character Sketch | Collaborator-Workflow-Character-Builder |
| TC-131 | Outline Gaps | Click Outline gaps | Guess sentence has moved past Ideation; remaining empty fields listed | Collaborator-Outline-Gaps-Midway |
| TC-132 | Character Workflows; Chat | Flaw and Backstory on Charlie Lacas. Ask a question in chat. Then ask chat to rewrite Flaw and Backstory. Accept nothing | Rows for Flaw and BackStory. The chat answers the question. The rewrite changes the waiting rows in place | Collaborator-Workflow-Flaw-Backstory, Collaborator-Chat, Collaborator-Workflow-Flaw-Backstory-Chat |
| TC-133 | Character Workflows | Character Relationship: Leonard Kraskin, then Tony Irwin | One relationship row | Collaborator-Workflow-Relationship |
| TC-134 | Character Workflows | Character Interview on Charlie Lacas. Type Flaw; answer the first question; Save interview to outline | Choice list, then one question per turn. No Accept All. Save writes a note under Charlie | Collaborator-Workflow-Character-Interview, Collaborator-Workflow-Character-Interview-End |
| TC-135 | World and Setting Workflows | Define Story World | No picker; a Story World is made if missing. Rows for World Type, description, Cultures and the live areas | Collaborator-Workflow-Story-World |
| TC-116 | World and Setting Workflows; Reviewing Suggestions | Setting Builder on Unmarked police car. Accept the first row, then the second, with the row check marks | Every row New. Each accepted row leaves the list; the others stay pending. Weather still pending | Collaborator-Workflow-Setting-Builder, Collaborator-Row-New |
| TC-136 | Reviewing Suggestions | Review Each | Card: field name, Yours empty, Proposed, Accept, Skip, Accept Free Remaining, a count | Collaborator-Review-Each |
| TC-137 | Scene Workflows | Scene Builder on We have a drug deal | Rows across the Scene, Development, Conflict and Sequel tabs | Collaborator-Workflow-Scene-Builder |
| TC-138 | Running a Workflow; Reviewing Suggestions | Exit. Open Catching the drug dealer Lacas, Protagonist tab; Overview Structure tab; Unmarked police car | TC-115: Protagonist is Leonard Kraskin, and the accepted rows show their text. TC-117: Genre Suspense, Type Short Story. TC-116: the two accepted fields hold text; Weather is empty | |
| TC-120 | | Buy Credits (tooltip "Buy Collaborator Credits"), then Not now | Button enabled when the command bar is idle. Buy Credits dialog opens; on a sideloaded or non-store build the pack list may be empty or show a store error. Assertion is **dialog opens**, not a purchase. Not now closes it; main window responsive | |

Script line numbers: each `step "TC-nnn"` line in the script.

## Notes

- Proxy responses can take up to 3 minutes. Do not treat a slow response as a hang before that. The script waits up to 5 minutes.
- Logs: under the app's `RootDirectory` logs folder (package or unpackaged); useful if open fails after Prerequisites were believed complete.
- Numbering: TC-118 (proxy unreachable) is an intentional gap; that case is on the private system setup plan. TC-121 to TC-138 are manual-section steps added in #1422.
- Image renames: `Collaborator-Session-ProblemBuilder.png` becomes `Collaborator-Session-Problem-Builder.png` (name standard); `Collaborator-Workflow-Problem-Builder.png` becomes `Collaborator-Workflow-Problem-Beats.png` (the image now shows a Problem Beats run).

**Pass/Fail:** ______
