# DI Cleanup — Batch 4 Checklist (for Issue #1063)

## Batch: Story Element ViewModels
**Services in this batch:** ProblemViewModel, SettingViewModel, CharacterViewModel, FolderViewModel, WebViewModel, OverviewViewModel

### Checkboxes (apply the playbook)
- [ ] Confirm services are registered in `ServiceLocator.cs` (Singletons; no lifetime changes).
- [ ] Search call sites for each service:
  ```bash
  git grep -n "Ioc.Default.GetRequiredService<(ProblemViewModel|SettingViewModel|CharacterViewModel|FolderViewModel|WebViewModel|OverviewViewModel)>"
  git grep -n "Ioc.Default.GetService<(ProblemViewModel|SettingViewModel|CharacterViewModel|FolderViewModel|WebViewModel|OverviewViewModel)>"
  ```
- [ ] Edit every consumer file:
  - **SKIP Page classes (Views/*.xaml.cs)** - these are out of scope
  - Add constructor parameters (prefer interfaces; logging uses `ILogService`).
  - Create `private readonly` fields named with underscore camelCase.
  - Replace **all** `Ioc.Default` calls with the injected fields.
- [ ] Fix construction sites (pass dependencies or resolve via DI), build clean.
- [ ] Tests:
  - [ ] Run unit tests (where present).
  - [ ] Smoke test the app (launch, navigate, verify logs).
- [ ] Grep gates (all must return **no results**):
  ```bash
  git grep -n "Ioc.Default.GetRequiredService<(ProblemViewModel|SettingViewModel|CharacterViewModel|FolderViewModel|WebViewModel|OverviewViewModel)>"
  git grep -n "Ioc.Default.GetService<(ProblemViewModel|SettingViewModel|CharacterViewModel|FolderViewModel|WebViewModel|OverviewViewModel)>"
  ```
- [ ] Commit message:
  ```
  DI: Replace Ioc.Default for ProblemViewModel, SettingViewModel, CharacterViewModel, FolderViewModel, WebViewModel, OverviewViewModel with constructor injection; keep singleton lifetimes
  ```

### Notes
- Field naming: `_camelCase` and `readonly`.
- Consumers of logging must depend on `ILogService` (not `LogService`).
- Page classes (Views/*.xaml.cs) are OUT OF SCOPE per the playbook.

---

## PR Description Template

**Title:** DI Cleanup: Batch 4 — Story Element ViewModels (Issue #1063)

**Summary**
- Apply the IOC → Constructor Injection Playbook to: ProblemViewModel, SettingViewModel, CharacterViewModel, FolderViewModel, WebViewModel, OverviewViewModel
- No lifetime changes (Singletons). No behavior changes.

**Changes**
- Replace all `Ioc.Default` usages for the listed ViewModels with constructor injection.
- Update construction sites accordingly.
- Tests: unit + smoke pass locally.

**Verification**
- Grep gates show zero remaining `Ioc.Default` for batch ViewModels.
- App launches; key flows exercised.

**Link:** #1063

---

## Call Sites Found

### ProblemViewModel
<details>
<summary>Click to expand ProblemViewModel call sites (5 total)</summary>

**GetRequiredService calls (2):**
```
StoryCADLib/ViewModels/ShellViewModel.cs:514
StoryCADLib/ViewModels/Tools/StructureBeatViewModel.cs:23
```

**GetService calls (3):**
```
StoryCAD/Views/ProblemPage.xaml.cs:19 [SKIP - Page class]
StoryCADTests/IocLoaderTests.cs:49
StoryCADTests/ProblemViewModelTests.cs:26
```

</details>

### SettingViewModel
<details>
<summary>Click to expand SettingViewModel call sites (2 total)</summary>

**GetRequiredService calls (1):**
```
StoryCADLib/ViewModels/ShellViewModel.cs:529
```

**GetService calls (1):**
```
StoryCAD/Views/SettingPage.xaml.cs:5 [SKIP - Page class]
```

</details>

### CharacterViewModel
<details>
<summary>Click to expand CharacterViewModel call sites (8 total)</summary>

**GetRequiredService calls (2):**
```
StoryCADLib/ViewModels/ShellViewModel.cs:518
StoryCADTests/CharacterModelTests.cs:20
```

**GetService calls (6):**
```
StoryCAD/Views/CharacterPage.xaml.cs:5 [SKIP - Page class]
StoryCADLib/Controls/RelationshipView.xaml.cs:8 [SKIP - Control/Page class]
StoryCADLib/Controls/RelationshipView.xaml.cs:64 [SKIP - Control/Page class]
StoryCADLib/Models/RelationshipModel.cs:35
StoryCADLib/Services/Dialogs/NewRelationshipPage.xaml.cs:7 [SKIP - Page class]
StoryCADTests/IocLoaderTests.cs:48
```

</details>

### FolderViewModel
<details>
<summary>Click to expand FolderViewModel call sites (3 total)</summary>

**GetRequiredService calls (1):**
```
StoryCADLib/ViewModels/ShellViewModel.cs:525
```

**GetService calls (2):**
```
StoryCAD/Views/FolderPage.xaml.cs:5 [SKIP - Page class]
StoryCADTests/IocLoaderTests.cs:51
```

</details>

### WebViewModel
<details>
<summary>Click to expand WebViewModel call sites (5 total)</summary>

**GetRequiredService calls (4):**
```
StoryCAD/Views/Shell.xaml.cs:76 [SKIP - Page class]
StoryCAD/Views/Shell.xaml.cs:83 [SKIP - Page class]
StoryCAD/Views/WebPage.xaml.cs:9 [SKIP - Page class]
StoryCADLib/ViewModels/ShellViewModel.cs:533
```

**GetService calls (1):**
```
StoryCADTests/IocLoaderTests.cs:50
```

</details>

### OverviewViewModel
<details>
<summary>Click to expand OverviewViewModel call sites (3 total)</summary>

**GetRequiredService calls (0):**
```
(none found)
```

**GetService calls (3):**
```
StoryCAD/Views/OverviewPage.xaml.cs:9 [SKIP - Page class]
StoryCADLib/ViewModels/ShellViewModel.cs:510
StoryCADTests/IocLoaderTests.cs:47
```

</details>

---

## ViewModels Remaining for Future Batches

The following ViewModels also have Ioc.Default calls but are being deferred to future batches:

### Dialog/Tool ViewModels (for a future batch):
- FileOpenVM
- NewProjectViewModel  
- StructureBeatViewModel
- NarrativeToolVM
- PrintReportDialogVM
- PreferencesViewModel
- InitVM
- FeedbackViewModel
- DramaticSituationsViewModel
- FlawViewModel
- KeyQuestionsViewModel
- MasterPlotsViewModel
- StockScenesViewModel
- TopicsViewModel
- TraitsViewModel

### Other ViewModels:
- StoryNodeItem
- ControlsData

These will need their own batch or be included with related services.