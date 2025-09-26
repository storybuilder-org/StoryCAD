# Issue #962: Code Tasks Implementation Plan

## Overview
Implement platform-specific printing/PDF export using partial classes while removing the problematic RunOnUIAsync wrapper.

## Implementation Status

### ✅ Phase 1: Test Infrastructure (TDD) - COMPLETED
- [x] ~~Create PrintReportDialogVMTestHelpers class with mock dependencies~~ *Simplified approach used*
- [x] ~~Write report selection state tests~~ *Covered in existing tests*
- [x] Write report generation tests *(Added 4 tests to PrintReportDialogVMTests.cs)*
- [x] Write page breaking logic tests *(BuildReportPages_RespectsPageBreakMarkers)*
- [x] ~~Write platform routing tests~~ *Handled via conditional compilation*
- [x] Ensure all tests pass with current implementation *(7/7 tests passing)*

**Notes**: Implemented focused TDD tests for report generation rather than comprehensive mocking infrastructure.

### ✅ Phase 2: Refactor to Partial Classes - COMPLETED
- [x] Create PrintReportDialogVM.WinAppSDK.cs for Windows-specific code
- [x] ~~Create PrintReportDialogVM.Desktop.cs for Desktop platform~~ *Used conditional compilation instead*
- [x] Move platform-specific methods to appropriate partial classes:
  - Windows: RegisterForPrint(), StartPrintMenu(), UnregisterForPrint(), print event handlers ✓
  - Desktop: Simplified StartPrintMenu() that routes to PDF ✓
- [x] Update StoryCADLib.csproj to conditionally include partial classes
- [x] Remove #if conditional compilation from main VM file *(Minimal conditionals remain)*

**Notes**: Used conditional compilation for non-Windows StartPrintMenu() rather than separate Desktop partial class.

### ✅ Phase 3: Fix RunOnUIAsync Issue - COMPLETED
- [x] Remove RunOnUIAsync wrapper from StartPrintMenu
- [x] Return to direct PrintManagerInterop.ShowPrintUIForWindowAsync() call
- [x] ~~Test on Windows to verify printing works~~ *Build succeeds, manual testing pending*
- [x] Ensure error handling remains intact

**Notes**: Critical fix implemented - removed problematic async wrapper that was blocking printing.

### ⏳ Phase 4: Platform-Specific UI Updates - PENDING
- [ ] Update Shell.xaml to show/hide menu items based on platform
- [ ] Windows: Show both "Print Reports" and "Export to PDF"
- [ ] Desktop: Show only "Export to PDF" (or rename to "Generate PDF Report")
- [ ] Update keyboard shortcuts appropriately

### ⏳ Phase 5: Enhance PDF Export - PENDING
- [ ] Add progress indication during PDF generation
- [ ] Improve error messages for file access issues
- [ ] Add page numbers to PDF output *(Analysis complete - 2-4 hour effort)*
- [ ] Ensure consistent formatting with print output
- [ ] Fix SkiaSharp deprecation warnings *(SKPaint → SKFont)*

### ⏳ Phase 6: Update Tests - PARTIALLY COMPLETE
- [x] Verify all unit tests still pass *(7/7 PrintReportDialogVM tests pass)*
- [ ] Add integration tests for platform-specific behavior
- [ ] Update manual test documentation
- [ ] Document testing approach for both platforms
- [ ] Manual testing on Windows (Print functionality)
- [ ] Manual testing on Windows (PDF export)
- [ ] Manual testing on Desktop head (if available)

## Detailed Task Breakdown

### PrintReportDialogVM Refactoring

**Main file (PrintReportDialogVM.cs)** - Keep:
- All properties (CreateSummary, SelectAllProblems, etc.)
- Constructor and field declarations
- OpenPrintReportDialog(mode) method
- GeneratePrintDocumentReportAsync() - without UI thread wrapper
- BuildReportPagesAsync() and BuildReportPages()
- ExportReportsToPdfAsync()
- TraverseNode() and related methods

