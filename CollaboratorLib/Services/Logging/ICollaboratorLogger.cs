using StoryCADLib.Services.Logging;
using LogLevelAlias = StoryCADLib.Services.Logging.LogLevel;

namespace StoryCollaborator.Services.Logging;

/// <summary>
/// Collaborator-specific logger that supports privacy controls.
/// </summary>
public interface ICollaboratorLogger : ILogService
{
    /// <summary>
    /// Logs a message that may contain sensitive user content. The implementation
    /// decides whether to redact this data based on user consent.
    /// </summary>
    void LogSensitive(LogLevelAlias level, string message);

    /// <summary>
    /// Enables or disables sensitive logging at runtime.
    /// </summary>
    void EnableSensitiveLogging(bool allow);
}
