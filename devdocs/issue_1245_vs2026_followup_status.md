# Issue 1245: VS2026 Follow-up Status

## Current State
- **Branch**: `issue-1245-vs2026-followup` (in StoryCAD repo)
- **Issue**: https://github.com/storybuilder-org/StoryCAD/issues/1245 (reopened)

## Completed
- Verified VS2026 paths:
  - MSBuild: `/mnt/c/Program Files/Microsoft Visual Studio/18/Community/MSBuild/Current/Bin/MSBuild.exe`
  - vstest: `/mnt/c/Program Files/Microsoft Visual Studio/18/Community/Common7/IDE/CommonExtensions/Microsoft/TestWindow/vstest.console.exe`

- Verified test commands work:
  - **Collaborator**: 226/226 passed
  - **StoryCAD**: 633/634 passed (1 failure - PluginLoadContextTests due to stale env var)

- Correct test commands (using Windows paths, WinAppSDK target):
  ```bash
  # Collaborator
  "/mnt/c/Program Files/Microsoft Visual Studio/18/Community/Common7/IDE/CommonExtensions/Microsoft/TestWindow/vstest.console.exe" "D:\\dev\\src\\Collaborator\\CollaboratorTests\\bin\\x64\\Debug\\net10.0-windows10.0.22621\\CollaboratorTests.dll"

  # StoryCAD
  "/mnt/c/Program Files/Microsoft Visual Studio/18/Community/Common7/IDE/CommonExtensions/Microsoft/TestWindow/vstest.console.exe" "D:\\dev\\src\\StoryCAD\\StoryCADTests\\bin\\x64\\Debug\\net10.0-windows10.0.22621\\StoryCADTests.dll"
  ```

- Updated PluginLoadContextTests.cs line 30: `net8.0-windows10.0.22621.0` → `net10.0-windows10.0.22621`

- Updated documentation with VS2026 commands:
  - `~/.claude/memory/build-commands.md` - All paths updated to VS2026 (version 18), net10.0
  - `/mnt/d/dev/src/StoryCAD/CLAUDE.md` - Build/test commands updated, removed incorrect `dotnet test --project` syntax

## Blocked On
- **Windows sign-out/sign-in required** to propagate `STORYCAD_PLUGIN_DIR` environment variable to all processes
- The env var was updated correctly (confirmed via PowerShell `[Environment]::GetEnvironmentVariable`), but running processes (including vstest.console.exe spawned from WSL) inherit stale values from their parent process chain
- WSL restart alone does NOT refresh Windows environment variables - Windows session restart is required

## Session Notes (2026-01-14)
1. Attempted to run tests - 1 failure due to env var showing old `net9.0` path
2. Confirmed Windows user env var is set correctly to `net10.0` via PowerShell
3. Issue: Child processes inherit env vars from parent, and the parent chain has stale values
4. Fixed fallback path in PluginLoadContextTests.cs (net8.0 → net10.0)
5. Updated all documentation files with correct VS2026 commands
6. Build succeeded after NuGet restore
7. Tests still show 1 failure because env var propagation requires Windows session restart

## After Windows Sign-out/Sign-in
1. Re-run StoryCAD tests - should see 648/648 pass (or 634 pass, 14 skipped)
2. If tests pass, commit changes and create PR
3. Remaining tasks from original issue:
   - Uninstall VS2022 (optional cleanup)
   - Uninstall .NET 8 and .NET 9 (optional cleanup)

## Key Findings
- The `--project` syntax for `dotnet test` was incorrectly added during .NET 10 work - not needed
- Use `vstest.console.exe` which has access to Windows environment variables
- Collaborator has NO .env loading code - relies on Windows env var `OPENAI_API_KEY`
- StoryCAD uses DotEnv to load `.env` files for `SYNCFUSION_TOKEN` and `DOPPLER_TOKEN`
- Windows environment variable changes require session restart (sign-out/sign-in) to propagate to all processes
