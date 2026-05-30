using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.Data.SqlClient;
using Project.Pages;

namespace Project.Data
{
    public class PaymentPlanDataAccess
    {
        // =========================
        // GET ALL
        // =========================
        public static List<Page48Page.PaymentPlan> GetAllPaymentPlans()
        {
            var plans = new List<Page48Page.PaymentPlan>();

            using var connection = DatabaseHelper.GetConnection();
            connection.Open();

            string query = @"
                SELECT 
                    pp.PaymentPlanId,
                    pp.TotalAmount,
                    pp.DownPayment,
                    pp.NumberOfInstallments,
                    pp.InstallmentAmount,
                    pp.StartDate,
                    pp.PlanType,
                    pp.Status,
                    pp.OverdueReminder,
                    pp.UpcomingReminder,
                    s.Status as SaleStatus,
                    p.PartyId as BuyerId,
                    p.Name as BuyerName,
                    pl.PlotId,
                    pl.PlotNo
                FROM PaymentPlans pp
                INNER JOIN Sales s ON pp.SaleId = s.SaleId
                INNER JOIN Parties p ON s.PartyId = p.PartyId
                INNER JOIN Plots pl ON s.PlotId = pl.PlotId
                ORDER BY pp.PaymentPlanId DESC";

            using var command = new SqlCommand(query, connection);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                plans.Add(new Page48Page.PaymentPlan
                {
                    PlanId = "PLN" + reader.GetInt32(0).ToString("D3"),
                    PaymentPlanId = reader.GetInt32(0),
                    BuyerId = reader.GetInt32(11).ToString(),
                    BuyerName = reader.GetString(12),
                    PlotId = reader.GetInt32(13).ToString(),
                    PlotNo = reader.GetString(14),
                    PlanType = reader.IsDBNull(6) ? "Monthly" : reader.GetString(6),
                    Status = reader.IsDBNull(7) ? "Active" : reader.GetString(7),
                    TotalAmount = reader.GetDecimal(1),
                    DownPayment = reader.GetDecimal(2),
                    InstallmentCount = reader.GetInt32(3),
                    InstallmentAmount = reader.GetDecimal(4),
                    StartDate = reader.GetDateTime(5),
                    OverdueReminder = !reader.IsDBNull(8) && reader.GetBoolean(8),
                    UpcomingReminder = !reader.IsDBNull(9) && reader.GetBoolean(9)
                });
            }

            return plans;
        }

