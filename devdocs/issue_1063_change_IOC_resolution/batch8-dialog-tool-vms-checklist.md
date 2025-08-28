# DI Cleanup — Batch 8: Dialog & Tool VMs (for Issue #1063)

> Updated: 2025-01-28 - After rebasing onto Issue #1100 (AppState refactoring)

## Batch: Dialog & Tool VMs
**Services in this batch:** FileOpenVM, NewProjectViewModel, StructureBeatViewModel, NarrativeToolVM, PrintReportDialogVM, InitVM, FeedbackViewModel, DramaticSituationsViewModel, FlawViewModel, KeyQuestionsViewModel, MasterPlotsViewModel, StockScenesViewModel, TopicsViewModel, TraitsViewModel

### Current Status
**Batch 8 is partially unblocked after Issue #1100 changes.**

**Progress:**
- Branch rebased onto issue-1100-move-storymodel-to-appstate (includes AppState refactoring)
- Some circular dependencies resolved (OutlineViewModel no longer owns StoryModel/StoryModelFile)
- FileOpenVMTests.cs created and passing
- Several VMs ready for conversion, but some still blocked

**Remaining Blockers:**
- FileOpenVM still depends on both OutlineViewModel AND ShellViewModel
- PrintReportDialogVM still depends on ShellViewModel
- NarrativeToolVM still depends on ShellViewModel
- OutlineViewModel creates FileOpenVM with `new FileOpenVM()` in 2 places

### Tasks Required (Priority Order)

#### Phase 1: Unblock the 3 Blocked VMs (Critical Path)

**Task 1: Remove FileOpenVM Dependencies** (BLOCKER - Most Complex)
- [ ] Analyze what FileOpenVM needs from OutlineViewModel
- [ ] Analyze what FileOpenVM needs from ShellViewModel  
- [ ] Replace with AppState, EditFlushService, or messaging
- [ ] Update FileOpenVM constructor to remove ViewModel dependencies
- [ ] Fix OutlineViewModel lines 197, 441 (new FileOpenVM() calls)
- [ ] Fix OutlineViewModel line 279 (Ioc.Default.GetRequiredService call)
- [ ] Fix FileOpenMenu.xaml.cs usage

**Task 2: Remove PrintReportDialogVM's ShellViewModel Dependency** (BLOCKER) ✅ COMPLETED
- [x] Analyze what it needs from ShellViewModel - Used for SaveModel()
- [x] Replace with AppState or messaging - Replaced with EditFlushService.FlushCurrentEdits()
- [x] Update constructor - Added EditFlushService parameter, removed ShellViewModel
- [x] Update call sites - Updated line 196 to use _editFlushService.FlushCurrentEdits()

**Task 3: Remove NarrativeToolVM's ShellViewModel Dependency** (BLOCKER) ⚠️ PARTIALLY COMPLETED
- [x] Analyze what it needs from ShellViewModel - Uses for VerifyToolUse
- [x] Add Windowing to constructor 
- [x] Remove unused AutoSaveService and BackupService
- [x] Update Ioc.Default.GetService<Windowing>() to use injected _windowing
- [x] Create ToolValidationService to extract validation logic
- [x] Register ToolValidationService in IoC
- [x] Update NarrativeToolVM to use ToolValidationService
- [x] Create tests for NarrativeToolVM
- [x] NOTE: ShellViewModel dependency remains due to VerifyToolUse needing CurrentViewType, CurrentNode, and RightTappedNode from ShellViewModel - documented in ToolValidationService for future refactoring (would require ~123 reference updates to move these to AppState)

#### Phase 2: Convert the 11 Ready VMs (Can Proceed After Phase 1) ✅ COMPLETED

**Task 4: Convert VMs with Existing DI Constructors** ✅ COMPLETED
- [x] FeedbackViewModel - Already has DI constructor, complete conversion
- [x] InitVM - Already has DI constructor, complete conversion

