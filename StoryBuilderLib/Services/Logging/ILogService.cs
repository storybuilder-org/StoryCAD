namespace StoryBuilder.Services.Logging
{
    public interface ILogService
    {
        void Log(LogLevel level, string message);
    }
}
