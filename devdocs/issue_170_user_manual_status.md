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
- [ ] `Getting_Started.md` "Turn it on": steps 1 to 4 offer Subscribe or Restore purchases, then a sentence on trial length and price. Replace with the beta path: invited testers, no purchase, the Foundation covers the AI cost.
- [ ] `Getting_Started.md` line 20 and `Tips_and_Common_Questions.md` "Connection and access" both say "additional paid feature with a free trial." Reword for beta.
- [ ] `Getting_Started.md` leftover placeholder `<!-- image: subscribe or start trial prompt -->`: drop, or replace with what a beta tester sees.
- [ ] Landing page and `What_Collaborator_Is.md`: check the "paid plug-in, dual store, free trial" framing against beta wording.
- [ ] Verify on the beta build before writing: what a tester sees when they click Collaborator, and whether the store dialog appears at all.

### 2. What Collaborator Is: strengthen "What it is not"; cite the AI use policy
- [ ] `What_Collaborator_Is.md` "## What it is not" (line 29, a short bullet list today). Reinforce it.
- [ ] Same page: state the Foundation's AI use policy and link https://storybuilder.org/ai-usage-policy/. Read the live page first so the wording matches.

### 3. Image folder and placeholder convention
Today 17 `Collaborator-*.png` files sit loose in `docs/media/` (151 files total; existing subfolders `Elements/`, `Tutorial/`). Pages reference them as `../media/Collaborator-X.png`.
- [ ] Create `docs/media/Collaborator/`, move the 17 files, fix every image path (`../media/Collaborator/...`; Tutorial pages `../../media/Collaborator/...`).
- [ ] One placeholder form for an image not yet captured: a visible line in the page, not an HTML comment. Example: `*[Screenshot to come: <what it shows>]*` plus the intended file name.
- [ ] Outline plan (open, Terry): one outline or several; start near-empty and follow Collaborator filling it in, or use the shipped sample (Danger Calls already has a Story Idea and Concept). Recommendation on the table: one fresh outline from a single Story Idea sentence printed in the manual, screenshots taken in spine order from that one session; Danger Calls appears once, where "Has your text" is explained.
- [ ] Then list every screenshot the topic needs, capture on a labeled beta build against that outline, replace placeholders.

### 4. Describe every workflow
The manual counts workflows inconsistently (`Opening_Collaborator.md`: "about twenty", seven default stars; `Tutorial/A_Path_to_Try.md`: thirteen, five stars) and describes none individually.
Registry on `dev` (counted 2026-09-05, `CollaboratorLib/Workflows/WorkflowRegistry.cs`, 12 `new Workflow(`): Premise (Ideation), StoryProblem, StoryForm, InnerOuterProblems, ProblemBuilder, DefineCharacter, StoryFunction (Character Story Function), FlawBackstory, Relationship, DefineStoryWorld, SettingBuilder, SceneBuilder. Collaborator #119 adds CharacterInterview (13). #224 (2026-09-01) folded SettingTimeSpace and Sensations into SettingBuilder, which is why "thirteen" (written 2026-08-23) is now 12. Default stars: Premise, StoryProblem, ProblemBuilder, StoryFunction, SceneBuilder.
- [ ] Fix the counts everywhere. Prefer wording that does not go stale, or one count in one place.
- [ ] One entry per registered workflow, sourced from the registry `title`, `description`, `explanation`, and the gather/write specs (what it reads, what it writes, which element it asks you to pick). Placement to decide: expand `Workflows_and_Writing_Craft.md`, or one page per element group.
- [ ] Add Character Interview when #119 merges to `dev`; placeholder entry until then.
- [ ] Screenshot per workflow follows the item 3 outline plan.

### 5. Gap analysis and story-development context
At launch Collaborator generates an Outline gaps workflow and builds a context describing where the outline stands (early ideation, mid-story, ...). The manual has one table row on gaps (`Opening_Collaborator.md` line 48) and nothing on the development guess.
Code (`CollaboratorLib/Context/`): `RequiredFieldGapScanner`, `GapDetail`, `GapWorkflowOwnership` (the generated gap workflow); `DevelopmentGuess`, `StoryContextBuilder` (phase labels such as "Early ideation - establishing basic story parameters").
- [ ] Explain the Outline gaps workflow: what counts as a gap (required fields), how the count is built, what running it does, why it appears only when gaps exist. Its own section plus an entry in the item 4 list.
- [ ] Explain the development context: the phases Collaborator recognizes (read the list from `StoryContextBuilder`), how it decides, how that steers suggestions. No guessing at phase names.
- [ ] Screenshot the gaps row and count on the item 3 outline, once near-empty and once mid-way.

### 6. Language review: manual voice, not the Rossmann register
The pages read in the Rossmann "punchy" voice: fragments, claim-then-proof paragraphs, aphoristic closers. Example from `Workflows_and_Writing_Craft.md`, "What is a workflow?": "A workflow is a short, focused craft job." ... "One workflow, one craft question. That keeps the work small enough to judge." That register does not fit a user manual. Collaborator #226 (closed) made the same call for the coach's own voice: supportive, keep the mechanics that transfer (concrete detail, plain connectives, varied sentence length), drop the combative register.
- [ ] Write down the target voice for this topic before editing: plain instructional prose that matches the rest of the StoryCAD manual; complete sentences; explain, then show; no aphorisms, no one-line paragraphs used for effect, no contempt-through-precision. Borrow the transferable rules from #226. Record the standard in this file so the item 4 and item 5 additions are written to it from the start.
- [ ] Read every page in the topic against that standard and revise: `index.md`, `Getting_Started.md`, `What_Collaborator_Is.md`, `Workflows_and_Writing_Craft.md`, `Opening_Collaborator.md`, `Running_a_Workflow.md`, `Reviewing_Suggestions.md`, `Chat.md`, `Tips_and_Common_Questions.md`, `Tutorial/index.md`, `Tutorial/A_Path_to_Try.md`, `Tutorial/An_Example_Session.md`.
- [ ] Do this pass after items 1 to 5 change the content, or fold it into each page as that page is rewritten, so no page gets revised twice.

### Later
- [ ] Rewrite the issue body to match this list; point its status line at this file.
- [ ] `Tips_and_Common_Questions.md` "Long lists of updates" still tells the reader to scroll the Property Updates list. Keep only if still true on the labeled build.
- [ ] Production merge `dev` to `main`, deliberately, when the topic is ready for manual.storybuilder.org.
