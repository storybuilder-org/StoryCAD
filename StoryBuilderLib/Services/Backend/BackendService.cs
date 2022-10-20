using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Windows.Storage;
using CommunityToolkit.Mvvm.DependencyInjection;
using MySql.Data.MySqlClient;
using StoryBuilder.DAL;
using StoryBuilder.Models;
using StoryBuilder.Models.Tools;
using StoryBuilder.Services.Json;
using StoryBuilder.Services.Logging;

namespace StoryBuilder.Services.Backend;

/// <summary>
/// BackendService is StoryBuilder's interface to our backend server 
/// which is hosted on ScaleGrid. The  server runs a MsSQL database.
///
/// We use three tables to store client account information:
///     users           userid (generated), name, and email address
///     preferences     consent to use elmah.io, consent to receive newsletter
///     versions        version change (current and previous version of StoryBuilder).
/// 
/// This service contains two methods used to log changes in a client that we need
/// to track:
///     PostPreferences() is called when a user establishes or changes his preferences
///         (specifically his permissions for elmah.io logging and for receiving
///         our newsletter.)
///     PostVersion() is called when the version of StoryBuilder is running changes,
///         via update or from the user uninstalling / reinstalling the app.
/// 
/// Both PostPreferences and PostVersion assume a user known to the back-end
/// StoryBuilder MySQL database and identified by a record in the 'users' table.
/// The 'user' table is keyed by an auto-incremented Int32 field, but contains
/// as email address for the user which is identified as UNIQUE. For our purposes,
/// the user's email address is his identification.
///
/// Access to the database is through the MySqlIO class in the DAL.
/// The 'AddUser' stored procedure in the database is called to either add a
/// new user or to return the userid (key) if the email address/userid already
/// exists. Similar 'add or update' logic is used for the preferences table.
/// Versions uses a unique primary key of the userid and an auto-incremented
/// version id. Unlike the user and preferences table, there can be multiple
/// versions rows for a user, and their timestamp can be used to measure and track
/// deployments.
/// </summary>
public class BackendService
{
    private LogService _log = Ioc.Default.GetService<LogService>();
    private string _connection = string.Empty;
    private string _sslCa = string.Empty;


    /// <summary>
    /// Do any necessary posting to the backend MySql server on app
    /// startup. This will include either preferences or versions
    /// posting that weren't successful during the last run.
    ///
    /// Also, if the app version has changed because of an update,
    /// post the new version.
    /// </summary>
    public void StartupRecording()
    {
        BackgroundWorker _Worker = new();
        _Worker.DoWork += async (_, _) =>
        {
            try
            {
                // If the previous attempt to communicate to the back-end server
                // or database failed, retry
                if (!GlobalData.Preferences.RecordPreferencesStatus)
                    await PostPreferences(GlobalData.Preferences);
                if (!GlobalData.Preferences.RecordVersionStatus)
                    await PostVersion();
                // If the StoryBuilder version has changed, post the version change
                if (!GlobalData.Version.Equals(GlobalData.Preferences.Version))
                {
                    // Process a version change (usually a new release)
                    _log.Log(LogLevel.Info, "Version mismatch: " + GlobalData.Version + " != " + GlobalData.Preferences.Version);
                    GlobalData.LoadedWithVersionChange = true;
                    PreferencesModel _Preferences = GlobalData.Preferences;
                    // Update Preferences
                    _Preferences.Version = GlobalData.Version;
                    PreferencesIo _PrefIo = new(_Preferences, GlobalData.RootDirectory);
                    await _PrefIo.UpdateFile();
                    // Post deployment to backend server
                    await PostVersion();
                }
            }
            catch (Exception _Ex) { _log.LogException(LogLevel.Warn, _Ex, "Error in parse service worker"); }
        };
        _Worker.RunWorkerAsync();
    }

