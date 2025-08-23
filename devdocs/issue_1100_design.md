# Move StoryModel and StoryModelFile to AppState #1100

## Design Phase - Approved Design

### Overview
This issue addresses circular dependencies in StoryCAD by moving StoryModel and StoryModelFile from OutlineViewModel to AppState, using a StoryDocument wrapper pattern.

### Current Architecture Problems
1. **Circular Dependencies**:
   - OutlineViewModel → AutoSaveService → OutlineViewModel
   - OutlineViewModel → BackupService → OutlineViewModel
   - StoryIO → OutlineViewModel (for setting StoryModelFile)

2. **Services Depend on ViewModels**:
   - AutoSaveService needs StoryModel.Changed and SaveFile()
   - BackupService needs StoryModelFile path
   - StoryIO needs to set StoryModelFile after loading

### Approved Design Solution

#### 1. StoryDocument Wrapper Class
Create new file: `StoryCADLib/Models/StoryDocument.cs`
```csharp
public sealed class StoryDocument
{
    public StoryModel Model { get; }
    public string? FilePath { get; set; }          // null for "Untitled"
    public bool IsDirty => Model.Changed;

    public StoryDocument(StoryModel model, string? filePath = null)
        => (Model, FilePath) = (model, filePath);
}
```

#### 2. Update AppState
Modify: `StoryCADLib/Models/AppState.cs`
```csharp
public sealed class AppState
{
    // Existing properties...
    
    // Single document property
    public StoryDocument? CurrentDocument { get; set; }
    
    // Compatibility properties for transition
    public StoryModel? StoryModel => CurrentDocument?.Model;
    public string? StoryModelFile => CurrentDocument?.FilePath;
    public bool StoryChanged => CurrentDocument?.IsDirty ?? false;
    public int StoryElementCount => CurrentDocument?.Model?.StoryElements?.Count ?? 0;
}
```

#### 3. Add ISaveable Interface and CurrentSaveable to AppState
**Create ISaveable interface** (`Services/ISaveable.cs`):
```csharp
public interface ISaveable
{
    void SaveModel(); // Existing method to copy ViewModel data to Model
}
```

**AppState** additions:
```csharp
public ISaveable? CurrentSaveable { get; set; }  // Currently active saveable ViewModel
```

#### 4. Create EditFlushService
**EditFlushService** (`Services/EditFlushService.cs`):
```csharp
public interface IEditFlushService
{
    void FlushCurrentPageEdits();
}

public sealed class EditFlushService : IEditFlushService
{
    private readonly AppState _appState;
    private readonly ILogService _logger;
    
    public EditFlushService(AppState appState, ILogService logger)
    {
        _appState = appState;
        _logger = logger;
    }

    public void FlushCurrentPageEdits()
    {
        try 
        { 
            _appState.CurrentSaveable?.SaveModel();  // Unconditionally flush
        }
        catch (Exception ex) 
        { 
            _logger.Log(LogLevel.Error, $"Flush failed: {ex.Message}"); 
        }
    }
}
```
Simple service with no ViewModel type dependencies or switch statements.

#### 5. Service Updates
**AutoSaveService** (`Services/Backup/AutoSaveService.cs`):
- Remove OutlineViewModel dependency
- Use AppState to check CurrentDocument?.IsDirty
- Call EditFlushService.FlushCurrentPageEdits() then OutlineService.WriteModel()
```csharp
if (_appState.CurrentDocument?.IsDirty ?? false)
{
    _editFlushService.FlushCurrentPageEdits();
    await _outlineService.WriteModel(
        _appState.CurrentDocument.Model,
        _appState.CurrentDocument.FilePath);
}
```

**BackupService** (`Services/Backup/BackupService.cs`):
- Remove OutlineViewModel dependency  
- Use AppState.CurrentDocument?.FilePath

**StoryIO** (`DAL/StoryIO.cs`):
- Remove OutlineViewModel dependency
- Update AppState.CurrentDocument.FilePath instead of OutlineViewModel.StoryModelFile

#### 6. ViewModel Updates
**Element ViewModels** (Character, Problem, Scene, Folder, Setting, Overview, Web):
- Implement ISaveable interface (they already have SaveModel() method)
```csharp
public class CharacterViewModel : ObservableRecipient, ISaveable
{
    public void SaveModel() { /* existing implementation */ }
}
```

**ShellViewModel** (`ViewModels/ShellViewModel.cs`):
- Remove SaveModel() method entirely (replaced by EditFlushService)
- Remove CurrentPageType property (not needed - Windowing.PageKey already exists)

