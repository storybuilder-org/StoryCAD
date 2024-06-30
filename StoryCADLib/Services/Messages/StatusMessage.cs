namespace StoryCAD.Services.Messages;

public class StatusMessage
{
    public string Status
    {
        get; private set;
    }
    public LogLevel Level
    {
        get; private set;
    }

    public StatusMessage(string status, LogLevel level, bool SendToLog = false)
    {
        Status = status;
        Level = level;
        
        if (SendToLog)
        {
            Ioc.Default.GetService<LogService>().Log(Level,status);
        }
    }
}