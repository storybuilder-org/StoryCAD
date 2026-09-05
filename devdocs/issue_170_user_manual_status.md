# Issue #170: StoryCAD Collaborator user manual, work list and status

Issue: https://github.com/storybuilder-org/Collaborator/issues/170
Branch: StoryCAD `issue-170-collaborator-manual` (from `dev` 61186daf, 2026-09-04).
Scope: the **StoryCAD Collaborator** topic only: `docs/StoryCAD Collaborator/` and its images under `docs/media/`. No other part of the manual changes without discussion.
Path to readers: PR into `dev`; `sync-beta-manual.yml` then publishes `docs/` to beta.manual.storybuilder.org. Production (`main`) gets the topic only on a deliberate later merge.

## Where it stood on 2026-09-05

- StoryCAD PR #1526 (2026-08-15) replaced 17 of 18 image placeholders, rewrote the shell copy after #1511 and #1518 (no Save button; Accept all changes at the foot of the list), moved the tutorial pages under `Tutorial/`, and added the Help flyout link that opens this topic at the beta or production host per the Use beta documentation preference.
- Beta URLs return 200. Production `main` has no `docs/StoryCAD Collaborator/` folder; that URL is 404.
- The issue body was last edited 2026-08-08 and still lists the #1526 work as remaining.

## Work list

Terry's items from the 2026-09-05 session. Update this list as the work proceeds.

### 0. Setup
- [x] StoryCAD branch `issue-170-collaborator-manual` from `origin/dev`.

### 1. Beta access, not sign-up
The manual describes subscribing with a free trial. That is the production path. The first release is an invited beta where StoryBuilder Foundation pays for the testers' AI use. The manual has to say so.
- [x] `Getting_Started.md` "Turn it on": steps 1 to 4 offer Subscribe or Restore purchases, then a sentence on trial length and price. Replace with the beta path: invited testers, no purchase, the Foundation covers the AI cost. Done 2026-09-05: rewritten for the beta (About the beta; install, first launch registers the copy, tell the Foundation, restart, click Collaborator). One text placeholder remains for what the app shows while approval is pending; see the findings below.
- [x] `Getting_Started.md` line 20 and `Tips_and_Common_Questions.md` "Connection and access" both say "additional paid feature with a free trial." Reword for beta. Getting Started done 2026-09-05; the Tips sentence is left for the item 6 pass on that page.
- [x] `Getting_Started.md` leftover placeholder `<!-- image: subscribe or start trial prompt -->`: drop, or replace with what a beta tester sees. Replaced 2026-09-05 with a placeholder for the Story Collaborator window as it first opens.
- [x] Landing page and `What_Collaborator_Is.md`: check the "paid plug-in, dual store, free trial" framing against beta wording. What Collaborator Is no longer states the business terms (item 2). Landing page fixed in the item 6 pass (f4ca5a58).
- [ ] Verify on the beta build before writing: what a tester sees when they click Collaborator, and whether the store dialog appears at all. Verified against the code 2026-09-05, not a build; the findings below are what the code does today.

### 2. What Collaborator Is: strengthen "What it is not"; cite the AI use policy
- [x] `What_Collaborator_Is.md` "## What it is not" (line 29, a short bullet list today). Reinforce it. Done 2026-09-05: six short paragraphs, each with its reason (not prose, not a substitute for craft, nothing written until accepted, no library of books, your work not kept or trained on, no guarantee).
- [ ] Same page: state the Foundation's AI use policy and link https://storybuilder.org/ai-usage-policy/. Read the live page first so the wording matches.

