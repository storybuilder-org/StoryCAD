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
- [x] Write down the target voice for this topic before editing. The rest of the manual was written by hand, mostly by Terry, before the no-ai-slop and Rossmann rules were adopted, so it is the reference. Read three or four of those pages (for example under `Story Elements/` and `Writing with StoryCAD/`), describe their style in a short list (sentence shape, paragraph length, how steps and screenshots are introduced, how much explanation precedes an instruction), and adopt that as the standard for this topic. Borrow the transferable rules from #226 (concrete detail, plain connectives, varied sentence length). Record the standard in this file so the item 4 and item 5 additions are written to it from the start.
- [ ] Read every page in the topic against that standard and revise: `index.md`, `Getting_Started.md`, `What_Collaborator_Is.md`, `Workflows_and_Writing_Craft.md`, `Opening_Collaborator.md`, `Running_a_Workflow.md`, `Reviewing_Suggestions.md`, `Chat.md`, `Tips_and_Common_Questions.md`, `Tutorial/index.md`, `Tutorial/A_Path_to_Try.md`, `Tutorial/An_Example_Session.md`.
- [ ] Do this pass after items 1 to 5 change the content, or fold it into each page as that page is rewritten, so no page gets revised twice.


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
- Tables only for reference material (a field list, the label meanings). Not for argument.
- Reassure where the product protects the reader (Has your text, nothing written until accepted) in the tone of item 7 above, without slogans.
- Test a revised page by reading it aloud next to `Creating_a_Story_pt_2.md`. If the Collaborator page sounds clipped or emphatic by comparison, it is not done.

Not adopted from the hand-written pages: empty image alt text, and the occasional typo or unfinished section. Alt text stays descriptive.

### Later
- [ ] Rewrite the issue body to match this list; point its status line at this file.
- [ ] `Tips_and_Common_Questions.md` "Long lists of updates" still tells the reader to scroll the Property Updates list. Keep only if still true on the labeled build.
- [ ] Production merge `dev` to `main`, deliberately, when the topic is ready for manual.storybuilder.org.
- [ ] Separate from #170: the manual has no written style standard. The writing-standards page covers design specs (ASD-STE100) and human-facing prose (no-ai-slop, Rossmann) and says nothing about end-user documentation. The item 6 description of the hand-written pages is the seed. File as its own issue when the Collaborator topic is done; do not widen #170 to the whole manual.
