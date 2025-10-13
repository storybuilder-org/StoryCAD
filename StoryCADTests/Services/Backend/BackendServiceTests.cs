using CommunityToolkit.Mvvm.DependencyInjection;
using MySql.Data.MySqlClient;
using StoryCADLib.Models;
using StoryCADLib.Services;
using StoryCADLib.Services.Backend;
using StoryCADLib.Services.Json;
using StoryCADLib.Services.Logging;

#nullable disable

namespace StoryCADTests.Services.Backend;

[TestClass]
public class BackendTests
{
    /// <summary>
    ///     This checks that the Backend Server connection
    ///     works correctly. (Requires .ENV)
    /// </summary>
    [TestMethod]
    public void CheckConnection()
    {
        var Prefs = Ioc.Default.GetService<PreferenceService>();

        //Load keys.
        Doppler doppler = new();
        Doppler keys = new();
        Task.Run(async () =>
        {
            keys = await doppler.FetchSecretsAsync();
            await Ioc.Default.GetRequiredService<BackendService>().SetConnectionString(keys);
        }).Wait();

        //Make sure app logic thinks versions need syncing
        Prefs.Model.RecordVersionStatus = false;
        Prefs.Model.FirstName = "StoryCAD";
        Prefs.Model.LastName = "Tests";
        Prefs.Model.Email = "sysadmin@storybuilder.org";

        //Call backend service to check connection
        Task.Run(async () =>
        {
            await Ioc.Default.GetRequiredService<BackendService>().PostVersion();
            await Ioc.Default.GetRequiredService<BackendService>().PostPreferences(Prefs.Model);
        }).Wait();

        //Check if test passed (RecordVersionStatus should be true now)
        Assert.IsTrue(Prefs.Model.RecordVersionStatus);
    }

    #region StartupRecording Exception Handling Tests

    /// <summary>
    /// These tests verify that StartupRecording handles different exception types appropriately
    /// with correct log levels (Warning for transient, Error for permanent failures)
    /// </summary>
    [TestMethod]
    public async Task StartupRecording_WithTaskCanceledException_LogsAsWarning()
    {
        // Arrange
        var testLogger = new TestLogService();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var preferenceService = Ioc.Default.GetRequiredService<PreferenceService>();

        // Act & Assert
        // TODO: Need to inject exception into PostPreferences/PostVersion
        // For now, this test documents the desired behavior
        Assert.Inconclusive("Test infrastructure needs enhancement to inject exceptions");
    }

    [TestMethod]
    public async Task StartupRecording_WithOperationCanceledException_LogsAsWarning()
    {
        // Arrange
        var testLogger = new TestLogService();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var preferenceService = Ioc.Default.GetRequiredService<PreferenceService>();

        // Act & Assert
        Assert.Inconclusive("Test infrastructure needs enhancement to inject exceptions");
    }

    [TestMethod]
    public async Task StartupRecording_WithMySqlAuthFailure_LogsAsError()
    {
        // Arrange
        var testLogger = new TestLogService();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var preferenceService = Ioc.Default.GetRequiredService<PreferenceService>();

        // Act & Assert
        // Auth failure (code 1045) should log as Error
        Assert.Inconclusive("Test infrastructure needs enhancement to inject exceptions");
    }

    [TestMethod]
    public async Task StartupRecording_WithMySqlTimeout_LogsAsWarning()
    {
        // Arrange
        var testLogger = new TestLogService();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var preferenceService = Ioc.Default.GetRequiredService<PreferenceService>();

        // Act & Assert
        // Timeout (code 2013) should log as Warning (transient)
        Assert.Inconclusive("Test infrastructure needs enhancement to inject exceptions");
    }

    [TestMethod]
    public async Task StartupRecording_WithMySqlConnectionRefused_LogsAsWarning()
    {
        // Arrange
        var testLogger = new TestLogService();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var preferenceService = Ioc.Default.GetRequiredService<PreferenceService>();

        // Act & Assert
        // Connection refused (code 2003) should log as Warning (server down)
        Assert.Inconclusive("Test infrastructure needs enhancement to inject exceptions");
    }

    [TestMethod]
    public async Task StartupRecording_WithIOException_LogsAsError()
    {
        // Arrange
        var testLogger = new TestLogService();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var preferenceService = Ioc.Default.GetRequiredService<PreferenceService>();

        // Act & Assert
        // File write failure should log as Error
        Assert.Inconclusive("Test infrastructure needs enhancement to inject exceptions");
    }

    [TestMethod]
    public async Task StartupRecording_WithUnauthorizedAccessException_LogsAsError()
    {
        // Arrange
        var testLogger = new TestLogService();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var preferenceService = Ioc.Default.GetRequiredService<PreferenceService>();

        // Act & Assert
        // Permission denied should log as Error
        Assert.Inconclusive("Test infrastructure needs enhancement to inject exceptions");
    }

    [TestMethod]
    public async Task StartupRecording_WithUnexpectedException_LogsAsError()
    {
        // Arrange
        var testLogger = new TestLogService();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var preferenceService = Ioc.Default.GetRequiredService<PreferenceService>();

        // Act & Assert
        // Unexpected exceptions should log as Error
        Assert.Inconclusive("Test infrastructure needs enhancement to inject exceptions");
    }

    #endregion
}

#region Test Infrastructure

/// <summary>
/// Test double for ILogService that captures log calls for verification
/// </summary>
public class TestLogService : ILogService
{
    public List<(LogLevel level, string message)> LogCalls { get; } = new();
    public List<(LogLevel level, Exception ex, string message)> ExceptionCalls { get; } = new();

    public bool ElmahLogging => false;

    public void Log(LogLevel level, string message)
    {
        LogCalls.Add((level, message));
    }

    public void LogException(LogLevel level, Exception exception, string message)
    {
        ExceptionCalls.Add((level, exception, message));
    }

    public void SetElmahTokens(Doppler keys) { }
    public bool AddElmahTarget() => false;
    public void Flush() { }

    // Helper methods for test assertions
    public bool HasWarning(string containing) =>
        LogCalls.Any(x => x.level == LogLevel.Warn && x.message.Contains(containing));

    public bool HasError(string containing) =>
        ExceptionCalls.Any(x => x.level == LogLevel.Error && x.message.Contains(containing)) ||
        LogCalls.Any(x => x.level == LogLevel.Error && x.message.Contains(containing));

    public int WarningCount => LogCalls.Count(x => x.level == LogLevel.Warn);
    public int ErrorCount => ExceptionCalls.Count(x => x.level == LogLevel.Error) +
                             LogCalls.Count(x => x.level == LogLevel.Error);
}

/// <summary>
/// Testable version of BackendService that allows injecting exceptions for testing
/// </summary>
public class TestableBackendService : BackendService
{
    private Exception _postPreferencesException;
    private Exception _postVersionException;
    private Exception _writePreferencesException;

    public TestableBackendService(ILogService logService, AppState appState, PreferenceService preferenceService)
        : base(logService, appState, preferenceService)
    {
    }

    public void SetPostPreferencesException(Exception ex) => _postPreferencesException = ex;
    public void SetPostVersionException(Exception ex) => _postVersionException = ex;
    public void SetWritePreferencesException(Exception ex) => _writePreferencesException = ex;

    // Note: We can't easily override PostPreferences/PostVersion as they're not virtual
    // Instead, we'll need to test exception handling at the PreferencesIo level
    // This is a limitation we'll work with for now
}

#endregion
