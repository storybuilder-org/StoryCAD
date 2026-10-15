using MySql.Data.MySqlClient;
using StoryCADLib.DAL;

#nullable disable

namespace StoryCADTests.DAL;

/// <summary>
///     Runs against the local test database (StoryCADTests/TestDb, with V003 to V005 applied).
///     Inconclusive when the database is not running, as on CI.
/// </summary>
[TestClass]
public class MySqlIoTests
{
    private static readonly string ConnectionString =
        Environment.GetEnvironmentVariable("STORYCAD_TEST_CONNECTION")
        ?? "Server=localhost;Port=3306;Database=StoryBuilder;Uid=stbtest;Pwd=123";

    private MySqlIo _sqlIo;
    private int _userId;

    [TestInitialize]
    public async Task Setup()
    {
        try
        {
            await using var conn = new MySqlConnection(ConnectionString);
            await conn.OpenAsync();
        }
        catch (MySqlException)
        {
            Assert.Inconclusive("The local test database is not running (StoryCADTests/TestDb).");
        }

        _sqlIo = new MySqlIo();
        _sqlIo.SetConnectionString(ConnectionString);
        _userId = await _sqlIo.AddOrUpdateUser("MySqlIo Test", $"mysqlio-{Guid.NewGuid():N}@example.test");
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        if (_userId > 0)
        {
            await _sqlIo.DeleteUser(_userId);
        }
    }

    [TestMethod]
    public async Task AddUserToGroup_NewMember_AddsOneRow()
    {
        // Act
        await _sqlIo.AddUserToGroup(_userId, "collab_beta");

        // Assert
        Assert.AreEqual(1, await GroupRows(_userId, "collab_beta"));
    }

    [TestMethod]
    public async Task AddUserToGroup_ExistingMember_KeepsOneRow()
    {
        // Arrange
        await _sqlIo.AddUserToGroup(_userId, "collab_beta");

        // Act - the app adds the user again at each startup
        await _sqlIo.AddUserToGroup(_userId, "collab_beta");

        // Assert
        Assert.AreEqual(1, await GroupRows(_userId, "collab_beta"));
    }

    private static async Task<long> GroupRows(int userId, string group)
    {
        await using var conn = new MySqlConnection(ConnectionString);
        await conn.OpenAsync();
        await using var cmd = new MySqlCommand(
            "SELECT COUNT(*) FROM user_groups WHERE user_id = @u AND group_name = @g", conn);
        cmd.Parameters.AddWithValue("@u", userId);
        cmd.Parameters.AddWithValue("@g", group);
        return (long)await cmd.ExecuteScalarAsync();
    }
}
