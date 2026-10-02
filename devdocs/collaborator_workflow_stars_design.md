# Collaborator workflow stars — design

**Date:** 2026-08-11
**Branch:** `faves`
**Status:** Draft for review

## Problem

The Collaborator navigation pane lists the whole workflow registry — 23 workflows under
five always-expanded element-type groups. That is the jam-table problem: a wide choice
attracts browsing and suppresses decisions. Writers on the free beta open the pane, scroll,
and pick nothing.

The catalog itself is not the problem. Its position as the default first surface is.

## Goal

Most sessions, the writer acts from a short band at the top of the pane without scrolling
the full catalog. The full catalog stays available, one click away.

## Menu order

1. **Outline gaps (n)** — when the scanner finds any. Already implemented; unchanged.
2. **Starred** — the writer's curated set, flat, expanded.
3. **Element-type groups** — Story Overview, Problem, Character, Setting, Scene. Collapsed
   by default, holding only the workflows that are not starred.

```
⚠ Outline gaps (4)
▾ Starred
    Ideation (Story idea => Concept => Premise)     ★
    Story Problem (Premise => Problem + Characters) ★
    Goal / Motivation / Conflict (GMC)              ★
    Problem Structure                               ★
    Role and Story Role                             ★
    Scene Summary                                   ★
    Scene Conflict                                  ★
▸ Story Overview
▸ Problem
▸ Character
▸ Setting
▸ Scene
```

A starred workflow appears **only** in the Starred band. Listing it in both places would put
two navigation items on the same registry instance, and `RestoreSelection` matches by tag —
it would highlight whichever copy it reached first. Empty type groups are skipped.

With the default set the pane opens on 13 rows, 14 when there are outline gaps, against 28 today.

## Default star set

Seven workflows, one per stage of the outlining arc:

| Label | Element type | Stage |
|---|---|---|
| `Premise` | Story Overview | Idea to premise |
| `StoryProblem` | Story Overview | Premise to problem and cast |
| `GMC` | Problem | Problem is well formed |
| `Structure` | Problem | Problem gets a shape |
| `RoleAndStoryRole` | Character | Cast has function |
| `SceneSummary` | Scene | Scenes happen |
| `SceneConflict` | Scene | Scenes have conflict |

Both scene workflows are starred deliberately. A Path to Try already tells writers to prefer
Scene Summary when they run only one, so starring Scene Conflict alone would have put the
default set at odds with the manual's craft guidance.

The set is a starting point, not a recommendation engine. Any of it can be unstarred.

## Persistence

`CollaboratorSettings` is session state — `Collaborator.SetSettings` holds it in a field and
nothing writes it to disk. Stars must survive a restart, so they go in `PreferencesModel`,
which `PreferencesIo` serializes to `Preferences.json`:

- `StarredCollaboratorWorkflows` — `List<string>` of registry labels.
- `CollaboratorStarDefaultsApplied` — `bool`.

`WorkflowStarService` (StoryCADLib, registered in `BootStrapper`) owns the rules:

- When `CollaboratorStarDefaultsApplied` is false, seed the list from the default set, set the
  flag, and persist. New users and existing users each get the defaults exactly once.
- After that the stored list wins, including when it is empty. A writer who unstars everything
  gets an empty band, not the defaults again.

Labels are stored, not indices, so reordering the registry does not scramble anyone's stars. A
label that no longer resolves is ignored at render and left in the file, so removing a workflow
for one release and restoring it in the next does not silently discard the star.

## Star toggle

Each workflow row carries a star button: the filled `FavoriteStarFill` glyph (U+E735) when
starred, the outline `FavoriteStar` glyph (U+E734) when not. Starred stars stay visible. Unstarred stars fade in on the row's `PointerEntered` and on
`GotFocus`, so keyboard users can reach them.

Toggling persists, rebuilds the menu, and restores the previous selection.

**Risk.** A click inside a `NavigationViewItem` may still invoke the item. Invoking a workflow
item runs the workflow, which is an LLM call the writer did not ask for and, on the beta, one
they are paying for in quota. WinUI and Skia do not agree on this kind of hit-testing, so the
button handling the click is not sufficient evidence that the item will not fire. The toggle
therefore raises a suppression flag that `WorkflowShellViewModel.NavView_SelectionChanged`
checks before it dispatches, on top of the existing same-tag guard.

This is the part of the feature that must be tested by hand on Windows. A desktop-head build
on macOS does not settle it.

## Customize dialog

A star `AppBarButton` beside Settings on the top bar opens **Customize workflows**: every
registry workflow as a checkbox with its title and description, grouped by element type. Save
applies to the menu immediately and persists. Cancel discards.

The dialog is built in code-behind, matching the Collaborator settings dialog next to it.

## Structure

`RebuildWorkflowMenu` currently interleaves ordering rules with `NavigationViewItem`
construction, so no part of the ordering can be tested. Band composition moves to
`WorkflowMenuComposer.Compose(workflows, starredLabels)`, a pure function returning a plain
band model. `Collaborator.cs` renders that model into navigation items and nothing else.

## Tests

- Composer: starred band ordering, starred workflows excluded from type groups, empty groups
  skipped, empty star set drops the band.
- Star service: defaults seeded once, user edits preserved across reads, empty list preserved.
- Registry: every default label resolves to a real workflow.
- ViewModel: the suppression flag blocks dispatch and clears afterward.
- Manual, on Windows: star click does not run a workflow; hover and keyboard focus reveal the
  outline star; the pane survives a rebuild with the selection intact.

## Non-goals

Recommendation engines, per-element-type star sets, star limits, and removing workflows from
the registry. This changes presentation only.
