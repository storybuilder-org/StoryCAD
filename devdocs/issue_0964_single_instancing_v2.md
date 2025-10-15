# Issue #964: UNO Platform Single Instancing (Revised)

**Issue**: [UNO] - Single Instancing
**GitHub**: https://github.com/storybuilder-org/StoryCAD/issues/964
**Status**: Research Complete - Ready for Implementation
**Date**: 2025-10-15
**Revision**: 2 (Incorporates GPT-5 feedback)

## Problem Statement

StoryCAD's Windows-only single instancing implementation (using `Microsoft.Windows.AppLifecycle.AppInstance.FindOrRegisterForKey` and `RedirectActivationToAsync`) doesn't work with UNO Platform because:

1. **`AppInstance` is Windows-specific** - The `Microsoft.Windows.AppLifecycle` namespace only works on Windows (WinAppSDK head)
2. **Platform differences** - Single instancing requires platform-specific approaches
3. **Current code location** - Single instance check is in `App()` constructor on **main branch**, but removed from **UNOTestBranch**
4. **ActivateMainInstance exists** - `Windowing.ActivateMainInstance()` at line 250 is ready to bring the main window to front

## GPT-5 Evaluation Summary

GPT-5 provided critical feedback that significantly improves the original plan:

### Key Improvements from GPT-5
1. ✅ **Use `LSMultipleInstancesProhibited` in Info.plist** - Native macOS solution, no code needed
2. ✅ **Implement AppKit delegate for file opening** - Proper Apple event handling
3. ✅ **Remove NSWorkspace/NSRunningApplication scanning** - Fragile and unnecessary
4. ✅ **Fix Windows exit** - Use `Environment.Exit(0)` instead of `Process.Kill()`
5. ✅ **Simplify architecture** - Avoid unnecessary abstractions

## Platform-Specific Implementation Strategy (Revised)

### Windows (WinAppSDK)

**Status**: Already implemented on main branch, needs minor cleanup

**Approach**: Keep existing `AppInstance` logic with safer exit

**Key Change**:
```csharp
// OLD (main branch)
await mainInstance.RedirectActivationToAsync(args);
Process.GetCurrentProcess().Kill();  // ❌ Can cause COM/STA shutdown errors

// NEW (revised)
await mainInstance.RedirectActivationToAsync(args);
Environment.Exit(0);  // ✅ Clean exit
```

**Implementation Location**: App.xaml.cs

**Rationale**:
- Windows AppInstance API works best in App lifecycle
- File activation args available in App constructor
- Consistency with Microsoft's documented pattern
- Minor cleanup prevents shutdown errors

### macOS (Desktop) - **SIGNIFICANTLY REVISED**

**Status**: New implementation needed

**Approach**: Use macOS native `LSMultipleInstancesProhibited` + AppKit delegate

**Implementation**: Two-part solution

#### Part 1: Info.plist Configuration (Primary)

Add to Info.plist:
```xml
<key>LSMultipleInstancesProhibited</key>
<true/>
```

**Benefits**:
- ✅ Zero code required for single instance enforcement
- ✅ Launch Services automatically activates existing instance
- ✅ Sandbox-safe (no process scanning)
- ✅ Native macOS behavior
- ✅ Works across all launchers (Finder, command line, Spotlight, etc.)

**Trade-off**: Also prevents concurrent launches under multiple macOS user sessions (acceptable for desktop apps)

#### Part 2: AppKit Delegate for File Opening

Implement standard AppKit delegate methods for file association:

```csharp
#if __MACOS__
using AppKit;
using Foundation;

[Register("AppDelegate")]
internal sealed class AppDelegate : NSApplicationDelegate
{
    public override void OpenFiles(NSApplication sender, string[] filenames)
    {
        var shell = Ioc.Default.GetRequiredService<ShellViewModel>();
        foreach (var filename in filenames)
        {
            shell.OpenPathFromOS(filename);
        }
        sender.ReplyToOpenOrPrint(NSApplicationSavePanel.Ok);
    }
}
#endif
```

**Wire in Program.cs** before `host.Run()`.

