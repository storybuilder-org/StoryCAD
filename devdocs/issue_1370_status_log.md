# Issue #1370: Account Data Deletion — Status Log

## Session Progress — 2026-03-29

### Completed
- **Phase 0.1**: Exported current schema from ScaleGrid (structure only, no data). Live schema has drifted from SQL files — `users` table has `first_name`, `last_name`, `email_verified` columns and `user_name` widened to VARCHAR(128), none of which were captured in alteration files.
- **Phase 0.2**: Created local `StoryBuilderTest` database with live schema, `spAddUser` procedure, and 3 fake test users. Created `stbtest` and `stbutil` database users with SELECT + EXECUTE only.
- **Phase 0.3**: Created 3 new stored procedures on local test DB: `spAddOrUpdatePreferences`, `spAddOrUpdateVersion`, `spDeleteUser`. All verified working with restricted `stbtest` user.
- **Phase 0.4**: Stored procedures applied to local test DB and verified.
- **Phase A**: Full PreferencesViewModel baseline test coverage — 19 tests, all passing. Covers LoadModel (5), SaveModel (5), SaveAsync (2), computed properties (4), PropertyChanged notifications (3).

### Next Session
- **Phase 0.4a**: Recreate local SPs with bare parameter names (were `p_` prefixed; must match `spAddUser` convention)
- **Phase 0.5**: Extract `IMySqlIo` interface, register in IoC
- **Phase B0**: Refactor `AddOrUpdatePreferences` + `AddVersion` in MySqlIo.cs to use stored procedures
- **Phase B1-B2**: Add `DeleteUser` to MySqlIo, `DeleteUserData` to BackendService
- **Phase B3**: Mocked automated tests using `TestMySqlIo` (no database dependency)
- **Phase B-audit**: DAL security audit

### Notes
- `tcox` MySQL user needed `GRANT OPTION` added (via `sudo mysql`) to create test user permissions
- Local MySQL accessible from WSL, no Docker needed

---

## Session Progress — 2026-03-30

