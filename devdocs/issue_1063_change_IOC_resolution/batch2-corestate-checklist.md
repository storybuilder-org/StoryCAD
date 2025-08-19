# DI Cleanup — Batch Checklist Template (for Issue #1063)

> Use this template **as-is** for every batch PR. Claude Code should fill in the placeholders.

## Batch: Core State
**Services in this batch:** AppState, PreferenceService, SerializationLock

### Checkboxes (apply the playbook)
- [ ] Confirm services are registered in `ServiceLocator.cs` (Singletons; no lifetime changes).
- [ ] Search call sites for each service:
  ```bash
  git grep -n "Ioc.Default.GetRequiredService<(AppState|PreferenceService|SerializationLock)>"
  git grep -n "Ioc.Default.GetService<(AppState|PreferenceService|SerializationLock)>"
  ```
- [ ] Edit every consumer file:
  - **SKIP Page classes (Views/*.xaml.cs)** - these are out of scope
  - Add constructor parameters (prefer interfaces; logging uses `ILogService`).
  - Create `private readonly` fields named with underscore camelCase (e.g., `_preferenceService`).
  - Replace **all** `Ioc.Default` calls with the injected fields.
- [ ] Fix construction sites (pass dependencies or resolve via DI), build clean.
- [x] Tests:
  - [x] Run unit tests (where present). ✅ 340 passed, 5 skipped, 0 failed
  - [x] Smoke test the app (launch, navigate, verify logs). ✅ PASSED
- [ ] Grep gates (all must return **no results**):
  ```bash
  git grep -n "Ioc.Default.GetRequiredService<(AppState|PreferenceService|SerializationLock)>"
  git grep -n "Ioc.Default.GetService<(AppState|PreferenceService|SerializationLock)>"
  ```
- [ ] Commit message:
  ```
  DI: Replace Ioc.Default for AppState, PreferenceService, SerializationLock with constructor injection; keep singleton lifetimes
  ```

### Notes
- Field naming: `_camelCase` and `readonly`.
- Consumers of logging must depend on `ILogService` (not `LogService`).
- `SerializationLock` logic bug is out of scope (separate issue).

---

## PR Description Template

**Title:** DI Cleanup: Batch 2 — Core State (Issue #1063)

**Summary**
- Apply the IOC → Constructor Injection Playbook to: AppState, PreferenceService, SerializationLock
- No lifetime changes (Singletons). No behavior changes.

**Changes**
- Replace all `Ioc.Default` usages for the listed services with constructor injection.
- Update construction sites accordingly.
- Tests: unit + smoke pass locally.

**Verification**
- Grep gates show zero remaining `Ioc.Default` for batch services.
- App launches; key flows exercised.

**Link:** #1063

---

## Call Sites Found

### AppState
<details>
<summary>Click to expand AppState call sites (44 total)</summary>

**GetRequiredService calls (34):**
```
StoryCAD/App.xaml.cs:66
StoryCAD/App.xaml.cs:139
StoryCAD/Views/Shell.xaml.cs:88 [SKIP - Page class]
StoryCADLib/DAL/PreferencesIO.cs:54
StoryCADLib/DAL/StoryIO.cs:34
StoryCADLib/Models/Windowing.cs:119, 124, 177
StoryCADLib/Services/Backup/AutoSaveService.cs:19
StoryCADLib/Services/Backup/BackupService.cs:203, 229
StoryCADLib/Services/Collaborator/CollaboratorService.cs:21
StoryCADLib/Services/Dialogs/Changelog.cs:13
StoryCADLib/Services/Dialogs/Tools/PreferencesDialog.xaml.cs:15, 47 [SKIP - Page class]
StoryCADLib/Services/IoC/ServiceLocator.cs:55
StoryCADLib/Services/Locking/SerializationLock.cs:49
StoryCADLib/Services/Logging/LogService.cs:24, 35, 123, 248
StoryCADLib/ViewModels/ShellViewModel.cs:293, 1157
StoryCADLib/ViewModels/SubViewModels/OutlineViewModel.cs:391, 473, 493, 772, 832, 902, 1057
StoryCADLib/ViewModels/WebViewModel.cs:16
StoryCADTests/App.xaml.cs:30, 44
StoryCADTests/PreferenceIOTests.cs:39, 70, 113
```

**GetService calls (10):**
```
StoryCAD/App.xaml.cs:211
StoryCAD/Views/Shell.xaml.cs:57, 70 [SKIP - Page class]
StoryCADLib/DAL/PreferencesIO.cs:20
StoryCADLib/Services/Backend/BackendService.cs:44
StoryCADLib/Services/Ratings/RatingService.cs:10
```

</details>

### PreferenceService
<details>
<summary>Click to expand PreferenceService call sites (36 total)</summary>

**GetRequiredService calls (25):**
```
StoryCAD/App.xaml.cs:126
StoryCAD/Views/Shell.xaml.cs:30 [SKIP - Page class]
StoryCADLib/DAL/PreferencesIO.cs:46
StoryCADLib/Models/WebModel.cs:20
StoryCADLib/Services/Backup/AutoSaveService.cs:20
StoryCADLib/Services/Collaborator/CollaboratorService.cs:436, 441
StoryCADLib/Services/Dialogs/BackupNow.xaml.cs:19 [SKIP - Page class]
StoryCADLib/Services/Dialogs/FileOpenMenu.xaml.cs:22, 44 [SKIP - Page class]
StoryCADLib/Services/IoC/ServiceLocator.cs:59
StoryCADLib/Services/Locking/SerializationLock.cs:88, 92
StoryCADLib/Services/Logging/LogService.cs:42, 247
StoryCADLib/ViewModels/FileOpenVM.cs:234
StoryCADLib/ViewModels/NewProjectViewModel.cs:26
StoryCADLib/ViewModels/ShellViewModel.cs:1159
StoryCADLib/ViewModels/StoryNodeItem.cs:215
StoryCADLib/ViewModels/SubViewModels/OutlineViewModel.cs:1347
StoryCADLib/ViewModels/WebViewModel.cs:278
StoryCADTests/IocLoaderTests.cs:29
StoryCADTests/LockTests.cs:30
StoryCADTests/PreferenceIOTests.cs:98, 121
```

**GetService calls (11):**
```
StoryCAD/App.xaml.cs:210
StoryCAD/Views/Shell.xaml.cs:90 [SKIP - Page class]
StoryCADLib/Services/Backend/BackendService.cs:45
StoryCADLib/Services/Backup/BackupService.cs:17
StoryCADLib/Services/Dialogs/HelpPage.xaml.cs:15 [SKIP - Page class]
StoryCADLib/Services/Ratings/RatingService.cs:11
StoryCADLib/ViewModels/FileOpenVM.cs:24
StoryCADLib/ViewModels/Tools/FeedbackViewModel.cs:147
StoryCADLib/ViewModels/Tools/InitVM.cs:15
StoryCADLib/ViewModels/Tools/PreferencesViewModel.cs:279
StoryCADTests/BackendServiceTests.cs:24
```

</details>

### SerializationLock
<details>
<summary>Click to expand SerializationLock call sites (3 total in SerializationLock.cs)</summary>

**Note:** SerializationLock is not a registered service, but it contains Ioc.Default calls internally:
```
StoryCADLib/Services/Locking/SerializationLock.cs:49 - GetRequiredService<AppState>
StoryCADLib/Services/Locking/SerializationLock.cs:88 - GetRequiredService<PreferenceService>
StoryCADLib/Services/Locking/SerializationLock.cs:92 - GetRequiredService<PreferenceService>
```

</details>