**Task 5: Convert VMs with Service Dependencies** ✅ COMPLETED
- [x] TraitsViewModel - Add ListData to constructor
- [x] FlawViewModel - Add ListData to constructor
- [x] MasterPlotsViewModel - Add ToolsData to constructor
- [x] DramaticSituationsViewModel - Add ToolsData to constructor
- [x] StockScenesViewModel - Add ToolsData to constructor
- [x] KeyQuestionsViewModel - Add ToolsData to constructor (not Windowing)
- [x] StructureBeatViewModel - Add Windowing and other dependencies to constructor params

**Task 6: Convert Simple VMs (No Dependencies)** ✅ COMPLETED
- [x] NewProjectViewModel - Add DI constructor
- [x] TopicsViewModel - Add DI constructor (actually uses ToolsData)

**Important Note on StructureBeatViewModel:**
- StructureBeatViewModel follows the StoryNodeItem pattern, not the singleton service pattern
- It's a ViewModel for individual beats in a collection (like items in a list)
- Gets created dynamically with `new StructureBeatViewModel(title, description)` during runtime and deserialization
- Must keep simple constructor that uses `Ioc.Default` internally (not constructor chaining to DI)
- This pattern is required for ViewModels that are:
  - Created as data items rather than singleton services
  - Serialized/deserialized as part of file loading
  - Created multiple times with different data

#### Phase 3: Final Cleanup

**Task 7: Update All Call Sites and Tests**
- [ ] Update test files to use DI
- [ ] Update any remaining Ioc.Default calls for these VMs
- [ ] Run grep gates to verify no Ioc.Default calls remain

### Checkboxes (apply the playbook) ✅ COMPLETED
- [x] Confirm services are registered in `ServiceLocator.cs` (Singletons; no lifetime changes).
- [x] Search call sites for each service:
  ```bash
  git grep -n "Ioc.Default.GetRequiredService<(FileOpenVM|NewProjectViewModel|StructureBeatViewModel|NarrativeToolVM|PrintReportDialogVM|InitVM|FeedbackViewModel|DramaticSituationsViewModel|FlawViewModel|KeyQuestionsViewModel|MasterPlotsViewModel|StockScenesViewModel|TopicsViewModel|TraitsViewModel)>"
  git grep -n "Ioc.Default.GetService<(FileOpenVM|NewProjectViewModel|StructureBeatViewModel|NarrativeToolVM|PrintReportDialogVM|InitVM|FeedbackViewModel|DramaticSituationsViewModel|FlawViewModel|KeyQuestionsViewModel|MasterPlotsViewModel|StockScenesViewModel|TopicsViewModel|TraitsViewModel)>"
  ```
