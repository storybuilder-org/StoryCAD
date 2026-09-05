---
title: Outline Gaps
layout: default
nav_enabled: true
nav_order: 1
parent: Workflow Reference
grand_parent: StoryCAD Collaborator
has_toc: false
---

# Outline Gaps

StoryCAD's forms have many fields, and you are never required to fill them all. Collaborator, though, treats a short list of fields on each kind of story element as required: the ones its workflows read, and the ones a story cannot do without, such as a Story Problem on the Overview or a protagonist on a Problem. When you open Collaborator, it checks every Overview, Problem, Character, Setting, and Scene in your outline against that list. If anything is missing, an Outline gaps row appears at the top of the workflow list with a count of the elements that have gaps. When nothing is missing, the row is not there.

## What the page shows

Clicking Outline gaps does not run anything. It opens a page in the center of the window headed, in the app's own words, "Required fields that are empty or broken. Click a field to open its helper workflow, or the element name to open it in StoryCAD." Below that, your elements with gaps are listed in outline order, the Overview first, then Problems, Characters, Settings, and Scenes, each with its missing fields under its name.

Each missing field is a link, and the small text under it tells you what the link does. Where a Collaborator workflow can fill the field, the text reads "via" and the workflow's name, and clicking it starts that workflow; you choose the element in the picker as usual. Where no workflow applies, for example the Author field on the Overview or an element's Name, the text reads "edit in StoryCAD," and clicking the element's name selects it in StoryCAD's navigation pane so you can type the value yourself.

A sentence at the top of the page beginning "Guess:" tells you where Collaborator thinks your outline stands. The last section of this page explains it.

*[Screenshot to come: the Outline gaps page on a new outline, with the guess sentence and the Overview's missing fields. File: Collaborator-Outline-Gaps.png]*

## The required fields

| Story element | Required fields | Which workflow helps |
|---|---|---|
| Story Overview | Title, Story Idea, Author, Concept, Premise, Type, Genre, Story Problem | Ideation (Story idea => Concept => Premise) for Story Idea, Concept, and Premise; Story Form for Type and Genre; Story Problem (Premise => Problem + Characters) for the Story Problem. Title and Author you type in StoryCAD. |
| Problem | Name, Story Question, Problem Category, Problem Type, Conflict Type, Subject, Premise, Protagonist, Antagonist, the Goal, Motivation, and Conflict for each of them, and Outcome | Story Problem (Premise => Problem + Characters) for the category and the two characters, and a first pass at the rest; Problem Builder for the goal, motivation, conflict, and outcome fields. Name you type in StoryCAD. |
| Character | Name, Character Sketch, Role, Story Role, Age, Sex, Appearance, Backstory | Define Character for Role, Age, Sex, and Appearance; Character Story Function for Story Role and Character Sketch; Flaw and Backstory for Backstory. Name you type in StoryCAD. |
| Setting | Name, Setting Summary | The page points Setting Summary at Setting Builder. Name you type in StoryCAD. |
| Scene | Name, Scene Sketch, Setting, Cast | Scene Builder for all three. Name you type in StoryCAD. |

## Where your outline stands

Every time a workflow runs, Collaborator tells the model which stage of outlining your story has reached, so that its suggestions fit an outline at that stage rather than a finished one. It decides the stage from the outline itself, checking in this order and stopping at the first condition that holds:

1. Ideation. The Overview's Type, Genre, or Premise is empty. The story is still an idea.
2. Problem Development. Those three are filled, but no Story Problem has been chosen on the Overview's Premise tab, or the chosen problem's Protagonist or Antagonist is not linked to a character.
3. Character Development. The Story Problem and its two characters are in place, but one of those characters is missing an essential field: Name, Character Sketch, Role, Story Role, Age, Sex, Appearance, or Backstory.
4. Structure Building. The cast is complete, but no scene has been assigned to a beat on the Story Problem's beat sheet or on any of its sub-problems.
5. Scene Work. At least one scene sits on a beat. The outline has a shape, and the remaining work is inside the scenes.

The same judgment appears to you as the Guess sentence on the Outline gaps page: "Guess: the outline is in Problem Development," for example. When a problem gap and a character gap are both open, the sentence says so and adds that the Story Problem still needs its protagonist and antagonist. It is a guess in the plain sense. It comes from which fields are filled, not from reading your prose, so if it seems wrong, the table above shows which fields it is looking at.
