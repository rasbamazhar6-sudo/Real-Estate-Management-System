using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;

namespace Project.Data
{
    public class LedgerDataAccess
    {
        public static List<LedgerEntryInfo> GetLedgerEntriesByPartyId(int partyId)
        {
            var entries = new List<LedgerEntryInfo>();

            string query = @"
                SELECT 
                    t.[TransactionId],
                    t.[Date],
                    t.[Type],
                    t.[Amount],
                    t.[Description],
                    pl.PlotNo,
                    pr.Name as ProjectName,
                    DATEDIFF(DAY, t.[Date], @AsOfDate) as DaysAged
                FROM [dbo].[Transactions] t
                LEFT JOIN [dbo].[Sales] s ON t.[SaleId] = s.[SaleId]
                LEFT JOIN [dbo].[Plots] pl ON s.[PlotId] = pl.[PlotId]
                LEFT JOIN [dbo].[Projects] pr ON pl.[ProjectId] = pr.[ProjectId]
                WHERE t.[PartyId] = @PartyId
                ORDER BY t.[Date] ASC, t.[TransactionId] ASC";

            try
            {
                using var connection = DatabaseHelper.GetConnection();
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@PartyId", partyId);
                command.Parameters.AddWithValue("@AsOfDate", DateTime.Now);
                connection.Open();

                using var reader = command.ExecuteReader();
                decimal runningBalance = 0;

                while (reader.Read())
                {
                    int transactionIdOrd = reader.GetOrdinal("TransactionId");
                    int dateOrd = reader.GetOrdinal("Date");
                    int typeOrd = reader.GetOrdinal("Type");
                    int amountOrd = reader.GetOrdinal("Amount");
                    int descriptionOrd = reader.GetOrdinal("Description");
                    int plotNoOrd = reader.GetOrdinal("PlotNo");
                    int projectNameOrd = reader.GetOrdinal("ProjectName");
                    int daysAgedOrd = reader.GetOrdinal("DaysAged");

                    DateTime transactionDate = reader.IsDBNull(dateOrd) ? DateTime.Now : reader.GetDateTime(dateOrd);
                    string transactionType = reader.IsDBNull(typeOrd) ? "" : reader.GetString(typeOrd);
                    decimal amount = reader.IsDBNull(amountOrd) ? 0 : reader.GetDecimal(amountOrd);
                    string description = reader.IsDBNull(descriptionOrd) ? "" : reader.GetString(descriptionOrd);
                    string plotNo = reader.IsDBNull(plotNoOrd) ? "" : reader.GetString(plotNoOrd);
                    string projectName = reader.IsDBNull(projectNameOrd) ? "" : reader.GetString(projectNameOrd);
                    int daysAged = reader.IsDBNull(daysAgedOrd) ? 0 : reader.GetInt32(daysAgedOrd);

                    // Build a better description if plot/project info is available
                    string fullDescription = description;
                    if (!string.IsNullOrEmpty(plotNo))
                    {
                        fullDescription = $"{description} (Plot: {plotNo}, {projectName})".Trim();
                    }

                    string reference = ExtractReference(description);

                    decimal debit = 0;
                    decimal credit = 0;
                    
                    if (transactionType.Equals("Debit", StringComparison.OrdinalIgnoreCase))
                    {
                        debit = amount;
                        runningBalance += amount;
                    }
                    else if (transactionType.Equals("Credit", StringComparison.OrdinalIgnoreCase))
                    {
                        credit = amount;
                        runningBalance -= amount;
                    }

                    entries.Add(new LedgerEntryInfo
                    {
                        TransactionId = reader.GetInt32(transactionIdOrd),
                        Date = transactionDate,
                        Description = fullDescription,
                        Reference = reference,
                        Debit = debit,
                        Credit = credit,
                        Balance = runningBalance,
                        Aging = $"{daysAged} days"
                    });
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Error loading ledger entries", ex);
            }

            return entries;
        }

        private static string ExtractReference(string description)
        {
            if (string.IsNullOrWhiteSpace(description))
                return "";

            int refIndex = description.IndexOf("Ref:", StringComparison.OrdinalIgnoreCase);
            if (refIndex < 0)
                return "";

            string refPart = description.Substring(refIndex + 4).Trim();

            if (string.IsNullOrEmpty(refPart))
                return "";

            // Stop at common separators
            char[] separators = { ' ', ',', ';', '-', ')', '(' };

            int endIndex = refPart.IndexOfAny(separators);

            if (endIndex > 0)
                return refPart.Substring(0, endIndex).Trim();

            return refPart.Trim();
        }

        public static LedgerSummaryInfo GetLedgerSummary(int partyId)
        {
            var entries = GetLedgerEntriesByPartyId(partyId);
            
            decimal runningBalance = entries.Count > 0 ? entries.Last().Balance : 0;
            decimal outstandingAmount = runningBalance < 0 ? Math.Abs(runningBalance) : 0;
            decimal totalCredit = entries.Sum(e => e.Credit);
            decimal totalDebit = entries.Sum(e => e.Debit);

            return new LedgerSummaryInfo
            {
                RunningBalance = runningBalance,
                OutstandingAmount = outstandingAmount,
                TotalCredit = totalCredit,
                TotalDebit = totalDebit
            };
        }

        public static List<AgingReportInfo> GetReceivablesAgingReport(DateTime asOfDate, int? partyId = null)
        {
            var report = new List<AgingReportInfo>();

            string query = @"
                SELECT 
                    p.[PartyId],
                    p.[Name] as CustomerName,
                    t.[TransactionId],
                    t.[Date],
                    t.[Type],
                    t.[Amount],
                    DATEDIFF(DAY, t.[Date], @AsOfDate) as DaysAged
                FROM [dbo].[Transactions] t
                INNER JOIN [dbo].[Parties] p ON t.[PartyId] = p.[PartyId]
                WHERE t.[Date] <= @AsOfDate
                  AND (@PartyId IS NULL OR t.[PartyId] = @PartyId)
                ORDER BY p.[PartyId], t.[Date] ASC, t.[TransactionId] ASC";

            try
            {
                using var connection = DatabaseHelper.GetConnection();
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@AsOfDate", asOfDate.Date);
                command.Parameters.AddWithValue("@PartyId", partyId ?? (object)DBNull.Value);
                connection.Open();

                using var reader = command.ExecuteReader();
                var customerData = new Dictionary<int, CustomerAgingData>();

                while (reader.Read())
                {
                    int partyIdOrd = reader.GetOrdinal("PartyId");
                    int customerNameOrd = reader.GetOrdinal("CustomerName");
                    int dateOrd = reader.GetOrdinal("Date");
                    int typeOrd = reader.GetOrdinal("Type");
                    int amountOrd = reader.GetOrdinal("Amount");
                    int daysAgedOrd = reader.GetOrdinal("DaysAged");

                    int customerPartyId = reader.GetInt32(partyIdOrd);
                    string customerName = reader.IsDBNull(customerNameOrd) ? "" : reader.GetString(customerNameOrd);
                    DateTime transactionDate = reader.IsDBNull(dateOrd) ? DateTime.Now : reader.GetDateTime(dateOrd);
                    string transactionType = reader.IsDBNull(typeOrd) ? "" : reader.GetString(typeOrd);
                    decimal amount = reader.IsDBNull(amountOrd) ? 0 : reader.GetDecimal(amountOrd);
                    int daysAged = reader.IsDBNull(daysAgedOrd) ? 0 : reader.GetInt32(daysAgedOrd);

                    if (!customerData.ContainsKey(customerPartyId))
                    {
                        customerData[customerPartyId] = new CustomerAgingData
                        {
                            PartyId = customerPartyId,
                            CustomerName = customerName,
                            OldestInvoiceDate = asOfDate,
                            Transactions = new List<(DateTime Date, string Type, decimal Amount, int DaysAged)>()
                        };
                    }

                    var data = customerData[customerPartyId];
                    data.Transactions.Add((transactionDate, transactionType, amount, daysAged));
                }
                reader.Close();

                foreach (var kvp in customerData)
                {
                    var data = kvp.Value;
                    decimal runningBalance = 0;
                    DateTime? oldestDebitDate = null;

                    data.Transactions.Sort((a, b) => a.Date.CompareTo(b.Date));

                    foreach (var trans in data.Transactions)
                    {
                        if (trans.Type.Equals("Debit", StringComparison.OrdinalIgnoreCase))
                        {
                            runningBalance += trans.Amount;
                            if (oldestDebitDate == null || trans.Date < oldestDebitDate.Value)
                                oldestDebitDate = trans.Date;
                        }
                        else if (trans.Type.Equals("Credit", StringComparison.OrdinalIgnoreCase))
                        {
                            runningBalance -= trans.Amount;
                        }

                        if (trans.Type.Equals("Debit", StringComparison.OrdinalIgnoreCase) && runningBalance > 0)
                        {
                            decimal outstandingAmount = Math.Min(trans.Amount, runningBalance);
                            
                            if (trans.DaysAged <= 30)
                                data.Current += outstandingAmount;
                            else if (trans.DaysAged <= 60)
                                data.Days31to60 += outstandingAmount;
                            else if (trans.DaysAged <= 90)
                                data.Days61to90 += outstandingAmount;
                            else
                                data.Over90 += outstandingAmount;
                        }
                    }

                    data.TotalOutstanding = runningBalance;
                    data.OldestInvoiceDate = oldestDebitDate ?? asOfDate;

                    if (data.TotalOutstanding > 0)
                    {
                        report.Add(new AgingReportInfo
                        {
                            PartyId = data.PartyId,
                            CustomerName = data.CustomerName,
                            TotalOutstanding = data.TotalOutstanding,
                            Current = data.Current,
                            Days31to60 = data.Days31to60,
                            Days61to90 = data.Days61to90,
                            Over90 = data.Over90,
                            OldestInvoiceDate = data.OldestInvoiceDate
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading receivables aging report: {ex.Message}", ex);
            }

            return report;
        }

        public static List<StatementTransactionInfo> GetStatementTransactions(int partyId, DateTime fromDate, DateTime toDate)
        {
            var transactions = new List<StatementTransactionInfo>();

            string query = @"
                SELECT 
                    t.[TransactionId],
                    t.[Date],
                    t.[Type],
                    t.[Amount],
                    t.[Description]
                FROM [dbo].[Transactions] t
                WHERE t.[PartyId] = @PartyId
                  AND t.[Date] >= @FromDate
                  AND t.[Date] <= @ToDate
                ORDER BY t.[Date] ASC, t.[TransactionId] ASC";

            try
            {
                using var connection = DatabaseHelper.GetConnection();
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@PartyId", partyId);
                command.Parameters.AddWithValue("@FromDate", fromDate.Date);
                command.Parameters.AddWithValue("@ToDate", toDate.Date);
                connection.Open();

                using var reader = command.ExecuteReader();

                while (reader.Read())
                {
                    int transactionIdOrd = reader.GetOrdinal("TransactionId");
                    int dateOrd = reader.GetOrdinal("Date");
                    int typeOrd = reader.GetOrdinal("Type");
                    int amountOrd = reader.GetOrdinal("Amount");
                    int descriptionOrd = reader.GetOrdinal("Description");

                    DateTime transactionDate = reader.IsDBNull(dateOrd) ? DateTime.Now : reader.GetDateTime(dateOrd);
                    string transactionType = reader.IsDBNull(typeOrd) ? "" : reader.GetString(typeOrd);
                    decimal amount = reader.IsDBNull(amountOrd) ? 0 : reader.GetDecimal(amountOrd);
                    string description = reader.IsDBNull(descriptionOrd) ? "" : reader.GetString(descriptionOrd);

                    string reference = ExtractReference(description);


                    decimal debit = 0;
                    decimal credit = 0;

                    if (transactionType.Equals("Debit", StringComparison.OrdinalIgnoreCase))
                    {
                        debit = amount;
                    }
                    else if (transactionType.Equals("Credit", StringComparison.OrdinalIgnoreCase))
                    {
                        credit = amount;
                    }

                    transactions.Add(new StatementTransactionInfo
                    {
                        TransactionId = reader.GetInt32(transactionIdOrd),
                        TransactionDate = transactionDate,
                        Description = description,
                        Reference = reference,
                        Debit = debit,
                        Credit = credit,
                        Balance = 0 // Will be calculated in the page code
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading statement transactions: {ex.Message}", ex);
            }

            return transactions;
        }


        public static decimal GetOpeningBalance(int partyId, DateTime fromDate)
        {
            string query = @"
                SELECT 
                    SUM(CASE WHEN [Type] = 'Debit' THEN [Amount] ELSE 0 END) - 
                    SUM(CASE WHEN [Type] = 'Credit' THEN [Amount] ELSE 0 END) as OpeningBalance
                FROM [dbo].[Transactions]
                WHERE [PartyId] = @PartyId
                  AND [Date] < @FromDate";

            try
            {
                using var connection = DatabaseHelper.GetConnection();
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@PartyId", partyId);
                command.Parameters.AddWithValue("@FromDate", fromDate.Date);
                connection.Open();

                var result = command.ExecuteScalar();
                return result != null && result != DBNull.Value ? Convert.ToDecimal(result) : 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error calculating opening balance: {ex.Message}", ex);
            }
        }


        public class LedgerEntryInfo
        {
            public int TransactionId { get; set; }
            public DateTime Date { get; set; }
            public string Description { get; set; } = string.Empty;
            public string Reference { get; set; } = string.Empty;
            public decimal Debit { get; set; }
            public decimal Credit { get; set; }
            public decimal Balance { get; set; }
            public string Aging { get; set; } = string.Empty;
        }

        public class LedgerSummaryInfo
        {
            public decimal RunningBalance { get; set; }
            public decimal OutstandingAmount { get; set; }
            public decimal TotalCredit { get; set; }
            public decimal TotalDebit { get; set; }
        }

        public class AgingReportInfo
        {
            public int PartyId { get; set; }
            public string CustomerName { get; set; } = string.Empty;
            public decimal TotalOutstanding { get; set; }
            public decimal Current { get; set; }
            public decimal Days31to60 { get; set; }
            public decimal Days61to90 { get; set; }
            public decimal Over90 { get; set; }
            public DateTime OldestInvoiceDate { get; set; }
        }

        public class StatementTransactionInfo
        {
            public int TransactionId { get; set; }
            public DateTime TransactionDate { get; set; }
            public string Description { get; set; } = string.Empty;
            public string Reference { get; set; } = string.Empty;
            public decimal Debit { get; set; }
            public decimal Credit { get; set; }
            public decimal Balance { get; set; }
        }

        private class CustomerAgingData
        {
            public int PartyId { get; set; }
            public string CustomerName { get; set; } = string.Empty;
            public decimal TotalOutstanding { get; set; }
            public decimal Current { get; set; }
            public decimal Days31to60 { get; set; }
            public decimal Days61to90 { get; set; }
            public decimal Over90 { get; set; }
            public DateTime OldestInvoiceDate { get; set; }
            public List<(DateTime Date, string Type, decimal Amount, int DaysAged)> Transactions { get; set; } = new();
        }
    }
}

