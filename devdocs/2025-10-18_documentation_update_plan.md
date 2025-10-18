# StoryCAD Documentation Update Plan
*Based on platform_targeting_guidelines.md analysis*
*Generated: 2025-10-18*

## Overview

The new `devdocs/platform_targeting_guidelines.md` introduces critical cross-platform development concepts that need to be integrated into existing documentation across three layers: memory files, CLAUDE.md files, and .claude/docs files.

---

## Key Concepts to Integrate

### 1. Multi-Targeting vs Platform-Specific Builds

**Critical Insight**: Building for `net9.0-desktop` on Windows creates a binary that CAN run on Mac, but WITHOUT Mac-specific features if you use platform-specific APIs.

```
Cross-platform code only:
  Windows build → Runs everywhere (one universal binary)

Platform-specific code (AppKit, Win32):
  Windows build → Excludes Mac features (can't compile AppKit)
  Mac build → Excludes Windows features (can't compile Win32)
  Need TWO builds for full feature sets
```

### 2. Cross-Machine Development Workflows

Three strategies documented:
1. **Git-based** (recommended) - Commit/push/pull cycle
2. **Network share** - Rapid iteration, must copy to local for build
3. **Separate folders** - Simultaneous debugging

### 3. Build Strategy Matrix

| Scenario | Build On | Why |
|----------|----------|-----|
| Pure UNO/XAML fix | Either platform | Cross-platform code |
| Added Mac menu (AppKit) | Must build on Mac | Platform APIs |
| Added Windows registry | Must build on Windows | Platform APIs |
| Both features | Need TWO builds | Platform-specific |

### 4. Manual Testing vs Debugging

