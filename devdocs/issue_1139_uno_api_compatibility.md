## Problem Statement

We have 11 **Uno0001** warnings indicating APIs that are not implemented in UNO Platform's desktop head (macOS). These warnings identify Windows-specific or WinUI 3-specific APIs that have no equivalent implementation in UNO Platform, preventing full cross-platform functionality.

**Impact**: HIGH - Directly affects macOS (desktop head) functionality and cross-platform feature parity.

## Current Status

Branch: `UNOTestBranch`
Target Platforms:
- Windows (net9.0-windows10.0.22621) - ✅ Full WinUI 3 support
- macOS (net9.0-desktop) - ⚠️ Limited by UNO API availability

## Affected APIs (11 instances)

### 1. ListBox Control
**API**: `Microsoft.UI.Xaml.Controls.ListBox`
**Status**: Not available on Desktop head
**Affected Files**: Multiple view files
**Impact**: UI controls using ListBox will not work on macOS
**Workaround Options**:
- Use `ListView` (UNO-supported alternative)
- Use `ItemsRepeater` with custom styling
- Platform-specific XAML

### 2. DisplayArea.WorkArea
**API**: `Microsoft.UI.Windowing.DisplayArea.WorkArea`
**Status**: Windows-only API (Windows App SDK)
**Affected Files**: Window management code
**Impact**: Cannot get screen working area on macOS
**Workaround Options**:
- Use platform-specific code with `#if __MACOS__`
- Use AppKit's `NSScreen` directly on macOS
- Abstract window positioning logic

### 3. StorageFile.DeleteAsync Overload
**API**: `Windows.Storage.StorageFile.DeleteAsync(StorageDeleteOption)`
**Status**: Overload with `StorageDeleteOption` not supported
**Affected Files**: File management code
**Impact**: Cannot control delete behavior (permanent vs. recycle bin)
**Workaround Options**:
- Use parameterless `DeleteAsync()` on non-Windows platforms
- Platform-specific code for delete options
- Use System.IO.File.Delete() as fallback

### 4. App Extensions System
**API**: `Windows.ApplicationModel.AppExtensions.*`
**Status**: Extension system not implemented in UNO
**Affected Files**: Extension/plugin loading code (if used)
**Impact**: Plugin/extension functionality unavailable on macOS
**Workaround Options**:
- Implement custom plugin system
- Disable extension features on macOS
- Use MEF (Managed Extensibility Framework) as cross-platform alternative

### 5. WebView2 Environment
**API**: `CoreWebView2Environment.GetAvailableBrowserVersionString()`
**Status**: WebView2-specific (Microsoft Edge WebView2)
**Affected Files**: WebView initialization/version checking
**Impact**: Cannot detect browser version on macOS
**Workaround Options**:
- Skip version check on macOS
- Use WKWebView on macOS (native WebKit)
- Detect platform and use appropriate API

## Related Documentation

- **Build Analysis**: `/devdocs/issue_1134_build_warnings_analysis.md` (Category 3)
- **UNO Platform Docs**: [Platform-Specific C#](https://platform.uno/docs/articles/platform-specific-csharp.html)
- **API Compatibility**: [Supported WinUI APIs](https://platform.uno/docs/articles/supported-features.html)

## Proposed Solution Strategy

For each API warning:

### Step 1: Identify Usage
```bash
# Find where each API is used
grep -r "ListBox" StoryCADLib/ --include="*.cs" --include="*.xaml"
grep -r "DisplayArea.WorkArea" StoryCADLib/ --include="*.cs"
# etc.
```

### Step 2: Categorize by Approach

**Category A: Use Platform-Specific Code**
```csharp
#if HAS_UNO_WINUI
    // Windows-specific implementation
    var workArea = DisplayArea.Primary.WorkArea;
#elif __MACOS__
    // macOS-specific implementation using AppKit
    var workArea = NSScreen.MainScreen.VisibleFrame;
#endif
```

**Category B: Use UNO-Compatible Alternative**
```xaml
<!-- Instead of ListBox -->
<ListView>
  <!-- Same items -->
</ListView>
```

**Category C: Feature Unavailable on macOS**
```csharp
#if !__MACOS__
    // Windows-only feature
    await LoadExtensions();
#endif
```

### Step 3: Document Platform Differences
- Update user documentation with platform-specific features
- Add comments in code explaining platform choices

## Acceptance Criteria

- [ ] All 11 Uno0001 warnings analyzed and categorized
- [ ] Solution approach documented for each API
- [ ] Platform-specific code implemented with proper #if directives
- [ ] UNO-compatible alternatives used where applicable
- [ ] Build succeeds on both Windows and macOS with zero Uno0001 warnings
- [ ] Functionality tested on Windows (no regressions)
- [ ] Functionality tested on macOS (acceptable feature parity or documented limitations)
- [ ] Platform differences documented in user manual
- [ ] Code comments explain platform-specific choices

## Out of Scope

This issue does NOT include:
- General UNO platform migration work
- New feature development
- UI redesign
- Performance optimization

This is specifically about resolving API compatibility warnings for existing code.

## Testing Plan

### Windows Testing
- [ ] All features work as before (no regressions)
- [ ] Build produces zero Uno0001 warnings for Windows target

### macOS Testing
- [ ] Build produces zero Uno0001 warnings for desktop target
- [ ] App launches successfully
- [ ] Features using affected APIs work correctly (or gracefully degrade)
- [ ] No crashes due to missing API implementations

### Cross-Platform Testing
- [ ] File operations work on both platforms
- [ ] Window management works on both platforms
- [ ] WebView features work on both platforms (or documented limitations)
- [ ] UI controls render and function correctly

## Priority

**HIGH** - Blocking full macOS support

This should be prioritized alongside other UNO platform migration work and before any macOS release.

## Estimated Effort

- **Investigation**: 2-4 hours (analyze each API usage)
- **Implementation**: 4-8 hours (platform-specific code)
- **Testing**: 4-6 hours (both platforms)
- **Documentation**: 1-2 hours

**Total**: 11-20 hours

## Dependencies

- UNO Platform migration work (UNOTestBranch)
- Access to macOS development environment for testing
- Understanding of AppKit APIs (for macOS alternatives)

## References

- [UNO Platform - Platform-Specific C#](https://platform.uno/docs/articles/platform-specific-csharp.html)
- [UNO Platform - Feature Compatibility](https://platform.uno/docs/articles/supported-features.html)
- [WinUI 3 to UNO Migration Guide](https://platform.uno/docs/articles/howto-migrate-existing-code.html)
- StoryCAD Testing Strategy: `/StoryCADTests/ManualTests/UNO_Platform_Testing_Strategy.md`
