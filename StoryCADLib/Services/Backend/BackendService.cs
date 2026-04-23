using MySql.Data.MySqlClient;
using StoryCADLib.DAL;
using StoryCADLib.Models.Tools;
using StoryCADLib.Services.Json;

namespace StoryCADLib.Services.Backend;

/// <summary>
///     BackendService is StoryCAD's interface to our backend server
///     hosted on ScaleGrid. It handles business logic for posting
///     user preferences, version tracking, and account deletion.
///
///     Database operations are delegated to IMySqlIo, which owns
///     connection management. BackendService never creates or opens
///     database connections directly — it calls IMySqlIo methods
///     with business parameters only.
///
///     Tables:
///       users        — user_id (auto), name, email (UNIQUE)
///       preferences  — elmah consent, newsletter consent, version
///       versions     — current/previous version per deployment
///
///     BackendService responsibilities:
///       - Doppler key retrieval and SSL certificate handling
///       - Business logic: when to post, what data to send
///       - Storing the returned UserId on PreferencesModel
///       - Exception handling and logging
///
///     IMySqlIo responsibilities:
///       - Connection lifecycle (open/close per operation)
///       - Stored procedure calls (spAddUser, spAddOrUpdatePreferences,
///         spAddOrUpdateVersion, spDeleteUser)
/// </summary>
public class BackendService
{
    private readonly AppState _appState;
    private readonly ILogService _logService;
    private readonly PreferenceService _preferenceService;
    private readonly IMySqlIo _sqlIo;
    private string _sslCaPath = string.Empty;

    public BackendService(ILogService logService, AppState appState,
        PreferenceService preferenceService, IMySqlIo sqlIo)
    {
        _logService = logService;
        _appState = appState;
        _preferenceService = preferenceService;
        _sqlIo = sqlIo;
    }

    /// <summary>
    ///     Whether the backend connection has been configured.
    ///     Delegates to IMySqlIo.IsConnectionConfigured.
    /// </summary>
    public bool IsConnectionConfigured => _sqlIo.IsConnectionConfigured;

    /// <summary>
    ///     Do any necessary posting to the backend MySQL server on app
    ///     startup. This will include either preferences or versions
    ///     posting that weren't successful during the last run.
    ///     Also, if the app version has changed because of an update,
    ///     post the new version.
    /// </summary>
    public async Task StartupRecording()
    {
        // Exception handling: This method runs during app startup and posts telemetry/version data
        // to the backend MySQL database. Since this is an optional feature, the app must continue
        // functioning even if backend communication fails. PostPreferences and PostVersion have
        // their own exception handling; this catch block handles exceptions from PreferencesIo
        // and serves as a safety net for any unexpected errors.
        try
        {
            // If the previous attempt to communicate to the back-end server
            // or database failed, retry
            if (!_preferenceService.Model.RecordPreferencesStatus)
            {
                await PostPreferences(_preferenceService.Model);
            }

            // If the StoryCAD version has changed, post the version change
            // Skip this check if preferences haven't been initialized yet (fresh install)
            if (_preferenceService.Model.PreferencesInitialized &&
                !_appState.Version.Equals(_preferenceService.Model.Version))
            {
                // Process a version change (usually a new release)
                _logService.Log(LogLevel.Info,
                    "Version mismatch: " + _appState.Version + " != " + _preferenceService.Model.Version);
                _appState.LoadedWithVersionChange = true;
                var preferences = _preferenceService.Model;

                // Update Preferences
                preferences.Version = _appState.Version;
                PreferencesIo prefIO = new();
                await prefIO.WritePreferences(preferences);

                // Post deployment to backend server
                await PostVersion();
            }
            else if (_preferenceService.Model.PreferencesInitialized &&
                     !_preferenceService.Model.RecordVersionStatus)
            {
                await PostVersion();
            }
        }
        catch (TaskCanceledException ex)
        {
            _logService.Log(LogLevel.Warn, $"Backend operation timed out during startup: {ex.Message}");
            _logService.Log(LogLevel.Info, "Backend telemetry will retry on next startup");
        }
        catch (OperationCanceledException ex)
        {
            _logService.Log(LogLevel.Warn, $"Backend operation cancelled during startup: {ex.Message}");
        }
        catch (MySqlException ex) when (ex.Number == 2003 || ex.Number == 2013)
        {
            _logService.Log(LogLevel.Warn, $"Backend server temporarily unavailable (Code {ex.Number}): {ex.Message}");
            _logService.Log(LogLevel.Info, "Backend telemetry will retry on next startup");
        }
        catch (MySqlException ex) when (ex.Number == 1045)
        {
            _logService.LogException(LogLevel.Error, ex, $"Backend authentication failed (Code {ex.Number}): Check credentials");
        }
        catch (MySqlException ex)
        {
            _logService.LogException(LogLevel.Error, ex, $"Backend MySQL error during startup (Code {ex.Number}): {ex.Message}");
        }
        catch (IOException ex)
        {
            _logService.LogException(LogLevel.Error, ex, "Failed to write preferences file during startup recording");
        }
        catch (UnauthorizedAccessException ex)
        {
            _logService.LogException(LogLevel.Error, ex, "Permission denied writing preferences file during startup recording");
        }
        catch (Exception ex)
        {
            _logService.LogException(LogLevel.Error, ex, "Unexpected error in StartupRecording method");
        }
    }

