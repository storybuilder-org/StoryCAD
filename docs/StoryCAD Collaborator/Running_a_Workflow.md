---
title: Running a Workflow
layout: default
nav_enabled: true
nav_order: 99
parent: StoryCAD Collaborator
has_toc: false
---

# Running a Workflow

## Pick a workflow

In the left list, click a workflow that matches the craft question you care about now. If you are unsure where to start, see [A Path to Try](Tutorial/A_Path_to_Try.html).

**Clicking a workflow runs it.** There is no separate Run button and no confirmation step, so treat the click as the decision. Its name appears on the top bar, and the center of the window shows a short description of what it is for: the purpose of the run, not the full craft essay.

## Choose the story element when asked

Most workflows work on one story element, and Collaborator has to know which one. The Story Overview is chosen for you, because an outline has only one. For any other kind of element a picker opens before anything runs. If your outline has none of that kind, the picker offers to create one; if it has exactly one, that one is preselected, and you can accept it or create another; if it has several, you choose. When the workflow follows a link in your outline, such as the protagonist of the Problem you just picked, that character is preselected as well. A few workflows ask for more than one element in turn; Story Problem (Premise => Problem + Characters), for example, asks for a Problem and then two characters.

![The Select Character dialog, listing the outline's characters with an option to create a new element](../media/Collaborator/Collaborator-Element-Picker.png)

The picker is your chance to back out. Cancel it and the workflow does not run. Nothing has been written at that point, with one exception worth knowing: where a workflow links elements as you pick them, as Story Problem (Premise => Problem + Characters) does when it makes your choice the Story Problem on the Overview, that link is made at once. The [Workflow Reference](Workflows/) notes this for the workflows that do it.

## What Collaborator sends with the run

Along with the elements you picked, Collaborator sends a short account of where your outline stands, the same judgment the Outline gaps page shows as its Guess sentence, so that the suggestions fit an outline at that stage rather than a finished one. [Outline Gaps](Workflows/Outline_Gaps.html) explains how that judgment is made.

## Wait for the result

Collaborator runs the workflow and reports progress in the chat column. When it finishes, you typically see:

- A short status, for example how many updates are free and how many need review
- A **Property Updates** list in the center: each row is one field Collaborator wants to change
- **Accept all changes** at the foot of that list, and **Review Each** and **Try Again** on the top bar

Nothing is written into those fields until you accept. See [Reviewing Suggestions](Reviewing_Suggestions.html).

![A finished run: six proposals headed 6: 6 free, 0 need review, five labeled New and one Update, with Accept all changes at the foot of the list](../media/Collaborator/Collaborator-Updates-After-Run.png)

## Try Again

**Try Again** runs the same workflow again with a fresh suggestion pass. Use it when the first result missed the mark. You still review and accept; a new run does not force old pending rows without your action.

## After you accept

Open the same story element in StoryCAD’s navigation pane. The fields you accepted should match what you approved in Collaborator. If something looks wrong, edit it in StoryCAD like any other text; the outline is still yours.
