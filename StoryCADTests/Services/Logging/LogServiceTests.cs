using CommunityToolkit.Mvvm.DependencyInjection;
using NLog;
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
}
