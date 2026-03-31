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
    Task AddOrUpdatePreferences(int id, bool elmah, bool newsletter, string version);

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
}
