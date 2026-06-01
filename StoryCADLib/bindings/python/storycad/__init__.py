"""Python wrapper around StoryCADLib.Services.API.StoryCADApi.

Hosts the CLR in-process via pythonnet and exposes a Pythonic, snake_case
surface over the C# API. Errors surface as StoryCADError instead of
OperationResult<T> sentinels. Async (Task<T>) methods are joined inline.
"""
from __future__ import annotations

import json
import os
import sys
import uuid
from dataclasses import dataclass
from importlib.metadata import PackageNotFoundError, version as _pkg_version
from pathlib import Path
from typing import Any

try:
    # Single source of truth: the version baked into the installed wheel
    # (derived from StoryCADLib.dll's FileVersion at build time). Falls back
    # when running from an un-installed source checkout.
    __version__ = _pkg_version("storycad")
except PackageNotFoundError:
    __version__ = "0.0.0+source"
__all__ = [
    "StoryCAD",
    "StoryCADError",
    "ElementSummary",
    "Beat",
    "ProblemStructure",
    "KeyQuestion",
    "PlotScene",
    "BeatSheet",
]

# ─── runtime bootstrap ──────────────────────────────────────────────────────
# Lazy: the CLR is loaded the first time StoryCAD() is constructed, not at
# import time. This keeps `import storycad` cheap and lets the caller install
# `pythonnet` / point at a publish dir before paying the cost.

_BUNDLED_RUNTIME_DIR = Path(__file__).parent / "runtime"

_loaded = False
_api_type = None
_bootstrapper = None
_item_type = None
_Ioc = None
_NetGuid = None
_NetString = None
_NetObject = None
_NetDict = None


class StoryCADError(RuntimeError):
    """Raised when an OperationResult comes back with IsSuccess=False."""


def _resolve_publish_dir(publish_dir: Path | None) -> Path:
    """Find a directory containing StoryCADLib.dll. Search order:

    1. Explicit publish_dir= argument.
    2. STORYCAD_DLL_PATH env var (file or directory; STORYCAD_PUBLISH_DIR
       is also honoured for backwards compatibility).
    3. Bundled runtime/ directory inside this package.
    """
    tried: list[Path] = []

    def _candidate(raw: str | Path) -> Path:
        p = Path(raw).expanduser().resolve()
        return p.parent if p.is_file() else p

    if publish_dir is not None:
        cand = _candidate(publish_dir)
        tried.append(cand)
        if (cand / "StoryCADLib.dll").exists():
            return cand

    env = os.environ.get("STORYCAD_DLL_PATH") or os.environ.get("STORYCAD_PUBLISH_DIR")
    if env:
        cand = _candidate(env)
        tried.append(cand)
        if (cand / "StoryCADLib.dll").exists():
            return cand

    bundled = _BUNDLED_RUNTIME_DIR.resolve()
    tried.append(bundled)
    if (bundled / "StoryCADLib.dll").exists():
        return bundled

    paths = "\n  ".join(str(p) for p in tried)
    raise StoryCADError(
        "StoryCADLib.dll not found. Tried:\n  " + paths + "\n"
        "Set STORYCAD_DLL_PATH, pass publish_dir=... to StoryCAD(), "
        "or install a wheel built via bindings/python/build-wheel.sh."
    )


def _ensure_loaded(publish_dir: Path | None = None) -> None:
    global _loaded, _api_type, _bootstrapper, _item_type
    global _Ioc, _NetGuid, _NetString, _NetObject, _NetDict
    if _loaded:
        return

    pdir = _resolve_publish_dir(publish_dir)

    from pythonnet import load
    load("coreclr")
    import clr  # noqa: F401  # side-effect: enables AddReference
    sys.path.insert(0, str(pdir))
    clr.AddReference("StoryCADLib")

    from StoryCADLib.Services.API import StoryCADApi as _StoryCADApi
    from StoryCADLib.Services.IoC import BootStrapper as _BootStrapper
    from StoryCADLib.Models import StoryItemType as _StoryItemType
    from CommunityToolkit.Mvvm.DependencyInjection import Ioc as _Ioc_
    from System import Guid as _Guid, String as _String, Object as _Object
    from System.Collections.Generic import Dictionary as _Dictionary

    _api_type = _StoryCADApi
    _bootstrapper = _BootStrapper
    _item_type = _StoryItemType
    _Ioc = _Ioc_
    _NetGuid = _Guid
    _NetString = _String
    _NetObject = _Object
    _NetDict = _Dictionary
    _loaded = True


