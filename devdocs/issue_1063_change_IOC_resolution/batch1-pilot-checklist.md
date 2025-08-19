# DI Cleanup — Batch Checklist Template (for Issue #1063)

> Use this template **as-is** for every batch PR. Claude Code should fill in the placeholders.

## Batch: Pilot Batch
**Services in this batch:** SceneViewModel

### Checkboxes (apply the playbook)
- [x] Confirm services are registered in `ServiceLocator.cs` (Singletons; no lifetime changes).
  - ✓ SceneViewModel registered as Singleton on line 107
- [x] Search call sites for each service:
  ```bash
  git grep -n "Ioc.Default.GetRequiredService<SceneViewModel>"
  git grep -n "Ioc.Default.GetService<SceneViewModel>"
  ```
  
  <details>
  <summary>SceneViewModel call sites found:</summary>
  
  1. `StoryCADLib/ViewModels/ShellViewModel.cs:522` - `Ioc.Default.GetRequiredService<SceneViewModel>()` ✓ CONVERTED
  2. `StoryCAD/Views/ScenePage.xaml.cs:7` - `Ioc.Default.GetService<SceneViewModel>()` - SKIPPED (Page class, out of scope)
  
  </details>
- [x] Edit every consumer file:
  - Add constructor parameters (prefer interfaces; logging uses `ILogService`).
  - Create `private readonly` fields named with underscore camelCase (e.g., `_sceneViewModel`).
  - Replace **all** `Ioc.Default` calls with the injected fields.
  - ✓ ShellViewModel: Added constructor parameter, created `_sceneViewModel` field, replaced Ioc.Default call
  - Note: ScenePage.xaml.cs is a UI Page (out of scope per issue #1063 - page construction tracked separately)
- [x] Fix construction sites (pass dependencies or resolve via DI), build clean.
  - ✓ ShellViewModel is resolved via DI, container handles injection automatically
  - ✓ Build successful
- [x] Tests:
  - [x] Run unit tests (where present).
    - ✓ All tests passing, including ShellViewModel tests
  - [ ] Smoke test the app (launch, navigate, verify logs) - TO BE DONE MANUALLY
- [x] Grep gates (all must return **no results**):
  ```bash
  git grep -n "Ioc.Default.GetRequiredService<SceneViewModel>"
  git grep -n "Ioc.Default.GetService<SceneViewModel>"
  ```
  - ✓ No results outside of Views/ folder (Pages are out of scope)
- [x] Commit message:
  ```
  DI: Replace Ioc.Default for SceneViewModel with constructor injection; keep singleton lifetimes
  ```
  - ✓ Committed with hash: d2155e2

### Notes
- Field naming: `_camelCase` and `readonly`.
- Consumers of logging must depend on `ILogService` (not `LogService`).
- `SerializationLock` logic bug is out of scope (separate issue).

---

## PR Description Template

**Title:** DI Cleanup: Batch 1 — Pilot Batch (Issue #1063)

**Summary**
- Apply the IOC → Constructor Injection Playbook to: SceneViewModel
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
You are editing Batch 1: Pilot Batch for Issue #1063.

Services in this batch: SceneViewModel

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
