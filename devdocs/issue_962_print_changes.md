# Jake Shaw's Reporting Changes for UNO Support

## Summary
Jake has implemented a dual-mode reporting system that maintains Windows printing functionality while adding PDF export for UNO platforms. This allows StoryCAD to provide report generation across all platforms while respecting platform-specific capabilities.

## Key Changes

### Comparison of Printing Code Before/After Jake's Changes:

**Before (August 2025):**
```csharp
private async void StartPrintMenu()
{
    GeneratePrintDocumentReport();
    PrintDocSource = Document.DocumentSource;

    // Old windows build check (pre 19045 doesn't support PrintManager)
    if (Environment.OSVersion.Version.Build <= 19045)
    {
        // Show error for old Windows
    }
    if (PrintManager.IsSupported())
    {
        try
        {
            // Direct call - no UI thread wrapper
            await PrintManagerInterop.ShowPrintUIForWindowAsync(Window.WindowHandle);
        }
        catch { /* handling */ }
    }
}
```

**After (Jake's changes, September 2025):**
```csharp
private async void StartPrintMenu()
{
    if (_isPrinting) return;  // Added guard
    _isPrinting = true;
    _printTaskCreated = false;

    try
    {
        await GeneratePrintDocumentReportAsync().ConfigureAwait(false);


        // Show message for non-Windows platforms
#endif
    }
    finally
    {
        _isPrinting = false;
    }
}
```

**Key changes made:**
- Changed from runtime OS version check to compile-time conditional compilation
- Added `RunOnUIAsync` wrapper to ensure UI thread execution
- Added `_isPrinting` guard to prevent re-entrance
- Better error handling with detailed messages
- Clear separation between Windows and non-Windows behavior

**Critical Question:**
If the old code worked in WinAppSDK without `RunOnUIAsync`, why is this wrapper needed now? The original code called `PrintManagerInterop.ShowPrintUIForWindowAsync()` directly and it worked. This suggests:
1. The `RunOnUIAsync` wrapper might be **unnecessary** for WinAppSDK
2. It might actually be **causing problems** by adding unnecessary threading complexity
3. The printing APIs in WinAppSDK should already handle their own threading requirements

**Update - False Alarm on Startup Hang:**
The startup hang was likely due to StoryCAD not being set as the startup project in Visual Studio, not a DI container deadlock. When the wrong project is set as startup, it can appear to hang.

**The Simplicity Principle:**
"The best code is the simplest code - or no code."

Since printing worked before with a direct call to `PrintManagerInterop.ShowPrintUIForWindowAsync()`, the `RunOnUIAsync` wrapper is:
- Unnecessary complexity
- Potentially the cause of the printing failure
- Violating YAGNI (You Aren't Gonna Need It)

**Recommended Fix:**
Remove the `RunOnUIAsync` wrapper and return to the simpler direct call that was already working, keeping only the compile-time conditional for platform detection.

### 1. PrintReportDialogVM.cs - Conditional Compilation for Printing

#### Windows-Specific Printing (Lines 163-193)
```csharp
am I to cancel/dropdown my CC plan?
#else
    // Show message that printing is only available on Windows
#endif
```

#### Platform Detection Points
- **Lines 211-218**: RegisterForPrint() - Only initializes PrintManager on Windows
- **Lines 282-288**: UnregisterForPrint() - Conditional cleanup
- **Line 491**: PrintTaskRequested handler - Windows-only functionality

### 2. New Report Output Modes

```csharp
public enum ReportOutputMode
{
    Print,  // Traditional Windows printing via PrintManager
    Pdf     // Cross-platform PDF export via SkiaSharp
}
```

### 3. PDF Export Implementation (Lines 348-457)

#### Key Features:
- **SkiaSharp Integration**: Uses SKDocument for PDF creation
- **Configurable Layout**:
  - Page dimensions: 612x792 points (standard US Letter)
  - Margins: 90pt left, 38pt top/bottom
  - Font size: 10pt
- **Dynamic Pagination**: Calculates lines per page based on font metrics
- **Memory-Based**: No temporary files, direct memory stream to file

#### PDF Generation Process:
1. Create SKDocument with memory stream
2. Configure SKPaint with font settings
3. Calculate lines per page from font metrics
4. Build report pages with proper line breaks
5. Draw text to canvas for each page
6. Save memory stream to user-selected file

### 4. UI Changes

#### PrintReportsDialog.xaml
- UI remains unchanged - same checkboxes and lists for report selection
- Dialog behavior adapts based on ReportOutputMode parameter

#### PrintReportsDialog.xaml.cs
- Line 18: `PrintVM.RegisterForPrint()` called during initialization
- Platform detection handled internally by PrintReportDialogVM

### 5. Platform-Specific Behavior

#### On Windows (WinAppSDK head):
- PrintManager functions exactly as before
- Native Windows print dialog with preview
- Full printer selection and settings
- Traditional printing workflow preserved

#### On UNO Platforms:
- Printing shows informational message: "Printing is only available on the Windows head"
- PDF export available as alternative
- Same report generation engine
- File save picker for PDF output

## Architecture Benefits

1. **Clean Separation**: Platform-specific code isolated with conditional compilation
2. **Shared Logic**: Report generation code remains platform-agnostic
3. **Graceful Degradation**: Clear messaging when features unavailable
4. **Future-Proof**: Easy to add more export formats (RTF, HTML, etc.)

## Technical Implementation Details

### Conditional Compilation Flags Used:
- `#if WINDOWS10_0_18362_0_OR_GREATER` - Windows 10 version 1903+
- `#if HAS_UNO` - UNO platform detection
- `#if WINDOWS` - General Windows platform

### Dependencies Added:
- **SkiaSharp**: Cross-platform 2D graphics library for PDF generation
- Already present: Windows.Graphics.Printing (Windows-only)

### Report Generation Flow:
1. User opens Generate Reports dialog
2. Selects report elements to include
3. Chooses output mode (implicitly based on platform)
4. Report content generated via `PrintReports.Generate()`
5. Output routed to appropriate handler:
   - Windows → PrintManager → Printer
   - UNO → SkiaSharp → PDF file

## Testing Considerations

### Windows Platform:
- Verify PrintManager initialization
- Test print preview generation
- Validate printer selection
- Confirm print job completion

### UNO Platforms:
- Verify PDF generation without errors
- Test file save picker integration
- Validate PDF content formatting
- Check memory management (streams disposed properly)

## Future Enhancements Possible

1. **Additional Export Formats**:
   - RTF for word processors
   - HTML for web viewing
   - EPUB for e-readers

2. **Enhanced PDF Features**:
   - Custom fonts
   - Headers/footers with page numbers
   - Table of contents
   - Embedded images

3. **Cloud Integration**:
   - Direct export to cloud storage
   - Email report functionality

## Files Modified

1. **PrintReportDialogVM.cs** - Main implementation
2. **PrintReportsDialog.xaml** - Minor updates
3. **PrintReportsDialog.xaml.cs** - RegisterForPrint call
4. **ReportFormatter.cs** - No significant changes
5. **Directory.Packages.props** - SkiaSharp package reference added
6. **StoryCADLib.csproj** - Project file updates

## Discovery Date
- Analysis performed: 2025-09-23
- Branch: UNOTestBranch
- Commits analyzed: e397b151 (latest after pull)

---

## Startup Hang Issue Analysis

### Problem Description
Application hangs during startup in `WaitHandle.cs` at line 139:
```csharp
num = WaitOneCore(waitHandle.DangerousGetHandle(), millisecondsTimeout, useTrivialWaits);
```

### Root Cause: Dependency Injection Deadlock

The hang is caused by a service locator anti-pattern in multiple singleton services that call `Ioc.Default.GetRequiredService<>()` inside their constructors.

### Problematic Services

1. **PrintReportDialogVM** (lines 57-63)
   ```csharp
   public PrintReportDialogVM() : this(
       Ioc.Default.GetRequiredService<AppState>(),
       Ioc.Default.GetRequiredService<Windowing>(),
       Ioc.Default.GetRequiredService<EditFlushService>(),
       Ioc.Default.GetRequiredService<ILogService>())
   {
   }
   ```

2. **Windowing** (lines 41-45)
   ```csharp
   public Windowing() : this(
       Ioc.Default.GetRequiredService<AppState>(),
       Ioc.Default.GetRequiredService<ILogService>())
   {
   }
   ```

3. **EditFlushService** (line 19)
   ```csharp
   public EditFlushService(AppState appState)
   {
       _appState = appState;
       _logger = Ioc.Default.GetRequiredService<ILogService>();  // <-- Problem
   }
   ```

### Why This Causes a Deadlock

1. All three services are registered as **singletons** in ServiceLocator.cs
2. When IoC container tries to create any of these singletons
3. The parameterless constructor calls `GetRequiredService` for dependencies
4. If those dependencies aren't fully constructed yet, it creates circular resolution
5. The container's internal synchronization locks cause a deadlock in WaitHandle

### Service Registration
From ServiceLocator.cs:
- Line 100: `Services.AddSingleton<Windowing>();`
- Line 126: `Services.AddSingleton<PrintReportDialogVM>();`
- Line 99: `Services.AddSingleton<EditFlushService>();`

### Proposed Solution

**Both parameterless constructors can be safely deleted** as they are not used anywhere:

1. **PrintReportDialogVM** - Remove lines 57-63:
   - No `new PrintReportDialogVM()` calls found in codebase
   - Not instantiated in any XAML files
   - Service is obtained via `Ioc.Default.GetRequiredService<PrintReportDialogVM>()`

2. **EditFlushService** - Fix constructor to use proper DI:
   - Remove the service locator call on line 19
   - Change constructor to:
   ```csharp
   public EditFlushService(AppState appState, ILogService logService)
   {
       _appState = appState;
       _logger = logService;
   }
   ```

3. **Update service registration** in ServiceLocator.cs if needed to ensure ILogService is registered before EditFlushService

This eliminates the recursive service resolution during construction and prevents the deadlock.

### Verification
- ✅ No usage of `new PrintReportDialogVM()` found
- ✅ No usage of `new EditFlushService()` found
- ✅ No XAML instantiation of either class
- ✅ Both services obtained only via IoC container

### Note
The print functionality itself isn't invoked at startup - the deadlock happens during IoC container initialization when creating singleton instances.

### Timeline
- **Before August 2025**: PrintReportDialogVM had no constructor
  - Used field initialization: `private Windowing Window = Ioc.Default.GetRequiredService<Windowing>();`
  - Services were resolved at field initialization time, not in a constructor
- **August 19, 2025** (commit 332aaa49): DI refactoring added constructors
  - Added parameterless constructor with service locator pattern as "XAML compatibility" workaround
  - Added comment: "Constructor for XAML compatibility - will be removed later"
  - Added proper constructor with dependency injection
  - Part of broader DI conversion effort
- **August 23, 2025** (commit 25b56268): EditFlushService created
  - Created with problematic `Ioc.Default.GetRequiredService<ILogService>()` in constructor
- **August 28, 2025** (commit f4d17555): PrintReportDialogVM updated
  - Changed to use EditFlushService dependency
- **September 18, 2025** (commit ef026a02): Jake's UNO changes
  - Changed comment from "Constructor for XAML compatibility - will be removed later" to just "Constructor for XAML compatibility"
  - Removed the "will be removed later" part
  - Added PDF export constants and functionality
  - Added `RunOnUIAsync` helper methods (lines 581-615) to ensure UI thread execution
  - Wrapped PrintManager operations in UI thread execution context
  - Did not change the constructor pattern itself

### The Real Problem
The issue isn't that the parameterless constructor was "added" - it's that moving from field initialization to constructor-based service resolution changed the timing of when services are resolved, creating the potential for deadlock during singleton initialization.



 Somewhere in a UNO change, we used partial classes to separate the WinAppSDK and desktop heads for a method or
  class. Can you find that for me?

● Search(pattern: "partial\s+class", path: "/mnt/d/dev/src/StoryCAD", glob: "*.cs")
  ⎿  Found 44 files (ctrl+o to expand)

● Found them! I can see Windowing.desktop.cs and Windowing.WinAppSDK.cs - these are the platform-specific partial
  classes. Let me check these:

● Read(StoryCADLib/Models/Windowing.desktop.cs)
  ⎿  Read 50 lines (ctrl+o to expand)

● Read(StoryCADLib/Models/Windowing.WinAppSDK.cs)
  ⎿  Read 50 lines (ctrl+o to expand)
  ⎿  Interrupted by user

> Which folder are these classes in?

● The Windowing partial classes are in:

  /mnt/d/dev/src/StoryCAD/StoryCADLib/Models/

  - Windowing.cs - Main partial class
  - Windowing.desktop.cs - Desktop-specific implementation
  - Windowing.WinAppSDK.cs - WinAppSDK-specific implementation

  These use partial classes to separate platform-specific code:
  - Desktop: Treats 1 DIP ≈ 1 pixel (no DPI scaling)
  - WinAppSDK: Uses GetDpiForWindow from User32.dll for proper DPI scaling

  This is a clean pattern for handling platform differences - the same approach could potentially be used for
  PrintReportDialogVM to separate WinAppSDK printing from UNO/Desktop implementations.

> Annotate the markup to refer to https://platform.uno/docs/articles/platform-specific-csharp.html which talks about
> partial classes as an alternative to #if statements: Heavy usage of #if conditionals in shared code makes it hard to
> read and comprehend. A better approach is to use partial class definitions to split shared and platform-specific
> code.
> ⎿  API Error: 500 {"type":"error","error":{"type":"api_error","message":"Overloaded"},"request_id":null}

> Continue
> ⎿  API Error: 500 {"type":"error","error":{"type":"api_error","message":"Overloaded"},"request_id":null}

> /compact
> ⎿  Error: Error during compaction: Error: API Error: 500
> {"type":"error","error":{"type":"api_error","message":"Overloaded"},"request_id":null}

There is also this:
#if WINDOWS10_0_18362_0_OR_GREATER
The above #if statement isn't a UNO platform if block; I think Jake made it.

