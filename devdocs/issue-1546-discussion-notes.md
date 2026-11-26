# StoryCAD issue 1546: notes from the discussion of 2026-11-25

These notes record what Terry Cox decided about StoryCAD issue 1546 in a working session on 2026-11-25, and what is still open. Issue 1546 is titled "Beat tree as narrative structure: cycle guard, unplaced elements, API, and Plot Board window". It has not been rewritten yet. These notes are the input for the mock-up and then for the rewrite.

This file is not a design. Nothing here is approved for building.

## What is wanted

Terry's statement of the aim, 2026-11-25: to develop a plot from several Problems that in the end break down into Scenes.

- A beat sheet is a help toward that aim. It is not the aim. Fitting a plot too closely to a beat sheet can make the story formulaic.
- StoryCAD's use of beats and beat sheets does not have to match any one book or article.

## Order of work

1. Build a mock-up. It is a web page outside the code repos, so it is not a change to StoryCAD. It covers the beat tree window and the Structure tab, because both change.
2. Use the mock-up and change it until the behaviour feels right. The use decides which rules are needed.
3. Rewrite issue 1546 in the new format: what is wanted, why, and what "done" means.
4. Write the rules into the design, part by part.

The better drawing of the beat tree is part of step 1. The mock-up serves as the drawing. It is the file `issue-1546-mockup.html` in this folder, and it shows the sample outline The Old Man and the Sea, read from the `.stbx` file that ships with StoryCAD.

One worktree holds the work of this issue: the folder `StoryCAD-1546`, on the branch `issue-1546-beat-tree`.

## Words used in these notes

| Word | Meaning |
|------|---------|
| Part | A Scene or a Problem. The rewritten issue says "element" instead (Terry, 2026-11-26: watch the wording, because some of it will move to ASD-STE100). The Structure tab uses "element" today. |
| Beat sheet | A list of beats, such as Save the Cat. A Problem has one at most. |
| Beat | One row of a beat sheet. It holds one part, or nothing. |
| Card | A beat as drawn on the screen. |
| Column | One Problem as drawn on the beat tree: a header, its cards, and its plain collection. |
| Plain collection | The parts that belong to a Problem but are on no beat. Terry also called it an unnamed beat sheet. |
| Assigned | The part belongs to a Problem. It is in that Problem's column, on a beat or in the plain collection. |
| Unassigned | The part belongs to no Problem. |
| Story Problem | The one Problem named on the Story Overview form as the main problem of the story. |

## The beat tree window

The beat tree is a new screen. It shows the whole structure of the story at once, which no screen does today.

- Each Problem is a column of cards.
- The first column is the Story Problem. It is the root of the tree.
- A card holds a Scene, or a Problem, or nothing.
- When a card holds a Problem, that Problem gets its own column. A line joins the card to that column.
- For now, any Problem can hold other Problems, so the tree can have more than two levels. Whether only the Story Problem may hold Problems is still open.
- Columns have a fixed width. The area that holds them scrolls sideways.
- The window is separate from the main window and can fill a whole screen. The Collaborator window is made the same way today.

### How to think of the tree

Terry's statement of it: a subproblem is a set of beats of its own, and the size of the beat that holds it is the sum of the sizes of its beats. This repeats at every level. In the end, the plot is a tree of Scenes.

- Problems are the branches and Scenes are the leaves.
- A Scene can cover more than one beat, so the same Scene can be a leaf in more than one place.
- Reading the leaves of one Problem in order explains the plot of that Problem. It does not give the order in which the story is told.
- The order of telling belongs to the Narrator view, the second of StoryCAD's two navigation trees. The two trees exist because the order of presentation can differ from the structure.
- Within the order of telling, the Scenes of any one Problem are still in the order of that Problem's beats. A flashback is the likely exception.
- A writer mixes Scenes from different Problems in the telling. Terry's picture of it is a stovetop with several burners: the writer switches from one Problem to another to braid the Scenes, and leaves the reader in suspense on the Problem just left. Each column of the beat tree is one burner. The braid is made in the Narrator view.

### The column header

- The Problem's title.
- The Problem's category: Story problem, Complication, Subplot or Sequence.
- A dropdown to choose the beat sheet.

### The plain collection

- A column does not need a named beat sheet. It can still hold Scenes and Problems.
- The reason: when the writer or the AI puts a part in a column, they had a reason to think it belongs to that Problem. That decision is kept.
- A column with a named beat sheet has its cards, plus the plain collection for parts not yet on a beat.

