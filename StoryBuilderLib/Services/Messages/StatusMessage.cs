namespace StoryBuilder.Services.Messages;

public class StatusMessage
{
    public string Status
    {
        get; private set;
    }
    public int TimeoutMilliseconds
    {
        get; private set;
    }

    public StatusMessage(string status, int timeoutMilliseconds)
    {
        Status = status;
        TimeoutMilliseconds = timeoutMilliseconds;
    }
}