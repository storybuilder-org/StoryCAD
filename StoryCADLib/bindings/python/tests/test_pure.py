"""Pure-Python unit tests for the storycad package.

These exercise the parts of the binding that never touch the CLR: DLL
discovery, the public surface, and the plain dataclasses. They run anywhere,
with no StoryCADLib.dll present.
"""
from __future__ import annotations

import uuid
from pathlib import Path

import pytest

import storycad
from storycad import (
    Beat,
    BeatSheet,
    ElementSummary,
    KeyQuestion,
    PlotScene,
    ProblemStructure,
    StoryCAD,
    StoryCADError,
)


# ── public surface ───────────────────────────────────────────────────────────

def test_version_is_a_string():
    assert isinstance(storycad.__version__, str)
    assert storycad.__version__


def test_all_exports_are_resolvable():
    for name in storycad.__all__:
        assert hasattr(storycad, name), f"{name} listed in __all__ but missing"


def test_storycaderror_is_runtimeerror():
    assert issubclass(StoryCADError, RuntimeError)


# ── dataclasses ──────────────────────────────────────────────────────────────

def test_beat_fields_and_equality():
    g = uuid.uuid4()
    a = Beat(title="Opening", description="d", linked_element=g)
    b = Beat(title="Opening", description="d", linked_element=g)
    assert a == b
    assert a.title == "Opening"
    assert a.linked_element == g


def test_beat_is_frozen():
    beat = Beat(title="t", description="d", linked_element=None)
    with pytest.raises(Exception):  # dataclasses.FrozenInstanceError
        beat.title = "changed"  # type: ignore[misc]


def test_problem_structure_holds_beats():
    beats = [Beat("t1", "d1", None), Beat("t2", "d2", None)]
    ps = ProblemStructure(title="Three Act", description="desc", beats=beats)
    assert ps.beats == beats
    assert len(ps.beats) == 2


def test_key_question_fields():
    kq = KeyQuestion(topic="Role", question="What does the hero want?")
    assert kq.topic == "Role"
    assert kq.question.endswith("?")


def test_plot_scene_fields():
    ps = PlotScene(title="The Reveal", notes="notes")
    assert (ps.title, ps.notes) == ("The Reveal", "notes")


def test_beat_sheet_fields():
    bs = BeatSheet(description="d", beats=[Beat("t", "d", None)])
    assert bs.description == "d"
    assert bs.beats[0].linked_element is None


# ── DLL discovery (_resolve_publish_dir) ─────────────────────────────────────

def _make_fake_publish(tmp_path: Path) -> Path:
    d = tmp_path / "publish"
    d.mkdir(parents=True)
    (d / "StoryCADLib.dll").write_bytes(b"")  # contents irrelevant to discovery
    return d


@pytest.fixture
def no_fallback_dlls(tmp_path, monkeypatch):
    """Point the bundled-runtime location at an empty directory so discovery
    failure can be tested even on a machine with a populated runtime/."""
    monkeypatch.delenv("STORYCAD_DLL_PATH", raising=False)
    monkeypatch.delenv("STORYCAD_PUBLISH_DIR", raising=False)
    monkeypatch.setattr(storycad, "_BUNDLED_RUNTIME_DIR", tmp_path / "no_bundle")


def test_resolve_explicit_dir(tmp_path):
    d = _make_fake_publish(tmp_path)
    assert storycad._resolve_publish_dir(d) == d.resolve()


def test_resolve_explicit_file_returns_parent(tmp_path):
    d = _make_fake_publish(tmp_path)
    dll = d / "StoryCADLib.dll"
    assert storycad._resolve_publish_dir(dll) == d.resolve()


def test_resolve_via_env_var(tmp_path, monkeypatch):
    d = _make_fake_publish(tmp_path)
    monkeypatch.setenv("STORYCAD_DLL_PATH", str(d))
    assert storycad._resolve_publish_dir(None) == d.resolve()


def test_resolve_via_legacy_env_var(tmp_path, monkeypatch):
    d = _make_fake_publish(tmp_path)
    monkeypatch.delenv("STORYCAD_DLL_PATH", raising=False)
    monkeypatch.setenv("STORYCAD_PUBLISH_DIR", str(d))
    assert storycad._resolve_publish_dir(None) == d.resolve()


def test_resolve_explicit_wins_over_env(tmp_path, monkeypatch):
    explicit = _make_fake_publish(tmp_path / "a")
    other = _make_fake_publish(tmp_path / "b")
    monkeypatch.setenv("STORYCAD_DLL_PATH", str(other))
    assert storycad._resolve_publish_dir(explicit) == explicit.resolve()


def test_resolve_missing_raises_with_tried_paths(tmp_path, no_fallback_dlls):
    missing = tmp_path / "nope"
    with pytest.raises(StoryCADError) as exc:
        storycad._resolve_publish_dir(missing)
    msg = str(exc.value)
    assert "StoryCADLib.dll not found" in msg
    assert str(missing.resolve()) in msg


# ── ElementSummary.from_clr (no real CLR object needed) ──────────────────────

class _FakeGuid:
    def __init__(self, value: str):
        self._value = value

    def ToString(self):
        return self._value


class _FakeElement:
    def __init__(self, guid, name, element_type):
        self.Uuid = guid
        self.Name = name
        self.ElementType = element_type


def test_element_summary_from_clr_maps_fields():
    g = uuid.uuid4()
    fake = _FakeElement(_FakeGuid(str(g)), "Hero", "Character")
    summary = ElementSummary.from_clr(fake)
    assert summary.uuid == g
    assert summary.name == "Hero"
    assert summary.element_type == "Character"


def test_element_summary_from_clr_handles_none_name():
    g = uuid.uuid4()
    fake = _FakeElement(_FakeGuid(str(g)), None, "Scene")
    summary = ElementSummary.from_clr(fake)
    assert summary.name == ""
