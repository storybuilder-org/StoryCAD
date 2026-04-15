# Issue 1333: Usage Statistics — Status Log

## 2026-04-02

### Session Summary
- Created branch `issue-1333-usage-statistics` off `dev`
- Researched OpenTelemetry as suggested by Jake (Rarisma) in issue comment
- Produced research document: `devdocs/issue_1333_opentelemetry_research.md`
- Mapped all telemetry instrumentation points in the codebase: `devdocs/issue_1333_instrumentation_points.md`
  - Session lifecycle: `App.OnLaunched()` / `OutlineViewModel.ExitApp()`
  - Outline lifecycle: `FileOpenService.OpenFile()` / `FileCreateService.CreateFile()` / `OutlineViewModel.CloseFile()`
  - Element operations: `AddStoryElement()` / `RemoveStoryElement()` / `MoveTreeViewItem*()`
  - Feature usage: 9 tools identified (Collaborator, Key Questions, Topics, Master Plots, Dramatic Situations, Stock Scenes, Scrivener Export, Print, Search)
  - Existing infrastructure: `BackendService`, `IMySqlIo`, `PreferencesModel` consent flags, Doppler secrets

### Key Findings (Exploratory — Nothing Settled)

**OpenTelemetry data model: Better fit than initially assessed.** The initial research concluded OTel was a poor fit because it aggregates rather than producing per-row data. However, the proposed MySQL tables are intended for summarization, not per-session granularity. OTel's aggregated metrics map well to the actual requirements:
- Session duration → Histogram (avg, p50, p95, p99)
- Genre/story form distribution → Counters with attributes
- Element counts by type → Histograms with attributes
- Feature usage → Counters with feature_name attribute
- These directly serve the stated goals: product development insights, community stats, resource allocation, subscription tier design

**OpenTelemetry infrastructure: Still a concern.** Even with good data model fit, practical issues remain:
- No MySQL exporter — would require a new backend (Prometheus, Grafana Cloud, etc.)
- New infrastructure to maintain for a volunteer team
- Existing codebase has `IMySqlIo`/stored procedure patterns that custom telemetry would reuse
- OTel requires building something new rather than extending what exists

**Decision on OTel approach is open.** The tradeoff is data model fit (decent) vs infrastructure fit (poor). Further exploration needed.

**Integer user_id vs GUID: No privacy difference.** Both are pseudonymous — as long as the `users` table maps either to an email, the user is identifiable. Consent gating is the real privacy mechanism, not identifier format.

**Unlinkable usage_id for true anonymity (if desired later):**
- App generates a random GUID locally on opt-in, stored only in local preferences
- All usage data sent to MySQL uses this `usage_id` — not the `user_id` from the `users` table
- No foreign key, no lookup table connecting the two in the database
- Database cannot join usage data back to user identity, even for admins
- Tradeoff: loses ability to correlate usage with user demographics, delete per-user usage data (GDPR), or debug individual user issues
- Still supports aggregate analytics (feature popularity, genre distribution, session duration patterns)

### Artifacts
- `devdocs/issue_1333_opentelemetry_research.md` — Full research document (committed f885b1c7)
- `devdocs/issue_1333_instrumentation_points.md` — Codebase instrumentation map (approach-independent)

### Next Steps
- Design phase planning for issue 1333 (schema, consent model, implementation approach)

## 2026-04-14

### Session Summary (Terry + Claude)
- Consolidated `/mnt/c/temp/` documents: split issue #1297 (Collaborator/app store) from #1332 (Discord/Pro) into separate folders
- Created design document: `devdocs/issue_1333_design.md` covering:
  - Shared database infrastructure for #1333 and #1377 (test DB environment, DAL read capability, spDeleteUser cascade, schema migration)
  - Usage statistics design: architecture, consent model, schema (4 tables), instrumentation points, data flow
  - UsageTrackingService interface with 18 hook locations
  - Single-round-trip flush via stored procedure using JSON parameters and JSON_TABLE
  - Privacy: unlinkable `usage_id` (random GUID, no FK to users), 90-day retention with MySQL EVENT purge
