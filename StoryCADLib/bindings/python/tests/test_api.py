"""Integration tests that drive the real StoryCADLib through pythonnet.

Skipped automatically when no StoryCADLib.dll can be located (see conftest).
Each test gets a fresh outline via the ``outline`` fixture, so they are
order-independent even though they share one CLR-backed instance.
"""
from __future__ import annotations

import uuid

import pytest

from storycad import StoryCADError
from conftest import requires_dll

pytestmark = requires_dll


# ── lifecycle ────────────────────────────────────────────────────────────────

def test_create_empty_outline_returns_root_guids(sc):
    guids = sc.create_empty_outline("Lifecycle", "pytest", "0")
    assert len(guids) == 3
    assert all(isinstance(g, uuid.UUID) for g in guids)


def test_write_outline_creates_nonempty_file(outline, tmp_path):
    sc, _ = outline
    path = tmp_path / "out.stbx"
    sc.write_outline(str(path))
    assert path.exists()
    assert path.stat().st_size > 0


def test_write_then_open_roundtrip(outline, tmp_path):
    sc, overview = outline
    sc.add_element(sc.item_type.Character, overview.uuid, "Roundtrip Hero")
    path = tmp_path / "rt.stbx"
    sc.write_outline(str(path))

    sc.open_outline(str(path))
    names = [e.name for e in sc.get_all_elements()]
    assert "Roundtrip Hero" in names


# ── elements ─────────────────────────────────────────────────────────────────

def test_get_all_elements_has_overview(outline):
    sc, overview = outline
    elements = sc.get_all_elements()
    assert overview.element_type == "StoryOverview"
    assert any(e.uuid == overview.uuid for e in elements)


def test_add_element_returns_guid(outline):
    sc, overview = outline
    g = sc.add_element(sc.item_type.Character, overview.uuid, "Hero")
    assert isinstance(g, uuid.UUID)
    fetched = sc.get_story_element(g)
    assert fetched.name == "Hero"


def test_get_elements_by_type(outline):
    sc, overview = outline
    sc.add_element(sc.item_type.Character, overview.uuid, "A")
    sc.add_element(sc.item_type.Character, overview.uuid, "B")
    chars = sc.get_elements_by_type(sc.item_type.Character)
    assert len([c for c in chars if c.name in ("A", "B")]) == 2


def test_add_element_with_properties(outline):
    sc, overview = outline
    g = sc.add_element_with_properties(
        sc.item_type.Character, overview.uuid, "Detailed",
        {"Role": "Protagonist", "Age": "40"},
    )
    assert isinstance(g, uuid.UUID)


def test_update_element_property(outline):
    sc, overview = outline
    g = sc.add_element(sc.item_type.Character, overview.uuid, "Mutable")
    summary = sc.update_element_property(g, "Name", "Renamed")
    assert summary.name == "Renamed"


def test_get_story_world(outline):
    sc, overview = outline
    sc.add_element(sc.item_type.StoryWorld, overview.uuid, "My World")
    world = sc.get_story_world()
    assert world is not None
    assert world.element_type == "StoryWorld"


def test_delete_then_restore(outline):
    # delete_element moves the node to Trash (the StoryElement stays in the
    # model), and restore_from_trash brings it back. Both exercise async
    # CLR paths; assert the element survives the round-trip.
    sc, overview = outline
    g = sc.add_element(sc.item_type.Character, overview.uuid, "Doomed")
    sc.delete_element(g)
    sc.restore_from_trash(g)
    assert sc.get_story_element(g).name == "Doomed"


# ── relationships / cast ─────────────────────────────────────────────────────

def test_add_relationship(outline):
    sc, overview = outline
    a = sc.add_element(sc.item_type.Character, overview.uuid, "Mentor")
    b = sc.add_element(sc.item_type.Character, overview.uuid, "Student")
    sc.add_relationship(a, b, "Mentor and student", mirror=True)  # should not raise


def test_add_cast_member(outline):
    sc, overview = outline
    scene = sc.add_element(sc.item_type.Scene, overview.uuid, "Opening")
    char = sc.add_element(sc.item_type.Character, overview.uuid, "Lead")
    sc.add_cast_member(scene, char)  # should not raise


# ── search ───────────────────────────────────────────────────────────────────

def test_search_for_text_finds_added_element(outline):
    sc, overview = outline
    sc.add_element(sc.item_type.Character, overview.uuid, "Zephyrine")
    hits = sc.search_for_text("Zephyrine")
    assert any("Zephyrine" in str(v) for h in hits for v in h.values())


def test_remove_references_on_unused_guid_is_zero(outline):
    sc, _ = outline
    assert sc.remove_references(uuid.uuid4()) == 0


def test_search_alias_matches_search_for_text(outline):
    sc, overview = outline
    sc.add_element(sc.item_type.Character, overview.uuid, "Aliased")
    assert sc.search("Aliased") == sc.search_for_text("Aliased")


