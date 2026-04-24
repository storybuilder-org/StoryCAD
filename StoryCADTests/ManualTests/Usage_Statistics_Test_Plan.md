# Usage Statistics Manual Test Plan
## End-to-End Verification of Opt-In Telemetry

This test plan exercises the opt-in usage telemetry added in PR #1380
(issue #1333).
It verifies the full path: consent → in-memory accumulation →
session-end flush → MySQL rows written → purge behavior.

Unit tests (`UsageTrackingServiceTests`, `BackendServiceTests`) cover the
service in isolation. This plan covers the integration points a mock
cannot: the Preferences dialog / Init page wiring, the DI registration,
the `BackendService.SetConnectionString` test-connection path, the
Shell/App close flow, and the actual stored procedure execution.

---

## Environment Setup

### Required

1. **Local test database running.** See `StoryCADTests/TestDb/README.md`
   for full details; the minimum is:

   ```bash
   # From StoryCADTests/TestDb/
   docker compose up -d
   docker ps                                              # container up
   docker compose exec mysql mysql -ustbtest -p123 StoryBuilder -e "SHOW TABLES;"
   ```

   The `SHOW TABLES` check should list `users`, `preferences`, `versions`,
   `sessions`, `outline_sessions`, `outline_metadata`, `feature_usage`,
   `schema_version`. If `docker compose up -d` isn't run first, `exec`
   fails with `service "mysql" is not running`.

2. **`STORYCAD_TEST_CONNECTION` environment variable set** to the value
   documented in that README. Restart the IDE afterward.
3. **Launch StoryCAD and confirm the log contains:**
   `Using local test database connection`
   If it doesn't, the env var wasn't picked up — fix before continuing.

### Verifying the database between tests

The README gives the base commands. For this test plan, you'll also run
inspection queries between steps. From the `TestDb` folder:

```bash
docker compose exec mysql mysql -ustbtest -p123 StoryBuilder
```

`mysql` appears twice on purpose: the first is the compose **service
name** (from `docker-compose.yml`), the second is the MySQL **client
binary** to run inside the container. `StoryBuilder` is case-sensitive.

Useful queries (run inside the MySQL prompt):

```sql
-- Session summary
SELECT session_id, usage_id, session_start, session_end, clock_time_seconds
  FROM sessions ORDER BY session_id DESC LIMIT 5;

-- Outline sessions for the latest session
SELECT os.* FROM outline_sessions os
  WHERE session_id = (SELECT MAX(session_id) FROM sessions);

-- Outline metadata (upserted per outline_guid)
SELECT usage_id, outline_guid, genre, story_form, element_count, last_updated
  FROM outline_metadata ORDER BY last_updated DESC LIMIT 10;

-- Feature counts for the latest session
SELECT feature_name, use_count FROM feature_usage
  WHERE session_id = (SELECT MAX(session_id) FROM sessions);

-- Preferences backend sync
SELECT user_id, elmah_consent, newsletter_consent, usage_consent, version
  FROM preferences;

-- Clean slate between tests (optional; resets all usage data)
TRUNCATE feature_usage;
TRUNCATE outline_sessions;
DELETE FROM sessions;
TRUNCATE outline_metadata;
```

### Preferences model inspection

The client-side consent state and `UsageId` are persisted in
`Preferences.json`. Several test cases inspect this file to verify
invariants that `PreferencesIO.WritePreferences` enforces.

**Windows (packaged StoryCAD — Store/MSIX build):**
```
C:\Users\<you>\AppData\Local\Packages\34432StoryBuilder.StoryBuilder_mty98bvf7kaq2\RoamingState\StoryCAD\Preferences.json
```

**Windows (unpackaged build):**
```
%APPDATA%\StoryCAD\Preferences.json
```
(i.e. `C:\Users\<you>\AppData\Roaming\StoryCAD\Preferences.json`)

**macOS:**
```
~/Library/Application Support/StoryCAD/Preferences.json
```

The file resolves via `ApplicationData.Current.RoamingFolder` plus
`StoryCAD\`. Use whichever path corresponds to the StoryCAD build under
test. Open in any text editor — it's plain JSON.

### Naming map for the consent flag

The same logical value lives under different names in four places.
When a test step refers to one, this table tells you what to look for
in the other three:

| Location | Identifier | Type / values |
|----------|-----------|---------------|
| `Preferences.json` (file) | `UsageStatsConsent` | JSON bool: `true` / `false` |
| `PreferencesModel` (C#) | `UsageStatsConsent` | `bool` |
| `spAddOrUpdatePreferences` (SP parameter) | `usage_stats` | `BOOL` |
| `preferences` table (column) | `usage_consent` | TINYINT(1): `1` / `0` |

The client writes `UsageStatsConsent` to its JSON file, sends it as
`usage_stats` to the stored procedure, which stores it in the
`usage_consent` column. Same concept, four spellings.

---

## Consent & Identity Tests

### TC-1333-001: First-Run Initialization Consent Checkbox
**Priority:** Critical
**Time:** ~3 minutes
**Focus:** New-user opt-in path via `PreferencesInitialization.xaml`

**Setup:**
- Delete or rename `Preferences.json` so StoryCAD treats this as a first run.

**Steps:**
1. Launch StoryCAD
   **Expected:** Initialization page appears, including a checkbox
   "Send anonymous usage statistics"

2. Check the box, fill in required fields, complete initialization
   **Expected:** App proceeds into normal Shell view

3. Inspect `Preferences.json`
   **Expected:** `"UsageStatsConsent": true` and `"UsageId"` is a
   non-empty GUID string

**Pass Criteria:** Consent stored, `UsageId` generated automatically

---

### TC-1333-002: Preferences Dialog Consent Toggle
**Priority:** Critical
**Time:** ~3 minutes
**Focus:** Existing-user opt-in via `PreferencesDialog.xaml`

**Steps:**
1. Launch StoryCAD with consent currently **off**
2. Open Tools > Preferences
   **Expected:** Checkbox "Send anonymous usage statistics" is
   unchecked

3. Check it and click OK/Save
4. Inspect `Preferences.json`
   **Expected:** `UsageStatsConsent: true`, `UsageId` is a fresh GUID

5. Reopen Preferences, uncheck the box, save
6. Inspect `Preferences.json`
   **Expected:** `UsageStatsConsent: false`, `UsageId` is empty string

**Pass Criteria:** `PreferencesIO.WritePreferences` enforces the
consent ↔ `UsageId` invariant in both directions

---

### TC-1333-003: Re-Opt-In Generates a Fresh UsageId
**Priority:** High
**Time:** ~2 minutes
**Focus:** Revoked consent followed by new consent produces a new ID
(no reuse of prior identifier)

**Steps:**
1. With consent on, note the current `UsageId` from `Preferences.json`
2. Open Preferences, turn consent off, save
3. Open Preferences, turn consent on, save
4. Inspect `Preferences.json`
   **Expected:** `UsageId` is a new GUID, not the original

**Pass Criteria:** Opt-out wipes the ID; next opt-in generates a
fresh one

---

### TC-1333-004: Preferences Backend Sync Includes usage_consent
**Priority:** High
**Time:** ~2 minutes
**Focus:** `BackendService.PostPreferences` writes the new
`usage_consent` column via `spAddOrUpdatePreferences`

**Setup:**
- Note the email address entered during first-run Init — it's how
  you'll identify your row in the database. The seed users
  (alice@test.local, bob@test.local, carol@test.local) are a
  separate set and won't match.

**Steps:**
1. In StoryCAD, open Tools > Preferences. Ensure the "Send anonymous
   usage statistics" checkbox is checked.
2. Click Save. This triggers `BackendService.PostPreferences`
   directly (see naming-map note: it sends `usage_stats=true`, which
   lands in column `usage_consent`).
3. Query the `preferences` table joined to `users` by your email:

   ```sql
   SELECT u.email, p.usage_consent, p.elmah_consent, p.newsletter_consent, p.version
     FROM preferences p
     JOIN users u ON p.user_id = u.id
    WHERE u.email = '<email you entered during first-run Init>';
   ```

   **Expected:** One row. `usage_consent = 1`.

4. Reopen Preferences, uncheck the consent box, click Save.
5. Re-run the same query.
   **Expected:** `usage_consent = 0` for the same email.

**Pass Criteria:** Toggling consent in the Preferences dialog
immediately mirrors to the server-side `preferences.usage_consent`
column within the same session. No restart required — `PostPreferences`
fires on Save.

---

## Session Lifecycle Tests

### TC-1333-005: Baseline Session — Launch and Close, No Activity
**Priority:** Critical
**Time:** ~2 minutes
**Focus:** `StartSession` / `EndSession` with no outline or feature work

**Setup:**
- Consent on
- Run the "clean slate" SQL from Environment Setup to empty usage tables

**Steps:**
1. Launch StoryCAD
2. Do not open, create, or edit anything
3. Close the app via the window X
4. Query `sessions`
   **Expected:** One new row. `usage_id` matches `Preferences.json`,
   `session_start` and `session_end` are within seconds of real time,
   `clock_time_seconds >= 0`

5. Query `outline_sessions`, `outline_metadata`, `feature_usage`
   **Expected:** No new rows

**Pass Criteria:** A minimal session produces exactly one sessions
row and nothing else

---

### TC-1333-006: Create Outline, Add Elements, Close
**Priority:** Critical
**Time:** ~4 minutes
**Focus:** `CreateFile` → `OutlineOpened`, `AddStoryElement` → `ElementAdded`,
`CloseFile` → `OutlineClosed`, end-of-session flush

**Setup:** Clean slate

**Steps:**
1. Launch StoryCAD
2. File > New Story, save as `TC1333_006.stbx`, fill in Story Name and
   a Genre
3. Add one Character element, one Problem, one Scene
4. File > Close
5. Close StoryCAD (window X)
6. Query `sessions` — one row
7. Query `outline_sessions` for that session
   **Expected:** One row. `outline_guid` matches the outline's Overview
   `Uuid`. `elements_added = 3` (or 4 if the overview itself counts — see
   note). `elements_deleted = 0`. `open_time` and `close_time` populated.

8. Query `outline_metadata`
   **Expected:** One row with `genre` set, `story_form` set,
   `element_count` reflecting the final outline size.

**Pass Criteria:** Full lifecycle captured end to end

**Note:** The design specifies `ElementAdded` increments on user-initiated
adds only. The trash-bin creation on new outlines is not a user add.
Confirm the count matches user adds only.

---

### TC-1333-007: Open Existing Outline — Metadata Captured
**Priority:** High
**Time:** ~3 minutes
**Focus:** `FileOpenService.OpenFile` triggers `OutlineOpened` with
genre/story_form/element_count

**Setup:** Clean slate. Pre-existing `.stbx` file with known Genre
and Story Type.

**Steps:**
1. Launch StoryCAD
2. File > Open, select the known outline
3. Do not add or remove elements
4. File > Close
5. Close StoryCAD
6. Query `outline_metadata`
   **Expected:** Row with `genre`, `story_form`, and `element_count`
   matching the file's Overview values

**Pass Criteria:** Opening an outline without editing still records
its metadata

---

### TC-1333-008: Multiple Outlines in One Session
**Priority:** High
**Time:** ~4 minutes
**Focus:** `OutlineOpened` auto-closes the previous outline when a
second one opens

**Setup:** Clean slate. Two pre-existing `.stbx` files (A and B).

**Steps:**
1. Launch StoryCAD
2. Open A
3. Add one element to A
4. File > Open B (without closing A first)
5. Add one element to B
6. Close StoryCAD
7. Query `outline_sessions` for the session
   **Expected:** Two rows (A and B). A's `close_time` is populated
   (auto-closed when B opened). Each has `elements_added = 1`.

8. Query `outline_metadata`
   **Expected:** Two rows, one per `outline_guid`, each with its own
   metadata

**Pass Criteria:** Switching outlines mid-session correctly splits
stats per outline

---

### TC-1333-009: Close Via File > Exit (Not Window X)
**Priority:** Critical
**Time:** ~2 minutes
**Focus:** `OutlineViewModel.ExitApp` flushes telemetry (important on
macOS where `Application.Current.Exit` doesn't fire `AppWindow.Closing`)

**Setup:** Clean slate

**Steps:**
1. Launch StoryCAD
2. Open an outline, add one element
3. Use File > Exit (menu item, not window X)
4. Query `sessions` and `outline_sessions`
   **Expected:** One sessions row, one outline_sessions row with the
   expected counts

**Pass Criteria:** Menu-exit path flushes just like window-X path.
Verify on both Windows and macOS if possible.

---

### TC-1333-010: Close App with Outline Still Open
**Priority:** High
**Time:** ~2 minutes
**Focus:** `EndSession` auto-closes any still-open outline before flush

**Setup:** Clean slate

**Steps:**
1. Launch StoryCAD
2. Open an outline (do not close it)
3. Add two elements
4. Close StoryCAD (window X)
5. Query `outline_sessions`
   **Expected:** One row. `close_time` is populated (auto-close kicked
   in during `EndSession`). `elements_added = 2`.

**Pass Criteria:** No lost data when user quits without closing the
outline first

---

## Element Counting Tests

### TC-1333-011: Add / Remove / Restore Counts
**Priority:** High
**Time:** ~4 minutes
**Focus:** Increment behavior for each element operation

**Setup:** Clean slate. Open an outline.

**Steps:**
1. Add 3 characters
2. Delete 2 of them (move to trash)
3. Restore 1 from trash
4. Close the outline and the app
5. Query `outline_sessions` for the session
   **Expected:** `elements_added = 4` (3 adds + 1 restore, since the
   design treats restore as add). `elements_deleted = 2`.

**Pass Criteria:** Counts match the design's semantics
(restore increments added, not a separate "restored" metric)

---

## Feature Usage Tests

### TC-1333-012: Feature Use Tracking — All Nine Tools
**Priority:** High
**Time:** ~8 minutes
**Focus:** Each instrumented tool writes to `feature_usage`

**Setup:** Clean slate. Open an outline with enough elements to run
each tool (characters, problems, scenes as appropriate).

**Steps:** Invoke each of the following once, then close the app.

| Tool | Invocation | Expected feature_name |
|------|-----------|----------------------|
| Collaborator | From Shell menu | `Collaborator` |
| KeyQuestions | Tools > Key Questions | `KeyQuestions` |
| Topics | Tools > Topics | `Topics` |
| MasterPlots | Tools > Master Plots | `MasterPlots` |
| DramaticSituations | Tools > Dramatic Situations | `DramaticSituations` |
| StockScenes | Tools > Stock Scenes | `StockScenes` |
| ScrivenerExport | File > Export > Scrivener | `ScrivenerExport` |
| Print | Print current node | `Print` |
| Search | Search within outline | `Search` |

After closing, query `feature_usage` for the session:

**Expected:** Nine rows, one per feature, each with `use_count = 1`.

**Pass Criteria:** All nine feature names land as documented in the
design. Opening a tool and cancelling out should still count as a use
(the instrumentation fires at the command, not at completion).

---

### TC-1333-013: Feature Use Count Accumulates
**Priority:** Medium
**Time:** ~2 minutes
**Focus:** Invoking the same feature multiple times increments the
count in a single row (not multiple rows)

**Setup:** Clean slate. Open an outline.

**Steps:**
1. Open Tools > Key Questions and close
2. Open Tools > Key Questions and close (second time)
3. Open Tools > Key Questions and close (third time)
4. Close StoryCAD
5. Query `feature_usage` for the session
   **Expected:** One row with `feature_name = KeyQuestions` and
   `use_count = 3`

**Pass Criteria:** Aggregation per feature per session, not one row
per invocation

---

## Negative / Consent-Off Tests

### TC-1333-014: Consent OFF — No Database Writes
**Priority:** Critical
**Time:** ~3 minutes
**Focus:** `UsageTrackingService` methods are no-ops when
`UsageStatsConsent` is false

**Setup:** Clean slate. Open Preferences, turn consent off, restart
StoryCAD to ensure in-memory state matches.

**Steps:**
1. Launch StoryCAD (consent off)
2. Open an outline, add elements, run a feature or two
3. Close StoryCAD
4. Query `sessions`, `outline_sessions`, `outline_metadata`,
   `feature_usage`
   **Expected:** No new rows in any of the four tables

**Pass Criteria:** Opt-out completely silences telemetry

---

### TC-1333-015: Consent Revoked Mid-Session
**Priority:** High
**Time:** ~3 minutes
**Focus:** Turning consent off during a session causes `EndSession`
to be a no-op — no partial flush

**Setup:** Clean slate. Start with consent on.

**Steps:**
1. Launch StoryCAD
2. Open an outline, add two elements, run one feature
3. Without closing the outline, open Preferences and turn consent off,
   save
4. Close StoryCAD
5. Query `sessions`
   **Expected:** No new row

**Pass Criteria:** Revoking consent mid-session prevents the flush.
Accumulated in-memory data is discarded, not sent.

---

## Resilience Tests

### TC-1333-016: Database Unreachable — Graceful Degradation
**Priority:** High
**Time:** ~3 minutes
**Focus:** `EndSession` catches SQL exceptions, logs a warning,
does not crash the shutdown path

**Setup:** Consent on. Clean slate.

**Steps:**
1. Launch StoryCAD (confirm backend connection established in log)
2. Open an outline, add an element, run a feature
3. Stop the Docker container: `docker compose stop mysql`
4. Close StoryCAD
   **Expected:** App closes normally, no crash dialog
5. Check StoryCAD log file
   **Expected:** Warning line: "Failed to flush usage data — data
   lost for this session" with the exception captured

6. Restart Docker: `docker compose start mysql`
7. Query `sessions`
   **Expected:** No row from the killed session (the failed flush
   was intentionally lossy; that's by design)

**Pass Criteria:** DB failure does not block shutdown or corrupt
preferences save. Log captures the loss. No retry.

---

### TC-1333-017: Connection Not Configured — Silent No-Op
**Priority:** Medium
**Time:** ~2 minutes
**Focus:** `IsConnectionConfigured == false` path

**Setup:** Temporarily unset `STORYCAD_TEST_CONNECTION` and ensure
no Doppler secrets are reachable (or disconnect network).

**Steps:**
1. Launch StoryCAD (consent on)
2. Do any work
3. Close
   **Expected:** No crash. Log notes backend not configured. Usage
   flush path silently skipped.

4. Restore the env var for subsequent tests.

**Pass Criteria:** Missing backend connection is not fatal; telemetry
is gracefully skipped.

---

## Retention / Purge (Informational)

### TC-1333-018: 90-Day Purge Event
**Priority:** Low (cannot be practically tested in a session)
**Time:** Read-only check
**Focus:** Confirm the `ev_purge_usage_data` event is scheduled and
enabled

**Steps:**
1. In the MySQL shell, run:
   ```sql
   SELECT event_name, status, interval_value, interval_field
     FROM information_schema.events
     WHERE event_schema = 'StoryBuilder';
   ```
   **Expected:** Row for `ev_purge_usage_data`, status `ENABLED`,
   interval `1 DAY`

2. Manually simulate aged data:
   ```sql
   INSERT INTO sessions (usage_id, session_start, session_end, clock_time_seconds)
     VALUES ('test-purge', NOW() - INTERVAL 100 DAY, NOW() - INTERVAL 100 DAY, 0);
   ```
3. Manually invoke the event:
   ```sql
   CALL mysql.rds_set_configuration('event_scheduler', 'ON'); -- no-op if already on
   ALTER EVENT ev_purge_usage_data ENABLE;
   -- Then execute the body manually:
   DELETE FROM sessions WHERE session_start < NOW() - INTERVAL 90 DAY LIMIT 5000;
   ```
4. Confirm the fake row was deleted

**Pass Criteria:** Event exists and enabled. Manual execution of the
body purges aged rows as designed.

---

## Regression Tests Summary

Run these to make sure the usage tracking changes didn't break
anything:

- **Smoke_Test.md** full pass — startup, open/save, element CRUD
- **Preferences_Test_Plan.md** — other consent flags still work
  (elmah, newsletter)
- **Account_Deletion_Test_Plan.md** — `spDeleteUser` still cascades
  correctly (preferences row with new `usage_consent` column)

---

## Test Execution Notes

1. **Run tests with a clean slate between them** — use the
   `TRUNCATE` block in the setup section to reset usage tables
   without wiping the three seed users.

2. **`Preferences.json` location**: see "Preferences model inspection"
   in Environment Setup above for the three possible paths (packaged
   Windows, unpackaged Windows, macOS). Back up the file before
   running consent tests if you care about your current settings.

3. **Critical path (~30 minutes):**
   TC-1333-001, 002, 005, 006, 009, 012, 014, 016. Covers consent,
   basic lifecycle, features, opt-out, and DB failure.

4. **Issues to watch for:**
   - App won't close (Shell.xaml.cs close-then-exit path)
   - Flush happens before `CloseFile` can save outline work
     (telemetry flush is step 1 of `OnApplicationClosing`; this
     is correct but check that outline saves still succeed)
   - `UsageId` persists across an opt-out (leak)
   - Genre/story_form missing when outline has no Overview
   - Feature count shown as zero when it should be ≥1
   - Macos: File > Exit skipping flush

5. **Logs to check:**
   - `%APPDATA%\StoryCAD\Logs\` — look for
     `"Usage data flushed:"`, `"Failed to flush usage data"`,
     `"Usage flush skipped: ..."`
   - Docker: `docker compose logs mysql | tail -50` for
     stored-procedure errors

---

## Sign-off Checklist

- [ ] First-run Init consent checkbox works (TC-001)
- [ ] Preferences dialog toggle works both directions (TC-002)
- [ ] UsageId lifecycle correct: generated on opt-in, cleared on
      opt-out, regenerated on re-opt-in (TC-003)
- [ ] `usage_consent` column mirrors client state (TC-004)
- [ ] Empty session produces one `sessions` row and nothing else
      (TC-005)
- [ ] Create → Add → Close → Exit produces correct rows across all
      four tables (TC-006)
- [ ] Opening an existing outline captures metadata without edits
      (TC-007)
- [ ] Multiple outlines split correctly (TC-008)
- [ ] File > Exit flushes the same as window X (TC-009)
- [ ] EndSession auto-closes a still-open outline (TC-010)
- [ ] Add/Remove/Restore counts match design (TC-011)
- [ ] All 9 features land in `feature_usage` (TC-012)
- [ ] Feature counts aggregate per session (TC-013)
- [ ] Consent OFF writes nothing (TC-014)
- [ ] Revoking mid-session discards data (TC-015)
- [ ] DB unreachable: graceful, logged, non-fatal (TC-016)
- [ ] No connection configured: silent no-op (TC-017)
- [ ] Purge event exists and enabled (TC-018)
- [ ] No regressions in smoke, preferences, or account deletion
      flows

**Tester:** ________________
**Date:** ________________
**Build:** PR #1380 (telemetry branch)
**Windows / macOS:** ________________
**Result:** Pass / Fail (circle one)