- Ran architecture and SQL agent reviews — incorporated all findings:
  - Added UNIQUE constraint on outline_metadata, transaction/rollback in SP, indexes, created_at column, flush timeout, deprecated VALUES() fix
- **ScaleGrid is running MySQL 5.7.30** — upgrade to 8.0 is a prerequisite (needed for JSON_TABLE and row alias syntax)
- Decisions made: element counts by type, count per tool per session, no auto opt-in (use #1377 messaging), one flush per session end, lossy/no-retry

### Session Summary (Jake)
- Set up local MySQL 8.0 via Docker on Mac laptop
- Got StoryCAD connecting to local Docker DB
- Started implementing services with Claude
- Used `StoryBuilderTest` database and `stbtest` account

### Decisions
- ScaleGrid upgrade to MySQL 8.0 is a prerequisite — will coordinate with ScaleGrid
- Docker is the recommended cross-platform approach for the local test database
- TDD for UsageTrackingService: mock the DB layer, test collection logic end-to-end

## 2026-04-15

### Morning sync (Terry + Jake)
- Confirmed Jake is on MySQL 8 locally (Docker)
- Discussed TDD approach for UsageTrackingService — test hooks and collection logic with mocked DB, use Claude Code's TDD agent
- Jake to document his Docker setup procedures for Terry
- Jake has interview + assessment today, limited availability

### Next Steps
- Jake: continue service implementation with TDD, document Docker setup
- Terry: coordinate ScaleGrid MySQL 8 upgrade, review Jake's work when available
- Both: design review discussion on edit tracking gap and privacy policy language

### INT→GUID User Key Migration Discussion (Terry, Jake, Claude)

No decisions made — problems and tradeoffs captured below.

**Why GUID**: INT keys are sequential and guessable — security risk if any API ever exposes them. Terry and Jake agreed on the change.

**SP impact**: Today's `spAddUser` uses `LAST_INSERT_ID(id)` trick on `ON DUPLICATE KEY UPDATE` to return existing INT id. `LAST_INSERT_ID()` only works with integers — can't pass a CHAR(36). Proposed replacement: `INSERT IGNORE` (skip if email exists via UNIQUE constraint) + `SELECT id WHERE email = p_email`. Two statements, one SP call, one round-trip. On the duplicate path, nothing is updated — the row already exists, the SELECT just retrieves the GUID. Parameter naming must avoid column shadowing (`p_name`, `p_email`).

**Migration mechanics**: ~2,000 existing users need GUIDs assigned during the migration script itself — FK tables (`preferences`, `versions`) can't contain rows without valid user_ids. Script must: add GUID column → populate with `UUID()` → update FK tables → drop old INT PK → rebuild constraints. Usage tables (#1333) unaffected (use `usage_id`). #1377's `message_recipients` should use GUID from the start.

**Old client compatibility**: Pre-4.1 app calls `spAddUser` expecting INT OUT parameter. New SP returns CHAR(36) — MySql.Data client throws type mismatch. App catches exception, logs, continues without backend. Old users lose backend recording until they update. App itself works fine — backend has always been optional.

**Local preferences deserialization**: `PreferencesModel.UserId` changes from `int` to `string`. Locally stored JSON has `"UserId": 42` — fails to deserialize into string. The SP call would overwrite it on next successful launch, but deserialization failure may happen first. Needs handling.

**Jake's alternative — keep old tables alongside new**: Leave existing 3 tables/4 SPs untouched for old clients. Add new GUID-based tables for 4.1. Drop old after a few months. Problem: 4.1 starts with zero users — ~2,000 existing users' data isn't migrated. Still need a migration path eventually.

**Coordination**: Schema migration and 4.1 release must be coordinated. ScaleGrid upgrade to MySQL 8.0 is the natural time. Email drafted to Emmanuel at ScaleGrid requesting a call.

**Resolution**: Keep INT keys. No GUID change. The security concern (guessable sequential IDs) is not a real exposure — user IDs are behind SSL, Doppler credentials, and ScaleGrid hosting, with no public API exposing them. Adding a GUID column (agents' recommendation) or replacing the PK both add complexity without clear benefit at this scale. The INT works, the stored procedures work, old clients work. Move on.
