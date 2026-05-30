using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;

namespace Project.Data
{
    public class TransactionDataAccess
    {
        public static int InsertTransaction(int partyId, string transactionType, decimal amount, 
            DateTime transactionDate, string? description = null, int? saleId = null, int? installmentId = null)
        {
            // If it's a payment (Credit) and linked to a sale, use the stored procedure
            if (transactionType.Equals("Credit", StringComparison.OrdinalIgnoreCase) && saleId.HasValue)
            {
                return RecordPayment(saleId.Value, amount, installmentId);
            }

            // Direct insert for other transaction types (like Debit)
            string query = @"
                INSERT INTO [dbo].[Transactions] 
                    ([Date], [Amount], [Type], [PartyId], [SaleId], [InstallmentId], [Description], [CreatedAt], [UpdatedAt])
                OUTPUT INSERTED.TransactionId
                VALUES 
                    (@Date, @Amount, @Type, @PartyId, @SaleId, @InstallmentId, @Description, GETDATE(), GETDATE())";

            try
            {
                using var connection = DatabaseHelper.GetConnection();
                using var command = new SqlCommand(query, connection);
                
                command.Parameters.AddWithValue("@Date", transactionDate.Date);
                command.Parameters.AddWithValue("@Amount", amount);
                command.Parameters.AddWithValue("@Type", transactionType ?? "Debit");
                command.Parameters.AddWithValue("@PartyId", partyId);
                command.Parameters.AddWithValue("@SaleId", saleId ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@InstallmentId", installmentId ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Description", string.IsNullOrWhiteSpace(description) ? (object)DBNull.Value : description);

                connection.Open();
                return (int)command.ExecuteScalar();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error inserting transaction: {ex.Message}", ex);
            }
        }

        public static int RecordPayment(int saleId, decimal amount, int? installmentId = null)
        {
            try
            {
                using var connection = DatabaseHelper.GetConnection();
                using var command = new SqlCommand("sp_RecordPayment", connection);
                command.CommandType = System.Data.CommandType.StoredProcedure;
                
                command.Parameters.AddWithValue("@SaleId", saleId);
                command.Parameters.AddWithValue("@Amount", amount);
                command.Parameters.AddWithValue("@InstallmentId", installmentId ?? (object)DBNull.Value);

                connection.Open();
                
                // sp_RecordPayment doesn't currently return the TransactionId in the script provided, 
                // but we can query the latest transaction for this sale if needed, 
                // or update the SP to return it. For now, we'll return 1 on success.
                command.ExecuteNonQuery();
                
                // Return the latest transaction ID for this sale
                using var getIdCmd = new SqlCommand("SELECT TOP 1 TransactionId FROM Transactions WHERE SaleId = @SaleId ORDER BY TransactionId DESC", connection);
                getIdCmd.Parameters.AddWithValue("@SaleId", saleId);
                var result = getIdCmd.ExecuteScalar();
                return result != null ? (int)result : 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error recording payment via SP: {ex.Message}", ex);
            }
        }

        public static List<TransactionInfo> GetAllTransactions()
        {
            var transactions = new List<TransactionInfo>();

            string query = @"
                SELECT 
                    t.[TransactionId],
                    t.[Date],
                    t.[Amount],
                    t.[Type],
                    t.[Description],
                    ISNULL(p.[Name], 'Unknown Customer') as CustomerName
                FROM [dbo].[Transactions] t
                LEFT JOIN [dbo].[Parties] p ON t.[PartyId] = p.[PartyId]
                ORDER BY t.[Date] DESC, t.[TransactionId] DESC";

            try
            {
                using var connection = DatabaseHelper.GetConnection();
                using var command = new SqlCommand(query, connection);
                connection.Open();

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    int transactionIdOrd = reader.GetOrdinal("TransactionId");
                    int dateOrd = reader.GetOrdinal("Date");
                    int amountOrd = reader.GetOrdinal("Amount");
                    int typeOrd = reader.GetOrdinal("Type");
                    int descriptionOrd = reader.GetOrdinal("Description");
                    int customerNameOrd = reader.GetOrdinal("CustomerName");

                    transactions.Add(new TransactionInfo
                    {
                        TransactionId = reader.GetInt32(transactionIdOrd),
                        TransactionDate = reader.IsDBNull(dateOrd) ? DateTime.Now : reader.GetDateTime(dateOrd),
                        Amount = reader.IsDBNull(amountOrd) ? 0 : reader.GetDecimal(amountOrd),
                        TransactionType = reader.IsDBNull(typeOrd) ? "" : reader.GetString(typeOrd),
                        Description = reader.IsDBNull(descriptionOrd) ? "" : reader.GetString(descriptionOrd),
                        CustomerName = reader.IsDBNull(customerNameOrd) ? "" : reader.GetString(customerNameOrd)
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading transactions: {ex.Message}", ex);
            }

            return transactions;
        }

        public static List<TransactionInfo> GetTransactionsByPartyId(int partyId)
        {
            var transactions = new List<TransactionInfo>();

            string query = @"
                SELECT 
                    t.[TransactionId],
                    t.[Date],
                    t.[Amount],
                    t.[Type],
                    t.[Description],
                    t.[SaleId],
                    t.[InstallmentId],
                    p.[PartyId],
                    p.[Name] as CustomerName
                FROM [dbo].[Transactions] t
                LEFT JOIN [dbo].[Parties] p ON t.[PartyId] = p.[PartyId]
                WHERE t.[PartyId] = @PartyId
                ORDER BY t.[Date] DESC, t.[TransactionId] DESC";

            try
            {
                using var connection = DatabaseHelper.GetConnection();
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@PartyId", partyId);
                connection.Open();

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    int transactionIdOrd = reader.GetOrdinal("TransactionId");
                    int dateOrd = reader.GetOrdinal("Date");
                    int amountOrd = reader.GetOrdinal("Amount");
                    int typeOrd = reader.GetOrdinal("Type");
                    int descriptionOrd = reader.GetOrdinal("Description");
                    int customerNameOrd = reader.GetOrdinal("CustomerName");

                    transactions.Add(new TransactionInfo
                    {
                        TransactionId = reader.GetInt32(transactionIdOrd),
                        TransactionDate = reader.IsDBNull(dateOrd) ? DateTime.Now : reader.GetDateTime(dateOrd),
                        Amount = reader.IsDBNull(amountOrd) ? 0 : reader.GetDecimal(amountOrd),
                        TransactionType = reader.IsDBNull(typeOrd) ? "" : reader.GetString(typeOrd),
                        Description = reader.IsDBNull(descriptionOrd) ? "" : reader.GetString(descriptionOrd),
                        CustomerName = reader.IsDBNull(customerNameOrd) ? "" : reader.GetString(customerNameOrd)
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading transactions: {ex.Message}", ex);
            }

            return transactions;
        }

        public class TransactionInfo
        {
            public int TransactionId { get; set; }
            public DateTime TransactionDate { get; set; }
            public decimal Amount { get; set; }
            public string TransactionType { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string CustomerName { get; set; } = string.Empty;
        }
    }
}