### Completed
- **Plan update**: Revised Phase 0.5, B1-B3 based on design discussion:
  - Automated tests cannot depend on local MySQL (other contributors won't have it, credentials are a security problem)
  - Phase 0.5 changed from "configure test connection" to "extract `IMySqlIo` interface"
  - Phase B tests use `TestMySqlIo` (mocked, no DB) following existing `TestSaveable`/`MockCollaborator` patterns
  - Manual test script in `ManualTests/` for real SP verification against local DB
  - Fixed SP parameter naming: bare names (matching `spAddUser`), not `p_` prefixes
  - Noted local SPs need to be recreated (0.4a)
  - Recorded exported schema location: `/mnt/c/temp/StoryBuilder DDL.txt`
- **Created status log**: `devdocs/issue_1370_status_log.md` (this file) — committed to repo this time

### Completed (continued)
- **Phase 0.4a**: Recreated local SPs with bare parameter names via `/mnt/c/temp/recreate_stored_procedures.sql`. All 4 SPs verified in `StoryBuilderTest`.
- **Phase 0.5**: Extracted `IMySqlIo` interface. Created `StoryCADLib/DAL/IMySqlIo.cs`, `MySqlIo` implements it, IoC registers `IMySqlIo → MySqlIo`, `BackendService` resolves `IMySqlIo`. Build: 0 errors. Tests: 910 passed, 14 skipped, 0 failures.

- **Phase B0**: Refactored `AddOrUpdatePreferences` and `AddVersion` to use stored procedures. Build: 0 errors. Tests: 909 passed, 1 failed (`CheckConnection` — expected, hits live ScaleGrid which doesn't have the new SPs yet), 14 skipped.

- **CheckConnection refactor**: Replaced old `CheckConnection` test (hit live ScaleGrid via Doppler) with 2 mocked tests: `PostVersion_WhenConnectionNotConfigured_SkipsGracefully` and `PostPreferences_WhenConnectionNotConfigured_SkipsGracefully`. No unit test hits production now. Tests: 925 total, 911 passed, 14 skipped, 0 failures.

- **Phase B1**: Added `DeleteUser` to `IMySqlIo` and `MySqlIo` (calls `spDeleteUser`). Build: 0 errors.

- **Phase B2**: Added `DeleteUserData` to `BackendService`. Build: 0 errors.

- **DAL refactor (B1+B2 revised)**: Complete separation of concerns:
  - `IMySqlIo` — connection-free interface with `SetConnectionString`, `IsConnectionConfigured`, 4 DB methods
  - `MySqlIo` — manages connections internally, each method opens/closes its own connection
  - `BackendService` — pure business logic, no connection management, delegates all DB work to `IMySqlIo`
  - `DeleteUserData()` uses stored `UserId` from `PreferencesModel` (new `internal int UserId` property)
  - `DeleteUser(int id)` on `IMySqlIo`/`MySqlIo` matches `spDeleteUser(IN user_id INT)`
  - Build: 0 errors. Tests: 925 total, 911 passed, 14 skipped, 0 failures.

- **Phase B3**: Added `TestMySqlIo` implementing `IMySqlIo` (records calls, no DB). Constructor-injected `IMySqlIo` into `BackendService` (replaced `Ioc.Default.GetService` calls). 4 new delete tests: not-configured, no-userId, configured-success, database-throws. Updated all existing `BackendService` tests to pass `TestMySqlIo`/`IMySqlIo` as constructor arg. Tests: 929 total, 915 passed, 14 skipped, 0 failures.

### Audit Findings (requiring code changes)
- **Critical**: SP `spAddOrUpdatePreferences` — `version = version` self-references column, never updates on duplicate. Rename parameter.
- **Critical**: SP `spDeleteUser` — no SQLEXCEPTION handler, partial deletes possible without rollback. Add handler.
- **High**: PII (names, userIDs) logged at INFO level — contradicts deletion compliance.
- **High**: `SetConnectionString` accepts null/garbage, marks configured. Add validation.
- **High**: `DeleteUserData` doesn't verify rows actually deleted. Check affected rows.
- **Medium**: `PostPreferences` exception handling inconsistent with `PostVersion`. Align.
- **Design bug**: If backend delete fails, local data is still cleared — user loses ability to retry. Fix: only clear local if backend succeeds or was never configured.
- **Design bug**: After successful delete, app session continues with cleared preferences in undefined state. Need to force-close or require restart.

### Deferred (separate issues, not 4.0.2 scope)
- `latin1` charset migration (production DB change, large scope)
- Connection string in memory (architectural, all .NET apps)
- SSL temp file predictability (low risk for desktop app)
- Doppler token validation (separate service)
- Thread safety (single-threaded UI app)
- Connection timeouts (tuning)

- **Audit fixes applied**:
  - SP `spAddOrUpdatePreferences`: renamed `version` param to `ver` (fixes column self-reference)
  - SP `spDeleteUser`: added `OUT deleted BOOL`, `DECLARE EXIT HANDLER FOR SQLEXCEPTION` with rollback
  - `MySqlIo.SetConnectionString`: rejects null/empty/whitespace
  - `MySqlIo.DeleteUser`: reads OUT `deleted` param, returns `Task<bool>`
  - `IMySqlIo.DeleteUser`: return type changed to `Task<bool>`
  - `BackendService.DeleteUserData`: checks `DeleteUser` return value, only reports success if SP confirmed deletion
  - `BackendService.PostPreferences`: exception handling aligned with `PostVersion` (all MySQL error codes, IO, UnauthorizedAccess)
  - `BackendService`: PII removed from log messages (no more names logged, only userId)
  - `TestMySqlIo.DeleteUser`: updated to return `Task<bool>`
  - Build: 0 errors. Tests: 929 total, 915 passed, 14 skipped, 0 failures.
  - **Pending**: Run updated SP script on local DB (`sudo mysql StoryBuilderTest < /mnt/c/temp/recreate_stored_procedures.sql`)

### Design decisions from audit
- Delete flow: only clear local data if backend delete succeeds or backend was never configured
- After successful delete: close the app (show goodbye message, then exit)

- **Phase C**: Added `DeleteMyDataAsync` to PreferencesViewModel (TDD). 3 tests: backend succeeds (clears local), backend fails (preserves local), backend not configured (clears local). Build: 0 errors. Tests: 932 total, 918 passed, 14 skipped, 0 failures.

- **Phase D**: XAML UI changes complete:
  - "General" tab renamed to "Account" (name, email, Delete My Data button)
  - New "Save Locations" tab (project dir, backup dir)
  - Backup directory removed from Backup tab
  - Delete My Data click handler: confirmation dialog → delete → goodbye message → `Application.Current.Exit()`
  - Failure path: shows error, local data preserved for retry
  - Build: 0 errors. Tests: 932 total, 918 passed, 14 skipped, 0 failures.

### Next Steps
- Commit all changes
- Manual testing against local DB
- Deploy SPs to ScaleGrid
- Mac testing + screen recording for Apple

### Local MySQL Environment

- **Installation**: MySQL 8.0.45 installed via apt in WSL (not Docker)
- **Process**: runs as `/usr/sbin/mysqld`, starts automatically with WSL
- **Root access**: `root` requires `sudo` — cannot be used from Claude Code's Bash tool
- **Test users created (2026-03-29)**: `stbtest` and `stbutil` with SELECT + EXECUTE only on `StoryBuilderTest`
- **Password**: `stbtest` requires a password (unknown — not documented from previous session)
- **Claude Code Bash tool**: Can run `mysql` commands directly (no `!` prefix needed) but cannot run `sudo` — so any operation requiring root must be done by the user in their terminal
- **MySQL Workbench connections**: WSL (root@localhost:3306), ScaleGrid (stbutil@SG-StoryBuilder-6466), marketing (tcox@127.0.0.1:3306)
- **Database**: `StoryBuilderTest` — created from exported live schema, seeded with 3 fake test users
- **SP script for 0.4a**: `/mnt/c/temp/recreate_stored_procedures.sql`
