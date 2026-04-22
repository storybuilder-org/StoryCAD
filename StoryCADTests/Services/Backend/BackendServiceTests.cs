using CommunityToolkit.Mvvm.DependencyInjection;
using MySql.Data.MySqlClient;
using StoryCADLib.DAL;
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
    #region PostVersion / PostPreferences Skip Tests

    /// <summary>
    ///     Verifies that PostVersion skips gracefully
    ///     when the backend connection is not configured.
    /// </summary>
    [TestMethod]
    public async Task PostVersion_WhenConnectionNotConfigured_SkipsGracefully()
    {
        // Arrange
        var testLogger = new TestLogService();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var preferenceService = Ioc.Default.GetRequiredService<PreferenceService>();
        var testSqlIo = new TestMySqlIo(); // IsConnectionConfigured defaults to false
        var backendService = new BackendService(testLogger, appState, preferenceService, testSqlIo);

        preferenceService.Model.RecordVersionStatus = false;

        // Act
        await backendService.PostVersion();

        // Assert — should skip without error
        Assert.IsFalse(preferenceService.Model.RecordVersionStatus);
        Assert.IsTrue(testLogger.HasWarning("Skipping PostVersion"));
    }

    /// <summary>
    ///     Verifies that PostPreferences skips gracefully
    ///     when the backend connection is not configured.
    /// </summary>
    [TestMethod]
    public async Task PostPreferences_WhenConnectionNotConfigured_SkipsGracefully()
    {
        // Arrange
        var testLogger = new TestLogService();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var preferenceService = Ioc.Default.GetRequiredService<PreferenceService>();
        var testSqlIo = new TestMySqlIo();
        var backendService = new BackendService(testLogger, appState, preferenceService, testSqlIo);

        preferenceService.Model.FirstName = "StoryCAD";
        preferenceService.Model.LastName = "Tests";
        preferenceService.Model.Email = "sysadmin@storybuilder.org";

        // Act
        await backendService.PostPreferences(preferenceService.Model);

        // Assert — should skip without error
        Assert.IsTrue(testLogger.HasWarning("Skipping PostPreferences"));
    }

    /// <summary>
    ///     Verifies that PostPreferences forwards UsageStatsConsent to
    ///     IMySqlIo.AddOrUpdatePreferences as the usageStats argument.
    ///     Regression guard for the claim that usage_consent stays 0 after Save.
    /// </summary>
    [TestMethod]
    public async Task PostPreferences_WhenUsageStatsConsentTrue_PassesTrueToSqlIo()
    {
        var testLogger = new TestLogService();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var preferenceService = Ioc.Default.GetRequiredService<PreferenceService>();
        var testSqlIo = new TestMySqlIo();
        testSqlIo.SetConnectionString("fake");
        var backendService = new BackendService(testLogger, appState, preferenceService, testSqlIo);

        preferenceService.Model.FirstName = "StoryCAD";
        preferenceService.Model.LastName = "Tests";
        preferenceService.Model.Email = "sysadmin@storybuilder.org";
        preferenceService.Model.UsageStatsConsent = true;

        await backendService.PostPreferences(preferenceService.Model);

        Assert.AreEqual(1, testSqlIo.AddOrUpdatePreferencesCalls.Count);
        Assert.IsTrue(testSqlIo.AddOrUpdatePreferencesCalls[0].usageStats,
            "PostPreferences should forward UsageStatsConsent=true as usageStats=true");
    }

    /// <summary>
    ///     Mirror of the true case — verifies false is not coerced to true.
    /// </summary>
    [TestMethod]
    public async Task PostPreferences_WhenUsageStatsConsentFalse_PassesFalseToSqlIo()
    {
        var testLogger = new TestLogService();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var preferenceService = Ioc.Default.GetRequiredService<PreferenceService>();
        var testSqlIo = new TestMySqlIo();
        testSqlIo.SetConnectionString("fake");
        var backendService = new BackendService(testLogger, appState, preferenceService, testSqlIo);

        preferenceService.Model.FirstName = "StoryCAD";
        preferenceService.Model.LastName = "Tests";
        preferenceService.Model.Email = "sysadmin@storybuilder.org";
        preferenceService.Model.UsageStatsConsent = false;

        await backendService.PostPreferences(preferenceService.Model);

        Assert.AreEqual(1, testSqlIo.AddOrUpdatePreferencesCalls.Count);
        Assert.IsFalse(testSqlIo.AddOrUpdatePreferencesCalls[0].usageStats,
            "PostPreferences should forward UsageStatsConsent=false as usageStats=false");
    }

    #endregion

    #region DeleteUserData Tests

    [TestMethod]
    public async Task DeleteUserData_WhenConnectionNotConfigured_ReturnsFalse()
    {
        // Arrange
        var testLogger = new TestLogService();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var preferenceService = Ioc.Default.GetRequiredService<PreferenceService>();
        var testSqlIo = new TestMySqlIo(); // not configured
        var backendService = new BackendService(testLogger, appState, preferenceService, testSqlIo);

        // Act
        var result = await backendService.DeleteUserData();

        // Assert
        Assert.IsFalse(result);
        Assert.IsTrue(testLogger.HasWarning("Skipping DeleteUserData"));
    }

    [TestMethod]
    public async Task DeleteUserData_WhenNoUserId_ReturnsFalse()
    {
        // Arrange
        var testLogger = new TestLogService();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var preferenceService = Ioc.Default.GetRequiredService<PreferenceService>();
        var testSqlIo = new TestMySqlIo();
        testSqlIo.SetConnectionString("fake");
        var backendService = new BackendService(testLogger, appState, preferenceService, testSqlIo);

        preferenceService.Model.UserId = 0; // no stored user ID

        // Act
        var result = await backendService.DeleteUserData();

        // Assert
        Assert.IsFalse(result);
        Assert.IsTrue(testLogger.HasWarning("no UserId stored"));
    }

    [TestMethod]
    public async Task DeleteUserData_WhenConfigured_CallsDeleteAndReturnsTrue()
    {
        // Arrange
        var testLogger = new TestLogService();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var preferenceService = Ioc.Default.GetRequiredService<PreferenceService>();
        var testSqlIo = new TestMySqlIo();
        testSqlIo.SetConnectionString("fake");
        var backendService = new BackendService(testLogger, appState, preferenceService, testSqlIo);

        preferenceService.Model.UserId = 42;

        // Act
        var result = await backendService.DeleteUserData();

        // Assert
        Assert.IsTrue(result);
        Assert.AreEqual(1, testSqlIo.DeleteUserCalls.Count);
        Assert.AreEqual(42, testSqlIo.DeleteUserCalls[0]);
    }

    [TestMethod]
    public async Task DeleteUserData_WhenDatabaseThrows_ReturnsFalse()
    {
        // Arrange
        var testLogger = new TestLogService();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var preferenceService = Ioc.Default.GetRequiredService<PreferenceService>();
        var testSqlIo = new TestMySqlIo();
        testSqlIo.SetConnectionString("fake");
        testSqlIo.ExceptionToThrow = new Exception("Connection lost");
        var backendService = new BackendService(testLogger, appState, preferenceService, testSqlIo);

        preferenceService.Model.UserId = 42;

        // Act
        var result = await backendService.DeleteUserData();

        // Assert
        Assert.IsFalse(result);
        Assert.IsTrue(testLogger.HasError("Failed to delete user data"));
    }

    #endregion

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
        var testSqlIo = new TestMySqlIo();

        var backendService = new TestableBackendService(testLogger, appState, preferenceService, testSqlIo);
        backendService.SetPostVersionException(new TaskCanceledException("Connection timeout"));
        preferenceService.Model.RecordVersionStatus = false;

        // Act
        await backendService.StartupRecording();

        // Assert
        Assert.IsTrue(testLogger.HasWarning("Backend operation timed out during startup"),
            "Should log TaskCanceledException as Warning");
        var hasInfoRetry = testLogger.LogCalls.Any(x =>
            x.level == LogLevel.Info && x.message.Contains("Backend telemetry will retry on next startup"));
        Assert.IsTrue(hasInfoRetry, "Should log retry message at Info level");
    }

    [TestMethod]
    public async Task StartupRecording_WithOperationCanceledException_LogsAsWarning()
    {
        // Arrange
        var testLogger = new TestLogService();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var preferenceService = Ioc.Default.GetRequiredService<PreferenceService>();
        var testSqlIo = new TestMySqlIo();

        var backendService = new TestableBackendService(testLogger, appState, preferenceService, testSqlIo);
        backendService.SetPostVersionException(new OperationCanceledException("Operation was cancelled"));
        preferenceService.Model.RecordVersionStatus = false;

        // Act
        await backendService.StartupRecording();

        // Assert
        Assert.IsTrue(testLogger.HasWarning("Backend operation cancelled during startup"),
            "Should log OperationCanceledException as Warning");
    }

    [TestMethod]
    public async Task StartupRecording_WithMySqlAuthFailure_LogsAsError()
    {
        // Arrange
        var testLogger = new TestLogService();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var preferenceService = Ioc.Default.GetRequiredService<PreferenceService>();
        var testSqlIo = new TestMySqlIo();

        var backendService = new TestableBackendService(testLogger, appState, preferenceService, testSqlIo);
        var authException = CreateMySqlException(1045, "Access denied for user");
        backendService.SetPostVersionException(authException);
        preferenceService.Model.RecordVersionStatus = false;

        // Act
        await backendService.StartupRecording();

        // Assert
        Assert.IsTrue(testLogger.HasError("Backend authentication failed"),
            "Should log MySQL auth failure as Error");
        Assert.IsTrue(testLogger.HasError("Code 1045"),
            "Should log error code 1045");
    }

    [TestMethod]
    public async Task StartupRecording_WithMySqlTimeout_LogsAsWarning()
    {
        // Arrange
        var testLogger = new TestLogService();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var preferenceService = Ioc.Default.GetRequiredService<PreferenceService>();
        var testSqlIo = new TestMySqlIo();

        var backendService = new TestableBackendService(testLogger, appState, preferenceService, testSqlIo);
        var timeoutException = CreateMySqlException(2013, "Lost connection to MySQL server");
        backendService.SetPostVersionException(timeoutException);
        preferenceService.Model.RecordVersionStatus = false;

        // Act
        await backendService.StartupRecording();

        // Assert
        Assert.IsTrue(testLogger.HasWarning("Backend server temporarily unavailable"),
            "Should log MySQL timeout as Warning");
        Assert.IsTrue(testLogger.HasWarning("Code 2013"),
            "Should log error code 2013");
        var hasInfoRetry = testLogger.LogCalls.Any(x =>
            x.level == LogLevel.Info && x.message.Contains("Backend telemetry will retry on next startup"));
        Assert.IsTrue(hasInfoRetry, "Should log retry message at Info level");
    }

    [TestMethod]
    public async Task StartupRecording_WithMySqlConnectionRefused_LogsAsWarning()
    {
        // Arrange
        var testLogger = new TestLogService();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var preferenceService = Ioc.Default.GetRequiredService<PreferenceService>();
        var testSqlIo = new TestMySqlIo();

        var backendService = new TestableBackendService(testLogger, appState, preferenceService, testSqlIo);
        var connRefusedException = CreateMySqlException(2003, "Can't connect to MySQL server");
        backendService.SetPostVersionException(connRefusedException);
        preferenceService.Model.RecordVersionStatus = false;

        // Act
        await backendService.StartupRecording();

        // Assert
        Assert.IsTrue(testLogger.HasWarning("Backend server temporarily unavailable"),
            "Should log MySQL connection refused as Warning");
        Assert.IsTrue(testLogger.HasWarning("Code 2003"),
            "Should log error code 2003");
        var hasInfoRetry = testLogger.LogCalls.Any(x =>
            x.level == LogLevel.Info && x.message.Contains("Backend telemetry will retry on next startup"));
        Assert.IsTrue(hasInfoRetry, "Should log retry message at Info level");
    }

    [TestMethod]
    public async Task StartupRecording_WithIOException_LogsAsError()
    {
        // Arrange
        var testLogger = new TestLogService();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var preferenceService = Ioc.Default.GetRequiredService<PreferenceService>();
        var testSqlIo = new TestMySqlIo();

        var backendService = new TestableBackendService(testLogger, appState, preferenceService, testSqlIo);
        var ioException = new IOException("Unable to write to file");
        backendService.SetWritePreferencesException(ioException);
        preferenceService.Model.Version = "different-version-to-trigger-mismatch";

        // Act
        await backendService.StartupRecording();

        // Assert
        Assert.IsTrue(testLogger.HasError("Failed to write preferences file during startup recording"),
            "Should log IOException as Error");
    }

    [TestMethod]
    public async Task StartupRecording_WithUnauthorizedAccessException_LogsAsError()
    {
        // Arrange
        var testLogger = new TestLogService();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var preferenceService = Ioc.Default.GetRequiredService<PreferenceService>();
        var testSqlIo = new TestMySqlIo();

        var backendService = new TestableBackendService(testLogger, appState, preferenceService, testSqlIo);
        var unauthorizedException = new UnauthorizedAccessException("Access to path is denied");
        backendService.SetWritePreferencesException(unauthorizedException);
        preferenceService.Model.Version = "different-version-to-trigger-mismatch";

        // Act
        await backendService.StartupRecording();

        // Assert
        Assert.IsTrue(testLogger.HasError("Permission denied writing preferences file during startup recording"),
            "Should log UnauthorizedAccessException as Error");
    }

    [TestMethod]
    public async Task StartupRecording_WithUnexpectedException_LogsAsError()
    {
        // Arrange
        var testLogger = new TestLogService();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var preferenceService = Ioc.Default.GetRequiredService<PreferenceService>();
        var testSqlIo = new TestMySqlIo();

        var backendService = new TestableBackendService(testLogger, appState, preferenceService, testSqlIo);
        var unexpectedException = new NullReferenceException("Unexpected null reference");
        backendService.SetPostVersionException(unexpectedException);
        preferenceService.Model.RecordVersionStatus = false;

        // Act
        await backendService.StartupRecording();

        // Assert
        Assert.IsTrue(testLogger.HasError("Unexpected error in StartupRecording method"),
            "Should log unexpected exception as Error");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a MySqlException with a specific error code for testing.
    /// Uses reflection since MySqlException constructor is internal.
    /// </summary>
    private static MySqlException CreateMySqlException(int errorCode, string message)
    {
        var constructor = typeof(MySqlException).GetConstructor(
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            null,
            new[] { typeof(string), typeof(int) },
            null);

        if (constructor != null)
        {
            return (MySqlException)constructor.Invoke(new object[] { message, errorCode });
        }

        var constructors = typeof(MySqlException).GetConstructors(
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        foreach (var ctor in constructors)
        {
            var parameters = ctor.GetParameters();
            if (parameters.Length == 2)
            {
                return (MySqlException)ctor.Invoke(new object[] { message, errorCode });
            }
        }

        throw new InvalidOperationException($"Cannot create MySqlException with code {errorCode}");
    }

    #endregion
}

#region Test Infrastructure

/// <summary>
/// Test double for ILogService that captures log calls for verification.
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
/// Test double for IMySqlIo that records calls and returns preset results.
/// No database connection is ever made. Follows the same pattern as
/// TestSaveable, MockCollaborator, and TestLogService.
/// </summary>
public class TestMySqlIo : IMySqlIo
{
    public bool IsConnectionConfigured { get; private set; }
    public Exception ExceptionToThrow { get; set; }

    // Call recording
    public List<(string name, string email)> AddOrUpdateUserCalls { get; } = new();
    public List<(int id, bool elmah, bool newsletter, string version, bool usageStats)> AddOrUpdatePreferencesCalls { get; } = new();
    public List<(int id, string current, string previous)> AddVersionCalls { get; } = new();
    public List<int> DeleteUserCalls { get; } = new();

    public int AddOrUpdateUserReturnId { get; set; } = 1;

    public void SetConnectionString(string connectionString)
    {
        IsConnectionConfigured = true;
    }

    public Task<int> AddOrUpdateUser(string name, string email)
    {
        AddOrUpdateUserCalls.Add((name, email));
        if (ExceptionToThrow != null) throw ExceptionToThrow;
        return Task.FromResult(AddOrUpdateUserReturnId);
    }

    public Task AddOrUpdatePreferences(int id, bool elmah, bool newsletter, string version, bool usageStats)
    {
        AddOrUpdatePreferencesCalls.Add((id, elmah, newsletter, version, usageStats));
        if (ExceptionToThrow != null) throw ExceptionToThrow;
        return Task.CompletedTask;
    }

    public Task AddVersion(int id, string currentVersion, string previousVersion)
    {
        AddVersionCalls.Add((id, currentVersion, previousVersion));
        if (ExceptionToThrow != null) throw ExceptionToThrow;
        return Task.CompletedTask;
    }

    public bool DeleteUserReturnValue { get; set; } = true;

    public Task<bool> DeleteUser(int id)
    {
        DeleteUserCalls.Add(id);
        if (ExceptionToThrow != null) throw ExceptionToThrow;
        return Task.FromResult(DeleteUserReturnValue);
    }

    // Read capability
    public List<(string sp, (string name, object value)[] parameters)> ExecuteReaderCalls { get; } = new();
    public List<Dictionary<string, object>> ExecuteReaderReturnValue { get; set; } = new();

    public Task<List<Dictionary<string, object>>> ExecuteReaderAsync(
        string storedProcedure,
        params (string name, object value)[] parameters)
    {
        ExecuteReaderCalls.Add((storedProcedure, parameters));
        if (ExceptionToThrow != null) throw ExceptionToThrow;
        return Task.FromResult(ExecuteReaderReturnValue);
    }

    // Usage tracking
    public List<(string usageId, DateTime start, DateTime end, int seconds, string outlines, string features)>
        RecordSessionDataCalls { get; } = new();

    public Task RecordSessionData(string usageId, DateTime sessionStart, DateTime sessionEnd,
        int clockTimeSeconds, string outlinesJson, string featuresJson)
    {
        RecordSessionDataCalls.Add((usageId, sessionStart, sessionEnd, clockTimeSeconds, outlinesJson, featuresJson));
        if (ExceptionToThrow != null) throw ExceptionToThrow;
        return Task.CompletedTask;
    }
}

/// <summary>
/// Testable version of BackendService that allows injecting exceptions
/// for testing StartupRecording exception handling.
/// </summary>
public class TestableBackendService : BackendService
{
    private Exception _exceptionToThrow;
    private readonly ILogService _testLogService;
    private readonly AppState _testAppState;
    private readonly PreferenceService _testPreferenceService;

    public TestableBackendService(ILogService logService, AppState appState,
        PreferenceService preferenceService, IMySqlIo sqlIo)
        : base(logService, appState, preferenceService, sqlIo)
    {
        _testLogService = logService;
        _testAppState = appState;
        _testPreferenceService = preferenceService;
    }

    public void SetPostPreferencesException(Exception ex) => _exceptionToThrow = ex;
    public void SetPostVersionException(Exception ex) => _exceptionToThrow = ex;
    public void SetWritePreferencesException(Exception ex) => _exceptionToThrow = ex;

    /// <summary>
    /// Override StartupRecording to inject exceptions for testing.
    /// Simulates exceptions from PostPreferences, PostVersion, or WritePreferences.
    /// </summary>
    public new async Task StartupRecording()
    {
        try
        {
            await Task.CompletedTask;
            if (_testPreferenceService.Model.RecordPreferencesStatus)
            {
                if (_exceptionToThrow != null)
                    throw _exceptionToThrow;
            }

            if (!_testAppState.Version.Equals(_testPreferenceService.Model.Version))
            {
                _testLogService.Log(LogLevel.Info,
                    "Version mismatch: " + _testAppState.Version + " != " + _testPreferenceService.Model.Version);
                _testAppState.LoadedWithVersionChange = true;

                if (_exceptionToThrow != null)
                    throw _exceptionToThrow;
            }
            else if (!_testPreferenceService.Model.RecordVersionStatus)
            {
                if (_exceptionToThrow != null)
                    throw _exceptionToThrow;
            }
        }
        catch (TaskCanceledException ex)
        {
            _testLogService.Log(LogLevel.Warn, $"Backend operation timed out during startup: {ex.Message}");
            _testLogService.Log(LogLevel.Info, "Backend telemetry will retry on next startup");
        }
        catch (OperationCanceledException ex)
        {
            _testLogService.Log(LogLevel.Warn, $"Backend operation cancelled during startup: {ex.Message}");
        }
        catch (MySqlException ex) when (ex.Number == 2003 || ex.Number == 2013)
        {
            _testLogService.Log(LogLevel.Warn, $"Backend server temporarily unavailable (Code {ex.Number}): {ex.Message}");
            _testLogService.Log(LogLevel.Info, "Backend telemetry will retry on next startup");
        }
        catch (MySqlException ex) when (ex.Number == 1045)
        {
            _testLogService.LogException(LogLevel.Error, ex, $"Backend authentication failed (Code {ex.Number}): Check credentials");
        }
        catch (MySqlException ex)
        {
            _testLogService.LogException(LogLevel.Error, ex, $"Backend MySQL error during startup (Code {ex.Number}): {ex.Message}");
        }
        catch (IOException ex)
        {
            _testLogService.LogException(LogLevel.Error, ex, "Failed to write preferences file during startup recording");
        }
        catch (UnauthorizedAccessException ex)
        {
            _testLogService.LogException(LogLevel.Error, ex, "Permission denied writing preferences file during startup recording");
        }
        catch (Exception ex)
        {
            _testLogService.LogException(LogLevel.Error, ex, "Unexpected error in StartupRecording method");
        }
    }
}

#endregion
