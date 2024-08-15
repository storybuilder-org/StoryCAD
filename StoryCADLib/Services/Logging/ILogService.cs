namespace StoryCAD.Services.Logging;

public interface ILogService
{
    void Log(LogLevel level, string message);

    void LogException(LogLevel level, Exception exception, string message);
}