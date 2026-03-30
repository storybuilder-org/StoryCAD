# Issue #1370: Account Data Deletion for Apple Guideline 5.1.1(v)

**Branch**: `issue-1370-account-data-deletion`
**Issue**: https://github.com/storybuilder-org/StoryCAD/issues/1370
**Created**: 2026-03-29

## Context

Apple App Store certification rejected StoryCAD (second round) because the app collects user data (name, email) and sends it to a backend MySQL database but provides no self-service way for users to delete that data. This violates Guideline 5.1.1(v) - Data Collection and Storage.

### What Data We Collect

| Location | Data | Purpose |
|----------|------|---------|
| Local `Preferences.json` | First name, surname, email | User identity |
| Backend `users` table | name, email (UNIQUE key) | User identification |
| Backend `preferences` table | elmah_consent, newsletter_consent, version | Telemetry consent |
| Backend `versions` table | current_version, previous_version | Deployment tracking |
| Elmah.io | Error logs (if consented) | Error monitoring |

### Current Architecture

- `PreferencesDialog.xaml` — 7-tab dialog (General, Backup, Other, About, What's new, Dev)
- `PreferencesViewModel.cs` — MVVM ViewModel with validation
- `PreferencesModel.cs` — Data model, serialized to `Preferences.json`
- `BackendService.cs` — Posts to MySQL backend via `MySqlIo.cs`
- `MySqlIo.cs` — DAL with `AddOrUpdateUser` (stored proc), `AddOrUpdatePreferences` (inline SQL), `AddVersion` (inline SQL)
- **ScaleGrid** — DBaaS hosting the MySQL backend (credentials via Doppler)
- **Schema source** — `/mnt/d/dev/src/storybuilder-miscellaneous/mysql/` (STORYBUILDER.sql + STORYBUILDER_ALTERATION1.sql)

## Design: UI Placement

**Tab restructuring**: Rename the "General" tab to **"Account"** and move directory settings to a new **"Save Locations"** tab immediately after Account. This groups personal data (name, email, deletion) together on the Account tab — exactly where Apple expects to find account management — and gives directory configuration its own dedicated space.

### Tab Layout (before → after)

| Before | After |
|--------|-------|
| General (name, email, project dir) | **Account** (name, email, **Delete My Data**) |
| Backup (autosave, backup dir) | **Save Locations** (project dir, backup dir) |
| Other | Backup (autosave, timed backup — no directory pickers) |
| About | Other |
| What's new | About |
| Dev | What's new |
| | Dev |

### UI Flow

```
Account Tab (PreferencesDialog.xaml)
  ├── First name [text field]
  ├── Surname [text field]
  ├── Email [text field]
  └── [Delete My Data] button (red text, bottom of tab)

Save Locations Tab (PreferencesDialog.xaml)
  ├── Project directory [browse]
  └── Backup directory [browse]
         │
         ▼
   Confirmation ContentDialog
   ┌─────────────────────────────────────────┐
   │  Delete My Data?                        │
   │                                         │
   │  This will permanently delete:          │
   │  • Your account from our servers        │
   │  • Error reporting preferences          │
   │  • Version tracking history             │
   │  • Newsletter subscription              │
   │                                         │
   │  Your local story files will NOT be     │
   │  deleted.                               │
   │                                         │
   │  StoryCAD will reset to its initial     │
   │  setup state.                           │
   │                                         │
   │  [Cancel]              [Delete My Data] │
   │  (default)                   (red)      │
   └─────────────────────────────────────────┘
         │
         ▼ (on confirm)
   1. Call backend to delete user record
   2. Revoke elmah.io consent locally
   3. Reset PreferencesInitialized = false
   4. Clear name/email from local Preferences.json
   5. Show success message
   6. Close Preferences dialog
   7. App shows initialization wizard on next launch
```

## Implementation Plan (TDD — Red-Green-Refactor)

Each step follows strict TDD: write a failing test first (RED), then write the minimum production code to pass (GREEN), then refactor if needed.

### Standards (from `/mnt/d/dev/src/.claude/memory/testing.md`)

- **Test file**: `PreferencesViewModelTests.cs` (one test class per production class)
- **Test class**: `PreferencesViewModelTests`
- **Test naming**: `MethodName_Scenario_ExpectedResult`
- **Test flow**: Arrange-Act-Assert
- **TDD cycle**: Red → Green → Refactor

### Existing test infrastructure to reuse

- `TestLogService` — captures log calls for assertion (in `BackendServiceTests.cs`)
- `TestableBackendService` — injectable exception testing (in `BackendServiceTests.cs`)
- IoC container — all services available via `Ioc.Default.GetRequiredService<T>()`

---

## Phase 0: Database Preparation

### Why

- The live ScaleGrid database has ~2,039 users — **never run destructive tests against it**
- `AddOrUpdatePreferences` and `AddVersion` use inline SQL, but `AddOrUpdateUser` uses stored procedure `spAddUser` — this is inconsistent and grants unnecessary direct table access
- The new `DeleteUser` operation is destructive and must use a stored procedure

### 0.1: Export Current Schema from ScaleGrid

The existing schema files (`STORYBUILDER.sql`, `STORYBUILDER_ALTERATION1.sql`) may not reflect the current live schema. Export the current schema (structure only, **no data**) from ScaleGrid via MySQL Workbench to establish the true baseline.

**Exported to**: `/mnt/c/temp/StoryBuilder DDL.txt`

**Schema drift found**: Live `users` table has `first_name`, `last_name`, `email_verified` columns and `user_name` widened to `VARCHAR(128)` — none captured in the stale alteration files.

### 0.2: Set Up Local MySQL Test Database

Using the existing local MySQL installation and MySQL Workbench:

1. Create the `StoryBuilder` database from the exported schema
2. Seed with fake test data (a few test users with preferences and version records)
3. Verify the local database matches the live schema

### 0.3: Write Stored Procedure SQL Scripts

Three new stored procedures to add to the existing `spAddUser`. These scripts are written and tested locally first — they are **not** committed to storybuilder-miscellaneous or applied to ScaleGrid until all testing passes (see Deploy step at end of plan).

**`spAddOrUpdatePreferences`** (replaces inline SQL in `AddOrUpdatePreferences`):

Parameter names use bare names (no `p_` prefix) to match the existing `spAddUser` convention.

```sql
DELIMITER $$

CREATE PROCEDURE spAddOrUpdatePreferences(
    IN user_id INT,
    IN elmah BOOL,
    IN newsletter BOOL,
    IN version VARCHAR(64)
)
BEGIN
    INSERT INTO StoryBuilder.preferences
        (user_id, elmah_consent, newsletter_consent, version)
    VALUES (user_id, elmah, newsletter, version)
    ON DUPLICATE KEY UPDATE
        elmah_consent = elmah,
        newsletter_consent = newsletter,
        version = version;
END$$

DELIMITER ;
```

**`spAddOrUpdateVersion`** (replaces inline SQL in `AddVersion`):

```sql
DELIMITER $$

CREATE PROCEDURE spAddOrUpdateVersion(
    IN user_id INT,
    IN current_ver VARCHAR(64),
    IN previous_ver VARCHAR(64)
)
BEGIN
    INSERT INTO StoryBuilder.versions
        (user_id, current_version, previous_version)
    VALUES (user_id, current_ver, previous_ver)
    ON DUPLICATE KEY UPDATE
        current_version = current_ver,
        previous_version = previous_ver;
END$$

DELIMITER ;
```

**`spDeleteUser`** (new — deletes user and all related data):

```sql
DELIMITER $$

CREATE PROCEDURE spDeleteUser(
    IN email VARCHAR(128)
)
BEGIN
    DECLARE v_user_id INT;

    -- Look up user by email
    SELECT id INTO v_user_id FROM StoryBuilder.users WHERE users.email = email;

    -- If user not found, do nothing
    IF v_user_id IS NOT NULL THEN
        START TRANSACTION;

        -- Delete in foreign key order: children first, then parent
        DELETE FROM StoryBuilder.versions WHERE user_id = v_user_id;
        DELETE FROM StoryBuilder.preferences WHERE user_id = v_user_id;
        DELETE FROM StoryBuilder.users WHERE id = v_user_id;

        COMMIT;
    END IF;
END$$

DELIMITER ;
```

### 0.4: Apply Stored Procedures to Local Test DB

Run all three CREATE PROCEDURE statements against the local test database. Verify they work with the seeded test data.

**NOTE**: The SPs created on 2026-03-29 used `p_` prefixed parameter names. They must be dropped and recreated with bare parameter names (matching `spAddUser` convention) before Phase B0.

### 0.5: Extract `IMySqlIo` Interface

Automated tests in `StoryCADTests` cannot depend on a local MySQL instance — other contributors (e.g., Jake) won't have it, and connection credentials would be a security problem in the repo. Instead, we mock the DAL layer.

**Steps**:
1. Create `IMySqlIo` interface in `StoryCADLib/DAL/` with the 3 existing methods + new `DeleteUser`
2. `MySqlIo` implements `IMySqlIo`
3. Register `IMySqlIo` → `MySqlIo` in IoC (instead of concrete `MySqlIo`)
4. `BackendService` resolves `IMySqlIo` instead of `MySqlIo`

**For automated tests**: Create `TestMySqlIo` (implements `IMySqlIo`) in `BackendServiceTests.cs` — records calls and returns preset results, no database connection. Follows the existing pattern of `TestSaveable : ISaveable` and `MockCollaborator : ICollaborator`.

**For manual verification**: The real stored procedures are tested manually against the local DB. See `StoryCADTests/ManualTests/Account_Deletion_Test_Plan.md`.

---

## Phase A: Full PreferencesViewModel Test Coverage (baseline)

**New file**: `StoryCADTests/ViewModels/PreferencesViewModelTests.cs`

PreferencesViewModel currently has **zero test coverage**. Before adding `DeleteMyDataAsync`, establish a verified baseline for all existing public methods and behaviors.

### A1: LoadModel Tests

```csharp
[TestMethod]
public void LoadModel_WithPopulatedModel_CopiesAllUserFields()
{
    // Arrange
    var preferenceService = Ioc.Default.GetRequiredService<PreferenceService>();
    var vm = Ioc.Default.GetRequiredService<PreferencesViewModel>();
    preferenceService.Model.FirstName = "Jane";
    preferenceService.Model.LastName = "Doe";
    preferenceService.Model.Email = "jane@example.com";

    // Act
    vm.LoadModel();

    // Assert
    Assert.AreEqual("Jane", vm.FirstName);
    Assert.AreEqual("Doe", vm.LastName);
    Assert.AreEqual("jane@example.com", vm.Email);
}

[TestMethod]
public void LoadModel_WithPopulatedModel_CopiesConsentFlags()
{
    // Arrange
    var preferenceService = Ioc.Default.GetRequiredService<PreferenceService>();
    var vm = Ioc.Default.GetRequiredService<PreferencesViewModel>();
    preferenceService.Model.ErrorCollectionConsent = true;
    preferenceService.Model.Newsletter = true;

    // Act
    vm.LoadModel();

    // Assert
    Assert.IsTrue(vm.ErrorCollectionConsent);
    Assert.IsTrue(vm.Newsletter);
}

[TestMethod]
public void LoadModel_WithPopulatedModel_CopiesBackupSettings()
{
    // Arrange
    var preferenceService = Ioc.Default.GetRequiredService<PreferenceService>();
    var vm = Ioc.Default.GetRequiredService<PreferencesViewModel>();
    preferenceService.Model.AutoSave = true;
    preferenceService.Model.AutoSaveInterval = 30;
    preferenceService.Model.BackupOnOpen = true;
    preferenceService.Model.TimedBackup = true;
    preferenceService.Model.TimedBackupInterval = 15;

    // Act
    vm.LoadModel();

    // Assert
    Assert.IsTrue(vm.AutoSave);
    Assert.AreEqual(30, vm.AutoSaveInterval);
    Assert.IsTrue(vm.BackupOnOpen);
    Assert.IsTrue(vm.TimedBackup);
    Assert.AreEqual(15, vm.TimedBackupInterval);
}

[TestMethod]
public void LoadModel_WithPopulatedModel_CopiesDirectoryPaths()
{
    // Arrange
    var preferenceService = Ioc.Default.GetRequiredService<PreferenceService>();
    var vm = Ioc.Default.GetRequiredService<PreferencesViewModel>();
    preferenceService.Model.ProjectDirectory = @"C:\Projects";
    preferenceService.Model.BackupDirectory = @"C:\Backups";

    // Act
    vm.LoadModel();

    // Assert
    Assert.AreEqual(@"C:\Projects", vm.ProjectDirectory);
    Assert.AreEqual(@"C:\Backups", vm.BackupDirectory);
}

[TestMethod]
public void LoadModel_WithPopulatedModel_CopiesDisplaySettings()
{
    // Arrange
    var preferenceService = Ioc.Default.GetRequiredService<PreferenceService>();
    var vm = Ioc.Default.GetRequiredService<PreferencesViewModel>();
    preferenceService.Model.ThemePreference = ElementTheme.Dark;
    preferenceService.Model.PreferredSearchEngine = BrowserType.Google;
    preferenceService.Model.AdvancedLogging = true;
    preferenceService.Model.ShowStartupDialog = false;
    preferenceService.Model.ShowFilePickerOnStartup = true;
    preferenceService.Model.UseBetaDocumentation = true;

    // Act
    vm.LoadModel();

    // Assert
    Assert.AreEqual(2, vm.PreferredThemeIndex); // Dark = 2
    Assert.AreEqual(BrowserType.Google, vm.PreferredSearchEngine);
    Assert.IsTrue(vm.AdvancedLogging);
    Assert.IsFalse(vm.ShowStartupPage);
    Assert.IsTrue(vm.ShowFilePickerOnStartup);
    Assert.IsTrue(vm.UseBetaDocumentation);
}
```

**All these tests should pass immediately (GREEN) — LoadModel already exists.** This establishes the baseline.

### A2: SaveModel Tests

```csharp
[TestMethod]
public void SaveModel_AfterPropertyChanges_WritesUserFieldsBackToModel()
{
    // Arrange
    var preferenceService = Ioc.Default.GetRequiredService<PreferenceService>();
    var vm = Ioc.Default.GetRequiredService<PreferencesViewModel>();
    vm.LoadModel();
    vm.FirstName = "Updated";
    vm.LastName = "Name";
    vm.Email = "updated@example.com";

    // Act
    vm.SaveModel();

    // Assert
    Assert.AreEqual("Updated", preferenceService.Model.FirstName);
    Assert.AreEqual("Name", preferenceService.Model.LastName);
    Assert.AreEqual("updated@example.com", preferenceService.Model.Email);
}

[TestMethod]
public void SaveModel_AfterPropertyChanges_WritesConsentFlagsBackToModel()
{
    // Arrange
    var preferenceService = Ioc.Default.GetRequiredService<PreferenceService>();
    var vm = Ioc.Default.GetRequiredService<PreferencesViewModel>();
    vm.LoadModel();
    vm.ErrorCollectionConsent = true;
    vm.Newsletter = false;

    // Act
    vm.SaveModel();

    // Assert
    Assert.IsTrue(preferenceService.Model.ErrorCollectionConsent);
    Assert.IsFalse(preferenceService.Model.Newsletter);
}

[TestMethod]
public void SaveModel_AfterPropertyChanges_WritesBackupSettingsBackToModel()
{
    // Arrange
    var preferenceService = Ioc.Default.GetRequiredService<PreferenceService>();
    var vm = Ioc.Default.GetRequiredService<PreferencesViewModel>();
    vm.LoadModel();
    vm.AutoSave = true;
    vm.AutoSaveInterval = 45;
    vm.BackupOnOpen = false;
    vm.TimedBackup = true;
    vm.TimedBackupInterval = 30;

    // Act
    vm.SaveModel();

    // Assert
    Assert.IsTrue(preferenceService.Model.AutoSave);
    Assert.AreEqual(45, preferenceService.Model.AutoSaveInterval);
    Assert.IsFalse(preferenceService.Model.BackupOnOpen);
    Assert.IsTrue(preferenceService.Model.TimedBackup);
    Assert.AreEqual(30, preferenceService.Model.TimedBackupInterval);
}

[TestMethod]
public void SaveModel_WhenThemeUnchanged_ThemeChangedIsFalse()
{
    // Arrange
    var preferenceService = Ioc.Default.GetRequiredService<PreferenceService>();
    var vm = Ioc.Default.GetRequiredService<PreferencesViewModel>();
    preferenceService.Model.ThemePreference = ElementTheme.Light;
    vm.LoadModel();
    // Don't change theme

    // Act
    vm.SaveModel();

    // Assert
    Assert.IsFalse(vm.ThemeChanged);
}

[TestMethod]
public void SaveModel_WhenThemeChanged_ThemeChangedIsTrue()
{
    // Arrange
    var preferenceService = Ioc.Default.GetRequiredService<PreferenceService>();
    var vm = Ioc.Default.GetRequiredService<PreferencesViewModel>();
    preferenceService.Model.ThemePreference = ElementTheme.Light;
    vm.LoadModel();
    vm.PreferredThemeIndex = 2; // Dark

    // Act
    vm.SaveModel();

    // Assert
    Assert.IsTrue(vm.ThemeChanged);
    Assert.AreEqual(ElementTheme.Dark, preferenceService.Model.ThemePreference);
}
```

**All should pass immediately (GREEN) — SaveModel already exists.**

### A3: SaveAsync Tests

```csharp
[TestMethod]
public async Task SaveAsync_WithValidModel_PersistsPreferencesToDisk()
{
    // Arrange
    var preferenceService = Ioc.Default.GetRequiredService<PreferenceService>();
    var appState = Ioc.Default.GetRequiredService<AppState>();
    var vm = Ioc.Default.GetRequiredService<PreferencesViewModel>();
    preferenceService.Model.FirstName = "SaveTest";
    preferenceService.Model.Email = "save@example.com";
    vm.LoadModel();

    // Act
    vm.SaveModel();
    await vm.SaveAsync();

    // Assert — read back from disk
    var prefsIo = new PreferencesIo();
    var reloaded = await prefsIo.ReadPreferences();
    Assert.AreEqual("SaveTest", reloaded.FirstName);
    Assert.AreEqual("save@example.com", reloaded.Email);
}

[TestMethod]
public async Task SaveAsync_AfterSave_SetsRecordPreferencesStatusToFalse()
{
    // Arrange
    var preferenceService = Ioc.Default.GetRequiredService<PreferenceService>();
    var vm = Ioc.Default.GetRequiredService<PreferencesViewModel>();
    preferenceService.Model.RecordPreferencesStatus = true;
    vm.LoadModel();

    // Act
    await vm.SaveAsync();

    // Assert — SaveAsync sets this to false to indicate need to update backend
    Assert.IsFalse(preferenceService.Model.RecordPreferencesStatus);
}
```

**Should pass immediately (GREEN) — SaveAsync already exists.**

### A4: Computed Property Tests

```csharp
[TestMethod]
public void SearchEngineIndex_WhenSet_UpdatesPreferredSearchEngine()
{
    // Arrange
    var vm = Ioc.Default.GetRequiredService<PreferencesViewModel>();

    // Act
    vm.SearchEngineIndex = 1; // Google

    // Assert
    Assert.AreEqual(BrowserType.Google, vm.PreferredSearchEngine);
}

[TestMethod]
public void SearchEngineIndex_WhenRead_ReturnsEnumAsInt()
{
    // Arrange
    var vm = Ioc.Default.GetRequiredService<PreferencesViewModel>();
    vm.PreferredSearchEngine = BrowserType.Bing;

    // Act & Assert
    Assert.AreEqual(2, vm.SearchEngineIndex);
}

[TestMethod]
public void PreferredThemeIndex_WhenSet_UpdatesInternalTheme()
{
    // Arrange
    var preferenceService = Ioc.Default.GetRequiredService<PreferenceService>();
    var vm = Ioc.Default.GetRequiredService<PreferencesViewModel>();
    vm.LoadModel();
    vm.PreferredThemeIndex = 1; // Light

    // Act
    vm.SaveModel();

    // Assert
    Assert.AreEqual(ElementTheme.Light, preferenceService.Model.ThemePreference);
}

[TestMethod]
public void AppStoreReviewButtonText_OnWindows_ReturnsWindowsStoreText()
{
    // Arrange
    var vm = Ioc.Default.GetRequiredService<PreferencesViewModel>();

    // Act
    var text = vm.AppStoreReviewButtonText;

    // Assert — test runs on Windows
    if (OperatingSystem.IsWindows())
        Assert.AreEqual("Review StoryCAD on the Microsoft Store", text);
}
```

**All should pass immediately (GREEN).**

### A5: PropertyChanged Notification Tests

```csharp
[TestMethod]
public void FirstName_WhenSet_RaisesPropertyChanged()
{
    // Arrange
    var vm = Ioc.Default.GetRequiredService<PreferencesViewModel>();
    var propertyChanged = false;
    vm.PropertyChanged += (s, e) =>
    {
        if (e.PropertyName == nameof(PreferencesViewModel.FirstName))
            propertyChanged = true;
    };

    // Act
    vm.FirstName = "Changed";

    // Assert
    Assert.IsTrue(propertyChanged);
}

[TestMethod]
public void ErrorCollectionConsent_WhenSet_RaisesPropertyChanged()
{
    // Arrange
    var vm = Ioc.Default.GetRequiredService<PreferencesViewModel>();
    var propertyChanged = false;
    vm.PropertyChanged += (s, e) =>
    {
        if (e.PropertyName == nameof(PreferencesViewModel.ErrorCollectionConsent))
            propertyChanged = true;
    };

    // Act
    vm.ErrorCollectionConsent = true;

    // Assert
    Assert.IsTrue(propertyChanged);
}

[TestMethod]
public void AutoSave_WhenSet_RaisesPropertyChanged()
{
    // Arrange
    var vm = Ioc.Default.GetRequiredService<PreferencesViewModel>();
    var propertyChanged = false;
    vm.PropertyChanged += (s, e) =>
    {
        if (e.PropertyName == nameof(PreferencesViewModel.AutoSave))
            propertyChanged = true;
    };

    // Act
    vm.AutoSave = true;

    // Assert
    Assert.IsTrue(propertyChanged);
}
```

**All should pass immediately (GREEN) — SetProperty already fires PropertyChanged.**

---

## Phase B: Stored Procedure Migration + Deletion (TDD)

### B0: Refactor existing inline SQL to stored procedures

**Prerequisite**: Phase 0 stored procedures (`spAddOrUpdatePreferences`, `spAddOrUpdateVersion`) are recreated on the local test DB with bare parameter names.

**File**: `StoryCADLib/DAL/MySqLIO.cs`

Refactor `AddOrUpdatePreferences` to call `spAddOrUpdatePreferences`:

```csharp
public async Task AddOrUpdatePreferences(MySqlConnection conn, int id, bool elmah, bool newsletter, string version)
{
    await using MySqlCommand cmd = new("spAddOrUpdatePreferences", conn);
    cmd.CommandType = CommandType.StoredProcedure;
    cmd.Parameters.AddWithValue("user_id", id);
    cmd.Parameters.AddWithValue("elmah", elmah);
    cmd.Parameters.AddWithValue("newsletter", newsletter);
    cmd.Parameters.AddWithValue("version", version);
    await cmd.ExecuteNonQueryAsync();
}
```

Refactor `AddVersion` to call `spAddOrUpdateVersion`:

```csharp
public async Task AddVersion(MySqlConnection conn, int id, string currentVersion, string previousVersion)
{
    await using MySqlCommand cmd = new("spAddOrUpdateVersion", conn);
    cmd.CommandType = CommandType.StoredProcedure;
    cmd.Parameters.AddWithValue("user_id", id);
    cmd.Parameters.AddWithValue("current_ver", currentVersion);
    cmd.Parameters.AddWithValue("previous_ver", previousVersion);
    await cmd.ExecuteNonQueryAsync();
}
```

**Verification**: Manually test against local DB to confirm stored procedures behave identically to the inline SQL they replace. The existing `CheckConnection` test uses Doppler/ScaleGrid and is not affected by this change until SPs are deployed to production.

---

### B1: Add DeleteUser to IMySqlIo / MySqlIo

**File**: `StoryCADLib/DAL/MySqLIO.cs`

```csharp
public async Task DeleteUser(MySqlConnection conn, string email)
{
    await using MySqlCommand cmd = new("spDeleteUser", conn);
    cmd.CommandType = CommandType.StoredProcedure;
    cmd.Parameters.AddWithValue("email", email);
    await cmd.ExecuteNonQueryAsync();
}
```

Add `DeleteUser` to the `IMySqlIo` interface as well.

### B2: Add DeleteUserData to BackendService

**File**: `StoryCADLib/Services/Backend/BackendService.cs`

```csharp
public async Task<bool> DeleteUserData(string email)
{
    if (!IsConnectionConfigured)
    {
        _logService.Log(LogLevel.Warn, "Skipping DeleteUserData - backend connection not configured");
        return false;
    }

    _logService.Log(LogLevel.Info, "Deleting user data from backend database");
    var sql = Ioc.Default.GetService<IMySqlIo>();
    MySqlConnection conn = new(connection);

    try
    {
        await conn.OpenAsync();
        await sql.DeleteUser(conn, email);
        _logService.Log(LogLevel.Info, "User data deleted successfully for: " + email);
        return true;
    }
    catch (Exception ex)
    {
        _logService.LogException(LogLevel.Error, ex, "Failed to delete user data: " + ex.Message);
        return false;
    }
    finally
    {
        await conn.CloseAsync();
    }
}
```

### B3: Automated Tests (Mocked — no database)

**Test file**: `StoryCADTests/Services/Backend/BackendServiceTests.cs`

`TestMySqlIo` implements `IMySqlIo` — records calls and returns preset results. Follows the existing `TestSaveable`, `MockCollaborator` patterns. Defined as a private class inside the test file.

```csharp
[TestMethod]
public async Task DeleteUserData_WhenConnectionNotConfigured_ReturnsFalse()
{
    // Arrange
    var testLogger = new TestLogService();
    var appState = Ioc.Default.GetRequiredService<AppState>();
    var preferenceService = Ioc.Default.GetRequiredService<PreferenceService>();
    var backendService = new BackendService(testLogger, appState, preferenceService);
    // IsConnectionConfigured defaults to false (no SetConnectionString called)

    // Act
    var result = await backendService.DeleteUserData("test@example.com");

    // Assert
    Assert.IsFalse(result);
    Assert.IsTrue(testLogger.HasWarning("Skipping DeleteUserData"));
}

[TestMethod]
public async Task DeleteUserData_WhenConnectionConfigured_CallsDeleteAndReturnsTrue()
{
    // Arrange — uses TestMySqlIo (no real database)
    var testLogger = new TestLogService();
    var appState = Ioc.Default.GetRequiredService<AppState>();
    var preferenceService = Ioc.Default.GetRequiredService<PreferenceService>();
    var backendService = new BackendService(testLogger, appState, preferenceService);
    // TODO: Configure backendService with TestMySqlIo registered in IoC

    // Act
    var result = await backendService.DeleteUserData("test@example.com");

    // Assert
    Assert.IsTrue(result);
    // Verify TestMySqlIo.DeleteUser was called with correct email
}

[TestMethod]
public async Task DeleteUserData_WhenDatabaseThrows_ReturnsFalse()
{
    // Arrange — TestMySqlIo configured to throw
    var testLogger = new TestLogService();
    var appState = Ioc.Default.GetRequiredService<AppState>();
    var preferenceService = Ioc.Default.GetRequiredService<PreferenceService>();
    var backendService = new BackendService(testLogger, appState, preferenceService);
    // TODO: Configure TestMySqlIo to throw on DeleteUser

    // Act
    var result = await backendService.DeleteUserData("test@example.com");

    // Assert
    Assert.IsFalse(result);
    Assert.IsTrue(testLogger.HasError("Failed to delete user data"));
}
```

**Real stored procedure behavior is verified manually** — see Phase M (Manual Testing).

---

## Phase B-audit: DAL Security Audit

**Gate**: Must pass before proceeding to Phase C. At this point all 4 stored procedures exist and `MySqlIo.cs` has been fully refactored — audit the complete DAL surface in one pass while findings are still actionable.

### Scope

**All 4 stored procedures** (local test DB + SQL source):
- `spAddUser` (existing — never audited)
- `spAddOrUpdatePreferences` (new)
- `spAddOrUpdateVersion` (new)
- `spDeleteUser` (new)

**All DAL code**:
- `MySqlIo.cs` — all 4 methods
- `BackendService.cs` — connection management, error handling, logging

### Audit Checklist

#### SQL Injection Prevention
- [ ] All MySqlIo methods use parameterized queries or stored procedure parameters (no string concatenation)
- [ ] Stored procedures use only declared parameters (no dynamic SQL / PREPARE / EXECUTE)
- [ ] No user input reaches SQL without parameterization

#### Connection Security
- [ ] SSL/TLS enforced via `SslCa` in connection string
- [ ] Connection string never logged or exposed in error messages
- [ ] Connections disposed properly (`CloseAsync` in `finally` blocks)
- [ ] Temp `.pem` file cleaned up (`DeleteWorkFile`)

#### Error Handling
- [ ] Exception messages logged internally do not leak schema details to users
- [ ] User-facing error messages are generic ("could not reach our server")
- [ ] MySQL error codes handled appropriately (transient vs. permanent)

#### Database User Permissions
- [ ] After SP migration, DB user should only need EXECUTE permission
- [ ] Document current DB user permissions on ScaleGrid
- [ ] Recommend removing direct INSERT/UPDATE/DELETE grants if present

#### Input Validation
- [ ] Email validated before reaching DAL (PreferencesViewModel validation attributes)
- [ ] Name fields have length constraints matching DB column sizes (VARCHAR(64) for user_name, VARCHAR(128) for email)
- [ ] Version strings have length constraints matching DB column sizes (VARCHAR(64))

#### spDeleteUser Specific
- [ ] Transaction wraps all three DELETEs (verified in procedure)
- [ ] Foreign key deletion order correct (versions → preferences → users)
- [ ] Non-existent email handled gracefully (no error, no-op)
- [ ] Cannot be called without authentication context (only reachable from authenticated app)

### Output

Document findings in a comment on issue #1370. Fix any critical/high issues before proceeding. Medium/low issues can be tracked as follow-up if they don't affect the 4.0.2 release timeline.

---

## Phase C: PreferencesViewModel.DeleteMyDataAsync (TDD — new code)

### C1: RED — DeleteMyDataAsync tests

**Test file**: `StoryCADTests/ViewModels/PreferencesViewModelTests.cs` (same file from Phase A)

```csharp
[TestMethod]
public async Task DeleteMyDataAsync_WithPopulatedModel_ResetsUserFields()
{
    // Arrange
    var preferenceService = Ioc.Default.GetRequiredService<PreferenceService>();
    var vm = Ioc.Default.GetRequiredService<PreferencesViewModel>();
    preferenceService.Model.FirstName = "Test";
    preferenceService.Model.LastName = "User";
    preferenceService.Model.Email = "test@example.com";
    preferenceService.Model.PreferencesInitialized = true;
    vm.LoadModel();
    vm.Email = "test@example.com";

    // Act
    await vm.DeleteMyDataAsync();

    // Assert
    Assert.AreEqual(string.Empty, preferenceService.Model.FirstName);
    Assert.AreEqual(string.Empty, preferenceService.Model.LastName);
    Assert.AreEqual(string.Empty, preferenceService.Model.Email);
    Assert.IsFalse(preferenceService.Model.PreferencesInitialized);
}

[TestMethod]
public async Task DeleteMyDataAsync_WithConsentEnabled_RevokesAllConsent()
{
    // Arrange
    var preferenceService = Ioc.Default.GetRequiredService<PreferenceService>();
    var vm = Ioc.Default.GetRequiredService<PreferencesViewModel>();
    preferenceService.Model.ErrorCollectionConsent = true;
    preferenceService.Model.Newsletter = true;
    vm.LoadModel();

    // Act
    await vm.DeleteMyDataAsync();

    // Assert
    Assert.IsFalse(preferenceService.Model.ErrorCollectionConsent);
    Assert.IsFalse(preferenceService.Model.Newsletter);
}

[TestMethod]
public async Task DeleteMyDataAsync_WithPendingBackendSync_ResetsStatusFlags()
{
    // Arrange
    var preferenceService = Ioc.Default.GetRequiredService<PreferenceService>();
    var vm = Ioc.Default.GetRequiredService<PreferencesViewModel>();
    preferenceService.Model.RecordPreferencesStatus = true;
    preferenceService.Model.RecordVersionStatus = true;
    vm.LoadModel();

    // Act
    await vm.DeleteMyDataAsync();

    // Assert
    Assert.IsFalse(preferenceService.Model.RecordPreferencesStatus);
    Assert.IsFalse(preferenceService.Model.RecordVersionStatus);
}

[TestMethod]
public async Task DeleteMyDataAsync_AfterDeletion_PersistsClearedStateToDisk()
{
    // Arrange
    var preferenceService = Ioc.Default.GetRequiredService<PreferenceService>();
    var vm = Ioc.Default.GetRequiredService<PreferencesViewModel>();
    preferenceService.Model.FirstName = "Persist";
    preferenceService.Model.Email = "persist@example.com";
    preferenceService.Model.PreferencesInitialized = true;
    vm.LoadModel();
    vm.Email = "persist@example.com";

    // Act
    await vm.DeleteMyDataAsync();

    // Assert — read back from disk
    var prefsIo = new PreferencesIo();
    var reloaded = await prefsIo.ReadPreferences();
    Assert.AreEqual(string.Empty, reloaded.FirstName);
    Assert.AreEqual(string.Empty, reloaded.Email);
    Assert.IsFalse(reloaded.PreferencesInitialized);
}
```

**Build → RED (DeleteMyDataAsync does not exist)**

### C1: GREEN — Add DeleteMyDataAsync to PreferencesViewModel

**File**: `StoryCADLib/ViewModels/Tools/PreferencesViewModel.cs`

```csharp
public async Task<bool> DeleteMyDataAsync()
{
    // 1. Delete from backend
    bool backendDeleted = await _backendService.DeleteUserData(Email);

    // 2. Reset local preferences regardless of backend result
    CurrentModel.FirstName = string.Empty;
    CurrentModel.LastName = string.Empty;
    CurrentModel.Email = string.Empty;
    CurrentModel.ErrorCollectionConsent = false;
    CurrentModel.Newsletter = false;
    CurrentModel.PreferencesInitialized = false;
    CurrentModel.RecordPreferencesStatus = false;
    CurrentModel.RecordVersionStatus = false;

    // 3. Save cleared preferences to disk
    PreferencesIo prfIo = new();
    await prfIo.WritePreferences(CurrentModel);
    _preferenceService.Model = CurrentModel;

    return backendDeleted;
}
```

**Build + run tests → GREEN**

---

## Phase D: UI Changes (no TDD — XAML is manually tested)

### D1. Rename General tab to Account, remove Project directory browse

**File**: `StoryCADLib/Services/Dialogs/Tools/PreferencesDialog.xaml`

```xml
<TabViewItem Header="Account" IsClosable="False" VerticalContentAlignment="Stretch" VerticalAlignment="Center">
    <StackPanel>
        <TextBox Header="Your first name:" ... />
        <TextBox Header="Your surname:" ... />
        <TextBox Header="Your email:" ... />
        <Button Content="Delete My Data"
                Foreground="Red"
                HorizontalAlignment="Center"
                Margin="8,16,8,8"
                Click="DeleteMyData_Click" />
    </StackPanel>
</TabViewItem>
```

### D2. Add new Save Locations tab after Account (before Backup)

```xml
<TabViewItem Header="Save Locations" IsClosable="False" VerticalContentAlignment="Center" VerticalAlignment="Center">
    <StackPanel>
        <controls:BrowseTextBox
            HorizontalAlignment="Right"
            Header="Project directory:"
            PlaceholderText="Where do you want to store your stories?"
            Path="{x:Bind PreferencesVm.ProjectDirectory, Mode=TwoWay}"
            BrowseMode="Folder"
            PathSelected="OnProjectPathSelected" />
        <controls:BrowseTextBox
            x:Name="BackupDirBrowse"
            HorizontalAlignment="Right"
            Header="Backup directory:"
            PlaceholderText="Where do you want to store your backups?"
            Path="{x:Bind PreferencesVm.BackupDirectory, Mode=TwoWay}"
            BrowseMode="Folder"
            PathSelected="OnBackupPathSelected" />
    </StackPanel>
</TabViewItem>
```

### D3. Remove Backup directory browse from the Backup tab (moved to Save Locations)

### D4. Add click handler in code-behind

**File**: `StoryCADLib/Services/Dialogs/Tools/PreferencesDialog.xaml.cs`

```csharp
private async void DeleteMyData_Click(object sender, RoutedEventArgs e)
{
    ContentDialog confirmDialog = new()
    {
        Title = "Delete My Data?",
        Content = "This will permanently delete:\n" +
                  "• Your account from our servers\n" +
                  "• Error reporting preferences\n" +
                  "• Version tracking history\n" +
                  "• Newsletter subscription\n\n" +
                  "Your local story files will NOT be deleted.\n\n" +
                  "StoryCAD will reset to its initial setup state.",
        PrimaryButtonText = "Delete My Data",
        CloseButtonText = "Cancel",
        DefaultButton = ContentDialogButton.Close,
        XamlRoot = this.XamlRoot
    };

    var result = await confirmDialog.ShowAsync();
    if (result == ContentDialogResult.Primary)
    {
        bool success = await PreferencesVm.DeleteMyDataAsync();

        ContentDialog resultDialog = new()
        {
            Title = success ? "Data Deleted" : "Partial Deletion",
            Content = success
                ? "Your data has been deleted. StoryCAD will show the setup wizard on next launch."
                : "Your local data has been cleared, but we could not reach our server. " +
                  "Please try again later, or contact support@storybuilder.org.",
            CloseButtonText = "OK",
            XamlRoot = this.XamlRoot
        };
        await resultDialog.ShowAsync();
    }
}
```

### D5. Close the Preferences Dialog After Deletion

The PreferencesDialog is shown as a ContentDialog from `ShellViewModel.OpenPreferences()`. The click handler returns naturally, and the calling code in Shell handles the dialog result. No additional work needed.

---

## TDD Execution Order Summary

| # | Phase | What | File |
|---|-------|------|------|
| 0.1 | SETUP | Export current schema from ScaleGrid (structure only, no data) | `/mnt/c/temp/StoryBuilder DDL.txt` |
| 0.2 | SETUP | Create local test DB from exported schema, seed fake test data | MySQL Workbench |
| 0.3 | SETUP | Write 3 stored procedure SQL scripts (bare parameter names) | Local SQL files |
| 0.4 | SETUP | Apply stored procedures to local test DB | MySQL Workbench |
| 0.4a | FIX | Recreate local SPs with bare parameter names (were `p_` prefixed) | MySQL Workbench |
| 0.5 | SETUP | Extract `IMySqlIo` interface, register in IoC | IMySqlIo.cs, MySqLIO.cs, IoC registration |
| A1 | GREEN | LoadModel tests (5 tests — baseline, existing code) | PreferencesViewModelTests.cs (new) |
| A2 | GREEN | SaveModel tests (5 tests — baseline, existing code) | PreferencesViewModelTests.cs |
| A3 | GREEN | SaveAsync tests (2 tests — baseline, existing code) | PreferencesViewModelTests.cs |
| A4 | GREEN | Computed property tests (4 tests — baseline) | PreferencesViewModelTests.cs |
| A5 | GREEN | PropertyChanged tests (3 tests — baseline) | PreferencesViewModelTests.cs |
| B0 | REFACTOR | Convert `AddOrUpdatePreferences` + `AddVersion` to stored procedure calls | MySqLIO.cs |
| B1 | CODE | Add `DeleteUser` to `IMySqlIo` / `MySqlIo` | IMySqlIo.cs, MySqLIO.cs |
| B2 | CODE | Add `DeleteUserData` to `BackendService` | BackendService.cs |
| B3 | TEST | Mocked delete tests (3 tests) using `TestMySqlIo` — no database | BackendServiceTests.cs |
| B-audit | GATE | DAL security audit: all 4 SPs, MySqlIo.cs, BackendService.cs | Issue #1370 comment |
| C1 | RED→GREEN | DeleteMyDataAsync tests (4 tests) | PreferencesViewModelTests.cs + PreferencesViewModel.cs |
| D1-D5 | UI | XAML tab restructure + Delete button + click handler | PreferencesDialog.xaml/.cs |
| M | MANUAL | Run manual test script against local DB (SP verification + end-to-end) | ManualTests/Account_Deletion_Test_Plan.md |
| — | POST | Commit STORYBUILDER_ALTERATION2.sql to storybuilder-miscellaneous | storybuilder-miscellaneous/mysql/ |
| — | POST | Replace stale schema files with exported schema | storybuilder-miscellaneous/mysql/ |
| — | DEPLOY | Apply stored procedures to live ScaleGrid | MySQL Workbench |
| — | DEPLOY | Verify existing functionality against live ScaleGrid | Manual |
| — | MANUAL | End-to-end on Mac device, screen recording (Cmd+Shift+5) | Physical Mac |
| — | SUBMIT | Upload screen recording to App Store Connect | App Store Connect |

## Files Changed (Summary)

| File | Change |
|------|--------|
| `storybuilder-miscellaneous/mysql/STORYBUILDER_ALTERATION2.sql` | **New file** (committed post-testing) — 3 stored procedures |
| `storybuilder-miscellaneous/mysql/` (schema files) | Replaced with exported current schema from ScaleGrid (`/mnt/c/temp/StoryBuilder DDL.txt`) |
| `StoryCADTests/ManualTests/Account_Deletion_Test_Plan.md` | **New file** — manual DB testing checklist + Apple screen recording procedure |
| `StoryCADTests/ViewModels/PreferencesViewModelTests.cs` | **New file** — full VM coverage (~19 tests) + deletion tests |
| `StoryCADTests/Services/Backend/BackendServiceTests.cs` | Add `TestMySqlIo`, mocked deletion tests (B3) |
| `StoryCADLib/DAL/IMySqlIo.cs` | **New file** — interface for `MySqlIo` (Phase 0.5) |
| `StoryCADLib/DAL/MySqLIO.cs` | Implement `IMySqlIo`, refactor to stored procs, add `DeleteUser()` |
| `StoryCADLib/Services/Backend/BackendService.cs` | Resolve `IMySqlIo` instead of `MySqlIo`, add `DeleteUserData()` |
| `StoryCADLib/ViewModels/Tools/PreferencesViewModel.cs` | Add `DeleteMyDataAsync()` method |
| `StoryCADLib/Services/Dialogs/Tools/PreferencesDialog.xaml` | Rename General→Account, add Save Locations tab, move directories, add Delete My Data button |
| `StoryCADLib/Services/Dialogs/Tools/PreferencesDialog.xaml.cs` | Add `DeleteMyData_Click` handler with confirmation dialog |
| IoC registration (likely `App.xaml.cs` or service registration) | Register `IMySqlIo` → `MySqlIo` |

## Manual Testing

See `StoryCADTests/ManualTests/Account_Deletion_Test_Plan.md` for the full manual testing checklist including the Apple screen recording procedure (use macOS built-in Cmd+Shift+5).

## Post-Testing Deployment

After all phases pass (A through D):

1. **Commit `STORYBUILDER_ALTERATION2.sql`** to `storybuilder-miscellaneous/mysql/` with all three new stored procedures
2. **Update schema files** in storybuilder-miscellaneous to reflect the exported current schema (from Phase 0.1)
3. **Apply stored procedures to live ScaleGrid** via MySQL Workbench
4. **Verify** existing app functionality against live ScaleGrid (PostPreferences, PostVersion still work)
5. **Submit to App Store Connect** with screen recording

## Edge Cases

| Case | Handling |
|------|----------|
| Backend unreachable | Local data cleared, user told to try again later or contact support |
| Backend connection not configured (no Doppler keys) | `DeleteUserData` returns false, local cleanup still happens |
| User clicks Delete then cancels | Cancel is the default button; no action taken |

## Apple Submission Checklist

- [ ] General tab renamed to Account
- [ ] New Save Locations tab with Project directory and Backup directory
- [ ] Backup directory moved from Backup tab to Save Locations tab
- [ ] "Delete My Data" button visible in Preferences > Account
- [ ] Confirmation dialog explains consequences clearly
- [ ] Deletion works end-to-end (backend + local)
- [ ] Screen recording captured on physical Mac device showing full flow
- [ ] Recording uploaded to App Review Notes in App Store Connect
