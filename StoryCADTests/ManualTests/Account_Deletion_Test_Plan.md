# Account Deletion Manual Test Plan

**Issue**: #1370 — Apple Guideline 5.1.1(v) compliance
**Feature**: Delete My Data (Preferences > Account tab)

## Prerequisites

### Local MySQL Test Database

The automated tests use `TestMySqlIo` (no database). These manual tests verify the real stored procedures against a local MySQL instance.

1. MySQL 8.0 installed in WSL (not Docker)
2. `StoryBuilderTest` database created from exported live schema (`/mnt/c/temp/StoryBuilder DDL.txt`)
3. Stored procedures applied via `/mnt/c/temp/recreate_stored_procedures.sql`
4. Test users seeded (see SQL below)

**Connection**: `sudo mysql StoryBuilderTest` from WSL terminal

### Seed Test Data

Adapt these for your local DB. Replace names/emails as needed.

```sql
-- Seed a test user
CALL spAddUser('Manual Tester', 'tester@test.local', @uid);
SELECT @uid;

-- Seed preferences for that user
CALL spAddOrUpdatePreferences(@uid, TRUE, TRUE, '4.0.2');

-- Seed a version record
CALL spAddOrUpdateVersion(@uid, '4.0.2', '4.0.1');

-- Verify data exists
SELECT * FROM users WHERE email = 'tester@test.local';
SELECT * FROM preferences WHERE user_id = @uid;
SELECT * FROM versions WHERE user_id = @uid;
```

## Test Cases

### T1: Stored Procedure — spAddOrUpdatePreferences (version update)

**Purpose**: Verify the `version` column actually updates (audit fix for parameter self-reference).

```sql
CALL spAddUser('Version Test', 'vertest@test.local', @uid);
CALL spAddOrUpdatePreferences(@uid, TRUE, FALSE, '4.0.1');
SELECT version FROM preferences WHERE user_id = @uid;
-- Expected: '4.0.1'

CALL spAddOrUpdatePreferences(@uid, TRUE, FALSE, '4.0.2');
SELECT version FROM preferences WHERE user_id = @uid;
-- Expected: '4.0.2' (NOT '4.0.1')
```

- [ ] Version column updates on duplicate key

### T2: Stored Procedure — spDeleteUser (success path)

```sql
CALL spAddUser('Delete Test', 'deltest@test.local', @uid);
CALL spAddOrUpdatePreferences(@uid, TRUE, TRUE, '4.0.2');
CALL spAddOrUpdateVersion(@uid, '4.0.2', '4.0.1');

-- Verify data exists
SELECT COUNT(*) FROM users WHERE id = @uid;        -- Expected: 1
SELECT COUNT(*) FROM preferences WHERE user_id = @uid; -- Expected: 1
SELECT COUNT(*) FROM versions WHERE user_id = @uid;    -- Expected: >= 1

-- Delete
CALL spDeleteUser(@uid, @deleted);
SELECT @deleted;
-- Expected: 1 (TRUE)

-- Verify data is gone
SELECT COUNT(*) FROM users WHERE id = @uid;        -- Expected: 0
SELECT COUNT(*) FROM preferences WHERE user_id = @uid; -- Expected: 0
SELECT COUNT(*) FROM versions WHERE user_id = @uid;    -- Expected: 0
```

- [ ] All three tables cleared for the user
- [ ] `@deleted` returns TRUE

### T3: Stored Procedure — spDeleteUser (invalid user)

```sql
CALL spDeleteUser(999999, @deleted);
SELECT @deleted;
-- Expected: 0 (FALSE) — no user with that ID
```

- [ ] `@deleted` returns FALSE
- [ ] No error thrown

### T4: Stored Procedure — spDeleteUser (user_id = 0)

```sql
CALL spDeleteUser(0, @deleted);
SELECT @deleted;
-- Expected: 0 (FALSE)
```

- [ ] `@deleted` returns FALSE
- [ ] No rows affected

### T5: Stored Procedure — spDeleteUser (NULL user_id)

```sql
CALL spDeleteUser(NULL, @deleted);
SELECT @deleted;
-- Expected: 0 (FALSE)
```

- [ ] `@deleted` returns FALSE

## UI Test Cases (requires running application)

### T6: Tab Structure

1. Open StoryCAD
2. Open Preferences (gear icon or menu)
3. Verify tab order: Account, Save Locations, Backup, Other, About, What's new, [Dev if dev build]

- [ ] "Account" tab shows: first name, surname, email, Delete My Data button
- [ ] "Save Locations" tab shows: project directory browse, backup directory browse
- [ ] "Backup" tab shows: autosave, backup on open, timed backup settings (no directory browse)

### T7: Delete My Data — Cancel

1. Go to Preferences > Account
2. Click "Delete My Data"
3. In the confirmation dialog, click "Cancel"

- [ ] Confirmation dialog appears with correct text
- [ ] Cancel is the default button
- [ ] No data is deleted
- [ ] Preferences dialog remains open

### T8: Delete My Data — Success (backend configured)

**Prerequisite**: App must have a valid backend connection (Doppler keys configured).

1. Go to Preferences > Account
2. Note your name and email
3. Click "Delete My Data"
4. Click "Delete My Data" in confirmation dialog
5. Observe success message

- [ ] Success dialog: "Your data has been deleted. Thank you for using StoryCAD."
- [ ] App closes after clicking OK
- [ ] On next launch, setup wizard appears (PreferencesInitialized = false)
- [ ] Backend database: user, preferences, versions rows are gone

### T9: Delete My Data — Success (backend NOT configured)

**Prerequisite**: App running without Doppler keys (no .env file, or keys empty).

1. Go to Preferences > Account
2. Click "Delete My Data"
3. Click "Delete My Data" in confirmation dialog

- [ ] Success dialog appears (no remote data to delete)
- [ ] App closes
- [ ] Local Preferences.json cleared

### T10: Delete My Data — Backend failure

**Prerequisite**: Simulate by temporarily breaking the backend connection (e.g., disconnect network).

1. Go to Preferences > Account
2. Click "Delete My Data"
3. Click "Delete My Data" in confirmation dialog

- [ ] Failure dialog: "We could not reach our server..."
- [ ] Local data is NOT cleared
- [ ] App remains running
- [ ] User can retry later

## Apple Screen Recording (macOS)

**Purpose**: Capture the deletion flow for App Store Connect review.

1. Build and run on physical Mac device
2. Start screen recording: Cmd+Shift+5
3. Open StoryCAD
4. Navigate to Preferences > Account
5. Show the Delete My Data button
6. Click it
7. Show the confirmation dialog
8. Confirm deletion
9. Show the success message
10. Show the app closing
11. Stop recording

- [ ] Recording saved
- [ ] Upload to App Store Connect > App Review Notes