### 3. Image folder and placeholder convention
Today 17 `Collaborator-*.png` files sit loose in `docs/media/` (151 files total; existing subfolders `Elements/`, `Tutorial/`). Pages reference them as `../media/Collaborator-X.png`.
- [x] Create `docs/media/Collaborator/`, move the 17 files, fix every image path (`../media/Collaborator/...`; Tutorial pages `../../media/Collaborator/...`). Done 2026-09-05; `Collaborator-Session-Problem.png` moved too, though no page references it: delete or use it at screenshot time.
- [x] Placeholder form, adopted 2026-09-05: a visible italic line in the page, `*[Screenshot to come: <what it shows>. File: Collaborator-<Name>.png]*`, placed where the image will go. Never an HTML comment. First use: `Getting_Started.md`, `Collaborator-Access-Prompt.png` (its fate is item 1).
- [x] Outline plan. Ruling (Terry, 2026-09-05): the screenshots come from a Collaborator tutorial, the same job [Tutorial Creating a Story](../docs/Tutorial%20Creating%20a%20Story/Tutorial_Creating_a_Story.md) does for Danger Calls, except Collaborator runs the workflows in order and the writer accepts or skips. Start from a Story Idea only. End with a complete outline. Danger Calls stays the one place the shipped sample appears, for Has your text. Story not chosen yet; candidate pool is `C:\temp\story-idea-only-outlines.docx` (94 idea-only `.stbx` files under `G:\My Drive\2-Areas\Writing\Projects`, scanned 2026-09-05).
- [x] Shot list written (below). Capture and placeholder replacement still open.
- [ ] Capture on a labeled beta build against the tutorial outline, replace placeholders. This run is the closest thing Collaborator has to an acceptance test: every registered workflow, in order, on one outline, with the writer in the loop.

### 4. Describe every workflow
The manual counts workflows inconsistently (`Opening_Collaborator.md`: "about twenty", seven default stars; `Tutorial/A_Path_to_Try.md`: thirteen, five stars) and describes none individually.
Registry on `dev` (counted 2026-09-05, `CollaboratorLib/Workflows/WorkflowRegistry.cs`, 12 `new Workflow(`): Premise (Ideation), StoryProblem, StoryForm, InnerOuterProblems, ProblemBuilder, DefineCharacter, StoryFunction (Character Story Function), FlawBackstory, Relationship, DefineStoryWorld, SettingBuilder, SceneBuilder. Collaborator #119 adds CharacterInterview (13). #224 (2026-09-01) folded SettingTimeSpace and Sensations into SettingBuilder, which is why "thirteen" (written 2026-08-23) is now 12. Default stars: Premise, StoryProblem, ProblemBuilder, StoryFunction, SceneBuilder.
- [x] Fix the counts everywhere. Prefer wording that does not go stale, or one count in one place. Done 2026-09-05: Opening Collaborator no longer states a count; A Path to Try points at the Workflow Reference instead of "thirteen"; Story World added to the group list.
- [x] One entry per registered workflow, sourced from the registry `title`, `description`, `explanation`, and the gather/write specs (what it reads, what it writes, which element it asks you to pick). Placement to decide: expand `Workflows_and_Writing_Craft.md`, or one page per element group. Done 2026-09-05 as a `Workflows/` child folder (index plus Outline Gaps, Overview, Problem, Character, World and Setting, Scene pages), nav_order 100.5, after Reviewing Suggestions. Entries sourced from the registry and cross-linked to the Writing with StoryCAD and Story Elements pages.
- [ ] Add Character Interview when #119 merges to `dev`; placeholder entry until then. Placeholder section is in `Workflows/Character_Workflows.md` (2026-09-05).
- [ ] Screenshot per workflow follows the item 3 outline plan.

### 5. Gap analysis and story-development context
At launch Collaborator generates an Outline gaps workflow and builds a context describing where the outline stands (early ideation, mid-story, ...). The manual has one table row on gaps (`Opening_Collaborator.md` line 48) and nothing on the development guess.
Code (`CollaboratorLib/Context/`): `RequiredFieldGapScanner`, `GapDetail`, `GapWorkflowOwnership` (the generated gap workflow); `DevelopmentGuess`, `StoryContextBuilder` (phase labels such as "Early ideation - establishing basic story parameters").
- [x] Explain the Outline gaps workflow: what counts as a gap (required fields), how the count is built, what running it does, why it appears only when gaps exist. Its own section plus an entry in the item 4 list. Done 2026-09-05: `Workflows/Outline_Gaps.md`, with the required-field table.
- [x] Explain the development context: the phases Collaborator recognizes (read the list from `StoryContextBuilder`), how it decides, how that steers suggestions. No guessing at phase names. Done 2026-09-05, same page: the five phases in the order the code checks them, and the Guess sentence.
- [ ] Screenshot the gaps row and count on the item 3 outline, once near-empty and once mid-way. Shot 2 and shot 15.

