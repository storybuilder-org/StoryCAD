# Issue 1333: TDD Guide for UsageTrackingService

**For**: Jake
**Context**: The UsageTrackingService collects usage data in memory during a session and flushes it to MySQL at session end. The collection logic is pure in-memory work with a clean boundary at the database — ideal for TDD.

---

## The Idea

Test everything up to the database. The service accumulates data from hooks (session start/end, outline open/close, element add/remove, feature use) and produces a payload that would be sent to MySQL. Mock the database layer, verify the payload is correct.

The existing `TestMySqlIo` pattern is the model — a fake that records calls without touching a database.

---

## Mock Boundary

```
Hooks → IUsageTrackingService → BackendService → IMySqlIo (mock here)
```

Create a `TestUsageTrackingService` that implements `IUsageTrackingService` and records all calls. For testing the real `UsageTrackingService` implementation, inject a mocked `BackendService` or `IMySqlIo` so you can inspect what would have been sent to MySQL.

---

## What to Test

### Session lifecycle
- `StartSession()` records a timestamp
- `EndSession()` produces a flush with correct session_start, session_end, clock_time_seconds
- `EndSession()` with no activity produces a valid (but minimal) payload

### Outline tracking
- `OutlineOpened()` followed by `OutlineClosed()` produces correct outline_sessions record
- Element count at open vs close captures the delta
- Multiple outlines opened in one session — each gets its own record
- `OutlineClosed()` without a matching `OutlineOpened()` — doesn't crash, logs or ignores
- Genre and story form from `OutlineOpened()` flow into outline_metadata

### Element operations
- `ElementAdded(StoryItemType.Character)` increments the correct per-type counter
- `ElementRemoved()` increments the removed counter
- `ElementRestored()` increments the restored counter
- Counters are per-outline (reset when a new outline is opened)
- Element counts by type are captured (not just totals)

### Feature usage
- `FeatureUsed("MasterPlots")` once → use_count = 1
- `FeatureUsed("MasterPlots")` three times → use_count = 3
- Multiple features used → separate records for each
- Feature counts are per-session

### Consent gating
- When `UsageStatsConsent = false`, all methods are no-ops — no data accumulated, no flush
- When headless mode is active, `UsageStatsConsent` is forced to `false`
- Toggling consent mid-session: if consent is false at flush time, nothing is sent

### Flush payload
- `EndSession()` serializes accumulated data into the JSON format expected by `spRecordSessionData`
- Outlines JSON array contains correct fields: outline_guid, open_time, close_time, elements_added, elements_edited, elements_deleted, genre, story_form, element_count, last_updated
- Features JSON array contains correct fields: feature_name, use_count
- Empty arrays (no outlines opened, no features used) produce valid empty JSON arrays `[]`

### Edge cases
- Session with zero activity — valid flush with empty arrays
- Very long session — clock_time_seconds is correct
- Outline opened but never closed (app crash scenario) — `EndSession()` handles gracefully, uses session end time as close time
- Same outline opened and closed multiple times in one session — multiple outline_sessions records
- `usage_id` is included in the payload and matches what's in PreferencesModel

---

## Test Structure

```csharp
[TestClass]
public class UsageTrackingServiceTests
{
    private UsageTrackingService _service;
    private TestMySqlIo _mockDb;
    // ... setup with mocked dependencies

    [TestInitialize]
    public void Setup()
    {
        // Create service with mocked BackendService/IMySqlIo
        // Set UsageStatsConsent = true (default for most tests)
    }

    [TestMethod]
    public async Task EndSession_WithOneOutlineAndTwoFeatures_ProducesCorrectPayload()
    {
        // Arrange
        _service.StartSession();
        _service.OutlineOpened(testGuid, "Mystery", "Novel", 10);
        _service.ElementAdded(StoryItemType.Character);
        _service.ElementAdded(StoryItemType.Scene);
        _service.FeatureUsed("MasterPlots");
        _service.FeatureUsed("Search");
        _service.FeatureUsed("Search");
        _service.OutlineClosed(testGuid, 12);

        // Act
        await _service.EndSession();

        // Assert — inspect what was passed to the mock
        // Session: clock_time > 0
        // Outlines: 1 record, elements_added = 2, element_count = 12
        // Features: MasterPlots (1), Search (2)
    }

    [TestMethod]
    public async Task EndSession_WhenConsentFalse_SendsNothing()
    {
        // Arrange — set UsageStatsConsent = false
        _service.StartSession();
        _service.FeatureUsed("Collaborator");

        // Act
        await _service.EndSession();

        // Assert — mock DB received no calls
    }
}
```

---

## Red-Green-Refactor Cycle

1. **Red**: Write a test for one behavior (e.g., "FeatureUsed increments count"). It fails because the method doesn't exist yet or isn't implemented.
2. **Green**: Write the minimum code to make the test pass.
3. **Refactor**: Clean up without breaking the test.
4. Repeat for the next behavior.

Start with the simplest tests (StartSession records timestamp, EndSession flushes) and build up to the complex ones (multiple outlines, edge cases).

---

## What TDD Does NOT Cover

- Whether the stored procedure correctly processes the JSON payload — that's integration testing against the local MySQL Docker instance
- Whether the hooks are wired up in the right places in the actual ViewModels — that's manual verification or UI testing
- Network/SSL behavior to ScaleGrid — that's production smoke testing

TDD covers the collection logic, which is the bulk of the code and the most likely source of bugs.