**Declare `.stbx` in Info.plist**:
```xml
<key>CFBundleDocumentTypes</key>
<array>
    <dict>
        <key>CFBundleTypeName</key>
        <string>StoryCAD Document</string>
        <key>CFBundleTypeRole</key>
        <string>Editor</string>
        <key>LSItemContentTypes</key>
        <array>
            <string>net.storybuilder.storycad.stbx</string>
        </array>
        <key>LSHandlerRank</key>
        <string>Owner</string>
    </dict>
</array>
```

#### Part 3: Minimal Fallback (Optional)

Keep minimal runtime check only for edge case launchers that bypass Launch Services:

```csharp
#if __MACOS__
private static bool CheckMacOSSingleInstance()
{
    // Minimal fallback - Launch Services should handle this
    // Only needed for rare launchers bypassing Launch Services

    var bundleId = NSBundle.MainBundle.BundleIdentifier
        ?? "net.storybuilder.storycad";
    var runningApps = NSWorkspace.SharedWorkspace.RunningApplications;

    foreach (var app in runningApps)
    {
        if (app.BundleIdentifier == bundleId &&
            !app.IsActive)  // Found inactive instance
        {
            // Activate it
            app.Activate(NSApplicationActivationOptions.ActivateIgnoringOtherApps);
            return false;
        }
    }

    return true;
}
#endif
```

**Note**: This fallback is likely unnecessary with `LSMultipleInstancesProhibited`, but can be kept for robustness.

### iOS (Future Platform)

**Status**: No implementation needed

**Approach**: Use modern scene-based URL handling

**File Opening Pattern**:
```csharp
#if __IOS__
using UIKit;
using Foundation;

namespace StoryCAD;

public partial class App
{
    // No single-instance logic needed - iOS enforces single-process automatically

    /// <summary>
    /// Modern iOS 13+ file opening
    /// Prefer scene-based URL handling over legacy OpenUrl
    /// </summary>
    public override void OpenUrl(UIApplication application, NSUrl url,
        NSDictionary options, Action<bool> completion)
    {
        if (url.Path.EndsWith(".stbx", StringComparison.OrdinalIgnoreCase))
        {
            var shell = Ioc.Default.GetRequiredService<ShellViewModel>();
            shell.OpenPathFromOS(url.Path);
            completion(true);
            return;
        }
        completion(false);
    }

    // TODO: When UNO Platform supports UISceneDelegate, migrate to:
    // public override void OpenUrlContexts(UIScene scene,
    //     NSSet<UIOpenURLContext> urlContexts)
}
#endif
```

**Rationale**:
- iOS enforces single-process architecture at OS level
- Focus on modern scene-based APIs when available
- GPT-5 recommendation: Prefer `scene(_:openURLContexts:)` over legacy methods

### WebAssembly / Android

**Status**: No implementation

**Rationale**: Single instancing doesn't apply to browser or mobile sandboxed environments

## Revised Implementation Steps

### Step 1: Update macOS Info.plist

