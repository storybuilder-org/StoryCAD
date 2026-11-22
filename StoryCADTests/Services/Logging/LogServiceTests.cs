using CommunityToolkit.Mvvm.DependencyInjection;
using Elmah.Io.Client;
using NLog;
using NLog.Config;
using NLog.Targets;
using StoryCADLib.Services.Logging;

#nullable disable

namespace StoryCADTests.Services.Logging;

[TestClass]
public class LogServiceTests
{
    private MemoryTarget _target;

    [TestInitialize]
    public void Setup()
    {
        // Resolving LogService first so its static constructor has set LogManager.Configuration.
        // The memory target sits beside the real targets and replaces nothing.
        Ioc.Default.GetRequiredService<ILogService>();
        _target = new MemoryTarget("LogServiceTests") { Layout = "${level}|${message}" };
        LogManager.Configuration.AddRule(NLog.LogLevel.Error, NLog.LogLevel.Fatal, _target);
        LogManager.ReconfigExistingLoggers();
    }

    [TestCleanup]
    public void Cleanup()
    {
        LogManager.Configuration.RemoveTarget(_target.Name);
        LogManager.ReconfigExistingLoggers();
    }

    [TestMethod]
    public void LogException_AtError_WritesOneEvent()
    {
        // Arrange - each Error event becomes one elmah.io report (#1615)
        var log = Ioc.Default.GetRequiredService<ILogService>();

        // Act
        log.LogException(StoryCADLib.Services.Logging.LogLevel.Error, new InvalidOperationException("boom"),
            "LogServiceTests error");

        // Assert
        Assert.AreEqual(1, _target.Logs.Count(l => l.Contains("LogServiceTests error")));
    }

    [TestMethod]
    public void LogException_AtFatal_WritesOneFatalEvent()
    {
        // Arrange
        var log = Ioc.Default.GetRequiredService<ILogService>();

        // Act
        log.LogException(StoryCADLib.Services.Logging.LogLevel.Fatal, new InvalidOperationException("boom"),
            "LogServiceTests fatal");

        // Assert
        var events = _target.Logs.Where(l => l.Contains("LogServiceTests fatal")).ToList();
        Assert.AreEqual(1, events.Count);
        Assert.IsTrue(events[0].StartsWith("Fatal|"));
    }

    [TestMethod]
    public void DecorateMessage_AfterEarlierException_AddsNoException()
    {
        // Arrange - an earlier exception must not reach a report that has none of its own (#1638)
        var log = Ioc.Default.GetRequiredService<LogService>();
        log.LogException(StoryCADLib.Services.Logging.LogLevel.Error, new InvalidOperationException("earlier"),
            "LogServiceTests earlier error");
        var msg = new CreateMessage { Title = "entry without exception" };

        // Act
        log.DecorateMessage(msg);

        // Assert
        Assert.IsNull(msg.Type);
        Assert.IsNull(msg.Source);
        Assert.IsNull(msg.Detail);
    }

    [TestMethod]
    public void DecorateMessage_WithOwnException_KeepsExceptionFields()
    {
        // Arrange - the target fills these fields from the entry's own exception (#1638)
        var log = Ioc.Default.GetRequiredService<LogService>();
        log.LogException(StoryCADLib.Services.Logging.LogLevel.Error, new InvalidOperationException("earlier"),
            "LogServiceTests earlier error");
        var msg = new CreateMessage
        {
            Title = "entry with exception",
            Type = "System.ArgumentException",
            Source = "LogServiceTests",
            Detail = "own detail",
            Data = new List<Item> { new("Own", "value") }
        };

        // Act
        log.DecorateMessage(msg);

        // Assert
        Assert.AreEqual("System.ArgumentException", msg.Type);
        Assert.AreEqual("LogServiceTests", msg.Source);
        Assert.AreEqual("own detail", msg.Detail);
        Assert.IsTrue(msg.Data.Any(i => i.Key == "Own"));
    }

    [TestMethod]
    public void AddReportRule_SendsErrorButNotWarn()
    {
        // Arrange - a Warn entry must not become an elmah.io report (#1638)
        var config = new LoggingConfiguration();
        var target = new MemoryTarget("report");

        // Act
        LogService.AddReportRule(config, target);

        // Assert
        var rule = config.LoggingRules.Single(r => r.Targets.Contains(target));
        Assert.IsTrue(rule.IsLoggingEnabledForLevel(NLog.LogLevel.Error));
        Assert.IsFalse(rule.IsLoggingEnabledForLevel(NLog.LogLevel.Warn));
    }

    [TestMethod]
    public void LogFileRule_TakesWarn()
    {
        // Arrange - an unavailable file is logged at Warn and must still reach the log file (#1638).
        // "logfile" is the target's key in the configuration; NLog leaves Target.Name empty.
        var fileTarget = LogManager.Configuration.FindTargetByName("logfile");
        var rule = LogManager.Configuration.LoggingRules.Single(r => r.Targets.Contains(fileTarget));

        // Act
        var takesWarn = rule.IsLoggingEnabledForLevel(NLog.LogLevel.Warn);

        // Assert
        Assert.IsTrue(takesWarn);
    }
}
