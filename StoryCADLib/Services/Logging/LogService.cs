using System.Diagnostics;
using System.Runtime.InteropServices;
using Elmah.Io.Client;
using Elmah.Io.NLog;
using NLog;
using NLog.Config;
using NLog.Targets;
using StoryCADLib.Services.Json;

namespace StoryCADLib.Services.Logging;

/// <summary>
///     Manage the Task Log file.
/// </summary>
public class LogService : ILogService
{
    private static readonly Logger Logger;
    private static readonly string logFilePath;
    private static Exception exceptionHelper;
    private static readonly AppState State;
    private static readonly PreferenceService PreferenceService;
    public static NLog.LogLevel MinLogLevel = NLog.LogLevel.Info;
    private string apiKey = string.Empty;
    private string logID = string.Empty;

    static LogService()
    {
        // Initialize static fields with Ioc for now - will be removed when LogService fully converted
        State = Ioc.Default.GetRequiredService<AppState>();
        PreferenceService = Ioc.Default.GetRequiredService<PreferenceService>();

        try
        {
            LoggingConfiguration config = new();

            // Create the file logging target
            FileTarget fileTarget = new();
            logFilePath = Path.Combine(State.RootDirectory, "logs");
            fileTarget.FileName = Path.Combine(logFilePath, "StoryCAD.${date:format=yyyy-MM-dd}.log");
            fileTarget.CreateDirs = true;
            fileTarget.MaxArchiveFiles = 7;
            fileTarget.ArchiveEvery = FileArchivePeriod.Day;
            fileTarget.Layout =
                "${longdate} | ${level} | ${message} | ${exception:format=Message,StackTrace,Data:MaxInnerExceptionLevel=15}";
            LoggingRule fileRule = new("*", NLog.LogLevel.Off, fileTarget);
            if (PreferenceService.Model.AdvancedLogging)
            {
                MinLogLevel = NLog.LogLevel.Trace;
            }
            else
            {
                MinLogLevel = NLog.LogLevel.Info;
            }

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

    public LogService()
    {
        Log(LogLevel.Info, "Starting Log service");
        Log(LogLevel.Info, "Detailed log at " + logFilePath);
    }

    public bool ElmahLogging { get; private set; }

    public bool AddElmahTarget()
    {
        if (apiKey == string.Empty || logID == string.Empty)
        {
            return false;
        }

        try
        {
            // create elmah.io target
            ElmahIoTarget elmahIoTarget = new();

            elmahIoTarget.OnMessage += msg =>
            {
                msg.Version = State.Version;


                try
                {
                    msg.Detail = exceptionHelper?.ToString();
                }
                catch (Exception e)
                {
                    msg.Detail = $"Error trying to obtain StackTrace helper Error: {e.Message}";
                }

                var baseException = exceptionHelper?.GetBaseException();
                msg.Version = State.Version;

                msg.Type = baseException?.GetType().FullName;
                msg.Data = baseException?.ToDataList() ?? new();
                msg.Source = baseException?.Source;
                msg.Hostname = Hostname();
                msg.ServerVariables = new List<Item>
                {
                    new("User-Agent",
                        $"X-ELMAHIO-APPLICATION; OS={Environment.OSVersion.Platform};" +
                        $" OSVERSION={Environment.OSVersion.Version}; ENGINE=UNO")
                };

                try
                {
                    var LogString = string.Empty;

                    try
                    {
                        msg.Data.Add(new Item("SystemInfo", SystemInfo()));
                    }
                    catch (Exception ex)
                    {
                        msg.Data.Add(new Item("Line " + 0, $"failed getting system info ({ex.Message})"));
                    }

                    using (var stream =
                           File.Open(
                               Path.Combine(State.RootDirectory, "logs",
                                   $"StoryCAD.{DateTime.Now.ToString("yyyy-MM-dd")}.log"), FileMode.Open,
                               FileAccess.Read, FileShare.ReadWrite))
                    {
                        using (StreamReader reader = new(stream))
                        {
                            LogString = reader.ReadToEnd();
                        }
                    }

                    var ln = 0;
                    if (LogString.Split("\n").Length > 250)
                    {
                        foreach (var line in LogString.Split("\n").TakeLast(250))
                        {
                            msg.Data.Add(new Item("Line " + ln, line));
                            ln++;
                        }
                    }
                    else
                    {
                        foreach (var line in LogString.Split("\n").TakeLast(250))
                        {
                            msg.Data.Add(new Item("Line ", line));
                            ln++;
                        }
                    }

                    msg.Data.Add(new Item("Log ", "end"));
                }
                catch (Exception e)
                {
                    msg.Data.Add(new Item("Error",
                        $"There was an error attempting to obtain the log, Error: {e.Message}"));
                }
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

    public void SetElmahTokens(Doppler keys)
    {
        apiKey = keys.APIKEY;
        logID = keys.LOGID;
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

    private string Hostname()
    {
        var machineName = Environment.MachineName;
        if (!string.IsNullOrWhiteSpace(machineName))
        {
            return machineName;
        }

        return Environment.GetEnvironmentVariable("COMPUTERNAME");
    }

    /// <summary>
    /// Gets the macOS CPU name (e.g., "Apple  M4")
    /// </summary>
    private string GetMacOSCpuName()
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "sysctl",
                    Arguments = "-n machdep.cpu.brand_string",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            var output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();
            return string.IsNullOrWhiteSpace(output) ? "Unknown" : output;
        }
        catch
        {
            return "Unknown";
        }
    }

    /// <summary>
    /// Gets the macOS version (e.g., "15.0.1")
    /// </summary>
    private string GetMacOSVersion()
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "sw_vers",
                    Arguments = "-productVersion",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            var output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();
            return string.IsNullOrWhiteSpace(output) ? "Unknown" : output;
        }
        catch
        {
            return "Unknown";
        }
    }

#if WINDOWS
    /// <summary>
    /// Gets the Windows CPU name from registry
    /// </summary>
    private string GetWindowsCpuName()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
            var cpuName = key?.GetValue("ProcessorNameString")?.ToString()?.Trim();
            return string.IsNullOrWhiteSpace(cpuName) ? "Unknown" : cpuName;
        }
        catch
        {
            return "Unknown";
        }
    }

