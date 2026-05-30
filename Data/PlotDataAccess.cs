using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;

namespace Project.Data
{
    public class PlotDataAccess
    {
        public static List<PlotInfo> GetAllPlots()
        {
            var plots = new List<PlotInfo>();

            string query = @"
                SELECT PlotId, PlotNo, p.Name as ProjectName
                FROM Plots pl
                INNER JOIN Projects p ON pl.ProjectId = p.ProjectId
                ORDER BY PlotNo";

            try
            {
                using var connection = DatabaseHelper.GetConnection();
                using var command = new SqlCommand(query, connection);
                connection.Open();

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    plots.Add(new PlotInfo
                    {
                        PlotId = reader.GetInt32(reader.GetOrdinal("PlotId")).ToString(),
                        PlotNo = reader.IsDBNull(reader.GetOrdinal("PlotNo")) ? "" : reader.GetString(reader.GetOrdinal("PlotNo"))
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading plots: {ex.Message}", ex);
            }

            return plots;
        }

        public class PlotInfo
        {
            public string PlotId { get; set; } = string.Empty;
            public string PlotNo { get; set; } = string.Empty;
        }
    }
}