# ─── helpers ────────────────────────────────────────────────────────────────

def _unwrap(op: Any, label: str = "") -> Any:
    if not op.IsSuccess:
        raise StoryCADError(f"{label}: {op.ErrorMessage}")
    return op.Payload


def _sync(task: Any) -> Any:
    return task.GetAwaiter().GetResult()


def _to_net_guid(g: uuid.UUID | str | Any) -> Any:
    if isinstance(g, uuid.UUID):
        return _NetGuid.Parse(str(g))
    if isinstance(g, str):
        return _NetGuid.Parse(g)
    # Already a System.Guid or has ToString()
    return _NetGuid.Parse(g.ToString()) if hasattr(g, "ToString") else _NetGuid.Parse(str(g))


def _from_net_guid(g: Any) -> uuid.UUID | None:
    if g is None:
        return None
    return uuid.UUID(g.ToString())


def _to_net_dict(d: dict[str, Any]) -> Any:
    # Flat only: values are inserted as-is (no recursive marshaling). Nested
    # dicts/lists will land as Python objects in the .NET dictionary, which
    # the C# property setters won't know how to coerce.
    nd = _NetDict[_NetString, _NetObject]()
    for k, v in d.items():
        nd[k] = v
    return nd


def _dict_from_net(d: Any) -> dict[str, Any]:
    # Iterating a .NET Dictionary yields KeyValuePair objects, not (k, v)
    # tuples, so access .Key/.Value explicitly rather than unpacking.
    return {str(kv.Key): kv.Value for kv in d}


# ─── dataclasses ────────────────────────────────────────────────────────────

@dataclass(frozen=True)
class ElementSummary:
    uuid: uuid.UUID
    name: str
    element_type: str

    @classmethod
    def from_clr(cls, e: Any) -> "ElementSummary":
        return cls(
            uuid=_from_net_guid(e.Uuid),
            name=str(e.Name) if e.Name is not None else "",
            element_type=str(e.ElementType),
        )


@dataclass(frozen=True)
class Beat:
    title: str
    description: str
    linked_element: uuid.UUID | None


@dataclass(frozen=True)
class ProblemStructure:
    title: str
    description: str
    beats: list[Beat]


@dataclass(frozen=True)
class KeyQuestion:
    topic: str
    question: str


@dataclass(frozen=True)
class PlotScene:
    title: str
    notes: str


@dataclass(frozen=True)
class BeatSheet:
    description: str
    beats: list[Beat]  # linked_element is always None for templates


# ─── facade ─────────────────────────────────────────────────────────────────

