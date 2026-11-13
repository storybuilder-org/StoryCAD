# StoryCAD Development Guide

Project-specific guidance for AI coding sessions in the StoryCAD repo. On a maintainer's machine, the shared StoryBuilder standards load from the `CLAUDE.md` in the folder above this repo; this file adds what is particular to StoryCAD.

## About StoryCAD

StoryCAD is a free, open-source outlining application for fiction writers, released under GNU GPL v3. It runs on Windows and macOS through the Uno Platform. A story's data stays on the user's machine. Telemetry and error reporting are opt-in.

## Branches

- `dev` is the default branch. It holds the work for the next release. Scheduled work branches from `dev` and its PR goes into `dev`.
- `main` holds released code. A production problem that must be fixed at once branches from `main`.
- Name a branch `issue-{number}-{description}`, for example `issue-1069-test-coverage`.
- A PR names its issue. Use the pull request template.

## Solution

`StoryCAD.sln` holds six projects:

| Project | Purpose |
|---------|---------|
| StoryCAD | The application (views, assets, manifests) |
| StoryCADLib | Models, data access, services, ViewModels. Ships as a NuGet package, so its public API has users outside this repo |
| CollaboratorLib | The AI Collaborator, compiled into the application |
| StoryCADTests | MSTest tests, manual test plans, automation scripts |
| StoryCADAutomation | The runner for the `.scs` automation scripts |
| StoryCADXamlScan | Static scan of XAML used by the tests |

## Targets

| Target | Runs on |
|--------|---------|
| `net10.0-windows10.0.22621` (WinAppSDK head) | Windows |
| `net10.0-desktop` (desktop head) | macOS, and also Windows |

A Windows machine builds both targets. A Mac builds `net10.0-desktop` only. The .NET SDK and Uno.Sdk versions are pinned in `global.json`.

Write shared code first. Put code that needs one platform's API inside a conditional block, so that the other platform's build leaves it out:

```csharp
#if __MACOS__
using AppKit;  // compiles on a Mac only
#endif
```

Use paths and keyboard shortcuts that work on both platforms. A change that touches platform-specific code needs a test on that platform before it is merged.

## Build and test

Windows (PowerShell):

```powershell
# Build
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" StoryCAD.sln -t:Build -p:Configuration=Debug -p:Platform=x64

# Test. vstest.console.exe sees the Windows environment variables that some tests need
& "C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe" "StoryCADTests\bin\x64\Debug\net10.0-windows10.0.22621\StoryCADTests.dll"

# One test class
& "C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe" "StoryCADTests\bin\x64\Debug\net10.0-windows10.0.22621\StoryCADTests.dll" /Tests:StoryModelTests
```

macOS:

```bash
dotnet run -f net10.0-desktop
```

A debugger on Windows cannot attach to the app on a Mac. Debug Mac problems on the Mac, or use the logs.

## Tests

- Test file: `[SourceFileName]Tests.cs`, one test class for each source class.
- Test method: `MethodName_Scenario_ExpectedResult`, for example `OpenFile_WhenFileNotFound_ThrowsException`.
- Arrange, Act, Assert inside each test. `[TestInitialize]` for setup, `[TestCleanup]` for teardown, `[UITestMethod]` for a test that needs the UI thread.
- Get services from the container with `Ioc.Default.GetRequiredService<T>()`. Do not build them by hand.
- Use `Assert.ThrowsExactly`, not `Assert.ThrowsException`.
- Use real implementations. A mock needs explicit approval for the case.
- A bug fix comes with a test that fails before the fix. Where no automated test is possible, the PR says why and adds a step to a manual test plan.
- Each public method of the API and of `OutlineService` has tests for its success path and its failure paths.
- Unit tests do not drive the UI. UI behavior is covered by the automation scripts in `StoryCADTests/AutomationScripts/` and the manual test plans in `StoryCADTests/ManualTests/`.

## The API has outside users

`StoryCADLib` is published, and other programs call its API. A change to a public API signature can break a program that the maintainers cannot see. Keep existing signatures; add, do not change.

## User manual

The manual's source is in `docs/` (Jekyll, Just the Docs theme). It is published at https://manual.storybuilder.org/ and, for the `dev` branch, at https://beta.manual.storybuilder.org/. Its readers are fiction writers, not programmers. The code and the manual should agree: when a change alters what a user sees, change the manual page in the same PR.

## Older notes

`.claude/docs/` holds older reference notes on architecture, dependencies and examples. They are not kept current. Where one disagrees with the code, the code is right.

## Keep private material out of this repo

This repo is public. Do not add review findings, cost figures, account identifiers, or notes about the Foundation's private repos.
