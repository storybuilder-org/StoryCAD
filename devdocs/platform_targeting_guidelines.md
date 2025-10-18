# Platform Targeting Guidelines for StoryCAD Development

*Last Updated: 2025-10-18*

This guide helps developers determine which platform to use for development and testing based on issue type, and provides practical guidance for working with Visual Studio 2022 and JetBrains Rider across Windows and macOS.

## Quick Answer for Common Issues

### Issue #1116: [UNO] UI goes off Screen
**Develop on:** Windows 11 with Visual Studio 2022
**Test on:** Both Windows (WinAppSDK) and macOS (desktop head)
**Reason:** Cross-platform UI issue requiring testing on both platforms

---

## Target Framework Overview

| Target | Runs On | Use Case | Build From |
|--------|---------|----------|------------|
| `net9.0-windows10.0.22621` | Windows 10/11 (modern) | Windows Store distribution | Windows only |
| `net9.0-desktop` | **Windows 7/8/10/11** (Win32)<br>**macOS**<br>**Linux** | Cross-platform & legacy Windows | Windows or Mac* |

*With important caveats - see Platform-Specific Code section below

---

## Critical: Platform-Specific Code Changes Everything

### The Simple Case (No Platform-Specific Code)
If you're only using cross-platform APIs (standard .NET, UNO controls):
- ✅ Build on Windows → Runs everywhere (Windows/Mac/Linux)
- ✅ Build on Mac → Runs everywhere
- **One binary works on all platforms**

### The Complex Case (With Platform-Specific Code)
When you use platform-specific APIs like:
- **AppKit** (macOS native APIs)
- **Windows.Win32** (Windows native APIs)
- **Platform-specific file pickers, menus, etc.**

