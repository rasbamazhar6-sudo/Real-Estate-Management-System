using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Project.Data;
using Project.Services;

namespace Project.Pages
{
    public partial class CustomerStatementsPage : Page
    {
        private List<CustomerInfo> _customers = new();
        private List<StatementTransaction> _statementTransactions = new();
        
        public CustomerStatementsPage()
        {
            InitializeComponent();
            LoadCustomers();
            dpToDate.SelectedDate = DateTime.Now;
            dpFromDate.SelectedDate = DateTime.Now.AddDays(-30);
        }
        
        private void LoadCustomers()
        {
            try
            {
                var parties = PartyDataAccess.GetAllParties();
                _customers = parties.Select(p => new CustomerInfo
                {
                    PartyId = p.PartyId,
                    Name = p.Name ?? "",
                    Address = p.Address ?? ""
                }).ToList();

                cmbCustomer.ItemsSource = _customers;
                cmbCustomer.DisplayMemberPath = "Name";
                cmbCustomer.SelectedValuePath = "PartyId";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading customers: {ex.Message}\n\nPlease ensure the database is set up correctly.", 
                    "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private class CustomerInfo
        {
            public int PartyId { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Address { get; set; } = string.Empty;
        }
        
        private void CmbCustomer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbCustomer.SelectedItem != null)
            {
                ResetStatementState();
            }
        }
        
        private void CmbPeriod_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbPeriod.SelectedItem == null) return;
            
            var selectedItem = cmbPeriod.SelectedItem as ComboBoxItem;
            if (selectedItem == null || selectedItem.Content == null) return;
            
            var content = selectedItem.Content.ToString();
            if (content == null) return;
            
            if (content == "Custom Range")
            {
                if (pnlDateRange != null) pnlDateRange.Visibility = Visibility.Visible;
                if (pnlToDate != null) pnlToDate.Visibility = Visibility.Visible;
            }
            else
            {
                if (pnlDateRange != null) pnlDateRange.Visibility = Visibility.Collapsed;
                if (pnlToDate != null) pnlToDate.Visibility = Visibility.Collapsed;
                
                var days = content switch
                {
                    "Last 30 Days" => 30,
                    "Last 60 Days" => 60,
                    "Last 90 Days" => 90,
                    _ => 30
                };
                
                if (dpToDate != null) dpToDate.SelectedDate = DateTime.Now;
                if (dpFromDate != null) dpFromDate.SelectedDate = DateTime.Now.AddDays(-days);
            }
            
            if (cmbCustomer != null && cmbCustomer.SelectedItem != null)
            {
                ResetStatementState();
            }
        }

        private void ResetStatementState()
        {

            // Disable export buttons
            btnExportPDF.IsEnabled = false;
            btnExportExcel.IsEnabled = false;

            // Clear UI (optional but recommended)
            dgStatement.ItemsSource = null;

            txtOpeningBalance.Text = "PKR 0.00";
            txtClosingBalance.Text = "PKR 0.00";
            txtOutstanding.Text = "PKR 0.00";
        }

        private void BtnGenerate_Click(object sender, RoutedEventArgs e)
        {
            if (cmbCustomer.SelectedItem == null)
            {
                MessageBox.Show("Please select a customer.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            GenerateStatement();


            btnExportPDF.IsEnabled = true;
            btnExportExcel.IsEnabled = true;
        }

        private void GenerateStatement()
        {

            if (cmbCustomer.SelectedItem == null) return;
            
            try
            {
                var customer = (CustomerInfo)cmbCustomer.SelectedItem;
                txtCustomerName.Text = customer.Name;
                txtCustomerAddress.Text = customer.Address;
                
                var fromDate = dpFromDate.SelectedDate ?? DateTime.Now.AddDays(-30);
                var toDate = dpToDate.SelectedDate ?? DateTime.Now;
                txtStatementPeriod.Text = $"{fromDate:dd/MM/yyyy} to {toDate:dd/MM/yyyy}";
                
                // Get opening balance (balance before fromDate)
                decimal openingBalance = LedgerDataAccess.GetOpeningBalance(customer.PartyId, fromDate);
                txtOpeningBalance.Text = $"PKR {openingBalance:N2}";
                
                // Load statement transactions from database
                var dbTransactions = LedgerDataAccess.GetStatementTransactions(customer.PartyId, fromDate, toDate);
                
                // Calculate running balance starting from opening balance
                decimal runningBalance = openingBalance;
                _statementTransactions = new List<StatementTransaction>();
                
                foreach (var t in dbTransactions.OrderBy(tr => tr.TransactionDate).ThenBy(tr => tr.TransactionId))
                {
                    runningBalance += t.Debit - t.Credit;
                    
                    _statementTransactions.Add(new StatementTransaction
                    {
                        TransactionDate = t.TransactionDate,
                        Description = t.Description,
                        Reference = t.Reference,
                        Debit = t.Debit,
                        Credit = t.Credit,
                        Balance = runningBalance
                    });
                }
                
                decimal closingBalance = _statementTransactions.Count > 0 
                    ? _statementTransactions.Last().Balance 
                    : openingBalance;
                
                txtClosingBalance.Text = $"PKR {closingBalance:N2}";
                txtOutstanding.Text = closingBalance > 0 ? $"PKR {closingBalance:N2}" : "PKR 0.00";
                
                dgStatement.ItemsSource = _statementTransactions.OrderBy(t => t.TransactionDate).ToList();

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating statement: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void BtnExportPDF_Click(object sender, RoutedEventArgs e)
        {
            if (_statementTransactions == null || !_statementTransactions.Any())
            {
                MessageBox.Show("No data to export.");
                return;
            }

            ExportService.ExportToPDF(dgStatement, "Customer Statement", "CustomerStatement.pdf");
        }
        
        private void BtnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            if (_statementTransactions == null || !_statementTransactions.Any())
            {
                MessageBox.Show("No data to export.");
                return;
            }

            ExportService.ExportToExcel(dgStatement, "CustomerStatement.xlsx");
        }
        
        
        private class StatementTransaction
        {
            public DateTime TransactionDate { get; set; }
            public string Description { get; set; } = string.Empty;
            public string Reference { get; set; } = string.Empty;
            public decimal Debit { get; set; }
            public decimal Credit { get; set; }
            public decimal Balance { get; set; }
        }
    }
}
