using System.Data;
using MySql.Data.MySqlClient;

namespace StoryCADLib.DAL;

/// <summary>
///     MySQL data access layer. Each method opens its own connection,
///     executes a stored procedure, and closes the connection.
///     Connection string is set once via SetConnectionString (called
///     by BackendService after Doppler key retrieval).
/// </summary>
public class MySqlIo : IMySqlIo
{
    private string _connectionString = string.Empty;

    /// <inheritdoc />
    public bool IsConnectionConfigured { get; private set; }

    /// <inheritdoc />
    public void SetConnectionString(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return;
        _connectionString = connectionString;
        IsConnectionConfigured = true;
    }

    /// <inheritdoc />
    public async Task<int> AddOrUpdateUser(string name, string email)
    {
        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();

        await using MySqlCommand cmd = new("spAddUser", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("name", name);
        cmd.Parameters.AddWithValue("email", email);
        cmd.Parameters.Add("@user_id", MySqlDbType.Int32);
        cmd.Parameters["@user_id"].Direction = ParameterDirection.Output;
        await cmd.ExecuteNonQueryAsync();
        return (int)cmd.Parameters["@user_id"].Value;
    }

    /// <inheritdoc />
    public async Task AddOrUpdatePreferences(int id, bool elmah, bool newsletter, string version)
    {
        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();

        await using MySqlCommand cmd = new("spAddOrUpdatePreferences", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("user_id", id);
        cmd.Parameters.AddWithValue("elmah", elmah);
        cmd.Parameters.AddWithValue("newsletter", newsletter);
        cmd.Parameters.AddWithValue("ver", version);
        await cmd.ExecuteNonQueryAsync();
    }

    /// <inheritdoc />
    public async Task AddVersion(int id, string currentVersion, string previousVersion)
    {
        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();

        await using MySqlCommand cmd = new("spAddOrUpdateVersion", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("user_id", id);
        cmd.Parameters.AddWithValue("current_ver", currentVersion);
        cmd.Parameters.AddWithValue("previous_ver", previousVersion);
        await cmd.ExecuteNonQueryAsync();
    }

    /// <inheritdoc />
    public async Task<bool> DeleteUser(int id)
    {
        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();

        await using MySqlCommand cmd = new("spDeleteUser", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("user_id", id);
        cmd.Parameters.Add("@deleted", MySqlDbType.Byte);
        cmd.Parameters["@deleted"].Direction = ParameterDirection.Output;
        await cmd.ExecuteNonQueryAsync();
        return Convert.ToBoolean(cmd.Parameters["@deleted"].Value);
    }
}
