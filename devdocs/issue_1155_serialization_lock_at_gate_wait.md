# Issue #1155: SerializationLock Deadlock at Gate.Wait()

## Executive Summary

The application hangs when opening story outlines on the desktop head (Windows) due to a deadlock in the SerializationLock implementation. The issue occurs when synchronous lock acquisition (`Gate.Wait()`) is used on the UI thread while async operations are performed within the lock.

## Problem Description

### Symptoms
- Application hangs when opening a sample outline file
- UI becomes completely unresponsive
- Must force-quit the application

### Root Cause
The SerializationLock class provides two patterns for acquiring locks:
1. **Synchronous pattern**: `using (new SerializationLock())` - calls `Gate.Wait()`
2. **Async pattern**: `await SerializationLock.RunExclusiveAsync()` - calls `await Gate.WaitAsync()`

When the synchronous pattern is used on the UI thread and an async operation (like `ShowFilePicker`) is called within the lock, it creates a classic async deadlock:
- The UI thread blocks at `Gate.Wait()` waiting for the semaphore
- The async operation inside the lock completes and needs to resume on the UI thread
- The UI thread is blocked waiting for the lock to release
- **DEADLOCK** - the lock can't be released until the async operation completes, but it can't complete because the UI thread is blocked

## Stack Trace Analysis

### Stack Trace 1 - Startup (PreferencesIO)
```
SerializationLock.SerializationLock() Line 28
PreferencesIO.ReadPreferences() Line 43  // <-- Synchronous lock
App.OnLaunched() Line 163
[Async continuation blocked on UI thread]
```

### Stack Trace 2 & 3 - UI Updates (OutlineService.SetChanged)
```
SerializationLock.SerializationLock() Line 48 (Gate.Wait())
OutlineService.SetChanged() Line 873  // <-- Synchronous lock
ShellViewModel.ShowChange() Line 580
OverviewViewModel.OnPropertyChanged() Line 311
[Triggered by RichEditBox text changes on UI thread]
```

## Affected Code Locations

### Critical Deadlock Points

#### 1. FileOpenService.OpenFile() - PRIMARY ISSUE
- **Location**: Lines 54-68 and 72-137
- **Problem**: TWO separate synchronous lock blocks
- **Async Operation**: `await _windowing.ShowFilePicker()` at line 84
- **Impact**: HIGH - Directly causes the reported hang

#### 2. OutlineService.SetChanged()
- **Location**: Line 873
- **Problem**: Synchronous lock called from UI property changes
- **Async Operation**: None, but runs on UI thread
- **Impact**: MEDIUM - Can block UI updates

#### 3. PreferencesIO.ReadPreferences()
- **Location**: Line 43
- **Problem**: Synchronous lock during app startup
- **Async Operations**: `StorageFile.GetFileFromPathAsync()`, `FileIO.ReadTextAsync()`
- **Impact**: MEDIUM - Can cause startup hangs

### Other Synchronous Lock Usage in OutlineService
The following methods use synchronous locks but are lower risk as they don't contain async operations:
- `SetCurrentView()` - Line 249
- `GetStoryElementByGuid()` - Line 902
- `UpdateStoryElement()` - Line 933
- `GetCharacterList()` - Line 958
- `GetSettingsList()` - Line 1016
- `GetAllStoryElements()` - Line 1041
- `SearchForText()` - Line 1073
- `SearchForUuidReferences()` - Line 1112
- `RemoveUuidReferences()` - Line 1151
- `SearchInSubtree()` - Line 1201
- `MoveToTrash()` - Line 1344
- `RestoreFromTrash()` - Line 1421
- `EmptyTrash()` - Line 1463

## Steps to Reproduce

1. Build and run StoryCAD on Windows with the desktop head (`net9.0-desktop`)
2. Launch the application
3. Click File → Open (or use the Open toolbar button)
4. Select any `.stbx` sample outline file
5. **Expected**: File opens successfully
6. **Actual**: Application hangs at SerializationLock line 48 (`Gate.Wait()`)

### Alternative Reproduction Path
1. Launch StoryCAD
2. Create a new outline
3. Type text rapidly in any RichEditBox field
4. Observe potential UI freezing as SetChanged() acquires locks

## Proposed Solution

### Fix Strategy

#### Priority 1 - Critical Fixes

**1. FileOpenService.OpenFile()**
Convert from synchronous to async lock pattern:
```csharp
public async Task OpenFile(string fromPath = "")
{
    await SerializationLock.RunExclusiveAsync(async ct =>
    {
        // Entire method body here
        // Including the ShowFilePicker call
    }, CancellationToken.None, _logger);
}
```

