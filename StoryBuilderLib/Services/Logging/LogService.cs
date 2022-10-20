using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Elmah.Io.Client;
using Elmah.Io.NLog;
using NLog;
using NLog.Config;
using NLog.Targets;
using StoryBuilder.Models;
using StoryBuilder.Services.Json;

namespace StoryBuilder.Services.Logging;

/// <summary>
/// Manage the Task Log file.
/// </summary>
public class LogService : ILogService
{
    private static readonly Logger Logger;
    private static readonly string LogFilePath;
    private static string _stackTraceHelper; //Elmah for some reason doesn't show the stack trace of an exception so this one does.
    private string _apiKey = string.Empty;
    private string _logId = string.Empty;
    static LogService()
    {
        try
        {
            LoggingConfiguration _Config = new();

            // Create the file logging target
            FileTarget _FileTarget = new();
            LogFilePath = Path.Combine(GlobalData.RootDirectory, "logs");
            _FileTarget.FileName = Path.Combine(LogFilePath, "updater.${date:format=yyyy-MM-dd}.log");
            _FileTarget.CreateDirs = true;
            _FileTarget.MaxArchiveFiles = 7;
            _FileTarget.ArchiveEvery = FileArchivePeriod.Day;
            _FileTarget.ConcurrentWrites = true;
            _FileTarget.Layout = "${longdate} | ${level} | ${message} | ${exception:format=Message,StackTrace,Data:MaxInnerExceptionLevel=5}";
            LoggingRule _FileRule = new("*", NLog.LogLevel.Info, _FileTarget);
            _Config.AddTarget("logfile", _FileTarget);
            _Config.LoggingRules.Add(_FileRule);

            // create console target
            if (Debugger.IsAttached)
            {
                ColoredConsoleTarget _ConsoleTarget = new();
                _ConsoleTarget.Layout = @"${date:format=HH\\:MM\\:ss} ${logger} ${message}";
                _Config.AddTarget("console", _ConsoleTarget);
                LoggingRule _ConsoleRule = new("*", NLog.LogLevel.Info, _ConsoleTarget);
                _Config.LoggingRules.Add(_ConsoleRule);
            }
            LogManager.Configuration = _Config;
            Logger = LogManager.GetCurrentClassLogger();
        }
        catch (Exception _E)
        {
            Debug.WriteLine(_E.Message);
            Debug.WriteLine(_E.StackTrace);
        }
    }

    public Task<bool> AddElmahTarget()
    {
        if (_apiKey == string.Empty | _logId == string.Empty)
            return Task.FromResult(false);

        try
        {
            // create elmah.io target
            var _ElmahIoTarget = new ElmahIoTarget();

            _ElmahIoTarget.OnMessage += msg =>
            {
                msg.Version = Package.Current.Id.Version.Major + "."
                                                               + Package.Current.Id.Version.Minor + "."
                                                               + Package.Current.Id.Version.Revision;
                
                try { msg.User = GlobalData.Preferences.Name + $"({GlobalData.Preferences.Email})"; }
                catch (Exception _E) { msg.User = $"There was an error attempting to obtain user information Error: {_E.Message}"; }

                try { msg.Source = _stackTraceHelper; } 
                catch (Exception _E) {msg.Source = $"There was an error attempting to obtain StackTrace helper Error: {_E.Message}";}
                
                try
                {
                    msg.Version = Package.Current.Id.Version.Major + "."
                                                                   + Package.Current.Id.Version.Minor + "."
                                                                   + Package.Current.Id.Version.Build + " Build " + Package.Current.Id.Version.Revision;
                }
                catch (Exception _E) { msg.Version = $"There was an error trying to obtain version information Error: {_E.Message}"; }

                try
                {
                    msg.Data = new List<Item>();
                    string _LogString;
                    using (FileStream _Stream = File.Open(Path.Combine(GlobalData.RootDirectory, "logs", $"updater.{DateTime.Now.ToString("yyyy-MM-dd")}.log"), FileMode.Open, FileAccess.Read,FileShare.ReadWrite))
                    {
                        using (StreamReader _Reader = new(_Stream))
                        {
                            _LogString = _Reader.ReadToEnd();
                        }
                    }

                    int _Ln = 0;
                    if (_LogString.Split("\n").Length > 50)
                    {
                        foreach (var _Line in _LogString.Split("\n").TakeLast(50))
                        {
                            msg.Data.Add(new(key: "Line " + _Ln, value: _Line));
                            _Ln++;
                        }
                    }
                    else
                    {
                        foreach (var _Line in _LogString.Split("\n").TakeLast(50))
                        {
                            msg.Data.Add(new(key: "Line ", value: _Line));
                            _Ln++;
                        }
                    }
                    msg.Data.Add(new(key: "Log ","end"));
                }
                catch (Exception _E) { msg.Data.Add(new("Error", $"There was an error attempting to obtain the log Error: {_E.Message}"));}
            };

            _ElmahIoTarget.Name = "elmahio";
            _ElmahIoTarget.ApiKey = _apiKey;
            _ElmahIoTarget.LogId = _logId;
            LogManager.Configuration.AddTarget(_ElmahIoTarget);
            LogManager.Configuration.AddRule(NLog.LogLevel.Error, NLog.LogLevel.Fatal, _ElmahIoTarget);
            LogManager.ReconfigExistingLoggers();
            GlobalData.ElmahLogging = true;
            return Task.FromResult(true);
        }
        catch (Exception _Ex)
        {
            LogException(LogLevel.Error, _Ex, _Ex.Message);
            return Task.FromResult(false);
        }
    }

    public void SetElmahTokens(Doppler keys)
    {
        apiKey = keys.APIKEY;
        logID = keys.LOGID;
    }

    public LogService()
    {
        Log(LogLevel.Info, "Starting Log service");
        Log(LogLevel.Info, "Detailed log at " + LogFilePath);
    }


    public void Log(LogLevel level, string message)
    {
        // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
        switch (level)
        {
            case LogLevel.Trace:
                Logger.Trace(message);
                break;
            case LogLevel.Debug:
                Logger.Debug(message);
                break;
            case LogLevel.Info:
                Logger.Info(message);
                break;
            case LogLevel.Warn:
                Logger.Warn(message);
                break;
            case LogLevel.Error:
                Logger.Error(message);
                break;
            case LogLevel.Fatal:
                Logger.Fatal(message);
                break;
        }
    }

    public void LogException(LogLevel level, Exception exception, string message)
    {
        // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
        switch (level)
        {
            case LogLevel.Error:
                _stackTraceHelper = exception.StackTrace;
                Logger.Error(exception, message);
                break;
            case LogLevel.Fatal:
                _stackTraceHelper = exception.StackTrace;
                Logger.Fatal(exception, message);
                break;
        }
    }

    public void Flush()
    {
        LogManager.Flush();
    }
}