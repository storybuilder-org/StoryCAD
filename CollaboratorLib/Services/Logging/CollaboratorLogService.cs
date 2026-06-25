using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Elmah.Io.Client;
using Elmah.Io.NLog;
using NLog;
using NLog.Config;
using NLog.Targets;
using StoryCADLib.Services.Json;
using StoryCADLib.Services.Logging;
using LogLevelAlias = StoryCADLib.Services.Logging.LogLevel;

namespace StoryCollaborator.Services.Logging;

/// <summary>
/// Collaborator-specific logger that keeps data out of StoryCAD's global logging pipeline.
/// </summary>
public class CollaboratorLogService : ICollaboratorLogger
{
    private readonly CollaboratorLogOptions _options;
    private readonly LogFactory _logFactory;
    private readonly LoggingConfiguration _config;
    private readonly Logger _logger;
    private Exception? _lastException;
    private ElmahIoTarget? _elmahTarget;

    public bool ElmahLogging { get; private set; }

    public CollaboratorLogService(CollaboratorLogOptions? options = null)
    {
        _options = options ?? new CollaboratorLogOptions();
        Directory.CreateDirectory(_options.LogFolder);

        // NLog 5.0+: Create LogFactory first, then pass to LoggingConfiguration
        _logFactory = new LogFactory();
        _config = new LoggingConfiguration(_logFactory);

        var fileTarget = new FileTarget("collaborator-log")
        {
            FileName = Path.Combine(_options.LogFolder, "collaborator.${date:format=yyyy-MM-dd}.log"),
            CreateDirs = true,
            MaxArchiveFiles = 7,
            ArchiveEvery = FileArchivePeriod.Day,
            Layout = "${longdate} | ${level} | ${message} | ${exception:format=Message,StackTrace:MaxInnerExceptionLevel=5}"
        };

        _config.AddTarget(fileTarget);
        _config.LoggingRules.Add(new LoggingRule("*", _options.MinimumLevel, fileTarget));

        if (Debugger.IsAttached)
        {
            var consoleTarget = new ColoredConsoleTarget("collaborator-console")
            {
                Layout = @"${date:format=HH\\:mm\\:ss} ${logger} ${message}"
            };
            _config.AddTarget(consoleTarget);
            _config.LoggingRules.Add(new LoggingRule("*", _options.MinimumLevel, consoleTarget));
        }

        _logFactory.Configuration = _config;
        _logger = _logFactory.GetLogger("Collaborator");
        Log(LogLevelAlias.Info, "Collaborator logging initialized");
        Log(LogLevelAlias.Info, $"Log folder: {_options.LogFolder}");
    }

    public void EnableSensitiveLogging(bool allow)
    {
        _options.AllowSensitiveLogging = allow;
        Log(LogLevelAlias.Info, $"Sensitive logging {(allow ? "enabled" : "disabled")}");
    }

    public void Log(LogLevelAlias level, string message)
    {
        Write(level, message);
    }

    public void LogSensitive(LogLevelAlias level, string message)
    {
        if (_options.AllowSensitiveLogging)
        {
            Write(level, message);
        }
        else
        {
            Write(level, "[redacted]");
        }
    }

    public void LogException(LogLevelAlias level, Exception exception, string message)
    {
        _lastException = exception;
        Write(level, message, exception);
    }

    public void SetElmahTokens(Doppler keys)
    {
        _options.ElmahApiKey = keys.APIKEY;
        _options.ElmahLogId = keys.LOGID;
    }

    public bool AddElmahTarget()
    {
        if (string.IsNullOrWhiteSpace(_options.ElmahApiKey) || string.IsNullOrWhiteSpace(_options.ElmahLogId))
        {
            return false;
        }

        if (_elmahTarget != null)
        {
            return true;
        }

        _elmahTarget = new ElmahIoTarget
        {
            Name = "collaborator-elmah",
            ApiKey = _options.ElmahApiKey,
            LogId = _options.ElmahLogId,
        };

        _elmahTarget.OnMessage += msg =>
        {
            msg.Detail = _lastException?.ToString();
            msg.Data = new List<Item>
            {
                new("Environment", SystemInfo()),
            };
        };

        _config.AddTarget(_elmahTarget);
        _config.LoggingRules.Add(new LoggingRule("*", NLog.LogLevel.Error, _elmahTarget));
        _logFactory.ReconfigExistingLoggers();
        ElmahLogging = true;
        return true;
    }

    public void Flush()
    {
        _logFactory.Flush();
    }

    private void Write(LogLevelAlias level, string message, Exception? exception = null)
    {
        var mapped = level switch
        {
            LogLevelAlias.Trace => NLog.LogLevel.Trace,
            LogLevelAlias.Debug => NLog.LogLevel.Debug,
            LogLevelAlias.Info => NLog.LogLevel.Info,
            LogLevelAlias.Warn => NLog.LogLevel.Warn,
            LogLevelAlias.Error => NLog.LogLevel.Error,
            LogLevelAlias.Fatal => NLog.LogLevel.Fatal,
            _ => NLog.LogLevel.Info
        };

        if (exception != null)
        {
            _logger.Log(mapped, exception, message);
        }
        else
        {
            _logger.Log(mapped, message);
        }
    }

    private static string SystemInfo()
    {
        var arch = RuntimeInformation.ProcessArchitecture;
        return $"OS: {Environment.OSVersion}; Architecture: {arch}; CPUs: {Environment.ProcessorCount}";
    }
}
