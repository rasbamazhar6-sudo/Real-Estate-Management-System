using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Project.Data;

namespace Project.Pages
{
    public partial class QuickEntryPage : Page
    {
        private List<CustomerInfo> _customers = new();
        private List<TransactionDisplay> _transactions = new();
        
        public QuickEntryPage()
        {
            InitializeComponent();
            LoadCustomers();
            LoadTransactionsFromDatabase();
            dpTransactionDate.SelectedDate = DateTime.Now;
        }
        
        private void LoadCustomers()
        {
            try
            {
                var parties = PartyDataAccess.GetAllParties();
                _customers = parties.Select(p => new CustomerInfo
                {
                    PartyId = p.PartyId,
                    Name = p.Name ?? ""
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
        
        private void LoadTransactionsFromDatabase()
        {
            try
            {
                var dbTransactions = TransactionDataAccess.GetAllTransactions();
                _transactions = dbTransactions.Select(t => new TransactionDisplay
                {
                    TransactionId = t.TransactionId,
                    CustomerName = t.CustomerName ?? "",
                    TransactionType = t.TransactionType ?? "",
                    Amount = t.Amount,
                    TransactionDate = t.TransactionDate,
                    Reference = ExtractReference(t.Description ?? ""),
                    Description = t.Description ?? ""
                }).ToList();
                
                dgTransactions.ItemsSource = _transactions.OrderByDescending(t => t.TransactionDate).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading transactions: {ex.Message}\n\nPlease ensure the database is set up correctly.", 
                    "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private string ExtractReference(string description)
        {
            // Try to extract reference from description (format: "Ref: XXX - Description")
            if (description.StartsWith("Ref:", StringComparison.OrdinalIgnoreCase))
            {
                int dashIndex = description.IndexOf(" - ");
                if (dashIndex > 0)
                {
                    return description.Substring(4, dashIndex - 4).Trim();
                }
            }
            return "";
        }
        
        private void CmbTransactionType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Update UI based on transaction type if needed
        }
        
        private void DpTransactionDate_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            // Handle date selection if needed
        }
        
        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (cmbCustomer.SelectedItem == null)
            {
                MessageBox.Show("Please select a customer.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            if (string.IsNullOrWhiteSpace(txtAmount.Text) || !decimal.TryParse(txtAmount.Text, out decimal amount) || amount <= 0)
            {
                MessageBox.Show("Please enter a valid amount.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            if (dpTransactionDate.SelectedDate == null)
            {
                MessageBox.Show("Please select a transaction date.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            try
            {
                var customer = (CustomerInfo)cmbCustomer.SelectedItem;
                var transactionType = ((ComboBoxItem)cmbTransactionType.SelectedItem)?.Content?.ToString() ?? "Debit";
                
                // Combine Reference and Description
                string reference = txtReference.Text?.Trim() ?? "";
                string description = txtDescription.Text?.Trim() ?? "";
                string fullDescription = "";
                
                if (!string.IsNullOrWhiteSpace(reference) && !string.IsNullOrWhiteSpace(description))
                {
                    fullDescription = $"Ref: {reference} - {description}";
                }
                else if (!string.IsNullOrWhiteSpace(reference))
                {
                    fullDescription = $"Ref: {reference}";
                }
                else if (!string.IsNullOrWhiteSpace(description))
                {
                    fullDescription = description;
                }
                
                // Insert transaction into database
                int transactionId = TransactionDataAccess.InsertTransaction(
                    customer.PartyId,
                    transactionType,
                    amount,
                    dpTransactionDate.SelectedDate.Value,
                    string.IsNullOrWhiteSpace(fullDescription) ? null : fullDescription,
                    null, // SaleId - not linked to a sale
                    null  // InstallmentId - not linked to an installment
                );
                
                MessageBox.Show("Transaction saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                
                // Reload transactions from database
                LoadTransactionsFromDatabase();
                BtnClear_Click(sender, e);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving transaction: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            cmbCustomer.SelectedItem = null;
            cmbTransactionType.SelectedIndex = 0;
            txtAmount.Clear();
            dpTransactionDate.SelectedDate = DateTime.Now;
            txtReference.Clear();
            txtDescription.Clear();
        }
        
        private void DgTransactions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Handle selection if needed
        }
        
        private class CustomerInfo
        {
            public int PartyId { get; set; }
            public string Name { get; set; } = string.Empty;
        }
        
        private class TransactionDisplay
        {
            public int TransactionId { get; set; }
            public string CustomerName { get; set; } = string.Empty;
            public string TransactionType { get; set; } = string.Empty;
            public decimal Amount { get; set; }
            public DateTime TransactionDate { get; set; }
            public string Reference { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
        }
    }
}