    public async Task PostPreferences(PreferencesModel preferences)
    {
        _log.Log(LogLevel.Info, "Post user preferences to back-end database");

        MySqlIo _Sql = Ioc.Default.GetService<MySqlIo>();

        // Get a connection to the database
        MySqlConnection _Conn = new(_connection);

        try
        {
            await _Conn.OpenAsync();

            string _Name = preferences.Name;
            string _Email = preferences.Email;
            int _Id = await _Sql!.AddOrUpdateUser(_Conn, _Name, _Email);
            _log.Log(LogLevel.Info, "Name: " + _Name + " userId: " + _Id);

            bool _Elmah = preferences.ErrorCollectionConsent;
            bool _Newsletter = preferences.Newsletter;
            string _Version = preferences.Version;
            // Workaround for an issue with the PreferencesModel Version property.
            // It has a built-in title. We need to remove the title before we log.
            if (_Version.StartsWith("Version: "))
                _Version = _Version[9..];
            // Post the preferences to the database
            await _Sql.AddOrUpdatePreferences(_Conn, _Id, _Elmah, _Newsletter, _Version);
            // Indicate we've stored them successfully
            GlobalData.Preferences.RecordPreferencesStatus = true;
            PreferencesIo _Loader = new(GlobalData.Preferences, GlobalData.RootDirectory);
            await _Loader.UpdateFile();
            _log.Log(LogLevel.Info, "Preferences:  elmah=" + _Elmah + " newsletter=" + _Newsletter);
        }
        // may want to use multiple catch clauses
        catch (Exception _Ex)
        {
            _log.LogException(LogLevel.Warn, _Ex, _Ex.Message);
        }
        finally
        {
            await _Conn.CloseAsync();
            _log.Log(LogLevel.Info, "Back-end database connection ended");
        }
    }

    public async Task PostVersion()
    {
        _log.Log(LogLevel.Info, "Posting version data to parse");

        PreferencesModel _Preferences = GlobalData.Preferences;
        MySqlIo _Sql = Ioc.Default.GetService<MySqlIo>();

        // Get a connection to the database
        MySqlConnection _Conn = new(_connection);

        try
        {
            await _Conn.OpenAsync();

            string _Name = _Preferences.Name;
            string _Email = _Preferences.Email;
            int _Id = await _Sql!.AddOrUpdateUser(_Conn, _Name, _Email);
            _log.Log(LogLevel.Info, "User Name: " + _Name + " userId: " + _Id);

            string _Current = GlobalData.Version;
            string _Previous = _Preferences.Version ?? "";
            // Post the version change to the database
            await _Sql.AddVersion(_Conn, _Id, _Current, _Previous);
            // Indicate we've stored it  successfully
            GlobalData.Preferences.RecordVersionStatus = true;
            PreferencesIo _Loader = new(GlobalData.Preferences, GlobalData.RootDirectory);
            await _Loader.UpdateFile();
            _log.Log(LogLevel.Info, "Version:  Current=" + _Current + " Previous=" + _Previous);
        }
        // May want to use multiple catch clauses
        catch (Exception _Ex)
        {
            _log.LogException(LogLevel.Warn, _Ex, _Ex.Message);
        }
        finally
        {
            await _Conn.CloseAsync();
            _log.Log(LogLevel.Info, "Back-end database connection ended");
        }
    }

    public async Task SetConnectionString(Doppler keys)
    {
        try
        {
            _log.Log(LogLevel.Info, "GetConnectionString");
            StorageFolder _TempFolder = ApplicationData.Current.TemporaryFolder;
            StorageFile _TempFile = 
                await _TempFolder.CreateFileAsync("storybuilder.pem", CreationCollisionOption.ReplaceExisting);
            string _CaFile = keys.CAFILE;
            await FileIO.WriteTextAsync(_TempFile, _CaFile);
            _sslCa = $"SslCa={_TempFile.Path};";
            // create MySQL connection string if keys are defined
            _connection = keys.CONNECTION + _sslCa; 
            // can compare the c:\certs and temp file to see if they are the same
        }
        catch (Exception _Ex)
        {
            _log.LogException(LogLevel.Warn, _Ex, _Ex.Message);
        }
    }

    public async Task DeleteWorkFile()
    {
        try
        {
            _log.Log(LogLevel.Info, "DeleteWorkFile");
            string _Path = _sslCa[6..];   // remove leading 'SslCa='  
            _Path = _Path[..^1];  // remove trailing ';'
            StorageFile _File = await StorageFile.GetFileFromPathAsync(_Path);
            await _File.DeleteAsync();
        }
        catch (Exception _Ex) { _log.LogException(LogLevel.Warn, _Ex, _Ex.Message); }
    }
}