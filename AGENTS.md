# Repository Guidelines

## Project Structure & Module Organization
`StoryCAD/` is the WinUI 3 desktop shell (App.xaml, views, assets, manifests). Cross-cutting models, data access, and services belong in `StoryCADLib/`, which should stay UI-agnostic so tests can consume it directly. `StoryCADTests/` hosts the MSTest harness, manual-check spreadsheets, fixtures under `TestInputs/`, and coverage artifacts. Shared build knobs live in `Directory.Build.*` plus `global.json`, which pins Uno.Sdk 6.4.13 on .NET SDK 10.0.100.

## Build, Test, and Development Commands
- `dotnet restore StoryCAD.sln` — hydrate NuGet/Uno feeds; run after branch switches.
- `dotnet build StoryCAD.sln -c Debug` — verify both WinUI and desktop targets; add `-c Release` before packaging.
- `dotnet run --project StoryCAD/StoryCAD.csproj` — launch the client for quick smoke tests.
- `dotnet test StoryCADTests/StoryCADTests.csproj --settings StoryCADTests/mstest.runsettings --logger trx` — executes MSTest suites; append `-f net10.0-windows10.0.22621` to exercise WinUI-specific code.
- `dotnet format StoryCAD.sln` — enforces EditorConfig and Roslyn analyzer suggestions prior to commits.

## Coding Style & Naming Conventions
`.editorconfig` mandates UTF-8, CRLF, trimmed whitespace, and space indentation (four spaces for C#, XAML, XML). Keep `using` directives sorted with `System.*` first, prefer file-scoped namespaces, and name interfaces with an `I` prefix plus PascalCase everywhere else. Dead code analyzers promote unused fields, parameters, and obsolete APIs to warnings; delete unused members instead of suppressing diagnostics.

## Testing Guidelines
Unit and integration suites run on MSTest via the Uno test runner. Place new files alongside the feature (`StoryCADTests/ViewModels/FooTests.cs`) and use descriptive `TestMethod` names such as `SaveCommand_WhenDirty_PromptsUser`. Keep `.env` secrets local; the MSIX test harness copies it automatically. Regenerate coverage with `dotnet test ... /p:CollectCoverage=true`; update the `test_coverage_tree.*` snapshots when behavior changes. Manual scenarios go in `StoryCADTests/ManualTests/Manual Tests.xlsx`; extend the sheet whenever UI paths lack automation.

## Commit & Pull Request Guidelines
Recent history favors short imperative subjects and optional prefixes (`refactor:`, `fix:`) plus explicit issue references (`issue #1088`). Before pushing, ensure `dotnet format` and the full MSTest run succeed and mention those commands in the PR description. Every pull request should link the GitHub issue, summarize the functional impact, and attach before/after screenshots for UI tweaks.

## Configuration & Security Notes
Respect `global.json` when installing SDKs, and avoid editing it unless coordinating with maintainers. Never commit production keys (elmah.io, telemetry, Microsoft Store secrets); keep them in user secrets or untracked `.env` files referenced by the test harness.