### Changing the beat sheet

- The parts stay in the column. None is lost.
- StoryCAD does not guess which beat of the new beat sheet a part belongs on. Each part shows as on no beat until someone moves it.
- The parts on no beat keep the order they had before the change. That order is only how they are listed. Some parts will fit no beat of the new beat sheet and stay in the plain collection.

### The panel on the right

- It lists every Scene and every Problem in the outline, as the right-hand list of the Structure tab does today.
- A part that is already used looks different from one that is not. How it is marked is open. It is not marked by colour alone, because a writer who cannot see colour, or who uses a screen reader, would miss it.
- The panel stays in place while the columns scroll.

### Moving parts

- The writer puts a part on a card, and rearranges parts, by drag and drop.
- Every action also has a button, for a writer who uses only the keyboard. The Structure tab has these buttons today.
- A Scene can be on more than one beat of the same Problem, and on beats of different Problems. The manual says so and the code allows it. The line in issue 1546 that limits a Scene to one beat for each Problem is wrong and comes out.
- A Problem can be on one beat only, of one Problem.
- Taking a part off a card leaves it in the column, on no beat. On the Structure tab today, the same button drops the part from the Problem.

### Changing the beats of a column

- The writer can still add, delete, rename and reorder the beats of a Problem after choosing its beat sheet. The Structure tab does this today, so the code needs less change.
- The up and down buttons move the selected beat, with whatever it holds, as they do today.
- To move a Scene or Problem to a different card while the beats stay in place, the writer drags it.
- A card can show the beat's description. A switch turns the descriptions on and off.

### Popups

| What the writer does | What happens |
|----------------------|--------------|
| Puts a Scene on a card when the Scene is already on another card in the same column | A popup asks: move it, put it here as well, or cancel. |
| Puts a Scene in a column when it is already in another Problem's column | No popup. This is how two Problems are tied together. |
| Puts a Problem on a card when it is already on a card somewhere | The popup that exists today, "Already assigned!", asks whether to assign it here instead or cancel. |

## The Structure tab

A column on the beat tree and the Structure tab of a Problem form show the same thing for one Problem. Terry's rule, in his words: "Do one thing one way, so that there is less confusion."

- The plain collection shows on the Structure tab too.
- Each action works the same in both places: the arrow buttons, drag and drop, and the popups.
- The Structure tab keeps its right-hand list of Scenes and Problems, with used parts marked the same way as in the panel of the beat tree window.
- The Problem's column and the right-hand list sit side by side. Together they must fit in the Content pane of the main window, with the navigation tree to their left.

## Access for the AI

- Everything the writer can do on the beat tree, an AI can also do.
- That covers Collaborator, and outside AI tools that reach an outline through the StoryCAD MCP server (the program in the StoryCADAPI repo that lets an AI tool read and change an outline).
- So each action lives in the layer underneath the screen, where the screen and the AI both call it.
- An AI cannot answer a popup. The layer therefore has "move" and "put here as well" as two separate actions.
- The Collaborator and MCP work may be a separate issue. Issue 1546 still builds the layer they depend on.

## How the work is divided

Decided by Terry on 2026-11-26.

- Issue 1546 holds the StoryCAD part. A new issue holds the Collaborator part: a new workflow that uses the beat tree. The Collaborator issue can be written and designed early, and it is built after issue 1546 delivers the layer it depends on.
- Issue 1546 starts with the new beat tree, built from the mock-up. If that is one pull request, that is fine.
- The Structure tab of the Problem form works like one Problem's column on the beat tree. The two use the same view model and share code.
- The first pull request carries the list of parts for each Problem, the shared view model, the beat tree window, and a guard that stops the drawing of the tree when it meets a Problem it has already drawn.
- The AI's access to the new actions comes in the next pull request of issue 1546, not the first. This sets aside, for the first pull request, the rule in Terry's comment of 2026-11-01 that a change to the layer gets its screen path and its AI path in the same pull request.
- The fixes do not come first. Done first, they would fix a product that is about to change. They follow the beat tree, as whatever is needed to protect it and to check it.

## The second goal: Collaborator runs the beat tree as a workflow

Terry named two main goals. The first is the beat tree screen. The second is that Collaborator, StoryCAD's AI helper, can work through the beat tree as one of its workflows.

