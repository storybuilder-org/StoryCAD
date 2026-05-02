namespace StoryCADLib.DAL;

/// <summary>
///     Interface for the MySQL data access layer.
///     Encapsulates all database operations including connection management.
///     Production uses MySqlIo (real database via ScaleGrid).
///     Tests use TestMySqlIo (records calls, no database).
/// </summary>
public interface IMySqlIo
{
    /// <summary>
    ///     Configures the database connection string.
    ///     Must be called before any database operations.
    /// </summary>
    void SetConnectionString(string connectionString);

    /// <summary>
    ///     Whether SetConnectionString has been called successfully.
    ///     When false, database operations should be skipped.
    /// </summary>
    bool IsConnectionConfigured { get; }

    /// <summary>
    ///     Adds a new user or returns the existing user's ID (via spAddUser).
    ///     The email column is UNIQUE — duplicate emails return the existing row's ID.
    /// </summary>
    Task<int> AddOrUpdateUser(string name, string email);

    /// <summary>
    ///     Adds or updates user preferences (via spAddOrUpdatePreferences).
    /// </summary>
    Task AddOrUpdatePreferences(int id, bool elmah, bool newsletter, string version, bool usageStats);

    /// <summary>
    ///     Adds or updates version tracking (via spAddOrUpdateVersion).
    /// </summary>
    Task AddVersion(int id, string currentVersion, string previousVersion);

    /// <summary>
    ///     Deletes a user and all related data — preferences, versions,
    ///     and the user row itself (via spDeleteUser).
    ///     Returns true if deletion succeeded, false if the user was not
    ///     found or the operation failed (rolled back).
    ///     Used for Apple Guideline 5.1.1(v) account data deletion.
    /// </summary>
    Task<bool> DeleteUser(int id);

    /// <summary>
    ///     Executes a stored procedure that returns rows.
    ///     Each row is a dictionary mapping column name to value.
    ///     Used for read operations (e.g., fetching messages in #1377).
    /// </summary>
    Task<List<Dictionary<string, object>>> ExecuteReaderAsync(
        string storedProcedure,
        params (string name, object value)[] parameters);

    /// <summary>
    ///     Flushes a session's usage data in a single round-trip
    ///     via spRecordSessionData. Outlines and features are passed
    ///     as JSON arrays.
    /// </summary>
    Task RecordSessionData(string usageId, DateTime sessionStart, DateTime sessionEnd,
        int clockTimeSeconds, string outlinesJson, string featuresJson);

    /// <summary>
    ///     Fetches unread, currently-visible messages for a user (via spGetUnreadMessages).
    ///     Filters out scheduled-future and expired messages server-side.
    /// </summary>
    Task<List<UserMessage>> GetUnreadMessages(int userId);

    /// <summary>
    ///     Marks a message as read/dismissed for a user (via spMarkMessageRead).
    /// </summary>
    Task MarkMessageRead(int userId, int messageId);
}