### 6. Language review: manual voice, not the Rossmann register
The pages read in the Rossmann "punchy" voice: fragments, claim-then-proof paragraphs, aphoristic closers. Example from `Workflows_and_Writing_Craft.md`, "What is a workflow?": "A workflow is a short, focused craft job." ... "One workflow, one craft question. That keeps the work small enough to judge." That register does not fit a user manual. Collaborator #226 (closed) made the same call for the coach's own voice: supportive, keep the mechanics that transfer (concrete detail, plain connectives, varied sentence length), drop the combative register.
- [x] Write down the target voice for this topic before editing. The rest of the manual was written by hand, mostly by Terry, before the no-ai-slop and Rossmann rules were adopted, so it is the reference. Read three or four of those pages (for example under `Story Elements/` and `Writing with StoryCAD/`), describe their style in a short list (sentence shape, paragraph length, how steps and screenshots are introduced, how much explanation precedes an instruction), and adopt that as the standard for this topic. Borrow the transferable rules from #226 (concrete detail, plain connectives, varied sentence length). Record the standard in this file so the item 4 and item 5 additions are written to it from the start.
- [x] Read every page in the topic against that standard and revise: `index.md`, `Getting_Started.md`, `What_Collaborator_Is.md`, `Workflows_and_Writing_Craft.md`, `Opening_Collaborator.md`, `Running_a_Workflow.md`, `Reviewing_Suggestions.md`, `Chat.md`, `Tips_and_Common_Questions.md`, `Tutorial/index.md`, `Tutorial/A_Path_to_Try.md`, `Tutorial/An_Example_Session.md`. Done 2026-09-05: Getting Started under item 1, What Collaborator Is under item 2, Workflows and Writing Craft under item 4; the landing page, Opening Collaborator, Running a Workflow, Chat, Reviewing Suggestions, Tips and Common Questions, Tutorial index, A Path to Try, and An Example Session in the item 6 pass (commits f4ca5a58, a0ba8ad5, and this one).
- [x] Do this pass after items 1 to 5 change the content, or fold it into each page as that page is rewritten, so no page gets revised twice. Folded in; no page was revised twice.


#### Voice standard for the StoryCAD Collaborator topic (written 2026-09-05)

Derived from four hand-written pages: `Writing with StoryCAD/Story_Idea_Concept_and_Premise.md` (Terry, 2022 to 2026), `Writing with StoryCAD/Workflow.md`, `Tutorial Creating a Story/Creating_a_Story_pt_2.md`, and the `Story Elements` reference pages `Problem_Form.md` and `Premise_Tab.md`.

What those pages do:

1. **A teacher talking to one writer.** Second person throughout; the tutorial uses "we" and "let's" as it works alongside the reader. Asides sit in parentheses: "(Don't forget to save.)", "(if you want to know what a button is, just mouse over it.)"
2. **Full sentences, mostly 20 to 35 words, joined with commas, semicolons, and "but" or "and".** Fragments are rare and never used for effect. A paragraph does not end on a one-line verdict.
3. **Paragraphs of two to five sentences.** A page alternates an explanation paragraph with a one-sentence instruction or a screenshot. There are no one-line paragraphs standing alone for emphasis.
4. **Explanation comes before the instruction.** A concept is defined, attributed to a named source (Larry Brooks, Eric Bork, IBM, Deborah Chester), and shown with a well-known story (The da Vinci Code, Jaws, Star Wars, Hamlet) before the reader is told to click anything.
5. **Instructions live inside the prose.** "Click on the Concept tab on your Story Overview and you'll see this:" followed by the screenshot. Numbered step lists are used only where the tutorial has a real sequence to walk. Screenshots follow a sentence that ends in a colon.
6. **Reasons are given.** The tutorial explains why it adds a third character ("policemen often work in teams... with two detectives, there's somebody to talk to") and admits when a choice is provisional ("if we don't need Tony, a touch of the Delete button can always get rid of him").
7. **Permission and reassurance.** "You can fill in as much or little of a story element as you like or need." "These lists are intended to be suggestions, not limitations." "At this point all three characters are stick figures, just names and roles. But that's okay, we're making progress."
8. **Reference pages are short and flat.** One or two paragraphs per tab: what the field is, what it is for, one example drawn from the screenshot ("here, 'Hamlet wants to avenge his father's murder'").
9. **Light formatting.** Bold is rare. UI names appear capitalized as on screen, sometimes in single quotes ('Open story from disk'). Tables are rare; bullets only list things. Contractions are natural and moderate, not a target rate.
10. **Reminders close a section**, not a punchline: "Remember to save your work frequently."

Rules for this topic, taken from the above:

- Write in complete sentences of ordinary length. No fragments for effect, no one-sentence paragraphs used as a closer, no "claim, then proof" paragraph shape.
- Open each section by explaining the idea, then show it, then say what to do. Name the craft source where one exists (Writing with StoryCAD pages, Brooks, Bork) rather than asserting.
- Give the reason for each recommendation in the same paragraph, and say when a choice is optional.
- Put instructions in prose with a screenshot after a colon sentence. Use a numbered list only for a real sequence of clicks the reader performs in order.
- Bold a control name only when the reader is told to click it, at most once per paragraph. Elsewhere, capitalize UI names as they appear on screen. No bold for emphasis.
- Name a workflow exactly as Collaborator's list shows it, every time: Ideation (Story idea => Concept => Premise), Story Problem (Premise => Problem + Characters), Story Form, Inner and Outer Problems, Problem Builder, Define Character, Character Story Function, Flaw and Backstory, Character Relationship, Define Story World, Setting Builder, Scene Builder. No short forms, no "the list shows it as" asides (Terry, 2026-09-05).
- Tables only for reference material (a field list, the label meanings). Not for argument.
- Reassure where the product protects the reader (Has your text, nothing written until accepted) in the tone of item 7 above, without slogans.
- Test a revised page by reading it aloud next to `Creating_a_Story_pt_2.md`. If the Collaborator page sounds clipped or emphatic by comparison, it is not done.

Not adopted from the hand-written pages: empty image alt text, and the occasional typo or unfinished section. Alt text stays descriptive.

### 7. The part and the whole (Terry, 2026-09-05)
Collaborator works on a part: one story element, or a small related set, and its properties. The whole outline improves as the parts do. That is the analysis and synthesis exchange in outlining, and it ties to craft because the manual's craft chapters are written at the story element level.
- [x] Written 2026-09-05 as the section "The part and the whole" in `Workflows_and_Writing_Craft.md`, between "What a workflow is" and the craft table. Four paragraphs: the outline as parts; analysis and synthesis; Collaborator on the analysis side, and why a part can be judged; synthesis stays with the writer, with Problem Builder as the one workflow that reaches across parts.

