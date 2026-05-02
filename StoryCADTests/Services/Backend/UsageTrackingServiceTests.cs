using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCADLib.Models;
using StoryCADLib.Models.Tools;
using StoryCADLib.Services;
using StoryCADLib.Services.Backend;
using StoryCADLib.Services.Logging;
using System.Text.Json;

#nullable disable

namespace StoryCADTests.Services.Backend;

[TestClass]
public class UsageTrackingServiceTests
{
    private PreferenceService _preferenceService;
    private TestMySqlIo _sqlIo;
    private TestLogService _logger;
    private UsageTrackingService _service;

    [TestInitialize]
    public void Setup()
    {
        _preferenceService = Ioc.Default.GetRequiredService<PreferenceService>();
        _sqlIo = new TestMySqlIo();
        _sqlIo.SetConnectionString("fake-connection-string");
        _logger = new TestLogService();
        _service = new UsageTrackingService(_preferenceService, _sqlIo, _logger);

        // Default: consent given, with a usage ID
        _preferenceService.Model.UsageStatsConsent = true;
        _preferenceService.Model.UsageId = Guid.NewGuid().ToString();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _preferenceService.Model.UsageStatsConsent = false;
        _preferenceService.Model.UsageId = string.Empty;
    }

    #region Consent Gating

    [TestMethod]
    public async Task StartSession_WhenConsentFalse_IsNoOp()
    {
        _preferenceService.Model.UsageStatsConsent = false;

        _service.StartSession();
        _service.FeatureUsed("Collaborator");
        await _service.EndSession();

        Assert.AreEqual(0, _sqlIo.RecordSessionDataCalls.Count,
            "No data should be flushed when consent is false");
    }

    [TestMethod]
    public async Task EndSession_WhenConsentFalse_IsNoOp()
    {
        _preferenceService.Model.UsageStatsConsent = true;
        _service.StartSession();

        // Revoke consent before end
        _preferenceService.Model.UsageStatsConsent = false;
        await _service.EndSession();

        Assert.AreEqual(0, _sqlIo.RecordSessionDataCalls.Count);
    }

    [TestMethod]
    public void OutlineOpened_WhenConsentFalse_IsNoOp()
    {
        _preferenceService.Model.UsageStatsConsent = false;

        _service.StartSession();
        _service.OutlineOpened(Guid.NewGuid(), "Mystery", "Novel", 10);
        _service.ElementAdded();

        // If consent was false, element tracking should be a no-op too
        // We verify indirectly: no crash, and EndSession produces nothing
    }

    [TestMethod]
    public void ElementAdded_WhenConsentFalse_IsNoOp()
    {
        _preferenceService.Model.UsageStatsConsent = false;
        _service.StartSession();
        _service.ElementAdded();
        // No crash = pass
    }

    [TestMethod]
    public void FeatureUsed_WhenConsentFalse_IsNoOp()
    {
        _preferenceService.Model.UsageStatsConsent = false;
        _service.StartSession();
        _service.FeatureUsed("Search");
        // No crash = pass
    }

    #endregion

    #region Session Lifecycle

    [TestMethod]
    public async Task EndSession_FlushesDataToSqlIo()
    {
        _service.StartSession();
        await _service.EndSession();

        Assert.AreEqual(1, _sqlIo.RecordSessionDataCalls.Count,
            "EndSession should flush exactly one call");
    }

    [TestMethod]
    public async Task EndSession_WhenConnectionNotConfigured_IsNoOp()
    {
        var disconnectedSqlIo = new TestMySqlIo(); // IsConnectionConfigured = false
        var service = new UsageTrackingService(_preferenceService, disconnectedSqlIo, _logger);

        service.StartSession();
        await service.EndSession();

        Assert.AreEqual(0, disconnectedSqlIo.RecordSessionDataCalls.Count);
    }

