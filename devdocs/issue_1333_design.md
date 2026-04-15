# Issue 1333: Usage Statistics — Design Document

**Issue**: https://github.com/storybuilder-org/StoryCAD/issues/1333
**Branch**: `issue-1333-usage-statistics`
**Last Updated**: 2026-04-14

---

## 1. Shared Database Infrastructure

Issues #1333 (usage statistics) and #1377 (admin messaging) both require new MySQL tables on the ScaleGrid backend. The shared infrastructure is built here in #1333 so that #1377 can inherit it.

### 1.1 Test Database Environment

**Problem**: The existing unit tests use `TestMySqlIo` (a fake that records calls, no DB connection). There is no way to run integration tests against a real MySQL instance. Schema changes, stored procedures, and queries cannot be validated without manual testing against production.

**Solution**: A local MySQL instance for integration testing, switchable via environment variable.

#### What already exists (from Issue #1370)

Issue #1370 (account data deletion, completed for 4.0.2) established a local test database on Terry's Windows/WSL machine:

| Component | Location | Status |
|-----------|----------|--------|
| MySQL 8.0.45 | WSL (apt install, not Docker) | Running — starts automatically with WSL |
| Test database | `StoryBuilderTest` | Created from exported live schema, 3 fake test users |
| DB users | `stbtest`, `stbutil` | SELECT + EXECUTE only. **`stbtest` password undocumented.** |
| Canonical schema | `storybuilder-miscellaneous/mysql/STORYBUILDER_CURRENT_SCHEMA.sql` | Matches ScaleGrid production as of 2026-03-31 |
| Schema changelog | `storybuilder-miscellaneous/mysql/SCHEMA_CHANGELOG.md` | Documents all changes from #1370 |
| SP recreation script | `/mnt/c/temp/recreate_stored_procedures.sql` | Used to apply bare-name SPs to local DB |
| MySqlConsole | `storybuilder-miscellaneous/mysql/MySqlConsole/` | Manual test harness with `EmptyTables()`, seed data. **Stale**: pre-refactor DAL, hardcoded connection string, .NET 6 |
| Root access | `root` requires `sudo` | Cannot be run from automated tools — user must run in their terminal |

#1370 used the local DB for **manual SP verification only** — no automated integration tests were created.

#### Docker approach (from Jake, 2026-04-14)

Jake set up MySQL 8.0 locally on his Mac laptop via Docker. This is now the recommended cross-platform approach for the test database:

- **Docker works on Mac, Windows, and Linux** — eliminates per-OS MySQL install procedures
- **Disposable**: spin up a container, run tests, tear it down. Clean state every time.
- **Reproducible**: Dockerfile or docker-compose.yml can be checked into the repo
- Jake has StoryCAD connecting to the local Docker MySQL and has started implementing services
- **Docker setup procedures need to be documented** — Jake to provide his Docker configuration

#### What's missing (gaps to fill for #1333/#1377)

1. **Environment switch in the code**: Both app and tests currently use `dp.st.prd` (production Doppler config). Need an environment variable (e.g., `STORYCAD_DB_ENV=test`) to select between production and local test database. The `.env` file (gitignored) holds the Doppler token; for local testing, could bypass Doppler entirely with a local connection string.

2. **Doppler access**: Needs to be verified — it has been a long time since the Doppler configuration was changed. Alternatively, local testing may not need Doppler at all — a local connection string in `.env` may suffice.

3. **`stbtest` password recovery**: Password was reset during #1370 but not documented. Need to reset again or create a new test user.

4. **Automated integration test path**: #1370 proved the local DB works for manual testing. #1333 needs a way to run integration tests from `StoryCADTests` against the local DB, gated so they don't run in CI or on machines without a local MySQL.

5. **MySqlConsole modernization**: The console app is useful for manual DB operations but needs updating (connection string, .NET version, current DAL patterns). Could be updated or replaced.

6. **ScaleGrid MySQL version**: Currently running **5.7.30** (verified 2026-04-14). Must be upgraded to **8.0.19+** for `JSON_TABLE` and row alias `ON DUPLICATE KEY UPDATE` syntax. **This is a prerequisite for implementation.** Working with ScaleGrid to upgrade — likely requires creating a new 8.0 deployment and migrating data via their Live Migration tool. MySQL 5.7 reached EOL in October 2023 (no Oracle security patches), so the upgrade is warranted independently of this issue.