# ── resources ────────────────────────────────────────────────────────────────

def test_get_examples_returns_list(sc):
    examples = sc.get_examples("Tone")
    assert isinstance(examples, list)
    assert examples and all(isinstance(x, str) for x in examples)


def test_conflict_categories_and_subcategories(sc):
    cats = sc.get_conflict_categories()
    assert cats
    subs = sc.get_conflict_subcategories(cats[0])
    assert isinstance(subs, list)


def test_get_key_questions(sc):
    questions = sc.get_key_questions("Character")
    assert questions
    assert questions[0].topic and questions[0].question


def test_master_plot_names_and_notes(sc):
    names = sc.get_master_plot_names()
    assert names
    notes = sc.get_master_plot_notes(names[0])
    assert isinstance(notes, str)


def test_stock_scene_categories(sc):
    cats = sc.get_stock_scene_categories()
    assert cats
    scenes = sc.get_stock_scenes(cats[0])
    assert isinstance(scenes, list)


# ── beats ────────────────────────────────────────────────────────────────────

def test_beat_sheet_names(sc):
    names = sc.get_beat_sheet_names()
    assert names and all(isinstance(n, str) for n in names)


def test_apply_beat_sheet_and_get_structure(outline):
    sc, overview = outline
    problem = sc.add_element(sc.item_type.Problem, overview.uuid, "Central Problem")
    sheet = sc.get_beat_sheet_names()[0]
    sc.apply_beat_sheet_to_problem(problem, sheet)
    struct = sc.get_problem_structure(problem)
    assert struct.title
    assert len(struct.beats) > 0


def test_create_update_delete_beat(outline):
    sc, overview = outline
    problem = sc.add_element(sc.item_type.Problem, overview.uuid, "P")
    before = len(sc.get_problem_structure(problem).beats)

    sc.create_beat(problem, "New Beat", "desc")
    after_create = sc.get_problem_structure(problem).beats
    assert len(after_create) == before + 1
    new_index = len(after_create) - 1

    sc.update_beat(problem, new_index, "Updated Beat", "new desc")
    updated = sc.get_problem_structure(problem).beats[new_index]
    assert updated.title == "Updated Beat"

    sc.delete_beat(problem, new_index)
    assert len(sc.get_problem_structure(problem).beats) == before


def test_assign_and_clear_beat_assignment(outline):
    sc, overview = outline
    problem = sc.add_element(sc.item_type.Problem, overview.uuid, "P")
    sc.apply_beat_sheet_to_problem(problem, sc.get_beat_sheet_names()[0])
    scene = sc.add_element(sc.item_type.Scene, overview.uuid, "S")

    sc.assign_element_to_beat(problem, 0, scene)
    assert sc.get_problem_structure(problem).beats[0].linked_element == scene

    sc.clear_beat_assignment(problem, 0)
    assert sc.get_problem_structure(problem).beats[0].linked_element is None


def test_save_and_load_beat_sheet(outline, tmp_path):
    sc, overview = outline
    problem = sc.add_element(sc.item_type.Problem, overview.uuid, "P")
    sc.apply_beat_sheet_to_problem(problem, sc.get_beat_sheet_names()[0])
    path = tmp_path / "beats.json"

    sc.save_beat_sheet(problem, str(path))
    assert path.exists() and path.stat().st_size > 0

    other = sc.add_element(sc.item_type.Problem, overview.uuid, "P2")
    sc.load_beat_sheet(other, str(path))  # should not raise


# ── collection entries ───────────────────────────────────────────────────────

def test_add_collection_entry_with_dict(outline):
    sc, overview = outline
    world = sc.add_element(sc.item_type.StoryWorld, overview.uuid, "World")
    index = sc.add_collection_entry(world, "PhysicalWorlds", {"Name": "Aerth"})
    assert index == 0


def test_collection_entry_add_update_remove(outline):
    sc, overview = outline
    world = sc.add_element(sc.item_type.StoryWorld, overview.uuid, "World")
    sc.add_collection_entry(world, "PhysicalWorlds", {"Name": "Aerth"})
    second = sc.add_collection_entry(world, "PhysicalWorlds", {"Name": "Mirror"})
    assert second == 1

    # dict marshaling must work for updates too
    sc.update_collection_entry(world, "PhysicalWorlds", 0, {"Name": "Aerth-2"})
    sc.remove_collection_entry(world, "PhysicalWorlds", 1)  # should not raise


def test_add_collection_entry_rejects_non_collection(outline):
    sc, overview = outline
    world = sc.add_element(sc.item_type.StoryWorld, overview.uuid, "World")
    with pytest.raises(StoryCADError):
        sc.add_collection_entry(world, "WorldType", {"Name": "x"})


# ── element JSON / bulk update / move / extra deletes ────────────────────────