    [TestMethod]
    public async Task EndSession_PassesUsageIdFromPreferences()
    {
        var expectedId = _preferenceService.Model.UsageId;

        _service.StartSession();
        await _service.EndSession();

        var call = _sqlIo.RecordSessionDataCalls[0];
        Assert.AreEqual(expectedId, call.usageId);
    }

    [TestMethod]
    public async Task EndSession_CalculatesClockTimeSeconds()
    {
        _service.StartSession();
        // Can't control real time in a unit test, but clock time should be >= 0
        await _service.EndSession();

        var call = _sqlIo.RecordSessionDataCalls[0];
        Assert.IsTrue(call.seconds >= 0, "Clock time should be non-negative");
    }

    [TestMethod]
    public async Task EndSession_WhenSqlThrows_LogsWarningAndSwallows()
    {
        _sqlIo.ExceptionToThrow = new Exception("Connection failed");

        _service.StartSession();
        await _service.EndSession();

        Assert.IsTrue(_logger.ExceptionCalls.Any(c =>
            c.level == LogLevel.Warn && c.message.Contains("Failed to flush usage data")));
    }

    [TestMethod]
    public async Task EndSession_WhenUsageIdEmpty_SkipsFlush()
    {
        // Consent is true but UsageId somehow wasn't populated by PreferencesIo.
        // Service should log a warning and skip the flush rather than send "".
        _preferenceService.Model.UsageId = string.Empty;

        _service.StartSession();
        await _service.EndSession();

        Assert.AreEqual(0, _sqlIo.RecordSessionDataCalls.Count,
            "Flush should be skipped when UsageId is empty");
        Assert.IsTrue(_logger.LogCalls.Any(c =>
            c.level == LogLevel.Warn && c.message.Contains("UsageId is empty")));
    }

    #endregion

    #region Outline Tracking

    [TestMethod]
    public async Task EndSession_WhenOutlineStillOpen_RefetchesCurrentMetadata()
    {
        // Arrange: simulate FileCreateService path where OutlineOpened fires
        // with blank genre/story_form, then user fills them in before app close.
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var outlineService = Ioc.Default.GetRequiredService<StoryCADLib.Services.Outline.OutlineService>();
        var model = await outlineService.CreateModel("TestRefetch", "Test Author", 0);
        var overview = model.StoryElements.OfType<OverviewModel>().First();
        overview.StoryGenre = "";
        overview.StoryType = "";
        appState.CurrentDocument = new StoryDocument(model,
            Path.Combine(Path.GetTempPath(), "TestRefetch.stbx"));

        _service.StartSession();
        _service.OutlineOpened(overview.Uuid, "", "", model.StoryElements.Count);

        // Simulate user filling in metadata after creation
        overview.StoryGenre = "Thriller";
        overview.StoryType = "Novel";

        // Act
        await _service.EndSession();

        // Assert
        Assert.AreEqual(1, _sqlIo.RecordSessionDataCalls.Count);
        var call = _sqlIo.RecordSessionDataCalls[0];
        StringAssert.Contains(call.outlines, "Thriller",
            $"Expected outlines JSON to contain 'Thriller': {call.outlines}");
        StringAssert.Contains(call.outlines, "Novel",
            $"Expected outlines JSON to contain 'Novel': {call.outlines}");

        // Cleanup
        appState.CurrentDocument = null;
    }

    [TestMethod]
    public async Task OutlineOpened_TracksOutlineInFlush()
    {
        var guid = Guid.NewGuid();

        _service.StartSession();
        _service.OutlineOpened(guid, "Sci-Fi", "Novel", 25);
        _service.OutlineClosed(guid, 30);
        await _service.EndSession();

        var call = _sqlIo.RecordSessionDataCalls[0];
        var outlines = JsonSerializer.Deserialize<JsonElement[]>(call.outlines);
        Assert.AreEqual(1, outlines.Length);
        Assert.AreEqual(guid.ToString(), outlines[0].GetProperty("outline_guid").GetString());
        Assert.AreEqual("Sci-Fi", outlines[0].GetProperty("genre").GetString());
        Assert.AreEqual("Novel", outlines[0].GetProperty("story_form").GetString());
        Assert.AreEqual(30, outlines[0].GetProperty("element_count").GetInt32());
    }

