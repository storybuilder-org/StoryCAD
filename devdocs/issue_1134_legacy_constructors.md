# StoryCAD Legacy Constructors - DI Migration Cleanup

Generated for issue #1134 - Code cleanup
Analysis date: 2025-10-05

## Summary

**Total Classes with Legacy Constructors: 21**

During the DI (Dependency Injection) migration, classes were updated to use constructor injection. However, **parameterless constructors** using `Ioc.Default.GetRequiredService()` or `Ioc.Default.GetService()` were kept for backward compatibility (primarily for XAML instantiation). These should now be removed.

All legacy constructors are marked with comments like:
- `// Constructor for XAML compatibility - will be removed later`
- `// Constructor for backward compatibility - will be removed later`

---

## ViewModels (17 classes)

### Main ViewModels

#### 1. ShellViewModel
**File**: `StoryCADLib/ViewModels/ShellViewModel.cs:1276-1279`
**Status**: Already commented out (ready for deletion)
```csharp
//// Constructor for XAML compatibility - will be removed later
//public ShellViewModel() : this(
//    Ioc.Default.GetRequiredService<SceneViewModel>(),
//    Ioc.Default.GetRequiredService<ILogService>(),
```

**Action**: Remove commented-out constructor entirely

---

#### 2. CharacterViewModel
**File**: `StoryCADLib/ViewModels/CharacterViewModel.cs:921-924`
```csharp
// Constructor for XAML compatibility - will be removed later
public CharacterViewModel(ILogService logger, AppState appState, Windowing windowing)
{
    _logger = logger;
```

**Note**: This appears to be a 3-parameter constructor marked as legacy. Need to verify if there's a newer DI constructor.

---

#### 3. FileOpenVM
**File**: `StoryCADLib/ViewModels/FileOpenVM.cs:235-241`
```csharp
// Constructor for XAML compatibility - will be removed later
public FileOpenVM() : this(
    Ioc.Default.GetRequiredService<ILogService>(),
    Ioc.Default.GetRequiredService<FileOpenService>(),
    Ioc.Default.GetRequiredService<FileCreateService>(),
    Ioc.Default.GetRequiredService<PreferenceService>(),
    Ioc.Default.GetRequiredService<Windowing>())
```

**Action**: Remove parameterless constructor

---

#### 4. FolderViewModel
**File**: `StoryCADLib/ViewModels/FolderViewModel.cs:136-139`
```csharp
// Constructor for XAML compatibility - will be removed later
public FolderViewModel() : this(
    Ioc.Default.GetRequiredService<ILogService>())
{
```

**Action**: Remove parameterless constructor

---

#### 5. NewProjectViewModel
**File**: `StoryCADLib/ViewModels/NewProjectViewModel.cs:25-28`
```csharp
// Constructor for XAML compatibility - will be removed later
public NewProjectViewModel() : this(Ioc.Default.GetRequiredService<PreferenceService>())
{
}
```

**Action**: Remove parameterless constructor

---

#### 6. OverviewViewModel
**File**: `StoryCADLib/ViewModels/OverviewViewModel.cs:410-413`
```csharp
// Constructor for XAML compatibility - will be removed later
public OverviewViewModel() : this(
    Ioc.Default.GetRequiredService<ILogService>(),
    Ioc.Default.GetRequiredService<AppState>())
```

**Action**: Remove parameterless constructor

---

#### 7. SettingViewModel
**File**: `StoryCADLib/ViewModels/SettingViewModel.cs:257-260`
```csharp
// Constructor for XAML compatibility - will be removed later
public SettingViewModel() : this(
    Ioc.Default.GetRequiredService<ILogService>(),
    Ioc.Default.GetRequiredService<ListData>(),
```

**Action**: Remove parameterless constructor

---

#### 8. WebViewModel
**File**: `StoryCADLib/ViewModels/WebViewModel.cs:328-331`
```csharp
// Constructor for XAML compatibility - will be removed later
public WebViewModel() : this(
    Ioc.Default.GetRequiredService<Windowing>(),
    Ioc.Default.GetRequiredService<AppState>(),
    Ioc.Default.GetRequiredService<ILogService>(),
    Ioc.Default.GetRequiredService<PreferenceService>())
```

**Action**: Remove parameterless constructor

---

### Tool ViewModels (9 classes)

#### 9. DramaticSituationsViewModel
**File**: `StoryCADLib/ViewModels/Tools/DramaticSituationsViewModel.cs:61-64`
```csharp
// Constructor for XAML compatibility - will be removed later
public DramaticSituationsViewModel() : this(Ioc.Default.GetRequiredService<ToolsData>())
{
}
```

**Action**: Remove parameterless constructor

