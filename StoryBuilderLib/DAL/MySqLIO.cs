using System.Data;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace StoryBuilder.DAL;

public class MySqlIo
{
    public async Task<int> AddOrUpdateUser(MySqlConnection conn, string name, string email)
    {
        await using MySqlCommand _Cmd = new("spAddUser", conn);
        _Cmd.CommandType = CommandType.StoredProcedure;
        _Cmd.Parameters.AddWithValue("name", name);
        _Cmd.Parameters.AddWithValue("email", email);
        _Cmd.Parameters.Add("@user_id", MySqlDbType.Int32);
        _Cmd.Parameters["@user_id"].Direction = ParameterDirection.Output;
        await _Cmd.ExecuteNonQueryAsync();
        int _Id = (int)_Cmd.Parameters["@user_id"].Value;
        return _Id;
    }
    public async Task AddOrUpdatePreferences(MySqlConnection conn, int id, bool elmah, bool newsletter, string version)
    {
        const string sql = "INSERT INTO StoryBuilder.preferences" +
                            " (user_id, elmah_consent, newsletter_consent, version)" +
                            " VALUES (@user_id,@elmah,@newsletter, @version)" +
                            " ON DUPLICATE KEY UPDATE elmah_consent = @elmah, newsletter_consent = @newsletter, version = @version";
        await using MySqlCommand _Cmd = new(sql, conn);
        _Cmd.Parameters.AddWithValue("@user_id", id);
        _Cmd.Parameters.AddWithValue("@elmah", elmah);
        _Cmd.Parameters.AddWithValue("@newsletter", newsletter);
        _Cmd.Parameters.AddWithValue("@version", version);
        await _Cmd.ExecuteNonQueryAsync();
    }

    public async Task AddVersion(MySqlConnection conn, int id, string currentVersion, string previousVersion)
    {
        const string sql = "INSERT INTO StoryBuilder.versions" +
                            " (user_id, current_version, previous_version)" +
                            " VALUES (@user_id,@current,@previous)";
        await using (MySqlCommand _Cmd = new(sql, conn))
        {
            _Cmd.Parameters.AddWithValue("@user_id", id);
            _Cmd.Parameters.AddWithValue("@current", currentVersion);
            _Cmd.Parameters.AddWithValue("@previous", previousVersion);
            await _Cmd.ExecuteNonQueryAsync();
        }
    }
}