    public async Task PostPreferences(PreferencesModel preferences)
    {
        if (!IsConnectionConfigured)
        {
            _logService.Log(LogLevel.Warn, "Skipping PostPreferences - backend connection not configured");
            return;
        }

        _logService.Log(LogLevel.Info, "Posting user preferences to back-end database");

        try
        {
            var name = preferences.FirstName + " " + preferences.LastName;
            var email = preferences.Email;
            var id = await _sqlIo.AddOrUpdateUser(name, email);
            preferences.UserId = id;
            _logService.Log(LogLevel.Info, "User registered, userId: " + id);

            var elmah = preferences.ErrorCollectionConsent;
            var newsletter = preferences.Newsletter;
            var version = preferences.Version;
            // Workaround for an issue with the PreferencesModel Version property.
            // It has a built-in title. We need to remove the title before logging.
            if (version.StartsWith("Version: "))
            {
                version = version.Substring(9);
            }

            await _sqlIo.AddOrUpdatePreferences(id, elmah, newsletter, version);
            _preferenceService.Model.RecordPreferencesStatus = true;
            PreferencesIo loader = new();
            await loader.WritePreferences(_preferenceService.Model);
            _logService.Log(LogLevel.Info, "Preferences posted: elmah=" + elmah + " newsletter=" + newsletter);
        }
        catch (TaskCanceledException ex)
        {
            _logService.Log(LogLevel.Warn, $"MySQL handshake timed out during preferences posting: {ex.Message}");
        }
        catch (OperationCanceledException ex)
        {
            _logService.Log(LogLevel.Warn, $"Operation cancelled during preferences posting: {ex.Message}");
        }
        catch (MySqlException ex) when (ex.Number == 2003 || ex.Number == 2013)
        {
            _logService.Log(LogLevel.Warn, $"Transient MySQL error during preferences posting (Code {ex.Number}): {ex.Message}");
        }
        catch (MySqlException ex) when (ex.Number == 1045)
        {
            _logService.LogException(LogLevel.Error, ex, $"MySQL authentication failed (Code {ex.Number}): {ex.Message}");
        }
        catch (MySqlException ex)
        {
            _logService.LogException(LogLevel.Error, ex, $"MySQL error during preferences posting (Code {ex.Number}): {ex.Message}");
        }
        catch (IOException ex)
        {
            _logService.LogException(LogLevel.Error, ex, "Failed to write preferences file during preferences posting");
        }
        catch (UnauthorizedAccessException ex)
        {
            _logService.LogException(LogLevel.Error, ex, "Permission denied writing preferences file during preferences posting");
        }
        catch (Exception ex)
        {
            _logService.LogException(LogLevel.Error, ex, $"Unexpected error during preferences posting: {ex.Message}");
        }
    }

