using System.Data;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using StoryBuilder.Services.Backend;

namespace StoryBuilder.DAL
{
    public class MySqlIO
    {
        public string ConnectionString = "server=localhost;database=StoryBuilder;uid=stb;pwd=stb;";

        /// <summary>
        ///  Read a specific users row given the id
        /// </summary>
        /// <param name="id"></param>
        public async Task<UsersTable> ReadUserByID(MySqlConnection conn, int id)
        {
            string sql = "SELECT id, user_name, email,date_added FROM users WHERE id = @Id";
            UsersTable users = new UsersTable();

            await using (var cmd = new MySqlCommand(sql, conn))
            {
                cmd.Parameters.Add("@Id", MySqlDbType.Int32);
                cmd.Parameters["@Id"].Value = id;
                var reader = await cmd.ExecuteReaderAsync();

                if (reader.Read())
                {
                    users.Id = reader.GetInt32(0);
                    users.UserName = reader.GetString(1);
                    users.Email = reader.GetString(2);
                    users.DateAdded = reader.GetDateTime(3);
                }
            }

            return users;
        }
        public async Task<UsersTable> ReadUserByEmail(MySqlConnection conn, string email)
        {
            string sql = "SELECT id, user_name, email, date_added FROM users WHERE email = @Email";
            UsersTable users = new UsersTable();

            await using (var cmd = new MySqlCommand(sql, conn))
            {
                cmd.Parameters.Add("@Email", MySqlDbType.String);
                cmd.Parameters["@UserName"].Value = email;
                var reader = await cmd.ExecuteReaderAsync();

                if (reader.Read())
                {
                    users.Id = reader.GetInt32(0);
                    users.UserName = reader.GetString(1);
                    users.Email = reader.GetString(2);
                    users.DateAdded = reader.GetDateTime(3);
                }
            }

            return users;
        }
        public async Task<int> AddOrUpdateUser(MySqlConnection conn, string name, string email)
        {
            string rtn = "spAddUser";
            await using var cmd = new MySqlCommand(rtn, conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("name", name);
            cmd.Parameters.AddWithValue("email", email);
            cmd.Parameters.Add("@user_id", MySqlDbType.Int32);
            cmd.Parameters["@user_id"].Direction = ParameterDirection.Output;
            await cmd.ExecuteNonQueryAsync();
            int id = (int)cmd.Parameters["@user_id"].Value;
            return id;
        }
        public async Task AddOrUpdatePreferences(MySqlConnection conn, int id, bool elmah, bool newsletter, string version)
        {
            string sql =
                "INSERT INTO StoryBuilder.preferences" +
                " (user_id, elmah_consent, newsletter_consent, version)" +
                " VALUES (@user_id,@elmah,@newsletter, @version)" +
                " ON DUPLICATE KEY UPDATE elmah_consent = @elmah, newsletter_consent = @newsletter, version = @version";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@user_id", id);
            cmd.Parameters.AddWithValue("@elmah", elmah);
            cmd.Parameters.AddWithValue("@newsletter", newsletter);
            cmd.Parameters.AddWithValue("@version", version);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task AddVersion(MySqlConnection conn, int id, string currentVersion, string previousVersion)
        {

            string sql = "INSERT INTO StoryBuilder.versions" +
                         " (user_id, current_version, previous_version)" +
                         " VALUES (@user_id,@current,@previous)";
            await using (var cmd = new MySqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@user_id", id);
                cmd.Parameters.AddWithValue("@current", currentVersion);
                cmd.Parameters.AddWithValue("@previous", previousVersion);
                await cmd.ExecuteNonQueryAsync();
            }
        }
        public MySqlIO()
        {
            // obtain connection string data from Doppler?
        }

    }
}