**OutlineViewModel** (`ViewModels/SubViewModels/OutlineViewModel.cs`):
- Remove StoryModel and StoryModelFile properties completely (no proxies!)
- SaveFile() method updated to use EditFlushService and AppState:
```csharp
public async Task SaveFile(bool autoSave = false)
{
    // Use EditFlushService to flush edits
    _editFlushService.FlushCurrentPageEdits();
    
    // Write to disk
    await _outlineService.WriteModel(
        _appState.CurrentDocument.Model,
        _appState.CurrentDocument.FilePath);
    
    // UI updates, status messages, etc.
}
```
- Let compiler errors guide us to fix all code that was using OutlineViewModel.StoryModel/StoryModelFile

#### 7. Page Registration
**Each saveable page** registers its ViewModel during navigation:
```csharp
// In CharacterPage.xaml.cs, ProblemPage.xaml.cs, etc.
protected override void OnNavigatedTo(NavigationEventArgs e)
{
    var appState = Ioc.Default.GetRequiredService<AppState>();
    appState.CurrentSaveable = DataContext as ISaveable; // Will be null for non-saveable pages
    base.OnNavigatedTo(e);
}
```

Pages that don't save (HomePage, TrashCanPage) will set CurrentSaveable to null automatically.

### Key Benefits
1. **Eliminates all circular dependencies**
2. **Maintains stateless OutlineService** - no changes needed
3. **Clean layer separation**: DAL → Models → Services → ViewModels
4. **Single source of truth**: AppState holds document state
5. **Atomic operations**: Document and FilePath always together
6. **Better testability**: Services don't depend on ViewModels

### Architecture After Changes
```
External API                    UI/ViewModels
     ↓                               ↓
SemanticKernelAPI              OutlineViewModel
     ↓                               ↓
     ├──> AppState.CurrentDocument <─┤
     ↓         (StoryDocument)       ↓
     └────> OutlineService <─────────┘
              (Stateless)
                  ↓
              StoryIO
                  ↓
        Updates AppState.CurrentDocument.FilePath
```

### Migration Strategy
1. Keep compatibility properties in AppState initially
2. Update services incrementally
3. Test at each step
4. Remove compatibility properties after full migration

### Risk Mitigation
- StoryDocument is a simple wrapper with minimal logic
- OutlineService remains unchanged (already stateless)
- Use existing messaging patterns
- Extensive testing at each step

---
*Design approved and ready for implementation in Code phase*

## Code Phase - Completed

### Implementation Order (All Completed)
1. ✅ **Create ISaveable interface in StoryCADLib/Services/**
2. ✅ **Create StoryDocument class in StoryCADLib/Models/**
3. ✅ **Add CurrentDocument and CurrentSaveable to AppState**
4. ✅ **Create EditFlushService in StoryCADLib/Services/**
5. ✅ **Implement ISaveable in element ViewModels (Character, Problem, Scene, etc.)**
6. ✅ **Update Pages to register CurrentSaveable in OnNavigatedTo**
7. ✅ **Update AutoSaveService to use EditFlushService and OutlineService**
8. ✅ **Update BackupService to use AppState**
9. ✅ **Update StoryIO to use AppState instead of OutlineViewModel**
10. ✅ **Remove StoryModel and StoryModelFile from OutlineViewModel**
11. ✅ **Remove SaveModel() and CurrentPageType from ShellViewModel**
12. ✅ **Fix all remaining OutlineViewModel.StoryModel/StoryModelFile usages**
13. ✅ **Compile and fix all build errors**
14. ✅ **Run existing tests and fix failures**
15. ✅ **Fix UI binding updates with CurrentDocumentChanged event**

### Key Implementation Notes
- ISaveable interface eliminates all switch statements and ViewModel type dependencies
- EditFlushService is a simple service with no knowledge of specific ViewModels
- Pages self-register their saveable ViewModels
- No messaging needed - direct service calls ensure proper sequencing
- Windowing.PageKey already tracks current page (no need for CurrentPageType in AppState)

### UI Binding Update Solution
- Added `CurrentDocumentChanged` event to AppState
- Shell subscribes to this event in constructor
- When CurrentDocument is set, event fires and triggers `Shell.UpdateDocumentBindings()`
- `Bindings.Update()` refreshes all x:Bind bindings including tree views
- This ensures UI displays properly when documents are loaded or created