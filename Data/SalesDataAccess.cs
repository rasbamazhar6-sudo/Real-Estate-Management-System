using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;

namespace Project.Data
{
    public class SalesDataAccess
    {
        public static List<SaleInfo> GetAllSales()
        {
            var sales = new List<SaleInfo>();

            string query = @"
                SELECT 
                    s.SaleId,
                    s.SaleDate,
                    s.SalePrice,
                    s.DownPayment,
                    s.Status,
                    ISNULL(s.Notes, '') as Notes,
                    pl.PlotId,
                    pl.PlotNo,
                    pr.ProjectId,
                    pr.Name as ProjectName,
                    buyer.PartyId as BuyerId,
                    buyer.Name as BuyerName,
                    buyer.Type as BuyerType,
                    seller.PartyId as SellerId,
                    seller.Name as SellerName,
                    seller.Type as SellerType
                FROM Sales s
                INNER JOIN Plots pl ON s.PlotId = pl.PlotId
                INNER JOIN Projects pr ON s.ProjectId = pr.ProjectId
                LEFT JOIN Parties buyer ON s.PartyId = buyer.PartyId
                LEFT JOIN Parties seller ON s.SellerId = seller.PartyId
                ORDER BY s.SaleDate DESC";

            try
            {
                using var connection = DatabaseHelper.GetConnection();
                using var command = new SqlCommand(query, connection);
                connection.Open();

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    int saleIdOrd = reader.GetOrdinal("SaleId");
                    int saleDateOrd = reader.GetOrdinal("SaleDate");
                    int salePriceOrd = reader.GetOrdinal("SalePrice");
                    int downPaymentOrd = reader.GetOrdinal("DownPayment");
                    int statusOrd = reader.GetOrdinal("Status");
                    int notesOrd = reader.GetOrdinal("Notes");
                    int plotIdOrd = reader.GetOrdinal("PlotId");
                    int plotNoOrd = reader.GetOrdinal("PlotNo");
                    int projectNameOrd = reader.GetOrdinal("ProjectName");
                    int buyerIdOrd = reader.GetOrdinal("BuyerId");
                    int buyerNameOrd = reader.GetOrdinal("BuyerName");
                    int buyerTypeOrd = reader.GetOrdinal("BuyerType");
                    int sellerIdOrd = reader.GetOrdinal("SellerId");
                    int sellerNameOrd = reader.GetOrdinal("SellerName");
                    int sellerTypeOrd = reader.GetOrdinal("SellerType");

                    string notes = reader.IsDBNull(notesOrd) ? "" : reader.GetString(notesOrd);

                    sales.Add(new SaleInfo
                    {
                        SaleId = reader.GetInt32(saleIdOrd),
                        SaleDate = reader.GetDateTime(saleDateOrd),
                        SalePrice = reader.IsDBNull(salePriceOrd) ? 0 : reader.GetDecimal(salePriceOrd),
                        DownPayment = reader.IsDBNull(downPaymentOrd) ? 0 : reader.GetDecimal(downPaymentOrd),
                        Status = reader.IsDBNull(statusOrd) ? "Active" : reader.GetString(statusOrd),
                        PlotId = reader.GetInt32(plotIdOrd),
                        PlotNo = reader.IsDBNull(plotNoOrd) ? "" : reader.GetString(plotNoOrd),
                        ProjectName = reader.IsDBNull(projectNameOrd) ? "" : reader.GetString(projectNameOrd),
                        BuyerId = reader.IsDBNull(buyerIdOrd) ? (int?)null : reader.GetInt32(buyerIdOrd),
                        BuyerName = reader.IsDBNull(buyerNameOrd) ? "" : reader.GetString(buyerNameOrd),
                        BuyerType = reader.IsDBNull(buyerTypeOrd) ? "" : reader.GetString(buyerTypeOrd),
                        SellerId = reader.IsDBNull(sellerIdOrd) ? (int?)null : reader.GetInt32(sellerIdOrd),
                        SellerName = reader.IsDBNull(sellerNameOrd) ? "" : reader.GetString(sellerNameOrd),
                        SellerType = reader.IsDBNull(sellerTypeOrd) ? "" : reader.GetString(sellerTypeOrd),
                        Notes = notes
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading sales: {ex.Message}", ex);
            }

            return sales;
        }

        public static int InsertSale(int projectId, int plotId, int? buyerId, int? sellerId, 
            decimal salePrice, decimal? downPayment, DateTime saleDate, string status, string notes = "")
        {
            string query = @"
                INSERT INTO Sales (ProjectId, PlotId, PartyId, SellerId, SalePrice, DownPayment, SaleDate, Status, Notes, CreatedAt, UpdatedAt)
                OUTPUT INSERTED.SaleId
                VALUES (@ProjectId, @PlotId, @PartyId, @SellerId, @SalePrice, @DownPayment, @SaleDate, @Status, @Notes, GETDATE(), GETDATE())";

            string updatePlotQuery = @"
                UPDATE Plots
                SET OwnerId = @BuyerId,
                    Status = CASE WHEN @BuyerId IS NOT NULL THEN 'Sold' ELSE Status END,
                    UpdatedAt = GETDATE()
                WHERE PlotId = @PlotId";

            try
            {
                using var connection = DatabaseHelper.GetConnection();
                connection.Open();
                using var transaction = connection.BeginTransaction();
                
                try
                {
                    int saleId;
                    using (var command = new SqlCommand(query, connection, transaction))
                    {
                        command.Parameters.AddWithValue("@ProjectId", projectId);
                        command.Parameters.AddWithValue("@PlotId", plotId);
                        command.Parameters.AddWithValue("@PartyId", buyerId ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@SellerId", sellerId ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@SalePrice", salePrice);
                        command.Parameters.AddWithValue("@DownPayment", downPayment ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@SaleDate", saleDate.Date);
                        command.Parameters.AddWithValue("@Status", status ?? "Active");
                        command.Parameters.AddWithValue("@Notes", string.IsNullOrWhiteSpace(notes) ? (object)DBNull.Value : notes);

                        saleId = (int)command.ExecuteScalar();
                    }

                    if (buyerId.HasValue)
                    {
                        using var updateCommand = new SqlCommand(updatePlotQuery, connection, transaction);
                        updateCommand.Parameters.AddWithValue("@PlotId", plotId);
                        updateCommand.Parameters.AddWithValue("@BuyerId", buyerId.Value);
                        updateCommand.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    return saleId;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error inserting sale: {ex.Message}", ex);
            }
        }

        public static void UpdateSale(int saleId, int? buyerId, int? sellerId, 
            decimal salePrice, decimal? downPayment, DateTime saleDate, string status, string notes = "")
        {
            string getPlotQuery = "SELECT PlotId FROM Sales WHERE SaleId = @SaleId";
            
            string query = @"
                UPDATE Sales
                SET PartyId = @BuyerId,
                    SellerId = @SellerId,
                    SalePrice = @SalePrice,
                    DownPayment = @DownPayment,
                    SaleDate = @SaleDate,
                    Status = @Status,
                    Notes = @Notes,
                    UpdatedAt = GETDATE()
                WHERE SaleId = @SaleId";

            string updatePlotQuery = @"
                UPDATE Plots
                SET OwnerId = @BuyerId,
                    Status = CASE WHEN @BuyerId IS NOT NULL THEN 'Sold' ELSE Status END,
                    UpdatedAt = GETDATE()
                WHERE PlotId = @PlotId";

            try
            {
                using var connection = DatabaseHelper.GetConnection();
                connection.Open();
                using var transaction = connection.BeginTransaction();

                try
                {
                    int plotId = 0;
                    using (var getCommand = new SqlCommand(getPlotQuery, connection, transaction))
                    {
                        getCommand.Parameters.AddWithValue("@SaleId", saleId);
                        var result = getCommand.ExecuteScalar();
                        if (result != null)
                            plotId = (int)result;
                    }

                    using (var command = new SqlCommand(query, connection, transaction))
                    {
                        command.Parameters.AddWithValue("@SaleId", saleId);
                        command.Parameters.AddWithValue("@BuyerId", buyerId ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@SellerId", sellerId ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@SalePrice", salePrice);
                        command.Parameters.AddWithValue("@DownPayment", downPayment ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@SaleDate", saleDate.Date);
                        command.Parameters.AddWithValue("@Status", status ?? "Active");
                        command.Parameters.AddWithValue("@Notes", string.IsNullOrWhiteSpace(notes) ? (object)DBNull.Value : notes);
                        command.ExecuteNonQuery();
                    }

                    if (buyerId.HasValue && plotId > 0)
                    {
                        string checkLatestQuery = @"
                            SELECT COUNT(*) 
                            FROM Sales 
                            WHERE PlotId = @PlotId 
                              AND (SaleDate > @SaleDate OR (SaleDate = @SaleDate AND SaleId > @SaleId))";
                        
                        using (var checkCommand = new SqlCommand(checkLatestQuery, connection, transaction))
                        {
                            checkCommand.Parameters.AddWithValue("@PlotId", plotId);
                            checkCommand.Parameters.AddWithValue("@SaleDate", saleDate.Date);
                            checkCommand.Parameters.AddWithValue("@SaleId", saleId);
                            int newerSales = (int)checkCommand.ExecuteScalar();
                            
                            if (newerSales == 0)
                            {
                                using var updateCommand = new SqlCommand(updatePlotQuery, connection, transaction);
                                updateCommand.Parameters.AddWithValue("@PlotId", plotId);
                                updateCommand.Parameters.AddWithValue("@BuyerId", buyerId.Value);
                                updateCommand.ExecuteNonQuery();
                            }
                        }
                    }

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error updating sale: {ex.Message}", ex);
            }
        }


        public static void UpdateSaleStatus(int partyId, int plotId, string status)
        {
            try
            {
                using var connection = DatabaseHelper.GetConnection();
                connection.Open();
                
                string query = @"
                    UPDATE Sales 
                    SET Status = @Status 
                    WHERE PartyId = @PartyId AND PlotId = @PlotId";
                
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Status", status);
                command.Parameters.AddWithValue("@PartyId", partyId);
                command.Parameters.AddWithValue("@PlotId", plotId);
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error updating sale status: {ex.Message}", ex);
            }
        }

        public static void DeleteSale(int saleId)
        {
            string query = "DELETE FROM Sales WHERE SaleId = @SaleId";

            try
            {
                using var connection = DatabaseHelper.GetConnection();
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@SaleId", saleId);
                connection.Open();
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error deleting sale: {ex.Message}", ex);
            }
        }

        public static SaleInfo? GetSaleByPlotId(int plotId)
        {
            string query = @"
                SELECT TOP 1
                    s.SaleId,
                    s.SaleDate,
                    s.SalePrice,
                    s.DownPayment,
                    s.Status,
                    ISNULL(s.Notes, '') as Notes,
                    pl.PlotId,
                    pl.PlotNo,
                    pr.ProjectId,
                    pr.Name as ProjectName,
                    buyer.PartyId as BuyerId,
                    buyer.Name as BuyerName,
                    buyer.Type as BuyerType,
                    seller.PartyId as SellerId,
                    seller.Name as SellerName,
                    seller.Type as SellerType
                FROM Sales s
                INNER JOIN Plots pl ON s.PlotId = pl.PlotId
                INNER JOIN Projects pr ON s.ProjectId = pr.ProjectId
                LEFT JOIN Parties buyer ON s.PartyId = buyer.PartyId
                LEFT JOIN Parties seller ON s.SellerId = seller.PartyId
                WHERE pl.PlotId = @PlotId
                ORDER BY s.SaleDate DESC";

            try
            {
                using var connection = DatabaseHelper.GetConnection();
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@PlotId", plotId);
                connection.Open();

                using var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    int saleIdOrd = reader.GetOrdinal("SaleId");
                    int saleDateOrd = reader.GetOrdinal("SaleDate");
                    int salePriceOrd = reader.GetOrdinal("SalePrice");
                    int downPaymentOrd = reader.GetOrdinal("DownPayment");
                    int statusOrd = reader.GetOrdinal("Status");
                    int notesOrd = reader.GetOrdinal("Notes");
                    int plotIdOrd = reader.GetOrdinal("PlotId");
                    int plotNoOrd = reader.GetOrdinal("PlotNo");
                    int projectNameOrd = reader.GetOrdinal("ProjectName");
                    int buyerIdOrd = reader.GetOrdinal("BuyerId");
                    int buyerNameOrd = reader.GetOrdinal("BuyerName");
                    int buyerTypeOrd = reader.GetOrdinal("BuyerType");
                    int sellerIdOrd = reader.GetOrdinal("SellerId");
                    int sellerNameOrd = reader.GetOrdinal("SellerName");
                    int sellerTypeOrd = reader.GetOrdinal("SellerType");

                    string notes = reader.IsDBNull(notesOrd) ? "" : reader.GetString(notesOrd);

                    return new SaleInfo
                    {
                        SaleId = reader.GetInt32(saleIdOrd),
                        SaleDate = reader.GetDateTime(saleDateOrd),
                        SalePrice = reader.IsDBNull(salePriceOrd) ? 0 : reader.GetDecimal(salePriceOrd),
                        DownPayment = reader.IsDBNull(downPaymentOrd) ? 0 : reader.GetDecimal(downPaymentOrd),
                        Status = reader.IsDBNull(statusOrd) ? "Active" : reader.GetString(statusOrd),
                        PlotId = reader.GetInt32(plotIdOrd),
                        PlotNo = reader.IsDBNull(plotNoOrd) ? "" : reader.GetString(plotNoOrd),
                        ProjectName = reader.IsDBNull(projectNameOrd) ? "" : reader.GetString(projectNameOrd),
                        BuyerId = reader.IsDBNull(buyerIdOrd) ? (int?)null : reader.GetInt32(buyerIdOrd),
                        BuyerName = reader.IsDBNull(buyerNameOrd) ? "" : reader.GetString(buyerNameOrd),
                        BuyerType = reader.IsDBNull(buyerTypeOrd) ? "" : reader.GetString(buyerTypeOrd),
                        SellerId = reader.IsDBNull(sellerIdOrd) ? (int?)null : reader.GetInt32(sellerIdOrd),
                        SellerName = reader.IsDBNull(sellerNameOrd) ? "" : reader.GetString(sellerNameOrd),
                        SellerType = reader.IsDBNull(sellerTypeOrd) ? "" : reader.GetString(sellerTypeOrd),
                        Notes = notes
                    };
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading sale by plot: {ex.Message}", ex);
            }

            return null;
        }

        public class SaleInfo
        {
            public int SaleId { get; set; }
            public DateTime SaleDate { get; set; }
            public decimal SalePrice { get; set; }
            public decimal DownPayment { get; set; }
            public string Status { get; set; } = string.Empty;
            public int PlotId { get; set; }
            public string PlotNo { get; set; } = string.Empty;
            public string ProjectName { get; set; } = string.Empty;
            public int? BuyerId { get; set; }
            public string BuyerName { get; set; } = string.Empty;
            public string BuyerType { get; set; } = string.Empty;
            public int? SellerId { get; set; }
            public string SellerName { get; set; } = string.Empty;
            public string SellerType { get; set; } = string.Empty;
            public string Notes { get; set; } = string.Empty;
        }
    }
}