**File**: `StoryCAD/Platforms/macOS/Info.plist` (create if doesn't exist)

Add:
```xml
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <!-- Single instance enforcement -->
    <key>LSMultipleInstancesProhibited</key>
    <true/>

    <!-- Document type registration -->
    <key>CFBundleDocumentTypes</key>
    <array>
        <dict>
            <key>CFBundleTypeName</key>
            <string>StoryCAD Document</string>
            <key>CFBundleTypeRole</key>
            <string>Editor</string>
            <key>LSItemContentTypes</key>
            <array>
                <string>net.storybuilder.storycad.stbx</string>
            </array>
            <key>LSHandlerRank</key>
            <string>Owner</string>
        </dict>
    </array>

    <!-- UTI (Uniform Type Identifier) declaration -->
    <key>UTExportedTypeDeclarations</key>
    <array>
        <dict>
            <key>UTTypeIdentifier</key>
            <string>net.storybuilder.storycad.stbx</string>
            <key>UTTypeConformsTo</key>
            <array>
                <string>public.data</string>
                <string>public.content</string>
            </array>
            <key>UTTypeDescription</key>
            <string>StoryCAD Document</string>
            <key>UTTypeTagSpecification</key>
            <dict>
                <key>public.filename-extension</key>
                <array>
                    <string>stbx</string>
                </array>
            </dict>
        </dict>
    </array>
</dict>
</plist>
```

### Step 2: Implement macOS AppDelegate

**File**: `StoryCAD/Platforms/Desktop/AppDelegate.macOS.cs`

```csharp
#if __MACOS__
using AppKit;
using Foundation;
using StoryCADLib.Services.IoC;
using StoryCADLib.ViewModels;

namespace StoryCAD;

[Register("AppDelegate")]
internal sealed class AppDelegate : NSApplicationDelegate
{
    /// <summary>
    /// Called when user opens .stbx file(s) from Finder
    /// </summary>
    public override void OpenFiles(NSApplication sender, string[] filenames)
    {
        try
        {
            var shell = Ioc.Default.GetRequiredService<ShellViewModel>();

            foreach (var filename in filenames)
            {
                if (filename.EndsWith(".stbx", StringComparison.OrdinalIgnoreCase))
                {
                    shell.OpenPathFromOS(filename);
                }
            }

            sender.ReplyToOpenOrPrint(NSApplicationSavePanel.Ok);
        }
        catch (Exception ex)
        {
            var log = Ioc.Default.GetRequiredService<ILogService>();
            log.LogException(LogLevel.Error, ex, "Failed to open file from Finder");
            sender.ReplyToOpenOrPrint(NSApplicationSavePanel.Cancel);
        }
    }
}
#endif
```

### Step 3: Wire AppDelegate in Program.cs

**File**: `StoryCAD/Platforms/Desktop/Program.cs`

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
        // macOS-specific initialization
        if (OperatingSystem.IsMacOS())
        {
#if __MACOS__
            NSApplication.Init();
            NSApplication.SharedApplication.Delegate = new AppDelegate();
#endif
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
}
```

**Note**: No runtime single-instance check needed - `LSMultipleInstancesProhibited` handles it.

### Step 4: Fix Windows Exit

**File**: `StoryCAD/App.xaml.cs`

Port from main branch with safer exit:

```csharp
#if HAS_UNO_WINUI
using Microsoft.Windows.AppLifecycle;

namespace StoryCAD;

public partial class App : Application
{
    private string launchPath = string.Empty;

    public App()
    {
        // Single instance check
        var mainInstance = AppInstance.FindOrRegisterForKey("main");
        mainInstance.Activated += OnInstanceActivated;

        var activatedEventArgs = AppInstance.GetCurrent().GetActivatedEventArgs();

        if (!mainInstance.IsCurrent)
        {
            // Redirect to existing instance
            var redirectTask = mainInstance.RedirectActivationToAsync(activatedEventArgs);
            redirectTask.Wait();  // Block until redirect completes

            // Clean exit (not Process.Kill())
            Environment.Exit(0);
            return;
        }

        // This is the main instance - check for file activation
        if (activatedEventArgs.Kind == ExtendedActivationKind.File)
        {
            if (activatedEventArgs.Data is IFileActivatedEventArgs fileArgs)
            {
                var file = fileArgs.Files.OfType<StorageFile>().FirstOrDefault();
                if (file != null && file.Path.EndsWith(".stbx", StringComparison.OrdinalIgnoreCase))
                {
                    launchPath = file.Path;
                }
            }
        }

        // Continue normal initialization
        BootStrapper.Initialise(false);
        // ... rest of App constructor
    }

    private void OnInstanceActivated(object sender, AppActivationArguments e)
    {
        // Bring main window to front when second instance tries to launch
        var windowing = Ioc.Default.GetRequiredService<Windowing>();
        windowing.ActivateMainInstance();
    }
}
#endif
```

### Step 5: Update Windowing.ActivateMainInstance()

**File**: `StoryCADLib/Models/Windowing.cs` (line 250)

Simplify implementation:

```csharp
/// <summary>
/// When a second instance is opened on Windows, this brings the main window to front.
///
/// Note: On macOS, LSMultipleInstancesProhibited + Launch Services handle this automatically.
/// Note: On iOS, system enforces single-process - this is not called.
/// </summary>
public void ActivateMainInstance()
{
    // Only needed on Windows
    // macOS: Launch Services handles activation via LSMultipleInstancesProhibited
    // iOS: N/A (single-process by design)

    if (!OperatingSystem.IsWindows())
    {
        return;
    }

    var wnd = Ioc.Default.GetRequiredService<Windowing>();
    wnd.MainWindow.Activate();

    wnd.GlobalDispatcher.TryEnqueue(() =>
    {
        Ioc.Default.GetRequiredService<ShellViewModel>()
            .ShowMessage(LogLevel.Warn, "You can only have one file open at once", false);
    });
}
```

### Step 6: Add ShellViewModel.OpenPathFromOS()

**File**: `StoryCADLib/ViewModels/ShellViewModel.cs`

Add helper method for file opening from OS:

```csharp
/// <summary>
/// Called when OS opens a file (Finder on macOS, Explorer on Windows)
/// </summary>
public void OpenPathFromOS(string filePath)
{
    if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
    {
        _logger.Log(LogLevel.Warn, $"OS requested to open invalid path: {filePath}");
        return;
    }

    _logger.Log(LogLevel.Info, $"OS requested to open: {filePath}");

    // Use existing file opening logic
    // TODO: Check if file is already open, handle unsaved changes, etc.
    FilePathToLaunch = filePath;
}
```

### Step 7: Documentation Updates

**Files to Update**:

1. **`/home/tcox/.claude/memory/architecture.md`**
   - Add ADR for `LSMultipleInstancesProhibited` approach on macOS
   - Document GPT-5 feedback integration

2. **`/home/tcox/.claude/memory/gotchas.md`**
   - macOS: `LSMultipleInstancesProhibited` prevents multi-user session launches
   - macOS: Requires `.stbx` UTI declaration for Finder association
   - Windows: Use `Environment.Exit(0)` not `Process.Kill()`

3. **`.claude/docs/architecture/debugging-guide.md`**
   - Update single instance testing notes
   - Document Info.plist requirement for macOS

4. **Create**: `StoryCADTests/ManualTests/MacOS_SingleInstance_Testing.md`

5. **Create**: `StoryCADTests/ManualTests/iOS_Platform_Notes.md`

## Testing Strategy (Revised)

### Manual Testing - macOS

**Test Case 1: Single Instance Enforcement**
1. Launch StoryCAD on macOS from Finder
2. While running, launch second instance
3. **Expected**: Launch Services brings first instance to front
4. **Expected**: Second instance never starts (handled by OS)

**Test Case 2: File Association**
1. Launch StoryCAD
2. Double-click .stbx file in Finder
3. **Expected**: Existing instance comes to front
4. **Expected**: `AppDelegate.OpenFiles()` called
5. **Expected**: File opens in existing instance
6. **Verify**: No second instance launches

**Test Case 3: Multiple Files**
1. Launch StoryCAD
2. Select multiple .stbx files in Finder
3. Right-click → Open With → StoryCAD
4. **Expected**: All files passed to `OpenFiles()` array
5. **Expected**: Files open sequentially in existing instance

**Test Case 4: Rapid Launches**
1. Quickly double-click app icon multiple times
2. **Expected**: Launch Services prevents duplicate launches
3. **Expected**: Only one instance ever exists

### Manual Testing - Windows

**Test Case 1: Clean Exit on Redirect**
1. Launch StoryCAD on Windows
2. Launch second instance
3. **Expected**: Second instance redirects and exits cleanly
4. **Expected**: No error dialogs or COM exceptions
5. **Verify**: First instance activates

**Test Case 2: File Association**
1. Launch StoryCAD
2. Double-click .stbx file in Explorer
3. **Expected**: File activation args passed to main instance
4. **Expected**: File opens in existing instance

### Manual Testing - iOS (Future)

**Test Case 1: File Opening**
1. Launch StoryCAD on iOS
2. Open Files app and tap .stbx file
3. **Expected**: `OpenUrl()` or `OpenUrlContexts()` called
4. **Expected**: File opens correctly

## Platform Behavior Matrix (Revised)

| Platform | Single Instance | Implementation | API Used | File Opening |
|----------|----------------|----------------|----------|--------------|
| **Windows** | Enforced in code | App() constructor | AppInstance.FindOrRegisterForKey | AppActivationArguments |
| **macOS** | Enforced by OS | Info.plist | LSMultipleInstancesProhibited | AppDelegate.OpenFiles |
| **iOS** | Enforced by OS | N/A | N/A (system-level) | App.OpenUrl / scene delegate |
| **WebAssembly** | N/A | N/A | N/A | N/A |
| **Android** | N/A | N/A | N/A | N/A |

## Files to Create/Modify (Revised)

### Create
- [ ] `StoryCAD/Platforms/macOS/Info.plist` - Single instance + file association
- [ ] `StoryCAD/Platforms/Desktop/AppDelegate.macOS.cs` - File opening handler
- [ ] `StoryCADTests/ManualTests/MacOS_SingleInstance_Testing.md`
- [ ] `StoryCADTests/ManualTests/iOS_Platform_Notes.md`

### Modify
- [ ] `StoryCAD/Platforms/Desktop/Program.cs` - Wire AppDelegate for macOS
- [ ] `StoryCAD/App.xaml.cs` - Port Windows code with `Environment.Exit(0)`
- [ ] `StoryCADLib/Models/Windowing.cs` - Simplify ActivateMainInstance()
- [ ] `StoryCADLib/ViewModels/ShellViewModel.cs` - Add OpenPathFromOS()
- [ ] `/home/tcox/.claude/memory/architecture.md` - Document revised approach
- [ ] `/home/tcox/.claude/memory/gotchas.md` - Add platform-specific gotchas
- [ ] `.claude/docs/architecture/debugging-guide.md` - Update testing notes

## Key Architectural Decisions (Revised)

### ADR 1: Use LSMultipleInstancesProhibited on macOS

**Decision**: Use macOS native `LSMultipleInstancesProhibited` in Info.plist instead of runtime process scanning.

**Rationale**:
- ✅ Native macOS solution (zero code)
- ✅ Launch Services automatically handles activation
- ✅ Sandbox-safe (no process enumeration)
- ✅ Works with all launchers (Finder, Spotlight, command line)
- ✅ Simpler and more robust than scanning

**Source**: GPT-5 feedback - "eliminates redundant code for detecting other running instances"

### ADR 2: Use AppKit Delegate for File Opening

**Decision**: Implement `NSApplicationDelegate.OpenFiles()` for macOS file association.

**Rationale**:
- ✅ Standard AppKit pattern
- ✅ Launch Services delivers file-open Apple Events
- ✅ No custom IPC required
- ✅ Proper macOS integration

**Source**: GPT-5 feedback - "Implement standard AppKit delegate methods"

### ADR 3: Remove NSWorkspace Process Scanning

**Decision**: Remove all `NSWorkspace.SharedWorkspace.RunningApplications` scanning code.

**Rationale**:
- ✅ Fragile approach (PID comparisons, timing issues)
- ✅ Unnecessary with `LSMultipleInstancesProhibited`
- ✅ Sandbox compatibility issues
- ✅ Simpler code

**Source**: GPT-5 feedback - "Process scanning is fragile and unnecessary"

### ADR 4: Use Environment.Exit(0) on Windows

**Decision**: Replace `Process.GetCurrentProcess().Kill()` with `Environment.Exit(0)`.

**Rationale**:
- ✅ Clean shutdown (no COM/STA errors)
- ✅ Allows finalizers to run
- ✅ Proper resource cleanup
- ✅ Recommended .NET practice

**Source**: GPT-5 feedback - "Prevents process instability"

### ADR 5: Avoid Cross-Platform Abstraction

**Decision**: Keep platform-specific implementations separate, no shared interface.

**Rationale**:
- ✅ Windows and macOS approaches are fundamentally different
- ✅ iOS requires no custom code
- ✅ Abstraction adds complexity without benefit
- ✅ Platform-specific code is clearer and more maintainable

**Source**: GPT-5 feedback - "Avoid abstracting single-instancing into a shared cross-platform service"

### ADR 6: Prefer Scene-Based APIs on iOS

**Decision**: Use modern `scene(_:openURLContexts:)` when UNO Platform supports it.

**Rationale**:
- ✅ iOS 13+ best practice
- ✅ Aligns with multi-window support
- ✅ Future-proof implementation

**Source**: GPT-5 feedback - "Prefer scene-based URL handling"

## Known Limitations (Revised)

### macOS

**Multi-User Sessions**
- `LSMultipleInstancesProhibited` prevents concurrent launches across macOS user sessions
- **Impact**: Two macOS users on same machine can't run StoryCAD simultaneously
- **Assessment**: Acceptable for desktop app use case

**UTI Declaration Required**
- Finder file association requires proper UTI (Uniform Type Identifier) declaration
- **Solution**: Included in Info.plist (see Step 1)

**Potential Window Over-Activation**
- UNO Skia macOS may call `Activate()` redundantly on launch
- **Mitigation**: Test for duplicate activation calls
- **Source**: GPT-5 edge case warning

### Windows

**No Argument Passing Limitation**
- Still requires `RedirectActivationToAsync()` for file passing
- macOS handles this natively via Launch Services

### iOS

**Scene Delegate Support**
- Full scene-based file opening requires UNO Platform support
- **Workaround**: Use legacy `OpenUrl()` initially
- **Future**: Migrate when UNO Platform supports `UISceneDelegate`

## Simplifications from GPT-5 Feedback

1. ✅ Dropped macOS runtime PID scans
2. ✅ Retained Windows logic in App.xaml.cs (no splitting)
3. ✅ Avoided cross-platform abstraction layer
4. ✅ Used native OS mechanisms where available
5. ✅ Simplified overall architecture

## Benefits Summary

| Change | Benefit |
|--------|----------|
| Info.plist `LSMultipleInstancesProhibited` | Native, zero-code single-instance on macOS |
| AppKit delegate `OpenFiles()` | Proper Apple event handling for files |
| Removed NSWorkspace scanning | Simpler, sandbox-safe, more robust |
| `Environment.Exit(0)` on Windows | Prevents process instability |
| No cross-platform abstraction | Clearer, more maintainable code |

## Acceptance Criteria (Revised)

- [ ] Windows single instance enforcement works with clean exit
- [ ] macOS single instance enforced via Info.plist
- [ ] macOS file opening works via AppKit delegate
- [ ] iOS documented as system-enforced (no custom code)
- [ ] All platform-specific code properly segregated
- [ ] Documentation updated with revised approach
- [ ] Manual test procedures validated
- [ ] GPT-5 feedback fully incorporated
- [ ] Issue #964 closed

## References

### Original Sources
- Apple: NSRunningApplication, NSWorkspace, NSApplication, UIApplication, UIScene
- Microsoft: App Lifecycle API, Single-Instance WinUI Apps
- UNO Platform: Platform-specific C#, macOS internals, Solution Structure

### GPT-5 Evaluation
- **File**: `/mnt/d/dev/src/StoryCAD/devdocs/Issue_0964_GPT5_evaluation.md`
- **Key Insights**: LSMultipleInstancesProhibited, AppKit delegate, remove scanning, clean exit

## Next Steps

1. ✅ Review GPT-5 feedback (complete)
2. ✅ Update plan with improvements (complete)
3. Create macOS Info.plist with LSMultipleInstancesProhibited
4. Implement AppDelegate.OpenFiles()
5. Port Windows code with Environment.Exit(0)
6. Update Windowing.ActivateMainInstance()
7. Add ShellViewModel.OpenPathFromOS()
8. Create manual test documentation
9. Test on macOS and Windows hardware
10. Update architecture documentation
11. Close issue #964

---

**Document Created**: 2025-10-15
**Last Updated**: 2025-10-15
**Revision**: 2 (Incorporates GPT-5 evaluation feedback)
**Status**: Ready for Implementation
