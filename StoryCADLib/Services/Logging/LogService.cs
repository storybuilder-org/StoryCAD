using System.Diagnostics;
using Elmah.Io.Client;
using Elmah.Io.NLog;
using NLog;
using NLog.Config;
using NLog.Targets;
using StoryCAD.Services.Json;
using Microsoft.UI.Windowing;
using CommunityToolkit.WinUI.Helpers;
using System.Runtime.InteropServices;
using Windows.Devices.Input;
using StoryCAD.Models.Tools;

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
    private static AppState State = Ioc.Default.GetRequiredService<AppState>();
    public static NLog.LogLevel MinLogLevel = NLog.LogLevel.Info;
    public bool ElmahLogging;
    static LogService()
    {
        try
        {
            LoggingConfiguration config = new();

            // Create the file logging target
            FileTarget fileTarget = new();
            logFilePath = Path.Combine(Ioc.Default.GetRequiredService<AppState>().RootDirectory, "logs");
            fileTarget.FileName = Path.Combine(logFilePath, "updater.${date:format=yyyy-MM-dd}.log");
            fileTarget.CreateDirs = true;
            fileTarget.MaxArchiveFiles = 7;
            fileTarget.ArchiveEvery = FileArchivePeriod.Day;
            fileTarget.ConcurrentWrites = true;
            fileTarget.Layout = "${longdate} | ${level} | ${message} | ${exception:format=Message,StackTrace,Data:MaxInnerExceptionLevel=5}";
            LoggingRule fileRule = new("*", NLog.LogLevel.Off, fileTarget);
            if (Ioc.Default.GetRequiredService<PreferenceService>().Model.AdvancedLogging)
                MinLogLevel = NLog.LogLevel.Trace;
            else
                MinLogLevel = NLog.LogLevel.Info;
            fileRule.EnableLoggingForLevels(MinLogLevel, NLog.LogLevel.Fatal);
            config.AddTarget("logfile", fileTarget);
            config.LoggingRules.Add(fileRule);

            // create console target
            if (Debugger.IsAttached)
            {
                ColoredConsoleTarget consoleTarget = new();
                consoleTarget.Layout = @"${date:format=HH\\:MM\\:ss} ${logger} ${message}";
                config.AddTarget("console", consoleTarget);
                LoggingRule consoleRule = new("*", NLog.LogLevel.Off, consoleTarget);
                fileRule.EnableLoggingForLevels(MinLogLevel, NLog.LogLevel.Fatal);
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
        if (apiKey == string.Empty || logID == string.Empty)
            return false;

        try
        {
            // create elmah.io target
            ElmahIoTarget elmahIoTarget = new();

            elmahIoTarget.OnMessage += msg =>
            {
                msg.Version = State.Version;


                try { msg.Detail = exceptionHelper?.ToString(); }
                catch (Exception e)
                {
                    msg.Detail = $"There was an error attempting to obtain StackTrace helper Error: {e.Message}";
                }

                try { msg.Version = State.Version; }
                catch (Exception e)
                {
                    msg.Version = $"There was an error trying to obtain version information Error: {e.Message}";
                }

                var baseException = exceptionHelper?.GetBaseException();

                msg.Type = baseException?.GetType().FullName;
                msg.Data = baseException?.ToDataList();
                msg.Source = baseException?.Source;
                msg.Hostname = Hostname();
                msg.ServerVariables = new List<Item>
                {
                    new("User-Agent",
                        $"X-ELMAHIO-APPLICATION; OS=Windows; OSVERSION={Environment.OSVersion.Version}; ENGINE=WinUI")
                };

                try
                {
                    try
                    {
                        var mainWindow = Ioc.Default.GetRequiredService<Windowing>().MainWindow;
                        if (mainWindow?.Width > 0)
                            msg.Data.Add(new Item("Browser-Width", ((int)mainWindow.Width).ToString()));
                        if (mainWindow?.Height > 0)
                            msg.Data.Add(new Item("Browser-Height", ((int)mainWindow.Height).ToString()));
                        if (DisplayArea.Primary?.WorkArea.Width > 0)
                            msg.Data.Add(new Item("Screen-Width", DisplayArea.Primary.WorkArea.Width.ToString()));
                        if (DisplayArea.Primary?.WorkArea.Height > 0)
                            msg.Data.Add(new Item("Screen-Height", DisplayArea.Primary.WorkArea.Height.ToString()));
                    }
                    catch (Exception ex) { msg.Data.Add(new Item($"An error occurred trying to obtain window size data {ex.Message}")); }


                    string LogString = string.Empty;
                
                    try
                    {
                        msg.Data.Add(new(key: "SystemInfo", SystemInfo()));
                    }
                    catch (Exception ex)
                    {
                        msg.Data.Add(new(key: "Line " + 0, value: $"failed getting system info ({ex.Message})"));
                    }

                    using (FileStream stream = File.Open(Path.Combine(Ioc.Default.GetRequiredService<AppState>().RootDirectory, "logs", $"updater.{DateTime.Now.ToString("yyyy-MM-dd")}.log"), FileMode.Open, FileAccess.Read,FileShare.ReadWrite))
                    {
                        using (StreamReader reader = new(stream))
                        {
                            LogString = reader.ReadToEnd();
                        }
                    }

                    int ln = 0;
                    if (LogString.Split("\n").Length > 250)
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
            ElmahLogging = true;
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
                Logger.Error($"{message}\nStack Trace:\n{exception.StackTrace}");
                break;
            case LogLevel.Fatal:
                exceptionHelper = exception;
                Logger.Fatal(exception, message);
                Logger.Error($"{message}\nStack Trace:\n{exception.StackTrace}");
                break;
        }
    }

    public void Flush()
    {
        LogManager.Flush();
    }


	/// <summary>
	/// Compiles a small report about the users device, StoryCAD information etc.
	/// </summary>
	/// <returns>StoryCAD Device Report</returns>
	public string SystemInfo()
	{
		try
		{
			PreferencesModel Prefs =  Ioc.Default.GetRequiredService<PreferenceService>().Model;
			AppState State =  Ioc.Default.GetRequiredService<AppState>();
			string AppArch = IntPtr.Size switch
			{
				4 => "32 bit",
				8 => "64 bit",
				_ => "Unknown"
			};

			string WinVer;
			try
			{
				WinVer = Environment.OSVersion.Version.Build >= 22000 ? "11" : "10";
			}
			catch { WinVer = "?"; }


			return $"""
                     ===== SYSTEM INFO =====
                     CPU ARCH - {RuntimeInformation.ProcessArchitecture}  
                     OS  ARCH - {RuntimeInformation.OSArchitecture}  
                     App ARCH - {AppArch}
                     .NET Ver - {RuntimeInformation.OSArchitecture}
                     Startup  - {State.StartUpTimer.ElapsedMilliseconds} ms
                     Elmah Status - {Ioc.Default.GetRequiredService<LogService>().ElmahLogging}
                     Windows {WinVer} Build - {Environment.OSVersion.Version.Build}
                     Debugger Attached - {Debugger.IsAttached}
                     Touchscreen - {PointerDevice.GetPointerDevices().Any(p => p.PointerDeviceType == PointerDeviceType.Touch)}
                     ProcessID - {Environment.ProcessId}
                     Core Count - {Environment.ProcessorCount}

                     === User Prefs ===
                     Name - {Prefs.FirstName}  {Prefs.LastName}
                     Email - {Prefs.Email}
                     Elmah Consent - {Prefs.ErrorCollectionConsent}
                     Theme - {Ioc.Default.GetRequiredService<Windowing>().PrimaryColor.Color.ToHex()}
                     Accent Color - {Ioc.Default.GetRequiredService<Windowing>().AccentColor} 
                     Last Version Prefs logged - {Prefs.Version}
                     Search Engine - {Prefs.PreferredSearchEngine} 
                     AutoSave - {Prefs.AutoSave}
                     AutoSave Interval - {Prefs.AutoSaveInterval} 
                     Backup - {Prefs.TimedBackup}
                     Backup Interval - {Prefs.TimedBackupInterval}
                     Backup on open - {Prefs.BackupOnOpen} 
                     Project Dir - {Prefs.ProjectDirectory}
                     Backup Dir - {Prefs.BackupDirectory} 
                     RecordPrefsStatus - {Prefs.RecordPreferencesStatus}

                     === CAD Info ===
                     StoryCAD Version - {State.Version}
                     Developer - {State.DeveloperBuild}
                     Env Present - {State.EnvPresent}
                     Doppler Connection - {Doppler.DopplerConnection}
                     Loaded with version change - {State.LoadedWithVersionChange}
                     Invoked through STBX File - {Ioc.Default.GetRequiredService<ShellViewModel>().FilePathToLaunch != ""}
                     """;
		}
		catch (Exception e) { return $"Error getting System Info, {e.Message}"; }
	}
}