### Later
- [ ] Rewrite the issue body to match this list; point its status line at this file.
- [x] `Tips_and_Common_Questions.md` "Long lists of updates" still tells the reader to scroll the Property Updates list. Keep only if still true on the labeled build. Reworded 2026-09-05 to a statement true on any build: the list scrolls, and Review Each walks the rows one at a time with a count.
- [ ] Production merge `dev` to `main`, deliberately, when the topic is ready for manual.storybuilder.org.
- [ ] Before that merge, Getting Started has to describe the production path again (Subscribe, free trial, Restore purchases, the Microsoft Store and App Store). The pre-beta wording is in git history at `07741499`.
- [x] Write an ADR from the voice standard above (Terry, 2026-09-05): the manual-wide rule for end-user documentation, on the model of ADR-008 (which set the language standard for workflow prompts). Home: the wiki, under the usermanual repo pages, plus a new "End-user documentation" row in `wiki/topics/writing-standards.md` pointing at it; walk the wiki ADR checklist first. Scope is the whole manual, not the Collaborator topic. Do it after the item 6 pass, which will refine the rules by applying them. Replaces the earlier note that the manual had no written style standard. Done 2026-09-05: wiki `repos/usermanual/adr/adr-001-manual-voice.md` (Accepted), `repos/usermanual/adr/index.md`, a fourth row and review check in `topics/writing-standards.md`, a source page, and a `log.md` ingest entry.

## Findings for the product, not the manual

- **Outline gaps points Setting Summary at Setting Builder, which never writes it.** `GapWorkflowOwnership` maps `(Setting, Description)` to `SettingBuilder`, but Setting Builder's outputs are Period, Locale, Season, Weather, Lighting, Temperature, Props, the four senses, and Notes. A writer who clicks that link runs a workflow that cannot fill the field. The manual describes the app as it is (2026-09-05); file against Collaborator when convenient.
- **Opening Collaborator's Settings table listed Content Preservation**, which #49 removed (Collaborator PR #228, StoryCAD PR #1545, 2026-09-03). Row removed 2026-09-05; the "settings that reset" count went from six to five. The six remaining rows match the dialog headers in `WorkflowShell.xaml.cs` (verified 2026-09-05).
- **A beta build does not reach the dev Worker or the allowlist by itself.** The client defaults to the production Worker URL (`KernelFactory.cs`, `ProxyActivationClient.cs`) and routes activation to the allowlist only when the `COLLAB_DEV_ACTIVATION` environment variable is `1` (`StoreActivationService.IsDevActivationEnabled`); `COLLAB_PROXY_URL` is the only way to point at dev. `IsBetaBuild` sets `BETA_BUILD` (docs preference) and nothing else. A tester installing a package flight today would hit production and the Store purchase flow. Lane A says testers never see purchase UI (`store_submissions.md`, `beta_lane_a_checklist.md`); #97 has to decide how the beta build carries the two settings.
- **While a tester's allowlist row is pending, clicking Collaborator shows the Subscribe dialog.** `CollaboratorService.OpenCollaborator` calls `ShowSubscribeDialogAsync` whenever the state is not Active; on Windows `IsSupported` is always true, so the dialog appears with Subscribe, Restore purchases, and Not now. No UI handles `ActivationState.Refused` and there is no "waiting for approval" message. The manual carries a text placeholder for this moment.
- **Approval takes effect on the next activation refresh.** Refresh runs at startup, on Restore purchases, and on a 401 during a workflow call. A pending tester never reaches a workflow call, so the manual says restart StoryCAD; Restore purchases would also work on a dev-routed build, which is not a sentence a tester should have to read.
- **The out-of-credits message tells testers to buy.** `StoreConfig.OutOfCreditsMessage`: "You've used all your credits for this period. Buy more from Collaborator's Buy Credits screen, or wait for your next monthly renewal." The manual tells testers not to buy and to contact the Foundation. A beta build should say that itself.
- **Ruling (Terry, 2026-09-05):** the onboarding details, including how a tester's build gets the dev-activation setting, get worked out with the first two beta testers rather than designed in advance. If beta feedback requires a StoryCAD change, that means a test deploy or a 4.3 dot release; Getting Started's pending-approval placeholder waits on that.

## State at the end of the 2026-09-05 session

