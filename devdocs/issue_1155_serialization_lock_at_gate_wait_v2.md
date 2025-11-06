# Issue #1155: SerializationLock Deadlock — TDD Refactor & Implementation Plan (v2)

## 1. Purpose and Context

This document supersedes the previous analysis of issue #1155 and integrates architectural clarifications from recent layering and DI refactors. It defines **a test-driven, architecture-consistent plan** for removing UI-thread deadlocks and replacing dual lock types with a single unified asynchronous serialization gate.

All implementation guidance adheres to **Claude Code–style naming** and emphasizes human readability, clear intent, and maintainability.

---

## 2. Architectural Summary

### 2.1 Layered Architecture Recap

| Layer | Responsibilities | Key Components |
|-------|------------------|----------------|
| **UI/ViewModels** | Handle user input and UI updates; no I/O or serialization logic. | `ShellViewModel`, `OutlineViewModel` |
| **Services** | Perform I/O, persistence, auto-save, and backup logic; expose async APIs; no UI thread blocking. | `OutlineService`, `StoryIO`, `AutoSaveService`, `BackupService` |
| **Infrastructure** | Cross-cutting utilities (locking, windowing, logging, messaging). | `SerializationLock`, `DispatcherQueueExtensions`, `LogService`, `Windowing` |

### 2.2 Current Pain Points

- Multiple **sync locks (`Gate.Wait()`)** held on the UI thread while awaiting async continuations.  
- Two lock patterns (sync and async) introduced ad hoc to mitigate cross-layer reentrancy.  
- **Circular dependencies** emerged between services and viewmodels prior to DI adoption.  
- **ShellViewModel** became a “God object”; **OutlineViewModel** now manages document-scoped logic.  
- Timed background services (`AutoSaveService`, `BackupService`) run concurrently with user actions, risking lock contention.  

---

## 3. Refactor Strategy: “Single Async Gate”

### 3.1 Core Principle

> There must be **exactly one serialization gate**, async and reentrant, managed by the service layer.  
> No code in the UI layer may block the UI thread while holding that gate.

### 3.2 Locking Rules

| Context | Lock Pattern | Rule |
|----------|---------------|------|
| Async I/O or UI-thread continuations | `await SerializationLock.RunExclusiveAsync(...)` | ✅ Always |
| Synchronous background logic (fast, non-UI) | `using (new SerializationLock())` | ✅ Allowed if <50 ms |
| Any code running on UI thread | ❌ Never block synchronously |
| Timer callbacks | ✅ Must use same async gate |

### 3.3 Removal of Lock Levels

All “levels” or “types” of locks are replaced by **one async, reentrant semaphore** (`SemaphoreSlim(1,1)`) managed in `SerializationLock`.  
A DEBUG-only assertion guards against misuse on the UI thread.

---

## 4. Implementation Plan

### 4.1 SerializationLock.cs

**Changes**
- Add `#if DEBUG` guard that throws if constructed on a UI thread.
- Mark sync constructor `[Obsolete("Use RunExclusiveAsync(...)")]`.
- Expose a single static async entry: `RunExclusiveAsync(Func<CancellationToken, Task>, ...)`.
- Marshal `CanExecuteStateChanged` events onto the UI thread via `DispatcherQueueExtensions`.
- Add optional timeout + log warning if wait > 500 ms.

**Unit Tests**
- `SerializationLock_ThrowsOnUIThreadInDebug()`  
- `RunExclusiveAsync_AllowsAsyncContinuationOnUIThread()`  
- `RunExclusiveAsync_TimesOutLogsWarning()`  

---

### 4.2 OutlineService.cs

**Objective:** Ensure all I/O and serialization operations go through `RunExclusiveAsync`.  

**Actions**
- Convert any `using (new SerializationLock())` wrapping async calls to async form.  
- Remove locking from `SetChanged()` (simple flag update).  
- Confirm that `WriteModel()` wraps `StoryIO.WriteStory()` inside `RunExclusiveAsync`.

**Unit Tests**
- `SetChanged_DoesNotBlockUIThread()`  
- `WriteModel_RunsExclusiveWithoutDeadlock()`  

---

### 4.3 StoryIO.cs

**Objective:** Pure async file I/O with no sync blocking.

**Actions**
- Ensure all file reads/writes use `await` only.  
- Verify `ReadStory()` never touches UI elements directly.  
- Add diagnostic logging for serialization version mismatches.

**Unit Tests**
- `ReadStory_HandlesLegacyXMLShowsDialog()` (mock Windowing)  
- `WriteStory_WritesFileAndUpdatesVersion()`  

---

### 4.4 AutoSaveService.cs【48†source】