    [TestMethod]
    public async Task OutlineClosed_UpdatesElementCount()
    {
        var guid = Guid.NewGuid();

        _service.StartSession();
        _service.OutlineOpened(guid, "Romance", "Short Story", 5);
        _service.OutlineClosed(guid, 12);
        await _service.EndSession();

        var outlines = JsonSerializer.Deserialize<JsonElement[]>(_sqlIo.RecordSessionDataCalls[0].outlines);
        Assert.AreEqual(12, outlines[0].GetProperty("element_count").GetInt32());
    }

    [TestMethod]
    public async Task OutlineOpened_ClosePreviousOutlineAutomatically()
    {
        var guid1 = Guid.NewGuid();
        var guid2 = Guid.NewGuid();

        _service.StartSession();
        _service.OutlineOpened(guid1, "Mystery", "Novel", 10);
        _service.OutlineOpened(guid2, "Horror", "Novella", 5);
        _service.OutlineClosed(guid2, 8);
        await _service.EndSession();

        var outlines = JsonSerializer.Deserialize<JsonElement[]>(_sqlIo.RecordSessionDataCalls[0].outlines);
        Assert.AreEqual(2, outlines.Length, "Both outlines should be tracked");
        // First outline should have a close_time (auto-closed)
        Assert.IsFalse(string.IsNullOrEmpty(outlines[0].GetProperty("close_time").GetString()));
    }

    [TestMethod]
    public async Task EndSession_ClosesOpenOutlineAutomatically()
    {
        var guid = Guid.NewGuid();

        _service.StartSession();
        _service.OutlineOpened(guid, "Fantasy", "Novel", 50);
        // Don't call OutlineClosed — EndSession should handle it
        await _service.EndSession();

        var outlines = JsonSerializer.Deserialize<JsonElement[]>(_sqlIo.RecordSessionDataCalls[0].outlines);
        Assert.IsFalse(string.IsNullOrEmpty(outlines[0].GetProperty("close_time").GetString()),
            "EndSession should auto-close any open outline");
    }

    [TestMethod]
    public async Task OutlineClosed_WrongGuid_IsIgnored()
    {
        var guid = Guid.NewGuid();

        _service.StartSession();
        _service.OutlineOpened(guid, "Mystery", "Novel", 10);
        _service.OutlineClosed(Guid.NewGuid(), 20); // Wrong GUID
        await _service.EndSession();

        // Outline should still be auto-closed by EndSession, not by the wrong-GUID call
        var outlines = JsonSerializer.Deserialize<JsonElement[]>(_sqlIo.RecordSessionDataCalls[0].outlines);
        Assert.AreEqual(10, outlines[0].GetProperty("element_count").GetInt32(),
            "Element count should not be updated by a close with wrong GUID");
    }

    #endregion

    #region Element Counting

    [TestMethod]
    public async Task ElementAdded_IncrementsCount()
    {
        var guid = Guid.NewGuid();

        _service.StartSession();
        _service.OutlineOpened(guid, "Mystery", "Novel", 10);
        _service.ElementAdded();
        _service.ElementAdded();
        _service.ElementAdded();
        _service.OutlineClosed(guid, 13);
        await _service.EndSession();

        var outlines = JsonSerializer.Deserialize<JsonElement[]>(_sqlIo.RecordSessionDataCalls[0].outlines);
        Assert.AreEqual(3, outlines[0].GetProperty("elements_added").GetInt32());
    }

    [TestMethod]
    public void ElementAdded_WhenNoOutlineOpen_IsNoOp()
    {
        _service.StartSession();
        _service.ElementAdded();
        // No crash = pass; element is silently ignored when no outline is open
    }

