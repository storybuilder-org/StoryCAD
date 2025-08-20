# DI Cleanup — Batch 5 Checklist (Issue #1063)

## Batch: Infrastructure
**Services in this batch:** BackendService, AutoSaveService, BackupService, Windowing (partial)

### Checkboxes (apply the playbook)
- [x] Confirm services are registered in `ServiceLocator.cs` (Singletons; no lifetime changes).
- [x] Search call sites for each service:
  ```bash
  git grep -n "Ioc.Default.GetRequiredService<(BackendService|AutoSaveService|BackupService|Windowing)>"
  git grep -n "Ioc.Default.GetService<(BackendService|AutoSaveService|BackupService|Windowing)>"
  ```

### BackendService Conversion
- [x] Search results identified call sites in:
  - OutlineViewModel
  - InitVM
  - PreferencesViewModel
  - App.xaml.cs (skipped - application entry point)
  
- [x] Converted files:
  - **OutlineViewModel**: Added BackendService via constructor injection
  - **InitVM**: Added BackendService and PreferenceService via constructor injection
  - **PreferencesViewModel**: Added BackendService, PreferenceService, RatingService, and Windowing via constructor injection

### AutoSaveService Conversion
- [x] Removed obsolete parameterless constructor (DI container handles resolution)
- [x] Documented circular dependency with BackupService via SerializationLock (TODO added)
- [x] Service now properly receives dependencies via constructor

### BackupService Conversion  
- [x] Removed obsolete parameterless constructor
- [x] Added Windowing injection for GlobalDispatcher access
- [x] Documented circular dependencies with AutoSaveService and OutlineService (TODOs added)

### Windowing (Partial Conversion)
- [x] Converted CharacterViewModel from service locator to constructor injection
- [x] Added Windowing to BackupService for GlobalDispatcher
- [ ] **DEFERRED**: 33 non-View files still use Windowing via service locator (too extensive for this batch)

### Build and Test Results
- [x] Build: Successful (only nullable warnings)
- [x] Tests:
  - [x] Run unit tests: 340 passed, 5 skipped, 0 failed
  - [x] Smoke test the app: Passed (launch, navigate, verify logs)
  
- [x] Grep gates for fully converted services:
  ```bash
  # BackendService - CLEAN (excluding App.xaml.cs and test files)
  git grep -n "Ioc.Default.GetRequiredService<BackendService>"
  git grep -n "Ioc.Default.GetService<BackendService>"
  
  # AutoSaveService - CLEAN
  git grep -n "Ioc.Default.GetRequiredService<AutoSaveService>"
  git grep -n "Ioc.Default.GetService<AutoSaveService>"
  
  # BackupService - CLEAN
  git grep -n "Ioc.Default.GetRequiredService<BackupService>"
  git grep -n "Ioc.Default.GetService<BackupService>"
  
  # Windowing - PARTIAL (33 files remain)
  git grep -n "Ioc.Default.GetRequiredService<Windowing>"
  git grep -n "Ioc.Default.GetService<Windowing>"
  ```

- [x] Commit message:
  ```
  DI: Replace Ioc.Default for BackendService, partial Windowing, AutoSaveService, BackupService with constructor injection; keep singleton lifetimes
  ```

### Notes
- Field naming: `_camelCase` and `readonly` maintained throughout
- App.xaml.cs unchanged (application entry point requires service locator)
- View files (Views/*.xaml.cs) skipped per playbook
- **Circular Dependencies Documented:**
  - AutoSaveService ↔ BackupService (via SerializationLock)
  - OutlineService ↔ AutoSaveService/BackupService ↔ OutlineViewModel
  - These require architectural refactoring (interface extraction, messaging, etc.) for future work
- **Services Not in Codebase** (removed from original plan):
  - ThemeService - doesn't exist
  - UpdateService - doesn't exist
  - FileService - doesn't exist

---

## PR #1097 Summary

**Title:** DI Cleanup: Batch 5 — Infrastructure (Issue #1063)

**Completed:**
- BackendService fully converted (except App.xaml.cs)
- AutoSaveService fully converted
- BackupService fully converted  
- Windowing partially converted (1 of 34 files)

**Deferred:**
- Remaining 33 Windowing conversions (too extensive, needs separate batch)

**Link:** #1063