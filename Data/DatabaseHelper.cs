using System.Data;
using System.IO;
using Microsoft.Data.SqlClient;

namespace Project.Data
{
    public static class DatabaseHelper
    {
        private static readonly string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "RealEstateDB.mdf");

        private static readonly string ConnectionString =
                                        $@"
                                Data Source=(LocalDB)\MSSQLLocalDB;
                                AttachDbFilename={dbPath};
                                Integrated Security=True;
                                Connect Timeout=30;";

        public static SqlConnection GetConnection()
        {
            return new SqlConnection(ConnectionString);
        }

        public static bool TestConnection()
        {
            try
            {
                using var connection = GetConnection();
                connection.Open();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Database connection error: {ex.Message}");
                return false;
            }
        }

        public static string GetConnectionError()
        {
            try
            {
                using var connection = GetConnection();
                connection.Open();
                return string.Empty;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
    }
}

