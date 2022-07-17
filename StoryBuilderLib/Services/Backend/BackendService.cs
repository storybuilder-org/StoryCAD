using System;
using System.ComponentModel;
using System.Threading.Tasks;
using StoryBuilder.Models;
using StoryBuilder.Models.Tools;
using StoryBuilder.Services.Logging;
using CommunityToolkit.Mvvm.DependencyInjection;
using StoryBuilder.DAL;
using MySql.Data.MySqlClient;

namespace StoryBuilder.Services.Backend
{
    /// <summary>
    /// BackendService is StoryBuilder's interface to our backend server 
    /// which is hosted by AARCH64.com  in partnership with FOSSHost.com. The
    /// server runs a MsSQL database. We use three tables to store client
    /// account information:
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
        private LogService log = Ioc.Default.GetService<LogService>();

        public void Begin()
        {
            BackgroundWorker Worker = new();
            Worker.DoWork += async (sender, e) =>
            {
                try
                {
                    // If the previous attempt to communicate to the back-end server
                    // or database failed, retry
                    if (!GlobalData.Preferences.ParsePreferencesStatus)
                        await PostPreferences(GlobalData.Preferences);
                    if (!GlobalData.Preferences.ParseVersionStatus)
                        await PostVersion();
                    // If the StoryBuilder version has changed, post the version change
                    if (!GlobalData.Version.Equals(GlobalData.Preferences.Version))
                    {
                        // Process a version change (usually a new release)
                        log.Log(LogLevel.Info, "Version mismatch: " + GlobalData.Version + " != " + GlobalData.Preferences.Version);
                        GlobalData.LoadedWithVersionChange = true;
                        var preferences = GlobalData.Preferences;
                        // Update Preferences
                        preferences.Version = GlobalData.Version;
                        PreferencesIO prefIO = new(preferences, GlobalData.RootDirectory);
                        await prefIO.UpdateFile();
                        // Post deployment to backend server
                        await PostVersion();
                    }
                }
                catch (Exception ex)
                {
                    log.LogException(LogLevel.Warn, ex, "Error in parse service worker");
                }
            };
            Worker.RunWorkerAsync();
        }
        public async Task PostPreferences(PreferencesModel preferences)
        {
            log.Log(LogLevel.Info, "Post user preferences to back-end database");

            MySqlIO sql = Ioc.Default.GetService<MySqlIO>();


            // Get a connection to the database
            MySqlConnection conn = new MySqlConnection(sql.ConnectionString);

            try
            {
                await conn.OpenAsync();

                string name = preferences.Name;
                string email = preferences.Email;
                int id = await sql.AddOrUpdateUser(conn, name, email);
                log.Log(LogLevel.Info, "Name: " + name + " userId: " + id);

                bool elmah = preferences.ErrorCollectionConsent;
                bool newsletter = preferences.Newsletter;
                string version = preferences.Version;
                await sql.AddOrUpdatePreferences(conn, id, elmah, newsletter, version);
                log.Log(LogLevel.Info, "Preferences:  elmah=" + elmah + " newsletter=" + newsletter);
            }
            // may want to use multiple catch clauses
            catch (Exception ex)
            {
                log.LogException(LogLevel.Warn, ex, ex.Message);
            }
            finally
            {
                await conn.CloseAsync();
                log.Log(LogLevel.Info, "Back-end database connection ended");
            }
        }

        public async Task PostVersion()
        {
            log.Log(LogLevel.Info, "Posting version data to parse");

            var preferences = GlobalData.Preferences;
            MySqlIO sql = Ioc.Default.GetService<MySqlIO>();

            // Get a connection to the database
            MySqlConnection conn = new MySqlConnection(sql.ConnectionString);

            try
            {
                await conn.OpenAsync();

                string name = preferences.Name;
                string email = preferences.Email;
                int id = await sql.AddOrUpdateUser(conn, name, email);
                log.Log(LogLevel.Info, "User Name: " + name + " userId: " + id);
                
                string current = GlobalData.Version;
                string previous = preferences.Version ?? "";
                await sql.AddVersion(conn, id, current, previous);
                log.Log(LogLevel.Info, "Version:  Current=" + current + " Previous=" + previous);
            }
            // May want to use multiple catch clauses
            catch (Exception ex)
            {
                log.LogException(LogLevel.Warn, ex, ex.Message);
            }
            finally
            {
                await conn.CloseAsync();
                log.Log(LogLevel.Info, "Back-end database connection ended");
            }
        }
    }
}
