# Issue #964: UNO Platform Single Instancing

**Issue**: [UNO] - Single Instancing
**GitHub**: https://github.com/storybuilder-org/StoryCAD/issues/964
**Status**: Research Complete - Ready for Implementation
**Date**: 2025-10-15

## Problem Statement

StoryCAD's Windows-only single instancing implementation (using `Microsoft.Windows.AppLifecycle.AppInstance.FindOrRegisterForKey` and `RedirectActivationToAsync`) doesn't work with UNO Platform because:

1. **`AppInstance` is Windows-specific** - The `Microsoft.Windows.AppLifecycle` namespace only works on Windows (WinAppSDK head)
2. **Platform differences** - Single instancing may not be appropriate for WebAssembly, iOS, or Android where apps run in sandboxed environments
3. **Current code location** - Single instance check is in `App()` constructor on **main branch**, but removed from **UNOTestBranch**
4. **ActivateMainInstance exists** - `Windowing.ActivateMainInstance()` at line 250 is ready to bring the main window to front when second instance tries to launch

## Research Summary

Based on comprehensive research from three specialized agents:

### Key Findings

1. **Windows (WinAppSDK)**: Use existing `Microsoft.Windows.AppLifecycle.AppInstance` approach
2. **macOS (Desktop)**: Use `NSRunningApplication` API via Xamarin.macOS bindings in UNO Platform
3. **iOS**: **No single-instance enforcement needed** - iOS enforces single-process architecture by design
4. **WebAssembly/Android**: Not applicable - single instancing doesn't make sense

### Main Branch Implementation (Windows-only)

```csharp
// App.xaml.cs constructor (main branch)
AppInstance _MainInstance = AppInstance.FindOrRegisterForKey("main");
_MainInstance.Activated += (((sender, e) => new Windowing().ActivateMainInstance()));

AppActivationArguments activatedEventArgs = AppInstance.GetCurrent().GetActivatedEventArgs();

if (!_MainInstance.IsCurrent)
{
    // Redirect to other instance
    await _MainInstance.RedirectActivationToAsync(activatedEventArgs);
    Process.GetCurrentProcess().Kill();
}
else
{
    if (activatedEventArgs.Kind == ExtendedActivationKind.File)
    {
        if (activatedEventArgs.Data is IFileActivatedEventArgs fileArgs)
        {
            LaunchPath = fileArgs.Files.FirstOrDefault().Path;
        }
    }
}
```

### UNOTestBranch Current State

- Single instancing code completely removed from App.xaml.cs
- File activation handling exists (lines 43-56) but only for Windows
- `Windowing.ActivateMainInstance()` still present but unused

## Platform-Specific Implementation Strategy

### Windows (WinAppSDK)

**Status**: Already implemented on main branch, needs porting to UNO

**Approach**: Keep existing `AppInstance` logic

**Implementation Location**: App.xaml.cs or move to Program.cs for consistency

**Key APIs**:
- `AppInstance.FindOrRegisterForKey("main")`
- `AppInstance.IsCurrent`
- `AppInstance.RedirectActivationToAsync()`

### macOS (Desktop)

**Status**: New implementation needed

**Approach**: Use `NSRunningApplication` API to detect and activate existing instance

**Implementation Location**: `Program.cs` with runtime detection

**Key APIs**:
- `NSBundle.MainBundle.BundleIdentifier` - Get app's bundle ID
- `NSProcessInfo.ProcessInfo.ProcessIdentifier` - Get current process ID
- `NSWorkspace.SharedWorkspace.RunningApplications` - List all running apps
- `NSRunningApplication.Activate(ActivateIgnoringOtherWindows)` - Bring existing instance to front

**Code Pattern**:
```csharp
private static bool CheckMacOSSingleInstance()
{
    var bundleId = NSBundle.MainBundle.BundleIdentifier ?? "net.storybuilder.storycad";
    var currentPid = NSProcessInfo.ProcessInfo.ProcessIdentifier;
    var runningApps = NSWorkspace.SharedWorkspace.RunningApplications;

    foreach (var app in runningApps)
    {
        if (app.BundleIdentifier == bundleId &&
            app.ProcessIdentifier != currentPid)
        {
            // Activate existing instance
            app.Activate(NSApplicationActivationOptions.ActivateIgnoringOtherWindows);
            return false;
        }
    }

    return true;
}
```

