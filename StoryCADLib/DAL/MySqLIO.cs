using System.Data;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace StoryCAD.DAL;

public class MySqlIo
{
    public async Task<int> AddOrUpdateUser(MySqlConnection conn, string name, string email)
    {
        await using MySqlCommand _cmd = new("spAddUser", conn);
        _cmd.CommandType = CommandType.StoredProcedure;
        _cmd.Parameters.AddWithValue("name", name);
        _cmd.Parameters.AddWithValue("email", email);
        _cmd.Parameters.Add("@user_id", MySqlDbType.Int32);
        _cmd.Parameters["@user_id"].Direction = ParameterDirection.Output;
        await _cmd.ExecuteNonQueryAsync();
        int _id = (int)_cmd.Parameters["@user_id"].Value;
        return _id;
    }
    public async Task AddOrUpdatePreferences(MySqlConnection conn, int id, bool elmah, bool newsletter, string version)
    {
        const string sql = "INSERT INTO StoryBuilder.preferences" +
                            " (user_id, elmah_consent, newsletter_consent, version)" +
                            " VALUES (@user_id,@elmah,@newsletter, @version)" +
                            " ON DUPLICATE KEY UPDATE elmah_consent = @elmah, newsletter_consent = @newsletter, version = @version";
        await using MySqlCommand _cmd = new(sql, conn);
        _cmd.Parameters.AddWithValue("@user_id", id);
        _cmd.Parameters.AddWithValue("@elmah", elmah);
        _cmd.Parameters.AddWithValue("@newsletter", newsletter);
        _cmd.Parameters.AddWithValue("@version", version);
        await _cmd.ExecuteNonQueryAsync();
    }

    public async Task AddVersion(MySqlConnection conn, int id, string currentVersion, string previousVersion)
    {
        const string sql = "INSERT INTO StoryBuilder.versions" +
                           " (user_id, current_version, previous_version)" +
                           " VALUES (@user_id, @current, @previous)" +
                           " ON DUPLICATE KEY UPDATE" +
                           " current_version = @current, previous_version = @previous";

        await using (MySqlCommand _cmd = new(sql, conn))
        {
            _cmd.Parameters.AddWithValue("@user_id", id);
            _cmd.Parameters.AddWithValue("@current", currentVersion);
            _cmd.Parameters.AddWithValue("@previous", previousVersion);
            await _cmd.ExecuteNonQueryAsync();
        }
    }

}