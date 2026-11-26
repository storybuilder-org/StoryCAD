## Problem Statement

A writer develops a plot from several Problems that in the end break down into Scenes (Terry, 2026-11-25). The Story Problem has beats. A beat holds a Scene, or a smaller Problem with beats of its own, and so on down. A beat sheet, such as Save The Cat, is a help toward that. It is not the aim.

Words used here:

| Word | Meaning |
|------|---------|
| Element | A Scene or a Problem. The Structure tab uses this word today for the list on its right-hand side. |
| Beat | One row of a Problem's beat sheet. It holds one element, or nothing. |
| Belongs to | An element belongs to a Problem when it is on one of that Problem's beats, or is held by that Problem on no beat. |
| On no beat | An element that belongs to a Problem but is on none of its beats. |
| Under | A Problem is under the Problem it belongs to, and under every Problem above that one. |
| Beat tree | The Story Problem, every Problem under it, and their beats. A Problem that belongs to its parent on no beat is in the beat tree. |
| Story Problem | The Problem that the Story Overview form names as the Story Problem. |
| Category | A Problem's Problem Category: Story problem, Complication, Subplot or Sequence. |
| Used | An element is used when it belongs to at least one Problem. A used element is in one of two states: on a beat of some Problem, or on no beat of every Problem it belongs to. |
| Content pane | The right-hand side of the main window, where the form of the selected element shows. |

StoryCAD gets in the way of this work in four places.

1. **No screen shows the whole structure.** The Structure tab of the Problem form shows one Problem. To see how the Problems fit together the writer prints the Story Problem Structure reports.
2. **Changing a beat sheet throws the work away.** The Scenes and Problems that were on the old beat sheet are dropped, and StoryCAD keeps no record of them. The warning says "This will clear selected story beats". A writer who wants to try a different beat sheet has to assign everything again. Deleting one beat drops the element on it in the same way.
3. **A Scene can belong to a Problem only by being on a beat.** A Problem with no beat sheet can hold nothing. A writer who knows which Scenes belong to a Problem, but has not chosen a beat sheet, has nowhere to say so.
4. **The structure is not protected.** A Problem can be put on a beat of a Problem under it, which makes a loop. A writer or an AI can change a beat sheet in four ways and they do not agree: three of them leave a child Problem recording a parent when no beat of that parent holds it.

Collaborator, StoryCAD's AI helper, and outside AI tools reach beats through the same code. They meet the same four problems.

## Proposed Solution

### A list of elements for each Problem

Each Problem keeps the Scenes and Problems that belong to it. An element is on one of the Problem's beats, or on no beat.

- Putting an element on a beat makes it belong to the Problem. A writer who starts from a beat sheet does nothing extra.
- An element can be given to a Problem with no beat. A Problem with no beat sheet can hold elements.
- Taking an element off a beat leaves it in the Problem, on no beat. A separate action removes it from the Problem.
- Changing the beat sheet keeps every element. The elements that were on beats go on no beat, after any that were there already, in the order of the old beats. A Scene that was on two beats is listed once. StoryCAD does not guess which new beat an element fits. Choosing the same beat sheet again counts as a change. So does the first choice of a beat sheet for a Problem that already holds elements.
- A Scene can be on more than one beat of a Problem, and in more than one Problem. The manual says so today.
- A Problem belongs to one Problem at most, and is on one beat at most. Any Problem can hold other Problems, so the tree can have more than two levels (Terry, 2026-11-26).
- The Story Problem belongs to no Problem.
- The writer can still add, delete, rename and reorder the beats of a Problem. Deleting a beat that holds an element asks the writer to choose: keep the element in the Problem, on no beat, or remove it from the Problem (Terry, 2026-11-26). Today the delete popup only warns that the assignment is removed.

An element on no beat is still used, and Terry expects problems from that (2026-11-26). The risk this issue guards against: an element left on no beat counts as used, so a writer who looks for Scenes that still need a place does not find it. So every list and report that says an element is used also says when it is on no beat.

### The beat tree window

A new window, opened from the main window, shows the beat tree.

- Each Problem is a column. The Story Problem is first.
- The column header shows the Problem's title, its category, and the dropdown that chooses its beat sheet.
- Each beat is a card. A card shows the beat's title and the Scene or Problem on it. A switch shows or hides the beat descriptions.
- Under the cards, the column lists its elements that are on no beat.
- A card that holds a Problem is joined by a line to that Problem's column. A Problem on no beat is joined from its place in that list.
- A list on the right shows every Scene and every Problem in the outline. It tells three states apart: not used, used and on a beat, and used but on no beat. The marks do not depend on colour alone.
- The columns scroll sideways. The list stays in place.

### One way to do each thing

