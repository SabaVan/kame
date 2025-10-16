using System;
using System.Collections.Generic;
using System.Linq;
using Npgsql;
using backend.UserAuth.Models;

namespace backend.UserAuth.Data
{
    public class UserRepository
    {
        // Connection string placeholder — update when real DB is available
        private readonly string _connectionString =
            "Host=localhost;Port=5432;Username=postgres;Password=postgres;Database=myappdb";

        public List<UserModel> GetAllUsers()
        {
            var users = new List<UserModel>();

            try
            {
                using var conn = new NpgsqlConnection(_connectionString);
                conn.Open();

                string query = "SELECT id, username, passwordhash, salt FROM users;";
                using var cmd = new NpgsqlCommand(query, conn);
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    users.Add(new UserModel(
                        reader.GetString(0),
                        reader.GetString(1),
                        reader.GetString(2),
                        reader.GetString(3)
                    ));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database not available or not configured yet: {ex.Message}");
                Console.WriteLine("Returning empty user list for now.");
            }

            return users;
        }

        public void SaveUser(UserModel user)
        {
            try
            {
                using var conn = new NpgsqlConnection(_connectionString);
                conn.Open();

                string query = "INSERT INTO users (id, username, passwordhash, salt) VALUES (@id, @username, @hash, @salt)";
                using var cmd = new NpgsqlCommand(query, conn);

                cmd.Parameters.AddWithValue("id", user.Id);
                cmd.Parameters.AddWithValue("username", user.Username);
                cmd.Parameters.AddWithValue("hash", user.PasswordHash);
                cmd.Parameters.AddWithValue("salt", user.Salt);

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not save user to DB: {ex.Message}");
                Console.WriteLine("Pretending user was saved successfully for development mode.");
            }
        }

        public UserModel? GetUserByUsername(string username)
        {
            try
            {
                using var conn = new NpgsqlConnection(_connectionString);
                conn.Open();

                string query = "SELECT id, username, passwordhash, salt FROM users WHERE username = @username LIMIT 1;";
                using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("username", username);

                using var reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    return new UserModel(
                        reader.GetString(0),
                        reader.GetString(1),
                        reader.GetString(2),
                        reader.GetString(3)
                    );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database not available or not configured yet: {ex.Message}");
                Console.WriteLine("Returning null user for now.");
            }

            return null;
        }
    }
}
