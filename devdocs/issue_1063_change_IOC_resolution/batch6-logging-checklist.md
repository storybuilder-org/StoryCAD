# DI Cleanup — Batch 6 Checklist (for Issue #1063)

## Batch: Logging
**Services in this batch:** LogService (refactor to use ILogService interface everywhere)

### Checkboxes (apply the playbook)
- [ ] Confirm ILogService interface and LogService are registered in `ServiceLocator.cs` (Singletons; no lifetime changes).
- [ ] Search call sites for LogService usage:
  ```bash
  git grep -n "Ioc.Default.GetRequiredService<LogService>"
  git grep -n "Ioc.Default.GetService<LogService>"
  git grep -n "LogService" | grep -v "ILogService"
  ```
- [ ] Edit every consumer file:
  - **SKIP Page classes (Views/*.xaml.cs)** - these are out of scope
  - Change all consumers to depend on `ILogService` interface (not `LogService` concrete)
  - Add constructor parameters using `ILogService`
  - Create `private readonly ILogService _logService` fields
  - Replace **all** `Ioc.Default` calls with the injected fields
- [ ] Refactor LogService itself:
  - Add constructor to receive its dependencies (`AppState`, `PreferenceService`, etc.)
  - Create `private readonly` fields for injected dependencies
  - Remove all `Ioc.Default` calls from within LogService
  - Keep behavior identical (file logs, console in debug, elmah integration)
- [ ] Fix construction sites (pass dependencies or resolve via DI), build clean.
- [ ] Tests:
  - [ ] Run unit tests (where present).
  - [ ] Smoke test the app (launch, navigate, verify logs still work).
  - [ ] Verify logging output still goes to expected destinations
- [ ] Grep gates (all must return **no results**):
  ```bash
  git grep -n "Ioc.Default.GetRequiredService<LogService>"
  git grep -n "Ioc.Default.GetService<LogService>"
  # Verify all non-test consumers use ILogService interface:
  git grep -n ": LogService " | grep -v "ILogService" | grep -v Test | grep -v "\.cs:"
  ```
- [ ] Commit message:
  ```
  DI: Replace Ioc.Default for LogService with ILogService interface injection; refactor LogService to use constructor injection
  ```

### Notes
- Field naming: `_logService` for ILogService fields, `_camelCase` and `readonly` for all fields
- **ALL** consumers must depend on `ILogService` interface, not `LogService` concrete class
- LogService implementation must receive its dependencies via constructor injection
- Behavior must remain identical: file logs, console output in debug, elmah.io integration
- App.xaml.cs may need special handling as application entry point

### Special Considerations for LogService Refactoring
- LogService currently depends on:
  - AppState (for environment/state information)
  - PreferenceService (for logging preferences)
  - Possibly other services for elmah.io integration
- These dependencies must be injected via constructor
- Ensure logging still works during application startup/shutdown
- Test circular dependency scenarios carefully

---

## PR Description Template

**Title:** DI Cleanup: Batch 6 — Logging (Issue #1063)

**Summary**
- Switch all consumers to use `ILogService` interface instead of concrete `LogService`
- Refactor `LogService` to receive dependencies via constructor injection
- No lifetime changes (Singletons). No behavior changes.

**Changes**
- Replace all `Ioc.Default` usages for LogService with ILogService interface injection
- Update LogService to use constructor injection for its dependencies
- Update all consumers to depend on ILogService interface
- Tests: unit + smoke pass locally

**Verification**
- Grep gates show zero remaining `Ioc.Default` for LogService
- All consumers use ILogService interface (except LogService implementation itself)
- Logging continues to work: file logs, console (debug), elmah.io
- App launches; key flows exercised with proper logging

**Link:** #1063

---