**Timing**: Check in `Main()` before `NSApplication.Init()` for fastest exit

**Edge Cases**:
- Bundle identifier can be null in debug builds - use fallback
- Must exclude current process by PID comparison
- Sandboxed apps may need entitlements for NSRunningApplication access

### iOS (Future Platform)

**Status**: No implementation needed

**Rationale**: iOS enforces single-process architecture by design

**Key Facts**:
- Every iOS app runs as **one process** managed by the system
- Each app has exactly **one instance of UIApplication** (singleton)
- Multiple instances of the same app **cannot run simultaneously**
- Users cannot launch StoryCAD twice on iOS (system prevents it)

**iPhone vs iPad**:
- **iPhone**: Single window only (one UIScene)
- **iPad**: Multiple windows supported (multiple UIScenes within single process)
- iPad multi-window is different from multi-instance

**File Opening Behavior**:
- App NOT running: iOS launches app and calls `application(_:open:options:)`
- App running (backgrounded): iOS brings app to foreground and calls `application(_:open:options:)`
- App running (foregrounded): iOS calls `scene(_:openURLContexts:)` on iOS 13+

**Implementation Focus**: File activation patterns, not single-instance enforcement

### WebAssembly / Android

**Status**: No implementation

**Rationale**: Single instancing doesn't make sense in browser or mobile sandboxed environments

## Proposed Solution

### Architecture Approach

**Use runtime platform detection in shared `Program.cs`** rather than separate platform-specific Program files

**Rationale**:
- UNO Platform `net9.0-desktop` produces single binary for Windows/macOS/Linux
- Conditional compilation symbols don't work with `net9.0-desktop` target (confirmed by UNO Platform team)
- Runtime detection (`OperatingSystem.IsMacOS()`) is recommended UNO Platform pattern
- Simpler to maintain single entry point with platform switches

### Implementation Steps

#### Step 1: Update Desktop Program.cs

**File**: `StoryCAD/Platforms/Desktop/Program.cs`

Add macOS single instance check:

```csharp
using Uno.UI.Hosting;
#if __MACOS__
using AppKit;
using Foundation;
#endif

namespace StoryCAD;

public class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Platform-specific single instance check
        if (OperatingSystem.IsMacOS())
        {
            if (!CheckMacOSSingleInstance())
                return; // Exit - existing instance activated
        }

        var host = UnoPlatformHostBuilder.Create()
            .App(() => new App())
            .UseX11()
            .UseLinuxFrameBuffer()
            .UseMacOS()
            .UseWin32()
            .Build();

        host.Run();
    }

#if __MACOS__
    private static bool CheckMacOSSingleInstance()
    {
        // Get bundle identifier with fallback for debug builds
        var bundleId = NSBundle.MainBundle.BundleIdentifier
            ?? "net.storybuilder.storycad";
        var currentPid = NSProcessInfo.ProcessInfo.ProcessIdentifier;
        var runningApps = NSWorkspace.SharedWorkspace.RunningApplications;

        foreach (var app in runningApps)
        {
            if (app.BundleIdentifier == bundleId &&
                app.ProcessIdentifier != currentPid)
            {
                // Activate existing instance
                app.Activate(NSApplicationActivationOptions.ActivateIgnoringOtherWindows);
                return false;
            }
        }

        return true;
    }
#endif
}
```

#### Step 2: Refactor Windows Single Instance Code

**Current Location**: `App()` constructor (main branch)

**Options**:
1. Keep in App.xaml.cs but refactor into separate method
2. Move to Program.cs for Windows (consistency with macOS)
3. Create platform-specific App initialization

**Recommendation**: Option 1 - Keep in App.xaml.cs for Windows but clean up

**Reasoning**:
- Windows AppInstance API works best in App lifecycle
- File activation args available in App constructor
- Consistency with Microsoft's documented pattern

#### Step 3: Update Windowing.ActivateMainInstance()

**File**: `StoryCADLib/Models/Windowing.cs` (line 250)

Update documentation and implementation:

```csharp
/// <summary>
/// When a second instance is opened, this code will be ran on the main (first) instance
/// It will bring up the main window.
///
/// Note: On macOS, activation happens in Program.cs before app starts,
/// so this method is only called on Windows.
/// </summary>
public void ActivateMainInstance()
{
    if (OperatingSystem.IsMacOS())
    {
        // Already handled in Program.cs before app starts
        return;
    }

    // Windows-specific activation
    var wnd = Ioc.Default.GetRequiredService<Windowing>();
    wnd.MainWindow.Activate();

    wnd.GlobalDispatcher.TryEnqueue(() =>
    {
        Ioc.Default.GetRequiredService<ShellViewModel>()
            .ShowMessage(LogLevel.Warn, "You can only have one file open at once", false);
    });
}
```

#### Step 4: iOS Preparation (Future)

**File**: `StoryCAD/App.iOS.cs` (create when iOS support added)

```csharp
#if __IOS__
using UIKit;
using Foundation;

namespace StoryCAD;

public partial class App
{
    // No single-instance logic needed - iOS enforces single-process automatically

    /// <summary>
    /// Handle file opening on iOS
    /// Called when user taps .stbx file in Files app
    /// </summary>
    public override bool OpenUrl(UIApplication app, NSUrl url, NSDictionary options)
    {
        if (url.Path.EndsWith(".stbx", StringComparison.OrdinalIgnoreCase))
        {
            var shellVM = Ioc.Default.GetRequiredService<ShellViewModel>();
            shellVM.FilePathToLaunch = url.Path;
            return true;
        }
        return false;
    }
}
#endif
```

**Note**: Modern iOS 13+ apps should use SceneDelegate for file opening, but basic UIApplicationDelegate pattern shown above for initial implementation.

#### Step 5: Documentation Updates

**Files to Update**:

1. **`/home/tcox/.claude/memory/architecture.md`**
   - Add ADR (Architecture Decision Record) for platform-specific single instance approach
   - Document why iOS doesn't need enforcement

2. **`/home/tcox/.claude/memory/gotchas.md`**
   - macOS bundle identifier can be null in debug builds
   - macOS sandboxing considerations
   - iOS single-process architecture
   - iPad multi-window vs multi-instance distinction

3. **`.claude/docs/architecture/debugging-guide.md`**
   - Update single instance testing section (line 87)
   - Add macOS testing instructions
   - Add iOS platform notes

4. **Create**: `StoryCADTests/ManualTests/MacOS_SingleInstance_Testing.md`
   - Manual test procedures for macOS
   - Expected behaviors
   - Edge cases to verify

5. **Create**: `StoryCADTests/ManualTests/iOS_Platform_Notes.md`
   - Document iOS single-process behavior
   - iPad multi-window considerations
   - File opening patterns

## Testing Strategy

### Manual Testing - macOS

**Test Case 1: Normal Launch**
1. Launch StoryCAD on macOS from Finder
2. Verify: App starts normally
3. Close app

**Test Case 2: Duplicate Launch (App Running)**
1. Launch StoryCAD on macOS
2. While running, launch second instance from Finder
3. **Expected**: First instance activates and comes to front
4. **Expected**: Second instance exits immediately (no window flash)

**Test Case 3: Duplicate Launch (App Minimized)**
1. Launch StoryCAD and minimize to Dock
2. Launch second instance
3. **Expected**: First instance unminimizes and comes to front
4. **Expected**: Second instance exits

**Test Case 4: Rapid Multiple Launches**
1. Quickly double-click app icon multiple times
2. **Expected**: Only one instance persists
3. **Expected**: All duplicate instances exit

**Test Case 5: File Association (macOS)**
1. Launch StoryCAD
2. Double-click .stbx file in Finder
3. **Expected**: Existing instance opens file
4. **Expected**: No second instance launches

### Manual Testing - Windows

**Test Case 1: Existing Behavior**
1. Verify single instance enforcement still works on Windows
2. Test file association launching
3. Confirm no regression from main branch behavior

### Manual Testing - iOS (Future)

**Test Case 1: System-Level Single Instance**
1. Launch StoryCAD on iOS
2. Attempt to launch again from Home screen
3. **Expected**: iOS brings existing instance to foreground (system behavior)
4. **Document**: No custom code involved

**Test Case 2: File Opening**
1. Launch StoryCAD on iOS
2. Open Files app and tap .stbx file
3. **Expected**: StoryCAD comes to foreground and opens file
4. Verify `OpenUrl()` called with correct path

