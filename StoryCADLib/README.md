# StoryCADLib

Core library for [StoryCAD](https://github.com/storybuilder-org/StoryCAD) — a free, open-source outline editor for fiction writers. StoryCADLib lets you create, query, mutate, save, and load StoryCAD outlines programmatically. The same library powers the StoryCAD desktop app.

## Install

```bash
dotnet add package StoryCADLib
```

## Target frameworks

- `net10.0-desktop` — cross-platform (Windows, macOS, Linux) via UNO Platform.
- `net10.0-windows10.0.22621` — Windows-specific, links the WinAppSDK head.

## Quick start

StoryCADLib uses a service-locator pattern. Bootstrap once at startup, then resolve the API surface (or individual services) from the container.

```csharp
using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCADLib.Services.API;
using StoryCADLib.Services.IoC;

// Initialise the library. headless: true skips UI-thread-bound services
// and is what you want from any non-WinUI host (console, test, server).
BootStrapper.Initialise(headless: true);

var api = Ioc.Default.GetRequiredService<StoryCADApi>();

// Create a new outline from a built-in template (index 0 = Blank).
var result = await api.CreateEmptyOutline(
    name: "The Old Man and the Sea",
    author: "Ernest Hemingway",
    templateIndex: "0");

if (result.IsSuccess)
{
    foreach (var guid in result.Payload)
        Console.WriteLine(guid);
}
else
{
    Console.Error.WriteLine(result.ErrorMessage);
}
```

All `StoryCADApi` methods return `OperationResult<T>` — no exceptions escape to callers. Check `IsSuccess` and read `ErrorMessage` on failure.

## When to use which entry point

- **`StoryCADApi`** — high-level façade designed for Semantic Kernel / AI agent integration. Stable, narrow surface.
- **`OutlineService`** — lower-level operations on outlines. Use directly when you need fine-grained control.
- **`StoryModel`** — the in-memory document. Mutable; pair with `SerializationLock` for safe concurrent access.

## Documentation

- API guide: <https://manual.storybuilder.org/docs/For%20Developers/Using_the_API.html>
- User manual: <https://manual.storybuilder.org/>
- Source, issues, samples: <https://github.com/storybuilder-org/StoryCAD>

The [`API-Samples`](https://github.com/storybuilder-org/API-Samples) repo has end-to-end examples — see `samples/Outliner` for a WinUI consumer and `samples/ConsistencyValidation` for headless usage.

## License

GNU GPL v3 — see [LICENSE.TXT](https://github.com/storybuilder-org/StoryCAD/blob/main/LICENSE.TXT). The proprietary "Collaborator" module is licensed separately; see `ADDITIONAL-LICENSE-PERMISSIONS.TXT` in the repo for the GPLv3 §7 additional permissions that govern combination with it and App Store distribution.
