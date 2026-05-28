"""Shared pytest fixtures for the storycad binding tests.

Layering:
* ``test_pure.py`` needs no CLR and always runs.
* ``test_api.py`` drives the real StoryCADLib via pythonnet. Those tests use
  the ``sc`` / ``outline`` fixtures below, which are skipped automatically when
  no ``StoryCADLib.dll`` can be located (CI without a build, fresh checkout, …).
"""
from __future__ import annotations

import sys
from pathlib import Path

import pytest

# Make the package importable when running from a plain checkout (no install).
sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

import storycad  # noqa: E402


def _dll_available() -> bool:
    """True if the binding can resolve a StoryCADLib.dll without loading it."""
    try:
        storycad._resolve_publish_dir(None)
        return True
    except storycad.StoryCADError:
        return False


requires_dll = pytest.mark.skipif(
    not _dll_available(),
    reason="StoryCADLib.dll not found; set STORYCAD_DLL_PATH to run integration tests",
)


@pytest.fixture(scope="session")
def sc():
    """A single CLR-backed StoryCAD instance shared across the API tests.

    The CLR can only be initialised once per process, so this is session
    scoped. Per-test isolation is provided by the ``outline`` fixture, which
    creates a fresh model for each test.
    """
    if not _dll_available():
        pytest.skip("StoryCADLib.dll not found")
    return storycad.StoryCAD(headless=True)


@pytest.fixture
def outline(sc):
    """Create a fresh empty outline and return (sc, overview_summary).

    ``create_empty_outline`` replaces the current model, giving each test a
    clean slate without re-initialising the CLR.
    """
    sc.create_empty_outline("Test Outline", "pytest", "0")
    overview = next(
        e for e in sc.get_all_elements() if e.element_type == "StoryOverview"
    )
    return sc, overview