- **Manual testing**: `dotnet run` on Mac (no IDE needed)
- **Debugging**: Must build on Mac and use Rider (can't remote debug)

---

## File-by-File Update Plan

### Memory Files (`/home/tcox/.claude/memory/`)

#### 1. patterns.md
**Section**: Pattern #9 - Platform-Specific Code

**Add after line 216**:
```markdown
### Multi-Targeting Build Behavior

**Windows can build both targets**:
```xml
<TargetFrameworks>net9.0-desktop;net9.0-windows10.0.22621</TargetFrameworks>
```

**Mac can only build desktop**:
```xml
<TargetFramework>net9.0-desktop</TargetFramework>
```

**Critical**: Platform-specific code determines what's included:

| Code Type | Windows Build | Mac Build |
|-----------|---------------|-----------|
| Cross-platform only | ✅ Runs everywhere | ✅ Runs everywhere |
| Uses AppKit APIs | ⚠️ Mac code EXCLUDED | ✅ Mac code INCLUDED |
| Uses Win32 APIs | ✅ Windows code INCLUDED | ⚠️ Windows code EXCLUDED |

**Example**:
```csharp
#if __MACOS__
using AppKit;
public void SetMacMenu() { /* Mac-only code */ }
#endif
```

**Building on Windows**: The `#if __MACOS__` section is completely skipped. Binary runs on Mac but WITHOUT the Mac menu.

**Building on Mac**: The AppKit code is included. Binary has full Mac features.

**Distribution**: For production, build on each platform to get platform-optimized binaries.
```

**Rationale**: This is THE most important concept from platform_targeting_guidelines.md and needs to be in the quick reference patterns file.

---

#### 2. build-commands.md
**Section**: After "Quick Reference (WSL/Claude Code)"

**Add new section after line 34**:
```markdown
## Multi-Target Builds

### Build Both Targets (Windows Only)
```bash
# Builds both net9.0-desktop AND net9.0-windows10.0.22621
"/mnt/c/Program Files/Microsoft Visual Studio/2022/Community/MSBuild/Current/Bin/MSBuild.exe" StoryCAD.sln -t:Build -p:Configuration=Debug
```

### Build Specific Target
```bash
# Desktop target only (works on Windows or Mac)
"/mnt/c/Program Files/Microsoft Visual Studio/2022/Community/MSBuild/Current/Bin/MSBuild.exe" StoryCAD.sln -t:Build -p:Configuration=Debug -p:TargetFramework=net9.0-desktop

# WinAppSDK target only (Windows only)
"/mnt/c/Program Files/Microsoft Visual Studio/2022/Community/MSBuild/Current/Bin/MSBuild.exe" StoryCAD.sln -t:Build -p:Configuration=Debug -p:TargetFramework=net9.0-windows10.0.22621
```

### macOS Builds (on Mac Mini)
```bash
# Desktop target (only option on Mac)
dotnet build StoryCAD.sln -c Debug -f net9.0-desktop

# Run on Mac for manual testing
dotnet run --project StoryCAD/StoryCAD.csproj -f net9.0-desktop

# Open in Rider for debugging
rider ~/Documents/dev/src/StoryCAD/StoryCAD.sln
```

### Cross-Machine Workflow

**Windows → Mac testing**:
```bash
# On Windows:
git add . && git commit -m "WIP: Test on Mac" && git push origin <branch>

# On Mac:
git pull origin <branch>
dotnet run -f net9.0-desktop  # Quick test
# OR open in Rider for debugging
```
```

**Rationale**: Users need to understand multi-targeting and cross-machine workflows.

---

#### 3. gotchas.md
**Section**: New section "Cross-Platform Gotchas"

**Add after line 173 (after Keyboard Shortcuts section)**:
```markdown
---

## Cross-Platform Development Gotchas

### Platform-Specific Code Compilation

**Gotcha**: Building on Windows excludes Mac-specific code even when targeting `net9.0-desktop`.

**Example**:
```csharp
#if __MACOS__
using AppKit;
public void SetupMacMenu() { /* This code */ }
#endif
```

**What happens**:
- **Build on Windows**: `#if __MACOS__` section is SKIPPED. Binary runs on Mac but missing Mac menu.
- **Build on Mac**: AppKit code is INCLUDED. Binary has full Mac features.

**Best Practice**: For production, build on the target platform to get platform-specific optimizations.

---

### Cross-Machine Testing Workflow

**Gotcha**: Network shares can't be used for builds on Mac.

**Problem**: Building directly from `/Volumes/WindowsShare/StoryCAD` fails on Mac.

**Solution**: Copy to local drive first:
```bash
rsync -av --exclude=bin --exclude=obj ~/WindowsStoryCAD/ ~/StoryCAD-Local/
cd ~/StoryCAD-Local
dotnet build -f net9.0-desktop
```

**Best Practice**: Use git workflow instead of network shares for most development.

---

### Remote Debugging Not Supported

**Gotcha**: Can't debug Mac build from Windows (or vice versa).

**Workaround**:
1. Use logging for remote troubleshooting:
   ```csharp
   #if __MACOS__
       _log.LogInfo($"Mac-specific value: {someVariable}");
   #endif
   ```
2. Debug locally on target platform
3. Use git to sync code between machines

---

### Build on Wrong Platform

**Gotcha**: Forgetting to test on both platforms before committing.

**Symptom**: Code works on Windows but fails/crashes on Mac.

**Best Practice**:
- Test on BOTH platforms before committing to UNOTestBranch
- Use `dotnet run -f net9.0-desktop` on Mac for quick verification
- Open Rider on Mac for debugging Mac-specific issues
```

**Rationale**: These are real pitfalls developers will encounter with cross-platform development.

---

#### 4. NEW FILE: cross-platform.md

**Create**: `/home/tcox/.claude/memory/cross-platform.md`

```markdown
# StoryCAD Cross-Platform Development

*Condensed from `devdocs/platform_targeting_guidelines.md`*
*Last updated: 2025-10-18*

Quick reference for cross-platform development between Windows and macOS.

---

## Platform Targets

| Target | Runs On | Build From | Use Case |
|--------|---------|------------|----------|
| `net9.0-windows10.0.22621` | Windows 10/11 | Windows only | Windows Store, modern Windows |
| `net9.0-desktop` | Windows 7/8/10/11, macOS, Linux | Windows or Mac* | Cross-platform, legacy Windows |

*With platform-specific code caveats - see below.

---

## Critical Concept: Platform-Specific Code

### Without Platform-Specific APIs
- ✅ Build on Windows → Binary runs everywhere
- ✅ Build on Mac → Binary runs everywhere
- One universal binary

### With Platform-Specific APIs (AppKit, Win32, etc.)
- ⚠️ Build on Windows → Mac features EXCLUDED (can't compile AppKit)
- ⚠️ Build on Mac → Windows features EXCLUDED (can't compile Win32)
- Need platform-specific builds for full features

### Example Impact

```csharp
// Windowing.desktop.cs
#if __MACOS__
using AppKit;
public void SetMacMenuBar()
{
    var menu = new NSMenu();
    NSApplication.SharedApplication.MainMenu = menu;
}
#endif
```

**Building on Windows**:
- `#if __MACOS__` section is skipped entirely
- Binary runs on Mac but WITHOUT custom menu
- Missing Mac-specific functionality

**Building on Mac**:
- AppKit code is included
- Binary has full Mac features
- Missing Windows-specific optimizations

**Distribution Strategy**: Build on each platform for production releases.

---

## Development Workflow (Windows Primary)

**Recommended Strategy**: Develop on Windows, test on Mac

### 1. Write Code (Windows - VS2022)
```bash
# Make changes in VS2022
# Test on Windows
# Commit when ready
git add .
git commit -m "Add feature X"
git push origin issue-123-feature-x
```

### 2. Test on Mac
```bash
# On Mac:
git pull origin issue-123-feature-x
dotnet run -f net9.0-desktop  # Quick manual test
```

### 3. Debug on Mac (if needed)
```bash
# On Mac:
rider ~/Documents/dev/src/StoryCAD/StoryCAD.sln
# Set breakpoints and debug normally
```

### 4. Fix Mac Issues (if any)
```bash
# On Mac:
# Make changes in Rider
# Test locally
git add .
git commit -m "Fix Mac-specific issue"
git push origin issue-123-feature-x

# Back on Windows:
git pull origin issue-123-feature-x
```

---

## When to Build Where

| Scenario | Build Platform | Test Platform | Why |
|----------|----------------|---------------|-----|
| Pure UNO/XAML UI | Either | Both | Cross-platform code |
| Business logic | Windows | Both | Faster on familiar platform |
| Mac menu (AppKit) | Mac | Mac only | Platform APIs |
| Windows registry | Windows | Windows only | Platform APIs |
| Performance testing | Both | Both | Platform differences |

---

## Quick Commands

### Windows (WSL)
```bash
# Build both targets
"/mnt/c/Program Files/Microsoft Visual Studio/2022/Community/MSBuild/Current/Bin/MSBuild.exe" StoryCAD.sln -t:Build -p:Configuration=Debug

# Build desktop only
"/mnt/c/Program Files/Microsoft Visual Studio/2022/Community/MSBuild/Current/Bin/MSBuild.exe" StoryCAD.sln -t:Build -p:Configuration=Debug -p:TargetFramework=net9.0-desktop
```

### macOS
```bash
# Build desktop (only option)
dotnet build -f net9.0-desktop

# Run for testing
dotnet run -f net9.0-desktop

# Open in Rider for debugging
rider StoryCAD.sln
```

---

## Manual Testing vs Debugging

### Manual Testing (Quick Check)
**When**: Verify UI looks correct, basic functionality works

**Method**:
```bash
# On Mac (no IDE needed):
dotnet run -f net9.0-desktop
```

### Debugging (Root Cause Analysis)
**When**: Investigating crashes, stepping through code

**Method**:
1. Must build on Mac
2. Open in Rider
3. Set breakpoints
4. Debug normally

**Important**: Can't remote debug from Windows to Mac.

---

## Common Pitfalls

### 1. Building on Wrong Platform
❌ Build on Windows → Test Mac features → Features missing!
✅ Build on Mac → Test Mac features → Works correctly

### 2. Network Share Builds
❌ Build from `/Volumes/WindowsShare` → Fails
✅ Copy to local, then build → Works

### 3. Forgetting to Test Both Platforms
❌ Test only on Windows → Works → Commit → Breaks on Mac
✅ Test on both platforms → Commit → Works everywhere

---

## Comprehensive Documentation

See these files for complete details:

- **[devdocs/platform_targeting_guidelines.md](/mnt/d/dev/src/StoryCAD/devdocs/platform_targeting_guidelines.md)** - 712-line comprehensive guide covering all workflows, IDE setup, decision trees, and troubleshooting
- **[.claude/docs/dependencies/uno-platform-guide.md](/mnt/d/dev/src/StoryCAD/.claude/docs/dependencies/uno-platform-guide.md)** - UNO Platform patterns and API usage
- **[.claude/docs/examples/platform-specific-code.md](/mnt/d/dev/src/StoryCAD/.claude/docs/examples/platform-specific-code.md)** - Copy-paste code examples

---

## Related Memory Files

- `/home/tcox/.claude/memory/patterns.md` - Platform-Specific Code pattern (#9)
- `/home/tcox/.claude/memory/build-commands.md` - Build commands for both platforms
- `/home/tcox/.claude/memory/gotchas.md` - Cross-platform pitfalls
```

**Rationale**: This creates a dedicated cross-platform quick reference that ties together the workflows and critical concepts.

---

### CLAUDE.md Files

#### 1. StoryCAD/CLAUDE.md (Project-Specific)

**Section**: "Platform Development (UNO)"

**Replace lines 8-23 with**:
```markdown
## Platform Development (UNO)

### Multi-Targeting Strategy

**Windows builds both targets**:
- `net9.0-windows10.0.22621` (WinAppSDK head) - Windows 10/11, Windows Store
- `net9.0-desktop` (Desktop head) - Windows 7/8/10/11, macOS, Linux

**macOS builds desktop only**:
- `net9.0-desktop` (Desktop head) - macOS, also runs on Windows/Linux

**Critical Concept - Platform-Specific Code**:

When using **cross-platform APIs only** (standard .NET, UNO controls):
- ✅ Build on either platform → Universal binary runs everywhere

When using **platform-specific APIs** (AppKit, Win32, etc.):
- ⚠️ Build on Windows → Excludes Mac-specific code (can't compile AppKit)
- ⚠️ Build on Mac → Excludes Windows-specific code (can't compile Win32)
- 📦 Need platform-specific builds for production distribution

**Example**:
```csharp
#if __MACOS__
using AppKit;  // This code ONLY compiles on Mac
public void SetupMacMenu() { }
#endif
```

See `/devdocs/platform_targeting_guidelines.md` for complete workflows and decision trees.

### Cross-Machine Development Workflow

**Primary Strategy**: Develop on Windows (VS2022), test on Mac (Rider)

1. **Code on Windows**:
   - Write code in VS2022 (familiar environment)
   - Test on Windows (WinAppSDK head)
   - Commit and push

2. **Test on Mac**:
   ```bash
   git pull
   dotnet run -f net9.0-desktop  # Quick verification
   ```

3. **Debug on Mac** (if needed):
   - Open in Rider on Mac Mini
   - Debug Mac-specific issues
   - Commit fixes and pull back to Windows

**Alternative Workflows**:
- Network share (rapid iteration, must copy to local for build)
- Separate build folders (simultaneous debugging)

See `devdocs/platform_targeting_guidelines.md` for detailed workflow comparison.

### Working with UNO Platform
```

**Add new section after "Platform Development (UNO)"**:
```markdown
### When to Build on Which Platform

Use this decision tree to determine where to develop:

| Issue Type | Build Platform | Test Platform | Rationale |
|------------|----------------|---------------|-----------|
| UNO Platform bugs | Windows | Both | Cross-platform testing required |
| Pure XAML/UI | Windows | Both | Faster in familiar IDE |
| Business logic | Windows | Both | No platform-specific code |
| Mac-specific features | Mac | Mac only | Requires AppKit/Mac APIs |
| Windows-specific features | Windows | Windows only | Requires Win32/WinAppSDK APIs |
| Performance optimization | Both | Both | Platform differences matter |

**Key Points**:
- ~90% of StoryCAD code is shared/cross-platform → Develop on Windows
- Mac-specific features (AppKit menus, sandboxing) → Must build on Mac
- Always test on both platforms before merging to UNOTestBranch

### Platform-Specific Testing

**Quick Manual Test (Mac)**:
```bash
# No IDE needed:
dotnet run -f net9.0-desktop
```

**Debugging (Mac)**:
```bash
# Must build on Mac, use Rider:
rider ~/Documents/dev/src/StoryCAD/StoryCAD.sln
# Set breakpoints and debug normally
```

**Important**: Cannot remote debug from Windows to Mac. Use logging for remote troubleshooting or debug locally on Mac.
```

**Rationale**: Project CLAUDE.md needs actionable guidance on when to use which platform and how to work across machines.

---

#### 2. /home/tcox/.claude/CLAUDE.md (User Global)

**Section**: "Development Notes"

**Add after line 11**:
```markdown
## Cross-Platform Development (StoryCAD)

When working on StoryCAD UNO Platform development:

### Build Strategy
- **Primary development**: Windows (VS2022) - familiar, faster iteration
- **Mac testing**: After each significant change (UI, file I/O, platform-specific)
- **Mac debugging**: Use Rider when Mac-specific issues occur

### Critical Understanding
- Building `net9.0-desktop` on Windows creates a binary that runs on Mac
- BUT if code uses Mac-specific APIs (AppKit), those features are EXCLUDED from Windows build
- For production releases with full platform features, build on each target platform

### Workflow
1. Code on Windows (VS2022)
2. Test on Windows
3. Push to git
4. Pull on Mac
5. `dotnet run -f net9.0-desktop` for quick test
6. Open Rider if debugging needed
7. Fix Mac issues if any
8. Push fixes back to Windows

See `StoryCAD/devdocs/platform_targeting_guidelines.md` for comprehensive workflows.
```

**Rationale**: User's global policy should have a quick reminder of the cross-platform workflow since this is a recurring pattern.

---

### .claude/docs Files

#### 1. .claude/docs/architecture/build-commands.md

**Section**: Add new section after "dotnet CLI"

**Add**:
```markdown
---

## Multi-Target Builds (UNO Platform)

### Building Both Targets (Windows Only)

Windows can build both WinAppSDK and Desktop heads in one command:

```bash
# Builds net9.0-desktop AND net9.0-windows10.0.22621
msbuild StoryCAD.sln /t:Build /p:Configuration=Debug
```

### Building Specific Target

```bash
# Desktop target only (works on Windows or Mac)
msbuild StoryCAD.sln /t:Build /p:Configuration=Debug /p:TargetFramework=net9.0-desktop

# WinAppSDK target only (Windows only)
msbuild StoryCAD.sln /t:Build /p:Configuration=Debug /p:TargetFramework=net9.0-windows10.0.22621
```

### macOS Builds

From macOS Terminal:

```bash
# Build desktop target (only option on Mac)
dotnet build StoryCAD.sln -c Debug -f net9.0-desktop

# Run on Mac for manual testing
dotnet run --project StoryCAD/StoryCAD.csproj -f net9.0-desktop

# Run tests on Mac
dotnet test StoryCADTests/StoryCADTests.csproj -f net9.0-desktop
```

### Cross-Machine Development

**Git-based workflow** (recommended):

```bash
# On Windows:
git add .
git commit -m "WIP: Test on Mac"
git push origin <branch>

# On Mac:
cd ~/Documents/dev/src/StoryCAD
git pull origin <branch>
dotnet build -f net9.0-desktop
dotnet run -f net9.0-desktop  # Manual test
# OR: open in Rider for debugging
```

**Network share workflow** (rapid iteration):

```bash
# On Mac:
# Copy from network share to local (can't build on network drive)
rsync -av --exclude=bin --exclude=obj ~/WindowsStoryCAD/ ~/StoryCAD-Local/
cd ~/StoryCAD-Local
dotnet build -f net9.0-desktop
```

---

## Platform-Specific Build Behavior

### Cross-Platform Code Only

If your code uses only UNO Platform and standard .NET APIs (no AppKit, no Win32):

- Build on Windows → Binary runs on Windows, Mac, Linux
- Build on Mac → Binary runs on Windows, Mac, Linux
- **One universal binary**

### Platform-Specific Code (AppKit, Win32, etc.)

If your code uses platform-specific APIs:

```csharp
#if __MACOS__
using AppKit;
public void SetupMacMenu() { /* Mac-only */ }
#endif

#if HAS_UNO_WINUI
using Windows.Win32;
public void SetupWindowsRegistry() { /* Windows-only */ }
#endif
```

- Build on Windows → Mac-specific code EXCLUDED (can't compile AppKit)
- Build on Mac → Windows-specific code EXCLUDED (can't compile Win32)
- **Need platform-specific builds**

**Distribution Strategy**: For production releases, build on each platform to get full platform-specific features.

See `devdocs/platform_targeting_guidelines.md` for complete details.
```

**Rationale**: Developers need to understand multi-targeting behavior and when builds differ.

---

#### 2. .claude/docs/dependencies/uno-platform-guide.md

**Section**: After "StoryCAD's Platform Targets" (around line 44)

**Add new section**:
```markdown
---

## Multi-Targeting vs Platform-Specific Builds

### Understanding the Difference

**Multi-Targeting** (what it means):
- Windows can build for BOTH `net9.0-desktop` and `net9.0-windows10.0.22621` targets
- macOS can only build for `net9.0-desktop` target

**Platform-Specific Builds** (what gets included):
- Determines which platform-specific code is compiled
- Independent of which targets you're building for

### The Critical Concept

**If you only use cross-platform APIs**:
```
Windows build → Desktop binary runs on Windows/Mac/Linux ✅
Mac build → Desktop binary runs on Windows/Mac/Linux ✅
One universal binary
```

**If you use platform-specific APIs** (AppKit, Win32):
```
Windows build → Desktop binary:
  - Includes Windows-specific code ✅
  - EXCLUDES Mac-specific code ❌ (can't compile AppKit on Windows)
  - Runs on Mac but missing Mac features

Mac build → Desktop binary:
  - Includes Mac-specific code ✅
  - EXCLUDES Windows-specific code ❌ (can't compile Win32 on Mac)
  - Runs on Windows but missing Windows features

Two platform-specific binaries needed for full features
```

### Real Example from StoryCAD

```csharp
// Windowing.desktop.cs
#if __MACOS__
using AppKit;  // macOS-only namespace

public partial void InitializeWindow()
{
    var menu = new NSMenu();
    NSApplication.SharedApplication.MainMenu = menu;
}
#endif
```

**What happens**:

| Build Platform | `#if __MACOS__` symbol | Result |
|----------------|------------------------|--------|
| Windows | ❌ Not defined | AppKit code SKIPPED, binary runs on Mac without menu |
| macOS | ✅ Defined | AppKit code INCLUDED, binary has full Mac features |

### Distribution Implications

**For development/testing**:
- Build on Windows (faster, familiar tools)
- Test on both platforms
- Cross-platform code works everywhere

**For production releases**:
- Build on Windows → Windows distribution
- Build on Mac → macOS distribution
- Each platform gets optimized binary with platform-specific features

### Quick Decision: Where to Build

| Scenario | Build Where | Why |
|----------|-------------|-----|
| Pure XAML UI fix | Either platform | Cross-platform code only |
| Added Mac menu (AppKit) | Must build on Mac | Uses Mac-specific APIs |
| Added Windows registry code | Must build on Windows | Uses Windows-specific APIs |
| Both platform features | Both platforms | Need two optimized binaries |
| Testing cross-platform code | Windows (faster) | Works everywhere |

See `devdocs/platform_targeting_guidelines.md` for comprehensive workflow guidance.

---
```

**Rationale**: This is THE most important concept for cross-platform development and needs to be explained clearly in the UNO Platform guide.

---

#### 3. .claude/docs/examples/platform-specific-code.md

**Section**: Add new section at the end (before "Quick Reference")

**Add**:
```markdown
---

## Example 10: Multi-Platform Build Matrix

### Understanding What Gets Built Where

This example demonstrates how platform-specific code behaves in different build scenarios.

### Scenario: Cross-Platform Feature with Platform Enhancements

```csharp
// FilePickerService.cs (shared)
public partial class FilePickerService
{
    // Shared logic
    public partial Task<string> PickFileAsync();

    public async Task<List<string>> PickMultipleFilesAsync()
    {
        // Shared implementation that calls platform-specific code
        var files = new List<string>();
        var firstFile = await PickFileAsync();
        if (firstFile != null)
        {
            files.Add(firstFile);
        }
        return files;
    }
}

// FilePickerService.WinAppSDK.cs (Windows-specific)
#if HAS_UNO_WINUI
using Windows.Storage.Pickers;

public partial class FilePickerService
{
    public partial async Task<string> PickFileAsync()
    {
        var picker = new FileOpenPicker();
        picker.FileTypeFilter.Add(".stbx");

        // Windows-specific initialization
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        var file = await picker.PickSingleFileAsync();
        return file?.Path;
    }
}
#endif

// FilePickerService.desktop.cs (macOS-specific)
#if __MACOS__
using AppKit;

public partial class FilePickerService
{
    public partial async Task<string> PickFileAsync()
    {
        await Task.CompletedTask;

        var panel = NSOpenPanel.OpenPanel;
        panel.AllowedFileTypes = new[] { "stbx" };
        panel.CanChooseFiles = true;

        // Mac-specific native dialog
        if (panel.RunModal() == 1)
        {
            return panel.Url?.Path;
        }

        return null;
    }
}
#endif
```

### Build Matrix: What Gets Included

| Build Platform | Target Framework | Files Compiled | Result |
|----------------|------------------|----------------|--------|
| **Windows** | net9.0-windows10.0.22621 | FilePickerService.cs<br>FilePickerService.WinAppSDK.cs | Windows-optimized binary with WinAppSDK picker |
| **Windows** | net9.0-desktop | FilePickerService.cs<br>~~FilePickerService.desktop.cs~~ ❌ | Runs on Mac but WITHOUT native Mac picker* |
| **macOS** | net9.0-desktop | FilePickerService.cs<br>FilePickerService.desktop.cs | Mac-optimized binary with AppKit picker |

*The `#if __MACOS__` symbol is NOT defined when building on Windows, so FilePickerService.desktop.cs code is excluded even though you're building the desktop target.

### Testing Matrix

| Built On | Tested On | File Picker Behavior |
|----------|-----------|---------------------|
| Windows (WinAppSDK) | Windows | ✅ Native Windows picker |
| Windows (Desktop) | Windows | ✅ Native Windows picker |
| Windows (Desktop) | macOS | ⚠️ Missing Mac picker (fallback behavior) |
| macOS (Desktop) | macOS | ✅ Native Mac picker |
| macOS (Desktop) | Windows | ⚠️ Missing Windows picker (fallback behavior) |

### Production Distribution Strategy

**Option 1: Cross-Platform Binary (Limited Features)**
- Build on Windows for desktop target
- Ships same binary for Windows and Mac
- Mac users get fallback file picker (not native)

**Option 2: Platform-Optimized Binaries (Recommended)**
- Build on Windows → Windows distribution
- Build on Mac → macOS distribution
- Each platform gets native file picker
- Best user experience

### Key Takeaway

The platform you BUILD on determines which platform-specific code is COMPILED, not which platforms the binary CAN run on.

```
Desktop target runs on: Windows, Mac, Linux
But includes platform code from: Build platform only
```

---
```

**Rationale**: A concrete example with a build matrix makes this concept crystal clear.

---

## Summary of Changes

### Memory Files (4 files)
1. **patterns.md** - Add multi-targeting build behavior to Pattern #9
2. **build-commands.md** - Add multi-target build commands and cross-machine workflow
3. **gotchas.md** - Add cross-platform development gotchas
4. **cross-platform.md** (NEW) - Dedicated cross-platform quick reference

### CLAUDE.md Files (2 files)
1. **StoryCAD/CLAUDE.md** - Expand Platform Development section with workflows and decision trees
2. **/home/tcox/.claude/CLAUDE.md** - Add cross-platform development quick reference

### .claude/docs Files (3 files)
1. **build-commands.md** - Add multi-target builds and platform-specific build behavior
2. **uno-platform-guide.md** - Add multi-targeting vs platform-specific builds section
3. **platform-specific-code.md** - Add build matrix example

### Total: 9 file updates (8 updates + 1 new file)

---

## Implementation Priority

**High Priority** (Critical concepts):
1. Memory: cross-platform.md (NEW) - Central reference
2. Memory: patterns.md - Most-referenced file
3. CLAUDE.md: StoryCAD/CLAUDE.md - Project-specific guidance

**Medium Priority** (Comprehensive docs):
4. .claude/docs: uno-platform-guide.md - Key dependency guide
5. Memory: build-commands.md - Frequently used
6. .claude/docs: build-commands.md - Comprehensive build docs

**Low Priority** (Additional context):
7. Memory: gotchas.md - Nice to have
8. CLAUDE.md: user global - User reminder
9. .claude/docs: platform-specific-code.md - Additional example

---

## Notes

All changes preserve existing content and only ADD new sections. No existing content is removed or significantly restructured.

Cross-references between files are maintained and enhanced.

The new `platform_targeting_guidelines.md` in devdocs remains the comprehensive source, with these updates providing condensed, actionable guidance in the appropriate documentation layers.
