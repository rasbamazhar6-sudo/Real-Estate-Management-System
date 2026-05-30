using System;
using Microsoft.Data.SqlClient;

namespace Project.Data
{
    public class PartyManagementDataAccess
    {
        public static int InsertParty(string name, string type = "Buyer", string? cnic = null, 
            string? contactPhone = null, string? contactEmail = null, string? address = null, string status = "Active")
        {
            string query = @"
                INSERT INTO [dbo].[Parties] 
                    ([Type], [Name], [CNIC], [ContactPhone], [ContactEmail], [Address], [Status], [CreatedAt], [UpdatedAt])
                OUTPUT INSERTED.PartyId
                VALUES 
                    (@Type, @Name, @CNIC, @ContactPhone, @ContactEmail, @Address, @Status, GETDATE(), GETDATE())";

            try
            {
                using var connection = DatabaseHelper.GetConnection();
                using var command = new SqlCommand(query, connection);
                
                command.Parameters.AddWithValue("@Type", type ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Name", name ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@CNIC", string.IsNullOrWhiteSpace(cnic) ? (object)DBNull.Value : cnic);
                command.Parameters.AddWithValue("@ContactPhone", string.IsNullOrWhiteSpace(contactPhone) ? (object)DBNull.Value : contactPhone);
                command.Parameters.AddWithValue("@ContactEmail", string.IsNullOrWhiteSpace(contactEmail) ? (object)DBNull.Value : contactEmail);
                command.Parameters.AddWithValue("@Address", string.IsNullOrWhiteSpace(address) ? (object)DBNull.Value : address);
                command.Parameters.AddWithValue("@Status", status ?? "Active");

                connection.Open();
                return (int)command.ExecuteScalar();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error inserting party: {ex.Message}", ex);
            }
        }

        public static void UpdateParty(int partyId, string name, string type, string? cnic = null,
            string? contactPhone = null, string? contactEmail = null, string? address = null, string status = "Active")
        {
            string query = @"
                UPDATE [dbo].[Parties]
                SET [Type] = @Type,
                    [Name] = @Name,
                    [CNIC] = @CNIC,
                    [ContactPhone] = @ContactPhone,
                    [ContactEmail] = @ContactEmail,
                    [Address] = @Address,
                    [Status] = @Status,
                    [UpdatedAt] = GETDATE()
                WHERE [PartyId] = @PartyId";

            try
            {
                using var connection = DatabaseHelper.GetConnection();
                using var command = new SqlCommand(query, connection);
                
                command.Parameters.AddWithValue("@PartyId", partyId);
                command.Parameters.AddWithValue("@Type", type ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Name", name ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@CNIC", string.IsNullOrWhiteSpace(cnic) ? (object)DBNull.Value : cnic);
                command.Parameters.AddWithValue("@ContactPhone", string.IsNullOrWhiteSpace(contactPhone) ? (object)DBNull.Value : contactPhone);
                command.Parameters.AddWithValue("@ContactEmail", string.IsNullOrWhiteSpace(contactEmail) ? (object)DBNull.Value : contactEmail);
                command.Parameters.AddWithValue("@Address", string.IsNullOrWhiteSpace(address) ? (object)DBNull.Value : address);
                command.Parameters.AddWithValue("@Status", status ?? "Active");

                connection.Open();
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error updating party: {ex.Message}", ex);
            }
        }

        public static void DeleteParty(int partyId)
        {
            string checkDependenciesQuery = @"
                DECLARE @RefCount INT = 0
                
                -- Check Sales table
                SELECT @RefCount = @RefCount + COUNT(*) 
                FROM [dbo].[Sales] 
                WHERE [PartyId] = @PartyId OR [SellerId] = @PartyId
                
                -- Check Transactions table
                SELECT @RefCount = @RefCount + COUNT(*) 
                FROM [dbo].[Transactions] 
                WHERE [PartyId] = @PartyId
                
                -- Check Plots table
                SELECT @RefCount = @RefCount + COUNT(*) 
                FROM [dbo].[Plots] 
                WHERE [OwnerId] = @PartyId
                
                SELECT @RefCount AS RefCount";


            string deleteQuery = "DELETE FROM [dbo].[Parties] WHERE [PartyId] = @PartyId";

            try
            {
                using var connection = DatabaseHelper.GetConnection();
                connection.Open();

                using (var checkCommand = new SqlCommand(checkDependenciesQuery, connection))
                {
                    checkCommand.Parameters.AddWithValue("@PartyId", partyId);
                    var refCount = (int)checkCommand.ExecuteScalar();
                    
                    if (refCount > 0)
                    {
                        throw new Exception($"Cannot delete this customer because they are referenced in {refCount} record(s) in Sales, Transactions, or Plots tables. Please delete or update those records first.");
                    }
                }

                using var deleteCommand = new SqlCommand(deleteQuery, connection);
                deleteCommand.Parameters.AddWithValue("@PartyId", partyId);
                int rowsAffected = deleteCommand.ExecuteNonQuery();
                
                if (rowsAffected == 0)
                {
                    throw new Exception("Customer not found or already deleted.");
                }
            }
            catch (SqlException sqlEx)
            {
                if (sqlEx.Number == 547)
                {
                    throw new Exception("Cannot delete this customer because they are referenced in other records (Sales, Transactions, or Plots). Please delete or update those records first.");
                }
                throw new Exception($"Error deleting party: {sqlEx.Message}", sqlEx);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Cannot delete") || ex.Message.Contains("not found"))
                {
                    throw;
                }
                throw new Exception($"Error deleting party: {ex.Message}", ex);
            }
        }

        public static void DeleteSampleData()
        {
            string query = @"
                DELETE FROM [dbo].[Parties]
                WHERE [Name] IN ('John Smith', 'Sarah Johnson', 'Michael Brown', 'Emily Davis')
                   OR ([CNIC] IN ('12345-1234567-1', '23456-2345678-2', '34567-3456789-3', '45678-4567890-4')
                       AND [ContactPhone] IN ('+1234567890', '+1234567891', '+1234567892', '+1234567893'))";

            try
            {
                using var connection = DatabaseHelper.GetConnection();
                using var command = new SqlCommand(query, connection);
                connection.Open();
                int rowsAffected = command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error deleting sample data: {ex.Message}", ex);
            }
        }
    }
}