**Test Case 3: iPad Multi-Window (Future Enhancement)**
1. On iPad, open StoryCAD
2. Open Files app in Split View
3. Drag .stbx file to right side of screen
4. **Expected** (if implemented): New window opens with that document
5. **Note**: This is multi-window, not multi-instance

## Platform Behavior Matrix

| Platform | Multiple Instances | Implementation | Entry Point | API Used |
|----------|-------------------|----------------|-------------|----------|
| **Windows** | Possible (need enforcement) | Main branch code | App() constructor | AppInstance.FindOrRegisterForKey |
| **macOS** | Possible (need enforcement) | New implementation | Program.Main() | NSRunningApplication |
| **iOS** | System-prevented | Not needed | App.OpenUrl() | N/A (iOS handles) |
| **iPad** | Single process, multi-window | Future enhancement | UISceneDelegate | UIWindowScene |
| **WebAssembly** | N/A | Skip | N/A | N/A |
| **Android** | N/A | Skip | N/A | N/A |

## Files to Create/Modify

### Create
- [ ] `StoryCADTests/ManualTests/MacOS_SingleInstance_Testing.md`
- [ ] `StoryCADTests/ManualTests/iOS_Platform_Notes.md`

### Modify
- [ ] `StoryCAD/Platforms/Desktop/Program.cs` - Add macOS single instance check
- [ ] `StoryCAD/App.xaml.cs` - Refactor Windows single instance code (port from main branch)
- [ ] `StoryCADLib/Models/Windowing.cs` - Update ActivateMainInstance() documentation
- [ ] `/home/tcox/.claude/memory/architecture.md` - Add ADR for platform-specific approach
- [ ] `/home/tcox/.claude/memory/gotchas.md` - Add platform-specific gotchas
- [ ] `.claude/docs/architecture/debugging-guide.md` - Update single instance testing notes

## Key Architectural Decisions

### ADR 1: Runtime Detection Over Conditional Compilation

**Decision**: Use `OperatingSystem.IsMacOS()` runtime detection rather than `#if __MACOS__` for platform branching in `net9.0-desktop` code.

**Rationale**:
- UNO Platform `net9.0-desktop` produces single binary for all desktop platforms
- Conditional compilation symbols like `__MACOS__` don't work with this target
- Microsoft and UNO Platform recommend runtime detection for `net9.0-desktop`
- Allows single Program.cs to handle Windows/macOS/Linux

**Exception**: Use `#if __MACOS__` to guard macOS-specific using statements and helper methods

### ADR 2: Single Program.cs with Platform Switches

**Decision**: Keep single `Program.cs` with runtime detection rather than creating separate `Program.WinAppSDK.cs` and `Program.macOS.cs`.

**Rationale**:
- Simpler maintenance - one entry point
- UNO Platform desktop head uses single binary anyway
- Platform differences are minimal (just single instance check)
- Follows UNO Platform documentation patterns

### ADR 3: macOS Check in Main() Before App Initialization

**Decision**: Perform single instance check in `Main()` before `NSApplication.Init()`.

**Rationale**:
- Fastest exit - minimal resource usage
- Consistent with Windows approach (early check)
- No dock icon flash for duplicate instance
- Recommended pattern in macOS single instance examples

**Trade-off**: Cannot easily show alert dialog (no window yet), but silent exit is acceptable

### ADR 4: No iOS Single Instance Enforcement

**Decision**: Do not implement custom single instance logic for iOS.

**Rationale**:
- iOS enforces single-process architecture at OS level
- Custom enforcement is unnecessary and impossible
- Focus development effort on iOS-specific features (file opening, multi-window)
- Document platform behavior for clarity

### ADR 5: Preserve Windows Behavior

**Decision**: Keep existing Windows single instance implementation from main branch with minimal changes.

**Rationale**:
- Already tested and working on Windows
- Microsoft's documented pattern for WinUI apps
- File activation integration works correctly
- No need to redesign working solution

## Known Limitations

### macOS

**Bundle Identifier Null in Debug**
- `NSBundle.MainBundle.BundleIdentifier` can return null in debug builds
- **Solution**: Fallback to hardcoded "net.storybuilder.storycad"

