# Evaluation of Issue #964: Single-Instancing Across Platforms

## Overview
This document evaluates and refines the plan described in `issue_0964_single_instancing.md` for StoryCAD’s UNO Platform conversion. The goal is to retain single-instancing functionality from WinAppSDK on Windows while ensuring compatibility with macOS and future iOS versions.

---

## Strengths
- Correct use of `AppInstance` for Windows single-instancing.
- Appropriate exclusion of iOS and Web (not applicable).
- Proper identification of macOS desktop head as next development target.
- Early initialization placement (`Program.Main`) on macOS is sound.

---

## Recommended Improvements

### 1. macOS: Prefer Info.plist Over Runtime Checks
Use Launch Services’ built-in mechanism:
```xml
<key>LSMultipleInstancesProhibited</key><true/>
```
This eliminates redundant code for detecting other running instances. Launch Services automatically activates the existing instance.

Keep minimal fallback code only for rare launchers bypassing Launch Services.

---

### 2. macOS: Handle “Open File” Correctly
Implement standard AppKit delegate methods:
```csharp
[Register("AppDelegate")]
internal sealed class AppDelegate : NSApplicationDelegate
{
    public override void OpenFiles(NSApplication sender, string[] filenames)
    {
        var shell = Ioc.Default.GetRequiredService<ShellViewModel>();
        foreach (var f in filenames) shell.OpenPathFromOS(f);
        sender.ReplyToOpenOrPrint(NSSaveOptions.Ok);
    }
}
```
Wire this in your Uno Skia macOS head before `host.Run()`.

Declare `.stbx` in Info.plist under `CFBundleDocumentTypes` for proper Finder association.

---

### 3. Remove NSWorkspace/NSRunningApplication Scanning
Process scanning is fragile and unnecessary when using Launch Services. Use:
```csharp
NSRunningApplication.Current.Activate(NSApplicationActivationOptions.ActivateIgnoringOtherApps);
```
only to front the existing app when needed.

---

### 4. Windows: Clean Exit After Redirect
Replace `Process.GetCurrentProcess().Kill()` with:
```csharp
await main.RedirectActivationToAsync(args);
Environment.Exit(0);
```
This avoids potential COM or STA shutdown errors.

---

### 5. Argument and File Handoff Parity
- **Windows**: Continue using redirected activation arguments.
- **macOS**: Let Launch Services deliver file-open Apple Events to your delegate (no IPC required).

---

### 6. iOS: Scene-Based URL Handling
When implemented, prefer `scene(_:openURLContexts:)` over legacy `OpenUrl` methods.

---

## Simplifications

- Drop macOS runtime scans for PID checks.
- Retain Windows logic in `App.xaml.cs` rather than splitting it.
- Avoid abstracting single-instancing into a shared cross-platform service; platform-specific handling is clearer and more maintainable.

---

## Edge Cases

- `LSMultipleInstancesProhibited` also prevents concurrent launches under multiple macOS user sessions — acceptable for desktop apps.
- `.stbx` association is required for Finder open-file routing.
- Uno Skia macOS can over-activate windows on launch; test for redundant calls to `Activate()`.

---

## Test Checklist

**macOS**
- Double-click `.stbx` → launches and opens in single instance.
- Double-click while running → existing instance activates.
- Rapid launches → only one instance.

**Windows**
- Secondary launch redirects activation and exits cleanly.

**iOS (later)**
- Confirm `scene(_:openURLContexts:)` receives file URLs correctly.

---

## Summary of Benefits
| Change | Benefit |
|--------|----------|
| Info.plist key | Native, zero-code single-instance |
| AppKit delegate | Proper Apple event handling |
| Removed scanning | Simpler, sandbox-safe |
| Clean exit on Windows | Prevents process instability |

---

## Conclusion
Implementing macOS single-instancing via `LSMultipleInstancesProhibited` and AppKit delegate open-file handling provides a more robust, idiomatic, and simpler solution than manual process enumeration. Windows code only needs minor cleanup for safe redirection termination.
