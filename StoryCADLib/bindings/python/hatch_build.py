"""Hatchling build hook: mark the wheel as platform-specific.

The wheel bundles native .NET libraries under storycad/runtime/, so it
cannot be pure-Python. infer_tag=True asks hatchling to compute a tag
matching the host platform (e.g. cp313-cp313-macosx_15_0_arm64).
"""
from __future__ import annotations

from typing import Any

from hatchling.builders.hooks.plugin.interface import BuildHookInterface


class PlatformWheelHook(BuildHookInterface):
    PLUGIN_NAME = "platform-wheel"

    def initialize(self, version: str, build_data: dict[str, Any]) -> None:
        build_data["pure_python"] = False
        build_data["infer_tag"] = True