What Collaborator has today, checked on 2026-11-25 in the Collaborator repo (`proxy/src/index.js`) and in `CollaboratorLib/WorkflowInputBuilder.cs`:

- A workflow that fills the beats of one Problem. It leaves a filled beat alone. For an empty beat it first looks for an existing Scene or Problem that fits. When none fits, it writes a stub: a new Scene or a new Problem with a name and a description, for the writer to accept or skip.
- The Scene workflows already look at every Problem whose beats hold the Scene. They treat one as the Scene's own Problem and the others as contributing Problems.

Stubs are worth considering for the beat tree: an empty card could be filled with a stub Scene or a stub Problem, by the AI or by the writer.

A task for later: read what the literature says about using beat sheets, and see whether it gives rules or features worth having. Terry recalls that Robert McKee's book Story has a page or two on beats and the length of a story. This has not been checked against the book. One link between beats and length is already in StoryCAD: the beat titles of its Save The Cat beat sheet carry the share of the story where each beat falls, such as "Catalyst: 10%" and "Midpoint: 50%".

First source read, on 2026-11-25: the AutoCrit article "Story Beats: How to Use a Beat Sheet (With Download)", at `https://www.autocrit.com/story-beats-beat-sheet/`. It shows no author and no date.

- It treats beats as the major events of the plot, and Scenes as what fills the space between them. One beat can cover several Scenes. That is the opposite direction from one Scene covering several beats.
- Its own template has 20 beats in three acts. Four of them are optional.
- A subplot enters at a beat named B Story and is resolved at a later beat. A subplot has its own beginning, middle and end.
- It suggests a second beat sheet for a character, laid over the beat sheet of the plot.
- The writer places the main beats first, then adds the Scenes between them. A beat sheet can also be used on a finished draft, to find a weak middle or a weak ending.
- It gives no page counts or word counts for beats, and it does not mention Robert McKee.

## What the code does today

Checked on 2026-11-25 against the StoryCAD repo (files equal to `origin/dev` of 2026-11-24) and the StoryCADAPI repo (`main`, 2026-11-18).

### Already there

- A beat stores a title, a description, and one Scene or one Problem or nothing (`StoryCADLib/ViewModels/Tools/StructureBeat.cs`).
- The code refuses to put the Story Problem on a beat, and sets no limit on depth (`AssignElementToBeat` in `StoryCADLib/Services/Outline/OutlineService.cs`).
- A Problem has a category with the four choices above (`ProblemCategory` in `StoryCADLib/Assets/Install/Lists.json`).
- The Structure tab has two lists, seven buttons and two description boxes (`StoryCAD/Views/ProblemPage.xaml`).
- The "Already assigned!" popup for a Problem (`AssignBeatAsync` in `StoryCADLib/ViewModels/SubViewModels/BeatSheetsViewModel.cs`).
- Twelve beat actions for the AI (`StoryCADLib/Services/Collaborator/Contracts/IStoryCADAPI.cs`), offered to outside tools as eleven MCP tools.
- Three printed reports of the structure, one of which walks every beat sheet from the Story Problem down.

### Not there

- Nothing records that a part belongs to a Problem without being on a beat.
- A Problem with no named beat sheet can hold nothing. The Structure tab refuses to add a beat until a beat sheet is chosen.
- Parts are dropped when a beat sheet changes. The warning popup says "This will clear selected story beats".
- There is no beat tree screen, and no drag and drop for beats.
- There is no popup for a Scene that is already on another beat.
- The AI has no single move action, and nothing for the plain collection.
- Nothing keeps two windows in step. The Problem form holds its own copy of the beat sheet's name and description, and of the record of which Problem's beat sheet this Problem is on. It writes them back when the writer leaves the form.

### Faults

- A Problem can be put on a beat of a Problem below it. That makes a loop in the tree.
- There are four ways to change a beat sheet, and they do not agree. Issue 1546 counts three. The one it misses is loading a beat sheet from a file on the Structure tab (`LoadBeatSheet` in `BeatSheetsViewModel.cs`). Three of the four leave a child Problem still recording that it is on the parent's beat sheet when no beat holds it.
- On the Structure tab, adding a beat and moving a beat up or down change the list directly and do not go through the shared layer.
- The Story Overview form names the Story Problem, and separately any Problem's category can be set to Story problem. Nothing keeps the two in agreement.

## Corrections to the problem statement of issue 1546

