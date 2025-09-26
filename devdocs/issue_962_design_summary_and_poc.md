# Issue #962: Design Phase Summary & Proof of Concept Plan

## Executive Summary

After comprehensive analysis, the path forward is clear:

1. **Keep Jake's SkiaSharp PDF implementation** - it's viable and working
2. **Remove the RunOnUIAsync wrapper** - return to simple direct PrintManager calls  
3. **Use partial classes** to separate platform-specific code
4. **Maintain full Windows printing** while adding PDF as an option
5. **Provide PDF-only solution** for macOS/Linux users

## Key Findings

### What Works
- ✅ Jake's PDF implementation using SkiaSharp
- ✅ Main branch PrintManager approach (without RunOnUIAsync)
- ✅ Report generation pipeline (PrintReports.Generate())
- ✅ Cross-platform UI components

### What Needs Fixing
- ❌ RunOnUIAsync wrapper causing printing issues
- ❌ Heavy #if conditional compilation  
- ❌ Monolithic PrintReportDialogVM with too many responsibilities
- ❌ Platform detection mixed with business logic

### Platform Capabilities Confirmed
- **Windows**: Full PrintManager support + PDF export
- **macOS/Linux**: PDF export only (no PrintManager available)
- **All platforms**: SkiaSharp works well for PDF generation

## Proof of Concept Plan

### Objective
Validate the partial class approach with minimal changes to prove:
1. Windows printing works without RunOnUIAsync
2. PDF export works on all platforms
3. Partial classes effectively separate concerns

### POC Implementation Steps

#### Step 1: Create Partial Class Structure
```
PrintReportDialogVM.cs              # Core + shared PDF logic
PrintReportDialogVM.WinAppSDK.cs    # Windows PrintManager code
PrintReportDialogVM.Desktop.cs      # Desktop platform code
```

#### Step 2: Move Platform-Specific Code

**PrintReportDialogVM.cs** (Shared):
- All properties and fields
- Constructor and dependencies
- OpenPrintReportDialog method
- ExportReportsToPdfAsync (entire PDF implementation)
- BuildReportPages methods
- Common helpers

**PrintReportDialogVM.WinAppSDK.cs**:
```csharp
public partial class PrintReportDialogVM
{
    private PrintManager _printManager;
    private bool _printHandlerAttached;
    
    partial void PlatformSpecificInit()
    {
        // Windows-specific initialization if needed
    }
    
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
            
            // Direct call - NO RunOnUIAsync wrapper
            if (PrintManager.IsSupported())
            {
                await PrintManagerInterop.ShowPrintUIForWindowAsync(Window.WindowHandle);
            }
        }
        finally
        {
            _isPrinting = false;
        }
    }
    
    // All other Windows-specific methods...
}
```

**PrintReportDialogVM.Desktop.cs**:
```csharp
public partial class PrintReportDialogVM  
{
    partial void PlatformSpecificInit()
    {
        // Desktop-specific initialization if needed
    }
    
    public void RegisterForPrint()
    {
        // No-op on Desktop
    }
    
    private async void StartPrintMenu()
    {
        // On Desktop, go straight to PDF
        await ExportReportsToPdfAsync();
    }
}
```

#### Step 3: Update Build Configuration

**StoryCADLib.csproj**:
```xml
<ItemGroup Condition="'$(TargetFramework)' == 'net9.0-windows10.0.19041.0'">
    <Compile Include="ViewModels\Tools\PrintReportDialogVM.WinAppSDK.cs" />
</ItemGroup>

<ItemGroup Condition="'$(TargetFramework)' != 'net9.0-windows10.0.19041.0'">  
    <Compile Include="ViewModels\Tools\PrintReportDialogVM.Desktop.cs" />
</ItemGroup>
```

#### Step 4: Test Points

1. **Windows Build**:
   - Print command shows print dialog
   - Export PDF command saves PDF
   - No RunOnUIAsync errors
   
2. **Desktop Build**:
   - Only Export option visible
   - PDF generation works
   - No compilation errors

### Success Criteria

- [ ] Windows printing works without RunOnUIAsync wrapper
- [ ] PDF export functions on all platforms
- [ ] No #if directives in main file
- [ ] Clean compilation on both platforms
- [ ] Existing functionality preserved

## Recommended Architecture (Post-POC)

### Short Term (This PR)
Use partial classes with minimal refactoring:
- Quick fix for printing issues
- Platform separation achieved
- Both output methods working

### Long Term (Future PR)
Extract services for clean architecture:
- IReportGenerator service
- IReportOutput implementations  
- Dependency injection
- Better testability

## Implementation Order

1. **Fix Critical Issue**: Remove RunOnUIAsync wrapper
2. **Implement Partials**: Separate platform code
3. **Test Thoroughly**: Ensure no regressions
4. **Update Menus**: Platform-appropriate options
5. **Polish UX**: Error messages, progress indication

## Risk Mitigation

### Risks
1. Breaking existing Windows printing
2. PDF differences across platforms
3. Build configuration complexity

### Mitigations  
1. Extensive testing on Windows first
2. Visual diff testing for PDFs
3. Clear build documentation

## Summary of Deliverables

### Completed Analysis Documents
1. ✅ Current State Analysis - Printing flow comparison
2. ✅ PDF Implementation Assessment - SkiaSharp viability confirmed  
3. ✅ Architecture Decomposition - Shared vs platform-specific mapped
4. ✅ Platform Matrix & Requirements - Complete specifications

### Next Steps
1. Get human approval for POC approach
2. Implement proof of concept
3. Test on both platforms
4. Refine based on findings
5. Proceed with full implementation

## Critical Decisions Made

1. **Technology**: Keep SkiaSharp for PDF (MIT license, proven, cross-platform)
2. **Architecture**: Use partial classes for platform separation
3. **PrintManager**: Revert to direct calls (remove RunOnUIAsync)
4. **Menu Strategy**: Platform-appropriate options (both on Windows, PDF-only on Desktop)
5. **Scope**: Fix immediate issues now, refactor architecture later

## Questions Resolved

- ✅ Where is Jake's PDF code? UNOTestBranch, working but not in production
- ✅ Is SkiaSharp viable? Yes, excellent choice for our needs
- ✅ Keep PrintManager as-is? Yes, just remove the problematic wrapper
- ✅ How to validate parity? Visual diff testing with golden samples

Ready to proceed with implementation upon approval.