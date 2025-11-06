using StoryCADLib.Services.Json;

namespace StoryCADLib.Services.Logging;

public interface ILogService
{
    bool ElmahLogging { get; }

    void Log(LogLevel level, string message);

    void LogException(LogLevel level, Exception exception, string message);

    void SetElmahTokens(Doppler keys);

    bool AddElmahTarget();

    void Flush();
}