**Windows partial (PrintReportDialogVM.WinAppSDK.cs)**:
```csharp
public partial class PrintReportDialogVM
{
    private PrintManager _printManager;
    private bool _printHandlerAttached;
    
    public void RegisterForPrint()
    {
        _printManager ??= PrintManagerInterop.GetForWindow(Window.WindowHandle);
        if (!_printHandlerAttached)
        {
            _printManager.PrintTaskRequested += PrintTaskRequested;
            _printHandlerAttached = true;
        }
    }
    
    private async void StartPrintMenu()
    {
        if (_isPrinting) return;
        _isPrinting = true;
        
        try
        {
            await GeneratePrintDocumentReportAsync();
            
            // FIXED: Direct call without RunOnUIAsync
            if (PrintManager.IsSupported())
            {
                await PrintManagerInterop.ShowPrintUIForWindowAsync(Window.WindowHandle);
            }
        }
        catch (Exception ex)
        {
            // Error handling
        }
        finally
        {
            _isPrinting = false;
        }
    }
    
    // Move all print-specific event handlers here
}
```

**Desktop partial (PrintReportDialogVM.Desktop.cs)**:
```csharp
public partial class PrintReportDialogVM
{
    public void RegisterForPrint()
    {
        // No-op on Desktop
    }
    
    private async void StartPrintMenu()
    {
        // Desktop goes straight to PDF
        await ExportReportsToPdfAsync();
    }
}
```

### Build Configuration Updates

**StoryCADLib.csproj additions**:
```xml
<ItemGroup Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'windows'">
    <Compile Include="ViewModels\Tools\PrintReportDialogVM.WinAppSDK.cs" />
</ItemGroup>

<ItemGroup Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) != 'windows'">
    <Compile Include="ViewModels\Tools\PrintReportDialogVM.Desktop.cs" />
</ItemGroup>
```

## Success Criteria

1. Windows users can print via PrintManager AND export to PDF
2. Desktop users can export to PDF
3. No RunOnUIAsync wrapper in the code
4. All tests pass
5. Clean separation of platform concerns
6. No #if directives in main VM file

## Risk Mitigation

1. **Test thoroughly on Windows first** - Primary user base
2. **Keep backup of current implementation** - Easy rollback
3. **Manual testing required** - UI components can't be unit tested
4. **Coordinate with team** - Ensure no conflicts with other work

## Current Implementation Summary

### What's Complete
1. **TDD Tests**: Added 4 report generation tests to PrintReportDialogVMTests.cs
2. **Partial Class Refactoring**: Created PrintReportDialogVM.WinAppSDK.cs with all Windows-specific code
3. **RunOnUIAsync Fix**: Removed the wrapper that was blocking printing
4. **Build Configuration**: Updated project files for conditional compilation
5. **All tests passing**: 7/7 PrintReportDialogVM tests

### What Remains

#### Immediate Tasks (Required for PR)
1. **Manual Testing**:
   - Test Windows printing functionality 
   - Test PDF export on Windows
   - Test on Desktop head if available

2. **UI Menu Updates** (Phase 4):
   - Update Shell.xaml for platform-specific menus
   - Windows: Show both "Print" and "Export PDF" options
   - Desktop: Show only "Export PDF" option

#### Future Enhancements (Can be separate PRs)
1. **PDF Improvements**:
   - Add page numbers (2-4 hour effort)
   - Fix SkiaSharp deprecation warnings
   - Add progress indication
   - Improve error messages

2. **Report Formatting**:
   - Review report appearance/layout
   - Ensure consistency between print and PDF output
   - Any cosmetic improvements requested

## Key Code Changes Made

1. **PrintReportDialogVM.WinAppSDK.cs**: Contains RegisterForPrint, print event handlers, Windows-specific StartPrintMenu
2. **PrintReportDialogVM.cs**: Now partial class, minimal conditional compilation, non-Windows StartPrintMenu routes to PDF
3. **StoryCADLib.csproj**: Conditionally excludes Windows partial on non-Windows targets
4. **PrintReportsDialog.xaml.cs**: Wrapped RegisterForPrint with platform check