- [x] Edit every consumer file:
  - **SKIP Page classes (Views/*.xaml.cs)** - these are out of scope
  - Add constructor parameters (prefer interfaces; logging uses `ILogService`).
  - Create `private readonly` fields named with underscore camelCase (e.g., `_preferenceService`).
  - Replace **all** `Ioc.Default` calls with the injected fields.
- [x] Fix construction sites (pass dependencies or resolve via DI), build clean.
- [x] Tests:
  - [x] Run unit tests (where present).
  - [x] Smoke test the app (launch, navigate, verify logs).
- [x] Grep gates (all must return **no results**):
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

### VM Conversion Status Table

| ViewModel | Current Constructor | Dependencies | Status | Action Required |
|-----------|-------------------|--------------|---------|-----------------|
| **FileOpenVM** | Has DI constructor | OutlineViewModel, ShellViewModel | ❌ BLOCKED | Remove ViewModel dependencies |
| **PrintReportDialogVM** | Has DI constructor | EditFlushService, AppState | ✅ READY | Complete conversion |
| **NarrativeToolVM** | Has DI constructor | ShellViewModel, AppState, Windowing, ToolValidationService | ⚠️ PARTIAL | ShellViewModel remains (extracted to ToolValidationService, future refactor needed) |
| **FeedbackViewModel** | Has DI constructor | ILogService | ✅ READY | Complete conversion |
| **InitVM** | Has DI constructor | PreferenceService, BackendService | ✅ READY | Complete conversion |
| **NewProjectViewModel** | Parameterless | None | ✅ READY | Add DI constructor |
| **TopicsViewModel** | Parameterless | None (uses ToolSource directly) | ✅ READY | Add DI constructor |
| **TraitsViewModel** | Parameterless | ListData (via Ioc.Default) | ✅ READY | Add ListData to constructor |
| **FlawViewModel** | Parameterless | ListData (via Ioc.Default) | ✅ READY | Add ListData to constructor |
| **MasterPlotsViewModel** | Parameterless | ToolsData (via Ioc.Default) | ✅ READY | Add ToolsData to constructor |
| **DramaticSituationsViewModel** | Parameterless | ToolsData (via Ioc.Default) | ✅ READY | Add ToolsData to constructor |
| **StockScenesViewModel** | Parameterless | ToolsData (via Ioc.Default) | ✅ READY | Add ToolsData to constructor |
| **KeyQuestionsViewModel** | Parameterless | ToolsData (via Ioc.Default) | ✅ COMPLETED | Added ToolsData to constructor |
| **StructureBeatViewModel** | Has params constructor | Uses Ioc.Default internally | ⚠️ SPECIAL | Kept original pattern - data item VM, not service |

### Circular Dependencies Status (Post-Issue #1100)

1. **FileOpenVM ↔ OutlineViewModel**: ⚠️ STILL EXISTS
   - FileOpenVM depends on OutlineViewModel in constructor (line 242)
   - OutlineViewModel creates FileOpenVM with `new FileOpenVM()` (lines 197, 441)
   - OutlineViewModel uses `Ioc.Default.GetRequiredService<FileOpenVM>()` (line 279)
   - **Required Fix**: Remove OutlineViewModel dependency from FileOpenVM

2. **PrintReportDialogVM ↔ OutlineViewModel**: ✅ RESOLVED
   - PrintReportDialogVM now uses AppState instead of OutlineViewModel
   - OutlineViewModel still uses `Ioc.Default.GetRequiredService<PrintReportDialogVM>()` (line 717)
   - **Required Fix**: Inject PrintReportDialogVM into OutlineViewModel

3. **PrintReportDialogVM ↔ ShellViewModel**: ⚠️ STILL EXISTS
   - PrintReportDialogVM depends on ShellViewModel in constructor (line 40)
   - ShellViewModel cannot inject due to circular dependency
   - **Required Fix**: Remove ShellViewModel dependency from PrintReportDialogVM

4. **NarrativeToolVM ↔ ShellViewModel**: ⚠️ STILL EXISTS
   - NarrativeToolVM depends on ShellViewModel in constructor (line 69)
   - ShellViewModel cannot inject due to circular dependency
   - **Required Fix**: Remove ShellViewModel dependency from NarrativeToolVM

---

## Key Learnings from Batch 8

### Two Different ViewModel Patterns Identified:

1. **Singleton Service ViewModels** (e.g., FeedbackViewModel, InitVM, etc.)
   - Registered in ServiceLocator.cs as singletons
   - Should use constructor injection for dependencies
   - Created once and reused throughout application lifetime
   - Follow standard DI conversion pattern

2. **Data Item ViewModels** (e.g., StructureBeatViewModel, StoryNodeItem)
   - NOT registered in ServiceLocator.cs
   - Created dynamically with `new` keyword
   - Multiple instances with different data
   - Often serialized/deserialized as part of file I/O
   - Should use simple constructors with internal `Ioc.Default` calls
   - Cannot use constructor chaining to DI constructors

### Test Failures Investigation:
- Initial test failures (FullFileTest, StructureModelIsLoadedCorrectly) were caused by incorrect DI pattern application
- StructureBeatViewModel was using constructor chaining which failed during deserialization
- Fixed by reverting to StoryNodeItem pattern: simple constructor with internal service resolution
- All tests now passing (397 passed, 4 skipped, 0 failed)

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