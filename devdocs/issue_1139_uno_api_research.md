# UNO Platform API Compatibility Research Report
**Issue**: #1139 - WinUI 3 to UNO Platform Migration
**Date**: 2025-10-15
**Analyst**: Research Analyst Agent

## Executive Summary

This report provides comprehensive research on 5 API categories (10 unique APIs) generating Uno0001 compatibility warnings during the WinUI 3 to UNO Platform migration. Each API has been analyzed for:
- Current UNO Platform implementation status
- Cross-platform alternatives
- Recommended solution strategies
- Implementation complexity estimates

**Key Finding**: All 5 API categories can be migrated using UNO Platform's platform-specific code patterns (partial classes with `.WinAppSDK.cs` and `.desktop.cs` suffixes). Most solutions are moderate complexity, with one complex case (AppExtensions plugin system).

---

## 1. WebView2: CoreWebView2Environment.GetAvailableBrowserVersionString()

### Current Usage in StoryCAD
**Location**: `/mnt/d/dev/src/StoryCAD/StoryCADLib/ViewModels/WebViewModel.cs:262`

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

**Purpose**: Checks if WebView2 runtime is installed before showing web content.

### UNO Platform Status

#### Implementation Details
- **WebView2 Control**: Supported on Windows (WinAppSDK) and macOS Catalyst
- **CoreWebView2 API**: Marked as `[NotImplemented]` in UNO Platform (as of v5.6, 2025)
- **macOS Desktop (Skia)**: Not supported - only macOS Catalyst has WebView2 support
- **WebAssembly**: Uses native iframe instead of WebView2

#### Known Issues
- Issue #18552: WebView2 does not display in UnoSdk projects (WinUI target)
- Issue #4758: General WebView2 support tracking issue
- CoreWebView2 property exists but generates Uno0001 warnings

### Cross-Platform Alternatives

#### Strategy A: Platform-Specific Implementation (RECOMMENDED)
Use partial classes to implement platform-specific version checking:

**Shared Code** (`WebViewModel.cs`):
```csharp
public partial class WebViewModel
{
    public Task<bool> CheckWebViewState()
    {
        return CheckWebViewStateCore();
    }

    // Implemented in platform-specific files
    partial Task<bool> CheckWebViewStateCore();
}
```

**Windows** (`WebViewModel.WinAppSDK.cs`):
```csharp
public partial class WebViewModel
{
    partial Task<bool> CheckWebViewStateCore()
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
}
```

**macOS** (`WebViewModel.desktop.cs`):
```csharp
public partial class WebViewModel
{
    partial Task<bool> CheckWebViewStateCore()
    {
        // On macOS, WebView2 isn't needed - use WKWebView natively
        // Always return true as UNO uses native web views
        return Task.FromResult(true);
    }
}
```

#### Strategy B: Conditional Compilation
Use `#if !HAS_UNO` guards:

```csharp
public Task<bool> CheckWebViewState()
{
#if !HAS_UNO
    try
    {
        if (CoreWebView2Environment.GetAvailableBrowserVersionString() != null)
        {
            return Task.FromResult(true);
        }
    }
    catch { }
#else
    // UNO Platform uses native web views - always available
    return Task.FromResult(true);
#endif
    return Task.FromResult(false);
}
```

#### Strategy C: Runtime Platform Detection
Use `OperatingSystem.IsWindows()`:

```csharp
public Task<bool> CheckWebViewState()
{
    if (OperatingSystem.IsWindows())
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

    // Non-Windows platforms use native web views
    return Task.FromResult(true);
}
```

### Recommended Approach
**Strategy A: Platform-Specific Partial Classes**

**Rationale**:
- Cleanest separation of platform concerns
- Follows UNO Platform best practices (file naming conventions)
- No `#if` directives cluttering shared code
- Easier to maintain and test platform-specific behavior
- macOS uses native WKWebView (always available), Windows needs WebView2 runtime check

### Implementation Complexity
**Trivial** - Simple method split into partial classes, no complex logic changes needed.

### Additional Considerations
- WebView installation UI (`ShowWebViewDialog()`, `InstallWebView()`) is Windows-specific
- Should be moved to `WebViewModel.WinAppSDK.cs`
- macOS doesn't need WebView installation prompts

---

## 2. Storage: StorageFile.DeleteAsync(StorageDeleteOption)

### Current Usage in StoryCAD
**Location**: `/mnt/d/dev/src/StoryCAD/StoryCADLib/ViewModels/FileOpenVM.cs:121`

```csharp
// Test write permissions by creating and deleting a temporary file
try
{
    var file = await folder.CreateFileAsync("StoryCAD" + DateTimeOffset.Now.ToUnixTimeSeconds());
    await file.DeleteAsync(StorageDeleteOption.PermanentDelete);
}
catch
{
    // No permissions, force user to pick different folder
    ShowWarning = false;
    WarningText = "You can't save outlines to that folder";
    OutlineFolder = "";
    return;
}
```

**Purpose**: Test write permissions by creating/deleting a temporary file with permanent deletion (bypassing Recycle Bin).

### UNO Platform Status

#### Implementation Details
- **API Availability**: Method signature exists in UNO Platform (added in v2.1)
- **Parameter Status**: `StorageDeleteOption` parameter is marked `[NotImplemented]` and **ignored**
- **Actual Behavior**: File is deleted, but deletion behavior doesn't change based on option

**UNO Source Code** (from GitHub):
```csharp
[NotImplemented] // The options is ignored, we implement this only to increase compatibility
public IAsyncAction DeleteAsync(StorageDeleteOption option)
    => AsyncAction.FromTask(ct => Implementation.DeleteAsync(ct, option));
```