Branch `issue-170-collaborator-manual`, 12 commits ahead of `dev` (`61186daf`), not pushed. Local preview: `bundle exec jekyll serve --port 4000 --livereload` from the StoryCAD root; the watcher on Windows dies every few edits (Ruby iteration and null-byte errors) and a restart fixes it. Done: items 0, 1 (page), 2, 3 (folder and placeholder form), 4 (pages), 5, 6 (standard), 7. Open: item 3 outline decision and screenshots (14 placeholders now), item 6 voice pass over Opening Collaborator, Running a Workflow, Reviewing Suggestions, Chat, Tips, both tutorial pages, the Tutorial index, and the landing page (which still says "additional paid feature" and "free trial"), Character Interview entry after #119, the Later list.

## Tutorial outline (item 3), ruling 2026-09-05

Danger Calls in [Tutorial Creating a Story](../docs/Tutorial%20Creating%20a%20Story/Tutorial_Creating_a_Story.md) is a complete outline built by hand, then shipped as a sample. The Collaborator topic needs the same kind of tutorial: one printed Story Idea, then the workflows in spine order, until the outline is complete. Collaborator proposes; the writer Accepts or Skips. "Help to build" is the product claim, not dump-and-done.

That session is also the acceptance test. Pass means the labeled beta build, against the chosen idea-only outline, can run every registered workflow in the shot-list order and leave a complete outline: Story Idea, Concept, Premise, Story Form, a Story Problem with protagonist and antagonist, Problem Builder beats, an inner problem, Character Story Function, Define Character, Flaw and Backstory, a relationship, Story World, a Setting, and at least one Scene Builder pass on a Problem Builder stub. Fail is a workflow that cannot run, a picker that cannot create what the next step needs, or a write that the writer did not accept.

Two outlines, not one:

| Outline | Role |
|---------|------|
| New tutorial outline | Screenshot source for the 14 workflow/gaps frames, the retakes, and the Collaborator tutorial walk. Starts as Story Idea only. |
| Danger Calls (shipped sample) | [An Example Session](../docs/StoryCAD%20Collaborator/Tutorial/An_Example_Session.md) only. Already has a Story Idea and a Concept, so Ideation comes back Has your text. Do not recapture those three session images against the new outline. |

[A Path to Try](../docs/StoryCAD%20Collaborator/Tutorial/A_Path_to_Try.md) stays the order. The four workflows it leaves for later (Define Character, Character Relationship, Define Story World, Setting Builder) still get shots, because a complete outline and the Workflow Reference both need them. Tutorial pages may need a third walk for the new story; do not collapse it into An Example Session.

Open: pick the Story Idea from the 94-file inventory, or write a new one-sentence idea for the manual. Then run the shot list on a labeled beta build.

## Shot list (item 3), written 2026-09-05

Capture on a labeled beta build against the tutorial outline, in spine order, so one story runs through every image. Window at a consistent size; the Property Updates list in view. Save to `docs/media/Collaborator/` under the file name given; replace the matching placeholder line with a Markdown image whose alt text describes what the frame shows.

### New images (14 placeholders)

