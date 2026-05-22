"""Read project metadata from the bundled StoryCADLib.dll at build time.

Reads VERSION_INFO from the PE resource via pefile. If the DLL is not yet
staged (fresh checkout, editable install before first publish), returns
empty values so hatch can still resolve the project.
"""
from __future__ import annotations

from pathlib import Path
from typing import Any

_DLL = Path(__file__).parent / "storycad" / "runtime" / "StoryCADLib.dll"


def _normalise_version(raw: str) -> str:
    """PEP 440-ish: drop trailing .0 segments until <=3 components remain."""
    parts = [p for p in (raw or "").split(".") if p != ""]
    while len(parts) > 3 and parts[-1] == "0":
        parts.pop()
    if len(parts) > 3:
        parts = parts[:3]
    return ".".join(parts) or "0.0.0"


def _read_pe_string_table() -> dict[str, str]:
    if not _DLL.exists():
        return {}
    try:
        import pefile  # type: ignore[import-not-found]
    except ImportError:
        return {}
    out: dict[str, str] = {}
    try:
        pe = pefile.PE(str(_DLL), fast_load=True)
        pe.parse_data_directories(
            directories=[pefile.DIRECTORY_ENTRY["IMAGE_DIRECTORY_ENTRY_RESOURCE"]]
        )
        for fileinfo in getattr(pe, "FileInfo", []) or []:
            for entry in fileinfo:
                if getattr(entry, "Key", b"") != b"StringFileInfo":
                    continue
                for st in entry.StringTable:
                    for k, v in st.entries.items():
                        out[k.decode("utf-8", "replace")] = v.decode("utf-8", "replace")
    except Exception:
        return {}
    return out


_meta = _read_pe_string_table()
_raw_version = _meta.get("FileVersion") or _meta.get("ProductVersion") or "0.0.0"
version = _normalise_version(_raw_version)


try:
    from hatchling.metadata.plugin.interface import MetadataHookInterface
except ImportError:  # not present outside hatch's build env
    MetadataHookInterface = object  # type: ignore[assignment,misc]


class DllMetadataHook(MetadataHookInterface):  # type: ignore[misc,valid-type]
    """Populate PEP 621 dynamic fields from the bundled DLL."""

    def update(self, metadata: dict[str, Any]) -> None:
        desc = _meta.get("FileDescription") or _meta.get("Comments")
        if desc:
            metadata["description"] = desc
        company = _meta.get("CompanyName")
        if company:
            metadata["authors"] = [{"name": company}]


def get_metadata_hook():
    return DllMetadataHook
