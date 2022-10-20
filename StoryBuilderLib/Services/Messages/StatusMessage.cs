using CommunityToolkit.Mvvm.DependencyInjection;
using StoryBuilder.Services.Logging;

namespace StoryBuilder.Services.Messages;

public class StatusMessage
{
    public string Status { get; }
    public LogLevel Level { get; }

    public StatusMessage(string status, LogLevel level, bool SendToLog = false)
    {
        Status = status;
        Level = level;
        
        if (SendToLog) { Ioc.Default.GetRequiredService<LogService>().Log(Level,status); }
    }
}