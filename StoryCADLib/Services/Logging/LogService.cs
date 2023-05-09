using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Devices.Input;
using Elmah.Io.Client;
using Elmah.Io.NLog;
using NLog;
using NLog.Config;
using NLog.Targets;
using StoryCAD.Models;
using StoryCAD.Services.Json;
using Microsoft.UI.Windowing;

namespace StoryCAD.Services.Logging;

/// <summary>
/// Manage the Task Log file.
/// </summary>
public class LogService : ILogService
{
    private static readonly Logger Logger;
    private static readonly string logFilePath;
    private static Exception exceptionHelper;
    private string apiKey = string.Empty;
    private string logID = string.Empty;
    static LogService()
    {
        try
        {
            LoggingConfiguration config = new();

            // Create the file logging target
            FileTarget fileTarget = new();
            logFilePath = Path.Combine(GlobalData.RootDirectory, "logs");
            fileTarget.FileName = Path.Combine(logFilePath, "updater.${date:format=yyyy-MM-dd}.log");
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

    public bool AddElmahTarget()
    {
        if (apiKey == string.Empty | logID == string.Empty)
            return false;

        try
        {
            // create elmah.io target
            ElmahIoTarget elmahIoTarget = new ElmahIoTarget();

            elmahIoTarget.OnMessage += msg =>
            {
                msg.Version = GlobalData.Version;
                
                try { msg.User = GlobalData.Preferences.Name + $"({GlobalData.Preferences.Email})"; }
                catch (Exception e) { msg.User = $"There was an error attempting to obtain user information Error: {e.Message}"; }

                try { msg.Detail = exceptionHelper?.ToString(); } 
                catch (Exception e) {msg.Detail = $"There was an error attempting to obtain StackTrace helper Error: {e.Message}";}
                
                try { msg.Version = GlobalData.Version; }
                catch (Exception e) { msg.Version = $"There was an error trying to obtain version information Error: {e.Message}"; }

                var baseException = exceptionHelper?.GetBaseException();

                msg.Type = baseException?.GetType().FullName;
                msg.Source = baseException?.Source;
                msg.Hostname = Hostname();
                msg.ServerVariables = new List<Item>
                {
                    new Item("User-Agent", $"X-ELMAHIO-APPLICATION; OS=Windows; OSVERSION={Environment.OSVersion.Version}; ENGINE=WinUI")
                };

                try
                {
                    msg.Data = new List<Item>();

                    var mainWindow = GlobalData.MainWindow;
                    if (mainWindow?.Width > 0) msg.Data.Add(new Item("Browser-Width", ((int)mainWindow.Width).ToString()));
                    if (mainWindow?.Height > 0) msg.Data.Add(new Item("Browser-Height", ((int)mainWindow.Height).ToString()));
                    if (DisplayArea.Primary?.WorkArea.Width > 0) msg.Data.Add(new Item("Screen-Width", DisplayArea.Primary.WorkArea.Width.ToString()));
                    if (DisplayArea.Primary?.WorkArea.Height > 0) msg.Data.Add(new Item("Screen-Height", DisplayArea.Primary.WorkArea.Height.ToString()));

                    string LogString = string.Empty;

                    try
                    {
                        msg.Data.Add(new(key: "Line " + 0, value: "OS Version: " + Environment.OSVersion
                            + "\nProcessor Count: " + Environment.ProcessorCount
                            + "\nProcessID:" + Environment.ProcessId
                            + "\nArchitecture:" + RuntimeInformation.ProcessArchitecture
                            + "\nTouchscreen:" + PointerDevice.GetPointerDevices().Any(p => p.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Touch)
                            + "\nWindows Build: " + Environment.OSVersion.Version.Build
                            + "\nUnoffical Build: " + GlobalData.DeveloperBuild));
                    }
                    catch (Exception ex)
                    {
                        msg.Data.Add(new(key: "Line " + 0, value: $"failed getting system info ({ex.Message})"));
                    }

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
                        foreach (string line in LogString.Split("\n").TakeLast(50))
                        {
                            msg.Data.Add(new(key: "Line " + ln, value: line));
                            ln++;
                        }
                    }
                    else
                    {
                        foreach (string line in LogString.Split("\n").TakeLast(50))
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
            LogManager.Configuration.AddRule(NLog.LogLevel.Error, NLog.LogLevel.Fatal, elmahIoTarget);
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

    private string Hostname()
    {
        var machineName = Environment.MachineName;
        if (!string.IsNullOrWhiteSpace(machineName)) return machineName;

        return Environment.GetEnvironmentVariable("COMPUTERNAME");
    }

    public void SetElmahTokens(Doppler keys)
    {
        apiKey = keys.APIKEY;
        logID = keys.LOGID;
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
                exceptionHelper = exception;
                Logger.Error(exception, message);
                break;
            case LogLevel.Fatal:
                exceptionHelper = exception;
                Logger.Fatal(exception, message);
                break;
        }
    }

    public void Flush()
    {
        LogManager.Flush();
    }
}