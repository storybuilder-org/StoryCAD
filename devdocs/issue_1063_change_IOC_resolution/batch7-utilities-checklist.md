# DI Cleanup — Batch Checklist Template (for Issue #1063)

> Use this template **as-is** for every batch PR. Claude Code should fill in the placeholders.

## Batch: Utilities
**Services in this batch:** SearchService, RatingService, SemanticKernelApi, ControlLoader, ListLoader, ToolLoader, ScrivenerIo, StoryIO, MySqlIo, ControlData, ListData, ToolsData, TreeViewSelection

### Checkboxes (apply the playbook)
- [ ] Confirm services are registered in `ServiceLocator.cs` (Singletons; no lifetime changes).
- [ ] Search call sites for each service:
  ```bash
  git grep -n "Ioc.Default.GetRequiredService<(SearchService|RatingService|SemanticKernelApi|ControlLoader|ListLoader|ToolLoader|ScrivenerIo|StoryIO|MySqlIo|ControlData|ListData|ToolsData|TreeViewSelection)>"
  git grep -n "Ioc.Default.GetService<(SearchService|RatingService|SemanticKernelApi|ControlLoader|ListLoader|ToolLoader|ScrivenerIo|StoryIO|MySqlIo|ControlData|ListData|ToolsData|TreeViewSelection)>"
  ```
- [ ] Edit every consumer file:
  - **SKIP Page classes (Views/*.xaml.cs)** - these are out of scope
  - Add constructor parameters (prefer interfaces; logging uses `ILogService`).
  - Create `private readonly` fields named with underscore camelCase (e.g., `_searchService`).
  - Replace **all** `Ioc.Default` calls with the injected fields.
- [ ] Fix construction sites (pass dependencies or resolve via DI), build clean.
- [ ] Tests:
  - [ ] Run unit tests (where present).
  - [ ] Smoke test the app (launch, navigate, verify logs).
- [ ] Grep gates (all must return **no results**):
  ```bash
  git grep -n "Ioc.Default.GetRequiredService<(SearchService|RatingService|SemanticKernelApi|ControlLoader|ListLoader|ToolLoader|ScrivenerIo|StoryIO|MySqlIo|ControlData|ListData|ToolsData|TreeViewSelection)>"
  git grep -n "Ioc.Default.GetService<(SearchService|RatingService|SemanticKernelApi|ControlLoader|ListLoader|ToolLoader|ScrivenerIo|StoryIO|MySqlIo|ControlData|ListData|ToolsData|TreeViewSelection)>"
  ```
- [ ] Commit message:
  ```
  DI: Replace Ioc.Default for SearchService, RatingService, SemanticKernelApi, ControlLoader, ListLoader, ToolLoader, ScrivenerIo, StoryIO, MySqlIo, ControlData, ListData, ToolsData, TreeViewSelection with constructor injection; keep singleton lifetimes
  ```

### Notes
- Field naming: `_camelCase` and `readonly`.
- Consumers of logging must depend on `ILogService` (not `LogService`).
- `SerializationLock` logic bug is out of scope (separate issue).

---

## PR Description Template

**Title:** DI Cleanup: Batch 7 — Utilities (Issue #1063)

**Summary**
- Apply the IOC → Constructor Injection Playbook to: SearchService, RatingService, SemanticKernelApi, ControlLoader, ListLoader, ToolLoader, ScrivenerIo, StoryIO, MySqlIo, ControlData, ListData, ToolsData, TreeViewSelection
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
You are editing Batch 7: Utilities for Issue #1063.

Services in this batch: SearchService, RatingService, SemanticKernelApi, ControlLoader, ListLoader, ToolLoader, ScrivenerIo, StoryIO, MySqlIo, ControlData, ListData, ToolsData, TreeViewSelection

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