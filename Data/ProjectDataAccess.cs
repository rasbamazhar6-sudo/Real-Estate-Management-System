using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;

namespace Project.Data
{
    public class ProjectDataAccess
    {
        public static List<ProjectInfo> GetAllProjects()
        {
            var projects = new List<ProjectInfo>();

            string query = @"
                SELECT ProjectId, Name, Location, Status, CreatedAt, UpdatedAt
                FROM Projects
                ORDER BY Name";

            try
            {
                using var connection = DatabaseHelper.GetConnection();
                using var command = new SqlCommand(query, connection);
                connection.Open();

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    int projectIdOrd = reader.GetOrdinal("ProjectId");
                    int nameOrd = reader.GetOrdinal("Name");
                    int locationOrd = reader.GetOrdinal("Location");
                    int statusOrd = reader.GetOrdinal("Status");

                    projects.Add(new ProjectInfo
                    {
                        ProjectId = reader.GetInt32(projectIdOrd).ToString(),
                        ProjectName = reader.IsDBNull(nameOrd) ? "" : reader.GetString(nameOrd),
                        Location = reader.IsDBNull(locationOrd) ? "" : reader.GetString(locationOrd),
                        Status = reader.IsDBNull(statusOrd) ? "Active" : reader.GetString(statusOrd)
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading projects: {ex.Message}", ex);
            }

            return projects;
        }

        public static int InsertProject(string name, string location, string status)
        {
            string query = @"
                INSERT INTO Projects (Name, Location, Status, CreatedAt, UpdatedAt)
                OUTPUT INSERTED.ProjectId
                VALUES (@Name, @Location, @Status, GETDATE(), GETDATE())";

            try
            {
                using var connection = DatabaseHelper.GetConnection();
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Name", name);
                command.Parameters.AddWithValue("@Location", location ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Status", status ?? "Active");

                connection.Open();
                return (int)command.ExecuteScalar();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error inserting project: {ex.Message}", ex);
            }
        }

        public static void UpdateProject(int projectId, string name, string location, string status)
        {
            string query = @"
                UPDATE Projects
                SET Name = @Name,
                    Location = @Location,
                    Status = @Status,
                    UpdatedAt = GETDATE()
                WHERE ProjectId = @ProjectId";

            try
            {
                using var connection = DatabaseHelper.GetConnection();
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@ProjectId", projectId);
                command.Parameters.AddWithValue("@Name", name);
                command.Parameters.AddWithValue("@Location", location ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Status", status ?? "Active");

                connection.Open();
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error updating project: {ex.Message}", ex);
            }
        }

        public static bool HasRelatedRecords(int projectId)
        {
            string query = @"
                SELECT 
                    (SELECT COUNT(1) FROM Plots WHERE ProjectId = @ProjectId) +
                    (SELECT COUNT(1) FROM Sales WHERE ProjectId = @ProjectId)";

            try
            {
                using var connection = DatabaseHelper.GetConnection();
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@ProjectId", projectId);
                connection.Open();
                var result = command.ExecuteScalar();
                return result != null && Convert.ToInt32(result) > 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error checking related records: {ex.Message}", ex);
            }
        }

        public static (int plotCount, int saleCount) GetRelatedRecordCounts(int projectId)
        {
            string query = @"
                SELECT 
                    (SELECT COUNT(1) FROM Plots WHERE ProjectId = @ProjectId) as PlotCount,
                    (SELECT COUNT(1) FROM Sales WHERE ProjectId = @ProjectId) as SaleCount";

            try
            {
                using var connection = DatabaseHelper.GetConnection();
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@ProjectId", projectId);
                connection.Open();
                
                using var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    int plotCount = reader.GetInt32(0);
                    int saleCount = reader.GetInt32(1);
                    return (plotCount, saleCount);
                }
                return (0, 0);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting related record counts: {ex.Message}", ex);
            }
        }

        public static void DeleteProject(int projectId)
        {
            if (HasRelatedRecords(projectId))
            {
                var (plotCount, saleCount) = GetRelatedRecordCounts(projectId);
                
                var reasons = new System.Collections.Generic.List<string>();
                if (plotCount > 0)
                    reasons.Add($"{plotCount} plot(s)");
                if (saleCount > 0)
                    reasons.Add($"{saleCount} sale(s)");
                
                string reasonText = string.Join(" and ", reasons);
                throw new Exception($"Cannot delete project. This project has {reasonText} associated with it. Please delete or reassign these records first.");
            }

            string query = "DELETE FROM Projects WHERE ProjectId = @ProjectId";

            try
            {
                using var connection = DatabaseHelper.GetConnection();
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@ProjectId", projectId);
                connection.Open();
                command.ExecuteNonQuery();
            }
            catch (Microsoft.Data.SqlClient.SqlException sqlEx)
            {
                if (sqlEx.Number == 547)
                {
                    try
                    {
                        var (plotCount, saleCount) = GetRelatedRecordCounts(projectId);
                        
                        var reasons = new System.Collections.Generic.List<string>();
                        if (plotCount > 0)
                            reasons.Add($"{plotCount} plot(s)");
                        if (saleCount > 0)
                            reasons.Add($"{saleCount} sale(s)");
                        
                        string reasonText = string.Join(" and ", reasons);
                        throw new Exception($"Cannot delete project. This project has {reasonText} associated with it. Please delete or reassign these records first.");
                    }
                    catch (Exception innerEx)
                    {
                        if (innerEx.Message.Contains("Cannot delete project"))
                        {
                            throw;
                        }
                        throw new Exception("Cannot delete project. This project has related records (plots, sales, etc.) that must be deleted first.");
                    }
                }
                throw new Exception("Database error occurred while deleting the project. Please check if the project has related records.");
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Cannot delete project"))
                {
                    throw;
                }
                throw new Exception($"An error occurred while deleting the project: {ex.Message}");
            }
        }

        public class ProjectInfo
        {
            public string ProjectId { get; set; } = string.Empty;
            public string ProjectName { get; set; } = string.Empty;
            public string Location { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
        }
    }
}

