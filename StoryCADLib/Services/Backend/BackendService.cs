using MySql.Data.MySqlClient;
using StoryCADLib.DAL;
using StoryCADLib.Models.Tools;
using StoryCADLib.Services.Json;

namespace StoryCADLib.Services.Backend;

/// <summary>
///     BackendService is StoryCAD's interface to our backend server
///     which is hosted on ScaleGrid. The  server runs a MsSQL database.
///     We use three tables to store client account information:
///     users           userid (generated), name, and email address
///     preferences     consent to use elmah.io, consent to receive newsletter
///     versions        version change (current and previous version of StoryCAD).
///     This service contains two methods used to log changes in a client that we need
///     to track:
///     PostPreferences() is called when a user establishes or changes his preferences
///     (specifically his permissions for elmah.io logging and for receiving
///     our newsletter.)
///     PostVersion() is called when the version of StoryCAD is running changes,
///     via update or from the user uninstalling / reinstalling the app.
///     Both PostPreferences and PostVersion assume a user known to the back-end
///     StoryCAD MySQL database and identified by a record in the 'users' table.
///     The 'user' table is keyed by an auto-incremented Int32 field, but contains
///     as email address for the user which is identified as UNIQUE. For our purposes,
///     the user's email address is his identification.
///     Access to the database is through the MySqlIO class in the DAL.
///     The 'AddUser' stored procedure in the database is called to either add a
///     new user or to return the userid (key) if the email address/userid already
///     exists. Similar 'add or update' logic is used for the preferences table.
///     Versions uses a unique primary key of the userid and an auto-incremented
///     version id. Unlike the user and preferences table, there can be multiple
///     versions rows for a user, and their timestamp can be used to measure and track
///     deployments.
/// </summary>
public class BackendService
{
    private readonly AppState _appState;
    private readonly ILogService _logService;
    private readonly PreferenceService _preferenceService;
    private string connection = string.Empty;
    private string sslCA = string.Empty;

    /// <summary>
    /// Indicates whether the backend connection was successfully configured.
    /// When false, database operations will be skipped to avoid cascading errors.
    /// </summary>
    public bool IsConnectionConfigured { get; private set; }

    public BackendService(ILogService logService, AppState appState, PreferenceService preferenceService)
    {
        _logService = logService;
        _appState = appState;
        _preferenceService = preferenceService;
    }