Then:
- ⚠️ **Build on Windows** → Mac-specific features are **excluded** (won't compile AppKit)
- ⚠️ **Build on Mac** → Windows-specific features are **excluded** (won't compile Win32)
- **You need platform-specific builds**

### Example: Using AppKit for Mac
```csharp
// In Windowing.desktop.cs
#if __MACOS__
using AppKit;

public void SetMacMenuBar()
{
    // This code ONLY compiles on Mac
    var menu = new NSMenu();
    NSApplication.SharedApplication.MainMenu = menu;
}
#endif
```

**What happens:**
- **Building on Windows**: The `#if __MACOS__` section is completely skipped. Binary runs on Mac but WITHOUT the custom menu
- **Building on Mac**: The AppKit code is included. Binary has full Mac features but may lack Windows-specific optimizations

### Practical Impact

| Scenario | Build Where | Result |
|----------|------------|--------|
| Pure UNO/XAML UI fix | Either platform | Same binary runs everywhere |
| Added Mac menu with AppKit | Must build on Mac | Full Mac features |
| Added Windows registry code | Must build on Windows | Full Windows features |
| Both platform features | Need TWO builds | Platform-specific distributions |

**Key Insight:** The desktop target CAN run everywhere, but platform-specific features require platform-specific builds.

### Real Example from StoryCAD

You already have platform-specific files:
- `Windowing.desktop.cs` - Desktop-specific window management
- `Windowing.WinAppSDK.cs` - Windows-specific window management
- `PrintReportDialogVM.WinAppSDK.cs` - Windows-specific printing

This means:
- **A Windows build** includes `Windowing.WinAppSDK.cs` but NOT `Windowing.desktop.cs`
- **A Mac build** includes `Windowing.desktop.cs` but NOT `Windowing.WinAppSDK.cs`
- **Each platform gets its optimized implementation**

So when you add Mac-specific code (like AppKit for native menus), it will ONLY be included when building on Mac.

---

## Platform Categories & Development Targets

### 1. UNO Platform Cross-Platform Issues
**Label:** `UNO Platform`
**Branch:** `UNOTestBranch`
**Develop on:** Windows with VS2022
**Test on:** Both platforms
**Target Frameworks:**
- Windows: `net9.0-windows10.0.22621`
- macOS: `net9.0-desktop`

**Examples:**
- #1116 - [UNO] UI goes off Screen
- #1141 - [UNO] Keybinds unsupported in MacOS
- #1119 - [UNO-NonWinAppSDK] No Spellcheck
- #965 - [UNO] Minimum window size
- #964 - [UNO] Single Instancing
- #963 - [UNO] STBX Files can't be opened from outside

**Development Approach:**
- Write code in VS2022 on Windows
- Test on both platforms
- Use platform conditionals: `#if HAS_UNO_WINUI` (Windows) or `#if __MACOS__` (Mac)
- Use partial classes for platform-specific code

---

### 2. macOS-Specific Features
**Branch:** `UNOTestBranch`
**Develop on:** Mac Mini with Rider
**Test on:** macOS only
**Target Framework:** `net9.0-desktop`

**Examples:**
- #1135 - Collaborator plug-in for macOS
- #1130 - Evaluate macOS RTF Support Strategy
- #1129 - Implement macOS app sandboxing (App Store)
- #1128 - Set up App Store Connect
- #1126 - Package and distribute for macOS
- #1124 - Release 4.0 for UNO desktop head for macOS

**Development Approach:**
- Code in Rider on Mac Mini
- Use `Windowing.desktop.cs` for Mac-specific windowing
- Test Apple Store compliance requirements
- Debug macOS-specific APIs directly

---

### 3. Windows-Specific (WinAppSDK/Modern Windows)
**Branch:** `main` or `UNOTestBranch`
**Develop on:** Windows with VS2022
**Test on:** Windows 11
**Target Framework:** `net9.0-windows10.0.22621`

**Examples:**
- #1137 - Full manual testing for WinAppSDK compatibility
- #1123 - Release 3.4 for UNO WinAppSDK head
- Windows Store-specific features

**Development Approach:**
- Use VS2022 on Windows 11
- Use `Windowing.WinAppSDK.cs` for Windows-specific code
- Test Windows Store requirements

---

### 4. Legacy Windows Support (Windows 7-10)
**Branch:** `UNOTestBranch`
**Develop on:** Windows with VS2022
**Test on:** Windows 7/8/10 VMs or older hardware
**Target Framework:** `net9.0-desktop` (using Win32 backend)

**Purpose:** Support older Windows versions not compatible with WinAppSDK

**Key Point:** The desktop target runs on ALL Windows versions (7+), macOS, and Linux. This is how we support users who can't use the modern Windows Store version.

**Testing approach:**
- Build `net9.0-desktop` on Windows with VS2022
- Test directly on Windows 7/8/10 machines
- Same binary also runs on Mac/Linux

---

### 5. Platform-Agnostic Issues
**Branch:** Usually `UNOTestBranch` for new work
**Develop on:** Windows with VS2022 (familiar environment)
**Test on:** Both platforms before PR

**Examples:**
- #1146 - Refactor: Move CurrentViewType to AppState
- #1145 - Move template loading into ReportFormatter
- #1089 - Update input processing
- #1077 - Create Interactive API Testing Tool

---

## Development Environment Setup

### Primary Strategy: Windows-First Development

Since you're experienced with VS2022, use this workflow for most development:

1. **Code** on Windows 11 in VS2022 (your comfort zone)
2. **Build/Test** locally for Windows (WinAppSDK head)
3. **Push** to git branch
4. **Pull** on Mac Mini
5. **Build/Test** with Rider on Mac (desktop head)
6. **Debug** Mac-specific issues in Rider if needed

This works because ~90% of StoryCAD code is shared/platform-agnostic.

---

## Cross-Machine Development Workflow

### The Challenge
Developing on Windows and testing on Mac requires moving code between machines efficiently. **Manual testing** and **debugging** require different approaches.

### Option 1: Git-Based Workflow (Recommended for Most Development)

**Best for:** Regular development, manual testing, occasional debugging

**Workflow:**
```bash
# On Windows (VS2022):
git add .
git commit -m "WIP: Testing on Mac"
git push origin issue-1116-ui-overflow

# On Mac (Terminal):
cd ~/Documents/dev/src/StoryCAD
git pull origin issue-1116-ui-overflow
dotnet build -f net9.0-desktop
dotnet run -f net9.0-desktop  # For manual testing
# OR open in Rider for debugging
```

**Pros:**
- Clean version control
- Works anywhere with internet
- Natural checkpoint system

**Cons:**
- Requires commit/push/pull cycle
- Slower iteration for quick fixes

**Tip:** Use "WIP" (Work In Progress) commits for testing, then squash before final PR:
```bash
# After testing is complete, on Windows:
git rebase -i HEAD~5  # Squash last 5 WIP commits
```

---

### Option 2: Network Share Workflow

**Best for:** Rapid iteration during active debugging

**Setup:**
1. **Share StoryCAD folder from Windows:**
   - Right-click StoryCAD folder → Properties → Sharing
   - Share with your Mac user account

2. **Mount on Mac:**
   ```bash
   # In Finder: Go → Connect to Server
   smb://YOUR-WINDOWS-PC/StoryCAD

   # Create a symbolic link for easier access:
   ln -s /Volumes/StoryCAD ~/WindowsStoryCAD
   ```

3. **Build locally on Mac:**
   ```bash
   # Copy to local drive first (builds fail on network drives)
   rsync -av --exclude=bin --exclude=obj ~/WindowsStoryCAD/ ~/StoryCAD-Local/
   cd ~/StoryCAD-Local
   dotnet build -f net9.0-desktop
   ```

**Pros:**
- No git commits needed
- See changes instantly
- Good for rapid testing

**Cons:**
- Requires local network
- Must copy to local drive to build
- Can't build directly on network share

---

### Option 3: Separate Build Folders

**Best for:** When you need to debug platform-specific issues simultaneously

**Setup:**
```
/Users/terrycox/Documents/dev/src/
├── StoryCAD/              # Main development (git)
├── StoryCAD-Mac-Debug/    # Mac-specific debugging
└── StoryCAD-Win-Test/     # Windows test builds
```

**Workflow:**
1. Main development in `StoryCAD/`
2. When debugging Mac issues, work in `StoryCAD-Mac-Debug/`
3. Cherry-pick fixes back to main:
   ```bash
   # In main StoryCAD folder:
   git cherry-pick COMMIT_HASH
   ```

---

### Manual Testing vs Debugging

#### Manual Testing (Quick Verification)
**When to use:** Checking if UI looks correct, basic functionality works

**Fastest approach:**
```bash
# On Mac, after git pull:
dotnet run -f net9.0-desktop --project StoryCAD/StoryCAD.csproj
# App launches immediately for testing
```

**No IDE needed** - just Terminal

#### Debugging (Finding Root Cause)
**When to use:** Investigating crashes, stepping through code, examining variables

**Required approach:**
1. **Must build on Mac** (can't use Windows build for debugging)
2. **Open in Rider:**
   ```bash
   rider ~/Documents/dev/src/StoryCAD/StoryCAD.sln
   ```
3. Set breakpoints and debug normally

**Important:** You cannot remote debug from Windows to Mac with UNO Platform

---

### Optimizing the Workflow

#### For UI/XAML Issues (Like #1116)
1. **First pass on Windows:**
   - Make changes in VS2022
   - Test on Windows
   - Git commit/push

2. **Quick Mac verification:**
   ```bash
   git pull && dotnet run -f net9.0-desktop
   ```

3. **If Mac-specific fix needed:**
   - Open Rider on Mac
   - Add platform-specific code
   - Test locally
   - Git commit/push
   - Pull back to Windows

#### For Logic/Business Rules
- Usually develop entirely on Windows
- Mac testing is just verification
- Rarely needs Mac debugging

#### For Mac-Specific Features
- Skip Windows entirely
- Develop directly on Mac in Rider

---

### Build Configuration Tips

**VS2022 on Windows:**
Can build BOTH targets from Windows:
```xml
<!-- In .csproj -->
<TargetFrameworks>net9.0-desktop;net9.0-windows10.0.22621</TargetFrameworks>
```

**What runs where:**
- `net9.0-windows10.0.22621` (WinAppSDK) - Windows 10/11 only (modern)
- `net9.0-desktop` (Desktop head) runs on:
  - **Windows 7/8/10/11** (using Win32 backend)
  - **macOS** (native)
  - **Linux** (X11/Framebuffer)

**Rider on Mac:**
Can ONLY build desktop target:
```bash
dotnet build -f net9.0-desktop  # Works (runs on Mac/Windows/Linux)
dotnet build -f net9.0-windows10.0.22621  # Fails - Windows SDK required
```

---

### Time-Saving Scripts

**Mac: Quick Test Script** (`~/test-storycad.sh`):
```bash
#!/bin/bash
cd ~/Documents/dev/src/StoryCAD
git pull origin $(git branch --show-current)
dotnet build -f net9.0-desktop --configuration Debug
if [ $? -eq 0 ]; then
    dotnet run -f net9.0-desktop --configuration Debug
else
    echo "Build failed!"
fi
```

**Mac: Quick Sync Script** (`~/sync-storycad.sh`):
```bash
#!/bin/bash
# For network share workflow
rsync -av --exclude=bin --exclude=obj \
    ~/WindowsStoryCAD/ ~/StoryCAD-Local/
cd ~/StoryCAD-Local
dotnet build -f net9.0-desktop
```

---

### When to Build Where

| Scenario | Build On | Test On | Debug On | Why |
|----------|----------|---------|----------|-----|
| Initial development | Windows | Windows | Windows | Fastest iteration |
| Mac UI verification (no AppKit) | Windows | Mac | - | Cross-platform code only |
| Mac UI verification (with AppKit) | Mac | Mac | Mac | Platform APIs require platform build |
| Mac crash investigation | Mac | Mac | Mac | Need platform debugging |
| Platform-specific feature | Target platform | Target platform | Target platform | Platform APIs |
| Performance testing | Both | Both | - | Platform differences |

### Distribution Strategy Based on Code

**If using only cross-platform code:**
```
One build (either platform) → Universal desktop binary
```

**If using platform-specific code (likely for production):**
```
Windows build → Windows distribution (includes Win32 optimizations)
Mac build → Mac distribution (includes AppKit features)
```

**Current StoryCAD situation:**
- You're adding Mac-specific features (like proper keybindings with Cmd)
- You'll need separate builds for optimal platform experience
- Windows build for Windows 7/8/10/11 users
- Mac build for macOS users

---

### Common Issues & Solutions

**"Can't build on network share"**
- Always copy to local drive first
- Use rsync to exclude bin/obj folders

**"Changes not showing on Mac"**
- Ensure you committed AND pushed on Windows
- Check correct branch: `git branch --show-current`

**"Can't debug Mac from Windows"**
- This is not supported - must debug on Mac
- Use logging for remote troubleshooting:
  ```csharp
  #if __MACOS__
      _log.LogInfo($"Mac-specific value: {someVariable}");
  #endif
  ```

**"Build works on Windows but fails on Mac"**
- Check for Windows-specific NuGet packages
- Verify all file paths use `/` not `\`
- Look for case-sensitivity issues (Mac is case-sensitive)

---

## IDE-Specific Guidance

### Visual Studio 2022 (Windows) - Primary IDE

**Advantages:**
- Full UNO Platform extension support
- Superior IntelliSense for UNO
- Hot Reload for XAML
- Familiar debugging tools

**Setup:**
1. Install "Uno Platform" extension from VS Marketplace
2. Open `StoryCAD.sln`
3. Use Configuration Manager to switch between targets:
   - `Debug | x64` for WinAppSDK head
   - `Debug | Any CPU` for desktop head

**Key Shortcuts:**
- F5: Start debugging
- Ctrl+Shift+B: Build solution
- Ctrl+.: Quick actions/refactoring

---

### JetBrains Rider (Mac/Windows) - Secondary IDE

**Getting Started (Coming from VS2022):**

1. **Set Visual Studio Keymap:**
   - Preferences → Keymap → Choose "Visual Studio"

2. **Solution Navigation:**
   - Use "Solution" view (not "Files") - similar to VS Solution Explorer
   - Cmd+1 (Mac) / Alt+1 (Windows): Toggle Solution view

3. **Building:**
   ```bash
   # Command line on Mac:
   dotnet build -f net9.0-desktop

   # Or use Rider's UI:
   Build → Build Solution (Cmd+B on Mac)
   ```

4. **Running/Debugging:**
   - Green arrow: Run
   - Bug icon: Debug
   - Select "StoryCAD.Desktop" configuration

**Rider Tips for VS Users:**
- Cmd+Shift+F (Mac): Find in files (like Ctrl+Shift+F in VS)
- Cmd+R,R: Rename (like Ctrl+R,R in VS)
- Double-Shift: Search everywhere (like Ctrl+Q in VS)

---

## Practical Workflow Examples

### Example 1: Cross-Platform UI Bug (Issue #1116)

```bash
# 1. On Windows (VS2022):
# - Open StoryCAD.sln
# - Fix the UI overflow in XAML
# - F5 to test on Windows
# - Commit and push

git add .
git commit -m "Fix UI overflow issue for Windows"
git push origin issue-1116-ui-overflow

# 2. On Mac (Rider):
git pull origin issue-1116-ui-overflow
# Open in Rider, build, and test
# If Mac needs different fix, add platform-specific code:
```

```csharp
// In relevant .cs file:
#if __MACOS__
    // Mac-specific margin adjustments
    myControl.Margin = new Thickness(10, 5, 10, 5);
#else
    myControl.Margin = new Thickness(8, 4, 8, 4);
#endif
```

### Example 2: Mac-Specific Feature (Issue #1135)

```bash
# Develop directly on Mac with Rider
# Work with macOS-specific APIs
# No need to test on Windows
```

---

## Decision Matrix

| Issue Type | Where to Code | IDE | Test On | Why |
|------------|--------------|-----|---------|-----|
| UNO Platform bugs | Windows | VS2022 | Both | Familiar tools, cross-platform testing |
| macOS-specific | Mac | Rider | Mac only | Direct Mac API access |
| Windows Store | Windows | VS2022 | Windows | WinAppSDK specific |
| Refactoring | Windows | VS2022 | Both | Faster in familiar IDE |
| UI/XAML tweaks | Windows | VS2022 | Both | Better XAML IntelliSense |
| File associations (Mac) | Mac | Rider | Mac only | Platform-specific |

---

## Tool Migration Path

### Phase 1: Current (Recommended Starting Point)
- **Primary:** VS2022 on Windows for all coding
- **Secondary:** Rider on Mac for testing/debugging only
- **Focus:** Maintain productivity in familiar environment

### Phase 2: Intermediate (After 2-4 weeks)
- Start doing simple fixes in Rider
- Learn Rider's debugging tools
- Use Rider for Mac-specific features
- Continue using VS2022 for complex work

### Phase 3: Advanced (Optional, 2-3 months)
- Use Rider on both platforms for consistency
- Benefit from identical IDE experience
- Leverage Rider's cross-platform advantages
- Keep VS2022 as fallback for complex debugging

---

## Quick Decision Tree

```
Is the issue labeled "UNO Platform"?
  → YES: Code on Windows (VS2022), test on both
  → NO: Continue ↓

Does it mention macOS/Apple/Mac specifically?
  → YES: Code on Mac (Rider)
  → NO: Continue ↓

Does it mention Windows Store/WinAppSDK?
  → YES: Code on Windows (VS2022)
  → NO: Continue ↓

Is it a refactoring/architecture issue?
  → YES: Code on Windows (VS2022), test on both
  → NO: Ask for clarification
```

---

## Platform-Specific Code Patterns

### File Naming Conventions
- Shared code: `MyClass.cs`
- Windows-specific: `MyClass.WinAppSDK.cs`
- Mac-specific: `MyClass.desktop.cs`

### Conditional Compilation
```csharp
// Windows (WinAppSDK)
#if WINDOWS10_0_18362_0_OR_GREATER
    // Windows-specific code
#endif

// Windows (UNO on Windows)
#if HAS_UNO_WINUI
    // UNO Windows-specific code
#endif

// macOS
#if __MACOS__
    // Mac-specific code
#endif

// Any desktop platform (Windows/Mac/Linux)
#if NET9_0_OR_GREATER && !WINDOWS10_0_18362_0_OR_GREATER
    // Desktop-specific code
#endif
```

### Common Platform Differences

| Feature | Windows (WinAppSDK) | macOS (Desktop) |
|---------|-------------------|-----------------|
| File Picker | `Windows.Storage.Pickers` | Native file dialog |
| Keyboard | Ctrl+Key | Cmd+Key |
| Path Separator | `\` | `/` |
| Temp Path | `%TEMP%` | `/tmp` |
| Settings | Registry/AppData | `~/Library/Preferences` |

---

## Troubleshooting

### Common Issues

**"Can't find gh command on Mac"**
```bash
brew install gh
gh auth login --web
```

**"Build fails on Mac"**
```bash
# Ensure correct framework target
dotnet build -f net9.0-desktop
```

**"Can't debug Mac version from Windows"**
- This is expected - you must test Mac builds on Mac hardware
- Use git to sync code between platforms

---

## Resources

- [UNO Platform Documentation](https://platform.uno/docs/)
- [Platform-Specific C# in UNO](https://platform.uno/docs/articles/platform-specific-csharp.html)
- [Rider for VS Users](https://www.jetbrains.com/help/rider/Getting_Started_with_Rider_for_VS_Users.html)
- [StoryCAD UNO Migration Project](https://github.com/orgs/storybuilder-org/projects/6)

---

## Updates

This document should be updated when:
- New platform-specific patterns are discovered
- Tool versions change significantly
- New platforms are added (Linux, WebAssembly)
- Development workflow improvements are identified

*For questions or clarifications, refer to the StoryCAD CLAUDE.md file or create an issue in the repository.*