---
title: Python Bindings
layout: default
nav_enabled: true
nav_order: 114.5
parent: For Developers
has_toc: true
---

# Python Bindings

The `storycad` Python package lets you build and edit StoryCAD outlines
(`.stbx` files) from Python — create characters, scenes, problems, beat sheets,
relationships, and more — without opening the StoryCAD app.

Under the hood it hosts the .NET runtime in-process via
[`pythonnet`](https://pythonnet.github.io/) and exposes a Pythonic, snake_case
surface over the same [StoryCAD API](Using_the_API.html) used by the C# code.
You don't need to know any C# to use it. Errors surface as a Python
`StoryCADError` exception (instead of the C# `OperationResult` return value),
and asynchronous methods are run synchronously for you.

The source lives in the repository under
`StoryCADLib/bindings/python/`.

---

## Install

**Prerequisites**

- Python 3.10 or newer
- The [.NET 10 runtime](https://dotnet.microsoft.com/download) (the runtime, not
  the full SDK)

```bash
pip install StoryCADLib
```

That installs everything — the package carries its own copy of the StoryCAD
engine, so there is nothing else to build or download. Contributors working
from a repo checkout build it locally instead; see
[Building from source](#building-from-source).

---

## Your first outline

```python
from storycad import StoryCAD

sc = StoryCAD()

# Every outline starts from a template. "0" is the blank template.
sc.create_empty_outline(name="My First Story", author="Your Name", template_index="0")

# Save it to disk.
sc.write_outline("/tmp/my_first_story.stbx")
```

That is a complete, valid StoryCAD file. Open it in the StoryCAD app to confirm.

---

## Core concepts

### The `StoryCAD` object

`StoryCAD()` boots the engine and gives you one handle for everything. Pass
`headless=True` when running on a server or in automated tests:

```python
sc = StoryCAD(headless=True)
```

There is one "current outline" at a time. Both `create_empty_outline(...)` and
`open_outline(...)` replace it.

### Elements and their GUIDs

An outline is a tree of **story elements** — the overview, characters, settings,
scenes, problems, and so on. Every element has a unique id, returned as a Python
`uuid.UUID`. You pass those ids back to address elements:

```python
hero = sc.add_element(sc.item_type.Character, parent.uuid, "Hero")
# hero is a uuid.UUID you use in later calls
```

### Element types

Element types live on `sc.item_type`:

```python
sc.item_type.Character
sc.item_type.Scene
sc.item_type.Setting
sc.item_type.Problem
sc.item_type.StoryWorld
sc.item_type.Folder
sc.item_type.Section
```

### The overview is the root

Most elements hang off the **StoryOverview**, the root of the outline. Grab it
after creating or opening a file:

```python
overview = next(e for e in sc.get_all_elements() if e.element_type == "StoryOverview")
```

`get_all_elements()` returns lightweight `ElementSummary` objects with `.uuid`,
`.name`, and `.element_type`.

### Errors

Any failed operation raises `StoryCADError`:

```python
from storycad import StoryCADError

try:
    sc.get_story_element(some_guid)
except StoryCADError as e:
    print("not found:", e)
```

---

## A complete worked example

This builds a small but real outline end to end:

```python
from storycad import StoryCAD

sc = StoryCAD()
sc.create_empty_outline("The Lighthouse", "Author", "0")
overview = next(e for e in sc.get_all_elements() if e.element_type == "StoryOverview")

# --- characters -----------------------------------------------------------
keeper = sc.add_element(sc.item_type.Character, overview.uuid, "Elias Trevethan")
apprentice = sc.add_element(sc.item_type.Character, overview.uuid, "Wren Carlyon")

# Set properties. NOTE: property values must be strings (see Gotchas).
sc.update_element_properties(keeper, {"Role": "Protagonist", "Age": "52", "Sex": "Male"})
sc.update_element_property(apprentice, "Role", "Deuteragonist")

# A relationship between them (mirror=True makes it reciprocal).
sc.add_relationship(keeper, apprentice, "Mentor and reluctant apprentice", mirror=True)

# --- settings -------------------------------------------------------------
sc.add_element(sc.item_type.Setting, overview.uuid, "Tregarrow Lighthouse")

# --- a problem with a beat sheet ------------------------------------------
problem = sc.add_element(sc.item_type.Problem, overview.uuid, "The Light Must Not Go Out")

sheet_name = sc.get_beat_sheet_names()[0]          # e.g. "Three Act Play"
sc.apply_beat_sheet_to_problem(problem, sheet_name)

# --- scenes, bound to the problem's beats ---------------------------------
scenes = [
    sc.add_element(sc.item_type.Scene, overview.uuid, title)
    for title in ["The Lamp Flickers", "Wren Arrives", "The Light Goes Out"]
]
for i, scene in enumerate(scenes):
    sc.assign_element_to_beat(problem, i, scene)

# Cast a character into the opening scene.
sc.add_cast_member(scenes[0], keeper)

# --- inspect --------------------------------------------------------------
structure = sc.get_problem_structure(problem)
print(f"{structure.title}: {len(structure.beats)} beats")
for beat in structure.beats:
    linked = " (assigned)" if beat.linked_element else ""
    print(f"  - {beat.title}{linked}")

# --- save -----------------------------------------------------------------
sc.write_outline("/tmp/lighthouse.stbx")
```

---

## Common tasks

### Open an existing outline

```python
sc.open_outline("/tmp/lighthouse.stbx")
```

### Find elements

```python
characters = sc.get_elements_by_type(sc.item_type.Character)
world = sc.get_story_world()                       # None if there isn't one

hits = sc.search_for_text("lighthouse")            # list of dicts
refs = sc.search_for_references(character_guid)     # who references this element
subtree_hits = sc.search_in_subtree(folder_guid, "lamp")
```

### Move and delete

```python
sc.move_element(child_guid, new_parent_guid)        # optional index= to position it
sc.delete_element(guid)                              # moves to Trash
sc.restore_from_trash(guid)                          # bring it back
sc.empty_trash()                                     # permanent
```

### Collections (for example, a StoryWorld's physical worlds)

Pass a `dict` for structured collection entries — it is converted for you:

```python
world = sc.add_element(sc.item_type.StoryWorld, overview.uuid, "World")
idx = sc.add_collection_entry(world, "PhysicalWorlds", {"Name": "Aerth"})
sc.update_collection_entry(world, "PhysicalWorlds", idx, {"Name": "Aerth Prime"})
sc.remove_collection_entry(world, "PhysicalWorlds", idx)
```

### Beats

```python
sc.create_beat(problem, "Midpoint", "Everything changes")
sc.update_beat(problem, 0, "Opening Image", "The world before")
sc.move_beat(problem, 0, 1)
sc.delete_beat(problem, 0)
sc.save_beat_sheet(problem, "/tmp/beats.json")
sc.load_beat_sheet(other_problem, "/tmp/beats.json")
```

### Reference data (no outline needed)

These read StoryCAD's built-in reference content:

```python
sc.get_examples("Tone")
sc.get_conflict_categories()
sc.get_conflict_subcategories(category)
sc.get_conflict_examples(category, subcategory)
sc.get_key_questions("Character")                   # list of KeyQuestion(topic, question)
sc.get_master_plot_names()
sc.get_master_plot_scenes(plot_name)                # list of PlotScene(title, notes)
sc.get_stock_scene_categories()
```

---

## Gotchas

- **Property values must be strings.** `update_element_property` and
  `update_element_properties` send values to a typed .NET property setter. Pass
  `"42"`, not `42` — a non-string value raises
  `StoryCADError: Specified cast is not valid`.
- **Collection entries take dicts.** For structured collections (such as
  `PhysicalWorlds`), pass a Python `dict`; the binding converts it. A plain
  string only works for collections whose element type is itself a string.
- **Search results contain raw values.** `search_for_text`,
  `search_for_references`, and `search_in_subtree` return lists of dicts whose
  values may be .NET objects (for example a `Guid`). Call `str(value)` if you
  need text.
- **One outline at a time.** Creating or opening an outline replaces the
  current one — there is no multi-document support.
- **Templates are required.** `create_empty_outline` needs a `template_index`;
  `"0"` is the blank template.

---

## Building from source

If you have cloned the repository instead of installing a wheel:

```bash
cd StoryCAD/StoryCADLib/bindings/python
./build-wheel.sh   # runs dotnet publish, stages the runtime, then python -m build
```

The script detects your host platform, publishes `StoryCADLib` for the matching
framework-dependent target, stages the runtime into `storycad/runtime/`, and
produces a platform wheel under `dist/`.

`StoryCAD()` looks for `StoryCADLib.dll` in this order:

1. a `publish_dir=` argument to `StoryCAD(...)`
2. the `STORYCAD_DLL_PATH` environment variable (a file or a directory)
3. the bundled `storycad/runtime/` directory

So you can also point it at any published build explicitly:

```python
sc = StoryCAD(publish_dir="/path/to/publish")
```

---

## License note

StoryCAD is licensed under GPL-3.0-or-later. A wheel bundles the StoryCAD
runtime, so any project that ships this wheel inherits and must comply with that
license.
