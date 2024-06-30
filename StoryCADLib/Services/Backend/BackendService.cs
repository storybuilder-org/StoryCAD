using Windows.Storage;
using MySql.Data.MySqlClient;
using StoryCAD.DAL;
using StoryCAD.Models.Tools;
using StoryCAD.Services.Json;

namespace StoryCAD.Services.Backend;

/// <summary>
/// BackendService is StoryCAD's interface to our backend server 
/// which is hosted on ScaleGrid. The  server runs a MsSQL database.
///
/// We use three tables to store client account information:
///     users           userid (generated), name, and email address
///     preferences     consent to use elmah.io, consent to receive newsletter
///     versions        version change (current and previous version of StoryCAD).
/// 
/// This service contains two methods used to log changes in a client that we need
/// to track:
///     PostPreferences() is called when a user establishes or changes his preferences
///         (specifically his permissions for elmah.io logging and for receiving
///         our newsletter.)
///     PostVersion() is called when the version of StoryCAD is running changes,
///         via update or from the user uninstalling / reinstalling the app.
/// 
/// Both PostPreferences and PostVersion assume a user known to the back-end
/// StoryCAD MySQL database and identified by a record in the 'users' table.
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
	private AppState State = Ioc.Default.GetService<AppState>();
	private PreferencesModel Preferences = Ioc.Default.GetService<PreferenceService>().Model;
	private string connection = string.Empty;
	private string sslCA = string.Empty;


	/// <summary>
	/// Do any necessary posting to the backend MySql server on app
	/// startup. This will include either preferences or versions
	/// posting that weren't successful during the last run.
	///
	/// Also, if the app version has changed because of an update,
	/// post the new version.
	/// </summary>
	public async Task StartupRecording()
	{
		try
		{
			// If the previous attempt to communicate to the back-end server
			// or database failed, retry
			if (Preferences.RecordPreferencesStatus)
				await PostPreferences(Preferences);
               
			// If the StoryCAD version has changed, post the version change
			if (!State.Version.Equals(Preferences.Version))
			{
				// Process a version change (usually a new release)
				log.Log(LogLevel.Info, "Version mismatch: " + State.Version + " != " + Preferences.Version);
				State.LoadedWithVersionChange = true;
				PreferencesModel preferences = Preferences;

				// Update Preferences
				preferences.Version = State.Version;
				PreferencesIo prefIO = new(preferences, State.RootDirectory);
				await prefIO.WritePreferences();

				// Post deployment to backend server
				await PostVersion();
			}
			else if (!Preferences.RecordVersionStatus)
				await PostVersion();
		}
		catch (Exception ex)
		{
			log.LogException(LogLevel.Error, ex, "Error in StartupRecording method");
		}
	}

	public async Task PostPreferences(PreferencesModel preferences)
	{
		log.Log(LogLevel.Info, "Post user preferences to back-end database");

		MySqlIo sql = Ioc.Default.GetService<MySqlIo>();


		// Get a connection to the database
		MySqlConnection conn = new(connection);

		try
		{
			await conn.OpenAsync();

			string name = preferences.FirstName + " " + preferences.LastName;
			string email = preferences.Email;
			int id = await sql.AddOrUpdateUser(conn, name, email);
			log.Log(LogLevel.Info, "Name: " + name + " userId: " + id);

			bool elmah = preferences.ErrorCollectionConsent;
			bool newsletter = preferences.Newsletter;
			string version = preferences.Version;
			// Workaround for an issue with the PreferencesModel Version property.
			// It has a built-in title. We need to remove the title before we log.
			if (version.StartsWith("Version: "))
				version = version.Substring(9);
			// Post the preferences to the database
			await sql.AddOrUpdatePreferences(conn, id, elmah, newsletter, version);
			// Indicate we've stored them successfully
			Preferences.RecordPreferencesStatus = true;
			PreferencesIo loader = new(Preferences, State.RootDirectory);
			await loader.WritePreferences();
			log.Log(LogLevel.Info, "Preferences:  elmah=" + elmah + " newsletter=" + newsletter);
		}
		// may want to use multiple catch clauses
		catch (Exception ex)
		{
			log.LogException(LogLevel.Error, ex, ex.Message);
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

		PreferencesModel preferences = Preferences;
		MySqlIo sql = Ioc.Default.GetService<MySqlIo>();

		// Get a connection to the database
		MySqlConnection conn = new(connection);

		try
		{
			await conn.OpenAsync();

			string name = preferences.FirstName + " " + preferences.LastName;
			string email = preferences.Email;
			int id = await sql.AddOrUpdateUser(conn, name, email);
			log.Log(LogLevel.Info, "User Name: " + name + " userId: " + id);

			string current = State.Version;
			string previous = preferences.Version ?? "";
			// Post the version change to the database
			await sql.AddVersion(conn, id, current, previous);
			// Indicate we've stored it  successfully
			Preferences.RecordVersionStatus = true;
			PreferencesIo loader = new(Preferences, State.RootDirectory);
			await loader.WritePreferences();
			log.Log(LogLevel.Info, "Version:  Current=" + current + " Previous=" + previous);
		}
		// May want to use multiple catch clauses
		catch (Exception ex)
		{
			log.LogException(LogLevel.Error, ex, ex.Message);
		}
		finally
		{
			await conn.CloseAsync();
			log.Log(LogLevel.Info, "Back-end database connection ended");
		}
	}

	public async Task SetConnectionString(Doppler keys)
	{
		try
		{
			log.Log(LogLevel.Info, "GetConnectionString");
			StorageFolder tempFolder = await StorageFolder.GetFolderFromPathAsync(System.IO.Path.GetTempPath());
			StorageFile tempFile = 
				await tempFolder.CreateFileAsync("StoryCAD.pem", CreationCollisionOption.ReplaceExisting);
			string caFile = keys.CAFILE;
			await FileIO.WriteTextAsync(tempFile, caFile);
			sslCA = $"SslCa={tempFile.Path};";
			// create MySQL connection string if keys are defined
			connection = keys.CONNECTION + sslCA; 
			// can compare the c:\certs and temp file to see if they are the same
		}
		catch (Exception ex)
		{
			log.LogException(LogLevel.Error, ex, ex.Message);
		}
	}

	public async Task DeleteWorkFile()
	{
		try
		{
			log.Log(LogLevel.Info, "DeleteWorkFile");
			string path = sslCA.Substring(6);   // remove leading 'SslCa='  
			path = path.Substring(0, path.Length - 1);  // remove trailing ';'
			StorageFile file = await StorageFile.GetFileFromPathAsync(path);
			await file.DeleteAsync();
		}
		catch (Exception ex)
		{
			log.LogException(LogLevel.Error, ex, ex.Message);
		}
	}
}