#### Components (target state)

| Component | Location | Notes |
|-----------|----------|-------|
| Schema DDL (CREATE TABLE, stored procedures) | `StoryCADTests/` | Safe for public repo — no secrets |
| Test setup/teardown scripts | `StoryCADTests/` | Reset DB to known state for repeatable tests |
| Test database credentials | `.env` (local connection string, gitignored) | For local testing; Doppler for CI if needed |
| Production-derived seed data (if any) | `storybuilder-miscellaneous` | May contain real user records — private repo |

#### Test database maintenance
- DDL kept in sync with production schema (update `STORYBUILDER_CURRENT_SCHEMA.sql` when adding tables)
- Process for cloning/updating from production schema (not data) — TBD
- Backup/restore procedures for repeatable test runs — TBD
- Mac testing: environment variable approach works cross-platform; local MySQL install on Mac TBD

### 1.2 DAL Read Capability

**Problem**: `IMySqlIo` is currently write-only (INSERT/UPDATE/DELETE via stored procedures). Issue #1377 (messaging) requires the app to read from the backend (fetch unread messages on launch). This is a new pattern.

**Solution**: Add read methods to `IMySqlIo` following the existing stored procedure pattern. The first read methods will be added under #1377, but the pattern and any shared infrastructure (e.g., result mapping) are established here.

### 1.3 spDeleteUser Cascade

**Problem**: The existing `spDeleteUser` stored procedure cascades deletes across `preferences` and `versions`. New tables from #1377 (messages/recipients) must be included in the cascade for Apple Guideline 5.1.1(v) compliance (account data deletion).

