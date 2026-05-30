using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;

namespace Project.Data
{
    public class UserDataAccess
    {
        public static bool EmailExists(string email)
        {
            string query = "SELECT COUNT(1) FROM Users WHERE Email = @Email";

            try
            {
                using var connection = DatabaseHelper.GetConnection();
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Email", email);
                connection.Open();
                return (int)command.ExecuteScalar() > 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error checking email: {ex.Message}", ex);
            }
        }

        public static bool AuthenticateUser(string email, string passwordHash)
        {
            string query = @"
                SELECT COUNT(1) 
                FROM Users 
                WHERE Email = @Email 
                AND PasswordHash = @PasswordHash 
                AND IsActive = 1";

            try
            {
                using var connection = DatabaseHelper.GetConnection();
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Email", email);
                command.Parameters.AddWithValue("@PasswordHash", passwordHash);
                connection.Open();
                return (int)command.ExecuteScalar() == 1;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error authenticating user: {ex.Message}", ex);
            }
        }

        public static int CreateUser(string name, string email, string passwordHash)
        {
            const string insertQuery = @"
                    INSERT INTO Users (Name, Email, PasswordHash, IsActive, CreatedAt, UpdatedAt)
                    OUTPUT INSERTED.UserId
                    VALUES (@Name, @Email, @PasswordHash, 1, GETDATE(), GETDATE())";

            try
            {
                using var connection = DatabaseHelper.GetConnection();
                using var command = new SqlCommand(insertQuery, connection);
                command.Parameters.AddWithValue("@Name", name);
                command.Parameters.AddWithValue("@Email", email);
                command.Parameters.AddWithValue("@PasswordHash", passwordHash);

                connection.Open();
                return (int)command.ExecuteScalar();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error creating user: {ex.Message}", ex);
            }
        }

        public static string HashPassword(string password)
        {
            using SHA256 sha = SHA256.Create();
            byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        public static UserInfo? GetUserByEmail(string email)
        {
            const string query = "SELECT UserId, Name, Email, IsActive FROM Users WHERE Email = @Email";

            try
            {
                using var connection = DatabaseHelper.GetConnection();
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Email", email);
                connection.Open();

                using var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    int userIdOrd = reader.GetOrdinal("UserId");
                    int NameOrd = reader.GetOrdinal("Name");
                    int emailOrd = reader.GetOrdinal("Email");
                    int isActiveOrd = reader.GetOrdinal("IsActive");

                    return new UserInfo
                    {
                        UserId = reader.GetInt32(userIdOrd),
                        Name = reader.IsDBNull(NameOrd) ? "" : reader.GetString(NameOrd),
                        Email = reader.IsDBNull(emailOrd) ? "" : reader.GetString(emailOrd),
                        IsActive = !reader.IsDBNull(isActiveOrd) && reader.GetBoolean(isActiveOrd)
                    };
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting user: {ex.Message}", ex);
            }

            return null;
        }

        public class UserInfo
        {
            public int UserId { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public bool IsActive { get; set; }
        }
    }
}