def test_get_element_returns_json(outline):
    sc, overview = outline
    ch = sc.add_element(sc.item_type.Character, overview.uuid, "JsonChar")
    raw = sc.get_element(ch)
    assert isinstance(raw, str)
    assert '"Type"' in raw and "Character" in raw


def test_update_story_element_json_roundtrip(outline):
    sc, overview = outline
    ch = sc.add_element(sc.item_type.Character, overview.uuid, "RT")
    raw = sc.get_element(ch)
    sc.update_story_element(raw, ch)  # feed the element's own JSON back; no raise
    assert sc.get_story_element(ch).name == "RT"


def test_update_element_properties_bulk(outline):
    sc, overview = outline
    ch = sc.add_element(sc.item_type.Character, overview.uuid, "Bulk")
    sc.update_element_properties(ch, {"Role": "Lead", "Age": "30"})  # no raise


def test_delete_story_element(outline):
    sc, overview = outline
    ch = sc.add_element(sc.item_type.Character, overview.uuid, "ToDelete")
    sc.delete_story_element(ch)  # string-path, non-async delete; should not raise


def test_move_element_into_folder(outline):
    sc, overview = outline
    folder = sc.add_element(sc.item_type.Folder, overview.uuid, "Folder")
    ch = sc.add_element(sc.item_type.Character, overview.uuid, "Mover")
    sc.move_element(ch, folder)
    # the moved element is now found when searching the folder's subtree
    hits = sc.search_in_subtree(folder, "Mover")
    assert any("Mover" in str(v) for h in hits for v in h.values())


def test_empty_trash(outline):
    sc, overview = outline
    ch = sc.add_element(sc.item_type.Character, overview.uuid, "Trashed")
    sc.delete_element(ch)
    sc.empty_trash()  # should not raise


# ── search (references / subtree) ────────────────────────────────────────────

def test_search_for_references_finds_relationship(outline):
    sc, overview = outline
    a = sc.add_element(sc.item_type.Character, overview.uuid, "Source")
    b = sc.add_element(sc.item_type.Character, overview.uuid, "Target")
    sc.add_relationship(a, b, "knows")
    refs = sc.search_for_references(b)
    assert isinstance(refs, list)
    assert len(refs) > 0


def test_search_in_subtree_finds_descendant(outline):
    sc, overview = outline
    sc.add_element(sc.item_type.Character, overview.uuid, "Findable")
    hits = sc.search_in_subtree(overview.uuid, "Findable")
    assert any("Findable" in str(v) for h in hits for v in h.values())


# ── conflict resources ───────────────────────────────────────────────────────

def test_get_conflict_examples(sc):
    cat = sc.get_conflict_categories()[0]
    sub = sc.get_conflict_subcategories(cat)[0]
    examples = sc.get_conflict_examples(cat, sub)
    assert examples and all(isinstance(x, str) for x in examples)


def test_apply_conflict_to_protagonist(outline):
    sc, overview = outline
    problem = sc.add_element(sc.item_type.Problem, overview.uuid, "P")
    sc.apply_conflict_to_protagonist(problem, "Fear of the dark")  # no raise


def test_apply_conflict_to_antagonist(outline):
    sc, overview = outline
    problem = sc.add_element(sc.item_type.Problem, overview.uuid, "P")
    sc.apply_conflict_to_antagonist(problem, "Greed for power")  # no raise


# ── key questions / plots / beat-sheet templates ─────────────────────────────

def test_get_key_question_elements(sc):
    elems = sc.get_key_question_elements()
    assert elems and all(isinstance(x, str) for x in elems)


def test_get_master_plot_scenes(sc):
    scenes = sc.get_master_plot_scenes(sc.get_master_plot_names()[0])
    assert scenes
    assert scenes[0].title and isinstance(scenes[0].notes, str)


def test_get_beat_sheet(sc):
    bs = sc.get_beat_sheet(sc.get_beat_sheet_names()[0])
    assert bs.description
    assert bs.beats and bs.beats[0].title
    assert bs.beats[0].linked_element is None


def test_move_beat_reorders(outline):
    sc, overview = outline
    problem = sc.add_element(sc.item_type.Problem, overview.uuid, "P")
    sc.apply_beat_sheet_to_problem(problem, sc.get_beat_sheet_names()[0])
    before = [b.title for b in sc.get_problem_structure(problem).beats]
    sc.move_beat(problem, 0, 1)
    after = [b.title for b in sc.get_problem_structure(problem).beats]
    assert after[0] == before[1] and after[1] == before[0]


# ── error handling ───────────────────────────────────────────────────────────

def test_get_story_element_missing_raises(outline):
    sc, _ = outline
    with pytest.raises(StoryCADError) as exc:
        sc.get_story_element(uuid.uuid4())
    assert "not found" in str(exc.value).lower()


def test_get_story_element_empty_guid_raises(outline):
    sc, _ = outline
    with pytest.raises(StoryCADError):
        sc.get_story_element(uuid.UUID(int=0))
