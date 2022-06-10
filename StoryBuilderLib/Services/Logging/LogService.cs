using NLog;
using NLog.Config;
using NLog.Targets;
using Elmah.Io.NLog;
using StoryBuilder.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using StoryBuilder.Services.Json;
using CommunityToolkit.WinUI.UI.Controls.TextToolbarSymbols;
using Elmah.Io.Client;

namespace StoryBuilder.Services.Logging;

/// <summary>
/// Manage the Task Log file.
/// </summary>
public class LogService : ILogService
{
    private static readonly Logger Logger;
    private static readonly string logFilePath;
    private static string stackTraceHelper; //Elmah for some reason doesn't show the stack trace of an exception so this one does.
    private static string logfilename;
    static LogService()

    {
        try
        {
            LoggingConfiguration config = new();

            // Create the file logging target
            FileTarget fileTarget = new();
            logFilePath = Path.Combine(GlobalData.RootDirectory, "logs");
            logfilename = Path.Combine(logFilePath, "updater.${date:format=yyyy-MM-dd}.log");
            fileTarget.FileName = logfilename;
            fileTarget.CreateDirs = true;
            fileTarget.MaxArchiveFiles = 7;
            fileTarget.ArchiveEvery = FileArchivePeriod.Day;
            fileTarget.ConcurrentWrites = true;
            fileTarget.Layout = "${longdate} | ${level} | ${message} | ${exception:format=Message,StackTrace,Data:MaxInnerExceptionLevel=5}";
            LoggingRule fileRule = new("*", NLog.LogLevel.Trace, fileTarget);
            config.AddTarget("logfile", fileTarget);
            config.LoggingRules.Add(fileRule);

            // create console target
            if (Debugger.IsAttached)
            {
                ColoredConsoleTarget consoleTarget = new();
                consoleTarget.Layout = @"${date:format=HH\\:MM\\:ss} ${logger} ${message}";
                config.AddTarget("console", consoleTarget);
                LoggingRule consoleRule = new("*", NLog.LogLevel.Info, consoleTarget);
                config.LoggingRules.Add(consoleRule);
            }
            LogManager.Configuration = config;
            Logger = LogManager.GetCurrentClassLogger();
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.Message);
            Debug.WriteLine(e.StackTrace);
        }
    }

    public async Task<bool> AddElmahTarget()
    {
        string apiKey = string.Empty;
        string logID = string.Empty;

        // create elmah.io target if keys are defined
        try
        {
            var doppler = new Doppler();
            var keys = await doppler.FetchSecretsAsync();
            apiKey = keys.APIKEY;
            logID = keys.LOGID;
        }
        catch (Exception ex) 
        {
            LogException(LogLevel.Error, ex, ex.Message);
            return false;
        }
        if (apiKey == string.Empty | logID == string.Empty)
            return false;

        try
        {
            // create elmah.io target
            var elmahIoTarget = new ElmahIoTarget();

            elmahIoTarget.OnMessage += msg =>
            {
                msg.Version = Windows.ApplicationModel.Package.Current.Id.Version.Major + "."
                + Windows.ApplicationModel.Package.Current.Id.Version.Minor + "."
                + Windows.ApplicationModel.Package.Current.Id.Version.Revision;
                
                try { msg.User = GlobalData.Preferences.Name + $"({GlobalData.Preferences.Email})"; }
                catch (Exception e) { msg.User = $"There was an error attempting to obtain user information Error: {e.Message}"; }

                try { msg.Source = stackTraceHelper; } 
                catch (Exception e) {msg.Source = $"There was an error attempting to obtain StackTrace helper Error: {e.Message}";}
                
                try
                {
                    msg.Version = Windows.ApplicationModel.Package.Current.Id.Version.Major + "."
                        + Windows.ApplicationModel.Package.Current.Id.Version.Minor + "."
                        + Windows.ApplicationModel.Package.Current.Id.Version.Build + " Build " + Windows.ApplicationModel.Package.Current.Id.Version.Revision;
                }
                catch (Exception e) { msg.Version = $"There was an error trying to obtain version information Error: {e.Message}"; }

                try
                {
                    msg.Data = new List<Item>();
                    string LogString = "";
                    using (FileStream stream = File.Open(Path.Combine(GlobalData.RootDirectory, "logs", $"updater.{DateTime.Now.ToString("yyyy-MM-dd")}.log"), FileMode.Open, FileAccess.Read,FileShare.ReadWrite))
                    {
                        using (StreamReader reader = new(stream))
                        {
                            LogString = reader.ReadToEnd();
                        }
                    }

                    int ln = 0;
                    if (LogString.Split("\n").Length > 50)
                    {
                        foreach (var line in LogString.Split("\n").TakeLast(50))
                        {
                            msg.Data.Add(new(key: "Line " + ln, value: line));
                            ln++;
                        }
                    }
                    else
                    {
                        foreach (var line in LogString.Split("\n").TakeLast(50))
                        {
                            msg.Data.Add(new(key: "Line ", value: line));
                            ln++;
                        }
                    }
                    msg.Data.Add(new(key: "Log ","end"));
                }
                catch (Exception e) { msg.Data.Add(new("Error", $"There was an error attempting to obtain the log Error: {e.Message}"));}
            };

            elmahIoTarget.Name = "elmahio";
            elmahIoTarget.ApiKey = apiKey;
            elmahIoTarget.LogId = logID;
            LogManager.Configuration.AddTarget(elmahIoTarget);
            LogManager.Configuration.AddRule(NLog.LogLevel.Error, NLog.LogLevel.Fatal, elmahIoTarget, "*");
            LogManager.ReconfigExistingLoggers();
            GlobalData.ElmahLogging = true;
            return true;
        }
        catch (Exception ex)
        {
            LogException(LogLevel.Error, ex, ex.Message);
            return false;
        }
    }
    public LogService()
    {
        Log(LogLevel.Info, "Starting Log service");
        Log(LogLevel.Info, "Detailed log at " + logFilePath);
    }


    public void Log(LogLevel level, string message)
    {
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
        switch (level)
        {
            case LogLevel.Error:
                stackTraceHelper = exception.StackTrace;
                Logger.Error(exception, message);
                break;
            case LogLevel.Fatal:
                stackTraceHelper = exception.StackTrace;
                Logger.Fatal(exception, message);
                break;
        }
    }

    public void Flush()
    {
        LogManager.Flush();
    }
}