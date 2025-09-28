# Issue #962: XAML Shutdown Exception Fix Plan

## Problem Analysis
- **Exception**: 0xc000027b (STATUS_STOWED_EXCEPTION) in Microsoft.UI.Xaml.dll
- **When**: Only occurs when debugging (F5), not in normal execution (Ctrl+F5)
- **Cause**: PrintManager event handlers trying to access XAML resources during app shutdown
- **Root Issue**: Print resources are being cleaned up too late in the shutdown sequence

## Current State
- PrintManager is registered when PrintReportsDialog is created
- No systematic cleanup during app shutdown
- Event handlers remain attached when XAML starts shutting down

## Proposed Solution

### Option 1: Implement IDisposable Pattern (Recommended)
1. Make `PrintReportDialogVM` implement `IDisposable`
2. Add proper cleanup in `Dispose()` method
3. Hook into dialog close/app shutdown to call Dispose

**Implementation Steps:**
```csharp
// In PrintReportDialogVM.WinAppSDK.cs
public partial class PrintReportDialogVM : IDisposable
{
    private bool _disposed;
    
    public void Dispose()
    {
        if (!_disposed)
        {
            UnregisterForPrint();
            Document = null;
            PrintDocSource = null;
            _disposed = true;
        }
    }
}
```

### Option 2: Early Cleanup in Window Closing
1. Hook into Shell window's Closing event
2. Find and dispose all PrintReportDialogVM instances
3. Ensure cleanup happens before XAML shutdown

**Implementation Steps:**
- Add cleanup in Shell.xaml.cs constructor:
```csharp
Windowing.MainWindow.Closing += (_, _) => 
{
    // Clean up any print resources
    CleanupPrintResources();
};
```

### Option 3: Weak Event Pattern
1. Use weak event handlers for PrintManager
2. Allows garbage collection even if not explicitly unregistered
3. More complex but more robust

### Option 4: Dialog Lifecycle Management
1. Track when PrintReportsDialog closes
2. Immediately cleanup print resources on dialog close
3. Don't wait for app shutdown

**Implementation Steps:**
- In PrintReportsDialog.xaml.cs:
```csharp
protected override void OnNavigatedFrom(NavigationEventArgs e)
{
    base.OnNavigatedFrom(e);
    PrintVM?.Dispose();
}
```

## Recommended Approach

**Combine Options 1 & 4:**
1. Implement IDisposable on PrintReportDialogVM
2. Call Dispose when dialog closes
3. Also call Dispose on app shutdown as safety net

This ensures:
- Immediate cleanup when dialog closes (normal case)
- Cleanup on shutdown (edge cases)
- No XAML resources accessed during shutdown

## Testing Plan
1. Implement the fix
2. Test with F5 (debugging) - should not crash
3. Test with Ctrl+F5 (no debugging) - should still work
4. Test all scenarios from manual test script
5. Verify no memory leaks or resource issues

## Risk Assessment
- **Low Risk**: Changes are isolated to print functionality
- **No Breaking Changes**: Maintains existing API
- **Debugging Only**: Issue only affects development, not production