    /// <summary>
    ///     Do any necessary posting to the backend MySql server on app
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
            if (_preferenceService.Model.RecordPreferencesStatus)
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
            // MySQL connection timeout during startup - transient issue
            _logService.Log(LogLevel.Warn, $"Backend operation timed out during startup: {ex.Message}");
            _logService.Log(LogLevel.Info, "Backend telemetry will retry on next startup");
        }
        catch (OperationCanceledException ex)
        {
            // Broader cancellation (e.g., app shutdown during startup)
            _logService.Log(LogLevel.Warn, $"Backend operation cancelled during startup: {ex.Message}");
        }
        catch (MySqlException ex) when (ex.Number == 2003 || ex.Number == 2013)
        {
            // Connection refused or timeout - server temporarily unavailable
            _logService.Log(LogLevel.Warn, $"Backend server temporarily unavailable (Code {ex.Number}): {ex.Message}");
            _logService.Log(LogLevel.Info, "Backend telemetry will retry on next startup");
        }
        catch (MySqlException ex) when (ex.Number == 1045)
        {
            // Authentication failure - configuration issue
            _logService.LogException(LogLevel.Error, ex, $"Backend authentication failed (Code {ex.Number}): Check credentials");
        }
        catch (MySqlException ex)
        {
            // Other MySQL errors
            _logService.LogException(LogLevel.Error, ex, $"Backend MySQL error during startup (Code {ex.Number}): {ex.Message}");
        }
        catch (IOException ex)
        {
            // File write failure for preferences - likely disk or permission issue
            _logService.LogException(LogLevel.Error, ex, "Failed to write preferences file during startup recording");
        }
        catch (UnauthorizedAccessException ex)
        {
            // Permission denied on preferences file
            _logService.LogException(LogLevel.Error, ex, "Permission denied writing preferences file during startup recording");
        }
        catch (Exception ex)
        {
            // Unexpected errors - catch-all safety net
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

        _logService.Log(LogLevel.Info, "Post user preferences to back-end database");

        var sql = Ioc.Default.GetService<MySqlIo>();

        // Get a connection to the database
        MySqlConnection conn = new(connection);

        try
        {
            await conn.OpenAsync();

            var name = preferences.FirstName + " " + preferences.LastName;
            var email = preferences.Email;
            var id = await sql.AddOrUpdateUser(conn, name, email);
            _logService.Log(LogLevel.Info, "Name: " + name + " userId: " + id);

            var elmah = preferences.ErrorCollectionConsent;
            var newsletter = preferences.Newsletter;
            var version = preferences.Version;
            // Workaround for an issue with the PreferencesModel Version property.
            // It has a built-in title. We need to remove the title before we _logService.
            if (version.StartsWith("Version: "))
            {
                version = version.Substring(9);
            }

            // Post the preferences to the database
            await sql.AddOrUpdatePreferences(conn, id, elmah, newsletter, version);
            // Indicate we've stored them successfully
            _preferenceService.Model.RecordPreferencesStatus = true;
            PreferencesIo loader = new();
            await loader.WritePreferences(_preferenceService.Model);
            _logService.Log(LogLevel.Info, "Preferences:  elmah=" + elmah + " newsletter=" + newsletter);
        }
        catch (TaskCanceledException ex) // Catch #986
        {
            _logService.Log(LogLevel.Warn, $"MySQL handshake timed out {ex.Message}");
        }
        catch (Exception ex)
        {
            _logService.LogException(LogLevel.Error, ex, ex.Message);
        }
        finally
        {
            await conn.CloseAsync();
            _logService.Log(LogLevel.Info, "Back-end database connection ended");
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
        var sql = Ioc.Default.GetService<MySqlIo>();

        // Get a connection to the database
        MySqlConnection conn = new(connection);

        try
        {
            await conn.OpenAsync();

            var name = preferences.FirstName + " " + preferences.LastName;
            var email = preferences.Email;
            var id = await sql.AddOrUpdateUser(conn, name, email);
            _logService.Log(LogLevel.Info, "User Name: " + name + " userId: " + id);

            var current = _appState.Version;
            var previous = preferences.Version ?? "";
            // Post the version change to the database
            await sql.AddVersion(conn, id, current, previous);
            // Indicate we've stored it  successfully
            _preferenceService.Model.RecordVersionStatus = true;
            PreferencesIo loader = new();
            await loader.WritePreferences(_preferenceService.Model);
            _logService.Log(LogLevel.Info, "Version:  Current=" + current + " Previous=" + previous);
        }
        catch (TaskCanceledException ex)
        {
            // MySQL connection handshake timeout (issue #986) - transient issue
            _logService.Log(LogLevel.Warn, $"MySQL handshake timed out during version posting: {ex.Message}");
        }
        catch (OperationCanceledException ex)
        {
            // Broader cancellation scenario (e.g., shutdown during operation) - transient
            _logService.Log(LogLevel.Warn, $"Operation cancelled during version posting: {ex.Message}");
        }
        catch (MySqlException ex) when (ex.Number == 2003 || ex.Number == 2013)
        {
            // Connection refused (2003) or timeout (2013) - transient network issues
            _logService.Log(LogLevel.Warn, $"Transient MySQL error during version posting (Code {ex.Number}): {ex.Message}");
        }
        catch (MySqlException ex) when (ex.Number == 1045)
        {
            // Authentication failure - configuration problem
            _logService.LogException(LogLevel.Error, ex, $"MySQL authentication failed (Code {ex.Number}): {ex.Message}");
        }
        catch (MySqlException ex)
        {
            // Other MySQL errors - log as Error for tracking
            _logService.LogException(LogLevel.Error, ex, $"MySQL error during version posting (Code {ex.Number}): {ex.Message}");
        }
        catch (IOException ex)
        {
            // File write failure for preferences
            _logService.LogException(LogLevel.Error, ex, "Failed to write preferences file during version posting");
        }
        catch (UnauthorizedAccessException ex)
        {
            // Permission denied on preferences file
            _logService.LogException(LogLevel.Error, ex, "Permission denied writing preferences file during version posting");
        }
        catch (Exception ex)
        {
            // Unexpected errors - track in elmah.io
            _logService.LogException(LogLevel.Error, ex, $"Unexpected error during version posting: {ex.Message}");
        }
        finally
        {
            await conn.CloseAsync();
            _logService.Log(LogLevel.Info, "Back-end database connection ended");
        }
    }

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
            sslCA = $"SslCa={tempFile.Path};";
            // create MySQL connection string if keys are defined
            connection = keys.CONNECTION + sslCA;
            IsConnectionConfigured = true;
            _logService.Log(LogLevel.Info, "Backend connection configured successfully");
        }
        catch (Exception ex)
        {
            IsConnectionConfigured = false;
            _logService.LogException(LogLevel.Error, ex, $"Failed to configure backend connection: {ex.Message}");
        }
    }

    public async Task DeleteWorkFile()
    {
        try
        {
            _logService.Log(LogLevel.Info, "DeleteWorkFile");
            var path = sslCA.Substring(6); // remove leading 'SslCa='  
            path = path.Substring(0, path.Length - 1); // remove trailing ';'
            var file = await StorageFile.GetFileFromPathAsync(path);
            await file.DeleteAsync();
        }
        catch (Exception ex)
        {
            _logService.LogException(LogLevel.Error, ex, ex.Message);
        }
    }
}