        // =========================
        // INSERT PLAN
        // =========================
        public static int InsertPaymentPlan(int saleId, decimal totalAmount, decimal downPayment,
            int numberOfInstallments, decimal installmentAmount, DateTime startDate,
            string planType = "Monthly", string status = "Active",
            bool overdueReminder = true, bool upcomingReminder = true)
        {
            using var connection = DatabaseHelper.GetConnection();
            connection.Open();

            using var transaction = connection.BeginTransaction();

            try
            {
                string query = @"
                    INSERT INTO PaymentPlans 
                    (SaleId, TotalAmount, DownPayment, NumberOfInstallments, InstallmentAmount,
                     StartDate, PlanType, Status, OverdueReminder, UpcomingReminder)
                    OUTPUT INSERTED.PaymentPlanId
                    VALUES
                    (@SaleId, @TotalAmount, @DownPayment, @NumberOfInstallments, @InstallmentAmount,
                     @StartDate, @PlanType, @Status, @OverdueReminder, @UpcomingReminder)";

                using var command = new SqlCommand(query, connection, transaction);

                command.Parameters.AddWithValue("@SaleId", saleId);
                command.Parameters.AddWithValue("@TotalAmount", totalAmount);
                command.Parameters.AddWithValue("@DownPayment", downPayment);
                command.Parameters.AddWithValue("@NumberOfInstallments", numberOfInstallments);
                command.Parameters.AddWithValue("@InstallmentAmount", installmentAmount);
                command.Parameters.AddWithValue("@StartDate", startDate.Date);
                command.Parameters.AddWithValue("@PlanType", planType);
                command.Parameters.AddWithValue("@Status", status);
                command.Parameters.AddWithValue("@OverdueReminder", overdueReminder);
                command.Parameters.AddWithValue("@UpcomingReminder", upcomingReminder);

                int paymentPlanId = (int)command.ExecuteScalar();

                string insertInstallment = @"
                    INSERT INTO Installments (PaymentPlanId, InstallmentNo, DueDate, Amount, Status)
                    VALUES (@PaymentPlanId, @InstallmentNo, @DueDate, @Amount, 'Due')";

                for (int i = 1; i <= numberOfInstallments; i++)
                {
                    using var cmd = new SqlCommand(insertInstallment, connection, transaction);
                    cmd.Parameters.AddWithValue("@PaymentPlanId", paymentPlanId);
                    cmd.Parameters.AddWithValue("@InstallmentNo", i);
                    cmd.Parameters.AddWithValue("@DueDate", startDate.AddMonths(i - 1).Date);
                    cmd.Parameters.AddWithValue("@Amount", installmentAmount);
                    cmd.ExecuteNonQuery();
                }

                transaction.Commit();
                return paymentPlanId;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        // =========================
        // UPDATE PLAN
        // =========================
        public static void UpdatePaymentPlan(int paymentPlanId, decimal totalAmount, decimal downPayment,
            int numberOfInstallments, decimal installmentAmount, DateTime startDate,
            string planType = "Monthly", string status = "Active",
            bool overdueReminder = true, bool upcomingReminder = true)
        {
            using var connection = DatabaseHelper.GetConnection();
            connection.Open();

            using var transaction = connection.BeginTransaction();

            try
            {
                string query = @"
                    UPDATE PaymentPlans
                    SET TotalAmount = @TotalAmount,
                        DownPayment = @DownPayment,
                        NumberOfInstallments = @NumberOfInstallments,
                        InstallmentAmount = @InstallmentAmount,
                        StartDate = @StartDate,
                        PlanType = @PlanType,
                        Status = @Status,
                        OverdueReminder = @OverdueReminder,
                        UpcomingReminder = @UpcomingReminder
                    WHERE PaymentPlanId = @PaymentPlanId";

                using var command = new SqlCommand(query, connection, transaction);

                command.Parameters.AddWithValue("@PaymentPlanId", paymentPlanId);
                command.Parameters.AddWithValue("@TotalAmount", totalAmount);
                command.Parameters.AddWithValue("@DownPayment", downPayment);
                command.Parameters.AddWithValue("@NumberOfInstallments", numberOfInstallments);
                command.Parameters.AddWithValue("@InstallmentAmount", installmentAmount);
                command.Parameters.AddWithValue("@StartDate", startDate.Date);
                command.Parameters.AddWithValue("@PlanType", planType);
                command.Parameters.AddWithValue("@Status", status);
                command.Parameters.AddWithValue("@OverdueReminder", overdueReminder);
                command.Parameters.AddWithValue("@UpcomingReminder", upcomingReminder);

                command.ExecuteNonQuery();

                SyncInstallments(paymentPlanId, numberOfInstallments, installmentAmount, startDate, connection, transaction);

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        // =========================
        // SYNC INSTALLMENTS
        // =========================
        private static void SyncInstallments(int paymentPlanId, int newCount, decimal newAmount,
            DateTime startDate, SqlConnection connection, SqlTransaction transaction)
        {
            var existing = new List<(int Id, int No, string Status)>();

            string fetch = @"SELECT InstallmentId, InstallmentNo, Status FROM Installments WHERE PaymentPlanId=@Id";

            using (var cmd = new SqlCommand(fetch, connection, transaction))
            {
                cmd.Parameters.AddWithValue("@Id", paymentPlanId);
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    existing.Add((
                        reader.GetInt32(0),
                        reader.GetInt32(1),
                        reader.IsDBNull(2) ? "Due" : reader.GetString(2)
                    ));
                }
            }

            for (int i = 1; i <= newCount; i++)
            {
                var e = existing.FirstOrDefault(x => x.No == i);

                if (e.Id != 0)
                {
                    if (e.Status != "Paid")
                    {
                        string update = @"UPDATE Installments SET Amount=@Amount, DueDate=@Date WHERE InstallmentId=@Id";

                        using var cmd = new SqlCommand(update, connection, transaction);
                        cmd.Parameters.AddWithValue("@Amount", newAmount);
                        cmd.Parameters.AddWithValue("@Date", startDate.AddMonths(i - 1));
                        cmd.Parameters.AddWithValue("@Id", e.Id);
                        cmd.ExecuteNonQuery();
                    }
                }
                else
                {
                    string insert = @"INSERT INTO Installments (PaymentPlanId, InstallmentNo, DueDate, Amount, Status)
                                      VALUES (@PlanId, @No, @Date, @Amount, 'Due')";

                    using var cmd = new SqlCommand(insert, connection, transaction);
                    cmd.Parameters.AddWithValue("@PlanId", paymentPlanId);
                    cmd.Parameters.AddWithValue("@No", i);
                    cmd.Parameters.AddWithValue("@Date", startDate.AddMonths(i - 1));
                    cmd.Parameters.AddWithValue("@Amount", newAmount);
                    cmd.ExecuteNonQuery();
                }
            }

            string delete = @"DELETE FROM Installments 
                              WHERE PaymentPlanId=@Id AND InstallmentNo>@Count AND Status!='Paid'";

            using var del = new SqlCommand(delete, connection, transaction);
            del.Parameters.AddWithValue("@Id", paymentPlanId);
            del.Parameters.AddWithValue("@Count", newCount);
            del.ExecuteNonQuery();
        }

        // =========================
        // DELETE PLAN
        // =========================
        public static void DeletePaymentPlan(int paymentPlanId)
        {
            using var connection = DatabaseHelper.GetConnection();
            connection.Open();

            using var transaction = connection.BeginTransaction();

            try
            {
                string deleteInstallments = "DELETE FROM Installments WHERE PaymentPlanId=@Id";
                using (var cmd = new SqlCommand(deleteInstallments, connection, transaction))
                {
                    cmd.Parameters.AddWithValue("@Id", paymentPlanId);
                    cmd.ExecuteNonQuery();
                }

                string deletePlan = "DELETE FROM PaymentPlans WHERE PaymentPlanId=@Id";
                using (var cmd = new SqlCommand(deletePlan, connection, transaction))
                {
                    cmd.Parameters.AddWithValue("@Id", paymentPlanId);
                    cmd.ExecuteNonQuery();
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }


        public static int GetOrCreateSale(int buyerId, int plotId, decimal salePrice, decimal downPayment)
        {
            using var connection = DatabaseHelper.GetConnection();
            connection.Open();

            string findQuery = @"
        SELECT TOP 1 SaleId 
        FROM Sales 
        WHERE PartyId = @BuyerId AND PlotId = @PlotId 
        ORDER BY SaleId DESC";

            using var findCommand = new SqlCommand(findQuery, connection);
            findCommand.Parameters.AddWithValue("@BuyerId", buyerId);
            findCommand.Parameters.AddWithValue("@PlotId", plotId);

            var existing = findCommand.ExecuteScalar();

            if (existing != null)
                return (int)existing;

            // get project
            string projectQuery = "SELECT ProjectId FROM Plots WHERE PlotId=@PlotId";
            using var projCmd = new SqlCommand(projectQuery, connection);
            projCmd.Parameters.AddWithValue("@PlotId", plotId);

            var projectId = projCmd.ExecuteScalar();
            if (projectId == null)
                throw new Exception("Plot not found");

            using var transaction = connection.BeginTransaction();

            try
            {
                string insertQuery = @"
            INSERT INTO Sales 
            (ProjectId, PlotId, PartyId, SalePrice, DownPayment, SaleDate, Status)
            OUTPUT INSERTED.SaleId
            VALUES 
            (@ProjectId, @PlotId, @BuyerId, @SalePrice, @DownPayment, @SaleDate, 'Active')";

                using var cmd = new SqlCommand(insertQuery, connection, transaction);

                cmd.Parameters.AddWithValue("@ProjectId", projectId);
                cmd.Parameters.AddWithValue("@PlotId", plotId);
                cmd.Parameters.AddWithValue("@BuyerId", buyerId);
                cmd.Parameters.AddWithValue("@SalePrice", salePrice);
                cmd.Parameters.AddWithValue("@DownPayment", downPayment);
                cmd.Parameters.AddWithValue("@SaleDate", DateTime.Now.Date);

                int saleId = (int)cmd.ExecuteScalar();

                transaction.Commit();
                return saleId;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
        public static void SavePaymentPlanComplete(int buyerId, int plotId, decimal totalAmount, decimal downPayment,
            int installments, decimal installmentAmount, DateTime startDate, string planType, string status,
            bool overdueReminder, bool upcomingReminder)
        {
            using var connection = DatabaseHelper.GetConnection();
            connection.Open();

            using var transaction = connection.BeginTransaction();

            try
            {
                // 1. Get or Create Sale (within transaction)
                int saleId = 0;
                
                string findQuery = @"
                    SELECT TOP 1 SaleId FROM Sales 
                    WHERE PartyId = @BuyerId AND PlotId = @PlotId 
                    ORDER BY SaleId DESC";

                using (var findCommand = new SqlCommand(findQuery, connection, transaction))
                {
                    findCommand.Parameters.AddWithValue("@BuyerId", buyerId);
                    findCommand.Parameters.AddWithValue("@PlotId", plotId);
                    var result = findCommand.ExecuteScalar();
                    if (result != null)
                    {
                        saleId = (int)result;
                    }
                }

                if (saleId == 0)
                {
                    // Get project
                    string projectQuery = "SELECT ProjectId FROM Plots WHERE PlotId=@PlotId";
                    int projectId = 0;
                    using (var projCmd = new SqlCommand(projectQuery, connection, transaction))
                    {
                        projCmd.Parameters.AddWithValue("@PlotId", plotId);
                        var projResult = projCmd.ExecuteScalar();
                        if (projResult == null) throw new Exception("Plot not found");
                        projectId = (int)projResult;
                    }

                    string insertSale = @"
                        INSERT INTO Sales (ProjectId, PlotId, PartyId, SalePrice, DownPayment, SaleDate, Status)
                        OUTPUT INSERTED.SaleId
                        VALUES (@ProjectId, @PlotId, @BuyerId, @SalePrice, @DownPayment, @SaleDate, 'Active')";

                    using var saleCmd = new SqlCommand(insertSale, connection, transaction);
                    saleCmd.Parameters.AddWithValue("@ProjectId", projectId);
                    saleCmd.Parameters.AddWithValue("@PlotId", plotId);
                    saleCmd.Parameters.AddWithValue("@BuyerId", buyerId);
                    saleCmd.Parameters.AddWithValue("@SalePrice", totalAmount);
                    saleCmd.Parameters.AddWithValue("@DownPayment", downPayment);
                    saleCmd.Parameters.AddWithValue("@SaleDate", DateTime.Now.Date);
                    saleId = (int)saleCmd.ExecuteScalar();
                }

                // 2. Insert Payment Plan
                string insertPlan = @"
                    INSERT INTO PaymentPlans 
                    (SaleId, TotalAmount, DownPayment, NumberOfInstallments, InstallmentAmount,
                     StartDate, PlanType, Status, OverdueReminder, UpcomingReminder)
                    OUTPUT INSERTED.PaymentPlanId
                    VALUES
                    (@SaleId, @TotalAmount, @DownPayment, @NumberOfInstallments, @InstallmentAmount,
                     @StartDate, @PlanType, @Status, @OverdueReminder, @UpcomingReminder)";

                int paymentPlanId = 0;
                using (var planCmd = new SqlCommand(insertPlan, connection, transaction))
                {
                    planCmd.Parameters.AddWithValue("@SaleId", saleId);
                    planCmd.Parameters.AddWithValue("@TotalAmount", totalAmount);
                    planCmd.Parameters.AddWithValue("@DownPayment", downPayment);
                    planCmd.Parameters.AddWithValue("@NumberOfInstallments", installments);
                    planCmd.Parameters.AddWithValue("@InstallmentAmount", installmentAmount);
                    planCmd.Parameters.AddWithValue("@StartDate", startDate.Date);
                    planCmd.Parameters.AddWithValue("@PlanType", planType);
                    planCmd.Parameters.AddWithValue("@Status", status);
                    planCmd.Parameters.AddWithValue("@OverdueReminder", overdueReminder);
                    planCmd.Parameters.AddWithValue("@UpcomingReminder", upcomingReminder);
                    paymentPlanId = (int)planCmd.ExecuteScalar();
                }

                // 3. Insert Installments
                string insertInstallment = @"
                    INSERT INTO Installments (PaymentPlanId, InstallmentNo, DueDate, Amount, Status)
                    VALUES (@PaymentPlanId, @InstallmentNo, @DueDate, @Amount, 'Due')";

                for (int i = 1; i <= installments; i++)
                {
                    using var instCmd = new SqlCommand(insertInstallment, connection, transaction);
                    instCmd.Parameters.AddWithValue("@PaymentPlanId", paymentPlanId);
                    instCmd.Parameters.AddWithValue("@InstallmentNo", i);
                    instCmd.Parameters.AddWithValue("@DueDate", startDate.AddMonths(i - 1).Date);
                    instCmd.Parameters.AddWithValue("@Amount", installmentAmount);
                    instCmd.ExecuteNonQuery();
                }

                // 4. Update Sale Status
                string updateSale = "UPDATE Sales SET Status = @Status WHERE SaleId = @SaleId";
                using (var updateCmd = new SqlCommand(updateSale, connection, transaction))
                {
                    updateCmd.Parameters.AddWithValue("@Status", status);
                    updateCmd.Parameters.AddWithValue("@SaleId", saleId);
                    updateCmd.ExecuteNonQuery();
                }

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception($"Error in atomic payment plan creation: {ex.Message}", ex);
            }
        }

        public static void UpdatePaymentPlanComplete(int paymentPlanId, int buyerId, int plotId, decimal totalAmount, decimal downPayment,
            int installments, decimal installmentAmount, DateTime startDate, string planType, string status,
            bool overdueReminder, bool upcomingReminder)
        {
            using var connection = DatabaseHelper.GetConnection();
            connection.Open();

            using var transaction = connection.BeginTransaction();

            try
            {
                // 1. Update Payment Plan
                string query = @"
                    UPDATE PaymentPlans
                    SET TotalAmount = @TotalAmount,
                        DownPayment = @DownPayment,
                        NumberOfInstallments = @NumberOfInstallments,
                        InstallmentAmount = @InstallmentAmount,
                        StartDate = @StartDate,
                        PlanType = @PlanType,
                        Status = @Status,
                        OverdueReminder = @OverdueReminder,
                        UpcomingReminder = @UpcomingReminder
                    WHERE PaymentPlanId = @PaymentPlanId";

                using (var command = new SqlCommand(query, connection, transaction))
                {
                    command.Parameters.AddWithValue("@PaymentPlanId", paymentPlanId);
                    command.Parameters.AddWithValue("@TotalAmount", totalAmount);
                    command.Parameters.AddWithValue("@DownPayment", downPayment);
                    command.Parameters.AddWithValue("@NumberOfInstallments", installments);
                    command.Parameters.AddWithValue("@InstallmentAmount", installmentAmount);
                    command.Parameters.AddWithValue("@StartDate", startDate.Date);
                    command.Parameters.AddWithValue("@PlanType", planType);
                    command.Parameters.AddWithValue("@Status", status);
                    command.Parameters.AddWithValue("@OverdueReminder", overdueReminder);
                    command.Parameters.AddWithValue("@UpcomingReminder", upcomingReminder);
                    command.ExecuteNonQuery();
                }

                // 2. Sync Installments
                SyncInstallments(paymentPlanId, installments, installmentAmount, startDate, connection, transaction);

                // 3. Update Sale Status
                string updateSale = "UPDATE Sales SET Status = @Status WHERE PartyId = @PartyId AND PlotId = @PlotId";
                using (var updateCmd = new SqlCommand(updateSale, connection, transaction))
                {
                    updateCmd.Parameters.AddWithValue("@Status", status);
                    updateCmd.Parameters.AddWithValue("@PartyId", buyerId);
                    updateCmd.Parameters.AddWithValue("@PlotId", plotId);
                    updateCmd.ExecuteNonQuery();
                }

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception($"Error in atomic payment plan update: {ex.Message}", ex);
            }
        }
    }
}