**Usage tables (#1333) are excluded from the cascade.** Usage data uses an unlinkable `usage_id` with no foreign key to `users` — there is nothing to cascade. The database cannot determine which usage rows belong to a given user. This is by design.

**Solution**: Update `spDeleteUser` to delete from #1377's tables (messages/recipients) when those are created. Usage tables require no changes.

### 1.4 Schema Migration Strategy

**Problem**: No migration tooling exists. The current schema was applied manually. Both issues add tables to a live database with ~2,000 users.

**Solution**: Numbered SQL scripts (e.g., `V001__usage_tables.sql`, `V002__messaging_tables.sql`) stored in `StoryCADTests/`. Applied manually to both local test DB and ScaleGrid production. Each script is idempotent where possible. A simple `schema_version` table tracks which scripts have been applied.

No migration framework — the volume of changes doesn't justify the overhead. Scripts are the source of truth; `STORYBUILDER_CURRENT_SCHEMA.sql` in `storybuilder-miscellaneous` is updated after each migration to reflect the full current state.

---

## 2. Usage Statistics Design

### 2.1 Approach Decision

**OpenTelemetry: rejected.** Research documented in `devdocs/issue_1333_opentelemetry_research.md`. OTel's aggregated metrics model doesn't fit StoryCAD's need for per-session, per-outline rows. No MySQL exporter exists. Infrastructure overhead is disproportionate for a ~2,000 user desktop app.

**Custom telemetry via existing `IMySqlIo` pattern: chosen.** Direct MySQL writes using stored procedures, extending the existing `BackendService` / `IMySqlIo` / Doppler infrastructure. Zero new dependencies.

### 2.2 Architecture

```
StoryCAD Desktop App
    |
    UsageTrackingService (accumulates data in memory during session)
    |
    BackendService (business logic, consent gating, exception handling)
    |
    IMySqlIo (stored procedure calls over SSL)
    |
    ScaleGrid MySQL (production) or local MySQL (test)
```

### 2.3 Consent Model

- Add `UsageStatsConsent` boolean to `PreferencesModel`
- Add checkbox to Preferences dialog (alongside existing elmah and newsletter checkboxes)
- Add `usage_consent` column to `preferences` table
- Gate all collection on this flag — if false, `UsageTrackingService` is a no-op
- **Headless/API mode**: Forces `UsageStatsConsent = false` — no usage data collected when running without an interactive user session
- Update privacy policy to disclose what's collected, how it's stored, how it's used

### 2.4 Proposed Schema

New tables on the existing `StoryBuilder` database. All usage tables use `usage_id` — a random GUID generated locally on opt-in, stored only in local preferences. There is **no foreign key to the `users` table** and no lookup table connecting them. The database cannot join usage data back to user identity, even for admins.

**`usage_id` lifecycle**:
- Generated once when the user opts in to usage statistics (random GUID)
- Stored in local `PreferencesModel` only — never in the `users` or `preferences` tables
- Sent with every usage data flush
- If the user opts out and back in, a new GUID is generated (previous data ages out via retention policy)

**`sessions`**
| Column | Type | Notes |
|--------|------|-------|
| session_id | INT AUTO_INCREMENT | PK |
| usage_id | CHAR(36) | Unlinkable — no FK to `users` |
| session_start | DATETIME | INDEX (drives retention purge) |
| session_end | DATETIME | |
| clock_time_seconds | INT | Wall clock duration |

**`outline_sessions`**
| Column | Type | Notes |
|--------|------|-------|
| id | INT AUTO_INCREMENT | PK |
| session_id | INT | FK → sessions.session_id, INDEX |
| outline_guid | CHAR(36) | From Story Overview element |
| open_time | DATETIME | |
| close_time | DATETIME | |
| elements_added | INT | Delta during this open |
| elements_edited | INT | Delta during this open |
| elements_deleted | INT | Delta during this open |

**`outline_metadata`**
| Column | Type | Notes |
|--------|------|-------|
| id | INT AUTO_INCREMENT | PK |
| usage_id | CHAR(36) | Unlinkable — no FK to `users` |
| outline_guid | CHAR(36) | UNIQUE (usage_id, outline_guid) — required for ON DUPLICATE KEY UPDATE |
| genre | VARCHAR(50) | From Story Overview |
| story_form | VARCHAR(50) | From Story Overview |
| element_count | INT | Total elements |
| last_updated | DATETIME | Story data timestamp |
| created_at | DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP | Row creation time — drives retention purge. INDEX |

**`feature_usage`**
| Column | Type | Notes |
|--------|------|-------|
| id | INT AUTO_INCREMENT | PK |
| session_id | INT | FK → sessions.session_id, INDEX |
| feature_name | VARCHAR(50) | e.g., "Collaborator", "MasterPlots" |
| use_count | INT | Times used within this session |

### 2.5 Instrumentation Points

Mapped in `devdocs/issue_1333_instrumentation_points.md`. Key hooks:

- **Session**: `App.OnLaunched()` (start) / `OutlineViewModel.ExitApp()` (end)
- **Outline**: `FileOpenService.OpenFile()` / `FileCreateService.CreateFile()` / `OutlineViewModel.CloseFile()`
- **Elements**: `AddStoryElement()` / `RemoveStoryElement()` / `RestoreStoryElement()`
- **Features**: 9 tools identified (Collaborator, Key Questions, Topics, Master Plots, Dramatic Situations, Stock Scenes, Scrivener Export, Print, Search)

### 2.6 Data Flow

1. Session start: `UsageTrackingService` records timestamp
2. During session: service accumulates counts in memory (elements added/deleted, features used, outlines opened/closed)
3. Session end: service flushes all accumulated data to MySQL via `BackendService` → `IMySqlIo`
4. If flush fails: data is lost for that session — no retry, no local queuing, no persistence between sessions. Log the failure via elmah so we can track how often data is lost and whether a more robust strategy is needed later.

**Design principle**: Usage data is inherently lossy. Connection issues, crashes, or process kills all result in lost data for that session. This is acceptable — the next session starts fresh. Simplicity over completeness.

### 2.6.1 Flush Optimization: Single Round-Trip via JSON

**Problem**: A naive flush could require 4+ separate SQL calls per session (1 session INSERT + N outline_sessions + N outline_metadata + N feature_usage). Over SSL to ScaleGrid, each round-trip adds latency — especially at shutdown.

**Solution**: One stored procedure, one round-trip, JSON parameters. The app serializes the session's accumulated data into JSON arrays and passes them as parameters to a single stored procedure.

**Stored procedure design** (`spRecordSessionData`):

```sql
CREATE PROCEDURE spRecordSessionData(
    IN p_usage_id CHAR(36),
    IN p_session_start DATETIME,
    IN p_session_end DATETIME,
    IN p_clock_time_seconds INT,
    IN p_outlines JSON,        -- array of outline objects
    IN p_features JSON          -- array of feature usage objects
)
BEGIN
    DECLARE v_session_id INT;

    -- Roll back all inserts if any statement fails (keeps DB consistent)
    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    START TRANSACTION;

    -- 1. Insert session row, capture auto-increment ID
    INSERT INTO sessions (usage_id, session_start, session_end, clock_time_seconds)
    VALUES (p_usage_id, p_session_start, p_session_end, p_clock_time_seconds);

    SET v_session_id = LAST_INSERT_ID();

    -- 2. Outline sessions (references session_id)
    INSERT INTO outline_sessions (session_id, outline_guid, open_time, close_time,
                                   elements_added, elements_edited, elements_deleted)
    SELECT v_session_id, jt.outline_guid, jt.open_time, jt.close_time,
           jt.elements_added, jt.elements_edited, jt.elements_deleted
    FROM JSON_TABLE(p_outlines, '$[*]' COLUMNS (
        outline_guid     CHAR(36)  PATH '$.outline_guid',
        open_time        DATETIME  PATH '$.open_time',
        close_time       DATETIME  PATH '$.close_time',
        elements_added   INT       PATH '$.elements_added',
        elements_edited  INT       PATH '$.elements_edited',
        elements_deleted INT       PATH '$.elements_deleted'
    )) AS jt;

    -- 3. Outline metadata — upsert via UNIQUE (usage_id, outline_guid)
    INSERT INTO outline_metadata (usage_id, outline_guid, genre, story_form,
                                   element_count, last_updated) AS new_row
    SELECT p_usage_id, jt.outline_guid, jt.genre, jt.story_form,
           jt.element_count, jt.last_updated
    FROM JSON_TABLE(p_outlines, '$[*]' COLUMNS (
        outline_guid  CHAR(36)    PATH '$.outline_guid',
        genre         VARCHAR(50) PATH '$.genre',
        story_form    VARCHAR(50) PATH '$.story_form',
        element_count INT         PATH '$.element_count',
        last_updated  DATETIME    PATH '$.last_updated'
    )) AS jt
    ON DUPLICATE KEY UPDATE
        genre         = new_row.genre,
        story_form    = new_row.story_form,
        element_count = new_row.element_count,
        last_updated  = new_row.last_updated;

    -- 4. Feature usage (references session_id)
    INSERT INTO feature_usage (session_id, feature_name, use_count)
    SELECT v_session_id, jt.feature_name, jt.use_count
    FROM JSON_TABLE(p_features, '$[*]' COLUMNS (
        feature_name VARCHAR(50) PATH '$.feature_name',
        use_count    INT         PATH '$.use_count'
    )) AS jt;

    COMMIT;
END;
```

**Key points**:
- **Transactional**: `START TRANSACTION` / `COMMIT` with `EXIT HANDLER FOR SQLEXCEPTION` that rolls back. Partial writes cannot occur — the SP either succeeds completely or writes nothing. (Same pattern as existing `spDeleteUser`.)
- `LAST_INSERT_ID()` is connection-scoped — no race condition with other sessions
- `JSON_TABLE()` (MySQL 8.0.4+) unpacks JSON arrays into row sets for INSERT...SELECT
- `ON DUPLICATE KEY UPDATE` on `outline_metadata` uses row alias syntax (not deprecated `VALUES()`) — requires MySQL 8.0.19+
- `UNIQUE (usage_id, outline_guid)` on `outline_metadata` is required for the upsert to work
- The entire procedure runs in one round-trip from the .NET client

**.NET call**:

```csharp
var outlines = JsonSerializer.Serialize(outlineData);
var features = JsonSerializer.Serialize(featureData);

await using var cmd = new MySqlCommand("spRecordSessionData", conn);
cmd.CommandType = CommandType.StoredProcedure;
cmd.Parameters.AddWithValue("p_usage_id", usageId);
cmd.Parameters.AddWithValue("p_session_start", sessionStart);
cmd.Parameters.AddWithValue("p_session_end", sessionEnd);
cmd.Parameters.AddWithValue("p_clock_time_seconds", clockTimeSeconds);
cmd.Parameters.AddWithValue("p_outlines", outlines);
cmd.Parameters.AddWithValue("p_features", features);
await cmd.ExecuteNonQueryAsync();
```

**Flush timeout**: The flush call should use an aggressive timeout (3-5 seconds) to avoid blocking app shutdown. WinUI gives limited time before killing the process; the transaction ensures partial calls roll back cleanly.

**Prerequisite**: ScaleGrid MySQL version must be 8.0.19+ for `JSON_TABLE` and row alias `ON DUPLICATE KEY UPDATE` syntax. Verify before implementation.

### 2.7 Privacy

- No story content is ever transmitted — only metadata (genre, element counts, feature names)
- **Usage data is unlinkable to user identity.** The `usage_id` is a random GUID generated locally, stored only in local preferences, with no foreign key or lookup table connecting it to the `users` table. The database cannot determine which user produced which usage data, even for administrators.
- Consent is required before any data is collected (`UsageStatsConsent` flag)
- If a user opts out and back in, a new `usage_id` is generated — previous data ages out naturally via retention policy
- **Data retention**: Usage data older than 90 days is purged via a MySQL EVENT running nightly. The event uses batched DELETEs (with LIMIT) to avoid long lock waits. `sessions`, `outline_sessions`, and `feature_usage` are purged via cascade on `sessions.session_start`. `outline_metadata` is purged separately on `created_at`. Verify that ScaleGrid supports `EVENT_SCHEDULER=ON` before implementation.

```sql
-- Purge sessions and cascade to outline_sessions, feature_usage
CREATE EVENT ev_purge_usage_data
  ON SCHEDULE EVERY 1 DAY
  STARTS '2026-01-01 02:00:00'
  DO
  BEGIN
    DELETE s, os, fu
    FROM sessions s
    LEFT JOIN outline_sessions os ON os.session_id = s.session_id
    LEFT JOIN feature_usage    fu ON fu.session_id = s.session_id
    WHERE s.session_start < NOW() - INTERVAL 90 DAY
    LIMIT 5000;

    DELETE FROM outline_metadata
    WHERE created_at < NOW() - INTERVAL 90 DAY
    LIMIT 5000;
  END;
```
- Tradeoff accepted: no per-user usage analysis, no debugging individual user issues via usage data. Aggregate analytics (feature popularity, genre distribution, session duration patterns) are fully supported.
- **`outline_guid` is not a privacy risk.** The GUID uniquely identifies an outline and is stored in both the database and the user's `.stbx` file. However, access to the outline file is the user's responsibility — we don't store the outline name or any content. The GUID is only correlatable by someone who already has access to the user's files, which is outside our threat model.
- Privacy policy update required before shipping — must clearly state that usage data cannot be linked to user identity

### 2.8 UsageTrackingService

#### Interface

```csharp
public interface IUsageTrackingService
{
    // Session lifecycle
    void StartSession();
    Task EndSession();  // flushes accumulated data to MySQL, then discards

    // Outline lifecycle — element counts captured at open and close
    void OutlineOpened(Guid outlineGuid, string genre, string storyForm, int elementCount);
    void OutlineClosed(Guid outlineGuid, int elementCount);

    // Element operations (accumulated as deltas per outline)
    void ElementAdded(StoryItemType type);
    void ElementRemoved();
    void ElementRestored();

    // Feature/tool usage
    void FeatureUsed(string featureName);
}
```

**Unit testability**: Tests use the real `UsageTrackingService` with `TestMySqlIo` injected underneath (the existing mock from `BackendServiceTests.cs`). Tests verify that hooks produce the correct flush payload by inspecting what `TestMySqlIo` received. No additional test doubles needed.

#### Hook Locations

| Hook | File | Method | Line | Call |
|------|------|--------|------|------|
| Session start | `App.xaml.cs` | `OnLaunched()` | ~204 | `StartSession()` |
| Session end | `OutlineViewModel.cs` | `ExitApp()` | 565 | `await EndSession()` |
| Open outline | `FileOpenService.cs` | `OpenFile()` | after model load | `OutlineOpened(guid, genre, form, count)` |
| Create outline | `FileCreateService.cs` | `CreateFile()` | after model created | `OutlineOpened(guid, genre, form, count)` |
| Close outline | `OutlineViewModel.cs` | `CloseFile()` | 519 | `OutlineClosed(guid, count)` |
| Add element | `OutlineViewModel.cs` | `AddStoryElement()` | 1085 | `ElementAdded(typeToAdd)` |
| Remove element | `OutlineViewModel.cs` | `RemoveStoryElement()` | 1126 | `ElementRemoved()` |
| Restore element | `OutlineViewModel.cs` | `RestoreStoryElement()` | 1249 | `ElementRestored()` |
| Collaborator | `ShellViewModel.cs` | `LaunchCollaborator()` | 985 | `FeatureUsed("Collaborator")` |
| Key Questions | `OutlineViewModel.cs` | `KeyQuestionsTool()` | 788 | `FeatureUsed("KeyQuestions")` |
| Topics | `OutlineViewModel.cs` | `TopicsTool()` | 813 | `FeatureUsed("Topics")` |
| Master Plots | `OutlineViewModel.cs` | `MasterPlotTool()` | 838 | `FeatureUsed("MasterPlots")` |
| Dramatic Situations | `OutlineViewModel.cs` | `DramaticSituationsTool()` | 912 | `FeatureUsed("DramaticSituations")` |
| Stock Scenes | `OutlineViewModel.cs` | `StockScenesTool()` | 1003 | `FeatureUsed("StockScenes")` |
| Scrivener Export | `OutlineViewModel.cs` | `GenerateScrivenerReports()` | 735 | `FeatureUsed("ScrivenerExport")` |
| Print | `OutlineViewModel.cs` | `PrintCurrentNodeAsync()` | 771 | `FeatureUsed("Print")` |
| Search | `SearchService.cs` | `SearchString()` | 33 | `FeatureUsed("Search")` |

#### Element Count Discussion

**What we capture**: Element count at outline open and element count at outline close. The delta tells us how many elements were structurally added or removed during that editing session.

**What we don't capture**: Edits to existing elements. A writer may open an outline with 50 elements, spend two hours fleshing out character backstories and scene descriptions, and close with the same 50 elements. The current design records zero change.

**This is a known gap.** Editing existing elements is likely the bulk of a writer's work, and the current instrumentation doesn't reflect it. Possible approaches (not designed here):
- Count saves or dirty-flag transitions per element
- Track which element types were navigated to / viewed
- Record time spent on each element page

**Decision needed**: Does edit tracking belong in this issue's scope, or is it a future enhancement? Flag for discussion during design review with Jake.

### 2.9 Open Questions

1. ~~Should element counts by type be stored, or just totals?~~ **Decided: by type.**
2. ~~How granular should feature tracking be?~~ **Decided: count per tool per session. Aggregated across sessions for totals.**
3. Does outline metadata (genre, form, counts) cross the "not referenced" line in the current privacy policy, even without content? **Probably not, but this is a privacy/ethics question, not a code security question. Flag for discussion during design review with Jake.**
4. ~~What's the opt-in rate for existing consent flags?~~ **Decided: no automatic opt-in. Add the consent switch to Preferences; use #1377 (admin messaging) to send a one-time message inviting users to opt in.**
5. ~~Flush strategy: session-end only, or periodic during long sessions?~~ **Decided: one flush at session end.**
6. Retention window: 90 days proposed. Is that the right balance between analytical value and data minimization?

---

## 3. Related Documents

- `devdocs/issue_1333_opentelemetry_research.md` — OpenTelemetry evaluation (rejected)
- `devdocs/issue_1333_instrumentation_points.md` — Codebase instrumentation map
- `devdocs/issue_1333_status_log.md` — Session-by-session progress log
- `devdocs/issue_1333_tdd_guide.md` — TDD guide for UsageTrackingService (test cases, mock boundaries, examples)
- `/mnt/c/temp/issue_1297_subscription_tiers/` — Collaborator billing (shares ScaleGrid backend)