- "Three code paths replace a sheet" is wrong. There are four.
- "Under the folders template every Scene sits in a Scenes folder": the templates only create an empty Scenes folder.
- "No view shows the whole tree": no screen does, but a printed report does.
- "A walk of the beat tree does not terminate": true only of a walk with no guard. The report that walks the tree today has a guard.
- "The same twelve beat operations": twelve in the API, eleven MCP tools.

## Still open

### Before the issue is rewritten

1. Answered: the second main goal is that Collaborator runs the beat tree as a workflow. What that workflow does, step by step, is not decided.
2. Answered on 2026-11-26, for now: any Problem can hold other Problems. Terry's example is the screenplay of The Wizard of Oz. The Story Problem is Dorothy getting back to Kansas. The trip to the Emerald City is a Problem under it, and the poppies and the like are Problems under that. The argument against: with too many levels the reader or viewer can get lost.
3. Answered on 2026-11-26, for now: a child Problem that loses its beat stays in its parent's column, as a Scene does. It follows from the answer above, and it has the same arguments for and against.
4. Answered on 2026-11-26. Terry's comment of 2026-11-01 on issue 1546 is where the idea came from: keep the Scenes and Problems of a Problem when its beat sheet changes, or when it has no beat sheet. The comment is an early form of what is decided in these notes. Where the two differ, these notes hold.
5. Answered on 2026-11-26. See "How the work is divided". The pieces are the fixes, the plain collection with the layer for the AI, the changed Structure tab, the beat tree window, and the Collaborator and MCP work.
6. Whether two Collaborator items are in or out: showing an AI proposal as ghost cards on the beat tree, and having the Problem Beats workflow keep the extra Scenes it finds.
7. Answered on 2026-11-26: all of issue 1546 is for Release 4.4.

### For the mock-up to settle

- The better drawing.
- Where a child Problem's column goes when that Problem is on no beat.
- Whether a Problem that belongs to no other Problem, but has its own beat sheet, shows on the beat tree.
- How a used part is marked.
- The wording of the popups.

### Result of the mock-up

Terry tried the mock-up on 2026-11-26. Drag and drop works and the overall look is good. It is a good start. Things it does not cover, for the issue and the design to take up:

- Actions on a right-click of a card.
- How the writer sees the details of a beat.
- The header of the window and its status bar.
- The arrow keys of the keyboard.
- A test with real outlines, which needs a working version in StoryCAD.

### The issue rewrite and its review

- The new body and title of issue 1546 were posted on 2026-11-26. The text is also in `issue-1546-rewrite-draft.md` in this folder. The body it replaced is in `issue-1546-original-body.md`.
- A reviewer session checked the issue the same day. Its whole report is `devdocs/issue-1546-issue-review.md` in the repo `storybuilder-miscellaneous`. It found two blocking points. Both are answered in the issue as it now stands.
- First blocking point: a child Problem that belongs to its parent on no beat is in the beat tree, gets a column, and counts for the loop check.
- Second blocking point, decided by Terry on 2026-11-26: when a beat is deleted, the element on it stays in the Problem, on no beat. The delete popup gives the writer the choice: keep the element, remove it from the Problem, or cancel. Through the API, the element is kept, and the AI removes it with the remove action.
- Terry expects that keeping the element may cause problems, and said they relate to "used". The issue now tells three states apart: not used, used and on a beat, and used but on no beat. The list on the right and the Unassigned Elements report both show the third state. Whether this covers the problems Terry has in mind is not confirmed.
- The review corrected one claim in these notes: the code does keep the two markers of the Story Problem in step, in three places, but not fully.
- The issue says "element" where these notes say "part".
- Terry accepted the issue on 2026-11-26 and ticked the box on GitHub.
- The Collaborator issue is not written yet. Terry, 2026-11-26: written now, it would likely need changing after the StoryCAD screen of issue 1546 is up. It waits for that screen.

### For the design

- How existing outlines get their plain collections when first opened.
- How the two windows stay in step.
- The two markers for the Story Problem.
- Sharing code between a column of the beat tree and the Structure tab. The logic of the Structure tab is already in one class, `BeatSheetsViewModel`, and the beat tree could use one copy of it for each column. The layout is written inside `ProblemPage.xaml` and would move into a control that both screens use. Three things in the class would change: its own copies of the beat sheet's name and description, the window its popups appear on, and its own list of Scenes and Problems.
