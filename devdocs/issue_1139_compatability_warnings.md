# Issue #1139: UNO Platform API Compatibility Research & Implementation Plan

**Issue**: [storybuilder-org/StoryCAD#1139](https://github.com/storybuilder-org/StoryCAD/issues/1139)
**Date**: 2025-10-15
**Status**: Research Complete - Ready for Implementation Planning

---

## Executive Summary

This document consolidates research on 11 Uno0001 warnings (10 unique APIs) preventing full cross-platform compatibility in the StoryCAD UNO Platform migration. Analysis includes:

- **Code location and context** for each warning
- **Platform compatibility status** in UNO Platform
- **Solution strategies** (platform-specific code, alternatives, feature degradation)
- **Implementation complexity** estimates
- **Recommended approaches** with rationale

### Key Findings

**Critical Issues (Must Fix)**:
- **Windowing.desktop.cs** - DisplayArea APIs may block macOS window management

**High Priority (Feature Availability)**:
- **WebViewModel.cs** - WebView2 feature unavailable on macOS
- **ElementPicker.xaml & FileOpenMenu.xaml** - ListBox controls not implemented

**Medium Priority (Optional Features)**:
- **CollaboratorService.cs** - Already handled with `#if !HAS_UNO` guards

**Low Priority (Quality)**:
- **FileOpenVM.cs** - StorageDeleteOption parameter ignored on macOS

---

## Warning Summary Table

| Priority | File | Line(s) | API | Impact | Solution | Effort |
|----------|------|---------|-----|--------|----------|--------|
| **CRITICAL** | Windowing.desktop.cs | 19, 37 | DisplayArea.GetFromWindowId(), .WorkArea | Core window management | Test + suppress warning OR NSScreen fallback | 30 min - 2 hrs |
| **HIGH** | WebViewModel.cs | 262 | CoreWebView2Environment.GetAvailableBrowserVersionString() | Embedded browser feature | Platform-specific partial classes | 15 min |
| **HIGH** | ElementPicker.xaml | 24 | ListBox control | Element selection dialog | Replace with ListView | 5 min |
| **HIGH** | FileOpenMenu.xaml | 99 | ListBox control | Backup selection list | Replace with ListView | 5 min |
| **MEDIUM** | CollaboratorService.cs | 422-442 | AppExtensionCatalog.*, Package.VerifyContentIntegrityAsync() | AI plugin system | Already handled (`#if !HAS_UNO`) | None |
| **LOW** | FileOpenVM.cs | 121 | StorageFile.DeleteAsync(StorageDeleteOption) | Permission test temp file | Accept limitation + comment | 2 min |

**Total Immediate Effort**: ~1 hour (excluding DisplayArea testing)

---

## Detailed Analysis by API Category

### 1. Windowing: DisplayArea.GetFromWindowId() and WorkArea

#### Location
- **File**: `StoryCADLib/Models/Windowing.desktop.cs`
- **Lines**: 19, 37 (in `SetWindowSize()` and `CenterOnScreen()`)

#### Current Code
```csharp
public void SetWindowSize(Window window, int widthDip, int heightDip)
{
    var appWindow = GetAppWindow(window);
    var wa = DisplayArea.GetFromWindowId(appWindow.Id, DisplayAreaFallback.Nearest).WorkArea;

    double s = GetDpiScale(window);
    int w = (int)Math.Round(widthDip * s);
    int h = (int)Math.Round(heightDip * s);

    w = Math.Clamp(w, minW, wa.Width);  // Clamp to screen work area
    h = Math.Clamp(h, minH, wa.Height);

    appWindow.Resize(new SizeInt32 { Width = w, Height = h });
}
```

#### Purpose
Gets screen work area (usable screen space excluding menu bar, dock, taskbar) to:
- Size windows appropriately (not larger than available space)
- Center windows on screen
- Support multi-monitor scenarios

#### UNO Platform Status
- **Multi-window Support**: Added in UNO Platform 5.4.5 (2024)
- **DisplayArea APIs**: Documented as implemented for desktop platforms
- **Current Issue**: Generates Uno0001 warnings despite implementation
- **macOS Equivalent**: Should map to `NSScreen.visibleFrame`

#### Criticality: **CRITICAL**
- Called during **every app startup** and window management operation
- No fallback currently implemented
- **Potential impact**: App may crash or windows incorrectly sized on macOS

#### Recommended Solution

**STRATEGY A (RECOMMENDED)**: Test existing code + suppress warning if working

```xml
<!-- StoryCADLib.csproj -->
<ItemGroup>
  <!-- DisplayArea APIs implemented in UNO 5.4.5+ but generate warnings -->
  <XamlGeneratorAnalyzerSuppressions Include="csharp-Uno0001" />
</ItemGroup>
```

Add comment in code:
```csharp
// DisplayArea.GetFromWindowId is implemented in UNO Platform 5.4.5+
// but generates Uno0001 warnings (false positive)
// See: https://platform.uno/blog/exploring-multi-window-support-for-linux-macos-and-windows/
var wa = DisplayArea.GetFromWindowId(appWindow.Id, DisplayAreaFallback.Nearest).WorkArea;
```

**STRATEGY B (FALLBACK)**: Platform-specific NSScreen implementation if Strategy A doesn't work

```csharp
// Windowing.desktop.cs
private RectInt32 GetWorkAreaSafe(Window window)
{
    try
    {
        var appWindow = GetAppWindow(window);
        return DisplayArea.GetFromWindowId(appWindow.Id, DisplayAreaFallback.Nearest).WorkArea;
    }
    catch (Exception)
    {
#if __MACOS__
        var screen = NSScreen.MainScreen;
        var visibleFrame = screen.VisibleFrame; // Excludes menu bar and dock
        return new RectInt32
        {
            X = (int)visibleFrame.X,
            Y = (int)visibleFrame.Y,
            Width = (int)visibleFrame.Width,
            Height = (int)visibleFrame.Height
        };
#endif
        // Fallback: assume 1920x1040
        return new RectInt32 { X = 0, Y = 0, Width = 1920, Height = 1040 };
    }
}
```

#### Testing Requirements
- [ ] Run app on macOS - verify window appears and sizes correctly
- [ ] Test window centering on macOS
- [ ] Test multi-monitor scenarios on macOS
- [ ] Verify minimum size constraints (1000x700 DIPs)

---

### 2. WebView2: CoreWebView2Environment.GetAvailableBrowserVersionString()

#### Location
- **File**: `StoryCADLib/ViewModels/WebViewModel.cs`
- **Line**: 262

#### Current Code
```csharp
public Task<bool> CheckWebViewState()
{
    try
    {
        if (CoreWebView2Environment.GetAvailableBrowserVersionString() != null)
        {
            return Task.FromResult(true);
        }
    }
    catch { }
    return Task.FromResult(false);
}
```

#### Purpose
Checks if Microsoft Edge WebView2 Runtime is installed before showing embedded web browser feature.

#### UNO Platform Status
- **WebView2 Control**: Supported on Windows (WinAppSDK) and macOS Catalyst
- **CoreWebView2 APIs**: Marked as `[NotImplemented]` in UNO Platform
- **macOS Desktop (Skia)**: Not supported - uses native WKWebView instead

#### Criticality: **HIGH** (Optional Feature)
- WebView functionality allows users to browse web resources within StoryCAD
- NOT critical for core outlining features
- Application continues to work without WebView2

#### Recommended Solution

**STRATEGY: Platform-specific partial classes**

**Shared Code** (`WebViewModel.cs`):
```csharp
public partial class WebViewModel : ObservableRecipient, INavigable, ISaveable
{
    public Task<bool> CheckWebViewState()
    {
        return CheckWebViewStatePlatform();
    }

    // Implemented in platform-specific files
    partial Task<bool> CheckWebViewStatePlatform();
}
```

**Windows** (`WebViewModel.WinAppSDK.cs` - NEW FILE):
```csharp
using Microsoft.Web.WebView2.Core;

namespace StoryCADLib.ViewModels;

public partial class WebViewModel
{
    partial Task<bool> CheckWebViewStatePlatform()
    {
        try
        {
            if (CoreWebView2Environment.GetAvailableBrowserVersionString() != null)
            {
                return Task.FromResult(true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogException(LogLevel.Warn, ex, "WebView2 runtime check failed");
        }
        return Task.FromResult(false);
    }
}
```

**macOS** (`WebViewModel.desktop.cs` - NEW FILE):
```csharp
namespace StoryCADLib.ViewModels;

public partial class WebViewModel
{
    partial Task<bool> CheckWebViewStatePlatform()
    {
        // macOS/Linux use native web views (WKWebView, WebKitGTK)
        // No separate runtime installation needed
        _logger.Log(LogLevel.Info, "Native web view available on this platform");
        return Task.FromResult(true);
    }
}
```

#### Additional Changes
WebView installation UI methods should also move to Windows-specific file:
- `ShowWebViewDialog()` (line 278)
- `InstallWebView()` (line 304)

---

### 3. ListBox Control: Not Implemented in UNO

#### Locations
1. **File**: `StoryCADLib/Services/Dialogs/ElementPicker.xaml`
   - **Line**: 24 (in element picker dialog)

2. **File**: `StoryCADLib/Services/Dialogs/FileOpenMenu.xaml`
   - **Line**: 99 (in backup file list)

#### Current Code

**ElementPicker.xaml**:
```xaml
<ScrollViewer Height="450">
    <ListBox Name="ElementBox" IsEnabled="False" HorizontalAlignment="Center"
             SelectedItem="{x:Bind PickerVM.SelectedElement, Mode=TwoWay}"
             DisplayMemberPath="Name" Margin="0,10,0,0"
             MinWidth="200" />
</ScrollViewer>
```

**FileOpenMenu.xaml**:
```xaml
<ListBox Grid.Row="1" VerticalAlignment="Center" HorizontalAlignment="Center"
         ItemsSource="{x:Bind FileOpenVM.BackupUI}"
         SelectedIndex="{x:Bind FileOpenVM.SelectedBackupIndex, Mode=TwoWay}" />
```

#### Purpose
- **ElementPicker**: Display filterable list of story elements by type
- **FileOpenMenu**: Display list of backup files for restoration

#### UNO Platform Status
- **ListBox**: Marked as "Not yet implemented" (GitHub Issue #1362)
- **ListView**: Fully implemented across all UNO Platform targets
- **UNO Recommendation**: Use ListView instead of ListBox

#### Criticality: **HIGH** (UI Component - Easy Fix)
- Dialogs won't work on macOS without this fix
- ListView is drop-in replacement with same API

#### Recommended Solution

**STRATEGY: Replace ListBox with ListView**

**ElementPicker.xaml**:
```xaml
<ScrollViewer Height="450">
    <ListView Name="ElementBox" IsEnabled="False" HorizontalAlignment="Center"
              SelectedItem="{x:Bind PickerVM.SelectedElement, Mode=TwoWay}"
              DisplayMemberPath="Name" Margin="0,10,0,0"
              MinWidth="200"
              SelectionMode="Single">
        <ListView.ItemsPanel>
            <ItemsPanelTemplate>
                <ItemsStackPanel />
            </ItemsPanelTemplate>
        </ListView.ItemsPanel>
    </ListView>
</ScrollViewer>
```

**FileOpenMenu.xaml**:
```xaml
<ListView Grid.Row="1" VerticalAlignment="Center" HorizontalAlignment="Center"
          ItemsSource="{x:Bind FileOpenVM.BackupUI}"
          SelectedIndex="{x:Bind FileOpenVM.SelectedBackupIndex, Mode=TwoWay}"
          SelectionMode="Single">
    <ListView.ItemsPanel>
        <ItemsPanelTemplate>
            <ItemsStackPanel />
        </ItemsPanelTemplate>
    </ListView.ItemsPanel>
</ListView>
```

**Benefits**:
- No code-behind changes required
- ListView has same API for `ItemsSource`, `SelectedIndex`, `DisplayMemberPath`
- Fully supported on all UNO platforms
- Better performance (native controls on iOS/Android)

#### Note on FileOpenMenu Consistency
The "Recents" tab already uses ListView successfully (line 80). This change makes the "Backups" tab consistent.

---

### 4. Storage: StorageFile.DeleteAsync(StorageDeleteOption)

#### Location
- **File**: `StoryCADLib/ViewModels/FileOpenVM.cs`
- **Line**: 121

#### Current Code
```csharp
// Test write permissions by creating and deleting a temporary file
try
{
    var file = await folder.CreateFileAsync("StoryCAD" + DateTimeOffset.Now.ToUnixTimeSeconds());
    await file.DeleteAsync(StorageDeleteOption.PermanentDelete);
}
catch
{
    ShowWarning = false;
    WarningText = "You can't save outlines to that folder";
    OutlineFolder = "";
    return;
}
```

#### Purpose
Tests write permissions for a selected folder by creating a temporary file and immediately deleting it with permanent deletion (bypassing Recycle Bin).

#### UNO Platform Status
- **API Availability**: Method signature exists in UNO Platform
- **Parameter Status**: `StorageDeleteOption` parameter is `[NotImplemented]` and **ignored**
- **Actual Behavior**: File is deleted, but deletion behavior doesn't change based on option

UNO Source Code:
```csharp
[NotImplemented] // The option is ignored, we implement this only to increase compatibility
public IAsyncAction DeleteAsync(StorageDeleteOption option)
    => AsyncAction.FromTask(ct => Implementation.DeleteAsync(ct, option));
```

#### Platform Behavior
- **Windows**: `PermanentDelete` bypasses Recycle Bin, `Default` sends to Recycle Bin
- **macOS**: Parameter ignored - file always permanently deleted (no Trash integration)

#### Criticality: **LOW** (Test Code - Minor Issue)
- Permission testing is defensive coding practice
- Main concern: may leave temp file on macOS if DeleteAsync fails
- Not a functional issue for the permission test

#### Recommended Solution

**STRATEGY: Accept limitation + add documentation comment**

```csharp
try
{
    var file = await folder.CreateFileAsync("StoryCAD" + DateTimeOffset.Now.ToUnixTimeSeconds());
    // Note: StorageDeleteOption.PermanentDelete parameter is ignored on macOS
    // (file always permanently deleted, no Trash integration)
    await file.DeleteAsync(StorageDeleteOption.PermanentDelete);
}
catch
{
    ShowWarning = false;
    WarningText = "You can't save outlines to that folder";
    OutlineFolder = "";
    return;
}
```

**Rationale**:
- Current code works cross-platform without changes
- Permission testing doesn't care about Recycle Bin vs permanent deletion
- UNO Platform provides compatibility layer
- Minimal code changes = fewer bugs

---

### 5. App Extensions: AppExtensionCatalog & Package.VerifyContentIntegrityAsync

#### Location
- **File**: `StoryCADLib/Services/Collaborator/CollaboratorService.cs`
- **Lines**: 422, 423, 428, 429, 438, 442

#### Current Code
```csharp
private async Task<bool> FindDll()
{
#if !HAS_UNO
    var catalog = AppExtensionCatalog.Open("org.storybuilder");
    var exts = await catalog.FindAllAsync();

    var collab = exts.FirstOrDefault(e =>
        string.Equals(e.Package.Id.Name, "StoryCADCollaborator", StringComparison.OrdinalIgnoreCase));

    if (collab == null) return false;

    if (!await collab.Package.VerifyContentIntegrityAsync())
    {
        _logService.Log(LogLevel.Error, "Integrity check failed");
        return false;
    }

    dllPath = Path.Combine(collab.Package.InstalledLocation.Path, PluginFileName);
    return File.Exists(dllPath);
#else
    // macOS plugin loading tracked in Issue #1126 and #1135
    await Task.CompletedTask;
    _logService.Log(LogLevel.Error, "Collaborator is not supported on this platform.");
    return false;
#endif
}
```

#### Purpose
Three-stage plugin discovery system:
1. **Environment Variable** (`STORYCAD_PLUGIN_DIR`): Explicit override
2. **Sibling Repository**: Scans for dev builds
3. **MSIX AppExtension**: Discovers installed MSIX package (production)

#### UNO Platform Status
- **AppExtensionCatalog**: Windows-only (MSIX packaging system)
- **Package.VerifyContentIntegrityAsync**: Windows-only (code signing validation)
- **macOS Equivalent**: Requires different plugin architecture (App Bundles, code signing)

#### Criticality: **MEDIUM** (Optional Feature - Already Handled)
- Collaborator provides AI-powered writing assistance
- NOT required for core story outlining functionality
- **Already correctly guarded** with `#if !HAS_UNO` at line 291
- macOS path logs error and returns false (line 340)

#### Recommended Solution

**STRATEGY: No immediate action - defer to Issues #1126 and #1135**

**Current State**: ✅ **ALREADY HANDLED CORRECTLY**
- Code wrapped in `#if !HAS_UNO` guard
- macOS users see appropriate message that Collaborator is not supported
- Application fully functional without Collaborator

**Long-term Strategy** (when implementing Issues #1126, #1135):
1. Create `CollaboratorService.WinAppSDK.cs` with MSIX plugin logic
2. Create `CollaboratorService.desktop.cs` with macOS plugin discovery:
   - Look in standard macOS plugin locations
   - Verify code signature using `Security.framework` or .NET strong name
   - Load plugin via existing `AssemblyLoadContext` (cross-platform)

**Complexity**: **COMPLEX** (4-8 hours when prioritized)
- Requires platform-specific plugin discovery mechanisms
- Different code signature verification APIs per platform
- File system layout differences (MSIX vs App Bundles)

---

## Implementation Plan

### Phase 1: Immediate Fixes (1 hour total)

#### Task 1: Replace ListBox with ListView ⏱️ 5 minutes
**Files**: `ElementPicker.xaml`, `FileOpenMenu.xaml`

1. Find-replace `<ListBox` → `<ListView` in both files
2. Add `SelectionMode="Single"` and `ItemsStackPanel` template
3. Test element selection and backup selection on Windows

**Agent Recommendation**: None needed (trivial XAML change)

---

#### Task 2: Split WebViewModel ⏱️ 15 minutes
**Files**: `WebViewModel.cs` (modify), `WebViewModel.WinAppSDK.cs` (new), `WebViewModel.desktop.cs` (new)

1. Extract `CheckWebViewState()` to partial method
2. Create platform-specific implementations
3. Move WebView installation UI to WinAppSDK.cs
4. Test on Windows (verify WebView2 detection works)

**Agent Recommendation**: **csharp-pro** for refactoring partial classes

---

#### Task 3: Add StorageFile Comment ⏱️ 2 minutes
**File**: `FileOpenVM.cs`

Add documentation comment explaining parameter limitation on macOS.

**Agent Recommendation**: None needed (trivial comment addition)

---

### Phase 2: Testing & Validation (30 minutes - 2 hours)

#### Task 4: Test DisplayArea APIs on macOS ⏱️ 30 minutes - 2 hours
**File**: `Windowing.desktop.cs`

**Testing Steps**:
1. Run StoryCAD on macOS development machine
2. Verify app launches without crashes
3. Test window sizing (not larger than screen, respects menu bar/dock)
4. Test window centering
5. Test multi-monitor scenarios (if applicable)

**If DisplayArea APIs work**:
- Add warning suppression to `StoryCADLib.csproj`
- Add comment explaining API is implemented but generates warnings
- ⏱️ 30 minutes

**If DisplayArea APIs don't work**:
- Implement NSScreen fallback (Strategy B from research)
- Test fallback implementation
- ⏱️ 2 hours

**Agent Recommendation**: **debugger** if issues encountered

---

### Phase 3: Deferred (Future Work)

#### Task 5: Collaborator Plugin System for macOS
**Tracked in**: Issues #1126, #1135
**Effort**: 4-8 hours
**Status**: Deferred - no immediate action needed

When prioritized:
1. Design macOS plugin installation flow
2. Implement `CollaboratorService.desktop.cs` with file system discovery
3. Add code signature verification
4. Update user documentation

**Agent Recommendation**: **architect-reviewer** for plugin architecture design

---

## Testing Checklist

### Critical Path Testing (Required Before macOS Release)

**Windowing** (CRITICAL):
- [ ] macOS: App launches without crashes
- [ ] macOS: Main window sizes correctly (not larger than screen)
- [ ] macOS: Main window centers correctly
- [ ] macOS: Window respects menu bar and dock (work area calculation)
- [ ] macOS: Multi-monitor support works (if applicable)
- [ ] Windows: Regression test - window sizing still works

**WebView2** (HIGH):
- [ ] Windows: WebView2 detection works (with runtime installed)
- [ ] Windows: Install dialog appears (without runtime)
- [ ] macOS: Web views load without WebView2 runtime
- [ ] macOS: No install prompts appear
- [ ] Both: Web navigation feature works

**ListBox → ListView** (HIGH):
- [ ] Windows: Element picker displays list correctly
- [ ] Windows: Element picker selection works
- [ ] Windows: Backup list displays correctly
- [ ] Windows: Backup selection and restore works
- [ ] macOS: Element picker displays list correctly
- [ ] macOS: Element picker selection works
- [ ] macOS: Backup list displays correctly
- [ ] macOS: Backup selection and restore works

### Optional Feature Testing

**StorageFile.DeleteAsync** (LOW):
- [ ] Windows: Folder permission test completes successfully
- [ ] macOS: Folder permission test completes successfully
- [ ] Both: "Can't save to folder" warning appears when needed

**Collaborator** (MEDIUM - No Changes):
- [ ] Windows: Collaborator loads if installed
- [ ] Windows: Collaborator menu appears
- [ ] macOS: Graceful message that Collaborator not supported
- [ ] macOS: Collaborator menu hidden

---

## Risk Assessment

### High Risk
- **DisplayArea APIs**: If not working on macOS, window management breaks
  - **Mitigation**: Test early, have NSScreen fallback ready
  - **Rollback**: Can implement fallback in <2 hours if needed

### Medium Risk
- **WebViewModel split**: Risk of breaking existing Windows functionality
  - **Mitigation**: Keep Windows code identical to current implementation
  - **Rollback**: Easy to revert partial class changes

### Low Risk
- **ListBox → ListView**: Proven working pattern (Recents tab already uses ListView)
- **StorageFile comment**: No code changes
- **Collaborator**: Already disabled on macOS

---

## Success Criteria

### Definition of Done

**Phase 1 Complete**:
- [ ] All ListBox references replaced with ListView
- [ ] WebViewModel split into platform-specific files
- [ ] StorageFile.DeleteAsync documented
- [ ] Windows builds without errors
- [ ] All Windows regression tests pass

**Phase 2 Complete**:
- [ ] macOS app launches successfully
- [ ] macOS window sizing/centering works correctly
- [ ] All critical path tests pass on macOS
- [ ] Uno0001 warnings reduced from 14 to 0 (or suppressed with justification)

**Release Ready**:
- [ ] All Phase 1 and Phase 2 tasks complete
- [ ] User documentation updated (if needed)
- [ ] Platform differences documented
- [ ] PR includes testing evidence (screenshots, test results)

---

## Documentation Requirements

### Code Documentation
1. **DisplayArea APIs**: Add comment explaining UNO Platform status
2. **WebViewModel**: Add XML doc comments to partial methods
3. **StorageFile.DeleteAsync**: Add comment about platform differences
4. **Platform-specific files**: Add header comments explaining why code is split

### User Documentation
1. **macOS Feature Parity**: Document which features differ on macOS vs Windows
   - Collaborator AI features not available
   - WebView uses native WKWebView instead of Edge
2. **Installation Guide**: Update with macOS-specific instructions (if needed)

### Developer Documentation
1. **Architecture Memory**: Update `/home/tcox/.claude/memory/architecture.md` with:
   - DisplayArea API status
   - Platform-specific WebViewModel pattern
   - ListView preference over ListBox for UNO
2. **Patterns**: Update `/home/tcox/.claude/memory/patterns.md` with:
   - Platform-specific partial class pattern
   - UNO Platform warning suppression guidelines

---

## Agent Assignments

Based on agent expertise and task requirements:

### Search-Specialist ✅ COMPLETED
- Located all Uno0001 warning sources
- Provided context for each warning
- Identified dependencies and callers

### Research-Analyst ✅ COMPLETED
- Researched UNO Platform implementation status
- Identified cross-platform alternatives
- Provided solution strategies with rationale

### csharp-pro (Recommended for Phase 1)
- **Task 2**: WebViewModel refactoring into partial classes
- **Rationale**: Expert in C# refactoring, MVVM patterns, and modern C# features
- **Deliverables**: Three files (WebViewModel.cs, .WinAppSDK.cs, .desktop.cs)

### debugger (On-call for Phase 2)
- **Task 4**: DisplayArea API testing on macOS
- **Rationale**: Expert in debugging cross-platform issues, if problems encountered
- **Trigger**: Only if DisplayArea testing reveals issues

---

## References

### UNO Platform Documentation
- [Platform-Specific C#](https://platform.uno/docs/articles/platform-specific-csharp.html)
- [Multi-Window Support Blog](https://platform.uno/blog/exploring-multi-window-support-for-linux-macos-and-windows/)
- [Implemented Views Matrix](https://platform.uno/docs/articles/implemented-views.html)
- [UNO Error Codes](https://platform.uno/docs/articles/uno-build-error-codes.html)

### GitHub Issues
- [ListBox Not Implemented (#1362)](https://github.com/unoplatform/uno/issues/1362)
- [WebView2 Support (#4758)](https://github.com/unoplatform/uno/issues/4758)

### StoryCAD Issues
- [#1139 (This Issue)](https://github.com/storybuilder-org/StoryCAD/issues/1139): UNO Platform API Compatibility
- [#1126](https://github.com/storybuilder-org/StoryCAD/issues/1126): Collaborator macOS Support
- [#1135](https://github.com/storybuilder-org/StoryCAD/issues/1135): Plugin System Architecture

### Supporting Research Documents
- `/tmp/uno0001_warning_analysis.md` - Detailed code context analysis
- `/mnt/d/dev/src/StoryCAD/devdocs/issue_1139_uno_api_research.md` - Comprehensive API research

---

## Next Steps

1. **Review this plan** with project stakeholders
2. **Approve Phase 1 implementation** (1 hour of work)
3. **Schedule macOS testing** for Phase 2 (requires macOS dev environment)
4. **Create implementation branch**: `issue-1139-uno-compatibility`
5. **Launch csharp-pro agent** for Task 2 (WebViewModel refactoring)

---

**Document Version**: 1.0
**Created**: 2025-10-15
**Agents Used**: search-specialist, research-analyst
**Status**: ✅ Research Complete - Ready for Implementation Approval
