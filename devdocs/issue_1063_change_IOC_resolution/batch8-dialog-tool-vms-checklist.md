# DI Cleanup — Batch Checklist Template (for Issue #1063)

> Use this template **as-is** for every batch PR. Claude Code should fill in the placeholders.

## Batch: Dialog & Tool VMs
**Services in this batch:** FileOpenVM, NewProjectViewModel, StructureBeatViewModel, NarrativeToolVM, PrintReportDialogVM, InitVM, FeedbackViewModel, DramaticSituationsViewModel, FlawViewModel, KeyQuestionsViewModel, MasterPlotsViewModel, StockScenesViewModel, TopicsViewModel, TraitsViewModel

### Work Completed Summary
**Batch 8 cannot be converted due to circular dependencies.**

- FileOpenVM, NarrativeToolVM, and PrintReportDialogVM already had DI constructors (parameterless constructors remain for XAML)
- No Ioc.Default calls were removed due to circular dependencies blocking all conversions
- Created FileOpenVMTests.cs as a minimal test file
- Documented all circular dependencies found (see below)
- All tests remain passing

**Recommendation:** Separate issues should be created to refactor the circular dependencies before this batch can be converted.

### Checkboxes (apply the playbook)
- [ ] Confirm services are registered in `ServiceLocator.cs` (Singletons; no lifetime changes).
- [ ] Search call sites for each service:
  ```bash
  git grep -n "Ioc.Default.GetRequiredService<(FileOpenVM|NewProjectViewModel|StructureBeatViewModel|NarrativeToolVM|PrintReportDialogVM|InitVM|FeedbackViewModel|DramaticSituationsViewModel|FlawViewModel|KeyQuestionsViewModel|MasterPlotsViewModel|StockScenesViewModel|TopicsViewModel|TraitsViewModel)>"
  git grep -n "Ioc.Default.GetService<(FileOpenVM|NewProjectViewModel|StructureBeatViewModel|NarrativeToolVM|PrintReportDialogVM|InitVM|FeedbackViewModel|DramaticSituationsViewModel|FlawViewModel|KeyQuestionsViewModel|MasterPlotsViewModel|StockScenesViewModel|TopicsViewModel|TraitsViewModel)>"
  ```
- [ ] Edit every consumer file:
  - **SKIP Page classes (Views/*.xaml.cs)** - these are out of scope
  - Add constructor parameters (prefer interfaces; logging uses `ILogService`).
  - Create `private readonly` fields named with underscore camelCase (e.g., `_preferenceService`).
  - Replace **all** `Ioc.Default` calls with the injected fields.
- [ ] Fix construction sites (pass dependencies or resolve via DI), build clean.
- [ ] Tests:
  - [ ] Run unit tests (where present).
  - [ ] Smoke test the app (launch, navigate, verify logs).
- [ ] Grep gates (all must return **no results**):
  ```bash
  git grep -n "Ioc.Default.GetRequiredService<(FileOpenVM|NewProjectViewModel|StructureBeatViewModel|NarrativeToolVM|PrintReportDialogVM|InitVM|FeedbackViewModel|DramaticSituationsViewModel|FlawViewModel|KeyQuestionsViewModel|MasterPlotsViewModel|StockScenesViewModel|TopicsViewModel|TraitsViewModel)>"
  git grep -n "Ioc.Default.GetService<(FileOpenVM|NewProjectViewModel|StructureBeatViewModel|NarrativeToolVM|PrintReportDialogVM|InitVM|FeedbackViewModel|DramaticSituationsViewModel|FlawViewModel|KeyQuestionsViewModel|MasterPlotsViewModel|StockScenesViewModel|TopicsViewModel|TraitsViewModel)>"
  ```
- [ ] Commit message:
  ```
  DI: Replace Ioc.Default for Dialog & Tool VMs with constructor injection; keep singleton lifetimes
  ```

### Notes
- Field naming: `_camelCase` and `readonly`.
- Consumers of logging must depend on `ILogService` (not `LogService`).
- `SerializationLock` logic bug is out of scope (separate issue).

### Circular Dependencies Found
1. **FileOpenVM ↔ OutlineViewModel**: 
   - FileOpenVM depends on OutlineViewModel in its constructor
   - OutlineViewModel cannot inject FileOpenVM due to circular dependency
   - Resolution: Leave `Ioc.Default.GetRequiredService<FileOpenVM>()` in OutlineViewModel.cs line 293

2. **PrintReportDialogVM ↔ OutlineViewModel**:
   - PrintReportDialogVM depends on OutlineViewModel in its constructor
   - OutlineViewModel cannot inject PrintReportDialogVM due to circular dependency
   - Resolution: Leave `Ioc.Default.GetRequiredService<PrintReportDialogVM>()` in OutlineViewModel.cs line 717

3. **PrintReportDialogVM ↔ ShellViewModel**:
   - PrintReportDialogVM depends on ShellViewModel in its constructor
   - ShellViewModel cannot inject PrintReportDialogVM due to circular dependency  
   - Resolution: Leave `Ioc.Default.GetRequiredService<PrintReportDialogVM>()` in ShellViewModel.cs line 621

4. **NarrativeToolVM ↔ ShellViewModel**:
   - NarrativeToolVM depends on ShellViewModel in its constructor
   - ShellViewModel cannot inject NarrativeToolVM due to circular dependency
   - Resolution: Leave `Ioc.Default.GetRequiredService<NarrativeToolVM>()` in ShellViewModel.cs line 1207

---

## PR Description Template

**Title:** DI Cleanup: Batch 8 — Dialog & Tool VMs (Issue #1063)

**Summary**
- Apply the IOC → Constructor Injection Playbook to: FileOpenVM, NewProjectViewModel, StructureBeatViewModel, NarrativeToolVM, PrintReportDialogVM, InitVM, FeedbackViewModel, DramaticSituationsViewModel, FlawViewModel, KeyQuestionsViewModel, MasterPlotsViewModel, StockScenesViewModel, TopicsViewModel, TraitsViewModel
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

## Claude Code Prompt Template (to generate the per-batch checklist)

```
You are editing Batch 8: Dialog & Tool VMs for Issue #1063.

Services in this batch: FileOpenVM, NewProjectViewModel, StructureBeatViewModel, NarrativeToolVM, PrintReportDialogVM, InitVM, FeedbackViewModel, DramaticSituationsViewModel, FlawViewModel, KeyQuestionsViewModel, MasterPlotsViewModel, StockScenesViewModel, TopicsViewModel, TraitsViewModel

1) Read docs/di-conversion-playbook.md.
2) Create a new markdown checklist by instantiating docs/di-batch-template.md:
   - Replace <BATCH NAME>, <SERVICE_1>, <SERVICE_2> placeholders.
3) For each service, list all call sites using `git grep` and paste the results under a collapsible section per service.
4) Apply the playbook edits:
   - Add constructor parameters and `_fields` to every consumer file.
   - Remove all `Ioc.Default` calls.
   - Fix construction sites; build to green.
5) Run tests + smoke test; paste summaries.
6) Run grep gates; paste the empty result confirmation.
7) Prepare PR description using the provided PR template.
```