| # | File | Page | Frame | Outline state when taken |
|---|------|------|-------|--------------------------|
| 1 | Collaborator-Access-Prompt.png | Getting_Started.md | The Story Collaborator window as it first opens on the fresh outline: workflow list, empty center, chat | New outline, Story Idea only, approved tester |
| 2 | Collaborator-Outline-Gaps.png | Workflows/Outline_Gaps.md | The Outline gaps page with its Guess sentence and the Overview's missing fields as links | Same as 1 |
| 3 | Collaborator-Workflow-Premise.png | Workflows/Overview_Workflows.md | Property Updates after Ideation (Story idea => Concept => Premise): Concept and Premise rows marked New, Story Idea marked Has your text | After 2; before accepting |
| 4 | Collaborator-Workflow-Story-Form.png | Workflows/Overview_Workflows.md | The two rows after Story Form | After accepting 3 |
| 5 | Collaborator-Workflow-Story-Problem.png | Workflows/Overview_Workflows.md | Property Updates after Story Problem (Premise => Problem + Characters): Problem rows and one Name row per created character | After 4; Problem and two characters created in the pickers |
| 6 | Collaborator-Workflow-Problem-Builder.png | Workflows/Problem_Workflows.md | Property Updates after Problem Builder: Problem fields above, one row per beat below | After accepting 5; Problem Category set |
| 7 | Collaborator-Workflow-Inner-Outer.png | Workflows/Problem_Workflows.md | Property Updates after Inner and Outer Problems: inner Problem rows and the protagonist's Flaw row | After 6; inner Problem created in the picker |
| 8 | Collaborator-Workflow-Story-Function.png | Workflows/Character_Workflows.md | The three rows after Character Story Function on the protagonist | After 7 |
| 9 | Collaborator-Workflow-Define-Character.png | Workflows/Character_Workflows.md | Property Updates after Define Character, scrolled to show the list's length | After 8, same character |
| 10 | Collaborator-Workflow-Flaw-Backstory.png | Workflows/Character_Workflows.md | The two rows after Flaw and Backstory on the antagonist, whose Flaw tab is empty | After 9, antagonist |
| 11 | Collaborator-Workflow-Relationship.png | Workflows/Character_Workflows.md | The relationship row after Character Relationship, with the resulting Relationships tab entry in StoryCAD beside it | After 10, protagonist and antagonist |
| 12 | Collaborator-Workflow-Story-World.png | Workflows/World_and_Setting_Workflows.md | Property Updates after Define Story World on a StoryWorld created in the picker | Any point after 5 |
| 13 | Collaborator-Workflow-Setting-Builder.png | Workflows/World_and_Setting_Workflows.md | Property Updates after Setting Builder: Setting tab rows above, the four senses below | A Setting created for the story, any point after 5 |
| 14 | Collaborator-Workflow-Scene-Builder.png | Workflows/Scene_Workflows.md | Property Updates after Scene Builder on a stub Problem Builder created | After 6 |
| 15 | Collaborator-Outline-Gaps-Midway.png | Workflows/Outline_Gaps.md | The Outline gaps row and count after Problem and cast exist, with remaining required fields still listed | After 5, before 12–14 have filled world/setting/scene |

Item 5 asked for gaps twice: shot 2 is near-empty, shot 15 is mid-way. Shot 15 has no placeholder on the page yet; add it when capturing.

Text placeholder, not a screenshot: Getting_Started.md, what the app shows between first launch and approval. Waits on the #97 onboarding decisions.

### Existing images (16 referenced), retake on the chosen outline or keep

| File | Pages | Decision |
|------|-------|----------|
| Collaborator-Toolbar-Button.png | Getting Started, Opening | Keep; outline-independent |
| Collaborator-With-StoryCAD.png | What Collaborator Is | Retake with the chosen outline visible in both windows |
| Collaborator-Workflow-List.png | Workflows and Writing Craft, Workflow Reference | Retake; must show the current 12 workflows in six groups |
| Collaborator-Window-Overview.png | Opening | Retake on the chosen outline after a run |
| Collaborator-Workflow-Pane.png | Opening, A Path to Try | Retake; must show the five current default stars and Story World group |
| Collaborator-Customize-Workflows.png | Opening | Retake; current workflow set |
| Collaborator-Pane-Collapsed.png | Opening | Retake on the chosen outline |
| Collaborator-Element-Picker.png | Running a Workflow | Retake during shot 5 (Select Character with Create a new element) |
| Collaborator-Updates-After-Run.png | Running a Workflow | Retake; can be the same frame as shot 3 or 4 |
| Collaborator-Property-Updates.png | Reviewing Suggestions | Retake with a mixed header (some free, some need review), e.g. a second Ideation run |
| Collaborator-Row-New.png | Reviewing Suggestions | Retake; one row labeled New |
| Collaborator-Review-Each.png | Reviewing Suggestions | Retake during a Review Each pass on shot 6 |
| Collaborator-Chat.png | Chat | Retake with a question about a proposal from the chosen outline |
| Collaborator-Session-Overview.png, -Premise.png, -ProblemBuilder.png | An Example Session | Keep if the tutorial stays on Danger Calls; the session page is the one place the shipped sample appears (item 3 recommendation) |
| Collaborator-Session-Problem.png | none | Delete; unreferenced |

