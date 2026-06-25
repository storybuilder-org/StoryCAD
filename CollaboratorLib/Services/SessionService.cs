using Microsoft.Extensions.Logging;

namespace StoryCollaborator.Services;

/// <summary>
/// Manages Collaborator session state and message history.
/// </summary>
public class SessionService
{
    private readonly List<string> _sessionMessages = new();
    private readonly ILogger<SessionService> _logger;
    private bool _isActive;

    public SessionService(ILogger<SessionService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Whether a session is currently active.
    /// </summary>
    public bool IsActive => _isActive;

    /// <summary>
    /// Gets a copy of all session messages.
    /// </summary>
    public IReadOnlyList<string> Messages => _sessionMessages.AsReadOnly();

    /// <summary>
    /// Starts a new session, clearing any previous messages.
    /// </summary>
    public void StartSession()
    {
        _sessionMessages.Clear();
        _isActive = true;
        _logger?.LogInformation("Collaborator session started");
    }

    /// <summary>
    /// Ends the current session.
    /// </summary>
    public void EndSession()
    {
        _isActive = false;
        _logger?.LogInformation("Collaborator session ended");
    }

    /// <summary>
    /// Records a message to the session history.
    /// </summary>
    public void RecordMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        _sessionMessages.Add(message);
    }

    /// <summary>
    /// Clears all session messages.
    /// </summary>
    public void ClearMessages()
    {
        _sessionMessages.Clear();
    }

    /// <summary>
    /// Gets all messages as an array (for CollaboratorResult).
    /// </summary>
    public string[] GetMessagesArray() => _sessionMessages.ToArray();
}
