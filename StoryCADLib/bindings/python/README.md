# storycad

Python wrapper around the StoryCAD .NET API
(`StoryCADLib.Services.API.StoryCADApi`). Hosts the CLR in-process via
[`pythonnet`](https://pythonnet.github.io/) and exposes a Pythonic, snake_case
surface; errors surface as `StoryCADError` instead of `OperationResult<T>`
sentinels, and async (`Task<T>`) methods are joined inline.

The wheel bundles a published build of `StoryCADLib` under
`storycad/runtime/`, so end users do **not** need the .NET SDK — only the
.NET 10 runtime (the wheel is built with `--self-contained false`).

## Prerequisites

- Python 3.10+
- .NET 10 runtime installed on the host (https://dotnet.microsoft.com/download)
- A wheel matching your platform (wheels are platform-specific: macOS arm64,
  macOS x64, Linux x64, Linux arm64, …)

## Install

```bash
pip install storycad-<version>-<platform>.whl
```

## Quick example

```python
from storycad import StoryCAD

sc = StoryCAD()
guids = sc.create_empty_outline("My Story", "Author", "0")
overview = next(e for e in sc.get_all_elements() if e.element_type == "StoryOverview")
hero = sc.add_element(sc.item_type.Character, overview.uuid, "Hero")
sc.write_outline("/tmp/my_story.stbx")
```

## Building from source

```bash
git clone https://github.com/storybuilder-org/StoryCAD
cd StoryCAD/bindings/python
./build-wheel.sh   # runs dotnet publish, stages runtime, then python -m build
```

The script detects your host RID, publishes `StoryCADLib` for the matching
framework-dependent target, and produces a platform wheel under `dist/`.

## Known limitations

- Async methods are joined synchronously on the calling thread — no real
  asyncio integration yet.
- Wheels are platform-specific; you need a wheel built for your OS + arch.
- StoryCAD is GPL-3.0-or-later. Bundling its DLL into a wheel inherits that
  license; any project that ships this wheel must comply.
