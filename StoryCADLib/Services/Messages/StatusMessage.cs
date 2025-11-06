namespace StoryCADLib.Services.Messages;

public class StatusMessage
{
    public StatusMessage(string status, LogLevel level, bool SendToLog = false)
    {
        Status = status;
        Level = level;

        if (SendToLog)
        {
            Ioc.Default.GetService<ILogService>()?.Log(Level, status);
        }
    }

    public string Status { get; private set; }

    public LogLevel Level { get; }
}