    /// <summary>
    /// Gets the Windows OS version (e.g., "Windows 11 23H2")
    /// </summary>
    private string GetWindowsVersion()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            var productName = key?.GetValue("ProductName")?.ToString() ?? "Windows";
            var displayVersion = key?.GetValue("DisplayVersion")?.ToString() ?? "";
            var currentBuild = key?.GetValue("CurrentBuild")?.ToString() ?? "";

            var version = productName;
            if (!string.IsNullOrWhiteSpace(displayVersion))
                version += $" {displayVersion}";
            if (!string.IsNullOrWhiteSpace(currentBuild))
                version += $" (Build {currentBuild})";

            return version;
        }
        catch
        {
            return "Unknown";
        }
    }
#endif


    /// <summary>
    ///     Compiles a small report about the users device, StoryCAD information etc.
    /// </summary>
    /// <returns>StoryCAD Device Report</returns>
    public string SystemInfo()
    {
        try
        {
            var Prefs = PreferenceService.Model;
            var AppArch = IntPtr.Size switch
            {
                4 => "32 bit",
                8 => "64 bit",
                _ => "Unknown"
            };

            // Detect platform at runtime and get platform-specific info
            string platformInfo;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                var cpuName = GetMacOSCpuName();
                var osVersion = GetMacOSVersion();
                platformInfo = $"""
                    CPU Name - {cpuName}
                    macOS Version - {osVersion}
                    """;
            }
#if WINDOWS
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var cpuName = GetWindowsCpuName();
                var osVersion = GetWindowsVersion();
                platformInfo = $"""
                    CPU Name - {cpuName}
                    Windows Version - {osVersion}
                    """;
            }
#endif
            else
            {
                platformInfo = "";
            }

            return $"""
                    ===== SYSTEM INFO =====
                    CPU ARCH - {RuntimeInformation.ProcessArchitecture}
                    OS  ARCH - {RuntimeInformation.OSArchitecture}
                    App ARCH - {AppArch}
                    .NET Ver - {RuntimeInformation.OSArchitecture}
                    Startup  - {State.StartUpTimer.ElapsedMilliseconds} ms
                    Elmah Status - {ElmahLogging}
                    OS Platform - {Environment.OSVersion.Platform}
                    OS Build - {Environment.OSVersion.Version.Build}
                    Debugger Attached - {Debugger.IsAttached}
                    Core Count - {Environment.ProcessorCount}
                    {platformInfo}

                    === User Prefs ===
                    Name - {Prefs.FirstName}
                    Email - {Prefs.Email}
                    Search Engine - {Prefs.PreferredSearchEngine} 
                    AutoSave - {Prefs.AutoSave} ({Prefs.AutoSaveInterval} sec)
                    Backup - {Prefs.TimedBackup} ({Prefs.TimedBackupInterval} sec)
                    Backup on open - {Prefs.BackupOnOpen} 
                    Project Dir - {Prefs.ProjectDirectory}
                    Backup Dir - {Prefs.BackupDirectory} 

                    === CAD Info ===
                    StoryCAD Version - {State.Version}
                    DevBuild? - {State.DeveloperBuild}
                    Env Present - {State.EnvPresent}
                    Doppler Connection - {Doppler.DopplerConnection}
                    Loaded with version change - {State.LoadedWithVersionChange}
                    """;
        }
        catch (Exception e)
        {
            return $"Error getting System Info, {e.Message}";
        }
    }
}
