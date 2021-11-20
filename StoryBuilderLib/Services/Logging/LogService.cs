using NLog;
using NLog.Config;
using NLog.Targets;
using System;
using System.Diagnostics;
using System.IO;
using Windows.Storage;
namespace StoryBuilder.Services.Logging
{
    /// <summary>
    /// Manage the Task Log file.
    /// </summary>
    public class LogService : ILogService
    {
        private static readonly Logger Logger;
        private static readonly string logFilePath;

        static LogService()
        {
            try
            {
                var config = new LoggingConfiguration();

                // Create file target
                var fileTarget = new FileTarget();
                //logFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "StoryBuilder", "logs");
                logFilePath = Path.Combine(ApplicationData.Current.RoamingFolder.Path.ToString(), "StoryBuilder", "logs");
                var logfilename = Path.Combine(logFilePath, "updater.${date:format=yyyy-MM-dd}.log");
                fileTarget.FileName = logfilename;
                fileTarget.CreateDirs = true;
                fileTarget.MaxArchiveFiles = 7;
                fileTarget.ArchiveEvery = FileArchivePeriod.Day;
                fileTarget.ConcurrentWrites = true;
                fileTarget.Layout =
                        "${longdate} | ${level} | ${message} | ${exception:format=Message,StackTrace,Data:MaxInnerExceptionLevel=5}";
                var fileRule = new LoggingRule("*", NLog.LogLevel.Trace, fileTarget);
                config.AddTarget("logfile", fileTarget);
                config.LoggingRules.Add(fileRule);

                // create console target
                if (!Debugger.IsAttached)
                {
                    var consoleTarget = new ColoredConsoleTarget();
                    consoleTarget.Layout = @"${date:format=HH\\:MM\\:ss} ${logger} ${message}";
                    config.AddTarget("console", consoleTarget);
                    var consoleRule = new LoggingRule("*", NLog.LogLevel.Info, consoleTarget);
                    config.LoggingRules.Add(consoleRule);
                }
                else
                {
                    var consoleTarget = new DebuggerTarget();
                    consoleTarget.Layout = @"${date:format=HH\\:MM\\:ss} ${logger} ${message}";
                    config.AddTarget("console", consoleTarget);
                    var consoleRule = new LoggingRule("*", NLog.LogLevel.Info, consoleTarget);
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
                    Logger.Error(exception, message);
                    break;
                case LogLevel.Fatal:
                    Logger.Fatal(exception, message);
                    break;
            }
        }

        public void Flush()
        {
            LogManager.Flush();
        }
    }
}