### The Scorecard outline and the session protocol

- Source file: `G:\My Drive\2-Areas\Writing\Projects\0211 Scorecard\0211 Scorecard.stbx`, 2,907 bytes, JSON text (diffable), last saved by StoryCAD 3.2.1.0; StoryCAD 4.x will migrate the file format on first open, so a copy taken before that open is the only pre-migration copy. Contents: Overview with the Story Idea, an empty folder, the trash. Nothing else.
- Fact for that protocol: StoryCAD stops its timed backup and autosave while Collaborator holds the model (`CollaboratorService.OpenCollaborator`) and restarts them when Collaborator closes, so the backup taken before a session is the only one until the session ends.

### Backup and session protocol (Terry, 2026-09-05; recorded here after a check found no other record)

Collaborator writes to the outline on every accept, and the sessions are also a test of Collaborator on a real outline, so each session starts from a copy that can be returned to and ends with a copy that shows what the session did.

- Working folder (Terry, 2026-09-05): `C:\temp\0211 Scorecard\`, a copy of the project folder. The sessions open `0211 Scorecard.stbx` there, converted to the 4.1.0.0 file format on 2026-09-05 (3,363 bytes). Session copies go in its `manual-sessions\` subfolder and nowhere else. The original on G: is not opened again. The Scrivener project beside it is empty.
- StoryCAD's own backups (the Backup on open preference, which Terry turns on for these sessions, and the timed backup) stay in the default Backups folder on G:. They are a second net, not the record: the session copies are named by session, the zips are named by time among some 5,200 others.
- `manual-sessions\Scorecard-S00-baseline.stbx`: byte-identical copy of the original 3.2.1-format file (2,907 bytes), taken 2026-09-05 before the first 4.x open. Never edited.
- Session NN, in this order: (1) copy `0211 Scorecard.stbx` to `manual-sessions\Scorecard-SNN-before.stbx`; (2) open it in StoryCAD, open Collaborator, run the spine step for that session, take the shots the shot list assigns to it, accept as the manual describes, Exit, save in StoryCAD; (3) copy to `manual-sessions\Scorecard-SNN-after.stbx`; (4) fill the row below. A retake of any shot starts from that session's before copy. StoryCAD's own timed backup and autosave are stopped while Collaborator holds the model, so the before copy is the only backup until the session ends.
- One spine step per session, matching the shot list: S01 open and Outline gaps (shots 1, 2) and Ideation (shot 3); S02 Story Form (4); S03 Story Problem (5, Element Picker retake); S04 Problem Builder (6, Review Each and Property Updates retakes); S05 Inner and Outer Problems (7); S06 the character workflows (8 to 11); S07 Define Story World and Setting Builder (12, 13); S08 Scene Builder (14); S09 the remaining retakes (window, list, pane, Customize Workflows, collapsed pane, Chat, With StoryCAD).
- Bugs found go to Collaborator issues as they appear, with the session number and the before copy named in the issue, since the before copy reproduces the state.

| Session | Date | Build | Step | Shots taken | Before | After | Notes / issues |
|---|---|---|---|---|---|---|---|
| S00 | 2026-09-05 | none | Baseline copy, then format conversion | none | `Scorecard-S00-baseline.stbx` (3.2.1 format, 2,907 bytes) | `0211 Scorecard.stbx` in `C:\temp\0211 Scorecard\` (4.1.0.0, 3,363 bytes) | Converted by opening in StoryCAD; Scrivener project empty, nothing to convert |

### Observations from the sessions

Collaborator issues found while taking the screenshots are tracked in Collaborator #236 (private repo), one line per finding, each becoming its own issue when confirmed. The S01 screenshots are in `manual-sessions\`.
