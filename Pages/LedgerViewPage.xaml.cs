using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Project.Data;

namespace Project.Pages
{
    /// <summary>
    /// Interaction logic for LedgerViewPage.xaml
    /// </summary>
    public partial class LedgerViewPage : Page
    {
        public class CustomerInfo
        {
            public int PartyId { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        public class LedgerEntry
        {
            public DateTime Date { get; set; }
            public string Description { get; set; } = string.Empty;
            public string Reference { get; set; } = string.Empty;
            public decimal Debit { get; set; }
            public decimal Credit { get; set; }
            public decimal Balance { get; set; }
            public string Aging { get; set; } = string.Empty;
        }

        private List<CustomerInfo> _customers = new();
        private List<LedgerEntry> _ledgerEntries = new();
        private bool _isLoaded = false;

        public LedgerViewPage()
        {
            InitializeComponent();
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            try
            {
                await LoadCustomers();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}\n\nPlease ensure the database is set up correctly.", 
                    "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadCustomers()
        {
            try
            {
                var parties = await Task.Run(() => PartyDataAccess.GetAllParties());
                var mappedCustomers = parties.Select(p => new CustomerInfo
                {
                    PartyId = p.PartyId,
                    Name = p.Name ?? ""
                }).ToList();

                _customers = mappedCustomers;
                cmbSelectCustomer.ItemsSource = _customers;
                cmbSelectCustomer.DisplayMemberPath = "Name";
                cmbSelectCustomer.SelectedValuePath = "PartyId";

                if (_customers.Count > 0)
                {
                    cmbSelectCustomer.SelectedIndex = 0;
                }
                _isLoaded = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading customers: {ex.Message}\n\nPlease ensure the database is set up correctly.", 
                    "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task UpdateLedgerView()
        {
            if (cmbSelectCustomer.SelectedItem is CustomerInfo selectedCustomer)
            {
                try
                {
                    // Simple loading feedback
                    if (dgLedger != null) dgLedger.IsEnabled = false;

                    // Load ledger entries from database
                    var dbEntries = await Task.Run(() => LedgerDataAccess.GetLedgerEntriesByPartyId(selectedCustomer.PartyId));
                    var mappedEntries = dbEntries.Select(e => new LedgerEntry
                    {
                        Date = e.Date,
                        Description = e.Description,
                        Reference = e.Reference,
                        Debit = e.Debit,
                        Credit = e.Credit,
                        Balance = e.Balance,
                        Aging = e.Aging
                    }).ToList();

                    // Get summary from database
                    var summary = await Task.Run(() => LedgerDataAccess.GetLedgerSummary(selectedCustomer.PartyId));
                    
                        _ledgerEntries = mappedEntries;
                        dgLedger.ItemsSource = _ledgerEntries;
                        if (dgLedger != null) dgLedger.IsEnabled = true;

                        txtRunningBalance.Text = $"PKR {summary.RunningBalance:N2}";
                        txtOutstandingAmount.Text = $"PKR {summary.OutstandingAmount:N2}";
                        txtTotalCredit.Text = $"PKR {summary.TotalCredit:N2}";
                }
                catch (Exception ex)
                {
                        if (dgLedger != null) dgLedger.IsEnabled = true;
                        dgLedger.ItemsSource = null;
                        txtRunningBalance.Text = "PKR 0.00";
                        txtOutstandingAmount.Text = "PKR 0.00";
                        txtTotalCredit.Text = "PKR 0.00";
                    MessageBox.Show($"Error loading ledger: {ex.Message}", 
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                dgLedger.ItemsSource = null;
                txtRunningBalance.Text = "PKR 0.00";
                txtOutstandingAmount.Text = "PKR 0.00";
                txtTotalCredit.Text = "PKR 0.00";
            }
        }

        private async void CmbSelectCustomer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded) return;
            await UpdateLedgerView();
        }

        private void DgLedger_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Ledger entries are read-only, no action needed
        }

        private void BtnLinkToPlots_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Link to Plots/Projects functionality would be implemented here.\nThis would open a dialog to link the selected customer to specific plots or projects.", 
                "Link to Plots/Projects", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}