The Structure tab and a column of the beat tree show the same thing for one Problem, and work the same way (Terry, 2026-11-25: "Do one thing one way, so that there is less confusion."). They use the same view model and share code (Terry, 2026-11-26).

The actions are:

1. Put an element on a beat.
2. Take an element off a beat.
3. Give an element to a Problem with no beat.
4. Remove an element from a Problem.
5. Move an element from one beat to another.
6. Change the beat sheet.
7. Add, delete, rename or reorder a beat.

- Both screens show the elements on no beat.
- Both do actions 1 to 5 by drag and drop, and by a button for a writer who uses the keyboard.
- Both show the same popups:

| What the writer does | What happens |
|----------------------|--------------|
| Puts a Scene on a beat when it is already on another beat of the same Problem | A popup asks: move it, put it here as well, or cancel. Today it goes on both beats with no question. |
| Puts a Scene in a Problem when it is already in another Problem | No popup. |
| Puts a Problem on a beat when it already belongs to a Problem, the same one or another | The "Already assigned!" popup of today asks whether to assign it here instead. |
| Deletes a beat that holds an element | A popup asks: keep the element in the Problem, remove it from the Problem, or cancel. |

### Access for the AI

Everything the writer can do here, an AI can do through the StoryCAD API and the MCP tools. The MCP tools are in the StoryCADAPI repo, and they let an outside AI tool read and change an outline.

An AI cannot answer a popup. So:

- The action that puts an element on a beat today keeps its behaviour. For a Scene that is already on another beat, that is "put here as well".
- "Move" is a new action.
- Deleting a beat keeps its element in the Problem, on no beat. The AI removes it with the remove action.

### Protection

- StoryCAD refuses to put a Problem in itself or in a Problem under it.
- An outline that already holds a loop still opens, and the beat tree shows each Problem once.
- All four ways of changing a beat sheet give the same result.

## Out of scope