**Sandboxed App Restrictions**
- App Store sandboxing may restrict `NSRunningApplication` access
- **Solution**: Add entitlement if needed (not required for macOS direct distribution)

**Argument Passing**
- Windows can pass arguments to existing instance via `RedirectActivationToAsync()`
- macOS activation doesn't pass arguments
- **Limitation**: File path not communicated to existing instance on macOS
- **Future**: Could implement via NSDistributedNotificationCenter or custom IPC

### iOS

**iPad Multi-Window**
- UNO Platform doesn't fully support UISceneDelegate yet (tracked in issue #8341)
- **Limitation**: Full iPad multi-window requires UNO Platform support or custom implementation
- **Timeline**: Future enhancement after basic iOS support

**File Opening on iOS 13+**
- Modern iOS uses SceneDelegate for file opening
- **Current Plan**: Use legacy UIApplicationDelegate pattern for initial implementation
- **Future**: Migrate to SceneDelegate when UNO Platform supports it

### Cross-Platform

**No Shared Abstraction**
- Windows and macOS single instance APIs are fundamentally different
- **Decision**: Keep platform-specific implementations separate
- **Rationale**: No benefit to creating shared abstraction layer

## Specialized Agents Used

### Research Phase
- ✅ **research-analyst** (macOS patterns) - Comprehensive NSRunningApplication guidance
- ✅ **research-analyst** (iOS behavior) - Confirmed iOS single-process architecture
- ✅ **search-specialist** (UNO examples) - Found runtime detection patterns and documentation

### Implementation Phase (Recommended)
- **architect-reviewer** - Validate platform-specific approach aligns with UNO Platform patterns
- **code-reviewer** - Review implementation for security and edge cases
- **test-automator** - Create manual test documentation and procedures

## Acceptance Criteria

- [ ] Windows single instance enforcement works (port main branch behavior)
- [ ] macOS single instance enforcement works (window activates, duplicate exits silently)
- [ ] iOS documented as not needing enforcement (system-level)
- [ ] No single instance code on WebAssembly/Android platforms
- [ ] File association still works on Windows and macOS
- [ ] Documentation updated with platform-specific behavior notes
- [ ] Manual test procedures created for macOS and iOS
- [ ] `Windowing.ActivateMainInstance()` updated with platform-aware implementation
- [ ] Issue #964 closed with explanation of platform-specific approach
- [ ] All platform-specific gotchas documented

## References

### Apple Documentation
- [NSRunningApplication](https://developer.apple.com/documentation/appkit/nsrunningapplication)
- [NSWorkspace](https://developer.apple.com/documentation/appkit/nsworkspace)
- [NSApplication Lifecycle](https://developer.apple.com/documentation/appkit/nsapplication)
- [UIApplication](https://developer.apple.com/documentation/uikit/uiapplication)
- [UIScene](https://developer.apple.com/documentation/uikit/uiscene)

### Microsoft Documentation
- [App instancing with the app lifecycle API](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/applifecycle/applifecycle-instancing)
- [How to create a single-instanced WinUI app](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/applifecycle/applifecycle-single-instance)

### UNO Platform Documentation
- [Platform-specific C# code](https://platform.uno/docs/articles/platform-specific-csharp.html)
- [How Uno works on macOS](https://platform.uno/docs/articles/uno-development/uno-internals-macos.html)
- [Solution Structure](https://platform.uno/docs/articles/uno-app-solution-structure.html)
- [Using the Skia Desktop](https://platform.uno/docs/articles/features/using-skia-desktop.html)

### Community Resources
- [Using Native Controls in Uno Platform](https://platform.uno/blog/using-native-controls-in-uno-platform-applications-demo-code-included/)
- [AppKit C# Wrapper for macOS](https://tagenigma.com/blog/2024/01/22/appkit-c-wrapper-for-macos/)

## Next Steps

1. Review this plan with development team
2. Get approval for platform-specific approach
3. Implement Step 1 (macOS Program.cs changes)
4. Port Windows single instance code from main branch (Step 2)
5. Update Windowing.ActivateMainInstance() (Step 3)
6. Create manual test documentation
7. Manual testing on macOS hardware
8. Update architecture documentation
9. Close issue #964

---

**Document Created**: 2025-10-15
**Last Updated**: 2025-10-15
**Status**: Research Complete - Ready for Implementation