---

#### 10. FeedbackViewModel
**File**: `StoryCADLib/ViewModels/Tools/FeedbackViewModel.cs:15-18`
```csharp
// Constructor for XAML compatibility - will be removed later
public FeedbackViewModel() : this(
    Ioc.Default.GetRequiredService<ILogService>(),
    Ioc.Default.GetRequiredService<PreferenceService>())
```

**Action**: Remove parameterless constructor

---

#### 11. FlawViewModel
**File**: `StoryCADLib/ViewModels/Tools/FlawViewModel.cs:36-39`
```csharp
// Constructor for XAML compatibility - will be removed later
public FlawViewModel() : this(Ioc.Default.GetRequiredService<ListData>())
{
}
```

**Action**: Remove parameterless constructor

---

#### 12. InitVM
**File**: `StoryCADLib/ViewModels/Tools/InitVM.cs:18-21`
```csharp
// Constructor for XAML compatibility - will be removed later
public InitVM() : this(
    Ioc.Default.GetRequiredService<PreferenceService>(),
    Ioc.Default.GetRequiredService<BackendService>())
```

**Action**: Remove parameterless constructor

---

#### 13. KeyQuestionsViewModel
**File**: `StoryCADLib/ViewModels/Tools/KeyQuestionsViewModel.cs:77-80`
```csharp
// Constructor for XAML compatibility - will be removed later
public KeyQuestionsViewModel() : this(Ioc.Default.GetRequiredService<ToolsData>())
{
}
```

**Action**: Remove parameterless constructor

---

#### 14. MasterPlotsViewModel
**File**: `StoryCADLib/ViewModels/Tools/MasterPlotsViewModel.cs:43-46`
```csharp
// Constructor for XAML compatibility - will be removed later
public MasterPlotsViewModel() : this(Ioc.Default.GetRequiredService<ToolsData>())
{
}
```

**Action**: Remove parameterless constructor

---

#### 15. NarrativeToolVM
**File**: `StoryCADLib/ViewModels/Tools/NarrativeToolVM.cs:65-68`
```csharp
// Constructor for XAML compatibility - will be removed later
public NarrativeToolVM() : this(
    Ioc.Default.GetRequiredService<ShellViewModel>(),
    Ioc.Default.GetRequiredService<AppState>(),
```

**Action**: Remove parameterless constructor

---

#### 16. StockScenesViewModel
**File**: `StoryCADLib/ViewModels/Tools/StockScenesViewModel.cs:50-53`
```csharp
// Constructor for XAML compatibility - will be removed later
public StockScenesViewModel() : this(Ioc.Default.GetRequiredService<ToolsData>())
{
}
```

**Action**: Remove parameterless constructor

---

#### 17. TopicsViewModel
**File**: `StoryCADLib/ViewModels/Tools/TopicsViewModel.cs:112-115`
```csharp
// Constructor for XAML compatibility - will be removed later
public TopicsViewModel() : this(Ioc.Default.GetRequiredService<ToolsData>())
{
}
```

**Action**: Remove parameterless constructor

---

#### 18. TraitsViewModel
**File**: `StoryCADLib/ViewModels/Tools/TraitsViewModel.cs:62-65`
```csharp
// Constructor for XAML compatibility - will be removed later
public TraitsViewModel() : this(Ioc.Default.GetRequiredService<ListData>())
{
}
```

**Action**: Remove parameterless constructor

---

### Collaborator ViewModels

#### 19. WorkflowViewModel
**File**: `StoryCADLib/Collaborator/ViewModels/WorkflowViewModel.cs:117-120`
```csharp
// Constructor for XAML compatibility - will be removed later
public WorkflowViewModel() : this(
    Ioc.Default.GetRequiredService<NavigationService>())
{
```

**Action**: Remove parameterless constructor

---

## Models (1 class)

#### 20. Windowing
**File**: `StoryCADLib/Models/Windowing.cs:40-44`
```csharp
// Constructor for backward compatibility - will be removed later
public Windowing() : this(
    Ioc.Default.GetRequiredService<AppState>(),
    Ioc.Default.GetRequiredService<ILogService>())
{
}
```

**Action**: Remove parameterless constructor

**Note**: This is a Model class, not a ViewModel, so XAML binding shouldn't apply here.

---

## Data Access Layer (1 class)

#### 21. PreferencesIO
**File**: `StoryCADLib/DAL/PreferencesIO.cs:37-40`
```csharp
// Constructor for backward compatibility - will be removed later
public PreferencesIo() : this(
    Ioc.Default.GetService<ILogService>(),
    Ioc.Default.GetService<AppState>(),
```

**Action**: Remove parameterless constructor