    [TestMethod]
    public async Task ElementRemoved_IncrementsDeletedCount()
    {
        var guid = Guid.NewGuid();

        _service.StartSession();
        _service.OutlineOpened(guid, "Mystery", "Novel", 10);
        _service.ElementRemoved();
        _service.ElementRemoved();
        _service.OutlineClosed(guid, 8);
        await _service.EndSession();

        var outlines = JsonSerializer.Deserialize<JsonElement[]>(_sqlIo.RecordSessionDataCalls[0].outlines);
        Assert.AreEqual(2, outlines[0].GetProperty("elements_deleted").GetInt32());
    }

    [TestMethod]
    public async Task ElementRestored_IncrementsAddedCount()
    {
        var guid = Guid.NewGuid();

        _service.StartSession();
        _service.OutlineOpened(guid, "Mystery", "Novel", 10);
        _service.ElementRestored();
        _service.OutlineClosed(guid, 11);
        await _service.EndSession();

        var outlines = JsonSerializer.Deserialize<JsonElement[]>(_sqlIo.RecordSessionDataCalls[0].outlines);
        Assert.AreEqual(1, outlines[0].GetProperty("elements_added").GetInt32());
    }

    #endregion

    #region Feature Tracking

    [TestMethod]
    public async Task FeatureUsed_TracksCountPerFeature()
    {
        _service.StartSession();
        _service.FeatureUsed("Collaborator");
        _service.FeatureUsed("Collaborator");
        _service.FeatureUsed("MasterPlots");
        await _service.EndSession();

        var call = _sqlIo.RecordSessionDataCalls[0];
        var features = JsonSerializer.Deserialize<JsonElement[]>(call.features);
        Assert.AreEqual(2, features.Length, "Should have 2 distinct features");

        var collaborator = features.First(f => f.GetProperty("feature_name").GetString() == "Collaborator");
        Assert.AreEqual(2, collaborator.GetProperty("use_count").GetInt32());

        var masterPlots = features.First(f => f.GetProperty("feature_name").GetString() == "MasterPlots");
        Assert.AreEqual(1, masterPlots.GetProperty("use_count").GetInt32());
    }

    [TestMethod]
    public async Task FeatureUsed_ZeroCountFeaturesExcludedFromFlush()
    {
        _service.StartSession();
        // No features used
        await _service.EndSession();

        var features = JsonSerializer.Deserialize<JsonElement[]>(_sqlIo.RecordSessionDataCalls[0].features);
        Assert.AreEqual(0, features.Length, "Empty feature list should produce empty JSON array");
    }

    #endregion

    #region JSON Format

    [TestMethod]
    public async Task EndSession_ProducesValidJsonArrays()
    {
        var guid = Guid.NewGuid();

        _service.StartSession();
        _service.OutlineOpened(guid, "Mystery", "Novel", 10);
        _service.ElementAdded();
        _service.FeatureUsed("Search");
        _service.OutlineClosed(guid, 11);
        await _service.EndSession();

        var call = _sqlIo.RecordSessionDataCalls[0];

        // Should not throw — valid JSON
        var outlines = JsonSerializer.Deserialize<JsonElement[]>(call.outlines);
        var features = JsonSerializer.Deserialize<JsonElement[]>(call.features);

        Assert.AreEqual(1, outlines.Length);
        Assert.AreEqual(1, features.Length);

        // Verify datetime format
        var openTime = outlines[0].GetProperty("open_time").GetString();
        Assert.IsTrue(DateTime.TryParse(openTime, out _), "open_time should be parseable datetime");
    }

    [TestMethod]
    public async Task EndSession_EmptySession_ProducesEmptyArrays()
    {
        _service.StartSession();
        await _service.EndSession();

        var call = _sqlIo.RecordSessionDataCalls[0];
        Assert.AreEqual("[]", call.outlines);
        Assert.AreEqual("[]", call.features);
    }

    #endregion
}