    public async Task PostVersion()
    {
        if (!IsConnectionConfigured)
        {
            _logService.Log(LogLevel.Warn, "Skipping PostVersion - backend connection not configured");
            return;
        }

        _logService.Log(LogLevel.Info, "Posting version data to back-end database");
        var preferences = _preferenceService.Model;

        try
        {
            var name = preferences.FirstName + " " + preferences.LastName;
            var email = preferences.Email;
            var id = await _sqlIo.AddOrUpdateUser(name, email);
            preferences.UserId = id;
            _logService.Log(LogLevel.Info, "User registered, userId: " + id);

            var current = _appState.Version;
            var previous = preferences.Version ?? "";
            await _sqlIo.AddVersion(id, current, previous);
            _preferenceService.Model.RecordVersionStatus = true;
            PreferencesIo loader = new();
            await loader.WritePreferences(_preferenceService.Model);
            _logService.Log(LogLevel.Info, "Version posted: Current=" + current + " Previous=" + previous);
        }
        catch (TaskCanceledException ex)
        {
            _logService.Log(LogLevel.Warn, $"MySQL handshake timed out during version posting: {ex.Message}");
        }
        catch (OperationCanceledException ex)
        {
            _logService.Log(LogLevel.Warn, $"Operation cancelled during version posting: {ex.Message}");
        }
        catch (MySqlException ex) when (ex.Number == 2003 || ex.Number == 2013)
        {
            _logService.Log(LogLevel.Warn, $"Transient MySQL error during version posting (Code {ex.Number}): {ex.Message}");
        }
        catch (MySqlException ex) when (ex.Number == 1045)
        {
            _logService.LogException(LogLevel.Error, ex, $"MySQL authentication failed (Code {ex.Number}): {ex.Message}");
        }
        catch (MySqlException ex)
        {
            _logService.LogException(LogLevel.Error, ex, $"MySQL error during version posting (Code {ex.Number}): {ex.Message}");
        }
        catch (IOException ex)
        {
            _logService.LogException(LogLevel.Error, ex, "Failed to write preferences file during version posting");
        }
        catch (UnauthorizedAccessException ex)
        {
            _logService.LogException(LogLevel.Error, ex, "Permission denied writing preferences file during version posting");
        }
        catch (Exception ex)
        {
            _logService.LogException(LogLevel.Error, ex, $"Unexpected error during version posting: {ex.Message}");
        }
    }

    /// <summary>
    ///     Deletes all backend data for the current user.
    ///     Uses the stored UserId from PreferencesModel.
    ///     Returns true only if the stored procedure confirmed deletion.
    ///     Local data cleanup is the caller's responsibility and should
    ///     only happen if this method returns true (or if the backend
    ///     was never configured, meaning no remote data exists).
    /// </summary>
    public async Task<bool> DeleteUserData()
    {
        if (!IsConnectionConfigured)
        {
            _logService.Log(LogLevel.Warn, "Skipping DeleteUserData - backend connection not configured");
            return false;
        }

        var userId = _preferenceService.Model.UserId;
        if (userId <= 0)
        {
            _logService.Log(LogLevel.Warn, "Skipping DeleteUserData - no UserId stored");
            return false;
        }

        _logService.Log(LogLevel.Info, "Deleting user data from backend database for userId: " + userId);

        try
        {
            var deleted = await _sqlIo.DeleteUser(userId);
            if (deleted)
            {
                _logService.Log(LogLevel.Info, "User data deleted successfully");
                return true;
            }

            _logService.Log(LogLevel.Warn, "DeleteUser returned false - user may not exist in database");
            return false;
        }
        catch (Exception ex)
        {
            _logService.LogException(LogLevel.Error, ex, "Failed to delete user data");
            return false;
        }
    }

    /// <summary>
    ///     Configures the backend connection from Doppler secrets.
    ///     Writes the SSL CA certificate to a temp file and builds
    ///     the connection string, then passes it to IMySqlIo.
    /// </summary>
    public async Task SetConnectionString(Doppler keys)
    {
        try
        {
            _logService.Log(LogLevel.Info, "SetConnectionString");
            var tempFolder = await StorageFolder.GetFolderFromPathAsync(Path.GetTempPath());
            var tempFile =
                await tempFolder.CreateFileAsync("StoryCAD.pem", CreationCollisionOption.ReplaceExisting);
            var caFile = keys.CAFILE;
            await FileIO.WriteTextAsync(tempFile, caFile);
            _sslCaPath = tempFile.Path;
            var sslCa = $"SslCa={_sslCaPath};";

            // Build connection string and pass to DAL
            var connectionString = keys.CONNECTION + sslCa;
            _sqlIo.SetConnectionString(connectionString);

            _logService.Log(LogLevel.Info, "Backend connection configured successfully");
        }
        catch (Exception ex)
        {
            _logService.LogException(LogLevel.Error, ex, $"Failed to configure backend connection: {ex.Message}");
        }
    }

    /// <summary>
    ///     Cleans up the temporary SSL certificate file created by SetConnectionString.
    /// </summary>
    public async Task DeleteWorkFile()
    {
        try
        {
            _logService.Log(LogLevel.Info, "DeleteWorkFile");
            var file = await StorageFile.GetFileFromPathAsync(_sslCaPath);
            await file.DeleteAsync();
        }
        catch (Exception ex)
        {
            _logService.LogException(LogLevel.Error, ex, ex.Message);
        }
    }
}