**Note**: DAL class should not be instantiated by XAML.

---

## Removal Strategy

### Prerequisites

Before removing these constructors, verify:

1. **XAML Files**: Ensure no XAML files instantiate these ViewModels directly
   - Search for `<local:ViewModelName>` in all .xaml files
   - Most ViewModels should be obtained via `Ioc.Default.GetService<T>()` in code-behind

2. **Code-Behind**: Check all .xaml.cs files
   - Look for `new ViewModelName()` without parameters
   - Replace with DI-based retrieval

3. **Tests**: Update test code to use DI constructors
   - Tests should inject mock/real dependencies explicitly

### Recommended Approach

**Phase 1: Investigation** (Before Removal)
1. Search for direct instantiation: `grep -r "new ClassName()" StoryCAD/`
2. Check XAML bindings: `grep -r "x:Name.*ViewModel" --include="*.xaml" StoryCAD/`
3. Review code-behind property initializers using `Ioc.Default.GetService<T>()`

**Phase 2: Safe Removal** (Per Class)
1. Remove the parameterless/legacy constructor
2. Build the project
3. Fix any compilation errors (direct instantiations)
4. Run tests
5. Fix any runtime errors

**Phase 3: Cleanup**
1. Remove any remaining `Ioc.Default.GetService<T>()` calls in ViewModels
2. Ensure all dependencies flow through DI constructors
3. Update documentation

---

## Special Cases

### CharacterViewModel (Line 921)

This has a **3-parameter constructor** marked as legacy:
```csharp
public CharacterViewModel(ILogService logger, AppState appState, Windowing windowing)
```

**Investigation needed**:
- Check if there's a newer constructor with more/different parameters
- Determine which constructor is the "real" DI constructor
- The 3-parameter version may be the intermediate migration step

### ShellViewModel (Line 1276)

Already commented out - just needs final deletion.

---

## Benefits of Removal

1. **Cleaner Code**: Eliminates service locator anti-pattern
2. **Testability**: Forces explicit dependency declaration
3. **Maintainability**: Single constructor per class (SRP)
4. **Performance**: Removes unnecessary service locator calls
5. **Architecture**: Enforces proper DI throughout the application

---

## Risks & Mitigation

### Risk: Breaking XAML Bindings

**Mitigation**:
- XAML should not directly instantiate ViewModels
- ViewModels should be provided via DataContext from code-behind or DI
- Review all .xaml files before making changes

### Risk: Breaking View Code-Behind

**Mitigation**:
- Search for `new ViewModelName()` patterns
- Replace with `Ioc.Default.GetRequiredService<ViewModelName>()`
- Or better: inject ViewModels into Views via DI

### Risk: Breaking Tests

**Mitigation**:
- Update test constructors to use explicit dependency injection
- This actually improves test quality by making dependencies explicit

---

## Verification Commands

After removal, run these commands to verify cleanup:

```bash
# Find any remaining service locator calls in ViewModels
grep -r "Ioc.Default.Get" StoryCADLib/ViewModels/

# Find any remaining service locator calls in Models
grep -r "Ioc.Default.Get" StoryCADLib/Models/

# Find any remaining service locator calls in DAL
grep -r "Ioc.Default.Get" StoryCADLib/DAL/

# Search for parameterless constructor usage
grep -r "new.*ViewModel()" StoryCAD/ --include="*.cs"
```

---

## Related TODOs

From `issue_1134_TODO_list.md`, these constructors relate to:

- **Windowing.cs:159**: Circular dependency - OutlineViewModel ↔ Windowing
- **Windowing.cs:188**: Architectural issue with UpdateUIToTheme

Removing these legacy constructors is part of breaking these circular dependencies.

---

## Summary Table

| Category | Count | Files |
|----------|-------|-------|
| Main ViewModels | 8 | CharacterViewModel, FileOpenVM, FolderViewModel, NewProjectViewModel, OverviewViewModel, SettingViewModel, ShellViewModel, WebViewModel |
| Tool ViewModels | 9 | DramaticSituationsViewModel, FeedbackViewModel, FlawViewModel, InitVM, KeyQuestionsViewModel, MasterPlotsViewModel, NarrativeToolVM, StockScenesViewModel, TopicsViewModel, TraitsViewModel |
| Collaborator ViewModels | 1 | WorkflowViewModel |
| Models | 1 | Windowing |
| Data Access Layer | 1 | PreferencesIO |
| **Total** | **21** | |

---

## Next Steps

1. Create a branch: `issue-1134-remove-legacy-constructors`
2. Work through classes systematically
3. Build and test after each removal
4. Document any XAML or code-behind changes required
5. Update tests to use explicit DI
6. Submit PR with full test suite passing
