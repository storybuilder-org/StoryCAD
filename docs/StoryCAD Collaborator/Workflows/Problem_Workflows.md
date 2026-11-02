---
title: Problem Workflows
layout: default
nav_enabled: true
nav_order: 3
parent: Workflow Reference
grand_parent: StoryCAD Collaborator
has_toc: false
---

# Problem Workflows

These three workflows work on a Problem element, and each opens a picker so you can say which one. The craft behind them is in [Defining Problems](../../Writing%20with%20StoryCAD/Defining_Problems.html) and [Problem and Character Development](../../Writing%20with%20StoryCAD/Problem_and_Character_Development.html), and the fields they write are described tab by tab starting at the [Problem Form](../../Story%20Elements/Problem_Form.html) page.

## Inner and Outer Problems

Defining Problems distinguishes the outer problem, the external goal the protagonist pursues, from the inner problem, the flaw or wound that must change before the outer problem can truly be resolved. Writers sometimes call these the Want and the Need. StoryCAD keeps them as two Problem elements, and this workflow builds the second from the first.

When you click it, the picker asks for the outer Problem, normally one you have already developed, then for the inner Problem, which you can create on the spot, and then takes the protagonist from the outer Problem's Protagonist tab, asking you to pick one only if that tab is empty. If that character already has a Flaw or a Backstory, the workflow uses them as the source of the inner struggle. If not, it proposes a flaw that would explain it.

It writes the inner Problem's form: the Story Question and Problem Type on the Problem tab, the Goal, Motivation, and Conflict on both the Protagonist and Antagonist tabs, and on the Resolution tab the Method and Theme, with its reasoning in Notes. The Conflict Type is set to Person vs. Self, and the protagonist and the antagonist of the inner Problem are the same character, because that is what an inner problem is. It also proposes a Flaw on that character's Flaw tab. Flaw and Backstory can write the same field from the character's side; whichever you accept last is what stays.

![Property Updates after Inner and Outer Problems on Leonard's impulsiveness lets Lacas capture him: the inner Problem's rows, with Leonard Kraskin as its antagonist and Person vs. Self as its Conflict Type](../../media/Collaborator/Collaborator-Workflow-Inner-Outer.png)

## Problem Builder

A Problem that is ready to use has a goal, a motive, and a conflict on each side, an outcome, and a shape: the beat sheet on its Structure tab that says how its events unfold. [Defining Problems](../../Writing%20with%20StoryCAD/Defining_Problems.html) covers the first part, and Problem Builder writes it. Problem Beats, below, writes the beat sheet.

Before you run it, set three things on the Problem by hand: the Problem Category, the Protagonist and Antagonist. Problem Builder reads all three, and a blank category stops the run before it starts. The picker asks for the Problem; the two characters come from its tabs. If you built the problem with Story Problem (Premise => Problem + Characters), all three are already in place.

The run proposes the Problem tab fields (Name, Problem Type, Conflict Type, Subject, Story Question, and Source of Conflict), the Goal, Motivation, and Conflict for both characters, and the Resolution tab (Premise, Outcome, Method, and Theme), with a situation sheet in Notes. Everything is a proposal on the Problem until you accept it. It is one of the starred workflows by default.

## Problem Beats

Problem Beats gives a Problem its shape. [Plotting with StoryCAD](../../Writing%20with%20StoryCAD/Plotting_with_StoryCAD.html) and the [Structure Tab](../../Story%20Elements/Structure_Tab.html) page cover the craft. Run it after Problem Builder, when the Problem's goals, conflict, and outcome are in place, because it reads them to judge which events fit. It needs the same three settings: Problem Category, Protagonist, and Antagonist.

It chooses a beat sheet that fits the Problem Category and Conflict Type, or keeps the one you already chose. For each empty beat it looks first at the scenes in your outline, including scenes that already sit on another problem's beats, since a scene can serve more than one problem, and at the problems that sit on no beat sheet yet. It binds one where its Scene Sketch or Story Question fits the beat. For the beats still empty, it creates a new Scene as a stub, with a Scene Type and a cast drawn from your characters, or, where the beat is a smaller conflict of its own, a new Problem as a Complication, Subplot, or Sequence. The beat sheet appears in the review as its own rows, one per beat, so you can accept or skip each assignment on its own.

Two rules protect your work. Problem Beats never changes a beat you have already filled, and it never adds beats to a sheet you have already chosen. The Scene and Problem stubs it creates are the only additional elements; everything else is a proposal until you accept it.

![Property Updates after Problem Beats: the beat sheet's title and description, then one row per beat](../../media/Collaborator/Collaborator-Workflow-Problem-Beats.png)