**2. OutlineService.SetChanged()**
Option A - Remove lock (recommended for simple property):
```csharp
internal void SetChanged(StoryModel model, bool changed)
{
    if (model == null)
        throw new ArgumentNullException(nameof(model));

    // No lock needed for simple bool assignment
    model.Changed = changed;
    _log.Log(LogLevel.Info, $"Model Changed status set to {changed}");
}
```

Option B - Convert to async:
```csharp
internal async Task SetChangedAsync(StoryModel model, bool changed)
{
    await SerializationLock.RunExclusiveAsync(async ct =>
    {
        model.Changed = changed;
        _log.Log(LogLevel.Info, $"Model Changed status set to {changed}");
    }, CancellationToken.None, _log);
}
```

**3. PreferencesIO.ReadPreferences()**
Convert to async pattern:
```csharp
public async Task<PreferencesModel> ReadPreferencesAsync()
{
    try
    {
        PreferencesModel model = null;
        await SerializationLock.RunExclusiveAsync(async ct =>
        {
            // Existing method body
        }, CancellationToken.None, _log);
        return model;
    }
    catch (Exception e)
    {
        // Error handling
    }
}
```

### Implementation Guidelines

#### When to Use Each Pattern

**Use `RunExclusiveAsync` when:**
- The method contains ANY async operations (`await` calls)
- The method might be called from the UI thread
- The method performs I/O operations (file, network, etc.)

**Use `new SerializationLock()` only when:**
- The method is purely synchronous (no async calls)
- The method is guaranteed to complete quickly (<50ms)
- The method is NOT called from the UI thread
- OR the operation is so simple it doesn't need a lock

**Never:**
- Use synchronous locks on the UI thread with async operations inside
- Mix synchronous and async operations within the same lock
- Hold locks during user interaction (file pickers, dialogs, etc.)

### Code Pattern Examples

#### ✅ Correct: Async operation with async lock
```csharp
await SerializationLock.RunExclusiveAsync(async ct =>
{
    var file = await ShowFilePicker();  // Async operation
    await ProcessFile(file);            // Async operation
}, cancellationToken, logger);
```

#### ✅ Correct: Synchronous operation with sync lock
```csharp
using (new SerializationLock(_logger))
{
    model.SomeProperty = value;  // Simple sync operation
    CalculateSomething();        // Sync calculation
}
```

#### ❌ Wrong: Async operation with sync lock
```csharp
using (new SerializationLock(_logger))  // WRONG!
{
    await SomeAsyncOperation();  // Will deadlock on UI thread!
}
```

## Testing Plan

### Functional Testing
1. **File Operations**
   - Open multiple sample outline files
   - Create new outlines
   - Save and save-as operations
   - Recent files menu

2. **UI Responsiveness**
   - Rapid text entry in RichEditBox fields
   - Quick navigation between story elements
   - Multiple property changes in succession

3. **Startup Performance**
   - Cold start with preferences file
   - Cold start without preferences file
   - Warm restarts

### Performance Testing
1. Add temporary logging to measure lock acquisition times
2. Monitor for lock contention (multiple threads waiting)
3. Check for reentrant locking situations
4. Verify no UI freezes lasting >100ms

### Regression Testing
1. Verify auto-save functionality still works
2. Confirm backup operations complete successfully
3. Test concurrent operations don't corrupt data
4. Ensure file locking prevents multiple access

## Long-term Recommendations

1. **Architecture Review**
   - Consider if all operations truly need serialization
   - Evaluate using reader-writer locks for read operations
   - Consider lock-free approaches for simple property updates

2. **Developer Guidelines**
   - Add XML documentation to SerializationLock clarifying usage patterns
   - Create analyzer rule to detect async operations in sync locks
   - Add debug assertions to detect UI thread blocking

3. **Monitoring**
   - Add telemetry for lock wait times
   - Log warnings when locks are held >100ms
   - Track reentrant lock usage

## Related Issues
- #1116 - UI goes off screen (UNO platform issues)
- #1139 - UNO Platform API compatibility warnings
- #1153 - Fixed null FilePath check in FileOpenService

## References
- [Async/Await Best Practices](https://docs.microsoft.com/en-us/archive/msdn-magazine/2013/march/async-await-best-practices-in-asynchronous-programming)
- [ConfigureAwait FAQ](https://devblogs.microsoft.com/dotnet/configureawait-faq/)
- [Diagnosing .NET Async Deadlocks](https://michaelscodingspot.com/c-debug-deadlocks/)

## Conclusion

This is a critical issue affecting basic file operations. The fix is straightforward - convert methods containing async operations to use the async lock pattern (`RunExclusiveAsync`). Priority should be given to FileOpenService.OpenFile() as it directly causes the reported hang.

The broader lesson is that mixing synchronous and asynchronous patterns in lock acquisition is dangerous, especially on UI threads. Clear guidelines and patterns should be established for the team to prevent similar issues in the future.