**Objective:** Auto-save must share the same serialization gate as manual saves.

**Actions**
- Replace `using (new SerializationLock(_logger))` with:
  ```csharp
  await SerializationLock.RunExclusiveAsync(async ct =>
  {
      await _windowing.GlobalDispatcher.EnqueueAsync(() => _editFlushService.FlushCurrentEdits());
      await _outlineService.WriteModel(_appState.CurrentDocument.Model, _appState.CurrentDocument.FilePath);
      WeakReferenceMessenger.Default.Send(new IsChangedMessage(false));
  }, CancellationToken.None, _logger);
  ```
- Ensure the timer callback (`AutoSaveTimer_Elapsed`) awaits `RunExclusiveAsync` fully.  
- Remove `SemaphoreSlim _autoSaveGate`; gate redundancy handled by `SerializationLock`.  

**Unit Tests**
- `AutoSave_DoesNotOverlapConcurrentSaves()`  
- `AutoSave_UsesSharedSerializationGate()`  
- `AutoSave_UIRemainsResponsiveDuringSave()`  

---

### 4.5 BackupService.cs【49†source】

**Objective:** Backup operations must also share the serialization gate.

**Actions**
- Replace internal `SemaphoreSlim _backupGate` + sync lock with single `await SerializationLock.RunExclusiveAsync(...)`.  
- Ensure zip creation and temp-folder cleanup all occur within the async lock.  
- Keep `_windowing.GlobalDispatcher.TryEnqueue()` for UI updates only.

**Unit Tests**
- `Backup_RunsWithoutOverlappingAutoSave()`  
- `Backup_CreatesZipAndCleansUpTempFolder()`  
- `Backup_UIUpdateDispatchedToMainThread()`  

---

### 4.6 OutlineViewModel.cs【52†source】

**Objective:** ViewModel should delegate all file operations to services.

**Actions**
- Replace any direct calls to `SerializationLock` or service internals with async service methods.  
- Ensure commands like *Open*, *Save*, *Save As* simply await OutlineService methods.  
- Validate that property change handlers never acquire locks.

**Unit Tests**
- `OutlineVM_OpenFile_UsesOutlineServiceAsync()`  
- `OutlineVM_SaveCommand_AwaitsCompletion_NoDeadlock()`  

---

### 4.7 ShellViewModel.cs【51†source】

**Objective:** Eliminate all file I/O responsibilities. Limit to global UI orchestration.

**Actions**
- Move file management responsibilities fully into `OutlineViewModel`.  
- Restrict cross-VM communication to `WeakReferenceMessenger`.  
- Ensure Shell never constructs or awaits `SerializationLock`.  

**Unit Tests**
- `ShellVM_DoesNotPerformFileIO()`  
- `ShellVM_DelegatesOpenToOutlineVM()`  

---

## 5. Testing Framework & TDD Sequence

### 5.1 Framework
- **xUnit** for logic tests.  
- **Moq** for Windowing and DispatcherQueue mocks.  
- **Uno.UI.RuntimeTests** for UI-level async validation (where applicable).  

### 5.2 TDD Workflow

1. **Write failing tests** for deadlock cases (UI-block simulation).  
2. **Implement async conversion** in smallest scope (SerializationLock).  
3. **Refactor each service**, one at a time, re-running the test suite.  
4. **Add integration test** for multi-service concurrency (AutoSave + Backup).  
5. **Verify no test exceeds 100 ms main-thread blocking time.**  

---

## 6. Migration Checklist

| Step | Task | Status |
|------|------|--------|
| 1 | Add async-only guard + obsolete sync lock | ☐ |
| 2 | Refactor OutlineService async paths | ☐ |
| 3 | Convert AutoSaveService + BackupService to async-only | ☐ |
| 4 | Remove redundant gates in services | ☐ |
| 5 | Add TDD test suite for locks, saves, backups | ☐ |
| 6 | Verify ShellVM/OutlineVM separation | ☐ |
| 7 | Update developer docs & analyzer rule | ☐ |

---

## 7. Design Philosophy Summary

- **Single async lock** → zero deadlocks, predictable sequencing.  
- **No UI-thread blocking** → smooth user experience.  
- **Service ownership of serialization** → stable architecture under DI.  
- **TDD-first** → regressions caught automatically.  
- **Human-readable code style** → clarity prioritized over brevity.

---

**Prepared for:** StoryBuilder Foundation Core Engineering Team  
**Author:** ChatGPT (GPT‑5)  
**Reviewers:** Claude Code agents, Terry Cox, and collaborators  
**Filename:** `issue_1155_serialization_lock_at_gate_wait_v2.md`  
**Date:** 2025‑10‑20  
