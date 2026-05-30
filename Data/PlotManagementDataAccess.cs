using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;

namespace Project.Data
{
    public class PlotManagementDataAccess
    {
        public static List<PlotInfo> GetAllPlots()
        {
            var plots = new List<PlotInfo>();

            string query = @"
                SELECT pl.PlotId, pl.PlotNo, pl.SizeMarla, pl.Price, pl.Status, pl.ProjectId, 
                       pr.Name as ProjectName, 
                       ISNULL(pl.OwnerId, 0) as OwnerId, 
                       ISNULL(pt.Name, '') as OwnerName
                FROM Plots pl
                INNER JOIN Projects pr ON pl.ProjectId = pr.ProjectId
                LEFT JOIN Parties pt ON pl.OwnerId = pt.PartyId
                ORDER BY pl.PlotNo";

            try
            {
                using var connection = DatabaseHelper.GetConnection();
                using var command = new SqlCommand(query, connection);
                connection.Open();

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    int plotIdOrd = reader.GetOrdinal("PlotId");
                    int plotNoOrd = reader.GetOrdinal("PlotNo");
                    int sizeMarlaOrd = reader.GetOrdinal("SizeMarla");
                    int priceOrd = reader.GetOrdinal("Price");
                    int statusOrd = reader.GetOrdinal("Status");
                    int projectIdOrd = reader.GetOrdinal("ProjectId");
                    int projectNameOrd = reader.GetOrdinal("ProjectName");
                    int ownerIdOrd = reader.GetOrdinal("OwnerId");
                    int ownerNameOrd = reader.GetOrdinal("OwnerName");

                    int ownerIdValue = reader.GetInt32(ownerIdOrd);
                    int? ownerId = ownerIdValue == 0 ? null : (int?)ownerIdValue;
                    string ownerName = reader.IsDBNull(ownerNameOrd) ? "" : reader.GetString(ownerNameOrd);

                    plots.Add(new PlotInfo
                    {
                        PlotId = reader.GetInt32(plotIdOrd),
                        PlotNo = reader.IsDBNull(plotNoOrd) ? "" : reader.GetString(plotNoOrd),
                        SizeMarla = reader.IsDBNull(sizeMarlaOrd) ? 0 : reader.GetDecimal(sizeMarlaOrd),
                        Price = reader.IsDBNull(priceOrd) ? 0 : reader.GetDecimal(priceOrd),
                        Status = reader.IsDBNull(statusOrd) ? "Available" : reader.GetString(statusOrd),
                        ProjectId = reader.GetInt32(projectIdOrd),
                        ProjectName = reader.IsDBNull(projectNameOrd) ? "" : reader.GetString(projectNameOrd),
                        OwnerId = ownerId,
                        OwnerName = ownerName
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading plots: {ex.Message}", ex);
            }

            return plots;
        }

        public static int InsertPlot(int projectId, string plotNo, decimal sizeMarla, decimal price, string status, int? ownerId = null)
        {
            string query = @"
                INSERT INTO Plots (ProjectId, PlotNo, SizeMarla, Price, Status, OwnerId, CreatedAt, UpdatedAt)
                OUTPUT INSERTED.PlotId
                VALUES (@ProjectId, @PlotNo, @SizeMarla, @Price, @Status, @OwnerId, GETDATE(), GETDATE())";

            try
            {
                using var connection = DatabaseHelper.GetConnection();
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@ProjectId", projectId);
                command.Parameters.AddWithValue("@PlotNo", plotNo);
                command.Parameters.AddWithValue("@SizeMarla", sizeMarla);
                command.Parameters.AddWithValue("@Price", price);
                command.Parameters.AddWithValue("@Status", status ?? "Available");
                command.Parameters.AddWithValue("@OwnerId", ownerId ?? (object)DBNull.Value);

                connection.Open();
                return (int)command.ExecuteScalar();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error inserting plot: {ex.Message}", ex);
            }
        }

        public static void UpdatePlot(int plotId, int projectId, string plotNo, decimal sizeMarla, decimal price, string status, int? ownerId = null)
        {
            string query = @"
                UPDATE Plots
                SET ProjectId = @ProjectId,
                    PlotNo = @PlotNo,
                    SizeMarla = @SizeMarla,
                    Price = @Price,
                    Status = @Status,
                    OwnerId = @OwnerId,
                    UpdatedAt = GETDATE()
                WHERE PlotId = @PlotId";

            try
            {
                using var connection = DatabaseHelper.GetConnection();
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@PlotId", plotId);
                command.Parameters.AddWithValue("@ProjectId", projectId);
                command.Parameters.AddWithValue("@PlotNo", plotNo);
                command.Parameters.AddWithValue("@SizeMarla", sizeMarla);
                command.Parameters.AddWithValue("@Price", price);
                command.Parameters.AddWithValue("@Status", status ?? "Available");
                command.Parameters.AddWithValue("@OwnerId", ownerId ?? (object)DBNull.Value);

                connection.Open();
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error updating plot: {ex.Message}", ex);
            }
        }


        public static bool HasRelatedSales(int plotId)
        {
            string query = "SELECT COUNT(1) FROM Sales WHERE PlotId = @PlotId";

            try
            {
                using var connection = DatabaseHelper.GetConnection();
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@PlotId", plotId);
                connection.Open();
                return (int)command.ExecuteScalar() > 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error checking related sales: {ex.Message}", ex);
            }
        }

        public static int GetSaleCount(int plotId)
        {
            string query = "SELECT COUNT(1) FROM Sales WHERE PlotId = @PlotId";

            try
            {
                using var connection = DatabaseHelper.GetConnection();
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@PlotId", plotId);
                connection.Open();
                return (int)command.ExecuteScalar();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting sale count: {ex.Message}", ex);
            }
        }

        public static void DeletePlot(int plotId)
        {
            if (HasRelatedSales(plotId))
            {
                int saleCount = GetSaleCount(plotId);
                throw new Exception($"Cannot delete plot. This plot has {saleCount} sale(s) associated with it. Please delete the sales first.");
            }

            string query = "DELETE FROM Plots WHERE PlotId = @PlotId";

            try
            {
                using var connection = DatabaseHelper.GetConnection();
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@PlotId", plotId);
                connection.Open();
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Cannot delete plot"))
                {
                    throw;
                }
                throw new Exception($"Error deleting plot: {ex.Message}", ex);
            }
        }

        public static int GetProjectIdByPlotId(int plotId)
        {
            string query = "SELECT ProjectId FROM Plots WHERE PlotId = @PlotId";
            try
            {
                using var connection = DatabaseHelper.GetConnection();
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@PlotId", plotId);
                connection.Open();
                var result = command.ExecuteScalar();
                return result != null ? (int)result : 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting project ID: {ex.Message}", ex);
            }
        }

        public class PlotInfo
        {
            public int PlotId { get; set; }
            public string PlotNo { get; set; } = string.Empty;
            public decimal SizeMarla { get; set; }
            public decimal Price { get; set; }
            public string Status { get; set; } = string.Empty;
            public int ProjectId { get; set; }
            public string ProjectName { get; set; } = string.Empty;
            public int? OwnerId { get; set; }
            public string OwnerName { get; set; } = string.Empty;
        }
    }
}