class StoryCAD:
    def __init__(self, publish_dir: str | Path | None = None, headless: bool = False):
        _ensure_loaded(Path(publish_dir) if publish_dir else None)
        _bootstrapper.Initialise(headless)
        self._api = _Ioc.Default.GetRequiredService[_api_type]()

    # ── enum passthrough ──────────────────────────────────────────────────
    @property
    def item_type(self) -> Any:
        return _item_type

    # ── lifecycle ─────────────────────────────────────────────────────────
    def create_empty_outline(self, name: str, author: str, template_index: str = "0") -> list[uuid.UUID]:
        guids = _unwrap(
            _sync(self._api.CreateEmptyOutline(name, author, template_index)),
            "create_empty_outline",
        )
        return [_from_net_guid(g) for g in guids]

    def write_outline(self, file_path: str) -> None:
        _unwrap(_sync(self._api.WriteOutline(file_path)), "write_outline")

    def open_outline(self, path: str) -> None:
        _unwrap(_sync(self._api.OpenOutline(path)), "open_outline")

    # ── elements ──────────────────────────────────────────────────────────
    def get_all_elements(self) -> list[ElementSummary]:
        coll = _unwrap(self._api.GetAllElements(), "get_all_elements")
        return [ElementSummary.from_clr(e) for e in coll]

    def get_elements_by_type(self, element_type: Any) -> list[ElementSummary]:
        items = _unwrap(self._api.GetElementsByType(element_type), "get_elements_by_type")
        return [ElementSummary.from_clr(e) for e in items]

    def get_story_world(self) -> ElementSummary | None:
        e = _unwrap(self._api.GetStoryWorld(), "get_story_world")
        return ElementSummary.from_clr(e) if e is not None else None

    def get_story_element(self, guid: uuid.UUID) -> ElementSummary:
        e = _unwrap(self._api.GetStoryElement(_to_net_guid(guid)), "get_story_element")
        return ElementSummary.from_clr(e)

    def get_element(self, guid: uuid.UUID) -> str:
        # Returns the JSON-serialized element string. Callers parse as they need.
        payload = _unwrap(self._api.GetElement(_to_net_guid(guid)), "get_element")
        return str(payload)

    def add_element(
        self,
        type_to_add: Any,
        parent_guid: uuid.UUID | str,
        name: str,
        guid_override: str = "",
    ) -> uuid.UUID:
        g = _unwrap(
            self._api.AddElement(type_to_add, str(parent_guid), name, guid_override),
            "add_element",
        )
        return _from_net_guid(g)

    def add_element_with_properties(
        self,
        type_to_add: Any,
        parent_guid: uuid.UUID | str,
        name: str,
        properties: dict[str, Any],
        guid_override: str = "",
    ) -> uuid.UUID:
        g = _unwrap(
            self._api.AddElement(
                type_to_add, str(parent_guid), name, _to_net_dict(properties), guid_override
            ),
            "add_element_with_properties",
        )
        return _from_net_guid(g)

    def update_element_property(self, element_uuid: uuid.UUID, property_name: str, value: Any) -> ElementSummary:
        e = _unwrap(
            self._api.UpdateElementProperty(_to_net_guid(element_uuid), property_name, value),
            "update_element_property",
        )
        return ElementSummary.from_clr(e)

    def update_element_properties(self, element_guid: uuid.UUID, properties: dict[str, Any]) -> None:
        _unwrap(
            self._api.UpdateElementProperties(_to_net_guid(element_guid), _to_net_dict(properties)),
            "update_element_properties",
        )

    def update_story_element(self, new_element: str | dict, guid: uuid.UUID) -> None:
        # `new_element` is a JSON-serialized StoryElement string, or a dict that
        # this method serialises for you. Anything else gets a confusing JSON
        # parse error from C# (it calls .ToString() and tries to deserialise),
        # so reject it up front.
        if isinstance(new_element, dict):
            new_element = json.dumps(new_element)
        elif not isinstance(new_element, str):
            raise TypeError(
                f"update_story_element expects str or dict, got {type(new_element).__name__}"
            )
        _unwrap(
            self._api.UpdateStoryElement(new_element, _to_net_guid(guid)),
            "update_story_element",
        )

    def delete_story_element(self, guid: uuid.UUID | str) -> None:
        _unwrap(self._api.DeleteStoryElement(str(guid)), "delete_story_element")

    def delete_element(self, element_to_delete: uuid.UUID) -> None:
        _unwrap(
            _sync(self._api.DeleteElement(_to_net_guid(element_to_delete))),
            "delete_element",
        )

    def move_element(self, element_guid: uuid.UUID, new_parent_guid: uuid.UUID, index: int | None = None) -> None:
        _unwrap(
            self._api.MoveElement(_to_net_guid(element_guid), _to_net_guid(new_parent_guid), index),
            "move_element",
        )

    # ── relationships / cast ──────────────────────────────────────────────
    def add_cast_member(self, scene: uuid.UUID, character: uuid.UUID) -> None:
        _unwrap(
            self._api.AddCastMember(_to_net_guid(scene), _to_net_guid(character)),
            "add_cast_member",
        )

    def add_relationship(
        self, source: uuid.UUID, recipient: uuid.UUID, desc: str, mirror: bool = False
    ) -> None:
        _unwrap(
            self._api.AddRelationship(_to_net_guid(source), _to_net_guid(recipient), desc, mirror),
            "add_relationship",
        )

    # ── search ────────────────────────────────────────────────────────────
    def search_for_text(self, search_text: str) -> list[dict[str, Any]]:
        raw = _unwrap(self._api.SearchForText(search_text), "search_for_text")
        return [_dict_from_net(d) for d in raw]

    # Alias for parity with the C# `Search` name.
    search = search_for_text

    def search_for_references(self, target_uuid: uuid.UUID) -> list[dict[str, Any]]:
        raw = _unwrap(
            self._api.SearchForReferences(_to_net_guid(target_uuid)),
            "search_for_references",
        )
        return [_dict_from_net(d) for d in raw]

    def remove_references(self, target_uuid: uuid.UUID) -> int:
        return int(_unwrap(self._api.RemoveReferences(_to_net_guid(target_uuid)), "remove_references"))

    def search_in_subtree(self, root_node_guid: uuid.UUID, search_text: str) -> list[dict[str, Any]]:
        raw = _unwrap(
            self._api.SearchInSubtree(_to_net_guid(root_node_guid), search_text),
            "search_in_subtree",
        )
        return [_dict_from_net(d) for d in raw]

    # ── trash ─────────────────────────────────────────────────────────────
    def restore_from_trash(self, element_to_restore: uuid.UUID) -> None:
        _unwrap(
            _sync(self._api.RestoreFromTrash(_to_net_guid(element_to_restore))),
            "restore_from_trash",
        )

    def empty_trash(self) -> None:
        _unwrap(_sync(self._api.EmptyTrash()), "empty_trash")

    # ── collections ───────────────────────────────────────────────────────
    def add_collection_entry(self, element: uuid.UUID, property_name: str, entry: Any) -> int:
        # A dict entry must be marshaled to a .NET Dictionary so the C# side
        # can deserialize it into the collection's element type; scalar entries
        # (e.g. a string for a List<string>) pass through unchanged.
        if isinstance(entry, dict):
            entry = _to_net_dict(entry)
        return int(_unwrap(
            self._api.AddCollectionEntry(_to_net_guid(element), property_name, entry),
            "add_collection_entry",
        ))

    def update_collection_entry(self, element: uuid.UUID, property_name: str, index: int, entry: Any) -> None:
        if isinstance(entry, dict):
            entry = _to_net_dict(entry)
        _unwrap(
            self._api.UpdateCollectionEntry(_to_net_guid(element), property_name, index, entry),
            "update_collection_entry",
        )

    def remove_collection_entry(self, element: uuid.UUID, property_name: str, index: int) -> None:
        _unwrap(
            self._api.RemoveCollectionEntry(_to_net_guid(element), property_name, index),
            "remove_collection_entry",
        )

    # ── resources ─────────────────────────────────────────────────────────
    def get_examples(self, property_name: str) -> list[str]:
        return [str(x) for x in _unwrap(self._api.GetExamples(property_name), "get_examples")]

    def get_conflict_categories(self) -> list[str]:
        return [str(x) for x in _unwrap(self._api.GetConflictCategories(), "get_conflict_categories")]

    def get_conflict_subcategories(self, category: str) -> list[str]:
        return [str(x) for x in _unwrap(
            self._api.GetConflictSubcategories(category), "get_conflict_subcategories",
        )]

    def get_conflict_examples(self, category: str, subcategory: str) -> list[str]:
        return [str(x) for x in _unwrap(
            self._api.GetConflictExamples(category, subcategory), "get_conflict_examples",
        )]

    def apply_conflict_to_protagonist(self, problem_guid: uuid.UUID, conflict_text: str) -> None:
        _unwrap(
            self._api.ApplyConflictToProtagonist(_to_net_guid(problem_guid), conflict_text),
            "apply_conflict_to_protagonist",
        )

    def apply_conflict_to_antagonist(self, problem_guid: uuid.UUID, conflict_text: str) -> None:
        _unwrap(
            self._api.ApplyConflictToAntagonist(_to_net_guid(problem_guid), conflict_text),
            "apply_conflict_to_antagonist",
        )

    # ── key questions ─────────────────────────────────────────────────────
    def get_key_question_elements(self) -> list[str]:
        return [str(x) for x in _unwrap(
            self._api.GetKeyQuestionElements(), "get_key_question_elements",
        )]

    def get_key_questions(self, element_type: str) -> list[KeyQuestion]:
        raw = _unwrap(self._api.GetKeyQuestions(element_type), "get_key_questions")
        return [KeyQuestion(topic=str(q.Item1), question=str(q.Item2)) for q in raw]

    # ── master plots ──────────────────────────────────────────────────────
    def get_master_plot_names(self) -> list[str]:
        return [str(x) for x in _unwrap(self._api.GetMasterPlotNames(), "get_master_plot_names")]

    def get_master_plot_notes(self, plot_name: str) -> str:
        return str(_unwrap(self._api.GetMasterPlotNotes(plot_name), "get_master_plot_notes"))

    def get_master_plot_scenes(self, plot_name: str) -> list[PlotScene]:
        raw = _unwrap(self._api.GetMasterPlotScenes(plot_name), "get_master_plot_scenes")
        return [PlotScene(title=str(s.Item1), notes=str(s.Item2)) for s in raw]

    # ── stock scenes ──────────────────────────────────────────────────────
    def get_stock_scene_categories(self) -> list[str]:
        return [str(x) for x in _unwrap(
            self._api.GetStockSceneCategories(), "get_stock_scene_categories",
        )]

    def get_stock_scenes(self, category: str) -> list[str]:
        return [str(x) for x in _unwrap(self._api.GetStockScenes(category), "get_stock_scenes")]

    # ── beats ─────────────────────────────────────────────────────────────
    def get_beat_sheet_names(self) -> list[str]:
        return [str(x) for x in _unwrap(self._api.GetBeatSheetNames(), "get_beat_sheet_names")]

    def get_beat_sheet(self, beat_sheet_name: str) -> BeatSheet:
        raw = _unwrap(self._api.GetBeatSheet(beat_sheet_name), "get_beat_sheet")
        beats = [
            Beat(title=str(b.Item1), description=str(b.Item2), linked_element=None)
            for b in raw.Item2
        ]
        return BeatSheet(description=str(raw.Item1), beats=beats)

    def apply_beat_sheet_to_problem(self, problem_guid: uuid.UUID, beat_sheet_name: str) -> None:
        _unwrap(
            self._api.ApplyBeatSheetToProblem(_to_net_guid(problem_guid), beat_sheet_name),
            "apply_beat_sheet_to_problem",
        )

    def get_problem_structure(self, problem_guid: uuid.UUID) -> ProblemStructure:
        raw = _unwrap(self._api.GetProblemStructure(_to_net_guid(problem_guid)), "get_problem_structure")
        beats = [
            Beat(
                title=str(b.Item1),
                description=str(b.Item2),
                linked_element=_from_net_guid(b.Item3) if b.Item3 is not None else None,
            )
            for b in raw.Item3
        ]
        return ProblemStructure(title=str(raw.Item1), description=str(raw.Item2), beats=beats)

    def assign_element_to_beat(self, problem_guid: uuid.UUID, beat_index: int, element_guid: uuid.UUID) -> None:
        _unwrap(
            self._api.AssignElementToBeat(
                _to_net_guid(problem_guid), beat_index, _to_net_guid(element_guid)
            ),
            "assign_element_to_beat",
        )

    def clear_beat_assignment(self, problem_guid: uuid.UUID, beat_index: int) -> None:
        _unwrap(
            self._api.ClearBeatAssignment(_to_net_guid(problem_guid), beat_index),
            "clear_beat_assignment",
        )

    def create_beat(self, problem_guid: uuid.UUID, title: str, description: str) -> None:
        _unwrap(
            self._api.CreateBeat(_to_net_guid(problem_guid), title, description),
            "create_beat",
        )

    def update_beat(self, problem_guid: uuid.UUID, beat_index: int, title: str, description: str) -> None:
        _unwrap(
            self._api.UpdateBeat(_to_net_guid(problem_guid), beat_index, title, description),
            "update_beat",
        )

    def delete_beat(self, problem_guid: uuid.UUID, beat_index: int) -> None:
        _unwrap(self._api.DeleteBeat(_to_net_guid(problem_guid), beat_index), "delete_beat")

    def move_beat(self, problem_guid: uuid.UUID, from_index: int, to_index: int) -> None:
        _unwrap(
            self._api.MoveBeat(_to_net_guid(problem_guid), from_index, to_index),
            "move_beat",
        )

    def save_beat_sheet(self, problem_guid: uuid.UUID, file_path: str) -> None:
        _unwrap(
            self._api.SaveBeatSheet(_to_net_guid(problem_guid), file_path),
            "save_beat_sheet",
        )

    def load_beat_sheet(self, problem_guid: uuid.UUID, file_path: str) -> None:
        _unwrap(
            self._api.LoadBeatSheet(_to_net_guid(problem_guid), file_path),
            "load_beat_sheet",
        )