- A Collaborator workflow that works through the beat tree, stub Scenes and stub Problems on empty cards (a stub is a new element that the AI proposes with a name and a description), and showing an AI proposal on the beat tree. A new issue in the Collaborator repo will cover these. It is not written yet, and it is built after this issue.
- The order in which the story is told. That stays in the Narrator view, the second of StoryCAD's two navigation trees. The beat tree does not read it or change it.
- Timeline (#747).

## Alternatives Considered

- **A list only of what a beat sheet change drops.** This was the first form of this issue. It keeps the dropped elements, but a Problem with no beat sheet still holds nothing. Terry's comment of 2026-11-01 on this issue replaced it with one list for each Problem.
- **Improve the reports.** A report shows the tree but the writer cannot work in it.

## Acceptance Criteria

Each criterion names the pull request that meets it. Between pull request 1 and pull request 3, the two API ways of changing a beat sheet still drop the elements, as they do today.

The list of elements:

- [ ] (PR 1) A Scene or a Problem can be given to a Problem that has no beat sheet, and it is still there after save and reopen.
- [ ] (PR 1) After a beat sheet change by the dropdown on the Structure tab, or by loading a beat sheet file on the Structure tab, every element that was on a beat belongs to the Problem, is on no beat, and is listed in the order the proposal gives.
- [ ] (PR 3) The same is true after a beat sheet change by the API's `ApplyBeatSheetToProblem` and `LoadBeatSheet`, and by the MCP tools and Python bindings that call them.
- [ ] (PR 3) After a beat sheet change by any of the four ways, a child Problem that was on a beat still belongs to that Problem, and no Problem records a parent that it does not belong to.
- [ ] (PR 1) Taking an element off a beat leaves it in the Problem, on no beat. Removing it from the Problem takes it off every beat of that Problem.
- [ ] (PR 1) Deleting a beat that holds an element asks the writer to keep the element, remove it, or cancel, and does what the writer chose.
- [ ] (PR 1) An outline saved by an earlier version opens with each Problem holding the elements that are on its beats. No beat, beat sheet name or beat sheet description is changed by opening it.
- [ ] (PR 3) Putting a Problem in itself, or in a Problem under it, is refused with a message, from the screen and from the API.
- [ ] (PR 1) The Story Problem cannot be put in another Problem.

The screens (all PR 1):

- [ ] The beat tree window opens from the main window and shows a column for the Story Problem and for each Problem under it, at every level. Each of those columns is joined by a line to the card, or to the place on no beat, that holds its Problem.
- [ ] An outline that holds a loop opens, and the beat tree shows each Problem once.
- [ ] A column shows the title, the category, the beat sheet dropdown, one card for each beat, and the elements on no beat.
- [ ] The list on the right shows every Scene and every Problem. An element that is not used, one that is on a beat, and one that is used but on no beat are told apart without relying on colour.
- [ ] The Unassigned Elements report lists the elements that are used but on no beat, apart from the elements that are not used.
- [ ] The Structure tab shows the elements on no beat, and each of the seven actions works on it as it does in a column.
- [ ] Each of actions 1 to 5 can be done by drag and drop and by a button.
- [ ] The four popups appear as the table says, in the beat tree window and on the Structure tab.
- [ ] Add, rename and reorder of beats work as they do today.
- [ ] A change made in the beat tree window shows at once on an open Problem form, and a change made on the Problem form shows at once in the beat tree window. Neither overwrites the other.
- [ ] The beat tree window closes when the outline closes.
- [ ] The Structure tab fits in the Content pane with the main window at 1036 by 744, the size the manual's pictures use.
- [ ] Each criterion for the screens passes on Windows and on macOS.

The AI (all PR 2):

- [ ] The API and the MCP tools can give an element to a Problem with no beat, remove an element from a Problem, list a Problem's elements on no beat, and move an element from one beat to another.
- [ ] The existing action that puts an element on a beat puts a Scene on a second beat of the same Problem, as it does today.
- [ ] Deleting a beat through the API keeps its element in the Problem, on no beat.
- [ ] `get_element` on a Problem shows the elements that belong to it.

The record:

- [ ] (PR 1) The Structure tab page of the manual says what a beat sheet change and a beat delete do now, describes the popup for a Scene on a second beat, and says what the Unassigned Elements report lists. Today it says that reloading a template clears the assignments.
- [ ] The design names a unit test, an automation script step or a manual test step for each criterion above.

## Delivery

Three pull requests, in this order (Terry, 2026-11-26):

1. The list of elements, the shared view model, the beat tree window, and a guard so that an outline with a loop shows each Problem once.
2. The AI's access: the API and the MCP tools.
3. The protection: refusing a loop, and making the four ways of changing a beat sheet agree.

The protection comes last because, done first, it would be made to code that the shared view model is about to reshape.

All three are for Release 4.4 (Terry, 2026-11-26).

## Mockups/Examples

- A working mock-up as a web page: `devdocs/issue-1546-mockup.html` on the branch `issue-1546-beat-tree`. It shows the sample outline The Old Man and the Sea in the beat tree window and on the Structure tab. Terry tried it on 2026-11-26.
- The first sketch is the photo in the first comment on this issue.
- The notes of the discussion: `devdocs/issue-1546-discussion-notes.md` on the same branch.
- The issue review: `devdocs/issue-1546-issue-review.md` in the repo `storybuilder-miscellaneous`.

## Points the design must settle

- **The shared view model.** `BeatSheetsViewModel` holds its own copies of the beat sheet's name and description and of the Problem's record of its parent, and writes them back when the writer leaves the form. Its popups appear on the main window. It holds its own list of Scenes and Problems. Adding a beat and moving a beat change the list directly and do not go through `OutlineService`.
- **The layout.** The Structure tab is written inside `StoryCAD/Views/ProblemPage.xaml`. It becomes a control that both screens use.
- **Drag and drop.** StoryCAD uses it today only inside the navigation tree. It must work between lists on Windows and on macOS.
- **The lines.** In the mock-up they cross the columns between a card and its child.
- **What the mock-up leaves out:** right-click actions on a card, how the writer sees the details of a beat, the header and status bar of the window, and the arrow keys.
- **A Problem with its own beat sheet that belongs to no other Problem.** Whether it shows on the beat tree.
- **An outline with no Story Problem.** What the beat tree shows. The structure reports come out blank today.
- **Two markers for the Story Problem.** The Story Overview form names it, and a Problem's category can be "Story problem". The code keeps the two in step in three places, but not fully: the old Story Problem keeps its category when the marker moves, and a change through the API does not sync them. The Story Overview marker is the one that counts.
- **Names for the new actions.** In the API and the MCP tools, "element" means any story element, and `AddElement` and `add_element` are taken.
- **Other ways to reach the beat actions.** The Python bindings and StoryCADCli expose the same beat calls as the MCP tools. `OutlineService.SetBeatSheet` is a fifth way to change a beat sheet. Only a test calls it today.
- **The other reports.** The Recursive Structure report and the Plot Structure Diagram walk beats only, so they would miss a child Problem that is on no beat.
- **Collaborator.** It reads beats today, in `CollaboratorLib`, to find the Problems of a Scene and to find elements that are free. Its rule for a free element is "on no beat of any Problem". The design checks that both still give the right answer when an element can belong to a Problem on no beat.

---

## Approvals
<!-- For a maintainer only. -->
- [ ] The issue is accepted.
- [ ] The design is approved.

## Tasks
StoryCADBot keeps this list.
- [ ]
