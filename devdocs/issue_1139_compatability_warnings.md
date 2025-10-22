# Issue #1139: UNO Platform API Compatibility Research & Implementation Plan

**Issue**: [storybuilder-org/StoryCAD#1139](https://github.com/storybuilder-org/StoryCAD/issues/1139)
**Date**: 2025-10-15
**Last Updated**: 2025-10-22
**Status**: Research Complete - Ready for Implementation Planning

---

## Executive Summary

This document consolidates research on 8 Uno0001 warnings preventing full cross-platform compatibility in the StoryCAD UNO Platform migration. Analysis includes:

- **Code location and context** for each warning
- **Platform compatibility status** in UNO Platform
- **Solution strategies** (platform-specific code, alternatives, feature degradation)
- **Implementation complexity** estimates
- **Recommended approaches** with rationale

### Key Findings

**Critical Issues (Must Fix)**:
- **Shell.xaml** - UIElement.ContextRequested event not implemented
- **WebPage.xaml** - WebView/WebView2 not implemented for desktop (Skia) targets

**High Priority (Feature Availability)**:
- **ElementPicker.xaml & FileOpenMenu.xaml** - ListBox controls not implemented

**Medium Priority (Optional Features)**:
- **CollaboratorService.cs** - Already handled with `#if !HAS_UNO` guards

**Low Priority (Quality)**:
- **FileOpenVM.cs** - StorageDeleteOption parameter ignored on macOS

**Already Fixed**:
- **Windowing.desktop.cs** - DisplayArea APIs replaced with alternative approaches
- **WebViewModel.cs** - WebView2 runtime check already handled with conditional compilation

---

## Warning Summary Table

| Priority | File | Line(s) | API | Impact | Solution | Effort |
|----------|------|---------|-----|--------|----------|--------|
| **CRITICAL** | Shell.xaml | 451, 515 | UIElement.ContextRequested | Context menu functionality | Use alternative context menu approach | 30 min |
| **CRITICAL** | WebPage.xaml | 43 | WebView/WebView2 | Web research feature unavailable | No solution for desktop/Skia targets | N/A |
| **HIGH** | ElementPicker.xaml | 24 | ListBox control | Element selection dialog | Replace with ListView | 5 min |
| **HIGH** | FileOpenMenu.xaml | 99 | ListBox control | Backup selection list | Replace with ListView | 5 min |
| **MEDIUM** | CollaboratorService.cs | 428-448 | AppExtensionCatalog.*, Package.VerifyContentIntegrityAsync() | AI plugin system | Already handled (`#if !HAS_UNO`) | None |
| **LOW** | FileOpenVM.cs | 121 | StorageFile.DeleteAsync(StorageDeleteOption) | Permission test temp file | Accept limitation + comment | 2 min |

**Previously Fixed**:
- **Windowing.desktop.cs** - DisplayArea APIs replaced with reflection/Win32/AppWindow fallbacks (already implemented)
- **WebViewModel.cs** - WebView2 runtime check wrapped in `#if WINDOWS10_0_18362_0_OR_GREATER` (already implemented)

**Total Immediate Effort**: ~42 minutes (excluding WebView which has no current solution)

---

## Detailed Analysis by API Category

### 1. UIElement.ContextRequested Event

#### Location
- **File**: `StoryCAD/Views/Shell.xaml`
- **Lines**: 451, 515 (in NavigationTree and TrashView TreeView controls)

#### Current Code
```xaml
<ItemsRepeater x:Name="NavigationTree"
               ContextFlyout="{StaticResource AddStoryElementFlyout}"
               ContextRequested="AddButton_ContextRequested">

<muxc:TreeView ItemsSource="{x:Bind AppState.CurrentDocument.Model.TrashView, Mode=TwoWay}"
               ContextFlyout="{StaticResource AddStoryElementFlyout}"
               ContextRequested="AddButton_ContextRequested"
               ...>
```

#### Purpose
The ContextRequested event fires when the user requests a context menu (typically right-click). It provides more control over context menu behavior than ContextFlyout alone, allowing dynamic menu modification based on the element being right-clicked.

#### UNO Platform Status
- **ContextRequested**: Not implemented in UNO Platform desktop head
- **ContextFlyout**: Fully supported across all UNO targets
- **Impact**: Context menus still work via ContextFlyout, but without the dynamic capabilities

#### Criticality: **CRITICAL** (User Experience)
- Context menus are essential for story element management
- ContextFlyout still works, but may not have full dynamic behavior
- Users expect right-click functionality for adding/managing story elements

#### Recommended Solution

**STRATEGY: Remove ContextRequested, rely on ContextFlyout**

Since ContextFlyout is already specified and fully supported, we can remove the ContextRequested event handler:

```xaml
<!-- Updated NavigationTree -->
<ItemsRepeater x:Name="NavigationTree"
               ContextFlyout="{StaticResource AddStoryElementFlyout}">
               <!-- ContextRequested removed -->

<!-- Updated TrashView -->
<muxc:TreeView ItemsSource="{x:Bind AppState.CurrentDocument.Model.TrashView, Mode=TwoWay}"
               ContextFlyout="{StaticResource AddStoryElementFlyout}"
               ItemInvoked="TreeViewItem_Invoked"
               ...>
               <!-- ContextRequested removed -->
```

**Code-Behind Changes**:
Check if `AddButton_ContextRequested` method contains essential logic that needs to be preserved:

```csharp
// In Shell.xaml.cs or ShellViewModel.cs
// Review and potentially remove or refactor:
private void AddButton_ContextRequested(UIElement sender, ContextRequestedEventArgs args)
{
    // If this contains dynamic menu logic, consider alternatives:
    // - Use Opening event of the MenuFlyout
    // - Implement platform-specific behavior with #if directives
}
```

#### Alternative Solutions

If dynamic context menu behavior is essential:

**Option A: Use MenuFlyout.Opening Event**
```xaml
<MenuFlyout x:Name="AddStoryElementFlyout" Opening="OnContextMenuOpening">
    <!-- Menu items -->
</MenuFlyout>
```

**Option B: Platform-Specific Implementation**
```csharp
#if WINDOWS10_0_18362_0_OR_GREATER
    // Use ContextRequested on Windows
    navigationTree.ContextRequested += AddButton_ContextRequested;
#else
    // Use alternative approach on other platforms
    // e.g., handle PointerPressed for right-click detection
#endif
```

---

### 2. Windowing: DisplayArea APIs (ALREADY FIXED)

**Status**: ✅ Fixed in Windowing.desktop.cs

The DisplayArea.GetFromWindowId() and WorkArea APIs were previously causing warnings. These have been resolved by implementing alternative approaches in `Windowing.desktop.cs`:

- Uses reflection to call UNO's WindowManagerHelper when available
- Falls back to Win32 APIs on Windows with valid HWND
- Uses AppWindow methods as final fallback
- No DisplayArea APIs are called, avoiding the UNO warnings entirely

No further action needed.

---

### 3. WebView2: CoreWebView2Environment (ALREADY FIXED)

**Status**: ✅ Fixed in WebViewModel.cs

The CoreWebView2Environment.GetAvailableBrowserVersionString() API warning has been resolved using conditional compilation:

```csharp
#if WINDOWS10_0_18362_0_OR_GREATER
    if (CoreWebView2Environment.GetAvailableBrowserVersionString() != null)
    {
        return Task.FromResult(true);
    }
#else
    // Bypass on non-WinAppSDK
    _logger.Log(LogLevel.Warn, "WebView check skipped, not on WAppSDK");
    return Task.FromResult(true);
#endif
```

The code only compiles the WebView2-specific code on Windows builds, avoiding the warning on desktop builds.

No further action needed.

---

### 4. ListBox Control: Not Implemented in UNO

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

### 5. Storage: StorageFile.DeleteAsync(StorageDeleteOption)

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

### 6. App Extensions: AppExtensionCatalog & Package.VerifyContentIntegrityAsync

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

### 7. WebView/WebView2: Not Implemented for Desktop (Skia)

#### Location
- **File**: `StoryCAD/Views/WebPage.xaml`
- **Line**: 43 (WebView2 control)
- **Code-behind**: `StoryCAD/Views/WebPage.xaml.cs`

#### Current Code
```xaml
<WebView2 Name="WebView" Grid.Row="2" Source="{x:Bind WebVM.Url, Mode=TwoWay}"
          NavigationCompleted="Web_OnNavigationCompleted" />
```

#### Purpose
Displays web content for research notes within StoryCAD. Users can navigate to web pages, save URLs with their story elements, and reference online resources.

#### The Problem

**WebView2 and WebView are both NOT IMPLEMENTED for UNO Platform desktop (Skia) targets.**

Despite UNO Platform documentation claiming WebView is "supported on all targets," this is misleading. The actual implementation status:

| Control | Windows (WinAppSDK) | macOS Catalyst | **macOS/Linux Desktop (Skia)** |
|---------|---------------------|----------------|--------------------------------|
| WebView | ✅ Works | ✅ Works | **❌ Not Implemented** |
| WebView2 | ✅ Works | ✅ Works | **❌ Not Implemented** |

The source code explicitly shows:
```csharp
#if IS_UNIT_TESTS || __SKIA__ || __NETSTD_REFERENCE__
[Uno.NotImplemented("IS_UNIT_TESTS", "__SKIA__", "__NETSTD_REFERENCE__")]
#endif
public partial class WebView2 : Control
```

#### Build Configuration Context

StoryCAD currently builds two targets:
- **Windows**: `net9.0-windows10.0.22621` - WebView2 works correctly
- **Desktop**: `net9.0-desktop` - This is the Skia target where WebView/WebView2 don't work

The `net9.0-desktop` target uses Skia for cross-platform rendering on Windows, macOS, and Linux. However, Skia-based rendering cannot easily embed native OS controls like web browsers.

#### Alternative Target (Not Currently Used)

`net9.0-maccatalyst` would support WebView on macOS (using iOS/UIKit controls), but:
- Would require significant project restructuring
- Creates an iOS-style app rather than native desktop app
- Different sandboxing and file system access patterns
- Not a drop-in replacement for desktop target

#### Criticality: **HIGH** (Feature Loss)
- Web research functionality completely unavailable on macOS/Linux
- No workaround within the current architecture
- Affects user workflow for story research

#### Technical Background

GitHub Issue #4681 explains: "The ability to display a system control over a Skia rendered canvas" is the blocking technical challenge. The Skia rendering pipeline draws everything to a graphics surface and cannot easily composite native OS windows/controls.

#### Current Status
- **No timeline for implementation** from UNO Platform team
- Issue has been open since 2023
- Fundamental architectural limitation, not a simple bug

---

## Implementation Plan

### Phase 1: Immediate Fixes (45 minutes total)

#### Task 1: Fix ContextRequested Warning ⏱️ 30 minutes
**File**: `StoryCAD/Views/Shell.xaml`

1. Remove `ContextRequested="AddButton_ContextRequested"` from lines 451 and 515
2. Check Shell.xaml.cs for `AddButton_ContextRequested` method
3. If method contains essential logic, refactor to use MenuFlyout.Opening event or platform-specific code
4. Test context menus still work on Windows

**Agent Recommendation**: None needed (straightforward XAML/code-behind change)

---

#### Task 2: Replace ListBox with ListView ⏱️ 10 minutes
**Files**: `ElementPicker.xaml`, `FileOpenMenu.xaml`

1. Find-replace `<ListBox` → `<ListView` in both files
2. Add `SelectionMode="Single"` and `ItemsStackPanel` template
3. Test element selection and backup selection on Windows

**Agent Recommendation**: None needed (trivial XAML change)

---

#### Task 3: Add StorageFile Comment ⏱️ 5 minutes
**File**: `FileOpenVM.cs`

Add documentation comment explaining parameter limitation on macOS.

**Agent Recommendation**: None needed (trivial comment addition)

---

### Phase 2: Deferred (Future Work)

#### Task 4: Collaborator Plugin System for macOS
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

**Context Menus** (CRITICAL):
- [ ] Windows: Context menus appear on right-click in NavigationTree
- [ ] Windows: Context menus appear on right-click in TrashView
- [ ] macOS: Context menus appear via ContextFlyout (after ContextRequested removal)
- [ ] macOS: Menu items function correctly
- [ ] Both: Story element creation from context menu works

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
- **ContextRequested Event**: Context menus are critical for user interaction
  - **Mitigation**: ContextFlyout already in place as fallback
  - **Rollback**: Can implement MenuFlyout.Opening event if needed

### Low Risk
- **ListBox → ListView**: Proven working pattern (Recents tab already uses ListView)
- **StorageFile comment**: No code changes
- **Collaborator**: Already disabled on macOS with #if guards
- **DisplayArea**: Already fixed with alternative implementation
- **WebView2**: Already fixed with conditional compilation

---

## Success Criteria

### Definition of Done

**Phase 1 Complete**:
- [ ] ContextRequested event handlers removed/refactored
- [ ] All ListBox references replaced with ListView
- [ ] StorageFile.DeleteAsync documented
- [ ] Windows builds without errors
- [ ] All Windows regression tests pass
- [ ] Context menus still functional

**Release Ready**:
- [ ] All Phase 1 tasks complete
- [ ] macOS app launches successfully
- [ ] All critical path tests pass on macOS
- [ ] Uno0001 warnings reduced to acceptable levels (CollaboratorService warnings are expected)
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

### No Specialized Agents Needed for Remaining Tasks
- **Task 1** (ContextRequested): Simple XAML/code-behind change
- **Task 2** (ListBox → ListView): Trivial XAML replacement
- **Task 3** (StorageFile comment): Documentation only

All remaining tasks are straightforward and don't require specialized agent assistance.

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

1. **Review this updated plan** with project stakeholders
2. **Approve Phase 1 implementation** (45 minutes of work)
3. **Schedule macOS testing** (requires macOS dev environment)
4. **Create implementation branch**: `issue-1139-uno-compatibility`
5. **Execute the three simple tasks** (no specialized agents needed)

---

**Document Version**: 1.1
**Created**: 2025-10-15
**Updated**: 2025-10-22
**Changes**: Updated warning list after building with desktop target; removed fixed warnings (DisplayArea, WebView2); added ContextRequested warning
**Status**: ✅ Analysis Updated - Ready for Implementation