#### Platform Behavior
- **Windows**: `StorageDeleteOption.PermanentDelete` bypasses Recycle Bin, `Default` sends to Recycle Bin
- **macOS/UNO**: Parameter ignored - file always permanently deleted (no Trash can integration)

### Cross-Platform Alternatives

#### Strategy A: Accept Parameter Limitation (RECOMMENDED)
Use the method as-is, understanding behavior differs by platform:

```csharp
// Works cross-platform but parameter has no effect on macOS
await file.DeleteAsync(StorageDeleteOption.PermanentDelete);
```

**Pros**:
- No code changes required
- Already cross-platform compatible
- UNO Platform provides compatibility layer

**Cons**:
- Behavior differs (macOS always permanent, Windows respects option)
- For permission testing, this doesn't matter

#### Strategy B: Use Parameterless Overload
Use the parameterless version for guaranteed cross-platform consistency:

```csharp
// Always works, always permanent deletion
await file.DeleteAsync();
```

**Pros**:
- Explicit about not using options
- Clearer intent for cross-platform code

**Cons**:
- Less semantic (doesn't express intent for permanent deletion)

#### Strategy C: Platform-Specific Implementation
Implement platform-specific deletion logic:

**Shared Code**:
```csharp
await DeleteFileAsync(file);
```

**Windows** (`FileOpenVM.WinAppSDK.cs`):
```csharp
private Task DeleteFileAsync(StorageFile file)
{
    return file.DeleteAsync(StorageDeleteOption.PermanentDelete).AsTask();
}
```

**macOS** (`FileOpenVM.desktop.cs`):
```csharp
private Task DeleteFileAsync(StorageFile file)
{
    // Option ignored on macOS anyway, use parameterless
    return file.DeleteAsync().AsTask();
}
```

### Recommended Approach
**Strategy A: Accept Parameter Limitation**

**Rationale**:
- Current code works cross-platform without changes
- Permission testing doesn't care about Recycle Bin vs permanent deletion
- UNO Platform provides compatibility (even if incomplete)
- Minimal code changes = fewer bugs
- Can add comment explaining platform differences

**Suggested Code**:
```csharp
try
{
    var file = await folder.CreateFileAsync("StoryCAD" + DateTimeOffset.Now.ToUnixTimeSeconds());
    // Note: StorageDeleteOption parameter ignored on macOS (always permanent)
    await file.DeleteAsync(StorageDeleteOption.PermanentDelete);
}
```

### Implementation Complexity
**Trivial** - No code changes required, just add documentation comment.

### Additional Considerations
- If future requirements need Recycle Bin/Trash integration, implement platform-specific solutions
- macOS Trash API: `NSWorkspace.SharedWorkspace.PerformFileOperation`
- Windows Recycle Bin: `IFileOperation` COM interface (already handled by WinAppSDK)

---

## 3. App Extensions: AppExtensionCatalog & Package.VerifyContentIntegrityAsync

### Current Usage in StoryCAD
**Location**: `/mnt/d/dev/src/StoryCAD/StoryCADLib/Services/Collaborator/CollaboratorService.cs`

```csharp
private async Task<bool> FindDll()
{
#if !HAS_UNO
    var _catalog = AppExtensionCatalog.Open("org.storybuilder");
    var InstalledExtensions = await _catalog.FindAllAsync();

    // ... find Collaborator extension ...

    if (!await pkg.VerifyContentIntegrityAsync())
    {
        _logService.Log(LogLevel.Error, "VerifyContentIntegrityAsync failed; refusing to load.");
        return (false, null);
    }

    var installDir = pkg.InstalledLocation.Path;
    var dll = Path.Combine(installDir, PluginFileName);
    return (true, dll);
#else
    // macOS plugin loading tracked in Issue #1126 and #1135
    _logService.Log(LogLevel.Error, "Collaborator is not supported on this platform.");
    return false;
#endif
}
```

**Purpose**:
1. Discover installed MSIX app extensions (plugin system)
2. Verify package integrity before loading plugin DLL
3. Load Collaborator plugin for AI assistance features

### UNO Platform Status

#### Windows.ApplicationModel.AppExtensions
- **API Availability**: Namespace exists in UNO Platform documentation
- **Implementation Status**: Limited/partial implementation
- **Platform Support**: Windows-only (MSIX packaging system)
- **macOS Equivalent**: None - different plugin architecture needed

#### Package.VerifyContentIntegrityAsync
- **API**: `Windows.ApplicationModel.Package.VerifyContentIntegrityAsync()`
- **Added**: Windows 10 version 1607 (SDK 14393)
- **Purpose**: Verify package hasn't been tampered with (code signing validation)
- **UNO Status**: Not documented as implemented
- **macOS Equivalent**: Code signing verification via `Security.framework`

### Cross-Platform Alternatives

#### macOS Plugin Discovery Alternatives

**Option 1: File System Plugin Directory**
```csharp
// macOS: Look in app bundle or known directory
if (OperatingSystem.IsMacOS())
{
    var bundlePath = NSBundle.MainBundle.BundlePath;
    var pluginDir = Path.Combine(bundlePath, "Contents", "Plugins");
    // Scan directory for .dylib or .dll files
}
```

**Option 2: .NET Plugin Discovery**
Use AssemblyLoadContext (already in use):
```csharp
// Works cross-platform for .NET assemblies
var pluginPath = "/path/to/plugin.dll";
var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(pluginPath);
```

**Option 3: macOS Bundle System**
```csharp
// Use NSBundle for macOS plugin bundles
var pluginBundles = NSBundle.AllBundles
    .Where(b => b.BundleIdentifier?.StartsWith("org.storybuilder.plugin") ?? false);
```

#### Package Integrity Verification Alternatives

**Option 1: Code Signature Verification (macOS)**
```csharp
#if __MACOS__
// Use macOS Security framework
var status = SecStaticCodeCheckValidity(
    codeRef,
    SecCSFlags.Default,
    null
);
if (status != 0)
{
    // Signature invalid
}
#endif
```

**Option 2: File Hash Verification (Cross-Platform)**
```csharp
// SHA-256 hash verification (works everywhere)
private async Task<bool> VerifyPluginIntegrity(string dllPath, string expectedHash)
{
    using var sha256 = SHA256.Create();
    using var stream = File.OpenRead(dllPath);
    var hash = await sha256.ComputeHashAsync(stream);
    var hashString = BitConverter.ToString(hash).Replace("-", "");
    return hashString.Equals(expectedHash, StringComparison.OrdinalIgnoreCase);
}
```

**Option 3: Strong Name Verification (.NET)**
```csharp
// Use .NET strong name verification (cross-platform)
var assemblyName = AssemblyName.GetAssemblyName(dllPath);
if (assemblyName.GetPublicKey()?.Length > 0)
{
    // Assembly is signed
}
```

#### Strategy A: Platform-Specific Plugin Systems (RECOMMENDED)
Implement separate plugin discovery per platform:

**Shared Interface**:
```csharp
public partial class CollaboratorService
{
    private async Task<bool> FindDll()
    {
        return await FindDllPlatformSpecific();
    }

    partial Task<bool> FindDllPlatformSpecific();
}
```

**Windows** (`CollaboratorService.WinAppSDK.cs`):
```csharp
public partial class CollaboratorService
{
    partial async Task<bool> FindDllPlatformSpecific()
    {
        // Existing MSIX AppExtension logic
        var catalog = AppExtensionCatalog.Open("org.storybuilder");
        var exts = await catalog.FindAllAsync();

        var collab = exts.FirstOrDefault(/* ... */);
        if (collab == null) return false;

        // Verify integrity
        if (!await collab.Package.VerifyContentIntegrityAsync())
        {
            _logService.Log(LogLevel.Error, "Integrity check failed");
            return false;
        }

        dllPath = Path.Combine(collab.Package.InstalledLocation.Path, PluginFileName);
        return File.Exists(dllPath);
    }
}
```

**macOS** (`CollaboratorService.desktop.cs`):
```csharp
public partial class CollaboratorService
{
    partial async Task<bool> FindDllPlatformSpecific()
    {
        await Task.CompletedTask; // Keep async signature

        // Look in standard macOS plugin locations
        var pluginPaths = new[]
        {
            // Application support directory
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "StoryCAD", "Plugins", PluginFileName
            ),
            // App bundle plugins folder
            Path.Combine(
                NSBundle.MainBundle.BundlePath,
                "Contents", "PlugIns", PluginFileName
            )
        };

        foreach (var path in pluginPaths)
        {
            if (File.Exists(path))
            {
                // Verify code signature on macOS
                if (VerifyCodeSignature(path))
                {
                    dllPath = path;
                    return true;
                }
            }
        }

        _logService.Log(LogLevel.Info, "Plugin not found in standard locations");
        return false;
    }

    private bool VerifyCodeSignature(string path)
    {
        // Option 1: Use Security framework (requires interop)
        // Option 2: Use codesign CLI tool
        // Option 3: Use .NET strong name verification

        try
        {
            var assemblyName = AssemblyName.GetAssemblyName(path);
            return assemblyName.GetPublicKey()?.Length > 0;
        }
        catch (Exception ex)
        {
            _logService.LogException(LogLevel.Warn, ex, "Code signature verification failed");
            return false;
        }
    }
}
```

#### Strategy B: Disable Plugin System on macOS
Simplest approach - documented in current code:

```csharp
#if !HAS_UNO
    // Windows: Full MSIX plugin support
#else
    // macOS plugin loading tracked in Issue #1126 and #1135
    _logService.Log(LogLevel.Error, "Collaborator is not supported on this platform.");
    return false;
#endif
```

#### Strategy C: Unified Plugin Discovery
Create platform-agnostic plugin system:

```csharp
public interface IPluginDiscovery
{
    Task<PluginInfo?> FindPluginAsync(string identifier);
    Task<bool> VerifyIntegrityAsync(PluginInfo plugin);
}

// Windows implementation uses AppExtensionCatalog
// macOS implementation uses file system + code signing
```

### Recommended Approach
**Strategy B (Short-term): Disable on macOS**
**Strategy A (Long-term): Platform-Specific Plugin Systems**

**Rationale**:
- **Short-term**: Current code already disables Collaborator on macOS (Issue #1126, #1135)
- **Long-term**: Implement macOS plugin discovery when Collaborator support is prioritized
- Plugin systems are platform-specific by nature (MSIX vs App Bundles)
- Code signature verification differs significantly between platforms
- Defer macOS implementation until feature requirements are clear

**Migration Path**:
1. **Phase 1** (Current): Keep `#if !HAS_UNO` guard, Collaborator Windows-only
2. **Phase 2** (Issue #1126): Implement `CollaboratorService.desktop.cs` with macOS plugin discovery
3. **Phase 3**: Unified plugin interface if needed for other extensions

### Implementation Complexity
**Complex** - Requires:
- Platform-specific plugin discovery mechanisms
- Code signature verification (different APIs per platform)
- File system layout differences (MSIX vs App Bundles)
- Testing on both platforms

**Deferred** - Already tracked in Issues #1126 and #1135, no immediate action needed.

### Additional Considerations

#### Windows MSIX AppExtensions
- Requires package identity (MSIX/APPX package)
- Extension catalog is Windows-specific
- Package integrity uses Windows code signing infrastructure

#### macOS Plugin Options
1. **App Bundles** (`.bundle`): Native macOS plugin format
2. **Dynamic Libraries** (`.dylib`): Native shared libraries
3. **.NET Assemblies** (`.dll`): Managed code (current approach)

#### Security Considerations
- **Windows**: MSIX packages verified by Windows Store/signing infrastructure
- **macOS**: Requires notarization and code signing for Gatekeeper
- **Cross-platform**: Strong name signing for .NET assemblies

#### Future Work (Issues #1126, #1135)
- Design macOS plugin installation flow
- Implement code signature verification
- Create plugin discovery service abstraction
- Update user documentation for macOS plugin installation

---

## 4. ListBox Control: XAML ListBox Not Implemented

### Current Usage in StoryCAD

**Location 1**: `/mnt/d/dev/src/StoryCAD/StoryCADLib/Services/Dialogs/FileOpenMenu.xaml:99`
```xaml
<!-- Backups tab -->
<ListBox Grid.Row="1" VerticalAlignment="Center" HorizontalAlignment="Center"
         ItemsSource="{x:Bind FileOpenVM.BackupUI}"
         SelectedIndex="{x:Bind FileOpenVM.SelectedBackupIndex, Mode=TwoWay}" />
```

**Location 2**: `/mnt/d/dev/src/StoryCAD/StoryCADLib/Services/Dialogs/ElementPicker.xaml:24`
```xaml
<!-- Element Picker -->
<ListBox Name="ElementBox" IsEnabled="False" HorizontalAlignment="Center"
         SelectedItem="{x:Bind PickerVM.SelectedElement, Mode=TwoWay}"
         DisplayMemberPath="Name" Margin="0,10,0,0"
         MinWidth="200" />
```

**Purpose**: Display selectable lists in dialog boxes (backup files, story elements).

### UNO Platform Status

#### Implementation Status
- **ListBox Control**: Marked as "Not yet implemented" in UNO Platform (GitHub Issue #1362)
- **Known Issues**: Items not populating when ItemsSource is set (WebAssembly platform)
- **Priority**: Medium difficulty, needs contributor help
- **Last Updated**: Issue remains open as of 2025

#### ListView Alternative
- **ListView**: Fully implemented across all UNO Platform targets
- **ListViewBase**: Implemented with native delegation on Android/iOS for performance
- **Recommendation**: UNO Platform team recommends ListView over ListBox

**UNO Platform Documentation Quote**:
> "While ListBox exists in Uno Platform, there are documented bugs with its implementation, particularly on WebAssembly. The ListBox control still exists, but it's more common to use the ListView as it has a nice set of built in styles."

### Cross-Platform Alternatives

#### Strategy A: Replace ListBox with ListView (RECOMMENDED)
Direct replacement using ListView with ListBox-like styling:

**Before** (ListBox):
```xaml
<ListBox Grid.Row="1" VerticalAlignment="Center" HorizontalAlignment="Center"
         ItemsSource="{x:Bind FileOpenVM.BackupUI}"
         SelectedIndex="{x:Bind FileOpenVM.SelectedBackupIndex, Mode=TwoWay}" />
```

**After** (ListView):
```xaml
<ListView Grid.Row="1" VerticalAlignment="Center" HorizontalAlignment="Center"
          ItemsSource="{x:Bind FileOpenVM.BackupUI}"
          SelectedIndex="{x:Bind FileOpenVM.SelectedBackupIndex, Mode=TwoWay}"
          SelectionMode="Single">
    <!-- Optional: Apply ListBox-like styling -->
    <ListView.ItemsPanel>
        <ItemsPanelTemplate>
            <ItemsStackPanel />
        </ItemsPanelTemplate>
    </ListView.ItemsPanel>
</ListView>
```

**ElementPicker.xaml**:
```xaml
<!-- Element Picker -->
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

**Key Differences**:
- `SelectionMode="Single"` makes ListView behave like ListBox (default is single selection anyway)
- `ItemsStackPanel` maintains vertical list layout
- All data binding (`ItemsSource`, `SelectedIndex`, `DisplayMemberPath`) works identically

#### Strategy B: Platform-Specific XAML
Use conditional XAML compilation:

**Shared XAML**:
```xaml
<not:ListBox xmlns:not="http://uno.ui/not"
             Grid.Row="1" ItemsSource="{x:Bind FileOpenVM.BackupUI}" />
<win:ListView xmlns:win="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
              Grid.Row="1" ItemsSource="{x:Bind FileOpenVM.BackupUI}" />
```

**Cons**: More complex, requires XAML prefixes for platform detection.

#### Strategy C: Use ItemsRepeater
Modern alternative to ListBox/ListView:

```xaml
<ScrollViewer>
    <ItemsRepeater ItemsSource="{x:Bind FileOpenVM.BackupUI}">
        <ItemsRepeater.ItemTemplate>
            <DataTemplate>
                <Border BorderBrush="Gray" BorderThickness="1" Padding="10">
                    <TextBlock Text="{Binding}" />
                </Border>
            </DataTemplate>
        </ItemsRepeater.ItemTemplate>
    </ItemsRepeater>
</ScrollViewer>
```

**Cons**: Requires manual selection handling, more complex for simple lists.

### Recommended Approach
**Strategy A: Replace ListBox with ListView**

**Rationale**:
- **ListView is fully implemented** in UNO Platform (all platforms)
- **API compatibility**: ListView and ListBox share the same base class (`Selector`)
- **No code changes**: ViewModel properties work identically (`SelectedIndex`, `ItemsSource`)
- **Better performance**: UNO uses native controls on iOS/Android
- **UNO Platform recommendation**: Official guidance to prefer ListView
- **Minimal migration effort**: Simple find-replace in XAML

**Migration Checklist**:
1. Replace `<ListBox` with `<ListView` in both XAML files
2. Add `SelectionMode="Single"` (optional, default is already single)
3. Test selection behavior on Windows and macOS
4. No ViewModel changes required

### Implementation Complexity
**Trivial** - Find-replace in XAML, no code changes, no behavior changes expected.

### Additional Considerations

#### Styling Differences
- ListView has different default styling (grid lines, hover effects)
- Can override with custom `ListViewItemStyle` to match ListBox appearance:

```xaml
<ListView.Resources>
    <Style TargetType="ListViewItem">
        <Setter Property="Padding" Value="8"/>
        <Setter Property="HorizontalContentAlignment" Value="Stretch"/>
        <!-- Add more setters to match ListBox style -->
    </Style>
</ListView.Resources>
```

#### Performance Benefits
- **Windows**: Similar performance
- **macOS**: Uses native NSTableView under the hood (better scrolling)
- **Mobile**: Uses UITableView (iOS) and RecyclerView (Android) for virtualization

#### Future-Proofing
- ListView is actively maintained in UNO Platform
- ListBox implementation may never be completed (low priority)
- ListView has more features (grouping, incremental loading, etc.)

---

## 5. Display/Windowing: DisplayArea.GetFromWindowId() and WorkArea

### Current Usage in StoryCAD

**Location**: `/mnt/d/dev/src/StoryCAD/StoryCADLib/Models/Windowing.WinAppSDK.cs` and `Windowing.desktop.cs`

**Windows** (`Windowing.WinAppSDK.cs:34`):
```csharp
public void SetWindowSize(Window window, int widthDip, int heightDip)
{
    var appWindow = GetAppWindow(window);
    var wa = DisplayArea.GetFromWindowId(appWindow.Id, DisplayAreaFallback.Nearest).WorkArea;

    var s = GetDpiScale(window);
    var w = (int)Math.Round(widthDip * s);
    var h = (int)Math.Round(heightDip * s);

    w = Math.Clamp(w, minW, wa.Width);  // Clamp to screen work area
    h = Math.Clamp(h, minH, wa.Height);

    appWindow.Resize(new SizeInt32(w, h));
}
```

**macOS** (`Windowing.desktop.cs:19`):
```csharp
public void SetWindowSize(Window window, int widthDip, int heightDip)
{
    var appWindow = GetAppWindow(window);
    var wa = DisplayArea.GetFromWindowId(appWindow.Id, DisplayAreaFallback.Nearest).WorkArea;
    // ... same logic ...
}
```

**Purpose**: Get screen work area (usable screen space excluding menu bar, dock, taskbar) to size and position windows appropriately.

### UNO Platform Status

#### Microsoft.UI.Windowing APIs
- **DisplayArea**: Part of WinAppSDK windowing APIs
- **GetFromWindowId**: Retrieves display information for a window
- **WorkArea**: Property returning usable screen rectangle (excludes system UI)

#### UNO Platform Support
- **Multi-window Support**: Added in UNO Platform 5.4.5 (2024)
- **AppWindow APIs**: Position, size, move, resize, show/hide implemented
- **DisplayArea Status**: Implementation exists for desktop platforms
- **Current Issue**: Generates Uno0001 warnings despite implementation

#### Platform Equivalents
- **Windows**: `DisplayArea.WorkArea` maps to monitor work area
- **macOS**: Should map to `NSScreen.visibleFrame` (area excluding menu bar and dock)
- **Linux**: Should map to screen geometry minus panels

### Cross-Platform Alternatives

#### Strategy A: Use Existing Code (RECOMMENDED)
Current code in `Windowing.desktop.cs` already uses DisplayArea APIs:

```csharp
var wa = DisplayArea.GetFromWindowId(appWindow.Id, DisplayAreaFallback.Nearest).WorkArea;
```

**Testing Required**: Verify this works on macOS despite Uno0001 warning.

**If Working**:
- Suppress Uno0001 warning in project file
- Add comment explaining API is implemented but generates warning

**Project File** (`StoryCADLib.csproj`):
```xml
<ItemGroup>
  <XamlGeneratorAnalyzerSuppressions Include="csharp-Uno0001" />
</ItemGroup>
```

#### Strategy B: Platform-Specific Screen APIs
Implement native screen queries per platform:

**Shared Code**:
```csharp
public partial class Windowing
{
    private RectInt32 GetWorkArea(Window window);
}
```

**Windows** (`Windowing.WinAppSDK.cs`):
```csharp
private RectInt32 GetWorkArea(Window window)
{
    var appWindow = GetAppWindow(window);
    var displayArea = DisplayArea.GetFromWindowId(appWindow.Id, DisplayAreaFallback.Nearest);
    return new RectInt32
    {
        X = displayArea.WorkArea.X,
        Y = displayArea.WorkArea.Y,
        Width = displayArea.WorkArea.Width,
        Height = displayArea.WorkArea.Height
    };
}
```

**macOS** (`Windowing.desktop.cs`):
```csharp
private RectInt32 GetWorkArea(Window window)
{
    // Get NSScreen for the window's current screen
    var screen = NSScreen.MainScreen; // or find specific screen
    var visibleFrame = screen.VisibleFrame; // Excludes menu bar and dock

    // Convert NSRect to RectInt32
    // Note: macOS coordinates have origin at bottom-left
    return new RectInt32
    {
        X = (int)visibleFrame.X,
        Y = (int)visibleFrame.Y,
        Width = (int)visibleFrame.Width,
        Height = (int)visibleFrame.Height
    };
}
```

**Coordinate System Note**: macOS uses bottom-left origin, Windows uses top-left. May need conversion.

#### Strategy C: Runtime Platform Detection
Check platform at runtime:

```csharp
private (int Width, int Height) GetWorkAreaSize(Window window)
{
    if (OperatingSystem.IsWindows())
    {
        var wa = DisplayArea.GetFromWindowId(
            GetAppWindow(window).Id,
            DisplayAreaFallback.Nearest
        ).WorkArea;
        return (wa.Width, wa.Height);
    }
    else if (OperatingSystem.IsMacOS())
    {
        // Use NSScreen
        var screen = NSScreen.MainScreen;
        return ((int)screen.VisibleFrame.Width, (int)screen.VisibleFrame.Height);
    }

    // Fallback: return large values
    return (2560, 1440);
}
```

### Recommended Approach
**Strategy A: Use Existing Code + Suppress Warning**

**Rationale**:
1. **Current code may already work**: UNO Platform 5.4.5+ has DisplayArea support
2. **Uno0001 warnings can be false positives**: API exists but analyzer flagged it
3. **Minimal changes**: Just suppress warning, test on macOS
4. **Fallback to Strategy B if needed**: If macOS DisplayArea doesn't work, implement platform-specific

**Migration Steps**:
1. **Test current code on macOS**: Run app, verify window sizing works correctly
2. **If working**: Add warning suppression to project file
3. **If not working**: Implement Strategy B (platform-specific screen APIs)

**Warning Suppression** (`StoryCADLib.csproj`):
```xml
<ItemGroup>
  <!-- Suppress Uno0001 for DisplayArea APIs (implemented in UNO 5.4.5+) -->
  <XamlGeneratorAnalyzerSuppressions Include="csharp-Uno0001" />
</ItemGroup>
```

**Add Comment in Code**:
```csharp
// DisplayArea.GetFromWindowId is implemented in UNO Platform 5.4.5+
// but generates Uno0001 warnings (false positive)
// See: https://platform.uno/blog/exploring-multi-window-support-for-linux-macos-and-windows/
var wa = DisplayArea.GetFromWindowId(appWindow.Id, DisplayAreaFallback.Nearest).WorkArea;
```

### Implementation Complexity
**Trivial to Moderate**:
- **If existing code works**: Trivial (just suppress warning)
- **If platform-specific needed**: Moderate (requires NSScreen interop for macOS)

### Additional Considerations

#### macOS Screen Coordinate System
**Key Differences**:
- **Windows**: Origin at top-left of primary monitor
- **macOS**: Origin at bottom-left of primary monitor
- **Multi-monitor**: Coordinate systems differ

**NSScreen Properties**:
- `Frame`: Full screen area (including menu bar, dock)
- `VisibleFrame`: Usable area (excludes menu bar, dock) - equivalent to WorkArea
- **Coordinate Conversion**: May be needed when positioning windows

```csharp
// macOS coordinate conversion (if needed)
var macOSY = screen.Frame.Height - window.Height - windowY;
```

#### Multi-Monitor Support
**Windows DisplayArea**:
- Supports multiple monitors
- `GetFromWindowId` returns display containing window
- `DisplayAreaFallback.Nearest` finds closest monitor if window not on any

**macOS NSScreen**:
- `NSScreen.MainScreen`: Primary display (with menu bar)
- `NSScreen.Screens`: Array of all displays
- `window.Screen`: Get screen containing window

**Cross-Platform Consideration**: Ensure multi-monitor logic works on both platforms.

#### DPI Scaling
**Current Code**:
- **Windows**: Uses `GetDpiForWindow()` Win32 API (correct)
- **macOS**: Hardcoded to `1.0` (may be incorrect for Retina displays)

**macOS Retina Displays**:
- `NSScreen.backingScaleFactor`: 2.0 for Retina, 1.0 for non-Retina
- Consider updating `GetDpiScale()` in `Windowing.desktop.cs`:

```csharp
private static double GetDpiScale(Window window)
{
    // Option 1: Always 1.0 (DIPs match pixels in UNO Skia)
    return 1.0;

    // Option 2: Account for Retina (if needed)
    // var screen = window.Content?.Window?.Screen as NSScreen;
    // return screen?.BackingScaleFactor ?? 1.0;
}
```

#### Window Positioning Best Practices
1. **Respect safe areas**: Avoid menu bar, dock, taskbar
2. **Multi-monitor aware**: Don't position off-screen
3. **Remember user preferences**: Save/restore window position
4. **Handle display changes**: Monitor added/removed/resolution changed

---

## Summary Table: API Migration Strategies

| API Category | Current Status | Recommended Strategy | Complexity | Timeline |
|--------------|---------------|----------------------|------------|----------|
| **WebView2 - GetAvailableBrowserVersionString** | Not implemented in UNO | Platform-specific partial classes | Trivial | Immediate |
| **StorageFile.DeleteAsync(option)** | Parameter ignored in UNO | Accept limitation, add comment | Trivial | Immediate |
| **AppExtensionCatalog & Package.VerifyContentIntegrityAsync** | Windows-only, not in UNO | Keep disabled on macOS (current) | Complex | Deferred (Issues #1126, #1135) |
| **ListBox Control** | Not implemented in UNO | Replace with ListView | Trivial | Immediate |
| **DisplayArea.GetFromWindowId/WorkArea** | Implemented but warns | Suppress warning, test on macOS | Trivial-Moderate | Immediate |

---

## Migration Recommendations

### Phase 1: Immediate Fixes (Trivial Complexity)

#### 1. Replace ListBox with ListView
- **Files**: `FileOpenMenu.xaml`, `ElementPicker.xaml`
- **Effort**: 5 minutes
- **Risk**: Very low
- **Testing**: Verify selection behavior unchanged

#### 2. Add StorageFile.DeleteAsync Comment
- **File**: `FileOpenVM.cs`
- **Effort**: 2 minutes
- **Risk**: None (no code change)
- **Action**: Add comment explaining parameter limitation

#### 3. Split WebViewModel.CheckWebViewState
- **Files**: Create `WebViewModel.WinAppSDK.cs` and `WebViewModel.desktop.cs`
- **Effort**: 15 minutes
- **Risk**: Low
- **Testing**: Verify web view state detection on both platforms

### Phase 2: Testing & Validation

#### 4. Test DisplayArea APIs on macOS
- **File**: `Windowing.desktop.cs`
- **Effort**: 30 minutes
- **Action**:
  1. Run app on macOS
  2. Test window sizing/centering
  3. If working: Add warning suppression
  4. If not working: Implement NSScreen fallback

### Phase 3: Deferred (Complex)

#### 5. Collaborator Plugin System for macOS
- **Tracked in**: Issues #1126, #1135
- **Effort**: 4-8 hours
- **Risk**: High (new functionality)
- **Dependencies**: macOS app bundle packaging, code signing
- **Action**: Design macOS plugin architecture when Collaborator features are prioritized

---

## Warning Suppression Strategy

### Project-Level Suppression
Add to `StoryCADLib.csproj`:

```xml
<PropertyGroup>
  <!-- Suppress Uno0001 warnings for implemented-but-flagged APIs -->
  <NoWarn>$(NoWarn);Uno0001</NoWarn>
</PropertyGroup>

<!-- OR more targeted suppression for XAML generation -->
<ItemGroup>
  <XamlGeneratorAnalyzerSuppressions Include="csharp-Uno0001" />
</ItemGroup>
```

### File-Level Suppression
Add to specific C# files:

```csharp
#pragma warning disable Uno0001 // API not implemented warning
// Code using DisplayArea, etc.
#pragma warning restore Uno0001
```

### Recommended Approach
- **Use XamlGeneratorAnalyzerSuppressions** for XAML-generated code warnings
- **Use #pragma warning** for specific C# API usage
- **Document why** suppression is safe (comment in code or this document)

---

## Testing Checklist

### Per-API Testing

**WebView2**:
- [ ] Windows: Verify WebView2 detection works
- [ ] Windows: Test install dialog flow
- [ ] macOS: Verify web views load without WebView2 runtime
- [ ] macOS: No install prompts appear

**StorageFile.DeleteAsync**:
- [ ] Windows: Verify temp file creation/deletion for permission test
- [ ] macOS: Verify temp file creation/deletion for permission test
- [ ] Both: Verify "can't save to folder" warning works

**ListBox → ListView**:
- [ ] Windows: Backup list displays correctly
- [ ] Windows: Backup selection works
- [ ] macOS: Backup list displays correctly
- [ ] macOS: Backup selection works
- [ ] Windows: Element picker list displays correctly
- [ ] Windows: Element picker selection works
- [ ] macOS: Element picker list displays correctly
- [ ] macOS: Element picker selection works

**DisplayArea/WorkArea**:
- [ ] Windows: Window sizes correctly (not larger than screen)
- [ ] Windows: Window centers correctly
- [ ] Windows: Multi-monitor support works
- [ ] macOS: Window sizes correctly (respects menu bar/dock)
- [ ] macOS: Window centers correctly
- [ ] macOS: Multi-monitor support works (if applicable)

**AppExtensions** (No testing - disabled on macOS):
- [ ] Windows: Collaborator loads if installed
- [ ] macOS: Graceful message that Collaborator not supported

---

## Implementation Priority

### High Priority (Blocking Migration)
1. **ListBox → ListView**: Required for dialogs to work
2. **WebViewModel platform split**: Required for web features

### Medium Priority (Warnings Only)
3. **DisplayArea testing**: Verify existing code works
4. **StorageFile documentation**: Add clarifying comments

### Low Priority (Deferred)
5. **Collaborator macOS support**: Feature parity, tracked separately

---

## Lessons Learned & Best Practices

### UNO Platform Migration Patterns

#### 1. Check Implementation Status First
- **Before making changes**: Research UNO Platform documentation and GitHub issues
- **Example**: DisplayArea might already work despite warnings
- **Tool**: UNO Platform API reference at https://platform.uno/docs/articles/implemented-views.html

#### 2. Prefer Platform-Specific Files Over #if Directives
**Good** (Partial Classes):
```
Windowing.cs           // Shared code
Windowing.WinAppSDK.cs // Windows-specific
Windowing.desktop.cs   // macOS/Linux-specific
```

**Avoid** (Conditional Compilation):
```csharp
#if !HAS_UNO
    // Windows code
#else
    // UNO code
#endif
```

**Why**: Cleaner code organization, easier testing, better IDE support.

#### 3. Use UNO Platform Analyzer Suppressions Correctly
- **XamlGeneratorAnalyzerSuppressions**: For XAML-generated code warnings
- **#pragma warning**: For specific API usage in C# code
- **NoWarn property**: Last resort (suppresses all warnings of type)

#### 4. Test on Target Platforms Early
- Don't assume API compatibility without testing
- UNO Platform warnings can be false positives
- Some APIs work but lack documentation

#### 5. Document Platform Differences
```csharp
// Windows: StorageDeleteOption.PermanentDelete bypasses Recycle Bin
// macOS: Parameter ignored, always permanent deletion
await file.DeleteAsync(StorageDeleteOption.PermanentDelete);
```

### Platform-Specific Considerations

#### Windows (WinAppSDK)
- Full WinUI 3 API support
- Use `!HAS_UNO` conditional for Windows-only code
- DPI scaling via Win32 APIs (`GetDpiForWindow`)

#### macOS (Desktop/Skia)
- Native APIs via `NSScreen`, `NSBundle`, etc.
- Coordinate system origin at bottom-left (vs top-left on Windows)
- Use `__MACOS__` conditional or `.desktop.cs` file naming
- DPI often 1:1 (DIPs = pixels) in UNO Skia rendering

#### Cross-Platform
- Prefer managed APIs when available
- Use platform detection: `OperatingSystem.IsWindows()`, `OperatingSystem.IsMacOS()`
- Test all code paths on all platforms

---

## References

### UNO Platform Documentation
- Platform-Specific C#: https://platform.uno/docs/articles/platform-specific-csharp.html
- Implemented Views: https://platform.uno/docs/articles/implemented-views.html
- Multi-Window Support: https://platform.uno/blog/exploring-multi-window-support-for-linux-macos-and-windows/
- UNO Error Codes: https://platform.uno/docs/articles/uno-build-error-codes.html
- Windowing APIs: https://platform.uno/docs/articles/features/windows-ui-xaml-window.html

### GitHub Issues
- ListBox Not Implemented: https://github.com/unoplatform/uno/issues/1362
- WebView2 Support: https://github.com/unoplatform/uno/issues/4758
- WebView2 UnoSdk Display: https://github.com/unoplatform/uno/issues/18552
- Multi-Window Discussion: https://github.com/unoplatform/uno/discussions/16644

### Microsoft Documentation
- DisplayArea Class: https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.windowing.displayarea
- AppExtensionCatalog: https://learn.microsoft.com/en-us/uwp/api/windows.applicationmodel.appextensions.appextensioncatalog
- StorageFile: https://learn.microsoft.com/en-us/uwp/api/windows.storage.storagefile
- WebView2: https://learn.microsoft.com/en-us/microsoft-edge/webview2/

### Apple Documentation
- NSScreen: https://developer.apple.com/documentation/appkit/nsscreen
- NSBundle: https://developer.apple.com/documentation/foundation/nsbundle
- Code Signing: https://developer.apple.com/documentation/security

### StoryCAD Issues
- Issue #1139: UNO Platform API Compatibility (this research)
- Issue #1126: Collaborator macOS Support
- Issue #1135: Plugin System Architecture

---

## Appendix: Code Examples

### Complete Platform-Specific WebViewModel

**WebViewModel.cs** (Shared):
```csharp
public partial class WebViewModel : ObservableRecipient, INavigable, ISaveable
{
    // ... existing shared code ...

    /// <summary>
    /// Checks if web view runtime is available on this platform
    /// </summary>
    public Task<bool> CheckWebViewState()
    {
        return CheckWebViewStatePlatform();
    }

    // Implemented in platform-specific files
    partial Task<bool> CheckWebViewStatePlatform();
}
```

**WebViewModel.WinAppSDK.cs**:
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

**WebViewModel.desktop.cs**:
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

### Complete DisplayArea Fallback

**Windowing.desktop.cs** (with NSScreen fallback):
```csharp
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;
#if __MACOS__
using AppKit;
#endif

namespace StoryCADLib.Models;

public partial class Windowing
{
    private RectInt32 GetWorkAreaSafe(Window window)
    {
        try
        {
            // Try UNO Platform DisplayArea API first
            var appWindow = GetAppWindow(window);
            var displayArea = DisplayArea.GetFromWindowId(
                appWindow.Id,
                DisplayAreaFallback.Nearest
            );
            return displayArea.WorkArea;
        }
        catch (Exception)
        {
#if __MACOS__
            // Fallback to NSScreen on macOS
            var screen = NSScreen.MainScreen ?? NSScreen.Screens.FirstOrDefault();
            if (screen != null)
            {
                var visibleFrame = screen.VisibleFrame;
                return new RectInt32
                {
                    X = (int)visibleFrame.X,
                    Y = (int)visibleFrame.Y,
                    Width = (int)visibleFrame.Width,
                    Height = (int)visibleFrame.Height
                };
            }
#endif

            // Ultimate fallback: assume 1920x1080 with 40px taskbar
            return new RectInt32 { X = 0, Y = 0, Width = 1920, Height = 1040 };
        }
    }

    public void SetWindowSize(Window window, int widthDip, int heightDip)
    {
        var appWindow = GetAppWindow(window);
        var wa = GetWorkAreaSafe(window);

        double s = GetDpiScale(window);
        int w = (int)Math.Round(widthDip * s);
        int h = (int)Math.Round(heightDip * s);

        int minW = (int)Math.Round(1000 * s);
        int minH = (int)Math.Round(700 * s);

        w = Math.Clamp(w, minW, wa.Width);
        h = Math.Clamp(h, minH, wa.Height);

        appWindow.Resize(new SizeInt32 { Width = w, Height = h });
    }
}
```

---

**End of Report**

Generated by: Research Analyst Agent
Date: 2025-10-15
Version: 1.0
Status: Complete
