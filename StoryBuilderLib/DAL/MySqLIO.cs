using System;
using System.Data;
using System.Runtime.Serialization.Json;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using StoryBuilder.Services.Backend;

namespace StoryBuilder.DAL
{

    /// <summary>
    /// don't hold database connection longer than necessary
    // connection string values from Doppler (with dev and prd variants)
    // (note: get rid of parse server Doppler data)
    // logging
    // async io
    //    see https://stackoverflow.com/questions/39967208/c-sharp-mysql-driver-async-operations
    // methods:
    //   read userid given userid number
    //   add user (use stored procedure)
    //   update user (if userid exists and name or email changes)
    //   add or update preferences (add if add user, update if user exists
    //   add version
    /// </summary>
    public class MySqlIO
    {
        private string connectionString = "server=localhost;database=StoryBuilder;uid=stb;pwd=stb;";

        /// <summary>
        ///  Read a specific users row given the id
        /// </summary>
        /// <param name="id"></param>
        public async Task<UsersTable> ReadUser(int id) 
        {
            string sql = "SELECT id, user_name, email,date_added FROM users WHERE id = @Id";
            UsersTable users = new UsersTable();

            try
            {
                await using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
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
                }

                return users;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<UsersTable> ReadUser(string email)
        {
            string sql = "SELECT id, user_name, email, date_added FROM users WHERE email = @Email";
            UsersTable users = new UsersTable();

            try
            {
                await using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
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
                }

                return users;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<int> AddUser(string name, string email)
        {
            try
            {
                await using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    string rtn = "AddUser";
                    await using (var cmd = new MySqlCommand(rtn, conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@user_name", name);
                        cmd.Parameters.AddWithValue("@email", email);
                        cmd.Parameters.Add("@Id", MySqlDbType.Int32);
                        cmd.Parameters["@Id"].Direction = ParameterDirection.Output;
                        var reader = await cmd.ExecuteReaderAsync();
                        int id = (int)cmd.Parameters["@Id"].Value;
                        return id;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
     
        }

        public MySqlIO()
        {
            // obtain connection string data from Doppler?
        }
    }
}
